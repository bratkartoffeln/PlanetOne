using UnityEngine;
namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private const float DefaultSurfaceYaw=-60;
        // The pointy-top grid is authored with its first vertex at 30 degrees.
        private const float HexHeadingOffset=30;
        private float systemYaw,systemPitch=50,surfaceYaw=DefaultSurfaceYaw,surfacePitch=50;
        private float cameraYaw
        {
            get{return openPlanetId==0?systemYaw:surfaceYaw;}
            set{if(openPlanetId==0)systemYaw=value;else surfaceYaw=value;}
        }
        private float cameraPitch
        {
            get{return openPlanetId==0?systemPitch:surfacePitch;}
            set{if(openPlanetId==0)systemPitch=value;else surfacePitch=value;}
        }
        private float WorldCameraYaw {get{return openPlanetId==0?systemYaw:surfaceYaw+HexHeadingOffset;}}
        private Vector3 spaceFocus;
        private Vector2 spacePanStart, spacePanLast;
        private bool spacePanCaptured, spacePanDragged, spacePanCancelledTarget,spacePanContextMenu;
        public float CameraYaw {get{return cameraYaw;}}
        public Vector3 SpaceFocus {get{return spaceFocus;}}
        public Vector3 SurfaceFocus {get{return surfaceFocus;}}
        internal Transform UnitView(long id){return unitViews[id];}
        internal Transform ShipView(long id){return shipViews[id];}
        public void SetOrbitCamera(float yaw,float pitch)
        {CancelPlanetFocus();cameraYaw=Mathf.Repeat(yaw,360);cameraPitch=Mathf.Clamp(pitch,30,80);UpdateCamera();}
        private void HandleOrbitCamera()
        {
            float turn=Input.GetKey(KeyCode.Q)?-1:Input.GetKey(KeyCode.E)?1:0;
            if(turn!=0)SetOrbitCamera(cameraYaw+turn*65*Time.unscaledDeltaTime,cameraPitch);
            if(Input.GetMouseButton(2) && IsWorldPoint(Input.mousePosition))
                SetOrbitCamera(cameraYaw+Input.GetAxis("Mouse X")*3,cameraPitch-Input.GetAxis("Mouse Y")*2);
            if(Input.GetKeyDown(KeyCode.Home))ResetCameraView();
        }
        public void ResetCameraView()
        {if(openPlanetId==0)spaceFocus=Vector3.zero;else SetSurfaceFocus(SurfacePosition(8,0,6));CancelSpacePan();SetOrbitCamera(openPlanetId==0?0:DefaultSurfaceYaw,50);}
        public void SetSurfaceFocus(Vector3 focus)
        {
            var p=simulation.FindPlanet(openPlanetId);if(p==null)return;
            float x=Mathf.Clamp(focus.x,0,p.Width-1),z=Mathf.Clamp(focus.z,0,(p.Height-1)*.8660254f);
            surfaceFocus=new Vector3(x,ReliefHeight(x,z),z);UpdateCamera();
        }
        public void SetSpaceFocus(Vector3 focus)
        {
            CancelPlanetFocus();
            spaceFocus=new Vector3(Mathf.Clamp(focus.x,-250,250),Mathf.Clamp(focus.y,-8,8),Mathf.Clamp(focus.z,-250,250));
            UpdateCamera();
        }
        public bool BeginSpacePan(Vector2 screenPoint,bool contextMenu=false)
        {
            if(openPlanetId!=0 || !IsWorldPoint(screenPoint))return false;
            spacePanCaptured=true;spacePanDragged=false;spacePanStart=spacePanLast=screenPoint;
            spacePanContextMenu=contextMenu;
            spacePanCancelledTarget=spaceMoveTargetMode;spaceMoveTargetMode=false;
            return true;
        }
        public bool BeginSurfacePan(Vector2 screenPoint,bool contextMenu=false)
        {
            if(openPlanetId==0 || !IsWorldPoint(screenPoint))return false;
            spacePanCaptured=true;spacePanDragged=false;spacePanStart=spacePanLast=screenPoint;
            spacePanContextMenu=contextMenu;
            spacePanCancelledTarget=placement.HasValue || placingRoad || removingRoad || planningSupply || militaryTargetMode.HasValue;
            return true;
        }
        public void ContinueSpacePan(Vector2 screenPoint)
        {
            if(settingsOpen || menuOpen){CancelSpacePan();return;}
            if(!spacePanCaptured)return;
            if(!spacePanDragged && Vector2.Distance(screenPoint,spacePanStart)<6)return;
            spacePanDragged=true;
            var plane=new Plane(Vector3.up,openPlanetId==0?Vector3.zero:surfaceFocus);
            var before=sceneCamera.ScreenPointToRay(spacePanLast);var after=sceneCamera.ScreenPointToRay(screenPoint);
            float beforeDistance,afterDistance;
            if(plane.Raycast(before,out beforeDistance) && plane.Raycast(after,out afterDistance))
            {
                Vector3 delta=before.GetPoint(beforeDistance)-after.GetPoint(afterDistance);
                if(openPlanetId==0)SetSpaceFocus(spaceFocus+delta);else SetSurfaceFocus(surfaceFocus+delta);
            }
            spacePanLast=screenPoint;
        }
        public bool EndSpacePan(Vector2 screenPoint)
        {
            if(!spacePanCaptured)return false;
            ContinueSpacePan(screenPoint);
            bool click=!spacePanDragged && !spacePanCancelledTarget;
            bool contextMenu=spacePanContextMenu;
            if(openPlanetId!=0 && !spacePanDragged && spacePanCancelledTarget){CancelPlacement();militaryTargetMode=null;}
            CancelSpacePan();
            return click && (contextMenu?OpenContextMenuAtScreen(screenPoint):IssueDefaultOrderAtScreen(screenPoint));
        }
        private void CancelSpacePan(){spacePanCaptured=false;spacePanDragged=false;spacePanCancelledTarget=false;spacePanContextMenu=false;}
        private bool HandleSpacePanInput()
        {
            if(settingsOpen || menuOpen){CancelSpacePan();return false;}
            if(Input.GetMouseButtonDown(1))
            {bool contextMenu=Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl);return openPlanetId==0?BeginSpacePan(Input.mousePosition,contextMenu):BeginSurfacePan(Input.mousePosition,contextMenu);}
            if(!spacePanCaptured)return false;
            if(Input.GetMouseButtonUp(1)){EndSpacePan(Input.mousePosition);return true;}
            if(Input.GetMouseButton(1)){ContinueSpacePan(Input.mousePosition);return true;}
            CancelSpacePan();return false;
        }
        private void UpdateMotionPose(Transform view,Vector3 position,float modelHeading)
        {
            var facing=view.GetComponent<MotionFacing>();if(facing==null)facing=view.gameObject.AddComponent<MotionFacing>();
            facing.MoveTick(position,modelHeading,Vector3.up,simulation.State.Tick,MovementInterpolation);
        }
        private float MovementInterpolation {get{return simulation.State.Paused?1:Mathf.Clamp01((float)(simulation.State.Accumulator/PlanetOne.Core.Definitions.TickSeconds));}}
        private void UpdateGroundPose(Transform view,PlanetOne.Core.UnitState unit)
        {
            var point=PlanetOne.Core.GameSimulation.GroundUnitPosition(unit);
            UpdateSurfaceMotion(view,new Vector3((float)point.X,ReliefHeight((float)point.X,(float)point.Y)+(AtmosphereSurface?.55f:.035f),(float)point.Y));
            if(!AtmosphereSurface)
            {
                bool onFoot=unit.Kind==PlanetOne.Core.UnitKind.Infantry || unit.Kind==PlanetOne.Core.UnitKind.AntiTank;
                SeatOnTerrain(view,onFoot);
                if(onFoot)view.GetComponent<InfantryStride>().Pose(view.position,AnimationDelta);
            }
        }
    }
    public sealed class MotionFacing:MonoBehaviour
    {
        private bool initialized;
        private Vector3 heading=Vector3.forward,from,target;
        private Quaternion headingRotation;
        private long sampleTick=long.MinValue;
        public void MoveTick(Vector3 position,float modelHeading,Vector3 normal,long tick,float alpha)
        {
            if(!initialized||sampleTick==long.MinValue||tick<sampleTick||tick-sampleTick>1)
            {
                if(!initialized)headingRotation=transform.rotation;
                var displacement=position-transform.position;displacement.y=0;
                if(initialized&&displacement.sqrMagnitude>.0000001f)heading=displacement.normalized;
                from=target=position;initialized=true;
            }
            else if(tick!=sampleTick){from=target;target=position;}
            else if((position-target).sqrMagnitude>.00000001f){from=target=position;}
            sampleTick=tick;
            var direction=target-from;direction.y=0;if(direction.sqrMagnitude>.0000001f)heading=direction.normalized;
            transform.position=Vector3.Lerp(from,target,Mathf.Clamp01(alpha));
            ApplyRotation(modelHeading,normal);
        }
        private void ApplyRotation(float modelHeading,Vector3 normal)
        {
            Vector3 projected=Vector3.ProjectOnPlane(heading,normal);if(projected.sqrMagnitude<.00001f)projected=Vector3.forward;
            Quaternion rotation=Quaternion.LookRotation(projected,normal)*Quaternion.Euler(0,modelHeading,0);
            headingRotation=Quaternion.Slerp(headingRotation,rotation,1-Mathf.Exp(-16*Time.unscaledDeltaTime));
            transform.rotation=headingRotation;
        }
    }
}
