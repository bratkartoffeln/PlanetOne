using System.Collections;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunSupplyNetworkChecks()
        {
            controller.SetSettingsVisible(false);controller.SetMenuVisible(false);controller.CloseCommandMenu();
            controller.StartPlanetEnvironmentFixture();controller.SetSimulationPaused(true);controller.SetView(1);controller.CloseContextPanel();
            if(controller.SupplyOverlayVisible)controller.ToggleSupplyOverlay();
            yield return null;yield return null;
            var p=controller.Simulation.FindPlanet(1);var hq=p.Buildings.Find(b=>b.Kind==BuildingKind.Headquarters);
            string before=SaveCodec.ToJson(controller.Simulation.State);
            Require(!controller.IsRoadRendered(10,6),"Normal supply view has no permanent asphalt meshes");
            yield return Capture("supply-normal.png");Require(captureFailure==null,"Uncluttered normal supply view captured");
            Require(controller.HandleShortcut(KeyCode.L)&&controller.SupplyOverlayVisible,"L opens the logistics overlay");
            yield return Capture("supply-overlay.png");Require(captureFailure==null,"Network overlay captured");
            controller.ToggleSupplyOverlay();controller.SelectBuilding(hq.Id);controller.CloseContextPanel();
            yield return Capture("supply-building-selection.png");Require(captureFailure==null,"Selected building supply view captured");
            Require(SaveCodec.ToJson(controller.Simulation.State)==before,"Overlay and selection do not change the shared simulation");
            controller.SelectBuilding(0);controller.BeginSupplyConnection();
            Require(controller.SelectSupplyEndpoint(hq.X,hq.Y),"Connection planner accepts an owned source building");
            Require(!controller.SelectSupplyEndpoint(-1,-1)&&controller.SupplyPlanning,"Invalid connection keeps planning without queuing a partial route");
            int tx=-1,ty=-1;string error;
            for(int y=10;y<p.Height&&tx<0;y++)for(int x=15;x<p.Width&&tx<0;x++)
                if(p.Terrain[y*p.Width+x]==0&&!p.Buildings.Exists(b=>b.X==x&&b.Y==y)&&controller.Simulation.PlanSupplyConnection(1,1,hq.Id,x,y,out error)!=null){tx=x;ty=y;}
            Require(tx>=0&&controller.PreviewSupplyEndpoint(tx,ty),"Connection preview uses the core-validated obstacle detour");
            yield return Capture("supply-connection-preview.png");Require(captureFailure==null,"Green complete connection preview captured");
            Require(controller.SelectSupplyEndpoint(tx,ty),"Two endpoint selections queue one complete supply connection");
            Require(controller.Simulation.State.PendingCommands.Exists(c=>c.Kind==CommandKind.ConnectSupply),"Planning uses the validated shared command interface");
            AdvanceSeconds(Definitions.TickSeconds);yield return null;
            Require(controller.Simulation.ValidatePlacement(1,BuildingKind.SolarPlant,tx,ty)==null,"Connected destination remains a buildable endpoint");
            Require(controller.RequestConstruction(1,BuildingKind.SolarPlant,tx,ty),"Connected site accepts a paid construction order");
            AdvanceSeconds(Definitions.TickSeconds);
            for(int i=0;i<1000&&!p.Shipments.Exists(s=>s.Amount>0&&s.Phase==ShipmentPhase.InTransit&&!GameSimulation.GroundCourierDocked(s)&&s.DistanceTravelled>2);i++)AdvanceSeconds(Definitions.TickSeconds);
            var cargo=p.Shipments.Find(s=>s.Amount>0&&s.Phase==ShipmentPhase.InTransit&&!GameSimulation.GroundCourierDocked(s)&&s.DistanceTravelled>2);
            Require(cargo!=null,"New network carries actual paid construction cargo");
            var point=GameSimulation.GroundCourierPosition(cargo);controller.SetSurfaceFocus(new Vector3((float)point.X,0,(float)point.Y));
            controller.SelectBuilding(cargo.TargetId);controller.CloseContextPanel();yield return null;yield return null;
            Require(controller.CourierView(cargo.Id)!=null&&controller.CourierView(cargo.Id).Find("Ladebehälter").gameObject.activeInHierarchy,"Actual moving courier shows its real loaded container");
            yield return Capture("supply-live-delivery.png");Require(captureFailure==null,"Selected delivery route and full-size loaded courier captured");
            controller.BeginSupplyConnection();Require(controller.HandleShortcut(KeyCode.Escape)&&!controller.SupplyPlanning,"Escape cancels supply planning");
            controller.BeginSupplyConnection();controller.SetView(0);Require(!controller.SupplyPlanning,"Changing views cancels stale connection planning");
        }
    }
}
