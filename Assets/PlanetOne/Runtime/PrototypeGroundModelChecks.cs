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
        private IEnumerator RunGroundModelRadiusChecks()
        {
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
                    Require(size!=null && yawRadius>0 && envelope<=size.PhysicalRadius+.00001f,"Real "+(gas?"gas":"land")+" model including moving parts fits its core body: "+names[i]);
                    if(!gas&&(names[i]=="Kurier"||names[i]=="Baufahrzeug"))
                        Require(envelope<GameSimulation.RoadLaneOffset&&envelope+GameSimulation.RoadLaneOffset<GameSimulation.RoadHalfWidth,
                            "Narrow-road model leaves space for opposing traffic and stays inside its lane: "+names[i]);
                    report.Add((gas?"Gas":"Land")+" / "+names[i]+": scale "+size.PreferredScale.ToString("F5",CultureInfo.InvariantCulture)+" -> "+size.AppliedScale.ToString("F5",CultureInfo.InvariantCulture)+
                        "; envelope "+size.BeforeRadius.ToString("F5",CultureInfo.InvariantCulture)+" -> "+envelope.ToString("F5",CultureInfo.InvariantCulture)+"; yaw="+yawRadius.ToString("F5",CultureInfo.InvariantCulture)+"; core="+size.PhysicalRadius.ToString("F5",CultureInfo.InvariantCulture));
                    var turret=model.Find("Turret");var crane=model.Find("Kranarm");Quaternion craneRest=crane==null?Quaternion.identity:crane.localRotation;
                    for(int heading=0;heading<4;heading++)
                    {
                        model.rotation=Quaternion.Euler(gas?0:37,heading*90+19,gas?0:23);
                        if(turret!=null)turret.localRotation=Quaternion.Euler(0,heading*90+31,0);
                        if(crane!=null)crane.localRotation=Quaternion.AngleAxis(heading%2==0?22:-22,Vector3.up)*craneRest;
                        Require(ActualHorizontalMeshRadius(model)<=size.PhysicalRadius+.0001f,"Rotated/tilted actual mesh remains within physical footprint: "+names[i]+" / "+heading+" / "+gas);
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
                        "Opposing real courier meshes fit side by side on the restored road width");
                    controller.SetSurfaceFocus(new Vector3(12,0,6*(float)HexGrid.RowHeight));controller.SetOrbitCamera(0,75);
                    yield return null;
                    // Synchronous world render: the normal camera update would reset a
                    // temporary inspection zoom before an asynchronous screenshot.
                    Camera.main.orthographicSize=.45f;
                    controller.CaptureWorld(Path.Combine(outputDirectory,"narrow-road-passing-models.png"));
                    Require(ScreenshotExists(Path.Combine(outputDirectory,"narrow-road-passing-models.png")),"Narrow two-lane road and opposing courier models captured close up");
                }
                foreach(var model in models)Destroy(model.gameObject);yield return null;
            }
            File.WriteAllLines(Path.Combine(outputDirectory,"ground-traffic-model-radii.txt"),report.ToArray());
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
