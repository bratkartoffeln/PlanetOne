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
        private IEnumerator RunPlanetFocusChecks()
        {
            controller.StartPlanetEnvironmentFixture();controller.SetSimulationPaused(true);
            controller.SetMenuVisible(false);controller.SetSettingsVisible(false);
            controller.SetInterfaceLayout(InterfaceLayout.OrbitalBridge);controller.SetView(0);
            controller.SetSpaceFocus(new Vector3(-25,0,-18));yield return null;
            string state=SaveCodec.ToJson(controller.Simulation.State);
            var worlds=controller.Simulation.State.Planets;
            int first=worlds[0].Id,second=worlds[1].Id;
            Require(!controller.FocusPlanet(int.MaxValue)&&controller.FocusedPlanetId==0,"Unknown focus targets are rejected without changing the camera mode");
            Vector3 start=controller.SpaceFocus;
            Require(controller.NavigateToPlanet(first)&&controller.OpenPlanetId==0&&controller.SelectedPlanetId==first,"Sidebar navigation selects a planet in the system");
            Require(Vector3.Distance(start,controller.SpaceFocus)<.001f&&controller.IsPlanetFocusTransitioning,"Planet selection starts a smooth camera flight without snapping");
            yield return new WaitForSecondsRealtime(.35f);
            var orbit=controller.Simulation.GetOrbitPosition(first);Vector3 destination=PrototypeController.SpacePosition(orbit.X,orbit.Y);
            Require(Vector3.Distance(start,controller.SpaceFocus)>.01f&&Vector3.Distance(destination,controller.SpaceFocus)>.01f,"Camera visits an intermediate position on the approach");
            yield return Capture("planet-focus-approach.png");Require(captureFailure==null,captureFailure??"Planet focus approach captured");
            float deadline=Time.realtimeSinceStartup+4;
            while(controller.IsPlanetFocusTransitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(!controller.IsPlanetFocusTransitioning&&controller.FocusedPlanetId==first&&Vector3.Distance(controller.SpaceFocus,destination)<.001f,"Planet approach completes in persistent focus mode");
            Require(Vector2.Distance(Camera.main.WorldToScreenPoint(destination),controller.WorldViewport.center)<1,"Tracked planet is centered in the actual world viewport");
            Require(SaveCodec.ToJson(controller.Simulation.State)==state,"Camera navigation issues no simulation orders");
            Vector3 beforeOrbit=controller.SpaceFocus;AdvanceSeconds(60);yield return null;yield return null;
            orbit=controller.Simulation.GetOrbitPosition(first);destination=PrototypeController.SpacePosition(orbit.X,orbit.Y);
            Require(Vector3.Distance(beforeOrbit,destination)>.001f&&Vector3.Distance(controller.SpaceFocus,destination)<.001f,"Focus follows the moving Kepler orbit without changing its simulation");
            yield return Capture("planet-focus-tracking.png");Require(captureFailure==null,captureFailure??"Moving planet remains centered");
            controller.SetSpaceFocus(controller.SpaceFocus+Vector3.right*2);
            Require(controller.FocusedPlanetId==0&&!controller.IsPlanetFocusTransitioning,"Manual pan releases persistent planet focus");
            Vector3 detached=controller.SpaceFocus;AdvanceSeconds(5);yield return null;
            Require(Vector3.Distance(detached,controller.SpaceFocus)<.001f,"Released camera no longer follows orbital movement");
            Require(controller.FocusPlanet(first,false),"Center-planet command can establish an immediate persistent focus");
            controller.SetOrbitCamera(controller.CameraYaw+15,50);
            Require(controller.FocusedPlanetId==0,"Manual camera rotation releases planet focus");
            Require(controller.FocusPlanet(first,false),"Focus can be restored after manual rotation");controller.ResetCameraView();
            Require(controller.FocusedPlanetId==0&&controller.SpaceFocus.sqrMagnitude<.001f,"Home returns to the star and releases tracking");
            controller.SetView(first);yield return null;
            string beforeReturn=SaveCodec.ToJson(controller.Simulation.State);
            Require(controller.NavigateToPlanet(second)&&controller.IsViewTransitioning&&controller.OpenPlanetId==first,"Sidebar selection from a surface starts the atmospheric return flight");
            deadline=Time.realtimeSinceStartup+7;
            while((controller.IsViewTransitioning||controller.IsPlanetFocusTransitioning)&&Time.realtimeSinceStartup<deadline)yield return null;
            orbit=controller.Simulation.GetOrbitPosition(second);destination=PrototypeController.SpacePosition(orbit.X,orbit.Y);
            Require(!controller.IsViewTransitioning&&!controller.IsPlanetFocusTransitioning&&controller.OpenPlanetId==0&&controller.FocusedPlanetId==second&&Vector3.Distance(controller.SpaceFocus,destination)<.001f,"Surface sidebar returns to the system, approaches the other planet and keeps it focused");
            Require(SaveCodec.ToJson(controller.Simulation.State)==beforeReturn,"Returning and focusing another world preserves the complete paused simulation");
            yield return Capture("planet-focus-from-surface.png");Require(captureFailure==null,captureFailure??"System navigation from another surface captured");
            foreach(var world in worlds)
            {
                var samples=CapturePlanetMeridian(controller.CelestialView(world.Id).GetComponent<Renderer>().sharedMaterial,world.Kind);
                Require(samples[0]<.1f,"Rendered longitude seam has no abrupt color jump: "+world.Kind+" ("+samples[0].ToString("F4",CultureInfo.InvariantCulture)+")");
                Require(samples[1]>.01f,"Seam correction retains actual surface texture contrast: "+world.Kind);
            }
            controller.CancelPlanetFocus();
        }

        // Render the true shader with the cut meridian facing the camera, not a CPU
        // reimplementation of the shader. The two center columns straddle its cut.
        private float[] CapturePlanetMeridian(Material source,PlanetKind kind)
        {
            var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.name="Isolated planet meridian check";
            sphere.transform.position=Vector3.one*2400;sphere.gameObject.layer=30;
            var material=new Material(source);sphere.GetComponent<Renderer>().sharedMaterial=material;
            material.SetVector("_SunDirection",new Vector4(0,0,-1,0));
            var cameraObject=new GameObject("Meridian probe camera");var camera=cameraObject.AddComponent<Camera>();
            var target=RenderTexture.GetTemporary(384,384,24);var previous=RenderTexture.active;Texture2D image=null;
            try
            {
                camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                camera.orthographic=true;camera.orthographicSize=.56f;camera.aspect=1;camera.nearClipPlane=.1f;camera.farClipPlane=5;
                camera.transform.position=sphere.transform.position+new Vector3(0,0,-2);camera.transform.LookAt(sphere.transform.position);
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                image=new Texture2D(384,384,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,384,384),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(outputDirectory,"planet-meridian-"+kind+".png"),image.EncodeToPNG());
                float seam=0,min=1,max=0;int count=0;
                for(int y=88;y<296;y++)
                {
                    Color a=image.GetPixel(191,y),b=image.GetPixel(192,y);
                    seam+=(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b))/3;count++;
                    float value=(a.r+a.g+a.b)/3;min=Mathf.Min(min,value);max=Mathf.Max(max,value);
                }
                float average=seam/count;
                File.AppendAllText(Path.Combine(outputDirectory,"planet-meridian-measurements.txt"),kind+": mean adjacent RGB difference="+average.ToString("F6",CultureInfo.InvariantCulture)+", surface contrast="+(max-min).ToString("F6",CultureInfo.InvariantCulture)+Environment.NewLine);
                return new[]{average,max-min};
            }
            finally
            {
                RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
                sphere.SetActive(false);Destroy(sphere);Destroy(cameraObject);Destroy(material);if(image!=null)Destroy(image);
            }
        }
    }
}
