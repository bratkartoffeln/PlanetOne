using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using PlanetOne.Core;

internal static class AsteroidTests
{
    internal static void Register(Action<string,Action> run)
    {
        run("Asteroid belts are seeded optional sparse and clear of all planetary orbits",Generation);
        run("Mining requires scouting own free freighters technology and a real return port",Permissions);
        run("Concurrent asteroid claims reserve only finite ore and actual port space",Reservations);
        run("Mining physically extracts loads returns and unloads without duplicating ore",Delivery);
        run("Mining cancellation and ship loss preserve partial cargo and release claims",Cancellation);
        run("Captured return ports never receive mined cargo from the former owner",PortLoss);
        run("Mining save continuation preserves extraction travel and unloading exactly",Continuation);
        run("Asteroid saves reject forged reserves totals cargo and missing references",InvalidSaves);
        run("Coincident legacy couriers retain their cargo and current asteroid saves preserve all values",LegacyOverlap);
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string fixture;
    private static GameSimulation Fixture()
    {
        if(fixture==null){var game=SpaceFixture.Create(2);foreach(var p in game.State.Planets)foreach(var b in p.Buildings)if(b.Kind==BuildingKind.Mine || b.Kind==BuildingKind.Smelter)b.Enabled=false;game.State.Paused=false;fixture=SaveCodec.ToJson(game.State);}
        return new GameSimulation(SaveCodec.FromJson(fixture));
    }
    private static ShipState Freighter(GameSimulation s){return s.State.Ships.First(v=>v.Kind==ShipKind.Freighter);}
    private static BuildingState Port(GameSimulation s){return s.FindPlanet(1).Buildings.First(b=>b.Kind==BuildingKind.Spaceport);}
    private static bool Cmd(GameSimulation s,CommandKind kind=CommandKind.MineAsteroid,int amount=40,ShipState ship=null,int faction=1,long destination=0)
    {
        s.Enqueue(new SimCommand {Id=s.NextCommandId(),Kind=kind,ActorId=(ship??Freighter(s)).Id,TargetId=s.State.Asteroids[0].Id,PlanetId=1,Amount=amount,IssuerId=faction,DestinationId=destination});
        s.Step();return s.State.CommandResults.Last().Success;
    }
    private static void Scout(GameSimulation s,ShipState ship=null)
    {var p=s.GetAsteroidPosition(s.State.Asteroids[0]);var v=ship??Freighter(s);v.X=p.X;v.Y=p.Y;v.Order=ShipOrder.Hold;}
    private static void Wait(GameSimulation s,Func<bool> done,int ticks=4000)
    {for(int i=0;i<ticks && !done();i++)s.Step();Check(done(),"Timeout: "+Freighter(s).Status);}
    private static int Ore(GameSimulation s)
    {return s.State.Ships.Sum(v=>v.Cargo[1])+s.State.SpaceLostTotals[1]+s.State.Planets.Sum((PlanetState p)=>p.Buildings.Sum((BuildingState b)=>b.Inventory[1])+p.Shipments.Where(v=>v.Resource==ResourceKind.Ore && v.Phase!=ShipmentPhase.Loading).Sum(v=>v.Amount)+p.ConsumedTotals[1]+p.LostTotals[1]-p.ProducedTotals[1]);}
    private static void Generation()
    {
        int present=0,absent=0;
        for(int seed=0;seed<50;seed++)
        {
            var a=GameSimulation.CreateDiplomacyScenario(seed);var b=GameSimulation.CreateDiplomacyScenario(seed);
            Check(SaveCodec.ToJson(a.State)==SaveCodec.ToJson(b.State),"Seed differs");
            if(a.State.Asteroids.Count==0){absent++;continue;}present++;
            Check(a.State.Asteroids.Count==32 && a.State.AsteroidBelts.Count==1,"Unbounded or empty belt");
            var belt=a.State.AsteroidBelts[0];
            foreach(var p in a.State.Planets.Where(p=>p.ParentId<0))Check(belt.OuterRadius<p.OrbitRadius*(1-p.OrbitEccentricity) || belt.InnerRadius>p.OrbitRadius*(1+p.OrbitEccentricity),"Belt intersects "+p.Name);
            foreach(var asteroid in a.State.Asteroids)Check(asteroid.DiameterKm<10 && asteroid.RemainingOre>0,"Oversized or empty asteroid");
        }
        Check(present>10 && absent>5,"Every system has the same belt presence");
    }
    private static void Permissions()
    {
        var s=Fixture();Check(!Cmd(s),"Unscouted stock exposed");Scout(s);
        Check(!Cmd(s,faction:2),"Foreign ship controlled");
        Check(!Cmd(s,amount:41) && !Cmd(s,amount:0),"Invalid manifest accepted");
        var colony=s.State.Ships.First(v=>v.Kind==ShipKind.ColonyShip);Scout(s,colony);Check(!Cmd(s,ship:colony),"Colony ship can mine");
        Check(Cmd(s),"Valid request rejected");Check(!Cmd(s),"Busy ship assigned twice");
        Check(!Cmd(s,CommandKind.FlyShip) && Freighter(s).Mining!=null,"Flight overwrote active mining");
        var t=Fixture();Scout(t);t.State.Factions[0].Research.Completed.Clear();Check(!Cmd(t),"Missing technology ignored");
    }
    private static void Reservations()
    {
        var s=Fixture();var a=s.State.Asteroids[0];a.InitialOre=a.RemainingOre=53;Scout(s);Check(Cmd(s),"First claim");
        var second=new ShipState {Id=s.State.NextEntityId++,Kind=ShipKind.Freighter,OwnerId=1,Crew=2};s.State.Ships.Add(second);Scout(s,second);
        Check(Cmd(s,ship:second) && second.Mining.Amount==13,"Overbooked finite deposit");Check(a.ReservedOre==53 && Port(s).ReservedIncoming[1]==53,"Reservations differ");SaveCodec.Validate(s.State);
        Cmd(s,CommandKind.CancelFreight);Check(a.ReservedOre==13,"Cancel released another ship claim");
        var full=Fixture();Scout(full);var port=Port(full);for(int r=0;r<6;r++)port.Inventory[r]=0;port.Inventory[1]=Definitions.Get(BuildingKind.Spaceport).Capacity;
        Check(!Cmd(full),"Full port overbooked");
        var other=new BuildingState {Id=full.State.NextEntityId++,Kind=BuildingKind.Spaceport,PlanetId=1,X=16,Y=6,Built=true,Health=Definitions.BuildingHealth(BuildingKind.Spaceport)};
        full.FindPlanet(1).Buildings.Add(other);
        Check(Cmd(full,destination:other.Id) && Freighter(full).Mining.PortId==other.Id && other.ReservedIncoming[1]==40,"Explicit second return port ignored");
    }
    private static void Delivery()
    {
        var s=Fixture();Scout(s);var ship=Freighter(s);var a=s.State.Asteroids[0];int before=Ore(s),reserve=a.RemainingOre;
        Check(Cmd(s,amount:7),"Mining rejected");Wait(s,()=>ship.Cargo[1]>0);
        Check(ship.Cargo[1]<7 && Port(s).ReservedIncoming[1]==7 && a.RemainingOre==reserve-ship.Cargo[1],"Cargo teleported");
        Wait(s,()=>ship.Freight!=null);Check(ship.Cargo[1]==7 && ship.Freight.Phase==FreightPhase.Deliver,"No physical return flight");
        Wait(s,()=>ship.Freight==null);Check(ship.Cargo[1]==0 && s.State.SpaceMinedOre==7 && Ore(s)==before+7,"Ore balance: before="+before+", after="+Ore(s)+", mined="+s.State.SpaceMinedOre+", cargo="+ship.Cargo[1]);
        Check(a.RemainingOre==reserve-7 && a.ReservedOre==0 && Port(s).ReservedIncoming[1]==0,"Final claims remain");SaveCodec.Validate(s.State);
        a.InitialOre-=a.RemainingOre;a.RemainingOre=0;Scout(s);Check(!Cmd(s),"Depleted asteroid mined again");SaveCodec.Validate(s.State);
    }
    private static void Cancellation()
    {
        foreach(bool destroy in new[]{false,true})
        {
            var s=Fixture();Scout(s);var ship=Freighter(s);var a=s.State.Asteroids[0];Cmd(s);Wait(s,()=>ship.Cargo[1]==3);int remaining=a.RemainingOre;
            if(destroy)s.DestroyShip(ship.Id);else Cmd(s,CommandKind.CancelFreight);
            Check(a.RemainingOre==remaining && a.ReservedOre==0 && ship.Mining==null,"Cancellation invents/reclaims mined ore");
            Check(destroy?s.State.SpaceLostTotals[1]==3:ship.Cargo[1]==3,"Partial cargo lost or copied");
            if(!destroy)Wait(s,()=>ship.Cargo[1]==0);SaveCodec.Validate(s.State);
        }
    }
    private static void PortLoss()
    {
        var s=Fixture();Scout(s);Cmd(s);var ship=Freighter(s);Wait(s,()=>ship.Cargo[1]==2);
        var port=Port(s);int stock=port.Inventory[1];s.FindPlanet(1).OwnerId=2;s.Step();
        Check(ship.Mining==null && ship.Cargo[1]==2 && port.Inventory[1]==stock && port.ReservedIncoming[1]==0 && s.State.Asteroids[0].ReservedOre==0,"Captured port received cargo");SaveCodec.Validate(s.State);
    }
    private static void Continuation()
    {
        var a=Fixture();Scout(a);Cmd(a,amount:9);var ship=Freighter(a);Wait(a,()=>ship.Cargo[1]==3);
        for(int phase=0;phase<3;phase++)
        {
            var b=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(a.State)));a.Step(50);b.Step(50);
            Check(SaveCodec.ToJson(a.State)==SaveCodec.ToJson(b.State),"Continuation differs at phase "+phase);
            if(phase==0)Wait(a,()=>ship.Freight!=null);
            if(phase==1)Wait(a,()=>ship.Freight!=null && ship.Freight.Phase==FreightPhase.Unloading);
        }
    }
    private static void InvalidSaves()
    {
        for(int kind=0;kind<6;kind++)
        {
            var s=Fixture();Scout(s);Cmd(s);var a=s.State.Asteroids[0];var ship=Freighter(s);
            if(kind==0)a.ReservedOre++;if(kind==1)s.State.SpaceMinedOre++;if(kind==2)ship.Cargo[1]++;if(kind==3)ship.Mining.AsteroidId=long.MaxValue;if(kind==4)ship.Mining.PortId=long.MaxValue;if(kind==5)ship.Mining.Progress=double.NaN;
            bool rejected=false;try{SaveCodec.ToJson(s.State);}catch(InvalidDataException){rejected=true;}Check(rejected,"Forged mining state accepted "+kind);
        }
    }
    private static void LegacyOverlap()
    {
        const string path="Tests/Core/Fixtures/prototype-v10.json";
        byte[] original=File.ReadAllBytes(path);GameState old;
        using(var stream=new MemoryStream(original))old=(GameState)new DataContractJsonSerializer(typeof(GameState)).ReadObject(stream);
        Check(old.Version==10,"Not the original version ten fixture");bool overlap=false;
        foreach(var planet in old.Planets)for(int i=0;i<planet.Shipments.Count;i++)for(int j=i+1;j<planet.Shipments.Count;j++)
        {
            var a=planet.Shipments[i];var b=planet.Shipments[j];if(GameSimulation.GroundCourierDocked(a)||GameSimulation.GroundCourierDocked(b))continue;
            var pa=GameSimulation.GroundCourierPosition(a);var pb=GameSimulation.GroundCourierPosition(b);
            double x=pa.X-pb.X,y=pa.Y-pb.Y,radius=GameSimulation.GroundCourierRadius(a)+GameSimulation.GroundCourierRadius(b)-GameSimulation.GroundTrafficTolerance;
            if(x*x+y*y<radius*radius)overlap=true;
        }
        Check(overlap,"Legacy fixture no longer has coincident physical couriers");
        var migrated=SaveCodec.FromJson(Encoding.UTF8.GetString(original));
        foreach(var planet in old.Planets)
        {
            var loaded=migrated.Planets.Find(p=>p.Id==planet.Id);
            Check(loaded.Shipments.Count==planet.Shipments.Count,"Migration changed courier count");
            foreach(var shipment in planet.Shipments)
            {
                var courier=loaded.Shipments.Find(s=>s.Id==shipment.Id);
                Check(courier!=null && courier.Amount==shipment.Amount && courier.Resource==shipment.Resource &&
                    courier.TargetId==shipment.TargetId && courier.DistanceTravelled==shipment.DistanceTravelled,"Migration changed an active delivery");
            }
        }
        Check(original.SequenceEqual(File.ReadAllBytes(path)),"Legacy fixture was modified");
        // The current-format preservation requirement remains exact; old files are never repaired in place.
        var current=GameSimulation.CreateDiplomacyScenario(2);string snapshot=SaveCodec.ToJson(current.State);
        var restored=SaveCodec.FromJson(snapshot);
        Check(SaveCodec.ToJson(restored)==snapshot,"Current snapshot changed owners, inventories, asteroid amounts or entity identities");
    }
}
