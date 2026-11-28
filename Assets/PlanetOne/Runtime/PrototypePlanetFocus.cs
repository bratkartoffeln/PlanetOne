using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private int focusedPlanetId,pendingPlanetFocusId;
        private bool planetFocusAnimating;
        private float planetFocusStarted;
        private Vector3 planetFocusFrom;
        private const float PlanetFocusDuration=.9f;
        public int FocusedPlanetId {get{return focusedPlanetId;}}
        public bool IsPlanetFocusTransitioning {get{return planetFocusAnimating || pendingPlanetFocusId!=0;}}

        // The selected object and the tracked camera target are presentation state only.
        public bool FocusPlanet(int id,bool animate=true)
        {
            if(simulation.FindPlanet(id)==null || !simulation.IsPlanetKnown(PlayerFactionId,id) || viewFlightActive)return false;
            if(openPlanetId!=0)return NavigateToPlanet(id);
            if(!SelectPlanet(id))return false;
            CancelSpacePan();focusedPlanetId=id;pendingPlanetFocusId=0;
            planetFocusFrom=spaceFocus;planetFocusStarted=Time.unscaledTime;planetFocusAnimating=animate;
            UpdatePlanetFocus();return true;
        }

        public bool NavigateToPlanet(int id)
        {
            if(simulation.FindPlanet(id)==null || !simulation.IsPlanetKnown(PlayerFactionId,id) || viewFlightActive)return false;
            if(openPlanetId==0)return FocusPlanet(id);
            // Return through the existing atmospheric flight before approaching the other world.
            if(!BeginViewFlight(0))return false;
            SelectPlanet(id);pendingPlanetFocusId=id;focusedPlanetId=0;planetFocusAnimating=false;return true;
        }

        public void CancelPlanetFocus()
        {focusedPlanetId=0;pendingPlanetFocusId=0;planetFocusAnimating=false;}
        private void ResetPlanetFocus(){CancelPlanetFocus();}

        private void UpdatePlanetFocus()
        {
            if(viewFlightActive)return;
            if(pendingPlanetFocusId!=0)
            {
                int pending=pendingPlanetFocusId;pendingPlanetFocusId=0;
                if(openPlanetId==0)FocusPlanet(pending);
                return;
            }
            if(focusedPlanetId==0)return;
            if(openPlanetId!=0 || simulation.FindPlanet(focusedPlanetId)==null || !simulation.IsPlanetKnown(PlayerFactionId,focusedPlanetId))
            {CancelPlanetFocus();return;}
            var position=simulation.GetOrbitPosition(focusedPlanetId);
            Vector3 target=SpacePosition(position.X,position.Y);
            float progress=planetFocusAnimating?Mathf.Clamp01((Time.unscaledTime-planetFocusStarted)/PlanetFocusDuration):1;
            spaceFocus=Vector3.Lerp(planetFocusFrom,target,FlightEase(progress));
            if(progress>=1)planetFocusAnimating=false;
            UpdateCamera();
        }
    }
}
