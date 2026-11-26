using System;
using System.Collections;
using System.IO;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunCommandChecks()
        {
            controller.ConfigurePresentationTestPath(Path.Combine(outputDirectory,"command-ui-settings.json"));
            foreach(InterfaceLayout layout in Enum.GetValues(typeof(InterfaceLayout)))
            {
                controller.StartMilitaryFixture();controller.SetInterfaceLayout(layout,false);
                var sim=controller.Simulation;var p=sim.FindPlanet(1);var b=p.Buildings.Find(v=>v.Kind==BuildingKind.SolarPlant);
                controller.SelectBuilding(b.Id);yield return null;
                string before=SaveCodec.ToJson(sim.State);
                Require(controller.OpenSelectionCommandMenu(new Vector2(Screen.width-2,2)),"Object commands open from a visible HUD action: "+layout);
                var bounds=controller.CommandMenuScreenRect;
                Require(bounds.x>=0 && bounds.y>=0 && bounds.xMax<=Screen.width && bounds.yMax<=Screen.height,"Context menu stays within screen at the lower-right edge: "+layout);
                Require(SaveCodec.ToJson(sim.State)==before && !controller.IsWorldPoint(controller.WorldViewport.center),"Opening a menu preserves state and blocks world input: "+layout);
                Require(!controller.BeginSurfacePan(controller.WorldViewport.center),"An open menu cannot begin a camera drag: "+layout);
                yield return Capture("commands-building-"+layout+".png");Require(captureFailure==null,captureFailure??"Building context captured");
                Require(controller.InvokeContextCommand("priority") && SaveCodec.ToJson(sim.State)==before,"Priority submenu does not submit an implicit command");
                Require(controller.InvokeContextCommand("priority-2"),"Priority choice uses the real command adapter");
                var command=sim.State.PendingCommands[sim.State.PendingCommands.Count-1];
                Require(command.Kind==CommandKind.SetBuildingPriority && command.TargetId==b.Id && command.IntValue==2,"Priority applies to the pinned building and exact chosen level");
                controller.OpenSelectionCommandMenu(controller.WorldViewport.center);int pending=sim.State.PendingCommands.Count;
                Require(controller.InvokeContextCommand("demolish") && sim.State.PendingCommands.Count==pending,"Destructive action first shows recovery confirmation");
                Require(controller.HandleShortcut(KeyCode.Escape) && !controller.IsCommandMenuOpen && sim.State.PendingCommands.Count==pending,"Escape cancels demolition without a queued order");
                controller.OpenSelectionCommandMenu(controller.WorldViewport.center);
                b.OwnerId=2;
                Require(!controller.InvokeContextCommand("toggle") && sim.State.PendingCommands.Count==pending,"Ownership change invalidates a stale building command");b.OwnerId=0;
                controller.CloseCommandMenu();controller.OpenSelectionCommandMenu(new Vector2(Screen.width-2,2));
                Require(controller.CommandPointerDown(new Vector2(1,Screen.height-1)) && !controller.IsCommandMenuOpen && !controller.IsWorldPoint(controller.WorldViewport.center),"Outside click closes and is consumed before reaching the world");
                yield return null;
                Require(controller.IsWorldPoint(controller.WorldViewport.center),"World input resumes after the consumed dismissal frame");
                controller.CloseContextPanel();controller.SetSurfaceFocus(controller.UnitView(p.Units.Find(u=>u.Kind==UnitKind.Tank && u.OwnerId==1).Id).position);yield return null;
                var tank=p.Units.Find(u=>u.Kind==UnitKind.Tank && u.OwnerId==1);controller.SelectUnit(tank.Id);
                controller.OpenSelectionCommandMenu(controller.WorldViewport.center);
                Require(!controller.InvokeContextCommand("repair") && !controller.InvokeContextCommand("bombard"),"Tank selection does not offer engineer or artillery orders");
                yield return Capture("commands-unit-"+layout+".png");Require(captureFailure==null,captureFailure??"Unit context captured");
                controller.CloseCommandMenu();controller.OpenSelectionCommandMenu(controller.WorldViewport.center);
                Require(controller.InvokeContextCommand("hold"),"Context hold command accepted");
                command=sim.State.PendingCommands[sim.State.PendingCommands.Count-1];Require(command.Kind==CommandKind.HoldPosition && command.ActorId==tank.Id,"Unit command retains the selected actor");
                controller.SetView(0);Require(!controller.IsCommandMenuOpen,"View changes close object menus");
            }
            controller.StartSpaceFixture();controller.SetInterfaceLayout(InterfaceLayout.CommandCenter,false);
            var space=controller.Simulation;var colony=space.State.Ships.Find(s=>s.Kind==ShipKind.ColonyShip);
            controller.SelectShip(colony.Id);controller.CloseContextPanel();var moon=space.GetOrbitPosition(3);controller.SetSpaceFocus(PrototypeController.SpacePosition(moon.X,moon.Y));yield return null;
            Vector2 target=Camera.main.WorldToScreenPoint(controller.CelestialView(3).position);int orders=space.State.PendingCommands.Count;
            Require(controller.BeginSpacePan(target,true) && controller.EndSpacePan(target) && controller.IsCommandMenuOpen,"Control-right-click on a target world opens contextual fleet orders");
            Require(space.State.PendingCommands.Count==orders,"Opening target world commands does not send a ship");
            yield return Capture("commands-planet-target.png");Require(captureFailure==null,captureFailure??"World target commands captured");
            Require(controller.InvokeContextCommand("fly"),"Explicit planet flight action accepted");
            var flight=space.State.PendingCommands[space.State.PendingCommands.Count-1];Require(flight.Kind==CommandKind.FlyShip && flight.ActorId==colony.Id && flight.PlanetId==3,"Context flight pins both selected ship and clicked world");
            AdvanceSeconds(.2);Require(colony.Order==ShipOrder.Fly && colony.TargetPlanetId==3,"Context flight executes in the shared simulation");
            controller.OpenSelectionCommandMenu(controller.WorldViewport.center);controller.SetSettingsVisible(true);
            Require(!controller.IsCommandMenuOpen && !controller.OpenSelectionCommandMenu(controller.WorldViewport.center),"Settings dismiss and block object commands");controller.SetSettingsVisible(false);
            controller.StartMilitaryFixture();controller.SetInterfaceLayout(InterfaceLayout.CommandCenter,false);var planet=controller.Simulation.FindPlanet(1);var solar=planet.Buildings.Find(b=>b.Kind==BuildingKind.SolarPlant);
            controller.SelectBuilding(solar.Id);yield return null;
            Vector2 buildingPoint=controller.SurfaceScreenPoint(solar.X,solar.Y);
            var engineer=planet.Units.Find(u=>u.OwnerId==1 && u.Kind==UnitKind.Engineer);solar.Health-=10;
            controller.SelectUnit(engineer.Id);
            Require(controller.OpenContextMenuAtScreen(buildingPoint) && controller.InvokeContextCommand("repair-here"),"Selected engineer receives a context action on an own building without losing the actor");
            var repair=controller.Simulation.State.PendingCommands[controller.Simulation.State.PendingCommands.Count-1];
            Require(repair.Kind==CommandKind.RepairTarget && repair.ActorId==engineer.Id && repair.TargetId==solar.Id,"Repair keeps the clicked building and previously selected engineer distinct");
            controller.SelectBuilding(solar.Id);
            Require(controller.BeginSurfacePan(buildingPoint,true) && controller.EndSpacePan(buildingPoint),"Control-right-click on a rendered building opens its actual actions");
            Require(controller.InvokeContextCommand("toggle"),"Rendered building menu toggles operation");AdvanceSeconds(.1);
            Require(!solar.Enabled,"Building operation changes through the normal simulation queue");
            controller.SelectBuilding(solar.Id);controller.OpenSelectionCommandMenu(controller.WorldViewport.center);
            Require(controller.InvokeContextCommand("demolish") && controller.InvokeContextCommand("confirm"),"Confirmed demolition is explicitly submitted");AdvanceSeconds(.1);
            Require(solar.Deconstructing || solar.Destroyed || !planet.Buildings.Contains(solar),"Confirmed demolition reaches the real building lifecycle");
            controller.StartMilitaryFixture();controller.SetInterfaceLayout(InterfaceLayout.OpenSpace,false);
            string settings=Path.Combine(outputDirectory,"command-large.json");File.WriteAllText(settings,"{\"Layout\":2,\"Scale\":1.2,\"Grid\":true}");controller.ConfigurePresentationTestPath(settings);
            controller.SelectUnit(controller.Simulation.FindPlanet(1).Units.Find(u=>u.Kind==UnitKind.Engineer && u.OwnerId==1).Id);
            controller.OpenSelectionCommandMenu(new Vector2(Screen.width-1,1));var large=controller.CommandMenuScreenRect;
            Require(large.xMin>=0 && large.yMin>=0 && large.xMax<=Screen.width && large.yMax<=Screen.height,"Large UI scale keeps the complete command menu reachable");
            yield return Capture("commands-large-scale.png");Require(captureFailure==null,captureFailure??"Large command menu captured");
            Require(controller.InvokeContextCommand("repair"),"Engineer contextual repair enters the normal target mode");
            controller.HandleShortcut(KeyCode.Escape);controller.CloseCommandMenu();
            controller.ConfigurePresentationTestPath(Path.Combine(outputDirectory,"fleet-ui-default.json"));controller.SetInterfaceLayout(InterfaceLayout.CommandCenter,false);
            controller.StartSpaceFixture();var ships=controller.Simulation;var chosen=ships.State.Ships.Find(s=>s.Kind==ShipKind.Freighter);controller.SelectShip(chosen.Id);
            controller.OpenSelectionCommandMenu(controller.WorldViewport.center);Require(controller.InvokeContextCommand("details"),"Ship command opens the dedicated order workspace");
            yield return Capture("commands-fleet-orders.png");Require(captureFailure==null,captureFailure??"Fleet orders captured");
            controller.SetView(1);var port=ships.FindPlanet(1).Buildings.Find(b=>b.Kind==BuildingKind.Spaceport && b.Built);controller.SelectBuilding(port.Id);
            string beforeProduction=SaveCodec.ToJson(ships.State);controller.OpenSelectionCommandMenu(controller.WorldViewport.center);
            Require(controller.InvokeContextCommand("production") && SaveCodec.ToJson(ships.State)==beforeProduction,"Spaceport production opens its shipyard without placing a build order");
            yield return Capture("commands-ship-production.png");Require(captureFailure==null,captureFailure??"Ship production captured");
            controller.StartAsteroidFixture();yield return null;yield return Capture("commands-mining.png");Require(captureFailure==null,captureFailure??"Dedicated asteroid workspace captured");
        }
    }
}
