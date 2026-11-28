using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using PlanetOne.Core;

internal static class PlanetEnvironmentTests
{
    public static void Register(Action<string,Action> run)
    {
        run("Planet environments have plausible stellar flux composition and distinct larger maps",Profiles);
        run("Gas colony transfers one real ship manifest without creating roads or deposits",GasFounding);
        run("Air freight pays construction over physical corridors and survives save load",AirFreight);
        run("M11 atmospheric hydrogen is extracted once, flown to storage and port, and resumes after loading",HydrogenDeliveries);
        run("M11 full storage and an exhausted gas band stop extraction without losing a batch",HydrogenLimits);
        run("Cancelled airborne cargo physically returns once without roads",AirRecovery);
        run("Ocean water blocks construction and ground movement while islands remain usable",Water);
        run("Gas rules reject roads ground mines and invalid saved infrastructure",InvalidGas);
        run("Version nine worlds preserve all existing infrastructure goods and orbit positions",Migration);
        run("Planet showcase is a labelled finite-stock fixture with paid buildings",Showcase);
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static bool Cmd(GameSimulation s,CommandKind kind,int world=7,int x=20,int y=16,BuildingKind building=BuildingKind.Warehouse,long actor=0,long target=0)
    {s.Enqueue(new SimCommand {Id=s.NextCommandId(),PlanetId=world,Kind=kind,X=x,Y=y,Building=building,ActorId=actor,TargetId=target});s.Step();return s.State.CommandResults[s.State.CommandResults.Count-1].Success;}
    private static GameSimulation Founded(int world=7)
    {
        var s=GameSimulation.CreateDefault();var at=s.GetOrbitPosition(world);var def=Definitions.Ships[(int)ShipKind.ColonyShip];
        var ship=new ShipState {Id=s.State.NextEntityId++,OwnerId=1,Kind=ShipKind.ColonyShip,X=at.X,Y=at.Y,Crew=def.Crew,Cargo=(int[])def.ColonyCargo.Clone()};
        s.State.Ships.Add(ship);Check(Cmd(s,CommandKind.Colonize,world,actor:ship.Id),"Founding rejected");return s;
    }
    private static int Goods(PlanetState p,int r)
    {return p.Buildings.Sum(b=>b.Inventory[r])+p.Shipments.Where(s=>(int)s.Resource==r).Sum(s=>s.Amount);}
    private static void Profiles()
    {
        var s=GameSimulation.CreateDefault();Check(s.State.Planets.Select(p=>p.Kind).Distinct().Count()==6,"Missing environment");
        foreach(var p in s.State.Planets)Check(p.Width*p.Height>18*14,"Map not enlarged");
        foreach(int id in new[]{1,6}) {
            var p=s.FindPlanet(id);double t=PlanetEnvironment.EquilibriumKelvin(s.State,p)+PlanetEnvironment.GreenhouseKelvin(p.Kind);
            Check(t>273 && t<310,"Temperate/ocean assignment implausible");
        }
        Check(PlanetEnvironment.EquilibriumKelvin(s.State,s.FindPlanet(2))<230,"Ice world too warm");
        Check(s.FindPlanet(5).OrbitRadius<s.FindPlanet(1).OrbitRadius && s.FindPlanet(7).OrbitRadius>s.FindPlanet(2).OrbitRadius,"Orbital ordering");
        Check(PlanetEnvironment.RadiusEarths(PlanetKind.GasGiant)>7*PlanetEnvironment.RadiusEarths(PlanetKind.Temperate),"Gas radius");
        Check(s.FindPlanet(6).Terrain.Count(t=>t==4)>s.FindPlanet(6).Terrain.Length/2,"Ocean is mostly land");
    }
    private static void GasFounding()
    {
        var s=Founded();var p=s.FindPlanet(7);var ship=s.State.Ships[0];
        Check(p.OwnerId==1 && p.Population==Definitions.Ships[(int)ShipKind.ColonyShip].Crew,"Crew transfer");
        Check(ship.Consumed && ship.Cargo.Sum()==0 && ship.Crew==0,"Ship duplicated");
        Check(p.Buildings.Count==1 && p.Buildings[0].Inventory[2]==80,"Real cargo lost");
        Check(p.Roads.All(r=>!r) && p.Terrain.All(t=>t==0) && p.OreRemaining.Sum()==0,"Gas ground invented");
        Check(!Cmd(s,CommandKind.Colonize,actor:ship.Id),"Second colony from consumed ship");SaveCodec.Validate(s.State);
    }
    private static void AirFreight()
    {
        var s=Founded();var p=s.FindPlanet(7);int metal=Goods(p,2),parts=Goods(p,3);
        Check(Cmd(s,CommandKind.PlaceBuilding),"Unconnected air site rejected");s.Step(12);
        Check(p.Shipments.Any(v=>v.Phase==ShipmentPhase.InTransit && v.Amount>0) && !p.Buildings[1].Built,"Delivery teleported");
        Check(p.Shipments.Count<=Definitions.CouriersPerColony && p.Shipments.All(v=>v.Amount<=Definitions.CourierCapacity),"Capacity exceeded");
        var loaded=p.Shipments.First(v=>v.Phase==ShipmentPhase.InTransit);var before=s.GetShipmentPosition(loaded);var after=before;int departureTicks=0;
        // Several paid loads share the dock; a body must wait for a physically free exit.
        while(after.X==before.X&&after.Y==before.Y&&departureTicks++<200){s.Step();after=s.GetShipmentPosition(loaded);}
        Check(after.X!=before.X||after.Y!=before.Y,"Air cargo never leaves its shared dock");
        var copy=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(s.State)));s.Step(1800);copy.Step(1800);
        Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(copy.State),"Air continuation changed");
        Check(p.Buildings[1].Built && p.Roads.All(r=>!r),"Air site not completed");var cost=Definitions.Get(BuildingKind.Warehouse).Cost;
        Check(Goods(p,2)==metal-cost[2] && Goods(p,3)==parts-cost[3],"Construction duplicated/lost goods");
    }
    private static void HydrogenDeliveries()
    {
        var s=Founded();var p=s.FindPlanet(7);int h=(int)ResourceKind.Hydrogen;
        int band=p.AtmosphericHydrogen;
        Check(s.ValidatePlacement(1,BuildingKind.GasExtractor,10,5)!=null,"Extractor accepted on solid ground");
        Check(Cmd(s,CommandKind.PlaceBuilding,x:10,y:5,building:BuildingKind.SolarPlant),"Solar construction rejected");
        Check(Cmd(s,CommandKind.PlaceBuilding,x:10,y:7,building:BuildingKind.GasExtractor),"Extractor rejected");
        Check(Cmd(s,CommandKind.PlaceBuilding,x:7,y:8,building:BuildingKind.Spaceport),"Port rejected");
        Check(Cmd(s,CommandKind.PlaceBuilding,x:6,y:5,building:BuildingKind.Warehouse),"Warehouse rejected");
        var extractor=p.Buildings.Single(b=>b.Kind==BuildingKind.GasExtractor);
        var port=p.Buildings.Single(b=>b.Kind==BuildingKind.Spaceport);
        var warehouse=p.Buildings.Single(b=>b.Kind==BuildingKind.Warehouse);
        ShipmentState flight=null;
        for(int i=0;i<5000 && flight==null;i++){
            s.Step();flight=p.Shipments.Find(d=>d.Resource==ResourceKind.Hydrogen && d.Airborne && d.Phase==ShipmentPhase.InTransit && d.Amount>0);
        }
        Check(flight!=null && p.ProducedTotals[h]>0,"No loaded hydrogen drone");
        var copy=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(s.State)));
        for(int i=0;i<14000 && (warehouse.Inventory[h]==0 || port.Inventory[h]<8);i++){s.Step();copy.Step();}
        Check(warehouse.Inventory[h]>0 && port.Inventory[h]==8 && p.Trails.Count==0,"Paid output failed to reach warehouse and port by air");
        Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(copy.State),"Mid-flight save changed delivery or cargo");
        Check(p.ProducedTotals[h]==Goods(p,h) && band-p.AtmosphericHydrogen==p.ProducedTotals[h]+extractor.ProductionReservedSpace,"Hydrogen ledger disagrees with band depletion");
        var destination=InvasionFixture.AddBuilding(s,1,BuildingKind.Spaceport,10,5);
        var ship=InvasionFixture.AddShip(s,7,1,ShipKind.Freighter);
        port.Supply.ExportReserve[h]=3;
        s.Enqueue(new SimCommand{Id=s.NextCommandId(),PlanetId=1,ActorId=ship.Id,TargetId=port.Id,Kind=CommandKind.AssignFreight,IntValue=h,Amount=8});
        s.Step();
        Check(ship.Freight!=null && ship.Freight.Amount==5,"Space freight ignored the export reserve");
        for(int i=0;i<4000 && ship.Freight!=null;i++)s.Step();
        Check(ship.Freight==null && destination.Inventory[h]==5 && ship.Cargo[h]==0 && destination.ReservedIncoming[h]==0,"Export did not arrive exactly once");
        SaveCodec.Validate(s.State);
    }
    private static void HydrogenLimits()
    {
        var s=Founded();var p=s.FindPlanet(7);int h=(int)ResourceKind.Hydrogen;
        Check(Cmd(s,CommandKind.PlaceBuilding,x:10,y:5,building:BuildingKind.GasExtractor),"Extractor rejected");
        var b=p.Buildings.Single(v=>v.Kind==BuildingKind.GasExtractor);
        // Fill the existing storage quota so the extractor must retain its full output.
        p.Buildings[0].Inventory[h]=16;
        s.Step(5000);
        Check(b.Inventory[h]==GameSimulation.StorageCapacity(b) && !b.CycleActive,"Extractor failed to stop at capacity");
        int remaining=p.AtmosphericHydrogen,produced=p.ProducedTotals[h];s.Step(400);
        Check(p.AtmosphericHydrogen==remaining && p.ProducedTotals[h]==produced,"Full store still drained atmosphere");
        b.Inventory[h]-=2;p.AtmosphericHydrogen=2;s.Step(700);
        Check(p.AtmosphericHydrogen==0 && p.ProducedTotals[h]==produced+2,"Last batch was not produced exactly once");
        b.Inventory[h]-=2;s.Step(400);
        Check(!b.CycleActive && b.Status.Contains("erschöpft") && p.ProducedTotals[h]==produced+2,"Empty band restarted production");
        SaveCodec.Validate(s.State);
    }
    private static void AirRecovery()
    {
        var s=Founded();var p=s.FindPlanet(7);int metal=Goods(p,2),parts=Goods(p,3);
        Cmd(s,CommandKind.PlaceBuilding);s.Step(18);long id=p.Buildings[1].Id;
        Check(p.Shipments.Exists(v=>v.Amount>0 && v.Phase==ShipmentPhase.InTransit),"No airborne cargo");
        Check(Cmd(s,CommandKind.CancelConstruction,target:id),"Cancellation rejected");
        Check(p.Shipments.Exists(v=>v.Amount>0),"Cargo remotely refunded");
        var copy=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(s.State)));s.Step(800);copy.Step(800);
        Check(SaveCodec.ToJson(copy.State)==SaveCodec.ToJson(s.State),"Recovery changed after reload");
        Check(p.Buildings.Count==1 && Goods(p,2)==metal && Goods(p,3)==parts,"Recovery duplicated or lost cargo");
    }
    private static void Water()
    {
        var s=Founded(6);var p=s.FindPlanet(6);int i=Array.FindIndex(p.Terrain,t=>t==4);
        Check(!Cmd(s,CommandKind.PlaceRoad,6,i%p.Width,i/p.Width),"Road on water");
        Check(s.ValidatePlacement(6,BuildingKind.Warehouse,i%p.Width,i/p.Width)!=null,"Building on water");
        foreach(UnitKind kind in Enum.GetValues(typeof(UnitKind)))Check(!s.CanTraverse(p,1,kind,i%p.Width,i/p.Width),"Ground unit crosses water");
        Check(s.ValidatePlacement(6,BuildingKind.SolarPlant,10,5)==null,"Starting island cannot expand");
    }
    private static void InvalidGas()
    {
        var s=Founded();var p=s.FindPlanet(7);
        Check(!Cmd(s,CommandKind.PlaceRoad,x:9,y:6) && !Cmd(s,CommandKind.RemoveRoad,x:9,y:6),"Road command accepted");
        foreach(var kind in new[]{BuildingKind.Mine,BuildingKind.CrystalMine,BuildingKind.Wall,BuildingKind.Gate})Check(s.ValidatePlacement(7,kind,10,5)!=null,"Ground structure accepted");
        p.Roads[1]=true;bool rejected=false;try{SaveCodec.ToJson(s.State);}catch(InvalidDataException){rejected=true;}Check(rejected,"Invalid gas save accepted");
        p.Roads[1]=false;p.Kind=(PlanetKind)99;rejected=false;try{SaveCodec.ToJson(s.State);}catch(InvalidDataException){rejected=true;}Check(rejected,"Unknown kind accepted");
    }
    private static string Raw(GameState state){using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(GameState)).WriteObject(stream,state);return Encoding.UTF8.GetString(stream.ToArray());}}
    private static void Migration()
    {
        string json=File.ReadAllText("Tests/Core/Fixtures/prototype-v9.json");GameState old;
        using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))old=(GameState)new DataContractJsonSerializer(typeof(GameState)).ReadObject(stream);
        Check(old.Version==9,"Not a genuine V9 fixture");var migrated=SaveCodec.FromJson(json);
        Check(migrated.Version==SaveCodec.CurrentVersion && migrated.Planets[0].Kind==PlanetKind.Temperate,"No migration");
        migrated.Asteroids=null;migrated.AsteroidBelts=null;migrated.Version=9;for(int i=0;i<old.Planets.Count;i++)migrated.Planets[i].Kind=old.Planets[i].Kind;
        foreach(var planet in migrated.Planets){Check(planet.Trails.Count==0,"Migration invented trail wear");planet.Trails=null;}
        Check(WithEmptyHydrogenSlots(Raw(old))==Raw(migrated),"Legacy world resized or existing state changed");
    }
    internal static string WithEmptyHydrogenSlots(string legacyJson)
    {
        // Compare the entire old snapshot, allowing exactly the new field in each six-good vector.
        return System.Text.RegularExpressions.Regex.Replace(legacyJson,
            "\"(Inventory|ReservedOutgoing|ReservedIncoming|ProducedTotals|ConsumedTotals|RecoveredTotals|LostTotals|SpaceLostTotals|Cargo|Minimum|Target|ExportReserve|Priority|Replenishing)\":\\[([^\\]]*)\\]",
            m=>m.Value.Substring(0,m.Value.Length-1)+","+(m.Groups[1].Value=="Priority"?"1":m.Groups[1].Value=="Replenishing"?"false":"0")+"]");
    }
    private static void Showcase()
    {
        var s=PlanetEnvironmentFixture.Create();Check(s.State.SpaceTestFixture && s.State.LastMessage.Contains("Prüfkarte"),"Unlabelled debug state");
        Check(s.State.Planets.All(p=>p.Buildings.Count(b=>b.Built)>=3),"Fixture buildings were not actually built");
        Check(s.FindPlanet(7).Roads.All(r=>!r),"Fixture gas roads");SaveCodec.Validate(s.State);
    }
}
