using System;
using System.Collections.Generic;
using PlanetOne.Core;

internal static class SpaceTrafficTests
{
    internal static void Register(Action<string,Action> run)
    {
        run("Space traffic counterflow stays separated throughout each swept tick",Counterflow);
        run("Space traffic mixed crossing fleets reach distinct occupied destinations",Crossing);
        run("Space traffic own enclosing formation makes room without losing orders",Enclosed);
        run("Space traffic foreign closed formation is not commanded or traversed",ForeignEnclosure);
        run("Space traffic routes around a known protected orbit without transit permission",ProtectedOrbit);
        run("A foreign moon cannot seal the owner's parent-world return port",ParentPortAccess);
        run("Space traffic blocked destination never reports a false arrival",BlockedDestination);
        run("Space traffic newly occupied arrival slot changes after a real stall",OccupiedAfterReservation);
        run("Space traffic production and orbital return find physically free positions",SpawnAndReturn);
        run("Space traffic blocked paid launch waits and completes once after clearing",BlockedSpawn);
        run("Space traffic separated docks transfer real cargo and guide observed AI colonies",DocksAndAi);
        run("Space traffic save load resumes yielding and cargo exactly",Persistence);
        run("Space traffic overlapping saves are rejected without relocating bodies",LegacyOverlap);
    }
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    private static void ProtectedOrbit()
    {
        var s=World();s.State.Relations[0].Relation=RelationKind.Peace;
        var world=s.FindPlanet(2);world.OwnerId=2;world.OrbitRadius=1;world.OrbitEccentricity=0;world.OrbitPhase=0;world.OrbitPeriapsis=0;world.OrbitPeriodSeconds=1000000000;
        var ship=Add(s,-8,0,ShipKind.Scout);Move(s,ship,10,0);bool detour=false;
        for(int i=0;i<600&&ship.Order!=ShipOrder.Hold||i==0;i++)
        {
            CheckedStep(s);var center=s.GetOrbitPosition(2);double x=ship.X-center.X,y=ship.Y-center.Y;
            Check(x*x+y*y>=Definitions.OrbitalRadius*Definitions.OrbitalRadius-.00001,"Detour entered the protected orbit without permission");
            detour|=Math.Abs(ship.Y)>1;
        }
        Check(detour&&ship.Order==ShipOrder.Hold&&Math.Abs(ship.X-10)<.1,"Ship stopped at a protected orbit instead of flying around it");
        Check(!s.HasAccess(1,2,2,AccessRight.CivilTransit),"Detour invented a transit agreement");
    }
    private static void ParentPortAccess()
    {
        var s=World();s.State.Relations[0].Relation=RelationKind.Peace;
        var moon=s.State.Planets.Find(p=>p.ParentId>=0);Check(moon!=null,"Missing moon fixture");moon.ParentId=1;
        moon.OwnerId=2;moon.OrbitRadius=2;moon.OrbitEccentricity=0;moon.OrbitPhase=0;moon.OrbitPeriapsis=0;moon.OrbitPeriodSeconds=1000000000;
        var home=s.GetOrbitPosition(1);var ship=Add(s,home.X+5,home.Y,ShipKind.LandingTransport);
        s.Enqueue(new SimCommand{Id=s.NextCommandId(),IssuerId=1,PlanetId=1,ActorId=ship.Id,Kind=CommandKind.RetreatShip});
        for(int i=0;i<1000;i++)
        {
            CheckedStep(s);var center=s.GetOrbitPosition(moon.Id);double dx=ship.X-center.X,dy=ship.Y-center.Y;
            Check(dx*dx+dy*dy>=.8*.8-.00001,"Return entered a foreign moon's protected local orbit");
            if(ship.Order==ShipOrder.Guard&&s.AtPlanetDock(ship,1))break;
        }
        Check(ship.Order==ShipOrder.Guard&&s.AtPlanetDock(ship,1),"Foreign moon blocked the actual parent port");
        Check(!s.HasAccess(1,2,moon.Id,AccessRight.MilitaryTransit),"Return invented access to the foreign moon");
    }
    private static GameSimulation World(){var s=GameSimulation.CreateDefault();s.State.SpaceTestFixture=true;return s;}
    private static ShipState Add(GameSimulation s,double x,double y,ShipKind kind=ShipKind.Freighter,int owner=1)
    {
        var d=Definitions.Ships[(int)kind];var w=Definitions.SpaceWeapons[(int)kind];
        var ship=new ShipState{Id=s.State.NextEntityId++,Kind=kind,OwnerId=owner,X=x,Y=y,Crew=d.Crew,Health=w.Health,Ammunition=w.Ammo};
        s.State.Ships.Add(ship);return ship;
    }
    private static void Move(GameSimulation s,ShipState ship,int x,int y)
    {s.Enqueue(new SimCommand{Id=s.NextCommandId(),IssuerId=ship.OwnerId,Kind=CommandKind.MoveShipPosition,ActorId=ship.Id,PlanetId=1,X=x,Y=y});}
    private static bool Active(ShipState s){return !s.Destroyed && !s.Consumed && s.TransportPhase==TransportPhase.Space;}
    private static double Dist(ShipState a,ShipState b){double x=a.X-b.X,y=a.Y-b.Y;return Math.Sqrt(x*x+y*y);}
    private static void CheckedStep(GameSimulation s)
    {
        var before=new Dictionary<long,SimPoint>();foreach(var ship in s.State.Ships)if(Active(ship))before[ship.Id]=new SimPoint(ship.X,ship.Y);
        s.Step();
        for(int i=0;i<s.State.Ships.Count;i++)
        {
            var a=s.State.Ships[i];if(!Active(a))continue;SimPoint pa;
            if(before.TryGetValue(a.Id,out pa))
            {double dx=a.X-pa.X,dy=a.Y-pa.Y,limit=a.Kind==ShipKind.OrbitalYard?.15:Definitions.Ships[(int)a.Kind].Speed*Definitions.TickSeconds;Check(dx*dx+dy*dy<=limit*limit+1e-8,"Ship exceeded physical speed: "+a.Id);}
            else pa=new SimPoint(a.X,a.Y);
            for(int j=i+1;j<s.State.Ships.Count;j++)
            {
                var b=s.State.Ships[j];if(!Active(b))continue;
                double radius=Definitions.ShipRadius(a.Kind)+Definitions.ShipRadius(b.Kind)+Definitions.SpaceTrafficGap;
                Check(Dist(a,b)>=radius-1e-6,"Ships overlap at tick "+s.State.Tick+": "+a.Id+" / "+b.Id);
                SimPoint pb;if(!before.TryGetValue(b.Id,out pb) || !before.ContainsKey(a.Id))continue;
                double rx=pa.X-pb.X,ry=pa.Y-pb.Y,vx=a.X-pa.X-b.X+pb.X,vy=a.Y-pa.Y-b.Y+pb.Y;
                double speed=vx*vx+vy*vy,t=speed<1e-16?0:Math.Max(0,Math.Min(1,-(rx*vx+ry*vy)/speed));
                Check((rx+t*vx)*(rx+t*vx)+(ry+t*vy)*(ry+t*vy)>=radius*radius-1e-6,"Swept paths intersect between ticks");
            }
        }
    }
    private static void Counterflow()
    {
        var s=World();var a=Add(s,75,80);var b=Add(s,85,80,ShipKind.Scout);
        Move(s,a,85,80);Move(s,b,75,80);
        for(int i=0;i<180;i++)CheckedStep(s);
        Check(a.Order==ShipOrder.Hold && b.Order==ShipOrder.Hold && a.X>84 && b.X<76,"Counterflow failed to finish");
        // Repeat with the same participants to expose fixed-priority starvation or alternating sides.
        Move(s,a,75,80);Move(s,b,85,80);for(int i=0;i<180;i++)CheckedStep(s);
        Check(a.Order==ShipOrder.Hold && b.Order==ShipOrder.Hold && a.X<76 && b.X>84,"Repeated encounter starved a ship");
    }
    private static void Crossing()
    {
        var s=World();var ships=new List<ShipState>();
        for(int i=0;i<6;i++)
        {double angle=i*Math.PI/3;var ship=Add(s,80+Math.Cos(angle)*5,80+Math.Sin(angle)*5,(ShipKind)(i==0?6:i==1?7:i==2?4:0));ships.Add(ship);Move(s,ship,80,80);}
        for(int i=0;i<320;i++)CheckedStep(s);
        foreach(var ship in ships)Check(ship.Order==ShipOrder.Hold && Math.Abs(ship.X-80)<3 && Math.Abs(ship.Y-80)<3,"Shared destination never obtained a free arrival slot");
    }
    private static void Enclosed()
    {
        var s=World();var mover=Add(s,80,80);var blockers=new List<ShipState>();
        for(int i=0;i<6;i++){double angle=i*Math.PI/3;blockers.Add(Add(s,80+Math.Cos(angle)*.68,80+Math.Sin(angle)*.68));}
        Move(s,mover,85,80);bool yielded=false;
        for(int i=0;i<280;i++){CheckedStep(s);foreach(var blocker in blockers)yielded|=blocker.TrafficYieldUntil>s.State.Tick;}
        Check(yielded && mover.Order==ShipOrder.Hold && mover.X>84.8,"Enclosed own ship did not gain an exit");
        foreach(var blocker in blockers)Check(blocker.Order==ShipOrder.Hold && blocker.Cargo[2]==0,"Making room replaced an original order or cargo");
    }
    private static void ForeignEnclosure()
    {
        var s=World();var mover=Add(s,80,80);var blockers=new List<ShipState>();
        for(int i=0;i<6;i++){double angle=i*Math.PI/3;blockers.Add(Add(s,80+Math.Cos(angle)*.68,80+Math.Sin(angle)*.68,ShipKind.Freighter,2));}
        Move(s,mover,85,80);for(int i=0;i<80;i++)CheckedStep(s);
        Check(mover.Order==ShipOrder.MovePosition && Math.Abs(mover.X-80)<.1 && Math.Abs(mover.Y-80)<.1,"Ship tunneled through a sealed foreign ring");
        foreach(var blocker in blockers)Check(blocker.TrafficYieldUntil==0 && blocker.Order==ShipOrder.Hold,"Foreign owner received an automatic command");
    }
    private static void BlockedDestination()
    {
        var s=World();var mover=Add(s,76,80);
        for(int i=0;i<6;i++){double angle=i*Math.PI/3;Add(s,80+Math.Cos(angle)*.68,80+Math.Sin(angle)*.68,ShipKind.Freighter,2);}
        Move(s,mover,80,80);for(int i=0;i<180;i++)CheckedStep(s);
        Check(mover.Order==ShipOrder.MovePosition,"Inaccessible reserved center reported arrival");
    }
    private static void SpawnAndReturn()
    {
        var s=World();var planet=s.FindPlanet(1);var center=s.GetOrbitPosition(1);
        var port=InvasionFixture.AddBuilding(s,1,BuildingKind.Spaceport,9,5);port.Inventory=new[]{20,0,80,40,0,0,0};
        var yard=Add(s,center.X,center.Y,ShipKind.OrbitalYard);yard.TargetPlanetId=1;
        var carrier=Add(s,center.X,center.Y,ShipKind.LandingTransport);carrier.TargetPlanetId=1;carrier.TransportPhase=TransportPhase.Ascending;
        carrier.LandingX=20;carrier.LandingY=20;carrier.ActionProgress=Definitions.TransitionSeconds-.01;
        var def=Definitions.Ships[(int)ShipKind.Scout];yard.ProductionKind=(int)ShipKind.Scout;yard.ProductionPaid=true;yard.ProductionProgress=def.Seconds-.01;
        planet.Population-=def.Crew;for(int r=0;r<Definitions.ResourceCount;r++)planet.ConsumedTotals[r]+=def.Cost[r];
        CheckedStep(s);Check(s.State.Ships.Count==3 && carrier.TransportPhase==TransportPhase.Space,"Ready production or orbital return failed");
        for(int i=0;i<60;i++)CheckedStep(s);SaveCodec.Validate(s.State);
        Check(yard.ProductionKind==-1 && s.State.Ships.Count==3,"Paid ship was duplicated");
    }
    private static void OccupiedAfterReservation()
    {
        var s=World();var mover=Add(s,70,80);var other=Add(s,80,83,ShipKind.Freighter,2);
        Move(s,mover,80,80);CheckedStep(s);
        Check(mover.TrafficGoalSet && Math.Abs(mover.TrafficOffsetX)+Math.Abs(mover.TrafficOffsetY)<1e-8,"Fixture failed to reserve initially free center");
        Move(s,other,80,80);for(int i=0;i<160;i++)CheckedStep(s);
        Check(other.Order==ShipOrder.Hold && mover.Order==ShipOrder.Hold && Dist(mover,other)<1.5,"Later occupied slot permanently blocked a free adjacent arrival");
        Check(other.TrafficYieldUntil==0,"Foreign arriving ship was commanded to yield");
    }
    private static void Persistence()
    {
        var s=World();var mover=Add(s,80,80);var blocker=Add(s,80.68,80);
        // A transport carries actual supplies while its unmodified mission temporarily yields.
        blocker.Kind=ShipKind.LandingTransport;blocker.Crew=Definitions.Ships[(int)blocker.Kind].Crew;
        blocker.Health=Definitions.SpaceWeapons[(int)blocker.Kind].Health;blocker.Cargo[2]=7;blocker.X=80.72;
        // An enclosing formation genuinely requires cooperation; an isolated ship must
        // now stay parked while the mover flies around it.
        for(int i=1;i<6;i++){double a=i*Math.PI/3;Add(s,80+Math.Cos(a)*.72,80+Math.Sin(a)*.72);}
        Move(s,mover,85,80);
        for(int i=0;i<100&&blocker.TrafficYieldUntil<=s.State.Tick;i++)CheckedStep(s);
        Check(blocker.TrafficYieldUntil>s.State.Tick,"Persistence fixture never entered yielding");
        var copy=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(s.State)));
        for(int i=0;i<100;i++){CheckedStep(s);CheckedStep(copy);}
        Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(copy.State),"Saved traffic continuation diverged");
        Check(blocker.Cargo[2]==7 && blocker.Order==ShipOrder.Hold,"Yielding lost physical cargo or mission");
    }
    private static void DocksAndAi()
    {
        var s=World();var center=s.GetOrbitPosition(1);
        var port=InvasionFixture.AddBuilding(s,1,BuildingKind.Spaceport,9,5);port.Inventory[2]=20;
        var yard=Add(s,center.X,center.Y,ShipKind.OrbitalYard);yard.TargetPlanetId=1;
        var carrier=Add(s,center.X+4,center.Y,ShipKind.LandingTransport);
        s.Enqueue(new SimCommand{Id=s.NextCommandId(),IssuerId=1,Kind=CommandKind.FlyShip,ActorId=carrier.Id,PlanetId=1});
        for(int i=0;i<100;i++)CheckedStep(s);
        Check(s.AtPlanetDock(carrier,1) && Dist(carrier,yard)>.7,"Separated ship never became docked");
        long command=s.NextCommandId();s.Enqueue(new SimCommand{Id=command,IssuerId=1,Kind=CommandKind.LoadShipSupplies,ActorId=carrier.Id,TargetId=port.Id,PlanetId=1,IntValue=2,Amount=3});
        CheckedStep(s);Check(s.State.CommandResults.Find(r=>r.CommandId==command).Success && carrier.Cargo[2]==3,"Valid offset dock could not load real cargo");
        var view=new AiObservation{FactionId=1,Tick=40,Research=new ResearchState()};view.Research.Completed.Add(Technology.Transport);
        var neutral=new AiWorld{Id=3,OwnerId=0,Current=true,X=80,Y=80};view.Worlds.Add(neutral);
        var colony=new ShipState{Id=999,OwnerId=1,Kind=ShipKind.ColonyShip,X=80.8,Y=80,TrafficGoalSet=true,TrafficAnchorX=80,TrafficAnchorY=80,TrafficOffsetX=.8};view.Ships.Add(colony);
        var plan=AiPlanner.Plan(view,new AiState{FactionId=1});
        Check(plan.Command!=null && plan.Command.Kind==CommandKind.Colonize,"AI failed to recognize its own offset docking position");
        neutral.Current=false;plan=AiPlanner.Plan(view,new AiState{FactionId=1});
        Check(plan.Command==null || plan.Command.Kind!=CommandKind.Colonize,"Stale world information authorized AI colonization");
    }
    private static void BlockedSpawn()
    {
        var s=World();var planet=s.FindPlanet(1);var center=s.GetOrbitPosition(1);
        var port=InvasionFixture.AddBuilding(s,1,BuildingKind.Spaceport,9,5);
        var def=Definitions.Ships[0];port.ShipBuildKind=0;port.ShipBuildPaid=true;port.ShipBuildProgress=def.Seconds-.001;
        planet.Population-=def.Crew;for(int r=0;r<Definitions.ResourceCount;r++)planet.ConsumedTotals[r]+=def.Cost[r];
        for(int x=-4;x<=4;x++)for(int y=-4;y<=4;y++)Add(s,center.X+x*.70,center.Y+y*.70);
        int count=s.State.Ships.Count,population=planet.Population;CheckedStep(s);
        Check(s.State.Ships.Count==count && port.ShipBuildPaid && port.ShipBuildKind==0 && port.ShipBuildProgress==def.Seconds,"Occupied launch created overlapping ship or discarded paid production");
        SaveCodec.Validate(s.State);
        foreach(var ship in s.State.Ships)s.DestroyShip(ship.Id);
        CheckedStep(s);Check(s.State.Ships.Count==count+1 && port.ShipBuildKind==-1 && planet.Population==population,"Clearing launch duplicated crew payment or lost ship");
        CheckedStep(s);Check(s.State.Ships.Count==count+1,"Completed launch repeated");
    }
    private static void LegacyOverlap()
    {
        var s=World();var a=Add(s,80,80);var b=Add(s,80,80);double previous=0;
        bool rejected=false;try{SaveCodec.ToJson(s.State);}catch(System.IO.InvalidDataException){rejected=true;}
        Check(rejected && a.X==80 && b.X==80,"Invalid overlapping save was accepted or silently relocated bodies");
        for(int i=0;i<20;i++)
        {
            double ax=a.X,ay=a.Y,bx=b.X,by=b.Y;s.Step();double limit=Definitions.Ships[0].Speed*Definitions.TickSeconds;
            Check((a.X-ax)*(a.X-ax)+(a.Y-ay)*(a.Y-ay)<=limit*limit+1e-8 && (b.X-bx)*(b.X-bx)+(b.Y-by)*(b.Y-by)<=limit*limit+1e-8,"Legacy separation teleported");
            Check(Dist(a,b)>=previous-1e-8,"Legacy overlap worsened");previous=Dist(a,b);
        }
        Check(previous>=Definitions.ShipRadius(a.Kind)+Definitions.ShipRadius(b.Kind),"Legacy overlap never cleared");
    }
}
