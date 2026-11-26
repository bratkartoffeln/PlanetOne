using System.Collections;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunOrderArrowChecks()
        {
            controller.StartMilitaryFixture();controller.SetSimulationPaused(true);controller.CloseContextPanel();
            var sim=controller.Simulation;var planet=sim.FindPlanet(1);
            var tank=planet.Units.Find(u=>u.OwnerId==1&&u.Kind==UnitKind.Tank);
            var enemy=planet.Units.Find(u=>u.OwnerId==2&&u.Kind==UnitKind.Tank);
            PlaceDirectOrderUnit(tank,12,15);PlaceDirectOrderUnit(enemy,30,25);
            yield return null;yield return null;
            controller.SelectUnit(tank.Id);controller.CloseContextPanel();controller.SetSurfaceFocus(controller.UnitView(tank.Id).position);
            yield return null;
            Require(controller.IssueDefaultOrderAtScreen(controller.SurfaceScreenPoint(16,15)),"Mouse movement creates an accepted target-arrow command");
            yield return Capture("order-arrow-move-green.png");
            Require(captureFailure==null&&controller.ActiveOrderArrowCount==1,"Transparent green movement arrow is present while its command is pending");
            AdvanceSeconds(Definitions.TickSeconds);
            for(int i=0;i<400&&tank.Order!=UnitOrder.Hold;i++)AdvanceSeconds(Definitions.TickSeconds);
            yield return null;yield return null;
            Require(tank.Order==UnitOrder.Hold&&controller.ActiveOrderArrowCount==0,"Movement arrow disappears on actual arrival");
            PlaceDirectOrderUnit(tank,12,15);PlaceDirectOrderUnit(enemy,15,15);tank.Cooldown=0;
            yield return null;yield return null;
            var target=controller.UnitSelectionScreenRect(enemy.Id,false).center;
            Require(controller.IssueDefaultOrderAtScreen(target),"Mouse attack creates an accepted attack-arrow command");
            yield return Capture("order-arrow-attack-red.png");
            Require(captureFailure==null&&controller.ActiveOrderArrowCount==1,"Transparent red attack arrow is present before the first shot");
            int ammunition=tank.Ammunition;AdvanceSeconds(.5);yield return null;yield return null;
            Require(tank.Ammunition<ammunition&&controller.ActiveOrderArrowCount==0,"Attack arrow disappears after a real shot at its target");
            yield return Capture("order-arrow-attack-complete.png");
        }
    }
}
