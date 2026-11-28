using System;
using System.Collections.Generic;
using System.IO;
using PlanetOne.Core;

internal static class ConstructionVehicleTests
{
    public static void Register(Action<string, Action> run)
    {
        run("Construction waits for a real vehicle after paid material delivery", PhysicalBuilder);
        run("Construction shares four couriers without starving material transport", SharedPool);
        run("A working construction vehicle keeps its position while a commanded tank passes", WorkingBuilderStays);
        run("Construction vehicle interruption preserves its position and save continuation", InterruptedJourney);
        run("A builder at a disconnected site stops work until road recovery", InterruptedWork);
        run("Cancelling construction returns the same builder and paid recovery physically", CancelWork);
        run("Cancelling an outbound builder preserves all unconsumed material", CancelJourney);
        run("Construction ownership change recalls the real builder without cargo", OwnershipChange);
        run("Destroyed construction recalls its builder without refund and waits for a real return route", DestroyedSite);
        run("Construction snapshots reject phantom cargo and duplicate staffing", InvalidBuilderSaves);
    }

    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    private static void Wait(GameSimulation sim, Func<bool> condition, int ticks = 2000)
    { for (int i = 0; i < ticks && !condition(); i++) sim.Step(); Check(condition(), "Expected construction state did not occur."); }

    private static void Command(GameSimulation sim, CommandKind kind, long target = 0, int x = 0, int y = 0)
    {
        Check(sim.Enqueue(new SimCommand { Id = sim.NextCommandId(), IssuerId = 1, PlanetId = 1,
            Kind = kind, TargetId = target, X = x, Y = y }), "Construction command rejected at enqueue.");
        sim.Step();
        Check(sim.State.CommandResults[sim.State.CommandResults.Count - 1].Success, "Construction command failed.");
    }

    // Explicit fixture: material was moved from the real headquarters stock to the site.
    // No resources or vehicle capacity are added by these focused tests.
    private static BuildingState PaidSite(GameSimulation sim, int x = 14)
    {
        var p = sim.FindPlanet(1); var cost = Definitions.Get(BuildingKind.SolarPlant).Cost;
        var site = new BuildingState { Id = sim.State.NextEntityId++, PlanetId = 1, Kind = BuildingKind.SolarPlant,
            X = x, Y = 5, Inventory = (int[])cost.Clone() };
        for (int r = 0; r < 6; r++) p.Buildings[0].Inventory[r] -= cost[r];
        p.Buildings.Add(site); return site;
    }

    private static ShipmentState Builder(PlanetState p, long site)
    { return p.Shipments.Find(s => s.TargetId == site && GameSimulation.IsConstructionVehicle(s)); }

    private static void Balance(PlanetState p, int metal = 100, int parts = 40)
    {
        foreach (int r in new[] { 2, 3 })
        {
            int actual = p.ConsumedTotals[r] - p.RecoveredTotals[r] + p.LostTotals[r];
            foreach (var b in p.Buildings) actual += b.Inventory[r];
            foreach (var s in p.Shipments)
                if ((int)s.Resource == r && s.Phase != ShipmentPhase.Loading) actual += s.Amount;
            Check(actual == (r == 2 ? metal : parts), "Construction duplicated or lost paid material.");
        }
    }

