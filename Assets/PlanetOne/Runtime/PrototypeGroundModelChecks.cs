using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunCourierSpacingChecks()
        {
            controller.StartMilitaryFixture();controller.SetSimulationPaused(true);
            controller.SetView(1);controller.CloseContextPanel();
            var planet=controller.Simulation.FindPlanet(1);
            planet.Shipments.Clear();
            var fleet=new List<ShipmentState>();
            for(int i=0;i<4;i++)
            {
                var vehicle=new ShipmentState{Id=controller.Simulation.State.NextEntityId++,SourceId=planet.Buildings[0].Id,
                    TargetId=planet.Buildings[1].Id,Phase=i==0?ShipmentPhase.BuilderOutbound:ShipmentPhase.InTransit};
                for(int x=16;x<=24;x++) vehicle.Path.Add(new GridCell(i==3?40-x:x,16));
                fleet.Add(vehicle);planet.Shipments.Add(vehicle);
            }
            controller.SetSurfaceFocus(new Vector3(20,0,16*(float)HexGrid.RowHeight));
            for(int frame=0;frame<=30;frame++)
            {
                foreach(var vehicle in fleet)vehicle.DistanceTravelled=2.5+frame*.1;
                yield return null;
                for(int i=0;i<fleet.Count;i++)
                {
                    var first=controller.ShipmentModel(fleet[i].Id);
                    for(int j=i+1;j<fleet.Count;j++)
                    {
                        var second=controller.ShipmentModel(fleet[j].Id);
                        var delta=first.position-second.position;
                        float gap=first.GetComponent<VehicleModelSize>().VisualRadius+second.GetComponent<VehicleModelSize>().VisualRadius;
                        Require(delta.x*delta.x+delta.z*delta.z>=gap*gap,"Rendered vehicles keep model clearance at frame "+frame+" pair "+i+"/"+j);
                    }
                }
                if(frame==15) {Camera.main.orthographicSize=2;controller.CaptureWorld(Path.Combine(outputDirectory,"courier-spacing.png"));}
            }
            planet.Shipments.RemoveRange(1,3);
            fleet[0].PassingOffset=0;
            yield return null;yield return null;
            var display=controller.ShipmentModel(fleet[0].Id).position;
            Require(Mathf.Abs(display.z-16*(float)HexGrid.RowHeight)<.001f,"Lone builder follows the route centre");
        }
        private IEnumerator RunGroundModelRadiusChecks()
        {
            var activity=RunModelActivityChecks();while(activity.MoveNext())yield return activity.Current;
            controller.StartPlanetEnvironmentFixture();controller.SetSimulationPaused(true);
            var report=new List<string>();
            foreach(int planetId in new[]{1,7})
            {
                controller.SetView(planetId);controller.CloseContextPanel();yield return null;yield return null;
                bool gas=PlanetEnvironment.Airborne(controller.Simulation.FindPlanet(planetId));
                var models=new List<Transform>();var names=new List<string>();
                foreach(UnitKind kind in Enum.GetValues(typeof(UnitKind)))
                {
                    int i=models.Count;models.Add(controller.CreateVehicleGeometryProbe(kind,0,16+(i%4)*3,12+(i/4)*3));names.Add(kind.ToString());
                }
                for(int type=1;type<=2;type++)
                {int i=models.Count;models.Add(controller.CreateVehicleGeometryProbe(UnitKind.Engineer,type,16+(i%4)*3,12+(i/4)*3));names.Add(type==1?"Kurier":"Baufahrzeug");}
                for(int i=0;i<models.Count;i++)
                {
                    var model=models[i];var size=model.GetComponent<VehicleModelSize>();
                    float yawRadius=ModelYawRadius(model,true),envelope=PrototypeController.ModelMotionRadius(model,true,!gas);
                    Require(size!=null && yawRadius>0 && envelope<=size.VisualRadius+.00001f,"Real "+(gas?"gas":"land")+" model including moving parts fits its visual envelope: "+names[i]);
                    if(!gas&&(names[i]=="Kurier"||names[i]=="Baufahrzeug"))
                        Require(envelope<GameSimulation.RoadLaneOffset&&size.VisualRadius>.2f,
                            "Restored full-size model leaves space for opposing traffic: "+names[i]);
                    report.Add((gas?"Gas":"Land")+" / "+names[i]+": scale "+size.PreferredScale.ToString("F5",CultureInfo.InvariantCulture)+" -> "+size.AppliedScale.ToString("F5",CultureInfo.InvariantCulture)+
                        "; envelope "+size.BeforeRadius.ToString("F5",CultureInfo.InvariantCulture)+" -> "+envelope.ToString("F5",CultureInfo.InvariantCulture)+"; yaw="+yawRadius.ToString("F5",CultureInfo.InvariantCulture)+"; visual="+size.VisualRadius.ToString("F5",CultureInfo.InvariantCulture));
                    var turret=model.Find("Turret");var crane=model.Find("Kranarm");Quaternion craneRest=crane==null?Quaternion.identity:crane.localRotation;
                    for(int heading=0;heading<4;heading++)
                    {
                        model.rotation=Quaternion.Euler(gas?0:37,heading*90+19,gas?0:23);
                        if(turret!=null)turret.localRotation=Quaternion.Euler(0,heading*90+31,0);
                        if(crane!=null)crane.localRotation=Quaternion.AngleAxis(heading%2==0?22:-22,Vector3.up)*craneRest;
                        Require(ActualHorizontalMeshRadius(model)<=size.VisualRadius+.0001f,"Rotated/tilted actual mesh remains within visual footprint: "+names[i]+" / "+heading+" / "+gas);
                    }
                    model.rotation=Quaternion.Euler(0,180,0);if(turret!=null)turret.localRotation=Quaternion.identity;if(crane!=null)crane.localRotation=craneRest;
                }
                controller.SetSurfaceFocus(new Vector3(20.5f,0,13.5f*(float)HexGrid.RowHeight));controller.SetOrbitCamera(0,65);Camera.main.orthographicSize=5;
                yield return null;yield return Capture(gas?"ground-traffic-models-gas.png":"ground-traffic-models-land.png");
                Require(captureFailure==null,captureFailure??"All six unit types plus courier and builder rendered at physical scale");
                if(!gas)
                {
                    var forward=controller.CreateVehicleGeometryProbe(UnitKind.Engineer,1,12,6);
                    var reverse=controller.CreateVehicleGeometryProbe(UnitKind.Engineer,1,12,6);
                    models.Add(forward);models.Add(reverse);
                    forward.position+=new Vector3(0,.02f,(float)GameSimulation.RoadLaneOffset);
                    reverse.position+=new Vector3(0,.02f,-(float)GameSimulation.RoadLaneOffset);
                    forward.rotation=Quaternion.Euler(0,90,0);reverse.rotation=Quaternion.Euler(0,-90,0);
                    Require(Vector3.Distance(forward.position,reverse.position)>ActualHorizontalMeshRadius(forward)+ActualHorizontalMeshRadius(reverse),
                        "Opposing full-size courier meshes fit side by side in the supply corridor");
                    controller.SetSurfaceFocus(new Vector3(12,0,6*(float)HexGrid.RowHeight));controller.SetOrbitCamera(0,75);
                    yield return null;
                    // Synchronous world render: the normal camera update would reset a
                    // temporary inspection zoom before an asynchronous screenshot.
                    Camera.main.orthographicSize=.8f;
                    controller.CaptureWorld(Path.Combine(outputDirectory,"supply-corridor-passing-models.png"));
                    Require(ScreenshotExists(Path.Combine(outputDirectory,"supply-corridor-passing-models.png")),"Full-size couriers captured without a permanent road surface");
                }
                foreach(var model in models)Destroy(model.gameObject);yield return null;
            }
            File.WriteAllLines(Path.Combine(outputDirectory,"ground-traffic-model-radii.txt"),report.ToArray());
        }

        private IEnumerator RunModelActivityChecks()
        {
            controller.StartMilitaryFixture();controller.SetSimulationPaused(true);controller.CloseContextPanel();
            var sim=controller.Simulation;var planet=sim.FindPlanet(1);
            foreach(var u in planet.Units)u.Ammunition=0;
            foreach(var b in planet.Buildings)b.Ammunition=0;
            var foot=planet.Units.Find(u=>u.OwnerId==1 && u.Kind==UnitKind.Infantry);
            var gate=planet.Buildings.Find(b=>b.Kind==BuildingKind.Gate && b.X==11);
            var depot=new BuildingState{Id=sim.State.NextEntityId++,PlanetId=1,Kind=BuildingKind.Warehouse,X=16,Y=16,Built=true};
            var destination=new BuildingState{Id=sim.State.NextEntityId++,PlanetId=1,Kind=BuildingKind.Warehouse,X=20,Y=16,Built=true};
            planet.Buildings.Add(depot);planet.Buildings.Add(destination);
            foot.X=gate.X;foot.Y=gate.Y;foot.GoalX=gate.X;foot.GoalY=gate.Y;
            yield return null;yield return null;
            var infantry=controller.UnitView(foot.Id);
            var terrain=infantry.parent.Find("Durchgehendes Relief").GetComponent<MeshCollider>();
            var tank=controller.UnitView(planet.Units.Find(u=>u.OwnerId==1 && u.Kind==UnitKind.Tank).Id);
            float worstGap=0;
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})
            {
                var sole=tank.TransformPoint(new Vector3(side*.25f,.031f,end*.25f));
                Require(terrain.Raycast(new Ray(sole+Vector3.up*2,Vector3.down),out var hit,4),"Track contact lies above rendered terrain");
                worstGap=Mathf.Max(worstGap,Mathf.Abs(hit.point.y-sole.y));
            }
            Require(worstGap<.018f && Vector3.Angle(tank.up,Vector3.up)>.1f,"Both track axles follow the terrain mesh slope within 0.018 world units");
            controller.SetOrbitCamera(-35,29);controller.SetSurfaceFocus(tank.position);Camera.main.orthographicSize=.8f;
            controller.CaptureWorld(Path.Combine(outputDirectory,"tank-track-ground-contact.png"));
            var gateView=Array.Find(infantry.parent.GetComponentsInChildren<RollingDoor>(),d=>d.name=="Torschranke" && Vector3.Distance(d.transform.position,infantry.position)<1);
            float closed=gateView.transform.localScale.y;
            var flag=infantry.parent.GetComponentInChildren<ColonyFlag>().GetComponent<MeshFilter>().sharedMesh;
            var flagStart=flag.vertices;
            controller.SetSimulationPaused(false);
            yield return new WaitForSecondsRealtime(.35f);
            Require(gateView.transform.localScale.y<closed*.15f,"Gate retracts for a permitted unit occupying its passage");
            var transverse=gateView.transform.parent.forward;
            controller.SetOrbitCamera(Mathf.Atan2(transverse.x,transverse.z)*Mathf.Rad2Deg-30,42);
            controller.SetSurfaceFocus(infantry.position);Camera.main.orthographicSize=.86f;
            controller.CaptureWorld(Path.Combine(outputDirectory,"gate-infantry-passage.png"));
            var flagEnd=flag.vertices;
            Require(flagStart[0]==flagEnd[0] && (flagStart[flagStart.Length-1]-flagEnd[flagEnd.Length-1]).magnitude>.0001f,"Flag ripples at its free edge while its mast edge stays pinned");
            var rig=infantry.GetComponent<InfantryStride>();
            sim.Enqueue(new SimCommand{Id=sim.NextCommandId(),IssuerId=1,PlanetId=1,ActorId=foot.Id,Kind=CommandKind.MoveUnit,X=14,Y=6});
            float until=Time.realtimeSinceStartup+4;bool gait=false;
            while(Time.realtimeSinceStartup<until)
            {
                yield return null;
                if(!gait && Quaternion.Angle(rig.Hips[0].localRotation,rig.Hips[1].localRotation)>12 &&
                    Quaternion.Angle(rig.Knees[0].localRotation,Quaternion.identity)>5)
                {
                    gait=true;controller.SetSurfaceFocus(infantry.position);Camera.main.orthographicSize=.7f;
                    controller.CaptureWorld(Path.Combine(outputDirectory,"infantry-stride.png"));
                }
                if(gait && gateView.transform.localScale.y>closed*.97f)break;
            }
            Require(gait,"Walking infantry alternates hips and flexes the knee during a move order");
            Require(gateView.transform.localScale.y>closed*.97f,"Gate closes again after the unit passes");
            // An empty courier returning after unloading still needs both loading bays.
            var courier=new ShipmentState{Id=sim.State.NextEntityId++,SourceId=destination.Id,TargetId=depot.Id,
                Phase=ShipmentPhase.Returning,Traffic=new GroundTrafficState{Docked=true}};
            for(int x=16;x<=20;x++)courier.Path.Add(new GridCell(x,16));
            planet.Shipments.Add(courier);
            var doors=infantry.parent.GetComponentsInChildren<RollingDoor>();
            var leaving=Array.Find(doors,d=>Mathf.Abs(d.transform.position.x-16)<.36f && Mathf.Abs(d.transform.position.z-16*(float)HexGrid.RowHeight)<.5f);
            var arriving=Array.Find(doors,d=>Mathf.Abs(d.transform.position.x-20)<.36f && Mathf.Abs(d.transform.position.z-16*(float)HexGrid.RowHeight)<.5f);
            float shutter=leaving.transform.localScale.y;bool departed=false,arrived=false;
            until=Time.realtimeSinceStartup+4;
            while(Time.realtimeSinceStartup<until)
            {
                yield return null;
                departed|=leaving.transform.localScale.y<shutter*.5f;arrived|=arriving.transform.localScale.y<shutter*.5f;
                if(departed && arrived && !planet.Shipments.Contains(courier) && arriving.transform.localScale.y>shutter*.98f)break;
            }
            Require(departed && arrived && leaving.transform.localScale.y>shutter*.98f && arriving.transform.localScale.y>shutter*.98f,
                "Departure and arrival open the depot shutters and both close after the courier clears them");
            controller.SetSimulationPaused(true);
        }
        private static float ActualHorizontalMeshRadius(Transform root)
        {
            float radius=0;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null)
                foreach(var vertex in filter.sharedMesh.vertices)
                {var delta=filter.transform.TransformPoint(vertex)-root.position;radius=Mathf.Max(radius,new Vector2(delta.x,delta.z).magnitude);}
            return radius;
        }
    }
}
