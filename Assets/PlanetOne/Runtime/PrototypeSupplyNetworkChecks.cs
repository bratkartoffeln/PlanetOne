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
            Require(controller.RenderedTrailCount>0,"Normal view renders traces from completed deliveries");
            yield return Capture("supply-normal.png");Require(captureFailure==null,"Uncluttered normal supply view captured");
            Require(controller.HandleShortcut(KeyCode.L)&&controller.SupplyOverlayVisible,"L opens the logistics overlay");
            yield return Capture("supply-overlay.png");Require(captureFailure==null,"Network overlay captured");
            controller.ToggleSupplyOverlay();controller.SelectBuilding(hq.Id);controller.CloseContextPanel();
            yield return Capture("supply-building-selection.png");Require(captureFailure==null,"Selected building supply view captured");
            Require(SaveCodec.ToJson(controller.Simulation.State)==before,"Overlay and selection do not change the shared simulation");
            controller.SelectBuilding(0);
            int tx=-1,ty=-1;
            for(int y=10;y<p.Height && tx<0;y++)for(int x=15;x<p.Width && tx<0;x++)
                if(!p.Roads[y*p.Width+x] && controller.Simulation.ValidatePlacement(1,BuildingKind.SolarPlant,x,y)==null){tx=x;ty=y;}
            Require(tx>=0,"A free building site is reachable across open ground");
            Require(controller.RequestConstruction(1,BuildingKind.SolarPlant,tx,ty),"A paid construction order needs no road");
            AdvanceSeconds(Definitions.TickSeconds);
            var site=p.Buildings.Find(b=>b.X==tx && b.Y==ty);
            Require(site!=null,"Ordered site exists at the chosen destination");
            ShipmentState cargo=null;
            for(int i=0;i<1200 && cargo==null;i++)
            {
                AdvanceSeconds(Definitions.TickSeconds);
                cargo=p.Shipments.Find(s=>s.TargetId==site.Id && s.Amount>0 && s.Phase==ShipmentPhase.InTransit && s.DistanceTravelled>2 && !GameSimulation.GroundCourierDocked(s));
            }
            Require(cargo!=null,"A loaded courier drives to the ordered site");
            var point=GameSimulation.GroundCourierPosition(cargo);controller.SetSurfaceFocus(new Vector3((float)point.X,0,(float)point.Y));
            controller.SelectBuilding(cargo.TargetId);controller.CloseContextPanel();yield return null;yield return null;
            Require(controller.CourierView(cargo.Id)!=null&&controller.CourierView(cargo.Id).Find("Ladebehälter").gameObject.activeInHierarchy,"Actual moving courier shows its real loaded container");
            yield return Capture("supply-live-delivery.png");Require(captureFailure==null,"Selected delivery route and full-size loaded courier captured");
            for(int i=0;i<1600 && cargo.Phase==ShipmentPhase.InTransit;i++)AdvanceSeconds(Definitions.TickSeconds);
            Require(cargo.Phase==ShipmentPhase.Unloading,"Courier reached the construction apron");
            yield return null;yield return null;
            var unloading=controller.CourierView(cargo.Id);
            var crate=unloading.Find("Ladebehälter");var bed=crate.localPosition;
            AdvanceSeconds(Definitions.TransferSeconds*.5);yield return null;
            Require(unloading.gameObject.activeInHierarchy && crate.gameObject.activeInHierarchy && (crate.localPosition-bed).magnitude>.12f,
                "Unloading at a construction site moves the real cargo model while the courier stays visible");
            controller.SetSurfaceFocus(unloading.position);
            Camera.main.orthographicSize=1.6f;controller.CaptureWorld(System.IO.Path.Combine(outputDirectory,"construction-delivery.png"));
            for(int i=0;i<120 && !site.Built;i++)AdvanceSeconds(1);
            Require(site.Built && !p.Roads[ty*p.Width+tx],"Delivered materials and a builder finish the site without road construction");
            controller.SetView(0);
        }
    }
}
