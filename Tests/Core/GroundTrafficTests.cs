using System;
using System.Collections.Generic;
using System.Reflection;
using PlanetOne.Core;

internal static class GroundTrafficTests
{
    public static void Register(Action<string,Action> run)
    {
        run("Adjacent hexes accommodate two tanks without moving the parked tank",AdjacentTanks);
        run("An open detour leaves a stationary vehicle at its original position",StationaryDetour);
        run("A mid-edge mover returns to a free route instead of displacing a newly parked tank",MidEdgeDetour);
        run("Opposing road couriers use separate lanes without give-way maneuvers",TwoLanes);
        run("Ground crossing and opposing orders keep physical bodies and tick sweeps separate",Crossing);
        run("A surrounded friendly mover opens a cooperative gap without deleting orders",Surrounded);
        run("A sealed hostile bottleneck waits without moving the blocker",SealedEnemy);
        run("A completely full friendly corridor keeps orders and waits honestly",SealedFriendly);
        run("Intersecting active give-way intentions replan from actual positions",ActiveYieldCrossing);
        run("A newly blocked give-way destination replans without losing its order",ChangedYieldTerrain);
        run("A stationary service order makes room and retains its original target",ServicingBlocker);
        run("A docked helper keeps a cyclic encounter open until the outside courier passes",DockedYieldCycle);
        run("A courier leaves its passing dock when the requester starts stationary service",DockedServiceRequester);
        run("Saving during a real give-way maneuver preserves exact continuation",YieldSave);
        run("A group reaches distinct legal places around one shared destination",GroupArrival);
        run("A curved courier reroute preserves its actual world position and saved continuation",CurveReroute);
        run("Couriers and construction vehicles share real collision-free surface traffic",Couriers);
        run("Occupied recruitment exits retain paid production until a legal place is free",Recruitment);
        run("Occupied transport exits retain the actual embarked troop until a legal place is free",Unloading);
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void AdjacentTanks()
    {
        var sim=Fixture();var parked=Add(sim,22,20);var moving=Add(sim,18,20);
        Move(sim,moving,21,20);
        for(int i=0;i<600;i++)StepSafe(sim);
        Check(moving.Order==UnitOrder.Hold&&HexGrid.Distance(moving.X,moving.Y,21,20)<.001,"Tank cannot occupy the neighbouring hex");
        Check(parked.X==22&&parked.Y==20,"Parked tank displaced for an adjacent goal");SaveCodec.Validate(sim.State);
    }
    private static void StationaryDetour()
    {
        var sim=Fixture();var parked=Add(sim,22,20);var moving=Add(sim,18,20);Move(sim,moving,26,20);
        for(int i=0;i<900;i++){StepSafe(sim);Check(parked.X==22&&parked.Y==20,"Stationary tank moved despite an open detour");}
        Check(moving.Order==UnitOrder.Hold&&HexGrid.Distance(moving.X,moving.Y,26,20)<.001,"Detour never reached its goal");
    }
    private static void TwoLanes()
    {TwoLanes(false);TwoLanes(true);}
    private static void TwoLanes(bool curved)
    {
        var sim=Fixture();var p=sim.FindPlanet(1);var hq=p.Buildings[0];
        var depot=new BuildingState{Id=sim.State.NextEntityId++,Kind=BuildingKind.Warehouse,X=18,Y=curved?7:6,Built=true,Health=Definitions.BuildingHealth(BuildingKind.Warehouse)};p.Buildings.Add(depot);
        var a=new ShipmentState{Id=sim.State.NextEntityId++,SourceId=depot.Id,TargetId=hq.Id,Phase=ShipmentPhase.Returning,Traffic=new GroundTrafficState{Docked=true}};
        var b=new ShipmentState{Id=sim.State.NextEntityId++,SourceId=hq.Id,TargetId=depot.Id,Phase=ShipmentPhase.Returning,Traffic=new GroundTrafficState{Docked=true}};
        Check(GameSimulation.GroundCourierRadius(a)>.2&&GameSimulation.GroundCourierRadius(a)<GameSimulation.RoadLaneOffset,"Large courier hull does not fit its directional corridor");
        for(int x=8;x<=18;x++)
        {
            int y=curved&&x>12?7:6;
            p.Roads[y*p.Width+x]=true;a.Path.Add(new GridCell(x,y));b.Path.Insert(0,new GridCell(x,y));
            if(curved&&x==12){p.Roads[7*p.Width+x]=true;a.Path.Add(new GridCell(x,7));b.Path.Insert(0,new GridCell(x,7));}
        }
        p.Shipments.Add(a);p.Shipments.Add(b);
        bool simultaneous=false;
        for(int i=0;i<600&&p.Shipments.Count>0;i++)
        {StepSafe(sim);simultaneous|=p.Shipments.Count==2&&!GameSimulation.GroundCourierDocked(a)&&!GameSimulation.GroundCourierDocked(b);Check(!a.Traffic.Yielding&&!b.Traffic.Yielding,"Opposing lanes triggered an evasive maneuver");}
        Check(p.Shipments.Count==0&&simultaneous,"Opposing couriers did not depart concurrently and arrive");
    }
    private static void MidEdgeDetour()
    {
        var sim=Fixture();var moving=Add(sim,20,20);Move(sim,moving,26,20);
        while(moving.X<20.35)StepSafe(sim);
        var parked=Add(sim,22,20);parked.X=moving.X+1.02;double x=parked.X;
        SaveCodec.Validate(sim.State);
        for(int i=0;i<900;i++){StepSafe(sim);Check(parked.X==x&&parked.Y==20,"Newly parked tank displaced despite a free route behind the mover");}
        Check(moving.Order==UnitOrder.Hold&&HexGrid.Distance(moving.X,moving.Y,26,20)<.001,"Mid-edge detour failed to arrive");
    }
    private static void DockedServiceRequester()
    {
        var sim=Fixture();var p=sim.FindPlanet(1);var hq=p.Buildings[0];
        var source=new BuildingState{Id=sim.State.NextEntityId++,PlanetId=1,Kind=BuildingKind.Warehouse,X=4,Y=6,Built=true,Health=Definitions.BuildingHealth(BuildingKind.Warehouse)};
        var depot=new BuildingState{Id=sim.State.NextEntityId++,PlanetId=1,Kind=BuildingKind.MilitaryDepot,X=9,Y=5,Built=true,Health=Definitions.BuildingHealth(BuildingKind.MilitaryDepot)};
        p.Buildings.Add(source);p.Buildings.Add(depot);for(int x=4;x<=8;x++)p.Roads[6*p.Width+x]=true;
        var worker=Add(sim,8,5,1,UnitKind.Infantry);worker.Order=UnitOrder.Resupply;worker.TargetId=depot.Id;worker.Ammunition=0;
        var request=GameSimulation.GroundUnitPosition(worker);var dock=HexGrid.ToWorld(8,6);
        var courier=new ShipmentState{Id=sim.State.NextEntityId++,SourceId=source.Id,TargetId=hq.Id,Phase=ShipmentPhase.Returning,Traffic=new GroundTrafficState{Yielding=true,Docked=true,YieldForId=worker.Id,StartX=dock.X-.3,StartY=dock.Y,RequestX=request.X,RequestY=request.Y,GoalX=8,GoalY=6,Progress=1}};
        for(int x=8;x>=4;x--)courier.Path.Add(new GridCell(x,6));p.Shipments.Add(courier);sim.State.Tick=20;
        for(int i=0;i<200&&p.Shipments.Contains(courier);i++)StepSafe(sim);
        Check(!p.Shipments.Contains(courier)&&worker.Order==UnitOrder.Resupply&&worker.TargetId==depot.Id,"Stationary requester held a courier inside its passing dock forever");
        SaveCodec.Validate(sim.State);
    }
    private static void DockedYieldCycle()
    {
        var sim=Fixture();var planet=sim.FindPlanet(1);var headquarters=planet.Buildings[0];
        var depot=new BuildingState{Id=sim.State.NextEntityId++,PlanetId=1,Kind=BuildingKind.Warehouse,X=4,Y=6,Built=true,Health=Definitions.BuildingHealth(BuildingKind.Warehouse)};
        planet.Buildings.Add(depot);for(int x=4;x<=8;x++)planet.Roads[6*planet.Width+x]=true;
        double y=6*HexGrid.RowHeight;
        var inside=new ShipmentState{Id=sim.State.NextEntityId++,SourceId=depot.Id,TargetId=headquarters.Id,Phase=ShipmentPhase.Returning};
        var outside=new ShipmentState{Id=sim.State.NextEntityId++,SourceId=headquarters.Id,TargetId=depot.Id,Phase=ShipmentPhase.Returning};
        for(int x=8;x>=4;x--)inside.Path.Add(new GridCell(x,6));
        for(int x=4;x<=8;x++)outside.Path.Add(new GridCell(x,6));
        inside.Traffic=new GroundTrafficState{Yielding=true,Docked=true,YieldForId=outside.Id,StartX=6.8,StartY=y,RequestX=6.2,RequestY=y,GoalX=8,GoalY=6,Progress=1};
        outside.Traffic=new GroundTrafficState{Yielding=true,YieldForId=inside.Id,StartX=6.2,StartY=y,RequestX=6.8,RequestY=y,GoalX=5,GoalY=6,Progress=1};
        planet.Shipments.Add(inside);planet.Shipments.Add(outside);sim.State.Tick=20;
        StepSafe(sim);Check(inside.Traffic.Docked&&inside.Traffic.Yielding,"Interior helper abandoned its passing bay before the outside courier passed");
        for(int i=0;i<500&&planet.Shipments.Exists(s=>s.Id==inside.Id||s.Id==outside.Id);i++)StepSafe(sim);
        Check(!planet.Shipments.Exists(s=>s.Id==inside.Id||s.Id==outside.Id),"Cyclic encounter repeated instead of completing both physical returns");
        SaveCodec.Validate(sim.State);
    }
    private static GameSimulation Fixture()
    {
        var sim=GameSimulation.CreateDefault(727);sim.State.MilitaryTestFixture=true;
        Array.Clear(sim.FindPlanet(1).Terrain,0,sim.FindPlanet(1).Terrain.Length);
        return sim;
    }
    private static UnitState Add(GameSimulation sim,int x,int y,int owner=1,UnitKind kind=UnitKind.Tank)
    {
        var definition=Definitions.Get(kind);var unit=new UnitState{Id=sim.State.NextEntityId++,OwnerId=owner,Kind=kind,X=x,Y=y,GoalX=x,GoalY=y,Health=definition.Health,Ammunition=definition.Ammo,Food=definition.Food};
        sim.FindPlanet(1).Units.Add(unit);return unit;
    }
    private static void Move(GameSimulation sim,UnitState unit,int x,int y)
    {Check(sim.Enqueue(new SimCommand{Id=sim.NextCommandId(),IssuerId=unit.OwnerId,PlanetId=1,ActorId=unit.Id,Kind=CommandKind.MoveUnit,X=x,Y=y}),"Move intent rejected");}
    private sealed class Body {public long Id;public double Radius;public SimPoint Position;}
    private static List<Body> Bodies(GameSimulation sim)
    {
        var result=new List<Body>();var planet=sim.FindPlanet(1);
        foreach(var unit in planet.Units)if(!unit.Destroyed&&unit.Layer==TargetLayer.Ground)result.Add(new Body{Id=unit.Id,Radius=GameSimulation.GroundUnitRadius(unit.Kind),Position=GameSimulation.GroundUnitPosition(unit)});
        foreach(var shipment in planet.Shipments)if(!GameSimulation.GroundCourierDocked(shipment))result.Add(new Body{Id=shipment.Id,Radius=GameSimulation.GroundCourierRadius(shipment),Position=GameSimulation.GroundCourierPosition(shipment)});
        return result;
    }
    private static double Separation(SimPoint a,SimPoint b){double x=a.X-b.X,y=a.Y-b.Y;return Math.Sqrt(x*x+y*y);}
    private static double Segments(SimPoint a,SimPoint b,SimPoint c,SimPoint d)
    {
        double x=b.X-a.X,y=b.Y-a.Y,u=d.X-c.X,v=d.Y-c.Y,den=x*v-y*u;
        if(Math.Abs(den)>1e-12){double t=((c.X-a.X)*v-(c.Y-a.Y)*u)/den,s=((c.X-a.X)*y-(c.Y-a.Y)*x)/den;if(t>=0&&t<=1&&s>=0&&s<=1)return 0;}
        return Math.Min(Math.Min(GameSimulation.GroundPointSegmentDistance(a,c,d),GameSimulation.GroundPointSegmentDistance(b,c,d)),Math.Min(GameSimulation.GroundPointSegmentDistance(c,a,b),GameSimulation.GroundPointSegmentDistance(d,a,b)));
    }
    private static void StepSafe(GameSimulation sim)
    {
        var before=Bodies(sim);sim.Step();var after=Bodies(sim);
        for(int i=0;i<after.Count;i++)for(int j=i+1;j<after.Count;j++)
        {
            double limit=after[i].Radius+after[j].Radius-GameSimulation.GroundTrafficTolerance;
            Check(Separation(after[i].Position,after[j].Position)>=limit,"Bodies overlap at tick "+sim.State.Tick+": "+after[i].Id+" / "+after[j].Id);
            var a=before.Find(v=>v.Id==after[i].Id);var b=before.Find(v=>v.Id==after[j].Id);
            if(a!=null&&b!=null)Check(Segments(a.Position,after[i].Position,b.Position,after[j].Position)>=limit,"Tick capsules cross");
        }
    }
    private static void Crossing()
    {
        var sim=Fixture();var a=Add(sim,20,20);var b=Add(sim,22,18);Move(sim,a,24,20);Move(sim,b,22,23);
        for(int i=0;i<1000;i++)StepSafe(sim);
        Check(HexGrid.Distance(a.X,a.Y,24,20)<.01&&HexGrid.Distance(b.X,b.Y,22,23)<.01,"Crossing orders failed to reach their targets");
        Move(sim,a,20,20);Move(sim,b,24,20);
        for(int i=0;i<1000;i++)StepSafe(sim);
        Check(HexGrid.Distance(a.X,a.Y,20,20)<.01&&HexGrid.Distance(b.X,b.Y,24,20)<1.01,"Opposing movement failed to finish");
    }
    private static GameSimulation Ring(out UnitState mover)
    {
        var sim=Fixture();mover=Add(sim,22,22,1,UnitKind.Infantry);
        for(int y=19;y<=25;y++)for(int x=19;x<=25;x++)if(HexGrid.Steps(22,22,x,y)>0&&HexGrid.Steps(22,22,x,y)<=2)Add(sim,x,y,1,UnitKind.Infantry);
        Move(sim,mover,29,22);return sim;
    }
    private static void Surrounded()
    {
        UnitState mover;var sim=Ring(out mover);var yielded=new HashSet<long>();
        for(int i=0;i<1600;i++){StepSafe(sim);foreach(var unit in sim.FindPlanet(1).Units)if(unit.Traffic!=null&&unit.Traffic.Yielding)yielded.Add(unit.Id);}
        Check(yielded.Count>=2&&HexGrid.Distance(mover.X,mover.Y,29,22)<.01,"A friendly chain did not open a usable gap");
        foreach(var unit in sim.FindPlanet(1).Units)if(unit.Id!=mover.Id)Check(unit.Order==UnitOrder.Hold,"Temporary yielding erased the original hold order");
    }
    private static void SealedEnemy()
    {
        var sim=Fixture();var planet=sim.FindPlanet(1);for(int i=0;i<planet.Terrain.Length;i++)planet.Terrain[i]=1;
        for(int x=20;x<=24;x++)planet.Terrain[20*planet.Width+x]=0;
        var mover=Add(sim,20,20);var enemy=Add(sim,22,20,2);Move(sim,mover,24,20);
        for(int i=0;i<250;i++)StepSafe(sim);
        Check(enemy.X==22&&enemy.Y==20&&enemy.Traffic==null,"Foreign blocker was commanded to yield");
        Check(mover.Order==UnitOrder.Move&&mover.X<22,"A blocked route teleported or discarded its order");
    }
    private static void YieldSave()
    {
        UnitState mover;var sim=Ring(out mover);int ticks=0;
        while(!sim.FindPlanet(1).Units.Exists(u=>u.Traffic!=null&&u.Traffic.Yielding&&u.Traffic.Progress>0&&u.Traffic.Progress<1)&&ticks++<300)StepSafe(sim);
        Check(ticks<300,"No intermediate cooperative maneuver observed");
        var restored=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(sim.State)));
        for(int i=0;i<400;i++){StepSafe(sim);StepSafe(restored);}
        Check(SaveCodec.ToJson(sim.State)==SaveCodec.ToJson(restored.State),"Traffic continuation diverged after save/load");
    }
    private static void SealedFriendly()
    {
        var sim=Fixture();var planet=sim.FindPlanet(1);for(int i=0;i<planet.Terrain.Length;i++)planet.Terrain[i]=1;
        for(int x=20;x<=24;x++)planet.Terrain[20*planet.Width+x]=0;
        var a=Add(sim,20,20,1,UnitKind.Infantry);var b=Add(sim,22,20,1,UnitKind.Infantry);var c=Add(sim,24,20,1,UnitKind.Infantry);
        // There is no passing bay: neither order can cross the other two physical bodies.
        Move(sim,a,24,20);Move(sim,c,20,20);
        for(int i=0;i<600;i++)StepSafe(sim);
        Check(a.Order==UnitOrder.Move&&c.Order==UnitOrder.Move&&b.Order==UnitOrder.Hold,"Sealed traffic discarded an order");
        Check(a.X<b.X&&b.X<c.X,"Units crossed in a corridor without space to pass");
    }
    private static void ActiveYieldCrossing()
    {
        var sim=Fixture();var a=Add(sim,20,20,1,UnitKind.Infantry);var b=Add(sim,22,20,1,UnitKind.Infantry);
        // Explicit saved-intention fixture: physical positions are legal, but both old maneuver
        // corridors cross after a route repair. Neither unit may teleport out of the dependency.
        a.Order=b.Order=UnitOrder.Move;a.GoalX=28;a.GoalY=20;b.GoalX=16;b.GoalY=20;
        foreach(var unit in new[]{a,b}){var at=GameSimulation.GroundUnitPosition(unit);var other=unit==a?b:a;unit.Traffic=new GroundTrafficState{Yielding=true,YieldForId=other.Id,StartX=at.X,StartY=at.Y,GoalX=(int)other.X,GoalY=(int)other.Y};}
        SaveCodec.Validate(sim.State);
        for(int i=0;i<1000;i++)StepSafe(sim);
        Check(HexGrid.Distance(a.X,a.Y,28,20)<.01&&HexGrid.Distance(b.X,b.Y,16,20)<.01,"Active maneuver dependency did not resolve");
    }
    private static void ChangedYieldTerrain()
    {
        var sim=Fixture();var unit=Add(sim,20,20,1,UnitKind.Tank);var requester=Add(sim,18,20,1,UnitKind.Infantry);
        unit.Order=UnitOrder.Move;unit.GoalX=27;unit.GoalY=20;var at=GameSimulation.GroundUnitPosition(unit);
        unit.Traffic=new GroundTrafficState{Yielding=true,YieldForId=requester.Id,StartX=at.X,StartY=at.Y,GoalX=21,GoalY=20};
        // A newly placed obstruction invalidates the temporary parking place, not the real order.
        sim.FindPlanet(1).Buildings.Add(new BuildingState{Id=sim.State.NextEntityId++,PlanetId=1,Kind=BuildingKind.Wall,X=21,Y=20,Built=true,Health=Definitions.BuildingHealth(BuildingKind.Wall)});
        for(int i=0;i<1000;i++)StepSafe(sim);
        Check(HexGrid.Distance(unit.X,unit.Y,27,20)<.01,"Invalidated temporary destination trapped a movable unit");
    }
    private static void ServicingBlocker()
    {
        var sim=Fixture();var mover=Add(sim,20,20,1,UnitKind.Infantry);var worker=Add(sim,22,20,1,UnitKind.Infantry);
        var depot=new BuildingState{Id=sim.State.NextEntityId++,PlanetId=1,Kind=BuildingKind.MilitaryDepot,X=22,Y=21,Built=true,Health=Definitions.BuildingHealth(BuildingKind.MilitaryDepot)};
        sim.FindPlanet(1).Buildings.Add(depot);worker.Order=UnitOrder.Resupply;worker.TargetId=depot.Id;worker.GoalX=depot.X;worker.GoalY=depot.Y;worker.Ammunition=0;
        Move(sim,mover,22,20);bool yielded=false,arrived=false;
        for(int i=0;i<600;i++){StepSafe(sim);yielded|=worker.Traffic!=null&&worker.Traffic.Yielding;arrived|=HexGrid.Distance(mover.X,mover.Y,22,20)<.01;}
        Check(yielded&&arrived&&worker.Order==UnitOrder.Resupply&&worker.TargetId==depot.Id,"Stationary service was treated as perpetual motion or lost its task");
    }
    private static void Couriers()
    {
        var sim=GameSimulation.CreateDefault(727);var planet=sim.FindPlanet(1);
        foreach(var cell in new[]{new GridCell(10,5),new GridCell(12,5),new GridCell(14,5)})
            Check(sim.Enqueue(new SimCommand{Id=sim.NextCommandId(),IssuerId=1,PlanetId=1,Kind=CommandKind.PlaceBuilding,Building=BuildingKind.SolarPlant,X=cell.X,Y=cell.Y}),"Construction order rejected");
        bool builder=false,yield=false;
        for(int i=0;i<5000;i++)
        {
            StepSafe(sim);foreach(var shipment in planet.Shipments)
            {
                builder|=GameSimulation.IsConstructionVehicle(shipment);yield|=shipment.Traffic!=null&&shipment.Traffic.Yielding;
                Check(shipment.Traffic==null||!shipment.Traffic.Yielding||shipment.Traffic.YieldForId==shipment.Id,"Junction traffic left its lane to give way to another courier");
            }
            if(i%400==0)SaveCodec.Validate(sim.State);
            if(planet.Buildings.Count==4&&planet.Buildings.TrueForAll(b=>b.Built)&&planet.Shipments.Count==0)break;
        }
        Check(builder&&planet.Buildings.Count==4&&planet.Buildings.TrueForAll(b=>b.Built),"Road traffic prevented paid construction completion");
        Check(planet.ConsumedTotals[2]==24&&planet.ConsumedTotals[3]==6,"Traffic duplicated construction material");
        Check(yield||planet.Shipments.Count==0,"Traffic did not clear");
    }
    private static void GroupArrival()
    {
        var sim=Fixture();var units=new List<UnitState>();
        for(int i=0;i<4;i++){var unit=Add(sim,20,18+i*3);units.Add(unit);Move(sim,unit,27,23);}
        for(int i=0;i<1800;i++)StepSafe(sim);
        foreach(var unit in units)Check(unit.Order==UnitOrder.Hold&&HexGrid.Distance(unit.X,unit.Y,27,23)<=2.01,"Shared goal left an endless moving order");
        var positions=new List<SimPoint>();foreach(var unit in units)positions.Add(GameSimulation.GroundUnitPosition(unit));
        for(int i=0;i<100;i++)StepSafe(sim);
        for(int i=0;i<units.Count;i++)Check(Separation(positions[i],GameSimulation.GroundUnitPosition(units[i]))<.000001,"Arrived formation keeps displacing itself");
    }
    private static void CurveReroute()
    {
        var sim=GameSimulation.CreateDefault();var planet=sim.FindPlanet(1);
        sim.Enqueue(new SimCommand{Id=sim.NextCommandId(),PlanetId=1,Kind=CommandKind.PlaceRoad,X=9,Y=5});
        sim.Enqueue(new SimCommand{Id=sim.NextCommandId(),PlanetId=1,Kind=CommandKind.PlaceBuilding,Building=BuildingKind.SolarPlant,X=10,Y=5});
        ShipmentState courier=null;
        for(int i=0;i<1000&&courier==null;i++){StepSafe(sim);courier=planet.Shipments.Find(s=>s.Phase==ShipmentPhase.InTransit&&s.DistanceTravelled>.6&&s.DistanceTravelled<1);}
        Check(courier!=null,"No courier on a curved approach");var before=GameSimulation.GroundCourierPosition(courier);
        sim.Enqueue(new SimCommand{Id=sim.NextCommandId(),PlanetId=1,Kind=CommandKind.RemoveRoad,X=10,Y=6});StepSafe(sim);
        Check(Separation(before,GameSimulation.GroundCourierPosition(courier))<=Definitions.CourierSpeed*Definitions.TickSeconds+.000001,"Changing the next curve teleported the real courier");
        var restored=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(sim.State)));
        for(int i=0;i<600;i++){StepSafe(sim);StepSafe(restored);}
        Check(planet.Buildings.Exists(b=>b.Kind==BuildingKind.SolarPlant&&b.Built),"Rerouted freight never completed its paid construction");
        Check(SaveCodec.ToJson(sim.State)==SaveCodec.ToJson(restored.State),"Saved curve rejoin diverged");
    }
    private static void Recruitment()
    {
        var sim=Fixture();var planet=sim.FindPlanet(1);var definition=Definitions.Get(UnitKind.Infantry);
        // Explicit paid-production fixture; only the real final exit check is exercised here.
        var building=new BuildingState{Id=sim.State.NextEntityId++,PlanetId=1,Kind=BuildingKind.Barracks,X=22,Y=22,Built=true,Operational=true,RecruitKind=(int)UnitKind.Infantry,RecruitPaid=true,RecruitProgress=definition.Seconds};planet.Buildings.Add(building);
        foreach(var cell in HexGrid.LandingCells(22,22))if(cell.X!=22||cell.Y!=22)Add(sim,cell.X,cell.Y,1,UnitKind.Infantry);
        var tick=typeof(GameSimulation).GetMethod("TickRecruitment",BindingFlags.NonPublic|BindingFlags.Instance);int count=planet.Units.Count;long id=sim.State.NextEntityId;
        tick.Invoke(sim,new object[]{planet,building});Check(planet.Units.Count==count&&building.RecruitPaid&&sim.State.NextEntityId==id,"Blocked production lost payment or created a duplicate");
        var departing=planet.Units[0];Move(sim,departing,27,22);for(int i=0;i<200;i++)StepSafe(sim);
        building.Operational=true;tick.Invoke(sim,new object[]{planet,building});Check(planet.Units.Count==count+1&&building.RecruitKind==-1,"Freed production exit did not release exactly one unit");
    }
    private static void Unloading()
    {
        var sim=Fixture();var planet=sim.FindPlanet(1);
        var troop=Add(sim,20,20,1,UnitKind.Infantry);planet.Units.Remove(troop);troop.X=troop.Y=troop.GoalX=troop.GoalY=0;
        var ship=new ShipState{Id=sim.State.NextEntityId++,OwnerId=1,Kind=ShipKind.LandingTransport,TargetPlanetId=1,TransportPhase=TransportPhase.Landed,LandingX=22,LandingY=22,Health=Definitions.SpaceWeapons[(int)ShipKind.LandingTransport].Health,Crew=Definitions.Ships[(int)ShipKind.LandingTransport].Crew};ship.Troops.Add(troop);sim.State.Ships.Add(ship);
        foreach(var cell in HexGrid.LandingCells(22,22))if(cell.X!=22||cell.Y!=22)Add(sim,cell.X,cell.Y,1,UnitKind.Infantry);
        Func<bool> unload=()=>sim.Enqueue(new SimCommand{Id=sim.NextCommandId(),IssuerId=1,PlanetId=1,ActorId=ship.Id,TargetId=troop.Id,Kind=CommandKind.UnloadTroop});
        Check(unload(),"Unload intent rejected");StepSafe(sim);Check(ship.Troops.Count==1&&!planet.Units.Contains(troop),"Blocked unloading duplicated or discarded the troop");
        Move(sim,planet.Units[0],27,22);for(int i=0;i<200;i++)StepSafe(sim);
        Check(unload(),"Second unload intent rejected");StepSafe(sim);Check(ship.Troops.Count==0&&planet.Units.Contains(troop),"Freed landing exit did not transfer the original troop");
    }
}
