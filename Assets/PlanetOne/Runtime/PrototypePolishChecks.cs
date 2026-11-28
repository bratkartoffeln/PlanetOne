using System;
using System.Collections;
using System.IO;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunPolishChecks()
        {
            var network=RunSupplyNetworkChecks();while(network.MoveNext())yield return network.Current;
            var walls=RunWallChecks();while(walls.MoveNext())yield return walls.Current;
            var radar=RunSelectionRadarChecks();while(radar.MoveNext())yield return radar.Current;
            controller.ConfigurePresentationTestPath(Path.Combine(outputDirectory,"polish-settings.json"));
            controller.StartReferenceScenario();controller.SetSimulationPaused(true);
            string state=SaveCodec.ToJson(controller.Simulation.State);
            foreach(InterfaceLayout layout in Enum.GetValues(typeof(InterfaceLayout)))
            {
                controller.SetInterfaceLayout(layout);controller.SetTextScale(1);
                controller.SetView(1);yield return null;
                Require(controller.BodyTextPixels>=18,"Default body text retains at least 18 nominal screen pixels: "+layout);
                yield return Capture("polish-"+layout+"-surface.png");Require(captureFailure==null,captureFailure??"Surface HUD captured");
                controller.SetView(0);controller.SelectPlanet(1);yield return null;
                yield return Capture("polish-"+layout+"-space.png");Require(captureFailure==null,captureFailure??"Space HUD captured");
                Require(controller.SetTextScale(1.4f),"Maximum independent text size accepted: "+layout);controller.ReloadPresentationSettings();yield return null;
                Require(Mathf.Approximately(controller.TextScale,1.4f) && controller.BodyTextPixels>=25,"Text size persists at its actual rendered scale: "+layout);
                controller.ShowSettingsPage(1);yield return null;
                Require(!controller.IsWorldPoint(controller.WorldViewport.center),"Typography settings block world input: "+layout);
                yield return Capture("polish-"+layout+"-type140.png");Require(captureFailure==null,captureFailure??"Typography preview captured");controller.SetSettingsVisible(false);controller.SetMenuVisible(false);
                controller.SetView(1);controller.SelectBuilding(controller.Simulation.FindPlanet(1).Buildings[0].Id);yield return null;
                yield return Capture("polish-"+layout+"-large.png");Require(captureFailure==null,captureFailure??"Large text in actual HUD captured");
                Require(SaveCodec.ToJson(controller.Simulation.State)==state,"All font and layout changes preserve exact simulation: "+layout);
            }
            Require(!controller.SetTextScale(float.NaN) && !controller.SetTextScale(2) && controller.TextScale==1.4f,"Invalid font settings leave the current preference intact");
            controller.SetTextScale(1);controller.SetInterfaceLayout(InterfaceLayout.OrbitalBridge);controller.SetView(0);controller.SelectPlanet(1);
            Require(controller.OpenSelectedPlanet() && controller.IsViewTransitioning && controller.OpenPlanetId==0,"Planet entry begins with an approach in the system");
            Require(!controller.IsWorldPoint(controller.WorldViewport.center),"Camera flight blocks accidental world commands");
            yield return new WaitForSecondsRealtime(.55f);yield return Capture("polish-planet-approach.png");Require(captureFailure==null,captureFailure??"Approach captured");
            while(controller.IsViewTransitioning)yield return null;
            Require(controller.OpenPlanetId==1 && SaveCodec.ToJson(controller.Simulation.State)==state,"Approach completes at the selected surface without changing the paused game");
            File.WriteAllText(Path.Combine(outputDirectory,"flight-performance.txt"),"Surface build ms: "+controller.LastFlightSurfaceBuildMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
            File.AppendAllText(Path.Combine(outputDirectory,"flight-performance.txt"),"\nMaximum preparation slice ms: "+controller.LastFlightBuildSliceMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
            Require(controller.LastFlightBuildSliceMilliseconds<33.3,"Atmospheric handoff prepares geometry in bounded frame batches");
            var frameTimes=new System.Collections.Generic.List<double>();
            for(int frame=0;frame<180;frame++){yield return null;frameTimes.Add(Time.unscaledDeltaTime*1000);}
            double frameP95=SimulationMetrics.Percentile(frameTimes,.95);
            File.AppendAllText(Path.Combine(outputDirectory,"flight-performance.txt"),"\nSurface frame p95 ms: "+frameP95.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
            Require(frameP95<33.3,"Larger surface render p95 remains within the 33.3 ms frame budget");
            var route=new ShipmentState();route.Path.Add(new GridCell(8,5));route.Path.Add(new GridCell(8,6));route.Path.Add(new GridCell(9,6));
            route.DistanceTravelled=.999;Vector3 beforeTurn=controller.ShipmentRoadPosition(route);
            route.DistanceTravelled=1;Vector3 inTurn=controller.ShipmentRoadPosition(route);
            route.DistanceTravelled=1.001;Vector3 afterTurn=controller.ShipmentRoadPosition(route);
            Require(Vector3.Angle(inTurn-beforeTurn,afterTurn-inTurn)<1,"Courier path crosses a road bend with a continuous heading");
            var motionProbe=new GameObject("Isolated interpolation check");var motion=motionProbe.AddComponent<MotionFacing>();
            motion.MoveTick(Vector3.zero,0,Vector3.up,0,1);yield return null;
            motion.MoveTick(Vector3.right,0,Vector3.up,1,0);
            Require(motionProbe.transform.position==Vector3.zero,"A new movement sample does not teleport the rendered vehicle");
            int intermediateFrames=0;float lastPosition=0;bool monotonic=true;
            for(int sample=1;sample<=5;sample++)
            {
                motion.MoveTick(Vector3.right,0,Vector3.up,1,sample/5f);yield return null;float x=motionProbe.transform.position.x;
                monotonic&=x>=lastPosition&&x<=1;
                if(x>0&&x<1)intermediateFrames++;lastPosition=x;
            }
            Require(monotonic,"Authoritative MoveTick interpolation advances without overshoot");
            Require(intermediateFrames>=2&&Mathf.Abs(lastPosition-1)<.001f,"MoveTick reaches its sample through multiple rendered positions");
            Destroy(motionProbe);
            Require(controller.HandleShortcut(KeyCode.Tab) && controller.IsViewTransitioning,"Return uses the inverse flight");
            yield return new WaitForSecondsRealtime(.55f);yield return Capture("polish-planet-departure.png");Require(captureFailure==null,captureFailure??"Departure captured");
            while(controller.IsViewTransitioning)yield return null;
            Require(controller.OpenPlanetId==0 && SaveCodec.ToJson(controller.Simulation.State)==state,"Inverse flight returns to the same shared simulation");
            foreach(int section in new[]{0,1,2,3})
            {
                controller.SetSpaceWorkspace(section);yield return null;
                Require(SaveCodec.ToJson(controller.Simulation.State)==state,"Space workspace navigation is presentation-only: "+section);
                yield return Capture("polish-workspace-"+section+".png");Require(captureFailure==null,captureFailure??"Space workspace captured");
            }
            for(int section=0;section<5;section++)
            {Require(controller.OpenDiplomacySection(section),"Diplomacy section opens: "+section);yield return null;Require(SaveCodec.ToJson(controller.Simulation.State)==state,"Diplomacy section issues no implicit orders: "+section);yield return Capture("polish-diplomacy-"+section+".png");Require(captureFailure==null,captureFailure??"Diplomacy captured");}
            var colony=RunColonyVisualChecks();while(colony.MoveNext())yield return colony.Current;
            var combat=RunCombatVisualChecks();while(combat.MoveNext())yield return combat.Current;
            var spaceTraffic=RunSpaceTrafficVisualChecks();while(spaceTraffic.MoveNext())yield return spaceTraffic.Current;
            var groundModels=RunGroundModelRadiusChecks();while(groundModels.MoveNext())yield return groundModels.Current;
            controller.StartReferenceScenario();controller.SetSimulationPaused(false);controller.SelectPlanet(1);
            var liveSimulation=controller.Simulation;long liveTick=liveSimulation.State.Tick;
            Require(controller.OpenSelectedPlanet(),"An active game can begin the same planet approach");
            while(controller.IsViewTransitioning)yield return null;
            Require(ReferenceEquals(liveSimulation,controller.Simulation)&&!liveSimulation.State.Paused&&liveSimulation.State.Tick>liveTick,"The shared simulation continues during incremental scene preparation and camera flight");
            controller.SetSimulationPaused(true);
            var directOrders=RunDirectOrderChecks();while(directOrders.MoveNext())yield return directOrders.Current;
            var orderArrows=RunOrderArrowChecks();while(orderArrows.MoveNext())yield return orderArrows.Current;
            var focus=RunPlanetFocusChecks();while(focus.MoveNext())yield return focus.Current;
        }
    }
}