    private static void PhysicalBuilder()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1);
        Check(sim.Enqueue(new SimCommand { Id = sim.NextCommandId(), Kind = CommandKind.PlaceBuilding,
            PlanetId = 1, Building = BuildingKind.SolarPlant, X = 14, Y = 5 }), "Build not queued.");
        sim.Step(); var site = p.Buildings[1];
        Wait(sim, () => Builder(p, site.Id) != null);
        var builder = Builder(p, site.Id); long id = builder.Id;
        Check(builder.Phase == ShipmentPhase.BuilderOutbound && builder.Amount == 0 && !site.ConstructionStarted &&
            site.BuildProgress == 0 && site.Inventory[2] == 8 && p.ConsumedTotals[2] == 0,
            "Builder started without its actual arrival or consumed material twice.");
        Wait(sim, () => builder.Phase == ShipmentPhase.Building);
        Check(!site.ConstructionStarted && site.BuildProgress == 0, "Travel time counted as construction work.");
        long arrived = sim.State.Tick; sim.Step();
        Check(site.ConstructionStarted && site.BuildProgress > 0 && site.Inventory[2] == 0 && p.ConsumedTotals[2] == 8,
            "Arrived builder did not consume paid material once.");
        Wait(sim, () => site.Built);
        Check((sim.State.Tick - arrived) * Definitions.TickSeconds + 1e-8 >= Definitions.Get(site.Kind).BuildSeconds &&
            builder.Id == id && builder.Phase == ShipmentPhase.BuilderReturning && p.Shipments.Contains(builder),
            "Construction duration or physical return was skipped.");
        Wait(sim, () => !p.Shipments.Contains(builder)); Balance(p); SaveCodec.Validate(sim.State);
    }

    private static void WorkingBuilderStays()
    {
        WorkingBuilderStays(false);
        WorkingBuilderStays(true);
    }
    private static void WorkingBuilderStays(bool narrowCorridor)
    {
        var sim=GameSimulation.CreateDefault();var p=sim.FindPlanet(1);var site=PaidSite(sim);
        Wait(sim,()=>site.BuildProgress>0);var builder=Builder(p,site.Id);
        var start=GameSimulation.GroundCourierPosition(builder);long id=builder.Id;
        if(narrowCorridor)
            for(int y=0;y<p.Height;y++)for(int x=0;x<p.Width;x++)
                if(y!=6&&!p.Buildings.Exists(b=>b.X==x&&b.Y==y))
                {p.Terrain[y*p.Width+x]=1;p.Roads[y*p.Width+x]=false;}
        var tank=InvasionFixture.AddUnit(sim,1,1,UnitKind.Tank,12,6);
        Check(sim.Enqueue(new SimCommand{Id=sim.NextCommandId(),IssuerId=1,PlanetId=1,ActorId=tank.Id,Kind=CommandKind.MoveUnit,X=16,Y=6}),"Passing order was rejected");
        bool passed=false;int workingTicks=0;
        for(int i=0;i<600&&!site.Built;i++)
        {
            sim.Step();SaveCodec.Validate(sim.State);
            if(builder.Phase!=ShipmentPhase.Building)continue;
            var at=GameSimulation.GroundCourierPosition(builder);workingTicks++;
            Check(builder.Id==id&&!builder.Traffic.Yielding&&Math.Abs(at.X-start.X)<1e-9&&Math.Abs(at.Y-start.Y)<1e-9,"Working builder moved aside for a passing unit");
            passed|=tank.Order==UnitOrder.Hold&&HexGrid.Distance(tank.X,tank.Y,16,6)<.01;
        }
        Check(workingTicks>20,"Worksite was not observed long enough");
        if(narrowCorridor)
        {
            Check(!passed,"Tank passed through the working builder without a bypass");
            Check(site.Built,"A waiting vehicle interrupted the active construction");
            Balance(p);return;
        }
        Check(passed,"Tank did not use the available detour around the active worksite");
        Wait(sim,()=>site.Built);Wait(sim,()=>!p.Shipments.Contains(builder));Balance(p);
    }
    private static void SharedPool()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1); var site = PaidSite(sim, 10);
        // Four already returning empty couriers occupy the existing pool until reaching HQ.
        for (int i = 0; i < Definitions.CouriersPerColony; i++)
            p.Shipments.Add(new ShipmentState { Id = sim.State.NextEntityId++, SourceId = p.Buildings[0].Id,
                TargetId = site.Id, Phase = ShipmentPhase.Returning,
                DistanceTravelled=i*.65,
                Path = new List<GridCell> { new GridCell(10,5), new GridCell(10,6), new GridCell(9,6), new GridCell(8,6) } });
        sim.Step(5);
        Check(Builder(p, site.Id) == null && site.BuildProgress == 0 && site.Inventory[2] == 8,
            "A fifth phantom vehicle was spawned or construction advanced without one.");
        Wait(sim, () => Builder(p, site.Id) != null);
        PaidSite(sim, 11); PaidSite(sim, 12); PaidSite(sim, 13);
        Wait(sim,()=>p.Shipments.FindAll(GameSimulation.IsConstructionVehicle).Count==2);
        Check(p.Shipments.FindAll(GameSimulation.IsConstructionVehicle).Count == 2 && p.Shipments.Count <= 4,
            "Construction consumed the two reserved logistics slots.");
        Wait(sim, () => p.Buildings.TrueForAll(b => b.Built));
        Wait(sim, () => p.Shipments.Count == 0); Balance(p); SaveCodec.Validate(sim.State);
    }

    private static void InterruptedJourney()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1); var site = PaidSite(sim);
        sim.Step(3); var builder = Builder(p, site.Id);
        Check(builder != null && builder.Phase == ShipmentPhase.BuilderOutbound, "Missing outbound builder.");
        Command(sim, CommandKind.RemoveRoad, x: 13, y: 6);
        Check(builder.RouteBlocked && site.BuildProgress == 0, "Builder ignored missing road.");
        var position = sim.GetShipmentPosition(builder); sim.Step(50);
        var stopped = sim.GetShipmentPosition(builder);
        Check(Math.Abs(position.X - stopped.X) < 1e-9 && Math.Abs(position.Y - stopped.Y) < 1e-9,
            "Blocked builder teleported or moved off its actual road.");
        var restored = new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(sim.State)));
        Command(sim, CommandKind.PlaceRoad, x: 13, y: 6); Command(restored, CommandKind.PlaceRoad, x: 13, y: 6);
        sim.Step(700); restored.Step(700);
        Check(site.Built && SaveCodec.ToJson(sim.State) == SaveCodec.ToJson(restored.State),
            "Saved blocked builder did not continue exactly after reconnection.");
        Balance(p);
    }

    private static void InterruptedWork()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1); var site = PaidSite(sim);
        Wait(sim, () => site.BuildProgress > 0); var builder = Builder(p, site.Id);
        Command(sim, CommandKind.RemoveRoad, x: 13, y: 6); double progress = site.BuildProgress;
        sim.Step(200);
        Check(site.BuildProgress == progress && builder.Phase == ShipmentPhase.Building && !site.Built,
            "Disconnected construction continued without its supply connection.");
        Command(sim, CommandKind.PlaceRoad, x: 13, y: 6); Wait(sim, () => site.Built);
        Balance(p); SaveCodec.Validate(sim.State);
    }

    private static void CancelWork()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1); var site = PaidSite(sim);
        Wait(sim, () => site.BuildProgress > 0); var builder = Builder(p, site.Id); long id = builder.Id;
        Command(sim, CommandKind.CancelConstruction, site.Id);
        Check(builder.Id == id && builder.Phase == ShipmentPhase.BuilderReturning && builder.Amount == 0 &&
            site.Inventory[2] == 4 && site.Inventory[3] == 1, "Cancellation skipped builder return or wrong paid recovery.");
        var restored = new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(sim.State)));
        sim.Step(700); restored.Step(700);
        Check(!p.Buildings.Contains(site) && !p.Shipments.Contains(builder) &&
            p.Buildings[0].Inventory[2] == 96 && p.Buildings[0].Inventory[3] == 39 &&
            SaveCodec.ToJson(sim.State) == SaveCodec.ToJson(restored.State), "Physical recovery or save continuation failed.");
        Balance(p);
    }

    private static void CancelJourney()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1); var site = PaidSite(sim);
        sim.Step(4); var builder = Builder(p, site.Id); var before = sim.GetShipmentPosition(builder);
        Command(sim, CommandKind.CancelConstruction, site.Id); var after = sim.GetShipmentPosition(builder);
        Check(builder.Phase == ShipmentPhase.BuilderReturning && !site.ConstructionStarted && site.Inventory[2] == 8 &&
              Math.Sqrt((before.X-after.X)*(before.X-after.X)+(before.Y-after.Y)*(before.Y-after.Y)) <= Definitions.CourierSpeed * Definitions.TickSeconds + 1e-8,
            "Cancellation teleported the builder or consumed undelivered work material.");
        Wait(sim, () => !p.Buildings.Contains(site));
        Check(p.Buildings[0].Inventory[2] == 100 && p.Buildings[0].Inventory[3] == 40, "Unstarted cancellation lost material.");
        Balance(p); SaveCodec.Validate(sim.State);
    }

    private static void OwnershipChange()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1); var site = PaidSite(sim);
        Wait(sim, () => site.BuildProgress > 0); var builder = Builder(p, site.Id); double progress = site.BuildProgress;
        // Isolate the already validated occupation result; the regular invasion tests exercise capture commands.
        site.OwnerId = 2; sim.Step();
        Check(builder.Phase == ShipmentPhase.BuilderReturning && site.BuildProgress == progress,
            "Builder kept working for a captured foreign site.");
        Wait(sim, () => !p.Shipments.Contains(builder)); Balance(p); SaveCodec.Validate(sim.State);
    }

    private static void InvalidBuilderSaves()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1); var site = PaidSite(sim);
        sim.Step(); var builder = Builder(p, site.Id); builder.Amount = 1;
        Reject(() => SaveCodec.ToJson(sim.State)); builder.Amount = 0;
        p.Shipments.Add(new ShipmentState { Id = sim.State.NextEntityId++, SourceId = builder.SourceId,
            TargetId = builder.TargetId, Phase = builder.Phase, Path = new List<GridCell>(builder.Path) });
        Reject(() => SaveCodec.ToJson(sim.State));
    }

    private static void DestroyedSite()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1); var site = PaidSite(sim);
        Wait(sim, () => site.BuildProgress > 0); var builder = Builder(p, site.Id);
        sim.State.MilitaryTestFixture = true; sim.State.Relations[0].Relation = RelationKind.War;
        var definition = Definitions.Get(UnitKind.Tank);
        var attacker = new UnitState { Id = sim.State.NextEntityId++, Kind = UnitKind.Tank, OwnerId = 2,
            X = 14, Y = 4, GoalX = 14, GoalY = 4, Health = definition.Health, Ammunition = definition.Ammo, Food = 2 };
        p.Units.Add(attacker); site.Health = 1;
        Check(sim.Enqueue(new SimCommand { Id = sim.NextCommandId(), IssuerId = 2, PlanetId = 1,
            Kind = CommandKind.AttackTarget, ActorId = attacker.Id, TargetId = site.Id }), "Fixture attack was not queued.");
        sim.Step();
        Check(site.Destroyed && builder.Phase == ShipmentPhase.BuilderReturning &&
            p.RecoveredTotals[2] == 0 && p.RecoveredTotals[3] == 0, "Destruction refunded a building or removed its builder.");
        Command(sim, CommandKind.RemoveRoad, x: 13, y: 6);
        Check(builder.RouteBlocked, "Returning builder ignored its broken road.");
        double distance = builder.DistanceTravelled; sim.Step(80);
        Check(builder.DistanceTravelled == distance && p.Buildings.Contains(site),
            "Blocked return teleported the builder or orphaned its job reference.");
        SaveCodec.Validate(sim.State);
        Command(sim, CommandKind.PlaceRoad, x: 13, y: 6);
        Wait(sim, () => !p.Shipments.Contains(builder));
        Check(!p.Buildings.Contains(site), "Destroyed site was not removed after actual vehicle return.");
        Balance(p); SaveCodec.Validate(sim.State);
    }

    private static void Reject(Action action)
    { bool rejected = false; try { action(); } catch (InvalidDataException) { rejected = true; } Check(rejected, "Invalid construction snapshot was accepted."); }
}
