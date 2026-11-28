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
            var other=planet.Units.Find(u=>u.OwnerId==1&&u.Id!=tank.Id);
            controller.SelectUnit(other.Id);yield return null;yield return null;
            Require(controller.VisibleOrderArrowCount==0&&controller.ActiveOrderArrowCount==1,"Deselecting the actor hides its arrow without deleting the queued movement");
            controller.SelectUnit(tank.Id);yield return null;yield return null;
            Require(controller.VisibleOrderArrowCount==1,"Reselecting the actor restores its active arrow");
            controller.SelectBuilding(0);yield return null;yield return null;
            Require(controller.SelectedUnitCount==0&&controller.VisibleOrderArrowCount==0&&controller.ActiveOrderArrowCount==1,"Landscape selection clears unit focus and arrows without cancelling movement");
            controller.SelectUnit(tank.Id);controller.CloseContextPanel();
            AdvanceSeconds(Definitions.TickSeconds);
            for(int i=0;i<400&&tank.Order!=UnitOrder.Hold;i++)AdvanceSeconds(Definitions.TickSeconds);
            yield return null;yield return null;
            Require(tank.Order==UnitOrder.Hold&&controller.ActiveOrderArrowCount==0,"Movement arrow disappears on actual arrival");
            PlaceDirectOrderUnit(tank,12,15);PlaceDirectOrderUnit(enemy,17,15);tank.Cooldown=0;
            controller.CloseContextPanel();var attackFocus=HexGrid.ToWorld(14.5,15);controller.SetSurfaceFocus(new Vector3((float)attackFocus.X,0,(float)attackFocus.Y));
            Require(HexGrid.Distance(tank.X,tank.Y,enemy.X,enemy.Y)>Definitions.Get(tank.Kind).Range&&sim.CanSeeEntity(planet,1,enemy.Id),"Mouse attack target starts visible but outside weapon range");
            yield return null;yield return null;
            var target=controller.UnitSelectionScreenRect(enemy.Id,false).center;
            Require(controller.IssueDefaultOrderAtScreen(target),"Mouse attack creates an accepted attack-arrow command");
            yield return Capture("order-arrow-attack-red.png");
            Require(captureFailure==null&&controller.ActiveOrderArrowCount==1,"Transparent red attack arrow is present before the first shot");
            int ammunition=tank.Ammunition;AdvanceSeconds(Definitions.TickSeconds);yield return null;yield return null;
            Require(HexGrid.Distance(12,15,tank.X,tank.Y)>0&&tank.Ammunition==ammunition&&controller.ActiveOrderArrowCount==1,"Out-of-range attack physically approaches while retaining its red arrow and ammunition");
            yield return Capture("order-arrow-attack-approach.png");Require(captureFailure==null,"Attack approach captured");
            for(int i=0;i<200&&tank.Ammunition==ammunition;i++)AdvanceSeconds(Definitions.TickSeconds);
            yield return null;yield return null;
            Require(tank.Ammunition<ammunition&&controller.ActiveOrderArrowCount==0,"Attack arrow disappears after a real shot at its target");
            yield return Capture("order-arrow-attack-complete.png");
        }
    }
}
