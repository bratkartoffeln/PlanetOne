using System;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private bool viewFlightActive,viewFlightSwitched,switchingFlight;
        private int flightDestination,flightPlanet;
        private float flightStarted,flightStartZoom,returnSpaceZoom=24,flightSurfaceZoom=6.2f;
        private float flightBuildStarted;
        private Vector3 flightStartFocus,returnSpaceFocus,flightSurfaceFocus;
        private const float FlightDuration=1.8f,FlightHandoff=.55f;
        public bool IsViewTransitioning {get{return viewFlightActive;}}
        public double LastFlightSurfaceBuildMilliseconds {get;private set;}
        public double LastFlightBuildSliceMilliseconds {get{return surfaceBuildSliceMilliseconds;}}
        public float ViewTransitionProgress {get{return viewFlightActive?Mathf.Clamp01((Time.unscaledTime-flightStarted)/FlightDuration):1;}}
        public bool BeginViewFlight(int destination)
        {
            if(viewFlightActive || destination==openPlanetId)return false;
            if(destination!=0 && (simulation.FindPlanet(destination)==null || !simulation.IsPlanetKnown(PlayerFactionId,destination)))return false;
            if(openPlanetId!=0 && destination!=0){SetView(destination);return true;}
            if(destination!=0)CancelPlanetFocus();
            CloseCommandMenu();CancelPlacement();CancelSpacePan();militaryTargetMode=null;spaceMoveTargetMode=false;
            flightDestination=destination;flightPlanet=destination==0?openPlanetId:destination;
            flightStartFocus=openPlanetId==0?spaceFocus:surfaceFocus;flightStartZoom=openPlanetId==0?spaceZoom:surfaceZoom;
            if(destination!=0){returnSpaceFocus=spaceFocus;returnSpaceZoom=spaceZoom;}
            viewFlightSwitched=false;viewFlightActive=true;flightStarted=Time.unscaledTime;surfaceBuildSliceMilliseconds=0;return true;
        }
        private static float FlightEase(float t){t=Mathf.Clamp01(t);return t*t*t*(t*(t*6-15)+10);}
        private Vector3 FlightPlanetPosition()
        {Transform view;return planetViews.TryGetValue(flightPlanet,out view)?view.position:spaceFocus;}
        private float FlightCloseZoom()
        {Transform view;return planetViews.TryGetValue(flightPlanet,out view)?Mathf.Max(.3f,view.localScale.x*.3f):.5f;}
        private void UpdateViewFlight()
        {
            if(!viewFlightActive)return;float t=ViewTransitionProgress;
            if(t>=FlightHandoff && !viewFlightSwitched)
            {
                flightBuildStarted=Time.realtimeSinceStartup;
                buildSurfaceInFrames=true;switchingFlight=true;SetView(flightDestination);switchingFlight=false;buildSurfaceInFrames=false;viewFlightSwitched=true;
                flightSurfaceFocus=surfaceFocus;flightSurfaceZoom=surfaceZoom;
            }
            if(viewFlightSwitched && surfaceBuildSteps!=null)
            {
                // Keep rendering the opaque atmospheric handoff while bounded preparation batches run.
                // The shared simulation continues normally; only the presentation waits for its geometry.
                flightStarted=Time.unscaledTime-FlightDuration*FlightHandoff;
                LastFlightSurfaceBuildMilliseconds=(Time.realtimeSinceStartup-flightBuildStarted)*1000;
                return;
            }
            if(!viewFlightSwitched)
            {
                float p=FlightEase(t/FlightHandoff);
                if(flightDestination!=0){spaceFocus=Vector3.Lerp(flightStartFocus,FlightPlanetPosition(),p);spaceZoom=Mathf.Lerp(flightStartZoom,FlightCloseZoom(),p);}
                else {surfaceFocus=flightStartFocus;surfaceZoom=Mathf.Lerp(flightStartZoom,Mathf.Max(11,flightStartZoom*1.4f),p);}
            }
            else
            {
                float p=FlightEase((t-FlightHandoff)/(1-FlightHandoff));
                if(flightDestination!=0){surfaceFocus=flightSurfaceFocus;surfaceZoom=Mathf.Lerp(Mathf.Max(11,flightSurfaceZoom*1.4f),flightSurfaceZoom,p);}
                else {spaceFocus=Vector3.Lerp(FlightPlanetPosition(),returnSpaceFocus,p);spaceZoom=Mathf.Lerp(FlightCloseZoom(),returnSpaceZoom,p);}
            }
            UpdateCamera();if(t>=1)viewFlightActive=false;
        }
        private void FinishViewFlight()
        {
            if(!viewFlightActive)return;flightStarted=Time.unscaledTime-FlightDuration;UpdateViewFlight();
        }
        private void DrawViewFlight(float width,float height)
        {
            if(!viewFlightActive)return;
            float t=ViewTransitionProgress,veil=Mathf.Clamp01(1-Mathf.Abs(t-FlightHandoff)/.12f);
            if(veil>0)Fill(new Rect(0,0,width,height),new Color(.06f,.11f,.15f,FlightEase(veil)));
            var planet=simulation.FindPlanet(flightPlanet);
            string label=(flightDestination==0?"Rückkehr ins Sonnensystem":"Anflug auf "+(planet==null?"Planet":planet.Name));
            float w=bodyStyle.CalcSize(new GUIContent(label)).x+48;
            w+=48;
            var panel=new Rect((width-w)/2,height-BottomHeight-65,w,48);DrawPanel(panel);LabelFitted(new Rect(panel.x+24,panel.y+10,w-48,TextLineHeight),label,bodyStyle);
        }
    }
}
