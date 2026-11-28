using System;
using System.IO;
using PlanetOne.Core;

internal static class CoreTests
{
    private static int failures;
    private static int checks;

    private static int Main(string[] args)
    {
        if(args.Length>0 && args[0]=="--ground-traffic") {GroundTrafficTests.Register(Run);Console.WriteLine(failures==0?"All "+checks+" ground traffic tests passed.":failures+" ground traffic test(s) failed.");return failures==0?0:1;}
        if(args.Length>0 && args[0]=="--space-traffic") {SpaceTrafficTests.Register(Run);Console.WriteLine(failures==0?"All "+checks+" space traffic tests passed.":failures+" space traffic test(s) failed.");return failures==0?0:1;}
        if(args.Length>0 && args[0]=="--construction") {ConstructionVehicleTests.Register(Run);Console.WriteLine(failures==0?"All "+checks+" construction tests passed.":failures+" construction test(s) failed.");return failures==0?0:1;}
        if(args.Length>0 && args[0]=="--combat") {Run("M8 paid reference defense",ReferenceCombat.Run);return failures==0?0:1;}
        if(args.Length>0 && args[0]=="--reference") {ReferenceTests.Register(Run);Console.WriteLine(failures==0?"All "+checks+" reference tests passed.":failures+" reference test(s) failed.");return failures==0?0:1;}
        Run("Fixed steps do not depend on render frame grouping", FixedSteps);
        Run("Pause preserves world and queues commands", Pause);
        Run("Invalid ownership, terrain and overlap reject builds", InvalidBuilds);
        Run("Duplicate commands spend materials once", DuplicateCommands);
        Run("Road-free construction still requires real material and builder delivery", PhysicalConstruction);
        Run("Unreachable construction never reserves local stock", UnreachableConstruction);
        Run("Concurrent construction cannot overbook materials", ReservationBounds);
        Run("Ore becomes metal through real local deliveries", ProductionChain);
        Run("Local production can use stock below an export reserve", LocalStockIsUsable);
        Run("Both colony economies advance independently of views", BothColonies);
        Run("Energy interruption does not consume recipe twice", InterruptedProduction);
        Run("Snapshots preserve in-flight cargo and future results", SaveLoadInFlight);
        Run("Invalid snapshots fail without partial recovery", InvalidSaves);
        Run("Moon orbit remains relative to its moving planet", MoonOrbit);
        Run("Faction commands share validation and structured feedback", FactionPermissions);
        Run("Session commands pause safely and 1x 2x 4x run the same world", SessionCommands);
        Run("Construction capacity respects priorities and unspent material", ConstructionCapacity);
        ConstructionVehicleTests.Register(Run);
        SupplyNetworkTests.Register(Run);
        Run("Food shortage recovers without free rations or worker extinction", FoodShortageRecovery);
        Run("Priorities and enabled state control scarce energy and workers", OperatingPriorities);
        Run("Terrain isolation holds real loaded cargo until access is restored", RoadInterruption);
        Run("Automatic terrain rerouting preserves the courier position", AlternateRoute);
        Run("Cancellation before collection releases every reservation", CancelUnloadedConstruction);
        Run("Cancellation returns loaded cargo physically and survives save load", CancelLoadedConstruction);
        Run("Demolition recovers declared material without teleportation", DemolitionRecovery);
        Run("Four returning cargo loads cannot deadlock at a full source", FullSourceRecovery);
        Run("Version 1 migration preserves stocks ids and returning couriers", LegacyMigration);
        Run("Original prototype v1 save migrates without losing real state", ActualLegacyFixture);
        Run("Twenty minute colony sustains population and pays expansion from local production", SustainableColony);
        MilitaryTests.Register(Run); SpaceTests.Register(Run);
        GroundTrafficTests.Register(Run);SpaceTrafficTests.Register(Run);
        CombatPresentationTests.Register(Run);
        InvasionTests.Register(Run);
        HexGridTests.Register(Run);
        AiTests.Register(Run);
        DiplomacyTests.Register(Run);
        OrbitTests.Register(Run);
        PlanetEnvironmentTests.Register(Run);
        AsteroidTests.Register(Run);
        SnapshotStoreTests.Register(Run);
        SaveSlotTests.Register(Run);
        TransitionTests.Register(Run);
        ReferenceTests.Register(Run);
        Console.WriteLine(failures == 0 ? "All " + checks + " core tests passed." : failures + " test(s) failed.");
        return failures == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        checks++;
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception error) { failures++; Console.WriteLine("FAIL " + name + ": " + error); }
    }

    private static void Assert(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    private static void Build(GameSimulation sim, BuildingKind kind, int x, int y, int planet = 1)
    {
        Assert(sim.Enqueue(new SimCommand { Id = sim.NextCommandId(), PlanetId = planet,
            Kind = CommandKind.PlaceBuilding, Building = kind, X = x, Y = y }), "Build was not queued.");
    }

    private static void Road(GameSimulation sim, int x, int y, int planet = 1)
    {
        sim.Enqueue(new SimCommand { Id = sim.NextCommandId(), PlanetId = planet,
            Kind = CommandKind.PlaceRoad, X = x, Y = y });
    }

    private static void FixedSteps()
    {
        var a = GameSimulation.CreateDefault(7); var b = GameSimulation.CreateDefault(7);
        for (int i = 0; i < 600; i++) a.Advance(1.0 / 60);
        for (int i = 0; i < 100; i++) b.Advance(0.1);
        Assert(a.State.Tick == 200 && b.State.Tick == 200, "Frame grouping changed ticks.");
        Assert(Math.Abs(a.GetOrbitPosition(1).X - b.GetOrbitPosition(1).X) < 1e-9, "Orbit differs.");
    }

    private static void Pause()
    {
        var sim = GameSimulation.CreateDefault(); sim.State.Paused = true;
        Build(sim, BuildingKind.SolarPlant, 10, 5);
        sim.Advance(10); sim.Step(100);
        Assert(sim.State.Tick == 0 && sim.FindPlanet(1).Buildings.Count == 1, "Pause changed world.");
        Assert(sim.State.PendingCommands.Count == 1, "Pause lost order.");
        sim.State.Paused = false; sim.Step();
        Assert(sim.FindPlanet(1).Buildings.Count == 2, "Queued command not applied.");
    }

    private static void InvalidBuilds()
    {
        var sim = GameSimulation.CreateDefault();
        Build(sim, BuildingKind.Mine, 3, 3); // No ore.
        Build(sim, BuildingKind.Smelter, 8, 6); // Headquarters.
        Build(sim, BuildingKind.Smelter, 0, 0); // Rock.
        Build(sim, BuildingKind.SolarPlant, 10, 5, 3); // Neutral moon.
        sim.Enqueue(new SimCommand { Id = sim.NextCommandId(), IssuerId = 2, PlanetId = 1,
            Kind = CommandKind.PlaceBuilding, Building = BuildingKind.SolarPlant, X = 10, Y = 5 });
        sim.Step();
        Assert(sim.FindPlanet(1).Buildings.Count == 1 && sim.FindPlanet(3).Buildings.Count == 0, "Invalid build succeeded.");
        Assert(sim.FindPlanet(1).Buildings[0].Inventory[(int)ResourceKind.Metal] == 100, "Invalid build spent stock.");
    }

    private static void DuplicateCommands()
    {
        var sim = GameSimulation.CreateDefault();
        var command = new SimCommand { Id = 99, PlanetId = 1, Kind = CommandKind.PlaceBuilding,
            Building = BuildingKind.SolarPlant, X = 10, Y = 5 };
        Assert(sim.Enqueue(command), "First order failed.");
        Assert(!sim.Enqueue(command), "Duplicate pending order accepted.");
        sim.Step(400);
        Assert(!sim.Enqueue(command), "Duplicate completed order accepted.");
        Assert(sim.FindPlanet(1).Buildings.Count == 2, "Duplicate created building.");
        Assert(sim.FindPlanet(1).Buildings[0].Inventory[(int)ResourceKind.Metal] == 92, "Cost applied more than once.");
    }

    private static void PhysicalConstruction()
    {
        var sim = GameSimulation.CreateDefault();
        var p = sim.FindPlanet(1);
        Array.Clear(p.Roads,0,p.Roads.Length);
        Build(sim, BuildingKind.SolarPlant, 2, 2); sim.Step();
        var site = p.Buildings[1];
        Assert(site.BuildProgress == 0 && p.Buildings[0].Inventory[(int)ResourceKind.Metal] == 100,
            "Connected construction teleported or immediately consumed its goods.");
        Assert(p.Buildings[0].ReservedOutgoing[(int)ResourceKind.Metal] == 8, "Connected construction did not reserve materials.");
        Assert(p.Shipments.Count > 0 && !site.Built, "No real courier dispatched.");
        sim.Step(500);
        Assert(site.Built, "Connected site did not receive its goods.");
        SaveCodec.Validate(sim.State);
    }

    private static void UnreachableConstruction()
    {
        var sim = GameSimulation.CreateDefault();
        var planet = sim.FindPlanet(1);
        LogisticsBarrier.Column(planet,2);
        string preview = sim.ValidatePlacement(1, BuildingKind.Warehouse, 1, 3);
        Assert(preview != null && preview.Contains("befahrbarer Weg"), "Preview did not explain blocked terrain access.");
        Build(sim, BuildingKind.Warehouse, 1, 3);
        Build(sim, BuildingKind.Warehouse, 0, 4);
        Build(sim, BuildingKind.Warehouse, 0, 3);
        sim.Step(100);
        Assert(planet.Buildings.Count == 1 && planet.Deliveries.Count == 0 && planet.Shipments.Count == 0,
            "Unreachable sites created buildings or delivery jobs.");
        var headquarters = planet.Buildings[0];
        Assert(headquarters.Inventory[(int)ResourceKind.Metal] == 100 && headquarters.Inventory[(int)ResourceKind.Parts] == 40,
            "Unreachable construction changed physical stock.");
        foreach (int amount in headquarters.ReservedOutgoing) Assert(amount == 0, "Unreachable site retained a reservation.");
        // Revalidation is essential: a preview/queued order may become unreachable before execution.
        Build(sim, BuildingKind.SolarPlant, 10, 5);
        LogisticsBarrier.Column(planet,9);
        sim.Step();
        Assert(planet.Buildings.Count == 1 && headquarters.ReservedOutgoing[(int)ResourceKind.Metal] == 0,
            "Command execution did not revalidate its route.");
        SaveCodec.Validate(sim.State);
    }

    private static void ReservationBounds()
    {
        var sim = GameSimulation.CreateDefault();
        var p = sim.FindPlanet(1); p.Buildings[0].Inventory[(int)ResourceKind.Metal] = 10;
        Build(sim, BuildingKind.SolarPlant, 10, 5); Build(sim, BuildingKind.SolarPlant, 11, 5);
        sim.Step();
        Assert(p.Buildings.Count == 2, "Concurrent orders overbooked stock.");
        Assert(p.Buildings[0].ReservedOutgoing[(int)ResourceKind.Metal] == 8, "Wrong reservation total.");
        SaveCodec.Validate(sim.State);
    }

    private static void BuildChain(GameSimulation sim, int planet = 1)
    {
        Build(sim, BuildingKind.SolarPlant, 10, 5, planet);
        Build(sim, BuildingKind.Mine, 4, 4, planet);
        Build(sim, BuildingKind.Smelter, 6, 5, planet);
    }

    private static int ActualAmount(PlanetState p, ResourceKind resource)
    {
        int total = 0;
        foreach (var building in p.Buildings) total += building.Inventory[(int)resource];
        foreach (var shipment in p.Shipments)
            if (shipment.Resource == resource && (shipment.Phase == ShipmentPhase.InTransit || shipment.Phase == ShipmentPhase.Unloading))
                total += shipment.Amount;
        return total;
    }

    private static void ProductionChain()
    {
        var sim = GameSimulation.CreateDefault();
        int initialDeposit = sim.FindPlanet(1).OreRemaining[4 * sim.FindPlanet(1).Width + 4];
        BuildChain(sim); sim.Step(3000);
        var p = sim.FindPlanet(1);
        Assert(p.Buildings.TrueForAll(b => b.Built), "Chain buildings incomplete.");
        Assert(p.OreRemaining[4 * p.Width + 4] < initialDeposit, "Mine did not deplete ore.");
        int metalAfterCosts = 100 - 8 - 8 - 12;
        Assert(ActualAmount(p, ResourceKind.Metal) > metalAfterCosts, "Smelter did not produce metal.");
        Assert(ActualAmount(p, ResourceKind.Ore) > 0, "No ore stored or shipped.");
        SaveCodec.Validate(sim.State);
    }

    private static void BothColonies()
    {
        var sim = GameSimulation.CreateDefault(); BuildChain(sim, 1); BuildChain(sim, 2); sim.Step(2000);
        Assert(ActualAmount(sim.FindPlanet(1), ResourceKind.Metal) == ActualAmount(sim.FindPlanet(2), ResourceKind.Metal),
            "Same colonies ran different amounts of simulation.");
        Assert(ActualAmount(sim.FindPlanet(2), ResourceKind.Metal) > 72, "Background colony did not produce.");
    }

    private static void LocalStockIsUsable()
    {
        var sim = GameSimulation.CreateDefault();
        var planet = sim.FindPlanet(1);
        var headquarters = planet.Buildings[0];
        // Explicit regression fixture: construction uses 14 metal, leaving 6 for three recipes.
        // This entire amount is below the former hidden 30-metal export reserve.
        headquarters.Inventory[(int)ResourceKind.Metal] = 20;
        Build(sim, BuildingKind.Workshop, 10, 5);
        sim.Step(2000);
        var workshop = planet.Buildings.Find(b => b.Kind == BuildingKind.Workshop);
        Assert(workshop != null && workshop.Built, "Workshop did not finish construction.");
        Assert(ActualAmount(planet, ResourceKind.Metal) == 0, "Uncommitted local metal was withheld from production.");
        Assert(ActualAmount(planet, ResourceKind.Parts) == 39,
            "Expected 40 initial parts minus 4 construction parts plus 3 locally produced parts.");
        Assert(planet.Deliveries.Count == 0 && planet.Shipments.Count == 0, "Local delivery did not complete.");
        SaveCodec.Validate(sim.State);
    }

    private static void InterruptedProduction()
    {
        var sim = GameSimulation.CreateDefault(); BuildChain(sim);
        var p = sim.FindPlanet(1); BuildingState smelter = null;
        for (int i = 0; i < 3000; i++)
        {
            sim.Step(); smelter = p.Buildings.Find(b => b.Kind == BuildingKind.Smelter);
            if (smelter != null && smelter.CycleActive && smelter.CycleProgress > 0.1) break;
        }
        Assert(smelter != null && smelter.CycleActive, "No active recipe found.");
        // Isolate solar supply without altering production or inventory values.
        p.Buildings.Find(b=>b.Kind==BuildingKind.SolarPlant).Enabled=false;
        sim.Step(1000); // The real energy buffer must discharge before the shortage stops production.
        int pausedTicks = 0;
        for (int i = 0; i < 200; i++)
        {
            double progress = smelter.CycleProgress;
            int consumed = p.ConsumedTotals[(int)ResourceKind.Ore];
            sim.Step();
            if (smelter.Status != "Zu wenig Energie") continue;
            pausedTicks++;
            Assert(Math.Abs(smelter.CycleProgress - progress) < 1e-9, "No energy did not pause recipe.");
            Assert(p.ConsumedTotals[(int)ResourceKind.Ore] == consumed, "Paused recipe consumed inputs again.");
        }
        Assert(pausedTicks > 0, "Energy shortage was entirely ignored.");
        p.Buildings.Find(b=>b.Kind==BuildingKind.SolarPlant).Enabled=true;
        sim.Step(300);
        Assert(smelter.Status != "Zu wenig Energie", "Restored energy was ignored.");
        SaveCodec.Validate(sim.State);
    }

    private static void SaveLoadInFlight()
    {
        var a = GameSimulation.CreateDefault(44); BuildChain(a); a.Step(18);
        Assert(a.FindPlanet(1).Shipments.Exists(s => s.Phase == ShipmentPhase.InTransit), "Fixture has no cargo in flight.");
        a.State.Paused = true; Road(a, 13, 8);
        var b = new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(a.State)));
        Assert(SaveCodec.ToJson(a.State) == SaveCodec.ToJson(b.State), "Snapshot changed state.");
        a.State.Paused = false; b.State.Paused = false;
        a.Step(1800); b.Step(1800);
        Assert(SaveCodec.ToJson(a.State) == SaveCodec.ToJson(b.State), "Restored future diverged.");
    }

    private static void InvalidSaves()
    {
        ExpectInvalid(() => SaveCodec.FromJson("{}"));
        ExpectInvalid(() => SaveCodec.FromJson("not json"));
        var state = GameSimulation.CreateDefault().State; state.Version = 99;
        ExpectInvalid(() => SaveCodec.ToJson(state));
        state.Version = SaveCodec.CurrentVersion; state.Planets[0].Buildings[0].ReservedOutgoing[2] = 1;
        ExpectInvalid(() => SaveCodec.ToJson(state));
        state.Planets[0].Buildings[0].ReservedOutgoing[2] = 0;
        state.Planets[1].ParentId = 3;
        ExpectInvalid(() => SaveCodec.ToJson(state));
    }

    private static void ExpectInvalid(Action action)
    {
        bool rejected = false;
        try { action(); } catch (InvalidDataException) { rejected = true; }
        Assert(rejected, "Invalid snapshot was accepted.");
    }

    private static void MoonOrbit()
    {
        var sim = GameSimulation.CreateDefault(); sim.Step(731);
        var p = sim.GetOrbitPosition(2); var m = sim.GetOrbitPosition(3);
        var moon=sim.FindPlanet(3);double c=Math.Cos(moon.OrbitPeriapsis),s=Math.Sin(moon.OrbitPeriapsis);
        double x=(m.X-p.X)*c+(m.Y-p.Y)*s+moon.OrbitRadius*moon.OrbitEccentricity;
        double y=-(m.X-p.X)*s+(m.Y-p.Y)*c;
        Assert(Math.Abs(x*x/(moon.OrbitRadius*moon.OrbitRadius)+y*y/(moon.OrbitRadius*moon.OrbitRadius*(1-moon.OrbitEccentricity*moon.OrbitEccentricity))-1)<1e-9,
            "Moon does not follow its parent-relative ellipse.");
    }

    private static void Command(GameSimulation sim, CommandKind kind, int planet = 1, long target = 0,
        int x = 0, int y = 0, int value = 0, bool flag = false, int issuer = 1)
    {
        sim.Enqueue(new SimCommand { Id = sim.NextCommandId(), IssuerId = issuer, Kind = kind,
            PlanetId = planet, TargetId = target, X = x, Y = y, IntValue = value, BoolValue = flag });
    }

    private static void WaitFor(GameSimulation sim, Func<bool> condition, int maxTicks = 5000)
    {
        for (int i = 0; i < maxTicks && !condition(); i++) sim.Step();
        Assert(condition(), "Expected condition did not occur within " + maxTicks + " ticks.");
    }

    private static void FactionPermissions()
    {
        var sim = GameSimulation.CreateDefault();
        sim.FindPlanet(2).OwnerId = 2;
        Build(sim, BuildingKind.SolarPlant, 10, 5, 2);
        Command(sim, CommandKind.RemoveRoad, 2, x: 9, y: 6);
        sim.Step();
        Assert(sim.FindPlanet(2).Buildings.Count == 1 && sim.FindPlanet(2).Roads[6 * sim.FindPlanet(2).Width + 9], "Foreign commands changed a colony.");
        Assert(sim.State.CommandResults.TrueForAll(r => !r.Success && r.Code == "PermissionDenied"), "Missing structured rejection.");
        Assert(sim.State.Events.Exists(e => e.Code == "CommandRejected" && e.PlanetId == 2), "Missing command event.");
        sim.Enqueue(new SimCommand { Id = sim.NextCommandId(), IssuerId = 2, PlanetId = 2,
            Kind = CommandKind.PlaceBuilding, Building = BuildingKind.SolarPlant, X = 10, Y = 5 });
        sim.Step(500);
        Assert(sim.FindPlanet(2).Buildings.Count == 2 && sim.FindPlanet(2).Buildings[1].Built, "Scenario faction could not use the shared commands.");
        Command(sim, CommandKind.SetPaused, flag: true, issuer: 2);
        Assert(!sim.State.Paused, "Non-player faction changed session controls.");
        sim.State.Relations[0].Relation = RelationKind.War;
        var restored = SaveCodec.FromJson(SaveCodec.ToJson(sim.State));
        Assert(restored.Relations[0].Relation == RelationKind.War && restored.Factions.Count == 2, "Scenario relationships were lost.");
    }

    private static void SessionCommands()
    {
        var a = GameSimulation.CreateDefault(19); var b = GameSimulation.CreateDefault(19); var c = GameSimulation.CreateDefault(19);
        BuildChain(a); BuildChain(b); BuildChain(c);
        Command(b, CommandKind.SetTimeScale, value: 2); Command(c, CommandKind.SetTimeScale, value: 4);
        for (int i = 0; i < 1200; i++) a.Advance(0.05);
        for (int i = 0; i < 600; i++) b.Advance(0.05);
        for (int i = 0; i < 300; i++) c.Advance(0.05);
        Assert(a.State.Tick == b.State.Tick && b.State.Tick == c.State.Tick, "Game speed changed simulated duration.");
        for (int r = 0; r < 6; r++)
            Assert(ActualAmount(a.FindPlanet(1), (ResourceKind)r) == ActualAmount(b.FindPlanet(1), (ResourceKind)r) &&
                ActualAmount(a.FindPlanet(1), (ResourceKind)r) == ActualAmount(c.FindPlanet(1), (ResourceKind)r), "Game speed changed economy.");
        Command(a, CommandKind.SetPaused, flag: true);
        long pausedTick = a.State.Tick;
        Build(a, BuildingKind.Farm, 9, 7); a.Advance(50);
        Assert(a.State.Tick == pausedTick && a.State.PendingCommands.Count == 1, "Session pause did not preserve queued intent.");
        var restored = new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(a.State)));
        Command(restored, CommandKind.SetPaused, flag: false); restored.Step();
        Assert(restored.State.Tick == pausedTick + 1 && restored.FindPlanet(1).Buildings.Exists(x => x.Kind == BuildingKind.Farm), "Paused saved command was not resumed.");
    }

    private static void ConstructionCapacity()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1); var hq = p.Buildings[0];
        // Explicit fixture: three sites have already received their full material shipments.
        for (int i = 0; i < 3; i++)
        {
            var cost = Definitions.Get(BuildingKind.SolarPlant).Cost;
            var site = new BuildingState { Id = sim.State.NextEntityId++, PlanetId = 1, Kind = BuildingKind.SolarPlant,
                X = 10 + i, Y = 5, Inventory = (int[])cost.Clone(), Priority = i == 2 ? 2 : 1 };
            for (int r = 0; r < 6; r++) hq.Inventory[r] -= cost[r];
            p.Buildings.Add(site);
        }
        sim.Step();
        Assert(p.Shipments.FindAll(GameSimulation.IsConstructionVehicle).Count == Definitions.ConstructionSlotsPerColony &&
            p.Buildings.TrueForAll(b => !b.ConstructionStarted), "Builders were not dispatched within the shared capacity or started before arrival.");
        Assert(p.Shipments.Exists(s => s.TargetId == p.Buildings[3].Id) &&
            !p.Shipments.Exists(s => s.TargetId == p.Buildings[2].Id), "Priority was ignored for construction.");
        Assert(p.Buildings[2].Inventory[(int)ResourceKind.Metal] == 8, "Waiting site consumed materials before construction.");
        WaitFor(sim, () => p.Buildings.TrueForAll(b => b.Built), 1000);
        Assert(p.Buildings.TrueForAll(b => b.Built), "Queued construction did not finish.");
        SaveCodec.Validate(sim.State);
    }

    private static void FoodShortageRecovery()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1);
        p.Buildings[0].Inventory[(int)ResourceKind.Food] = 1; // Deliberate shortage fixture, not free recovery supplies.
        sim.Step(2000);
        Assert(p.FoodShortageSeconds > 0 && p.Productivity == Definitions.MinimumProductivity && p.WorkersTotal >= 4,
            "Food shortage failed to reduce productivity while preserving recovery workers.");
        int initialPopulation = p.Population;
        Build(sim, BuildingKind.Farm, 9, 7); Build(sim, BuildingKind.Housing, 7, 7); Build(sim, BuildingKind.SolarPlant, 10, 5);
        sim.Step(6000);
        Assert(p.FoodProducedTotal > 0 && p.FoodConsumedTotal > 1 && p.FoodShortageSeconds == 0 && p.Productivity == 1,
            "Locally produced food did not restore supply.");
        Assert(p.Population > initialPopulation && p.Population <= p.HousingCapacity, "Fed housed colony did not grow within housing capacity.");
        Assert(sim.State.Events.Exists(e => e.Code == "FoodRestored"), "Recovery event missing.");
        SaveCodec.Validate(sim.State);
    }

    private static void OperatingPriorities()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1);
        Build(sim, BuildingKind.Mine, 4, 4); Build(sim, BuildingKind.Smelter, 6, 5); sim.Step(1400);
        var mine = p.Buildings.Find(b => b.Kind == BuildingKind.Mine);
        var smelter = p.Buildings.Find(b => b.Kind == BuildingKind.Smelter);
        int originalShortageTicks = 0;
        for (int i = 0; i < 1000; i++)
        { sim.Step(); if (smelter.Status == "Zu wenig Energie") originalShortageTicks++; }
        Assert(originalShortageTicks > 0, "Scarce energy was not enforced after buffer discharge.");
        Command(sim, CommandKind.SetBuildingPriority, target: smelter.Id, value: 2);
        int minePowerShortageTicks = 0, smelterPowerShortageTicks = 0;
        for (int i = 0; i < 1000; i++)
        {
            sim.Step();
            if (mine.Status == "Zu wenig Energie") minePowerShortageTicks++;
            if (smelter.Status == "Zu wenig Energie") smelterPowerShortageTicks++;
        }
        Assert(minePowerShortageTicks > 0 && smelterPowerShortageTicks == 0, "Priority did not protect the higher-priority energy consumer.");
        Command(sim, CommandKind.SetBuildingEnabled, target: smelter.Id, flag: false); sim.Step(200);
        Assert(smelter.Status == "Betrieb angehalten" && mine.Status != "Zu wenig Energie", "Disabling did not free energy.");
        // Distinct worker shortage fixture, enough energy but only four people available.
        p.Population = 4; Build(sim, BuildingKind.SolarPlant, 10, 5);
        Command(sim, CommandKind.SetBuildingEnabled, target: smelter.Id, flag: true); sim.Step(500);
        Assert(p.WorkersUsed <= p.WorkersTotal && mine.Status == "Zu wenig Arbeitskräfte", "Labor allocation ignored priority or exceeded workers.");
        SaveCodec.Validate(sim.State);
    }

    private static void RoadInterruption()
    {
        var sim = GameSimulation.CreateDefault(); BuildChain(sim); var p = sim.FindPlanet(1);
        ShipmentState cargo = null;
        WaitFor(sim, () =>
        {
            cargo = p.Shipments.Find(s => s.Phase == ShipmentPhase.InTransit && s.DistanceTravelled == 0 &&
                p.Buildings.Find(b => b.Id == s.SourceId).Kind == BuildingKind.Mine);
            return cargo != null;
        });
        int amount = cargo.Amount; var position = sim.GetShipmentPosition(cargo);
        var terrain=LogisticsBarrier.Ring(p,4,4);sim.Step(120);
        var held = sim.GetShipmentPosition(cargo);
        Assert(cargo.Amount == amount && cargo.RouteBlocked && held.X == position.X && held.Y == position.Y,
            "Disconnected loaded courier lost cargo or moved through the break.");
        Assert(sim.State.Events.Exists(e => e.Code == "DeliveryBlocked"), "Blocked delivery event missing.");
        LogisticsBarrier.Restore(p,terrain);
        var restored = new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(sim.State)));
        sim.Step(1800); restored.Step(1800);
        Assert(SaveCodec.ToJson(sim.State) == SaveCodec.ToJson(restored.State), "Road repair continuation differs after reload.");
        Assert(!p.Shipments.Exists(s => s.Id == cargo.Id), "Repaired road did not finish the held trip.");
    }

    private static void AlternateRoute()
    {
        var sim = GameSimulation.CreateDefault(); Road(sim, 9, 5); Build(sim, BuildingKind.SolarPlant, 10, 5);
        var p = sim.FindPlanet(1); ShipmentState cargo = null;
        WaitFor(sim, () => { cargo = p.Shipments.Find(s => s.Phase == ShipmentPhase.InTransit && s.DistanceTravelled > 0.5); return cargo != null; });
        var before = sim.GetShipmentPosition(cargo);
        var blocked=cargo.Path[Math.Min((int)cargo.DistanceTravelled+1,cargo.Path.Count-2)];p.Terrain[blocked.Y*p.Width+blocked.X]=1;sim.Step();
        var after = sim.GetShipmentPosition(cargo);
        double movement = Math.Sqrt((after.X - before.X) * (after.X - before.X) + (after.Y - before.Y) * (after.Y - before.Y));
        Assert(movement <= Definitions.CourierSpeed * Definitions.TickSeconds + 0.000001, "Rerouting teleported the courier.");
        Assert(!cargo.Path.Exists(cell => cell.X == blocked.X && cell.Y == blocked.Y), "Reroute still crosses the new terrain obstacle.");
        sim.Step(500);
        Assert(p.Buildings[1].Built, "Alternative road did not deliver construction supplies.");
        SaveCodec.Validate(sim.State);
    }

    private static void CancelUnloadedConstruction()
    {
        var sim = GameSimulation.CreateDefault(); Build(sim, BuildingKind.SolarPlant, 10, 5); sim.Step();
        var p = sim.FindPlanet(1);
        Command(sim, CommandKind.CancelConstruction, target: p.Buildings[1].Id); sim.Step();
        Assert(p.Buildings.Count == 1 && p.Deliveries.Count == 0 && p.Shipments.Count == 0, "Uncollected cancellation left a site or order.");
        Assert(ActualAmount(p, ResourceKind.Metal) == 100 && ActualAmount(p, ResourceKind.Parts) == 40, "Uncollected cancellation lost or duplicated goods.");
        SaveCodec.Validate(sim.State);
    }

    private static void CancelLoadedConstruction()
    {
        var sim = GameSimulation.CreateDefault(); Build(sim, BuildingKind.SolarPlant, 12, 5); sim.Step(18);
        var p = sim.FindPlanet(1); var hq = p.Buildings[0]; int stockBefore = hq.Inventory[(int)ResourceKind.Metal];
        Assert(p.Shipments.Exists(s => s.Phase == ShipmentPhase.InTransit), "No loaded courier in cancellation fixture.");
        Command(sim, CommandKind.CancelConstruction, target: p.Buildings[1].Id); sim.Step();
        Assert(hq.Inventory[(int)ResourceKind.Metal] == stockBefore && p.Shipments.Exists(s => s.Phase == ShipmentPhase.Recovering),
            "Loaded cancellation remotely refunded cargo.");
        var restored = new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(sim.State)));
        sim.Step(500); restored.Step(500);
        Assert(SaveCodec.ToJson(sim.State) == SaveCodec.ToJson(restored.State), "Recovery changed after reload.");
        Assert(p.Buildings.Count == 1 && ActualAmount(p, ResourceKind.Metal) == 100 && ActualAmount(p, ResourceKind.Parts) == 40,
            "Loaded cancellation did not recover actual goods exactly once.");
    }

    private static void DemolitionRecovery()
    {
        var sim = GameSimulation.CreateDefault(); Build(sim, BuildingKind.SolarPlant, 10, 5); sim.Step(500);
        var p = sim.FindPlanet(1); var solar = p.Buildings[1];
        var preview = GameSimulation.GetRecoveryPreview(solar);
        Assert(preview[(int)ResourceKind.Metal] == 4 && preview[(int)ResourceKind.Parts] == 1, "Demolition preview differs from 50% rule.");
        int before = p.Buildings[0].Inventory[(int)ResourceKind.Metal];
        Command(sim, CommandKind.DemolishBuilding, target: solar.Id); sim.Step();
        Assert(solar.Deconstructing && solar.Inventory[(int)ResourceKind.Metal] == 4 &&
            p.Buildings[0].Inventory[(int)ResourceKind.Metal] == before, "Salvage teleported or building remained operational.");
        sim.Step(600);
        Assert(p.Buildings.Count == 1 && ActualAmount(p, ResourceKind.Metal) == 96 && ActualAmount(p, ResourceKind.Parts) == 39,
            "Demolition produced the wrong material total.");
        SaveCodec.Validate(sim.State);
    }

    private static void FullSourceRecovery()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1);
        for (int x = 10; x <= 13; x++) Build(sim, BuildingKind.SolarPlant, x, 5);
        sim.Step(10);
        Assert(p.Shipments.Count == 4 && p.Shipments.TrueForAll(s => s.Phase == ShipmentPhase.InTransit), "Expected four loaded couriers.");
        var hq = p.Buildings[0];
        Assert(hq.ReservedReturnSpace == 20, "Loaded cargo has no guaranteed return capacity.");
        // Explicit capacity fixture: other stock fills every unreserved source slot.
        int nonFood = 0; for (int r = 1; r < 6; r++) nonFood += hq.Inventory[r];
        hq.Inventory[0] = GameSimulation.StorageCapacity(hq) - hq.ReservedReturnSpace - nonFood;
        for (int i = 1; i < p.Buildings.Count; i++) Command(sim, CommandKind.CancelConstruction, target: p.Buildings[i].Id);
        sim.Step();
        var restored = new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(sim.State)));
        sim.Step(600); restored.Step(600);
        Assert(SaveCodec.ToJson(sim.State) == SaveCodec.ToJson(restored.State), "Full-source recovery diverged after reload.");
        Assert(p.Shipments.Count == 0 && p.Deliveries.Count == 0 && p.Buildings.Count == 1,
            "Four recovering couriers deadlocked despite guaranteed capacity.");
        Assert(hq.Inventory[(int)ResourceKind.Metal] == 100 && hq.Inventory[(int)ResourceKind.Parts] == 40,
            "Full-source recovery lost real cargo.");
        SaveCodec.Validate(sim.State);
    }

    private static void LegacyMigration()
    {
        var sim = GameSimulation.CreateDefault(); Build(sim, BuildingKind.SolarPlant, 10, 5);
        WaitFor(sim, () => sim.FindPlanet(1).Shipments.Exists(s => s.Phase == ShipmentPhase.Returning));
        var p = sim.FindPlanet(1); var returning = p.Shipments.Find(s => s.Phase == ShipmentPhase.Returning);
        var before = sim.GetShipmentPosition(returning); int metal = ActualAmount(p, ResourceKind.Metal);
        // Reproduce the version-1 encoding: return routes used the outward orientation.
        foreach (var shipment in p.Shipments) if (shipment.Phase == ShipmentPhase.Returning) shipment.Path.Reverse();
        sim.State.Version = 1;
        string legacy;
        using (var stream = new MemoryStream())
        {
            new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(GameState)).WriteObject(stream, sim.State);
            legacy = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        var migrated = new GameSimulation(SaveCodec.FromJson(legacy));
        var after = migrated.GetShipmentPosition(migrated.FindPlanet(1).Shipments.Find(s => s.Id == returning.Id));
        Assert(before.X == after.X && before.Y == after.Y && ActualAmount(migrated.FindPlanet(1), ResourceKind.Metal) == metal,
            "Legacy migration changed returning courier position or stock.");
        Assert(migrated.State.Version == SaveCodec.CurrentVersion && migrated.State.Factions.Count == 2 && migrated.FindPlanet(1).Population == 16,
            "Legacy migration did not initialize current schema.");
        migrated.Step(1000); SaveCodec.Validate(migrated.State);
    }

    private static void ActualLegacyFixture()
    {
        // Unmodified save from the first shipped prototype's graphical smoke run (2026-11-23).
        string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "Tests", "Core", "Fixtures", "prototype-v1.json"));
        string json = File.ReadAllText(path);
        GameState original;
        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            original = (GameState)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(GameState)).ReadObject(stream);
        Assert(original.Version == 1 && original.Tick == 7000, "The fixture is not the original prototype save.");
        var migrated = new GameSimulation(SaveCodec.FromJson(json));
        Assert(migrated.State.Tick == original.Tick && migrated.State.NextEntityId == original.NextEntityId &&
            migrated.State.NextCommand == original.NextCommand && migrated.State.PendingCommands.Count == original.PendingCommands.Count,
            "Migration changed time, ids or outstanding commands.");
        foreach (var oldPlanet in original.Planets)
        {
            var current = migrated.FindPlanet(oldPlanet.Id);
            Assert(current.Buildings.Count == oldPlanet.Buildings.Count && current.Shipments.Count == oldPlanet.Shipments.Count &&
                current.Deliveries.Count == oldPlanet.Deliveries.Count, "Migration changed existing entities.");
            for (int r = 0; r < 6; r++)
                Assert(ActualAmount(current, (ResourceKind)r) == ActualAmount(oldPlanet, (ResourceKind)r), "Migration changed an actual material amount.");
            foreach (var oldBuilding in oldPlanet.Buildings)
            {
                var building = current.Buildings.Find(b => b.Id == oldBuilding.Id);
                Assert(building != null && building.Kind == oldBuilding.Kind && building.X == oldBuilding.X && building.Y == oldBuilding.Y &&
                    building.Built == oldBuilding.Built && building.BuildProgress == oldBuilding.BuildProgress &&
                    building.CycleActive == oldBuilding.CycleActive && building.CycleProgress == oldBuilding.CycleProgress,
                    "Migration changed a building or running recipe.");
                for (int r = 0; r < 6; r++)
                    Assert(building.Inventory[r] == oldBuilding.Inventory[r] && building.ReservedIncoming[r] == oldBuilding.ReservedIncoming[r] &&
                        building.ReservedOutgoing[r] == oldBuilding.ReservedOutgoing[r], "Migration changed a local inventory or reservation.");
            }
            foreach (var oldShipment in oldPlanet.Shipments)
            {
                var shipment = current.Shipments.Find(s => s.Id == oldShipment.Id);
                Assert(shipment != null && shipment.Amount == oldShipment.Amount && shipment.Phase == oldShipment.Phase &&
                    shipment.DistanceTravelled == oldShipment.DistanceTravelled && shipment.PhaseProgress == oldShipment.PhaseProgress,
                    "Migration changed actual cargo or progress.");
                for (int i = 0; i < oldShipment.Path.Count; i++)
                {
                    int originalIndex = oldShipment.Phase == ShipmentPhase.Returning ? oldShipment.Path.Count - 1 - i : i;
                    Assert(shipment.Path[i].X == oldShipment.Path[originalIndex].X && shipment.Path[i].Y == oldShipment.Path[originalIndex].Y,
                        "Migration changed a courier route beyond the required return direction conversion.");
                }
            }
        }
        var restored = new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(migrated.State)));
        migrated.Step(1000); restored.Step(1000);
        Assert(SaveCodec.ToJson(migrated.State) == SaveCodec.ToJson(restored.State), "Migrated save continued differently after another reload.");
    }

    private static void SustainableColony()
    {
        var sim = GameSimulation.CreateDefault(2026); var p = sim.FindPlanet(1);
        // Deliberately documented starting fixture proves both building resources are replenished locally.
        p.Buildings[0].Inventory[(int)ResourceKind.Parts] = 20;
        int[] initial = { 60, 0, 100, 20, 0, 0 };
        Build(sim, BuildingKind.SolarPlant, 10, 5);
        Build(sim, BuildingKind.Farm, 9, 7);
        Build(sim, BuildingKind.Mine, 4, 4);
        Build(sim, BuildingKind.Smelter, 6, 5);
        Build(sim, BuildingKind.Workshop, 11, 5);
        Build(sim, BuildingKind.Housing, 7, 7);
        Build(sim, BuildingKind.Farm, 9, 7, 2); Build(sim, BuildingKind.SolarPlant, 10, 5, 2);
        sim.Step(3600);
        Assert(p.Buildings.TrueForAll(b => b.Built), "Initial colony did not complete after three minutes.");
        long startTick = sim.State.Tick;
        for (int i = 0; i < 24000; i++)
        {
            sim.Step();
            Assert(p.FoodShortageSeconds == 0 && p.Productivity == 1, "Colony starved during the 20-minute run.");
            if (i % 1000 == 0) SaveCodec.Validate(sim.State);
        }
        Assert(sim.State.Tick - startTick == 24000 && p.FoodConsumedTotal > 60 && p.FoodProducedTotal > 60,
            "Run did not last 20 simulated minutes or replace starting rations.");
        Assert(p.Population == 28 && p.WorkersTotal == 21, "Fed population did not grow to its housing limit.");
        for (int x = 12; x <= 15; x++) Build(sim, BuildingKind.Warehouse, x, 5);
        sim.Step(1800);
        Assert(p.Buildings.TrueForAll(b => b.Built) && p.Buildings.FindAll(b => b.Kind == BuildingKind.Warehouse).Count == 4,
            "Locally financed expansion failed.");
        int totalMetalCost = 0, totalPartsCost = 0;
        foreach (var building in p.Buildings)
        { totalMetalCost += Definitions.Get(building.Kind).Cost[2]; totalPartsCost += Definitions.Get(building.Kind).Cost[3]; }
        Assert(totalMetalCost > initial[2] && totalPartsCost > initial[3], "Expansion did not require both locally produced construction goods.");
        for (int r = 0; r < 6; r++)
            Assert(ActualAmount(p, (ResourceKind)r) == initial[r] + p.ProducedTotals[r] + p.RecoveredTotals[r] - p.ConsumedTotals[r],
                "Material conservation failed for " + (ResourceKind)r + ".");
        SaveCodec.Validate(sim.State);
        Console.WriteLine("  20-minute proof: population=" + p.Population + ", food produced/consumed=" +
            p.FoodProducedTotal + "/" + p.FoodConsumedTotal + ", constructed metal/parts=" + totalMetalCost + "/" + totalPartsCost + ".");
    }
}
