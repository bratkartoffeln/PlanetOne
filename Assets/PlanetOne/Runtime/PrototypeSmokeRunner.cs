using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    /// <summary>Explicit graphics-enabled player smoke test, never installed during normal play.</summary>
    public sealed class PrototypeSmokeRunner : MonoBehaviour
    {
        private string outputDirectory;
        private string captureFailure;
        private readonly List<string> results = new List<string>();
        private PrototypeController controller;

        public static bool IsRequested
        {
            get
            {
                foreach (string argument in Environment.GetCommandLineArgs())
                    if (string.Equals(argument, "-planetOneSmoke", StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
        }

        private IEnumerator Start()
        {
            if (!IsRequested) { Destroy(this); yield break; }
            Exception setupFailure = null;
            try
            {
                outputDirectory = ReadOutputPath();
                Directory.CreateDirectory(outputDirectory);
                controller = GetComponent<PrototypeController>();
                Require(controller != null && controller.Simulation != null, "Controller and simulation initialized");
                controller.ConfigureSmokeSavePath(Path.Combine(outputDirectory, "smoke-save.json"));
                Require(!string.Equals(controller.ActiveSavePath, PrototypeController.SavePath, StringComparison.OrdinalIgnoreCase), "Smoke uses separate storage");
            }
            catch (Exception exception) { setupFailure = exception; }
            if (setupFailure != null) { Complete(false, setupFailure); yield break; }

            IEnumerator run = RunChecks();
            while (true)
            {
                bool hasNext = false;
                Exception failure = null;
                try { hasNext = run.MoveNext(); }
                catch (Exception exception) { failure = exception; }
                if (failure != null) { Complete(false, failure); yield break; }
                if (!hasNext) break;
                yield return run.Current;
            }
            Complete(true, null);
        }

        private IEnumerator RunChecks()
        {
            GameSimulation original = controller.Simulation;
            Require(controller.SetSimulationPaused(true), "Pause uses the validated controller command");
            long pausedTick = original.State.Tick;
            controller.SetView(0);
            yield return null;
            yield return null;
            Require(original.State.Tick == pausedTick, "Pause freezes simulation across rendered frames");
            Require(controller.SetSimulationSpeed(2) && original.State.TimeScale == 2, "Speed control submits a validated 2x command");
            Require(controller.SetSimulationSpeed(1) && original.State.TimeScale == 1 && original.State.Tick == pausedTick, "Returning to 1x preserves the paused simulation tick");
            yield return Capture("space.png");
            Require(captureFailure == null, captureFailure ?? "System screenshot written");

            controller.SetView(1);
            yield return null;
            Require(ReferenceEquals(original, controller.Simulation), "Space and surface share one simulation instance");
            Require(controller.OpenPlanetId == 1, "Surface view opens Aster");
            PlanetState planet = original.FindPlanet(1);
            int previousBuildings = planet.Buildings.Count;
            int initialFood = GoodsOnPlanet(planet, ResourceKind.Food);
            int initialMetal = GoodsOnPlanet(planet, ResourceKind.Metal);
            int initialOreDeposit = planet.OreRemaining[4 + 4 * planet.Width];
            QueueBuild(1, BuildingKind.SolarPlant, 10, 5);
            QueueBuild(1, BuildingKind.Farm, 9, 7);
            QueueBuild(1, BuildingKind.Mine, 4, 4);
            QueueBuild(1, BuildingKind.Smelter, 6, 5);
            QueueBuild(1, BuildingKind.Workshop, 11, 5);
            QueueBuild(1, BuildingKind.Housing, 7, 7);
            QueueBuild(2, BuildingKind.SolarPlant, 10, 5);
            QueueBuild(2, BuildingKind.Farm, 9, 7);
            Require(planet.Buildings.Count == previousBuildings, "Paused build commands do not execute early");
            yield return null;
            Require(original.State.Tick == pausedTick, "Queued orders preserve pause");

            AdvanceSeconds(180);
            Require(CompletedBuildings(planet) == previousBuildings + 6, "Basic food and industry chain plus housing built through real deliveries");
            Require(planet.Population > 16 && planet.HousingCapacity == 28, "Housing permits population and workforce growth");
            BuildingState farm = FindBuilding(planet, BuildingKind.Farm);
            Require(controller.SelectBuilding(farm.Id) && controller.SelectedBuildingId == farm.Id, "Building selection opens the economy inspector");
            Require(controller.RequestBuildingOperation(1, farm.Id, CommandKind.SetBuildingPriority, 2), "Priority uses the controller command adapter");
            AdvanceSeconds(0.05);
            Require(farm.Priority == 2, "Farm receives high operating and supply priority");
            Require(controller.RequestBuildingOperation(1, farm.Id, CommandKind.SetBuildingEnabled, 0, false), "Pause building operation is accepted");
            AdvanceSeconds(0.05);
            Require(!farm.Enabled, "Building production can be paused independently");
            yield return null;
            yield return Capture("surface-details.png");
            Require(captureFailure == null, captureFailure ?? "Building inspector screenshot written");
            Require(controller.RequestBuildingOperation(1, farm.Id, CommandKind.SetBuildingEnabled, 0, true), "Resume building operation is accepted");
            AdvanceSeconds(0.05);
            Require(farm.Enabled, "Building production resumes through a validated command");

            // Time advances in the shared core, without stock changes or a second gameplay simulation.
            int consumedAtStart = planet.FoodConsumedTotal;
            int producedAtStart = planet.FoodProducedTotal;
            long economyStartTick = original.State.Tick;
            for (int minute = 0; minute < 20; minute++)
            {
                AdvanceSeconds(60);
                if (planet.FoodShortageSeconds > 0 || planet.Productivity < 0.999)
                    throw new InvalidOperationException("Food supply failed in economy minute " + (minute + 1) + ".");
                yield return null;
            }
            Require(original.State.Tick - economyStartTick == 24000, "Colony runs for twenty additional simulation minutes");
            Require(planet.FoodConsumedTotal - consumedAtStart > initialFood, "Ongoing food consumption exceeds the complete starting ration stock");
            Require(planet.FoodProducedTotal > producedAtStart && GoodsOnPlanet(planet, ResourceKind.Food) > 0, "Local hydroponics replaces consumed food without imports or injections");
            Require(planet.Population == 28 && planet.WorkersTotal > 12, "Population reaches housing capacity and supplies additional workers");
            Require(GoodsOnPlanet(planet, ResourceKind.Ore) > 0 && GoodsOnPlanet(planet, ResourceKind.Metal) > 0 && GoodsOnPlanet(planet, ResourceKind.Parts) > 0,
                "Ore, metal and parts are present in the locally supplied economy");
            Require(planet.OreRemaining[4 + 4 * planet.Width] > 0 && planet.OreRemaining[4 + 4 * planet.Width] < initialOreDeposit,
                "Finite ore deposit is reduced by real mining without being reset");

            // Expansion drains full factory output buffers and costs more metal than the initial endowment.
            for (int x = 12; x <= 15; x++) QueueBuild(1, BuildingKind.Warehouse, x, 5);
            AdvanceSeconds(90);
            Require(CompletedBuildings(planet) == previousBuildings + 10, "Locally supplied economy completes four additional warehouses");
            Require(BuildingMaterialCost(planet, ResourceKind.Metal) > initialMetal,
                "Completed expansion uses more metal than the starting stock and is funded by local production");
            controller.SetView(1);
            yield return null;
            Require(controller.RenderedBuildingCount == planet.Buildings.Count, "Advanced economy renders every current building");
            Require(controller.RenderedShipmentCount == planet.Shipments.Count, "Visible couriers match the shared simulation");
            yield return Capture("surface.png");
            Require(captureFailure == null, captureFailure ?? "Surface screenshot written after simulation advance");

            // Separate disruption checks follow the uninterrupted twenty-minute economy period.
            BuildingState mine = FindBuilding(planet, BuildingKind.Mine);
            ShipmentState tracked = WaitForFreshShipment(planet, mine.Id);
            long trackedId = tracked.Id;
            int trackedAmount = tracked.Amount;
            Require(controller.RequestRoadChange(1, 4, 5, true), "Road removal uses the same controller adapter as pointer input");
            AdvanceSeconds(3);
            tracked = planet.Shipments.Find(s => s.Id == trackedId);
            Require(!planet.Roads[5 * planet.Width + 4], "The selected road is removed from the simulation");
            Require(tracked != null && tracked.Amount == trackedAmount, "Loaded goods stay with the interrupted courier");
            SimPoint stopped = original.GetShipmentPosition(tracked);
            AdvanceSeconds(3);
            tracked = planet.Shipments.Find(s => s.Id == trackedId);
            Require(tracked != null && tracked.Amount == trackedAmount && SamePosition(stopped, original.GetShipmentPosition(tracked)), "Blocked cargo waits without disappearing or moving through the missing road");
            yield return null;
            Require(!controller.IsRoadRendered(4, 5), "Removed road also disappears from the rendered surface");
            Require(controller.RequestRoadChange(1, 4, 5, false), "Road repair can be queued while paused");
            string expected = SaveCodec.ToJson(controller.Simulation.State);
            long saveTick = controller.Simulation.State.Tick;
            Require(controller.SaveGame(), "Runtime save adapter writes a validated file");
            Require(File.Exists(controller.ActiveSavePath), "Smoke save exists at isolated path");
            AdvanceSeconds(2);
            Require(controller.Simulation.State.Tick > saveTick && planet.Roads[5 * planet.Width + 4], "Live simulation executes pending road repair after save");
            Require(controller.LoadGame(), "Runtime load adapter restores the saved state");
            Require(SaveCodec.ToJson(controller.Simulation.State) == expected, "Save/load preserves population, cargo, production and pending road repair exactly");
            Require(controller.OpenPlanetId == 1, "Surface remains selected after loading");
            GameSimulation restored = controller.Simulation;
            planet = restored.FindPlanet(1);
            Require(!planet.Roads[5 * planet.Width + 4] && planet.Shipments.Exists(s => s.Id == trackedId && s.Amount == trackedAmount), "Load restores the interrupted road and its physical cargo");
            AdvanceSeconds(90);
            Require(planet.Roads[5 * planet.Width + 4], "Restored pending repair rebuilds the road");
            Require(!planet.Shipments.Exists(s => s.Id == trackedId && s.Amount > 0), "Previously blocked cargo is delivered after road repair");
            yield return null;
            Require(controller.IsRoadRendered(4, 5), "Repaired road is visible again");

            // Exercise cancellation and real salvage through the inspector's own command adapter.
            QueueBuild(1, BuildingKind.Warehouse, 7, 5);
            AdvanceSeconds(0.05);
            BuildingState cancelled = BuildingAt(planet, 7, 5);
            Require(cancelled != null && !cancelled.Built, "Additional construction is created as a real unfinished site");
            long cancelledId = cancelled.Id;
            Require(controller.RequestBuildingOperation(1, cancelledId, CommandKind.CancelConstruction), "Unfinished site can be cancelled through the controller");
            AdvanceSeconds(60);
            Require(!planet.Buildings.Exists(b => b.Id == cancelledId), "Cancelled site clears after its physical reservations and cargo settle");
            QueueBuild(1, BuildingKind.Warehouse, 7, 5);
            AdvanceSeconds(90);
            BuildingState warehouse = BuildingAt(planet, 7, 5);
            Require(warehouse != null && warehouse.Built, "Economy finances and completes an additional warehouse");
            int[] recovery = GameSimulation.GetRecoveryPreview(warehouse);
            Require(recovery[(int)ResourceKind.Metal] >= Definitions.Get(BuildingKind.Warehouse).Cost[(int)ResourceKind.Metal] / 2, "Demolition preview includes the stated material recovery");
            Require(controller.RequestBuildingOperation(1, warehouse.Id, CommandKind.DemolishBuilding), "Completed building can enter physical recovery through the controller");
            AdvanceSeconds(0.05);
            Require(warehouse.Deconstructing || !planet.Buildings.Contains(warehouse), "Demolished warehouse no longer operates as a normal building");

            controller.SetView(2);
            controller.SetView(0);
            controller.SetView(1);
            Require(ReferenceEquals(restored, controller.Simulation), "Repeated view switches preserve restored simulation identity");
            Require(controller.SelectBuilding(warehouse.Id), "Recovery site opens in the same inspector used during normal play");
            yield return null;
            yield return Capture("surface-restored.png");
            Require(captureFailure == null, captureFailure ?? "Restored surface screenshot written");
            controller.StartMilitaryFixture();
            Require(controller.Simulation.State.MilitaryTestFixture && controller.Simulation.State.Paused, "M3 fixture is explicitly labelled and initially paused");
            PlanetState battlefield = controller.Simulation.FindPlanet(1);
            UnitState tank = battlefield.Units.Find(u => u.OwnerId == 1 && u.Kind == UnitKind.Tank);
            Require(controller.SelectUnit(tank.Id) && controller.SelectedUnitCount == 1, "Military selection uses the controller adapter");
            yield return null;
            Require(controller.RenderedUnitCount >= 6, "Own troops are visibly rendered");
            yield return Capture("military-defense.png");
            Require(captureFailure == null, captureFailure ?? "Defense screenshot written");
            Require(controller.RequestUnitOrder(tank.Id, CommandKind.MoveUnit, 8, 2), "Reserve movement queued through normal input adapter");
            AdvanceSeconds(12);
            Require(Math.Abs(tank.X - 8) < 0.01 && Math.Abs(tank.Y - 2) < 0.01, "Mobile reserve reaches the northern approach around existing buildings");
            MilitaryFixture.OrderAttack(controller.Simulation, true);
            MilitaryFixture.OrderAttack(controller.Simulation, false);
            AdvanceSeconds(10);
            Require(battlefield.Units.Exists(u => u.Health < Definitions.Get(u.Kind).Health), "Both finite attack groups produce real combat damage");
            Require(controller.RequestUnitOrder(tank.Id, CommandKind.RetreatUnit, 5, 3), "Retreat command accepted");
            AdvanceSeconds(6);
            Require(tank.Destroyed || (tank.X == 5 && tank.Y == 3), "Retreat follows a physical route or records a real casualty");
            yield return null;
            yield return Capture("military-combat.png");
            Require(captureFailure == null, captureFailure ?? "Combat screenshot written");
            Require(controller.SaveGame(), "Military state saves through normal storage adapter");
            string militaryJson = SaveCodec.ToJson(controller.Simulation.State);
            Require(controller.LoadGame() && SaveCodec.ToJson(controller.Simulation.State) == militaryJson, "Military load preserves exact state and orders");
            yield return null;
            yield return Capture("military-restored.png");
            Require(captureFailure == null, captureFailure ?? "Restored military screenshot written");
            controller.StartSpaceFixture();
            var space = controller.Simulation;
            Require(space.State.SpaceTestFixture && space.State.Paused, "M4 demonstration uses a separate paused session");
            Require(space.HasTechnology(1, Technology.Colonization) && space.FindPlanet(1).ProducedTotals[2] > 100, "M4 ships and research were paid through the regular economy");
            var colonyShip = space.State.Ships.Find(s => s.Kind == ShipKind.ColonyShip);
            var freighter = space.State.Ships.Find(s => s.Kind == ShipKind.Freighter);
            Require(controller.SelectShip(colonyShip.Id), "Colony ship selection uses the input adapter");
            yield return null;
            Require(controller.RenderedShipCount == 2, "Both paid ships are rendered in the system");
            yield return Capture("space-fleet.png");
            Require(captureFailure == null, captureFailure ?? "Fleet screenshot written");
            Require(controller.RequestSpaceOrder(CommandKind.FlyShip, colonyShip.Id, planet: 3), "Flight to moving moon queued");
            AdvanceSeconds(2);
            controller.SetView(1); controller.SetView(2); controller.SetView(0);
            Require(ReferenceEquals(space, controller.Simulation), "Flight keeps simulation identity across surface switches");
            Require(controller.SaveGame(), "In-flight space state saves");
            string spaceJson = SaveCodec.ToJson(space.State);
            Require(controller.LoadGame() && SaveCodec.ToJson(controller.Simulation.State) == spaceJson, "In-flight space state restores exactly");
            space = controller.Simulation;
            AdvanceSeconds(15);
            Require(controller.RequestSpaceOrder(CommandKind.Colonize, colonyShip.Id, planet: 3), "Founding uses normal validated adapter");
            AdvanceSeconds(0.05);
            Require(space.FindPlanet(3).OwnerId == 1 && space.State.Ships.Find(s => s.Id == colonyShip.Id).Consumed, "Moon receives colony and consumes its original ship");
            controller.SetView(3); controller.ShowSpacePanel(0);
            yield return null;
            yield return Capture("space-colony.png");
            Require(captureFailure == null, captureFailure ?? "Founded colony screenshot written");
            QueueBuild(3, BuildingKind.Farm, 9, 7); QueueBuild(3, BuildingKind.SolarPlant, 10, 5); QueueBuild(3, BuildingKind.Spaceport, 11, 5);
            AdvanceSeconds(100);
            var sourcePort = space.FindPlanet(1).Buildings.Find(b => b.Kind == BuildingKind.Spaceport);
            var destinationPort = FindBuilding(space.FindPlanet(3), BuildingKind.Spaceport);
            Require(controller.RequestSpaceOrder(CommandKind.SetSupplyPolicy, target: sourcePort.Id, value: 2, amount: 40, minimum: 20, reserve: 10), "Port buffer and export reserve configured through controller");
            AdvanceSeconds(40);
            Require(sourcePort.Inventory[2] >= 20, "Couriers physically stock the source port");
            int moonMetal = GoodsOnPlanet(space.FindPlanet(3), ResourceKind.Metal);
            Require(controller.RequestSpaceOrder(CommandKind.AssignFreight, freighter.Id, sourcePort.Id, 3, 2, 10), "Physical freight order accepted through controller");
            AdvanceSeconds(7);
            controller.SetView(0); controller.ShowSpacePanel(2);
            yield return null;
            yield return Capture("space-logistics.png");
            Require(captureFailure == null, captureFailure ?? "Freight screenshot written");
            AdvanceSeconds(30);
            Require(GoodsOnPlanet(space.FindPlanet(3), ResourceKind.Metal) == moonMetal + 10 && destinationPort.Inventory[2] == 10, "Real cargo arrives without duplicate planetary stock");
            controller.ShowSpacePanel(1);
            yield return null;
            yield return Capture("space-research.png");
            Require(captureFailure == null, captureFailure ?? "Research screenshot written");
            Require(controller.SaveGame(), "M4 session saves after founding and delivery");
            spaceJson = SaveCodec.ToJson(space.State);
            Require(controller.LoadGame() && SaveCodec.ToJson(controller.Simulation.State) == spaceJson, "M4 final save retains colony freight research and knowledge");

            controller.StartInvasionFixture();
            var invasion=controller.Simulation;
            Require(invasion.State.InvasionTestFixture && invasion.State.Paused,"M5 finite fixture starts paused in its own session");
            var carrier=invasion.State.Ships.Find(s=>s.Kind==ShipKind.LandingTransport);
            Require(carrier.Troops.Count==4 && invasion.FindPlanet(1).Units.Count==0,"M5 four troops exist only in their carrier");
            invasion.Enqueue(new SimCommand {Id=invasion.NextCommandId(),IssuerId=1,Kind=CommandKind.SetPaused,BoolValue=false});
            controller.SelectShip(carrier.Id);controller.ShowSpacePanel(3);
            foreach(var ship in invasion.State.Ships) if(ship.OwnerId==1 && ship.Kind!=ShipKind.OrbitalYard) controller.RequestSpaceOrder(CommandKind.FlyShip,ship.Id,planet:2);
            invasion.Step(500);
            Require(invasion.State.Ships.Exists(s=>s.OwnerId==2 && s.Destroyed),"M5 real fleet weapons defeat the local corvette");
            Require(!carrier.Destroyed && carrier.Troops.Count==4,"M5 supplied transport survives escorted approach");
            yield return null;yield return Capture("invasion-orbit.png");
            controller.RequestInvasionOrder(CommandKind.LandTransport,carrier.Id,planet:2,x:4,y:9);invasion.Step(50);
            Require(carrier.TransportPhase==TransportPhase.Descending,"M5 descent has a real vulnerable transition");
            controller.SetView(2);yield return null;yield return Capture("invasion-descent.png");
            Require(controller.SaveGame(),"M5 descent saves");string descent=SaveCodec.ToJson(invasion.State);
            Require(controller.LoadGame() && SaveCodec.ToJson(controller.Simulation.State)==descent,"M5 descent restores exactly");
            invasion=controller.Simulation;carrier=invasion.State.Ships.Find(s=>s.Kind==ShipKind.LandingTransport);invasion.Step(120);
            var troopIds=new System.Collections.Generic.List<long>();foreach(var u in carrier.Troops) troopIds.Add(u.Id);
            foreach(long id in troopIds){controller.RequestInvasionOrder(CommandKind.UnloadTroop,carrier.Id,id,2);invasion.Step();}
            Require(carrier.Troops.Count==0 && invasion.FindPlanet(2).Units.FindAll(u=>u.OwnerId==1).Count==4,"M5 all transported identities are active only on destination surface");
            var ground=invasion.FindPlanet(2);var engineer=ground.Units.Find(u=>u.OwnerId==1 && u.Kind==UnitKind.Engineer);
            controller.RequestInvasionOrder(CommandKind.EstablishBeachhead,engineer.Id,planet:2,x:(int)engineer.X,y:(int)engineer.Y);invasion.Step(140);
            Require(invasion.State.Beachheads.Exists(h=>h.Progress==Definitions.BeachheadSeconds && h.Food>0),"M5 paid pioneer establishes supplied beachhead");
            var infantry=ground.Units.Find(u=>u.OwnerId==1 && u.Kind==UnitKind.Infantry);
            controller.RequestUnitOrder(infantry.Id,CommandKind.MoveUnit,7,6);invasion.Step(160);
            var hq=ground.Buildings.Find(b=>b.Kind==BuildingKind.Headquarters);
            controller.RequestInvasionOrder(CommandKind.CaptureBuilding,infantry.Id,hq.Id,2);invasion.Step(180);
            Require(ground.OwnerId==1 && ground.Units.Exists(u=>u.OwnerId==2 && !u.Destroyed),"M5 infantry and supply capture planet without deleting surviving defenders");
            controller.SelectUnit(infantry.Id);controller.ShowSpacePanel(3);yield return null;yield return Capture("invasion-capture.png");
            Require(controller.SaveGame(),"M5 occupation saves");string occupied=SaveCodec.ToJson(invasion.State);
            Require(controller.LoadGame() && SaveCodec.ToJson(controller.Simulation.State)==occupied,"M5 occupation stocks owners and troops restore exactly");
        }

        private void QueueBuild(int planetId, BuildingKind kind, int x, int y)
        {
            string invalidReason = controller.Simulation.ValidatePlacement(planetId, kind, x, y, controller.PlayerFactionId);
            if (invalidReason != null) throw new InvalidOperationException("Smoke placement invalid: " + invalidReason);
            bool accepted = controller.RequestConstruction(planetId, kind, x, y);
            if (!accepted) throw new InvalidOperationException("Smoke build command was rejected.");
        }

        private void AdvanceSeconds(double seconds)
        {
            if (!controller.SetSimulationPaused(false)) throw new InvalidOperationException("Unpause command rejected.");
            controller.Simulation.Step((int)Math.Round(seconds / Definitions.TickSeconds));
            if (!controller.SetSimulationPaused(true)) throw new InvalidOperationException("Pause command rejected.");
        }

        private ShipmentState WaitForFreshShipment(PlanetState planet, long sourceId)
        {
            controller.SetSimulationPaused(false);
            ShipmentState found = null;
            for (int tick = 0; tick < 6000 && found == null; tick++)
            {
                controller.Simulation.Step();
                found = planet.Shipments.Find(s => s.SourceId == sourceId && s.Amount > 0 && s.Phase == ShipmentPhase.InTransit && s.DistanceTravelled < 0.01);
            }
            controller.SetSimulationPaused(true);
            if (found == null) throw new InvalidOperationException("No fresh loaded mine courier appeared for the interruption check.");
            return found;
        }

        private static int GoodsOnPlanet(PlanetState planet, ResourceKind resource)
        {
            int amount = 0;
            foreach (BuildingState building in planet.Buildings) amount += building.Inventory[(int)resource];
            foreach (ShipmentState shipment in planet.Shipments) if (shipment.Resource == resource) amount += shipment.Amount;
            return amount;
        }

        private static int CompletedBuildings(PlanetState planet)
        {
            int amount = 0;
            foreach (BuildingState building in planet.Buildings) if (building.Built) amount++;
            return amount;
        }

        private static int BuildingMaterialCost(PlanetState planet, ResourceKind resource)
        {
            int amount = 0;
            foreach (BuildingState building in planet.Buildings)
                if (building.Built) amount += Definitions.Get(building.Kind).Cost[(int)resource];
            return amount;
        }

        private static BuildingState FindBuilding(PlanetState planet, BuildingKind kind)
        {
            BuildingState building = planet.Buildings.Find(b => b.Kind == kind && b.Built);
            if (building == null) throw new InvalidOperationException("Missing completed building " + kind + ".");
            return building;
        }

        private static BuildingState BuildingAt(PlanetState planet, int x, int y)
        {
            return planet.Buildings.Find(b => b.X == x && b.Y == y);
        }

        private static bool SamePosition(SimPoint a, SimPoint b)
        {
            return Math.Abs(a.X - b.X) < 0.001 && Math.Abs(a.Y - b.Y) < 0.001;
        }

        private IEnumerator Capture(string fileName)
        {
            captureFailure = null;
            string path = Path.Combine(outputDirectory, fileName);
            yield return new WaitForEndOfFrame();
            try
            {
                if (File.Exists(path)) File.Delete(path);
                ScreenCapture.CaptureScreenshot(path);
            }
            catch (Exception exception) { captureFailure = "Screenshot scheduling failed: " + exception.Message; }
            if (captureFailure != null) yield break;
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!ScreenshotExists(path))
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    captureFailure = "Screenshot timeout: " + path;
                    yield break;
                }
                yield return null;
            }
        }

        private static bool ScreenshotExists(string path)
        {
            try { return File.Exists(path) && new FileInfo(path).Length > 256; }
            catch (IOException) { return false; }
        }

        private void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            results.Add("PASS " + message);
            Debug.Log("PlanetOne smoke: PASS " + message);
        }

        private void Complete(bool success, Exception exception)
        {
            results.Insert(0, success ? "PASS PlanetOne graphics smoke" : "FAIL PlanetOne graphics smoke");
            if (exception != null) results.Add(exception.ToString());
            results.Add("This is a programmatic render/integration check, not a complete manual usability test.");
            try
            {
                if (!string.IsNullOrEmpty(outputDirectory)) File.WriteAllLines(Path.Combine(outputDirectory, "smoke-result.txt"), results.ToArray());
            }
            catch (Exception reportFailure) { Debug.LogException(reportFailure); success = false; }
            if (success) Debug.Log("PlanetOne smoke completed successfully.");
            else Debug.LogError("PlanetOne smoke failed: " + exception);
            Application.Quit(success ? 0 : 1);
        }

        private static string ReadOutputPath()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (!string.Equals(arguments[i], "-smokeOutput", StringComparison.OrdinalIgnoreCase)) continue;
                if (i + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[i + 1]))
                    throw new ArgumentException("-smokeOutput needs a directory path.");
                return Path.GetFullPath(arguments[i + 1]);
            }
            return Path.Combine(Application.persistentDataPath, "Smoke", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        }
    }
}
