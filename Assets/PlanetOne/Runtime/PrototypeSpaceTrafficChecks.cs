using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunSpaceTrafficVisualChecks()
        {
            controller.StartInvasionFixture();controller.SetSimulationPaused(true);controller.SetView(0);controller.CloseContextPanel();
            var actual=controller.Simulation;
            foreach(var old in actual.State.Ships)actual.DestroyShip(old.Id);
            var runner=TrafficVisualShip(actual,ShipKind.Scout,29,30,40,30);
            yield return null;yield return null;
            var movingScout=controller.ShipView(runner.Id);
            var plume=movingScout.GetComponent<ShipExhaust>().Plume;
            controller.SetSimulationPaused(false);
            float deadline=Time.realtimeSinceStartup+2;
            while(plume.particleCount<4 && Time.realtimeSinceStartup<deadline)yield return null;
            Require(plume.particleCount>=4,"Moving scout emits paired engine particles");
            controller.SetSimulationPaused(true);
            controller.SetSpaceFocus(movingScout.position);Camera.main.orthographicSize=1.25f;
            controller.CaptureWorld(Path.Combine(outputDirectory,"space-engine-plume.png"));
            actual.DestroyShip(runner.Id);
            var ships=new Dictionary<ShipKind,ShipState>();
            foreach(ShipKind kind in Enum.GetValues(typeof(ShipKind)))ships[kind]=InvasionFixture.AddShip(actual,1,1,kind);
            yield return null;yield return null;
            var radii=new Dictionary<ShipKind,float>();var report=new List<string>();
            foreach(var entry in ships)
            {
                var view=controller.ShipView(entry.Value.Id);float radius=ModelYawRadius(view,true);
                float allowed=(float)Definitions.ShipRadius(entry.Key)*PrototypeController.SpaceDistanceScale;
                radii[entry.Key]=radius;
                report.Add(entry.Key+": mesh yaw envelope="+radius.ToString("F5",System.Globalization.CultureInfo.InvariantCulture)+"; core world radius="+allowed.ToString("F5",System.Globalization.CultureInfo.InvariantCulture));
                Require(view.gameObject.activeInHierarchy && radius>0 && radius<=allowed+.00001f,"Rendered ship body including full turret sweep stays inside its physical radius: "+entry.Key);
            }
            File.WriteAllLines(Path.Combine(outputDirectory,"space-traffic-model-radii.txt"),report.ToArray());

            // Clone the actual rendered geometry into an isolated, labelled rendering fixture. Its positions
            // come exclusively from ordinary fixed ticks of a separate test simulation, never invented paths.
            var first=Instantiate(controller.ShipView(ships[ShipKind.Scout].Id).gameObject);first.name="Verkehrsprüfung · Aufklärer";
            var second=Instantiate(controller.ShipView(ships[ShipKind.TorpedoShip].Id).gameObject);second.name="Verkehrsprüfung · Torpedoschiff";
            first.transform.SetParent(null,true);second.transform.SetParent(null,true);
            foreach(var old in actual.State.Ships)actual.DestroyShip(old.Id);
            var simulation=GameSimulation.CreateDefault(1818);simulation.State.SpaceTestFixture=true;
            var a=TrafficVisualShip(simulation,ShipKind.Scout,29.2,30,34,30);
            var b=TrafficVisualShip(simulation,ShipKind.TorpedoShip,30.8,30,26,30);
            var motionA=first.GetComponent<MotionFacing>();var motionB=second.GetComponent<MotionFacing>();
            if(motionA==null)motionA=first.AddComponent<MotionFacing>();if(motionB==null)motionB=second.AddComponent<MotionFacing>();
            Vector3 startA=PrototypeController.SpacePosition(a.X,a.Y,2.3f),startB=PrototypeController.SpacePosition(b.X,b.Y,2.3f);
            motionA.MoveTick(startA,0,Vector3.up,simulation.State.Tick,1);motionB.MoveTick(startB,0,Vector3.up,simulation.State.Tick,1);
            controller.SetSpaceFocus(PrototypeController.SpacePosition(30,30,2.3f));Camera.main.orthographicSize=4;
            bool separated=true,monotonic=true,exact=true;int middleFrames=0;
            for(int tick=0;tick<12;tick++)
            {
                startA=PrototypeController.SpacePosition(a.X,a.Y,2.3f);startB=PrototypeController.SpacePosition(b.X,b.Y,2.3f);
                simulation.Step();
                Vector3 targetA=PrototypeController.SpacePosition(a.X,a.Y,2.3f),targetB=PrototypeController.SpacePosition(b.X,b.Y,2.3f);
                Vector3 priorA=startA,priorB=startB;
                for(int sample=0;sample<=4;sample++)
                {
                    float alpha=sample/4f;
                    motionA.MoveTick(targetA,0,Vector3.up,simulation.State.Tick,alpha);
                    motionB.MoveTick(targetB,0,Vector3.up,simulation.State.Tick,alpha);
                    var turret=second.transform.Find("Turret");if(turret!=null)turret.localRotation=Quaternion.Euler(0,tick*53+sample*41,0);
                    Vector3 renderA=first.transform.position,renderB=second.transform.position;
                    exact&=Vector3.Distance(renderA,Vector3.Lerp(startA,targetA,alpha))<.0001f && Vector3.Distance(renderB,Vector3.Lerp(startB,targetB,alpha))<.0001f;
                    monotonic&=Vector3.Dot(renderA-priorA,targetA-startA)>=-.00001f && Vector3.Dot(renderB-priorB,targetB-startB)>=-.00001f;
                    separated&=Vector3.Distance(renderA,renderB)+.0001f>=radii[ShipKind.Scout]+radii[ShipKind.TorpedoShip];
                    if(sample>0 && sample<4 && Vector3.Distance(targetA,startA)>.001f)middleFrames++;
                    priorA=renderA;priorB=renderB;
                    yield return null;
                    if(tick==4 && sample==2){yield return Capture("space-traffic-swept-models.png");Require(captureFailure==null,captureFailure??"Actual ship geometry follows shared tick interpolation during counterflow");}
                }
            }
            Require(exact && monotonic && middleFrames>=12,"Actual MoveTick path renders several monotonic shared-alpha positions for real simulation ticks");
            Require(separated,"Rotating rendered ship envelopes remain separate throughout counterflow intermediate frames");
            Vector3 skipped=first.transform.position+Vector3.right*3;
            motionA.MoveTick(skipped,0,Vector3.up,simulation.State.Tick+3,.2f);
            Require(Vector3.Distance(first.transform.position,skipped)<.0001f,"Skipped movement ticks use the safe authoritative sample instead of cutting across unobserved segments");
            Destroy(first);Destroy(second);yield return null;
        }

        private static ShipState TrafficVisualShip(GameSimulation simulation,ShipKind kind,double x,double y,int goalX,int goalY)
        {
            var d=Definitions.Ships[(int)kind];var w=Definitions.SpaceWeapons[(int)kind];
            var ship=new ShipState{Id=simulation.State.NextEntityId++,OwnerId=1,Kind=kind,X=x,Y=y,GoalX=goalX,GoalY=goalY,Order=ShipOrder.MovePosition,Crew=d.Crew,Health=w.Health,Ammunition=w.Ammo};
            simulation.State.Ships.Add(ship);return ship;
        }

        // Vertex projection bounds the real triangle surface. For an independently rotating turret,
        // pivot distance + vertex distance is a conservative envelope for every possible yaw angle.
        private static float ModelYawRadius(Transform root,bool includeTurretSweep)
        {
            return PrototypeController.ModelMotionRadius(root,includeTurretSweep,false);
        }
    }
}
