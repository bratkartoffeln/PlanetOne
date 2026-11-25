using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    /// <summary>Explicit graphics-enabled player smoke test, never installed during normal play.</summary>
    public sealed partial class PrototypeSmokeRunner : MonoBehaviour
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
            Application.runInBackground=true;
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

            IEnumerator run = Array.IndexOf(Environment.GetCommandLineArgs(),"-sessionUiOnly")>=0?RunSessionChecks():Array.IndexOf(Environment.GetCommandLineArgs(),"-commandUiOnly")>=0?RunCommandChecks():RunChecks();
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
            string presentationPath=Path.Combine(outputDirectory,"presentation-test.json");
            controller.ConfigurePresentationTestPath(presentationPath);
            string unchanged=SaveCodec.ToJson(original.State);
            foreach(InterfaceLayout layout in Enum.GetValues(typeof(InterfaceLayout)))
            {
                Require(controller.SetInterfaceLayout(layout),"Layout switches: "+layout);
                controller.ReloadPresentationSettings();
                Require(controller.CurrentLayout==layout,"Layout survives settings reload: "+layout);
                controller.SetView(1);yield return null;
                controller.OpenPresentationSection(1);yield return null;
                var center=controller.SurfaceScreenPoint(8,6);
                var cell=controller.SurfaceCellAtScreen(center);
                Require(cell.X==8 && cell.Y==6,"Layout camera preserves hex picking: "+layout);
                Require(!controller.IsWorldPoint(new Vector2(20,Screen.height-20)),"Header never sends a world command: "+layout);
                controller.SetSettingsVisible(true);
                Require(!controller.IsWorldPoint(controller.WorldViewport.center),"Settings block world input: "+layout);
                controller.SetSettingsVisible(false);
                Require(SaveCodec.ToJson(original.State)==unchanged,"Layout preserves complete simulation state: "+layout);
                yield return Capture("layout-"+layout+"-surface.png");
                Require(captureFailure==null,captureFailure??("Surface frame captured: "+layout));
                controller.SetView(0);yield return null;
                Require(controller.SelectPlanet(1) && controller.OpenPlanetId==0 && controller.SelectedPlanetId==1,"Planet selection keeps the system open: "+layout);
                if(layout!=InterfaceLayout.CommandCenter)
                {
                    Require(controller.IsContextCardOpen && !controller.IsWorldPoint(controller.ContextCardScreenRect.center),"Compact planet card blocks world input: "+layout);
                    var covered=controller.ContextCardScreenRect.center;
                    controller.CloseContextPanel();
                    Require(controller.IsWorldPoint(covered),"Closing the selection card releases the world: "+layout);
                    controller.SelectPlanet(1);
                }
                yield return Capture("layout-"+layout+"-space.png");
                Require(captureFailure==null,captureFailure??("System frame captured: "+layout));
                Require(controller.OpenSelectedPlanet() && controller.OpenPlanetId==1,"Surface action opens the selected world: "+layout);
                for(int section=0;section<=6;section++){controller.OpenPresentationSection(section);yield return null;}
                Require(SaveCodec.ToJson(original.State)==unchanged,"Selection and all management sections preserve simulation: "+layout);
            }
            File.WriteAllText(presentationPath,"{\"Layout\":999,\"Scale\":0}");controller.ReloadPresentationSettings();
            Require(controller.CurrentLayout==InterfaceLayout.CommandCenter && controller.WorldViewport.width>0,"Invalid preferences recover to a usable layout");
            controller.SetView(0);controller.ResetCameraView();yield return null;
            string beforeCamera=SaveCodec.ToJson(original.State);
            var background=controller.SpaceBackdropCamera;var stars=controller.BackgroundStarPositions;
            int nearStar=-1,farStar=-1;float nearDepth=float.MaxValue,farDepth=0;
            for(int i=0;i<stars.Length;i++)
            {
                var projected=background.WorldToScreenPoint(stars[i]);
                if(projected.z<=0 || !background.pixelRect.Contains(projected))continue;
                if(projected.z<nearDepth){nearStar=i;nearDepth=projected.z;}
                if(projected.z>farDepth){farStar=i;farDepth=projected.z;}
            }
            Require(nearStar>=0 && farStar>=0 && farDepth>nearDepth*4,"Visible background stars occupy genuinely different distances");
            Vector2 nearBefore=background.WorldToScreenPoint(stars[nearStar]),farBefore=background.WorldToScreenPoint(stars[farStar]);
            Vector2 sunBefore=Camera.main.WorldToScreenPoint(Vector3.zero);
            controller.SetSpaceFocus(new Vector3(10,0,0));
            float nearShift=Vector2.Distance(nearBefore,background.WorldToScreenPoint(stars[nearStar]));
            float farShift=Vector2.Distance(farBefore,background.WorldToScreenPoint(stars[farStar]));
            float worldShift=Vector2.Distance(sunBefore,Camera.main.WorldToScreenPoint(Vector3.zero));
            Require(nearShift>farShift*3 && farShift>0.01f && nearShift<worldShift*0.1f,"Parallax is subtle and decreases with actual star depth");
            controller.ResetCameraView();Vector2 starBeforeTurn=background.WorldToScreenPoint(stars[nearStar]);
            controller.SetOrbitCamera(22,50);
            Require(Vector2.Distance(starBeforeTurn,background.WorldToScreenPoint(stars[nearStar]))>20,"World-fixed background rotates with the camera view");
            yield return Capture("space-background-rotated.png");Require(captureFailure==null,captureFailure??"Rotated background captured");
            controller.ResetCameraView();
            foreach(InterfaceLayout layout in Enum.GetValues(typeof(InterfaceLayout)))
            {
                controller.SetInterfaceLayout(layout);controller.CloseContextPanel();var start=controller.WorldViewport.center;
                Require(!controller.BeginSpacePan(new Vector2(20,Screen.height-20)),"HUD cannot start a camera drag: "+layout);
                Require(controller.BeginSpacePan(start),"Right drag begins in the world: "+layout);
                controller.ContinueSpacePan(start+new Vector2(95,40));controller.EndSpacePan(start+new Vector2(95,40));
                Require(controller.SpaceFocus.sqrMagnitude>1 && Vector2.Distance(controller.WorldViewport.center,Camera.main.WorldToScreenPoint(Vector3.zero))>50,"System camera pans away from the central star: "+layout);
                yield return Capture("space-panned-"+layout+".png");Require(captureFailure==null,captureFailure??"Panned system captured");
                controller.ResetCameraView();Require(controller.SpaceFocus==Vector3.zero,"Home restores system focus: "+layout);
            }
            Require(SaveCodec.ToJson(original.State)==beforeCamera,"Camera panning and background rotation preserve complete simulation state");
            var spinBefore=controller.CelestialView(1).rotation;yield return null;yield return null;
            Require(Quaternion.Angle(spinBefore,controller.CelestialView(1).rotation)<0.001f,"Paused planets preserve their axial rotation");
            float spin=Quaternion.Angle(PrototypeController.PlanetAxialRotation(1,0),PrototypeController.PlanetAxialRotation(1,30));
            Require(spin>15 && spin<20,"Axial rotation is clearly visible but remains slower than the camera");
            Require(controller.HasSurfaceAtlas,"Authored material atlas loaded in the Windows player");
            controller.ExportBuildingCatalogue(outputDirectory);
            Require(File.Exists(Path.Combine(outputDirectory,"building-Headquarters.png")),"Building catalogue renders actual textured models");
            controller.SetInterfaceLayout(InterfaceLayout.CommandCenter);
            controller.SetView(0);
            controller.SetSettingsVisible(true);
            yield return Capture("presentation-settings.png");
            Require(captureFailure==null,captureFailure??"Settings frame captured");
            controller.SetSettingsVisible(false);
            controller.SetMenuVisible(true);
            Require(!controller.IsWorldPoint(controller.WorldViewport.center),"Game menu blocks world input");
            yield return Capture("game-menu.png");Require(captureFailure==null,captureFailure??"Game menu captured");
            controller.SetMenuVisible(false);
            Require(controller.SetSimulationSpeed(2) && original.State.TimeScale == 2, "Speed control submits a validated 2x command");
            Require(controller.SetSimulationSpeed(1) && original.State.TimeScale == 1 && original.State.Tick == pausedTick, "Returning to 1x preserves the paused simulation tick");
            yield return Capture("space.png");
            Require(captureFailure == null, captureFailure ?? "System screenshot written");

            controller.SetView(1);
            yield return null;
            Require(ReferenceEquals(original, controller.Simulation), "Space and surface share one simulation instance");
            Require(controller.OpenPlanetId == 1, "Surface view opens Aster");
            PlanetState planet = original.FindPlanet(1);
            int checkedCenters=0,checkedEdges=0;
            for (int y = 0; y < planet.Height; y++) for (int x = 0; x < planet.Width; x++)
            {
                if(!controller.IsSurfacePointVisible(x,y))continue;
                var picked = controller.SurfaceCellAtScreen(controller.SurfaceScreenPoint(x, y));
                if (picked.X != x || picked.Y != y) throw new InvalidOperationException("Hex camera picking mismatch at "+x+","+y+" -> "+picked.X+","+picked.Y);
                checkedCenters++;
                for (int d = 0; d < 6; d++)
                {
                    var n = HexGrid.Neighbor(x, y, d);
                    if(!controller.IsSurfacePointVisible(x+(n.X-x)*0.51,y+(n.Y-y)*0.51))continue;
                    picked = controller.SurfaceCellAtScreen(controller.SurfaceScreenPoint(x + (n.X-x)*0.51, y + (n.Y-y)*0.51));
                    if (picked.X != n.X || picked.Y != n.Y) throw new InvalidOperationException("Hex edge camera picking mismatch");
                    checkedEdges++;
                }
            }
            Require(checkedCenters>45 && checkedEdges>240,"Relief camera resolves visible hex centers and edges: "+checkedCenters+" / "+checkedEdges);
            float low=float.MaxValue,high=float.MinValue;
            for(int y=0;y<planet.Height;y++)for(int x=0;x<planet.Width;x++){float h=controller.SurfaceElevation(x,y);low=Mathf.Min(low,h);high=Mathf.Max(high,h);}
            Require(high-low>.65f,"Temperate terrain has rolling relief without a forced valley");
            float savedHeight=controller.SurfaceElevation(3,4);
            controller.SetView(0);controller.SetView(1);
            Require(Mathf.Approximately(savedHeight,controller.SurfaceElevation(3,4)),"Reopening a world preserves deterministic relief");
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
            foreach(InterfaceLayout layout in Enum.GetValues(typeof(InterfaceLayout)))
            {
                controller.SetInterfaceLayout(layout);controller.SetView(1);
                Require(controller.SelectBuilding(farm.Id),"Populated colony selection works: "+layout);
                if(layout!=InterfaceLayout.CommandCenter)Require(controller.IsContextCardOpen && !controller.IsWorldPoint(controller.ContextCardScreenRect.center),"Building opens a compact protected selection card: "+layout);
                yield return Capture("colony-"+layout+"-selection.png");Require(captureFailure==null,captureFailure??"Populated colony selection captured");
            }
            controller.SetInterfaceLayout(InterfaceLayout.CommandCenter);controller.SetView(1);
            string beforeOrbit=SaveCodec.ToJson(original.State);
            foreach(float yaw in new[]{45f,135f,225f,315f})
            {
                controller.SetOrbitCamera(yaw,38);yield return null;
                int visible=0;
                for(int y=0;y<planet.Height;y++)for(int x=0;x<planet.Width;x++)
                {
                    if(!controller.IsSurfacePointVisible(x,y))continue;
                    var cell=controller.SurfaceCellAtScreen(controller.SurfaceScreenPoint(x,y));
                    if(cell.X!=x || cell.Y!=y)throw new InvalidOperationException("Rotated relief picking mismatch at "+yaw+": "+x+","+y);
                    visible++;
                }
                Require(visible>30,"Rotated terrain selection resolves visible cells at "+yaw+" degrees: "+visible);
                yield return Capture("surface-orbit-"+yaw+".png");Require(captureFailure==null,captureFailure??"Orbit frame captured");
            }
            Require(SaveCodec.ToJson(original.State)==beforeOrbit,"Orbit and elevation changes preserve the complete simulation");
            controller.SetOrbitCamera(0,50);

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
            string beforeGroundPan=SaveCodec.ToJson(controller.Simulation.State);var groundStart=controller.WorldViewport.center;
            Require(controller.BeginSurfacePan(groundStart),"Selected ground unit permits camera drag");
            controller.ContinueSpacePan(groundStart+new Vector2(-80,30));controller.EndSpacePan(groundStart+new Vector2(-80,30));
            Require(SaveCodec.ToJson(controller.Simulation.State)==beforeGroundPan,"Ground dragging does not issue an order to selected troops");controller.ResetCameraView();
            yield return Capture("military-defense.png");
            Require(captureFailure == null, captureFailure ?? "Defense screenshot written");
            Vector2 groundTarget=controller.SurfaceScreenPoint(8,2);int pendingGround=controller.Simulation.State.PendingCommands.Count;
            Require(controller.BeginSurfacePan(groundTarget) && controller.Simulation.State.PendingCommands.Count==pendingGround,"Short right click waits until release on the surface");
            Require(controller.EndSpacePan(groundTarget) && controller.IsCommandMenuOpen && controller.Simulation.State.PendingCommands.Count==pendingGround,"Short right click opens ground commands without issuing an order");
            Require(controller.InvokeContextCommand("move-here") && controller.Simulation.State.PendingCommands.Count==pendingGround+1,"Explicit context movement queues exactly one ground order");
            Vector3 tankBefore=controller.UnitView(tank.Id).position;
            AdvanceSeconds(1);yield return null;
            var tankView=controller.UnitView(tank.Id);Vector3 tankDelta=tankView.position-tankBefore;tankDelta.y=0;
            Vector3 tankForward=tankView.forward;tankForward.y=0;
            Require(tankDelta.sqrMagnitude>0.001f && Vector3.Dot(tankForward.normalized,tankDelta.normalized)>0.99f,"Vehicle hull faces actual movement on the terrain");
            Quaternion stoppedHeading=tankView.rotation;yield return null;
            Require(Quaternion.Angle(stoppedHeading,tankView.rotation)<0.01f,"Paused vehicle preserves its heading");
            controller.SetOrbitCamera(120,42);yield return null;
            Require(Quaternion.Angle(stoppedHeading,tankView.rotation)<0.01f,"Camera rotation does not rotate the vehicle model");
            yield return Capture("military-orbit.png");Require(captureFailure==null,captureFailure??"Rotated vehicle frame captured");controller.SetOrbitCamera(0,50);
            AdvanceSeconds(11);
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
            string beforeFleetDrag=SaveCodec.ToJson(space.State);var dragStart=controller.WorldViewport.center;
            Require(controller.BeginSpacePan(dragStart),"Selected fleet permits camera dragging");
            controller.ContinueSpacePan(dragStart+new Vector2(85,30));
            Require(!controller.EndSpacePan(dragStart+new Vector2(85,30)) && SaveCodec.ToJson(space.State)==beforeFleetDrag,"Right dragging with a selected fleet never queues a flight");
            controller.ResetCameraView();
            string beforeHudOrders=SaveCodec.ToJson(space.State);
            foreach(InterfaceLayout layout in Enum.GetValues(typeof(InterfaceLayout)))
            {
                controller.SetInterfaceLayout(layout);controller.SelectShip(colonyShip.Id);controller.BeginShipMove();
                Require(!controller.RequestShipMoveAtScreen(new Vector2(20,Screen.height-20)) && controller.IsShipMovePending,"Header click cannot issue or consume a flight command: "+layout);
                if(layout!=InterfaceLayout.CommandCenter)Require(!controller.RequestShipMoveAtScreen(controller.ContextCardScreenRect.center) && controller.IsShipMovePending,"Selection card cannot issue or consume a flight command: "+layout);
                controller.SetSettingsVisible(true);
                Require(!controller.RequestShipMoveAtScreen(controller.WorldViewport.center),"Modal settings block flight orders: "+layout);
                controller.SetSettingsVisible(false);
                yield return Capture("fleet-"+layout+"-selection.png");Require(captureFailure==null,captureFailure??"Fleet selection captured");
            }
            Require(SaveCodec.ToJson(space.State)==beforeHudOrders,"Blocked HUD orders leave complete space state unchanged");
            controller.SetInterfaceLayout(InterfaceLayout.CommandCenter);
            Require(controller.RequestShipMoveAtScreen(controller.WorldViewport.center) && !controller.IsShipMovePending,"Move action accepts a world target and exits target mode");
            string beforeCancel=SaveCodec.ToJson(space.State);controller.BeginShipMove();controller.BeginSpacePan(controller.WorldViewport.center);
            Require(!controller.EndSpacePan(controller.WorldViewport.center) && !controller.IsShipMovePending && SaveCodec.ToJson(space.State)==beforeCancel,"Short right click cancels an explicit flight target without an extra order");
            Require(controller.BeginSpacePan(controller.WorldViewport.center) && controller.EndSpacePan(controller.WorldViewport.center) && controller.IsCommandMenuOpen && SaveCodec.ToJson(space.State)==beforeCancel,"Short fleet right click opens a menu without an unintended flight");
            controller.CloseCommandMenu();
            AdvanceSeconds(0.2);
            Require(colonyShip.Order==ShipOrder.MovePosition,"HUD movement reaches the shared simulation command queue");
            Require(controller.RequestSpaceOrder(CommandKind.FlyShip, colonyShip.Id, planet: 3), "Flight to moving moon queued");
            Vector3 shipBefore=controller.ShipView(colonyShip.Id).position;
            AdvanceSeconds(2);yield return null;
            var shipView=controller.ShipView(colonyShip.Id);Vector3 shipDelta=shipView.position-shipBefore;shipDelta.y=0;
            Require(shipDelta.sqrMagnitude>0.01f && Vector3.Dot(shipView.forward,shipDelta.normalized)>0.99f,"Spacecraft hull faces its actual flight direction");
            controller.SetOrbitCamera(65,35);yield return null;yield return Capture("space-orbit.png");Require(captureFailure==null,captureFailure??"Spatial fleet frame captured");controller.SetOrbitCamera(0,50);
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
            controller.StartAiScenario();var computerGame=controller.Simulation;
            Require(computerGame.State.AiScenario && computerGame.FindPlanet(2).OwnerId==2 && computerGame.State.Ships.Count==0,"M6 normal AI scenario starts with its own colony and no free fleet");
            string playerMessage=computerGame.State.LastMessage;
            computerGame.Step(400);
            Require(computerGame.State.LastMessage==playerMessage,"M6 private AI orders and construction do not leak through player notifications");
            // Run the actual AI while the player views another world; no scripted build commands.
            controller.SetView(1);
            for(int i=0;i<59;i++){computerGame.Step(400);yield return null;}
            Require(computerGame.FindPlanet(2).ProducedTotals[2]>30 && computerGame.FindPlanet(2).ProducedTotals[3]>10,"M6 AI funds its economy through real production in the background");
            Require(computerGame.State.Factions.Find(f=>f.Id==2).Research.Completed.Contains(Technology.Transport),"M6 AI researches through its paid laboratory");
            controller.SetView(0);yield return null;yield return Capture("ai-system.png");
            Require(captureFailure==null,captureFailure??"M6 system render written without revealing hidden enemy colonies");
            computerGame.State.Paused=true;
            Require(controller.SaveGame(),"M6 plans knowledge reservations and logs save");string aiSaved=SaveCodec.ToJson(computerGame.State);
            Require(controller.LoadGame()&&SaveCodec.ToJson(controller.Simulation.State)==aiSaved,"M6 restored AI state matches exactly");
            var aiCopy=new GameSimulation(SaveCodec.FromJson(aiSaved));aiCopy.State.Paused=false;controller.Simulation.State.Paused=false;
            aiCopy.Step(80);controller.Simulation.Step(80);
            Require(SaveCodec.ToJson(aiCopy.State)==SaveCodec.ToJson(controller.Simulation.State),"M6 restored AI continues with identical decisions and commands");
            controller.Simulation.State.Paused=true;controller.SetView(1);yield return null;yield return Capture("ai-player-colony.png");
            Require(captureFailure==null,captureFailure??"M6 player world remains renderable during AI operation");
            controller.StartDiplomacyScenario();var diplomacy=controller.Simulation;
            Require(diplomacy.State.Factions.Count==3&&diplomacy.State.ComputerFactions.Count==2&&!diplomacy.AtWar(1,2)&&!diplomacy.AtWar(2,3),"M7 regular scenario starts with three peaceful independent factions");
            Require(diplomacy.State.Ships.Count==0&&diplomacy.State.Planets.FindAll(p=>p.OwnerId>0).TrueForAll(p=>p.Buildings.Count==1&&p.Buildings[0].Inventory[2]==100),"M7 normal start has equal finite stocks and no fixture fleet");
            diplomacy.Step(2400);
            Require(diplomacy.State.Treaties.Exists(t=>GameSimulation.TreatyLive(t)&&t.Terms.Members.Contains(2)&&t.Terms.Members.Contains(3)),"M7 computer factions negotiate through normal simulation commands");
            controller.StartDiplomacyFixture();diplomacy=controller.Simulation;
            Require(diplomacy.State.SpaceTestFixture&&diplomacy.State.LastMessage.Contains("TESTFIXTURE"),"M7 prepared gate assets are explicitly isolated as a test fixture");
            Require(controller.RequestDiplomacy(new SimCommand{Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms{Kind=TreatyKind.Coalition,Members=new List<int>{1,2,3}}}),"M7 controller queues coalition proposal");diplomacy.Step();
            var pact=diplomacy.State.Treaties[0];
            Require(pact.Phase==TreatyPhase.Proposed&&!diplomacy.SharesSensors(1,2),"M7 proposal alone grants no coalition rights");
            diplomacy.Enqueue(new SimCommand{Id=diplomacy.NextCommandId(),IssuerId=2,Kind=CommandKind.AcceptTreaty,TargetId=pact.Id});diplomacy.Step();
            Require(!GameSimulation.TreatyLive(pact),"M7 every coalition participant must consent");
            diplomacy.Enqueue(new SimCommand{Id=diplomacy.NextCommandId(),IssuerId=3,Kind=CommandKind.AcceptTreaty,TargetId=pact.Id});diplomacy.Step();
            Require(GameSimulation.TreatyLive(pact)&&diplomacy.SharesSensors(1,2)&&diplomacy.SharesSensors(1,3),"M7 unanimous coalition shares current sensors");
            Require(!diplomacy.HasAccess(1,2,2,AccessRight.Landing),"M7 coalition grants no unagreed landing rights");
            controller.SetView(0);yield return null;yield return Capture("diplomacy-coalition.png");
            Require(captureFailure==null,captureFailure??"M7 shared system view renders coalition worlds");
            var jumpGate=diplomacy.FindPlanet(1).Buildings.Find(b=>b.Kind==BuildingKind.JumpGate);
            Require(controller.RequestDiplomacy(new SimCommand{Kind=CommandKind.ActivateGate,TargetId=jumpGate.Id}),"M7 controller requests public gate activation");diplomacy.Step();
            Require(diplomacy.State.GateProjects.Count==1&&diplomacy.State.GateProjects[0].Activating&&diplomacy.State.GateProjects[0].Participants.Count==3,"M7 gate locks consenting coalition participants at activation start");
            diplomacy.State.Paused=true;Require(controller.SaveGame(),"M7 contracts and gate progress save");string diplomaticSave=SaveCodec.ToJson(diplomacy.State);
            Require(controller.LoadGame()&&SaveCodec.ToJson(controller.Simulation.State)==diplomaticSave,"M7 save restores treaties participants and exact remaining time");
            diplomacy=controller.Simulation;diplomacy.State.Paused=false;diplomacy.Step(Definitions.GateSecureTicks);
            Require(diplomacy.State.Winners.Count==3&&diplomacy.State.GateProjects[0].Complete,"M7 coalition completes the real securing phase and wins together");
            Require(jumpGate.Inventory[4]==8,"M7 replacing the simulation did not mutate detached old state");
            Require(diplomacy.FindPlanet(1).ConsumedTotals[4]>=8,"M7 successful activation consumes actual delivered crystals");
            diplomacy.State.Paused=true;controller.SetView(1);yield return null;yield return Capture("diplomacy-gate.png");
            Require(captureFailure==null,captureFailure??"M7 completed gate world renders in the real player");
            controller.StartPlanetEnvironmentFixture();var environments=controller.Simulation;
            Require(environments.State.SpaceTestFixture && environments.State.Paused,"Environment showcase is isolated and paused");
            controller.SetInterfaceLayout(InterfaceLayout.OpenSpace);controller.SetView(0);yield return null;
            yield return Capture("planet-types-system.png");Require(captureFailure==null,captureFailure??"Six distinct planetary bodies rendered");
            foreach(var world in environments.State.Planets) {
                controller.SelectPlanet(world.Id);controller.SetView(world.Id);controller.ResetCameraView();yield return null;
                float elevation=controller.SurfaceElevation(8,6);
                Require(!float.IsNaN(elevation) && !float.IsInfinity(elevation),"Finite environment elevation: "+world.Kind);
                var relief=GameObject.Find("Durchgehendes Relief").GetComponent<Renderer>();
                if(world.Kind!=PlanetKind.GasGiant)Require(relief.enabled && (Camera.main.cullingMask & (1<<relief.gameObject.layer))!=0 && relief.sharedMaterial.mainTexture!=null,"Textured terrain reaches the actual camera: "+world.Kind);
                else Require(!relief.enabled && GameObject.Find("Wolkendecke unter den Stationen")!=null,"Gas view has clouds below platforms instead of solid terrain");
                yield return Capture("biome-"+world.Kind+".png");Require(captureFailure==null,captureFailure??"Biome captured");
                string stateBefore=SaveCodec.ToJson(environments.State);var focus=controller.SurfaceFocus;var start=controller.WorldViewport.center;
                Require(controller.BeginSurfacePan(start),"Right drag starts on the surface: "+world.Kind);
                controller.ContinueSpacePan(start+new Vector2(-120,-35));controller.EndSpacePan(start+new Vector2(-120,-35));
                Require(Vector3.Distance(focus,controller.SurfaceFocus)>1,"Right drag moves surface focus: "+world.Kind);
                Require(SaveCodec.ToJson(environments.State)==stateBefore,"Surface dragging issues no simulation command: "+world.Kind);
                Require(!controller.BeginSurfacePan(new Vector2(8,Screen.height-8)),"HUD blocks surface drag: "+world.Kind);
                controller.SetOrbitCamera(55,55);yield return null;yield return Capture("biome-panned-"+world.Kind+".png");Require(captureFailure==null,captureFailure??"Rotated biome captured");
            }
            var atmosphere=environments.FindPlanet(7);controller.SetView(7);controller.ResetCameraView();
            QueueBuild(7,BuildingKind.Warehouse,20,16);AdvanceSeconds(1);yield return null;
            Require(atmosphere.Shipments.Exists(s=>s.Amount>0 && s.Phase==ShipmentPhase.InTransit),"Gas construction uses actual airborne cargo");
            bool wheel=false,container=false,platform=false;
            foreach(var t in controller.GetComponentsInChildren<Transform>()) {if(t.name=="Laufrad")wheel=true;if(t.name=="Frachtcontainer")container=true;if(t.name=="Schwebendes Fundament")platform=true;}
            Require(!wheel && container && platform,"Gas colony renders flying cargo and floating bases without wheels");
            yield return Capture("gas-air-freight.png");Require(captureFailure==null,captureFailure??"Air freight captured");
            Require(controller.SaveGame() && controller.LoadGame(),"Planet environments and airborne cargo load in the real player");
            controller.StartAsteroidFixture();var mining=controller.Simulation;controller.SetOrbitCamera(0,50);
            foreach(var world in mining.State.Planets)
                Require(controller.CelestialView(world.Id).localScale.x<=controller.StarView.localScale.x*.5f,"Planet radius limited to half the star radius: "+world.Name);
            Require(controller.CelestialView(3).localScale.x<controller.CelestialView(2).localScale.x*.3f,"Moon is much smaller than its parent");
            foreach(var world in mining.State.Planets)
            {var position=mining.GetOrbitPosition(world.Id);Require(Vector3.Distance(controller.CelestialView(world.Id).position,PrototypeController.SpacePosition(position.X,position.Y))<.001f,"Scaled orbit and planet position agree: "+world.Name);}
            Require(controller.CoronaSymmetry(0,0)<.03f && controller.CoronaSymmetry(6,0)<.03f && controller.CoronaSymmetry(-6,53)<.03f,"Corona stays symmetric at centre and both panned edges including rotation");
            controller.ShowSpacePanel(0);yield return null;yield return Capture("asteroid-system.png");Require(captureFailure==null,captureFailure??"Scaled system captured");
            controller.SetSpaceFocus(new Vector3(9,0,0));yield return null;yield return Capture("corona-panned.png");Require(captureFailure==null,captureFailure??"Off-centre corona captured");
            var deposit=mining.State.Asteroids[0];var miner=mining.State.Ships.Find(s=>s.Kind==ShipKind.Freighter);var miningPort=mining.FindPlanet(1).Buildings.Find(b=>b.Kind==BuildingKind.Spaceport);
            controller.SelectShip(miner.Id);controller.FocusAsteroid(deposit.Id);Require(controller.RequestAsteroidApproach(miner.Id,deposit.Id),"Asteroid approach uses ordinary ship orders");
            AdvanceSeconds(24);yield return null;
            Require(mining.IsAsteroidVisible(1,deposit),"Scouting reveals the real deposit");
            foreach(var asteroid in mining.State.Asteroids) {
                var size=controller.AsteroidView(asteroid.Id).GetComponent<Renderer>().bounds.size;
                Require(Mathf.Max(size.x,Mathf.Max(size.y,size.z))<controller.CelestialView(3).lossyScale.x*.25f,"Enlarged asteroid geometry remains below a quarter of the moon diameter: "+asteroid.Id);
            }
            Require(controller.RequestSpaceOrder(CommandKind.MineAsteroid,miner.Id,deposit.Id,1,amount:12),"Mining submitted through the common command adapter");
            AdvanceSeconds(8);yield return null;
            Require(miner.Mining!=null && miner.Cargo[1]>0 && miner.Cargo[1]<12 && miningPort.ReservedIncoming[1]==12,"Mining loads partial real cargo and reserves its return capacity");
            foreach(InterfaceLayout layout in Enum.GetValues(typeof(InterfaceLayout)))
            {controller.SetInterfaceLayout(layout);controller.SelectShip(miner.Id);controller.FocusAsteroid(deposit.Id);yield return null;yield return Capture("asteroid-mining-"+layout+".png");Require(captureFailure==null,captureFailure??"Mining panel captured");}
            string miningSave=SaveCodec.ToJson(mining.State);Require(controller.SaveGame() && controller.LoadGame() && SaveCodec.ToJson(controller.Simulation.State)==miningSave,"Active mining reloads exactly in the player");
            mining=controller.Simulation;miner=mining.State.Ships.Find(s=>s.Id==miner.Id);deposit=mining.State.Asteroids.Find(a=>a.Id==deposit.Id);
            AdvanceSeconds(50);yield return null;
            Require(miner.Mining==null && miner.Freight==null && miner.Cargo[1]==0 && mining.State.SpaceMinedOre==12 && deposit.InitialOre-deposit.RemainingOre==12,"Mined ore returns and unloads once at a working port");
            controller.SetInterfaceLayout(InterfaceLayout.CommandCenter);controller.SelectShip(miner.Id);controller.SelectAsteroid(deposit.Id);
            yield return Capture("asteroid-delivered.png");Require(captureFailure==null,captureFailure??"Mining delivery captured");
            string beforeMapInspection=SaveCodec.ToJson(mining.State);
            controller.ShowBeltOverview();yield return null;
            Require(controller.BeltDustVisibility>.95f,"Belt dust provides an overview cue");
            yield return Capture("asteroid-dust-overview.png");Require(captureFailure==null,captureFailure??"Dust overview captured");
            controller.InspectAsteroid(deposit.Id);yield return null;
            Require(controller.BeltDustVisibility<.01f,"Dust recedes on approach to reveal individual rocks");
            yield return Capture("asteroid-closeup.png");Require(captureFailure==null,captureFailure??"Individual asteroid captured");
            foreach(InterfaceLayout layout in Enum.GetValues(typeof(InterfaceLayout)))
            {
                controller.SetInterfaceLayout(layout);controller.ShowBeltOverview();yield return null;
                Vector2 starPoint=Camera.main.WorldToScreenPoint(Vector3.zero);
                Require(controller.SelectStarAtScreen(starPoint) && controller.StarSelected,"Star click selects its own information card: "+layout);
                Require(!controller.OpenSelectedPlanet() && controller.OpenPlanetId==0,"Star selection offers no planetary surface: "+layout);
                controller.SetSettingsVisible(true);Require(!controller.SelectStarAtScreen(starPoint),"Modal settings block star clicks: "+layout);controller.SetSettingsVisible(false);
                Require(!controller.SelectStarAtScreen(new Vector2(8,Screen.height-8)),"HUD clicks cannot select the star: "+layout);
                yield return Capture("star-selection-"+layout+".png");Require(captureFailure==null,captureFailure??"Star card captured");
                controller.OpenStarDetails();yield return null;yield return Capture("star-details-"+layout+".png");Require(captureFailure==null,captureFailure??"Star information captured");
                Require(controller.SelectPlanet(1) && !controller.StarSelected,"Planet selection replaces star selection: "+layout);
            }
            Require(SaveCodec.ToJson(mining.State)==beforeMapInspection,"Belt zoom and star information never mutate resources orders or time");

            controller.StartReferenceScenario();controller.SetSimulationPaused(true);
            var reference=controller.Simulation;
            Require(reference.State.Planets.Count==5 && reference.State.ComputerFactions.Count==2 && reference.State.Ships.Count==0,"F05 starts three ordinary colonies and two independent AI without free fleets");
            Require(controller.SaveGame(),"Reference game saves through the real filesystem adapter");
            string referenceJson=SaveCodec.ToJson(reference.State);
            Require(controller.SaveGame(),"Second save creates the previous valid snapshot");
            File.WriteAllText(controller.ActiveSavePath,"{damaged");
            Require(!controller.LoadGame() && ReferenceEquals(reference,controller.Simulation) && SaveCodec.ToJson(reference.State)==referenceJson,"Corrupt file leaves the complete running simulation unchanged");
            Require(controller.LoadBackupGame() && SaveCodec.ToJson(controller.Simulation.State)==referenceJson,"Explicit backup action restores the previous valid reference game");
            Require(controller.SaveGame(),"Saving after recovery keeps the last valid backup");
            Require(SaveCodec.ToJson(PlanetOne.Platform.SnapshotStore.Load(controller.ActiveSavePath+".bak"))==referenceJson,"Corrupt primary never overwrites the valid backup");
            controller.StartSpaceFixture();controller.SetView(2);
            Require(controller.OpenPlanetId==2 && !reference.IsPlanetKnown(1,2),"Fixture views a world that is unknown in the saved reference game");
            Require(controller.LoadGame() && controller.OpenPlanetId==0,"Loading an unknown former surface returns to the system without leaking old terrain");
            Require(SaveCodec.ToJson(controller.Simulation.State)==referenceJson,"View fallback does not change saved knowledge or simulation state");
            controller.SetView(1);yield return null;yield return Capture("reference-start-surface.png");Require(captureFailure==null,captureFailure??"Reference colony captured");
            controller.SetView(0);yield return null;yield return Capture("reference-start-system.png");Require(captureFailure==null,captureFailure??"Reference system captured");
            controller.SetSettingsVisible(true);
            Require(!controller.HandleShortcut(KeyCode.Space)&&controller.Simulation.State.Paused,"Settings consume pause shortcuts without changing the simulation");
            Require(!controller.HandleShortcut(KeyCode.F9),"Settings block accidental load shortcuts");
            Require(controller.HandleShortcut(KeyCode.Escape),"Escape dismisses the active modal through the keyboard adapter");
            Require(controller.HandleShortcut(KeyCode.F5),"F5 dispatches the same validated save action as the menu");
            string keyboardSave=SaveCodec.ToJson(controller.Simulation.State);
            Require(controller.HandleShortcut(KeyCode.Space)&&!controller.Simulation.State.Paused,"Space resumes through the real shortcut dispatcher");
            controller.Simulation.Step(5);controller.HandleShortcut(KeyCode.Space);
            Require(controller.HandleShortcut(KeyCode.F9)&&SaveCodec.ToJson(controller.Simulation.State)==keyboardSave,"F9 restores the exact saved state after keyboard pause and continuation");
            controller.SetView(1);var shortcutWorld=controller.Simulation;
            Require(controller.HandleShortcut(KeyCode.Tab)&&controller.OpenPlanetId==0&&ReferenceEquals(shortcutWorld,controller.Simulation),"Tab returns to the system without replacing the simulation");
            string referenceFile=null;var arguments=Environment.GetCommandLineArgs();
            for(int i=0;i<arguments.Length-1;i++)if(arguments[i]=="-referenceSave")referenceFile=arguments[i+1];
            if(referenceFile!=null)
            {
                var startup=GameSimulation.CreateReferenceScenario(8026);startup.Metrics=new SimulationMetrics();startup.Step(5000);
                File.WriteAllText(Path.Combine(outputDirectory,"startup-performance.csv"),startup.Metrics.Csv());
                Require(SimulationMetrics.Percentile(startup.Metrics.TickMilliseconds,.95)<20,"Reference startup simulation p95 stays within 20 ms during the first 5000 ticks");
                PlanetOne.Platform.SnapshotStore.Save(controller.ActiveSavePath,PlanetOne.Platform.SnapshotStore.Load(referenceFile));
                Require(controller.LoadGame(),"Exported Windows player loads the paid end-to-end reference outcome");
                var measured=controller.Simulation;controller.SetSimulationPaused(false);
                measured.Step(200);measured.Metrics=new SimulationMetrics();measured.Step(5000);
                var metrics=measured.Metrics;measured.Metrics=null;controller.SetSimulationPaused(true);
                File.WriteAllText(Path.Combine(outputDirectory,"simulation-performance.csv"),metrics.Csv());
                int buildings=0,units=0,couriers=0;foreach(var world in measured.State.Planets){buildings+=world.Buildings.Count;units+=world.Units.Count;couriers+=world.Shipments.Count;}
                string hardware="CPU: "+SystemInfo.processorType+"\nRAM MiB: "+SystemInfo.systemMemorySize+"\nGPU: "+SystemInfo.graphicsDeviceName+"\nGPU MiB: "+SystemInfo.graphicsMemorySize+"\nAPI: "+SystemInfo.graphicsDeviceVersion+"\nOS: "+SystemInfo.operatingSystem+"\nResolution: "+Screen.width+"x"+Screen.height+"\nWorlds: "+measured.State.Planets.Count+"\nBuildings: "+buildings+"\nShips: "+measured.State.Ships.Count+"\nUnits: "+units+"\nCouriers: "+couriers+"\nAI: "+measured.State.ComputerFactions.Count+"\nTarget FPS: "+Application.targetFrameRate+"\nVSync: "+QualitySettings.vSyncCount;
                File.WriteAllText(Path.Combine(outputDirectory,"performance-hardware.txt"),hardware);
                Require(SimulationMetrics.Percentile(metrics.TickMilliseconds,.95)<20,"Reference simulation p95 stays within the declared 20 ms per tick budget");
                foreach(int view in new[]{0,1})
                {
                    controller.SetView(view);for(int i=0;i<60;i++)yield return null;
                    var frames=new List<double>();for(int i=0;i<360;i++){yield return null;frames.Add(Time.unscaledDeltaTime*1000);}
                    double p95=SimulationMetrics.Percentile(frames,.95);
                    File.AppendAllText(Path.Combine(outputDirectory,"render-performance.csv"),(view==0?"system":"surface")+","+frames.Count+","+p95.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+"\n");
                    Require(p95<33.3,"Reference "+(view==0?"system":"surface")+" render frame p95 stays within 33.3 ms (simulation measured separately)");
                    yield return Capture(view==0?"reference-completed-system.png":"reference-completed-surface.png");Require(captureFailure==null,captureFailure??"Completed reference view captured");
                }
                long memory=PlanetOne.Platform.ProcessMemory.WorkingSetBytes();
                File.AppendAllText(Path.Combine(outputDirectory,"performance-hardware.txt"),"\nWorking set bytes: "+memory);
                Require(memory>0&&memory<2L*1024*1024*1024,"Reference player working set stays below the declared 2 GiB budget");
                Require(measured.State.Winners.Contains(1)&&controller.ScenarioOutcomeText.Contains("Koloniekonsortium"),"Completed reference outcome names the player in the persistent result display");
                if(measured.State.Winners.Contains(3))Require(controller.ScenarioOutcomeText.Contains("Helios-Verbund"),"Shared victory names the actual coalition partner in the result display");
            }
            var commandChecks=RunCommandChecks();while(commandChecks.MoveNext())yield return commandChecks.Current;
            var sessionChecks=RunSessionChecks();while(sessionChecks.MoveNext())yield return sessionChecks.Current;
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
            if (Application.isBatchMode)
            {
                yield return null;
                try { controller.CaptureWorld(path); }
                catch (Exception exception) { captureFailure = "Offscreen render failed: " + exception.Message; }
                yield break;
            }
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
            if (Application.isBatchMode) results.Add("Headless batch mode: world rendered via Camera.Render/RenderTexture; IMGUI and manual input not visually tested.");
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
