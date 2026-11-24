using System;
using System.IO;
using PlanetOne.Core;

internal static class SpaceTests
{
    internal static void Register(Action<string, Action> run)
    {
        run("M4 ship construction uses couriers time crew and technology", Construction);
        run("M4 moving moon guard escort and faction knowledge remain physical", Navigation);
        run("M4 free flight discovers previously unknown worlds through sensors", Exploration);
        run("M4 colony consumes exactly one ship with its actual manifest", Colonization);
        run("M4 concurrent freight reserves partial amounts and protects exports", Reservations);
        run("M4 loading unloading berth and power constrain physical transfers", Transfers);
        run("M4 cancel before and after loading preserves actual cargo", Cancellation);
        run("M4 lost ships and captured ports never duplicate or deliver to enemies", Loss);
        run("M4 automatic demand counts promised cargo and fixed routes repeat", Automatic);
        run("M4 research prerequisites materials laboratory loss and unlocks", Research);
        run("M4 technology graph rejects missing references and cycles", Graph);
        run("M4 space saves migrate and preserve exact freight continuation", Persistence);
        run("M4 paid ship cancellation recovers crew and materials only once", CancelConstruction);
        run("M4 full destinations release capacity after real space becomes available", FullDestination);
        run("M4 regular economy finances colonization crystals electronics and sensors", FullEconomy);
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Wait(GameSimulation s, Func<bool> done, int ticks = 24000)
    { for (int i = 0; i < ticks && !done(); i++) s.Step(); if (!done()) { foreach(var p in s.State.Planets) foreach(var b in p.Buildings) Console.WriteLine(p.Name+" "+b.Kind+" "+b.Status+" inv="+string.Join(",",b.Inventory)+" in="+string.Join(",",b.ReservedIncoming)+" out="+string.Join(",",b.ReservedOutgoing)); Console.WriteLine("research="+s.State.Factions[0].Research.Status); } Check(done(), "Timeout: " + s.State.LastMessage); }
    private static bool Cmd(GameSimulation s, CommandKind kind, long actor = 0, long target = 0, int planet = 1, int value = 0, int amount = 0, int min = 0, int reserve = 0, int x = 0, bool flag = false)
    { s.Enqueue(new SimCommand { Id = s.NextCommandId(), Kind = kind, ActorId = actor, TargetId = target, PlanetId = planet, IntValue = value, Amount = amount, Minimum = min, Reserve = reserve, X = x, BoolValue = flag }); s.Step(); return s.State.CommandResults[s.State.CommandResults.Count - 1].Success; }
    private static BuildingState Add(GameSimulation s, int planet, BuildingKind kind, int x, int y)
    { var b = new BuildingState { Id = s.State.NextEntityId++, PlanetId = planet, Kind = kind, X = x, Y = y, Built = true, Health = Definitions.BuildingHealth(kind) }; s.FindPlanet(planet).Buildings.Add(b); return b; }
    private static GameSimulation Fixture(out BuildingState a, out BuildingState b)
    {
        var s = GameSimulation.CreateDefault(); s.State.SpaceTestFixture = true;
        a = Add(s, 1, BuildingKind.Spaceport, 9, 5); b = Add(s, 2, BuildingKind.Spaceport, 9, 5);
        a.Inventory[2] = 100; a.Supply.ExportReserve[2] = 20; s.Step(); return s;
    }
    private static ShipState Ship(GameSimulation s, ShipKind kind = ShipKind.Freighter)
    { var pt = s.GetOrbitPosition(1); var d = Definitions.Ships[(int)kind]; var ship = new ShipState { Id = s.State.NextEntityId++, OwnerId = 1, Kind = kind, X = pt.X, Y = pt.Y, Crew = d.Crew, Cargo = (int[])d.ColonyCargo.Clone() }; s.State.Ships.Add(ship); return ship; }
    private static void Construction()
    {
        var s = GameSimulation.CreateDefault(); s.State.Factions[0].Research.Completed.Add(Technology.Transport);
        s.Enqueue(new SimCommand { Id = s.NextCommandId(), PlanetId = 1, Kind = CommandKind.PlaceBuilding, Building = BuildingKind.Spaceport, X = 9, Y = 5 });
        Wait(s, () => s.FindPlanet(1).Buildings.Exists(b => b.Kind == BuildingKind.Spaceport && b.Built));
        var port = s.FindPlanet(1).Buildings.Find(b => b.Kind == BuildingKind.Spaceport);
        Check(!Cmd(s, CommandKind.BuildShip, target: port.Id, value: 1), "Locked colony ship allowed");
        int before = s.FindPlanet(1).Population;
        Check(Cmd(s, CommandKind.BuildShip, target: port.Id), "Freighter rejected"); Check(s.State.Ships.Count == 0 && !port.ShipBuildPaid, "Build skipped material delivery");
        Wait(s, () => s.State.Ships.Count == 1); Check(s.FindPlanet(1).Population == before - 2, "Crew not paid");
        s.Step(400); Check(s.State.Ships.Count == 1, "Ship duplicated"); SaveCodec.Validate(s.State);
    }
    private static void Navigation()
    {
        var s = GameSimulation.CreateDefault(); var ship = Ship(s); var follower = Ship(s);
        Check(Cmd(s, CommandKind.FlyShip, ship.Id, planet: 3), "Known moon inaccessible");
        Cmd(s, CommandKind.EscortShip, follower.Id, ship.Id);
        double x = ship.X, y = ship.Y; s.Step(); Check(Math.Abs(ship.X-x) + Math.Abs(ship.Y-y) < 0.5, "Movement teleported");
        s.Step(1200); var moon = s.GetOrbitPosition(3); Check(Math.Abs(ship.X-moon.X) < 0.2 && Math.Abs(ship.Y-moon.Y) < 0.2, "Missed moving moon");
        Check(Math.Abs(ship.X-follower.X) < 0.4, "Escort failed");
        Check(!Cmd(s, CommandKind.EscortShip, ship.Id, follower.Id), "Escort cycle accepted");
        var enemy = Ship(s); enemy.OwnerId = 2; enemy.X = 200; enemy.Y = 200;
        Check(!s.IsShipVisible(1, enemy), "Hidden enemy revealed"); enemy.X = ship.X; enemy.Y = ship.Y; s.Step();
        var sight = s.State.SpaceSightings.Find(v => v.ShipId == enemy.Id && v.FactionId == 1); Check(sight != null, "No legitimate sight");
        double old = sight.X; enemy.X = 200; s.Step(5); Check(sight.X == old, "Stale sight tracks hidden ship"); SaveCodec.Validate(s.State);
    }
    private static void Colonization()
    {
        var s = GameSimulation.CreateDefault(); var ship = Ship(s, ShipKind.ColonyShip); var moon = s.FindPlanet(3);
        Check(!Cmd(s, CommandKind.Colonize, ship.Id, planet: 3), "Distant colony teleported");
        Cmd(s, CommandKind.FlyShip, ship.Id, planet: 3); s.Step(1200);
        Check(Cmd(s, CommandKind.Colonize, ship.Id, planet: 3), "Colonization failed");
        Check(ship.Consumed && ship.Cargo[2] == 0 && moon.Population == 12 && moon.Buildings[0].Inventory[2] == 80, "Manifest duplicated or wrong population");
        Check(!Cmd(s, CommandKind.Colonize, ship.Id, planet: 3) && moon.Buildings.Count == 1, "Repeated founding duplicated colony"); SaveCodec.Validate(s.State);
    }
    private static void Reservations()
    {
        BuildingState a,b; var s=Fixture(out a,out b); var first=Ship(s); var second=Ship(s); var third=Ship(s);
        Cmd(s,CommandKind.AssignFreight,first.Id,a.Id,2,2,40); Cmd(s,CommandKind.AssignFreight,second.Id,a.Id,2,2,40);
        Check(!Cmd(s,CommandKind.AssignFreight,third.Id,a.Id,2,2,40),"Export reserve consumed");
        Check(a.ReservedOutgoing[2]==80 && b.ReservedIncoming[2]==80 && first.Cargo[2]==0,"Reservation duplicated or preloaded"); SaveCodec.Validate(s.State);
        Cmd(s,CommandKind.CancelFreight,second.Id); a.Inventory[2]=70;
        Check(Cmd(s,CommandKind.AssignFreight,third.Id,a.Id,2,2,40) && third.Freight.Amount==10,"Partial order ignored reservation"); SaveCodec.Validate(s.State);
    }
    private static void Transfers()
    {
        BuildingState a,b; var s=Fixture(out a,out b); var ship=Ship(s); var other=Ship(s);
        Cmd(s,CommandKind.AssignFreight,ship.Id,a.Id,2,2,10); Cmd(s,CommandKind.AssignFreight,other.Id,a.Id,2,2,10); s.Step(40);
        Check(ship.Cargo[2]==0 && other.Freight.Progress==0,"Instant load or parallel berth");
        a.Enabled=false; double progress=ship.Freight.Progress; s.Step(20); Check(ship.Freight.Progress==progress,"Unpowered port loads"); a.Enabled=true;
        Wait(s,()=>ship.Freight==null && other.Freight==null); Check(b.Inventory[2]==20 && a.Inventory[2]==80,"Physical transfer totals wrong"); SaveCodec.Validate(s.State);
    }
    private static void Cancellation()
    {
        BuildingState a,b; var s=Fixture(out a,out b); var ship=Ship(s);
        Cmd(s,CommandKind.AssignFreight,ship.Id,a.Id,2,2,20); Cmd(s,CommandKind.CancelFreight,ship.Id);
        Check(a.Inventory[2]==100 && a.ReservedOutgoing[2]==0 && b.ReservedIncoming[2]==0,"Unloaded cancellation lost goods");
        Cmd(s,CommandKind.AssignFreight,ship.Id,a.Id,2,2,20); Wait(s,()=>ship.Cargo[2]==20);
        Cmd(s,CommandKind.CancelFreight,ship.Id); Check(ship.Cargo[2]==20 && a.Inventory[2]==80,"Loaded cargo remotely refunded");
        Wait(s,()=>ship.Cargo[2]==0); Check(a.Inventory[2]+b.Inventory[2]==100,"Recovery changed total"); SaveCodec.Validate(s.State);
    }
    private static void Loss()
    {
        BuildingState a,b; var s=Fixture(out a,out b); var ship=Ship(s);
        Cmd(s,CommandKind.AssignFreight,ship.Id,a.Id,2,2,20); Wait(s,()=>ship.Cargo[2]==20);
        s.DestroyShip(ship.Id); s.DestroyShip(ship.Id); Check(s.State.SpaceLostTotals[2]==20 && a.Inventory[2]==80 && b.ReservedIncoming[2]==0,"Loss refunded or doubled"); SaveCodec.Validate(s.State);
        ship=Ship(s); Cmd(s,CommandKind.AssignFreight,ship.Id,a.Id,2,2,20); Wait(s,()=>ship.Cargo[2]==20); s.FindPlanet(2).OwnerId=2; s.Step();
        Check(b.ReservedIncoming[2]==0 && b.Inventory[2]==0 && ship.Cargo[2]==20,"Captured port received cargo"); Wait(s,()=>ship.Cargo[2]==0); Check(a.Inventory[2]==80,"Return lost cargo"); SaveCodec.Validate(s.State);
    }
    private static void Automatic()
    {
        BuildingState a,b; var s=Fixture(out a,out b); var ship=Ship(s); var second=Ship(s);
        s.FindPlanet(2).Buildings[0].Inventory[2]=0;
        Cmd(s,CommandKind.SetSupplyPolicy,target:b.Id,planet:2,value:2,amount:60,min:20,reserve:0,x:1);
        // Isolated transport test: prevent local warehouses from satisfying the demand.
        s.FindPlanet(2).Buildings[0].Inventory[2]=0;
        Cmd(s,CommandKind.SetAutoSupply,ship.Id,flag:true); Cmd(s,CommandKind.SetAutoSupply,second.Id,flag:true);
        Check(b.ReservedIncoming[2]==60,"Repeated demand overordered"); Wait(s,()=>b.Inventory[2]==60); Check(a.Inventory[2]==40,"Automatic export wrong");
        Cmd(s,CommandKind.SetAutoSupply,ship.Id,flag:false); Cmd(s,CommandKind.SetAutoSupply,second.Id,flag:false);
        Cmd(s,CommandKind.SetTradeRoute,ship.Id,a.Id,2,2,8); Wait(s,()=>a.Inventory[2]==20 && ship.Freight==null);
        Check(b.Inventory[2]==80,"Fixed route failed repeat or export protection"); SaveCodec.Validate(s.State);
    }
    private static void Research()
    {
        var s=GameSimulation.CreateDefault(); var lab=Add(s,1,BuildingKind.Laboratory,9,5);
        Check(!Cmd(s,CommandKind.StartResearch,target:lab.Id,value:3),"Skipped technology prerequisites");
        Cmd(s,CommandKind.StartResearch,target:lab.Id); Wait(s,()=>s.State.Factions[0].Research.Progress>2);
        var r=s.State.Factions[0].Research; double progress=r.Progress; Cmd(s,CommandKind.DemolishBuilding,target:lab.Id); s.Step(100);
        Check(r.Progress==progress && r.Paid,"Laboratory loss reset research");
        var replacement=Add(s,1,BuildingKind.Laboratory,10,5); Cmd(s,CommandKind.StartResearch,target:replacement.Id); Wait(s,()=>s.HasTechnology(1,Technology.Transport));
        Check(s.State.Ships.Count==0 && s.BuildingTechnologyError(BuildingKind.ElectronicsFactory,1)!=null,"Research spawned assets or unlocked unrelated technology"); SaveCodec.Validate(s.State);
    }
    private static void Graph()
    {
        Definitions.ValidateTechnologies(Definitions.Technologies);
        bool rejected=false; try { Definitions.ValidateTechnologies(new[] { new TechnologyDefinition(Technology.Transport,"cycle",1,new int[6],Technology.Transport) }); } catch(ArgumentException) { rejected=true; } Check(rejected,"Cycle accepted");
        rejected=false; try { Definitions.ValidateTechnologies(new[] { new TechnologyDefinition(Technology.Transport,"missing",1,new int[6],Technology.Electronics) }); } catch(ArgumentException) { rejected=true; } Check(rejected,"Missing node accepted");
    }
    private static void Persistence()
    {
        BuildingState a,b; var s=Fixture(out a,out b); var ship=Ship(s); Cmd(s,CommandKind.AssignFreight,ship.Id,a.Id,2,2,20); Wait(s,()=>ship.Cargo[2]>0); s.Step(20);
        var restored=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(s.State))); s.Step(500); restored.Step(500); Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(restored.State),"Freight continuation differs");
        string old=SaveCodec.ToJson(GameSimulation.CreateDefault().State).Replace("\"Version\":"+SaveCodec.CurrentVersion,"\"Version\":3"); var migrated=SaveCodec.FromJson(old); Check(migrated.Version==SaveCodec.CurrentVersion && migrated.Ships.Count==0,"V3 migration failed");
        restored.State.Ships[0].Cargo[2]=200; bool rejected=false; try { SaveCodec.Validate(restored.State); } catch(InvalidDataException) { rejected=true; } Check(rejected,"Corrupt cargo accepted");
    }
    private static void FullEconomy()
    {
        var s = SpaceFixture.Create(); s.State.Paused = false;
        Check(s.State.Ships.Exists(v=>v.Kind==ShipKind.ColonyShip) && s.FindPlanet(1).ProducedTotals[2]>100 && s.FindPlanet(1).ProducedTotals[3]>20,"Economy did not finance colonization");
        var colony=s.State.Ships.Find(v=>v.Kind==ShipKind.ColonyShip); Cmd(s,CommandKind.FlyShip,colony.Id,planet:3); s.Step(1000); Check(Cmd(s,CommandKind.Colonize,colony.Id,planet:3),"Real colony failed");
        SpaceFixture.Build(s,3,BuildingKind.Farm,9,7); SpaceFixture.Build(s,3,BuildingKind.SolarPlant,10,5); SpaceFixture.Build(s,3,BuildingKind.Spaceport,11,5);
        for(int y=7;y<=8;y++) { s.Enqueue(new SimCommand {Id=s.NextCommandId(),PlanetId=3,Kind=CommandKind.PlaceRoad,X=13,Y=y}); s.Step(); }
        SpaceFixture.Build(s,3,BuildingKind.CrystalMine,13,9);
        var moon=s.FindPlanet(3); var source=moon.Buildings.Find(v=>v.Kind==BuildingKind.Spaceport); var target=s.FindPlanet(1).Buildings.Find(v=>v.Kind==BuildingKind.Spaceport);
        Cmd(s,CommandKind.SetSupplyPolicy,target:source.Id,planet:3,value:4,amount:20,min:4,reserve:0,x:1);
        Wait(s,()=>source.Inventory[4]>=12);
        var freight=s.State.Ships.Find(v=>v.Kind==ShipKind.Freighter); Check(Cmd(s,CommandKind.AssignFreight,freight.Id,source.Id,1,4,12),"Crystal shipment rejected"); Wait(s,()=>freight.Freight==null);
        var lab=s.FindPlanet(1).Buildings.Find(v=>v.Kind==BuildingKind.Laboratory); Cmd(s,CommandKind.StartResearch,target:lab.Id,value:3); Wait(s,()=>s.HasTechnology(1,Technology.Electronics));
        SpaceFixture.Build(s,1,BuildingKind.ElectronicsFactory,15,5); Wait(s,()=>s.FindPlanet(1).ProducedTotals[5]>=4);
        Cmd(s,CommandKind.StartResearch,target:lab.Id,value:4); Wait(s,()=>s.HasTechnology(1,Technology.Sensors));
        Cmd(s,CommandKind.BuildShip,target:target.Id,value:2); Wait(s,()=>s.State.Ships.Exists(v=>v.Kind==ShipKind.SurveyShip));
        SaveCodec.Validate(s.State); Console.WriteLine("  M4 economy proof: metal="+s.FindPlanet(1).ProducedTotals[2]+", parts="+s.FindPlanet(1).ProducedTotals[3]+", crystals="+moon.ProducedTotals[4]+", electronics="+s.FindPlanet(1).ProducedTotals[5]);
    }
    private static void CancelConstruction()
    {
        BuildingState a,b; var s=Fixture(out a,out b); s.State.Factions[0].Research.Completed.Add(Technology.Transport);
        a.Inventory[0]=4; a.Inventory[3]=6; int people=s.FindPlanet(1).Population;
        Cmd(s,CommandKind.BuildShip,target:a.Id); Check(a.ShipBuildPaid,"Build fixture did not pay");
        var restored=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(s.State)));
        s.Step(20); restored.Step(20); Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(restored.State),"Paid build continuation differs");
        Cmd(s,CommandKind.DemolishBuilding,target:a.Id);
        Check(s.State.Ships.Count==0 && s.FindPlanet(1).Population==people && a.Inventory[2]>=100,"Cancellation lost crew/material or created ship");
        Check(!Cmd(s,CommandKind.DemolishBuilding,target:a.Id),"Repeated cancellation accepted"); SaveCodec.Validate(s.State);
    }
    private static void FullDestination()
    {
        BuildingState a,b; var s=Fixture(out a,out b); var ship=Ship(s); b.Inventory[1]=240;
        Check(!Cmd(s,CommandKind.AssignFreight,ship.Id,a.Id,2,2,20) && a.ReservedOutgoing[2]==0,"Full port overbooked");
        b.Inventory[1]-=10;
        Check(Cmd(s,CommandKind.AssignFreight,ship.Id,a.Id,2,2,20) && ship.Freight.Amount==10,"Freight did not respect partial capacity");
        s.Enqueue(new SimCommand {Id=s.NextCommandId(),IssuerId=2,Kind=CommandKind.CancelFreight,ActorId=ship.Id,PlanetId=2}); s.Step();
        Check(ship.Freight!=null,"Foreign faction controlled the ship"); SaveCodec.Validate(s.State);
    }
    private static void Exploration()
    {
        var s=GameSimulation.CreateDefault(); s.FindPlanet(2).OwnerId=2; s.State.SpaceSightings.Clear(); var ship=Ship(s);
        Check(!s.IsPlanetKnown(1,2) && !Cmd(s,CommandKind.FlyShip,ship.Id,planet:2),"Unknown target was exposed");
        var point=s.GetOrbitPosition(2);
        s.Enqueue(new SimCommand {Id=s.NextCommandId(),Kind=CommandKind.MoveShipPosition,ActorId=ship.Id,PlanetId=1,X=(int)Math.Round(point.X),Y=(int)Math.Round(point.Y)});
        s.Step(200); Check(s.IsPlanetKnown(1,2),"Physical sensor failed to discover world");
        var saved=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(s.State))); s.Step(30); saved.Step(30);
        Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(saved.State),"Free flight did not persist");
    }
}
