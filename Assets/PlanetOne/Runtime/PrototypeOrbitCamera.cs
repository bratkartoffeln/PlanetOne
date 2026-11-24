using UnityEngine;
namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private float cameraYaw,cameraPitch=50;
        private Vector3 spaceFocus;
        private Vector2 spacePanStart, spacePanLast;
        private bool spacePanCaptured, spacePanDragged, spacePanCancelledTarget;
        public float CameraYaw {get{return cameraYaw;}}
        public Vector3 SpaceFocus {get{return spaceFocus;}}
        internal Transform UnitView(long id){return unitViews[id];}
        internal Transform ShipView(long id){return shipViews[id];}
        public void SetOrbitCamera(float yaw,float pitch)
        {cameraYaw=Mathf.Repeat(yaw,360);cameraPitch=Mathf.Clamp(pitch,30,80);UpdateCamera();}
        private void HandleOrbitCamera()
        {
            float turn=Input.GetKey(KeyCode.Q)?-1:Input.GetKey(KeyCode.E)?1:0;
            if(turn!=0)SetOrbitCamera(cameraYaw+turn*65*Time.unscaledDeltaTime,cameraPitch);
            if(Input.GetMouseButton(2) && IsWorldPoint(Input.mousePosition))
                SetOrbitCamera(cameraYaw+Input.GetAxis("Mouse X")*3,cameraPitch-Input.GetAxis("Mouse Y")*2);
            if(Input.GetKeyDown(KeyCode.Home))ResetCameraView();
        }
        public void ResetCameraView()
        {if(openPlanetId==0)spaceFocus=Vector3.zero;CancelSpacePan();SetOrbitCamera(0,50);}
        public void SetSpaceFocus(Vector3 focus)
        {
            spaceFocus=new Vector3(Mathf.Clamp(focus.x,-250,250),0,Mathf.Clamp(focus.z,-250,250));
            UpdateCamera();
        }
        public bool BeginSpacePan(Vector2 screenPoint)
        {
            if(openPlanetId!=0 || !IsWorldPoint(screenPoint))return false;
            spacePanCaptured=true;spacePanDragged=false;spacePanStart=spacePanLast=screenPoint;
            spacePanCancelledTarget=spaceMoveTargetMode;spaceMoveTargetMode=false;
            return true;
        }
        public void ContinueSpacePan(Vector2 screenPoint)
        {
            if(settingsOpen || menuOpen){CancelSpacePan();return;}
            if(!spacePanCaptured)return;
            if(!spacePanDragged && Vector2.Distance(screenPoint,spacePanStart)<6)return;
            spacePanDragged=true;
            var plane=new Plane(Vector3.up,Vector3.zero);
            var before=sceneCamera.ScreenPointToRay(spacePanLast);var after=sceneCamera.ScreenPointToRay(screenPoint);
            float beforeDistance,afterDistance;
            if(plane.Raycast(before,out beforeDistance) && plane.Raycast(after,out afterDistance))
                SetSpaceFocus(spaceFocus+before.GetPoint(beforeDistance)-after.GetPoint(afterDistance));
            spacePanLast=screenPoint;
        }
        public bool EndSpacePan(Vector2 screenPoint)
        {
            if(!spacePanCaptured)return false;
            ContinueSpacePan(screenPoint);
            bool click=!spacePanDragged && !spacePanCancelledTarget;
            CancelSpacePan();
            return click && RequestShipMoveAtScreen(screenPoint);
        }
        private void CancelSpacePan(){spacePanCaptured=false;spacePanDragged=false;spacePanCancelledTarget=false;}
        private bool HandleSpacePanInput()
        {
            if(settingsOpen || menuOpen || openPlanetId!=0){CancelSpacePan();return false;}
            if(Input.GetMouseButtonDown(1))return BeginSpacePan(Input.mousePosition);
            if(!spacePanCaptured)return false;
            if(Input.GetMouseButtonUp(1)){EndSpacePan(Input.mousePosition);return true;}
            if(Input.GetMouseButton(1)){ContinueSpacePan(Input.mousePosition);return true;}
            CancelSpacePan();return false;
        }
        private static void UpdateMotionPose(Transform view,Vector3 position,float modelHeading)
        {
            var facing=view.GetComponent<MotionFacing>();if(facing==null)facing=view.gameObject.AddComponent<MotionFacing>();
            facing.Move(position,modelHeading,Vector3.up);
        }
        private void UpdateGroundPose(Transform view,double x,double y)
        {
            var position=SurfacePosition(x,0.035f,y);
            float dx=ReliefHeight(position.x+0.12f,position.z)-ReliefHeight(position.x-0.12f,position.z);
            float dz=ReliefHeight(position.x,position.z+0.12f)-ReliefHeight(position.x,position.z-0.12f);
            var normal=new Vector3(-dx,0.24f,-dz).normalized;
            var facing=view.GetComponent<MotionFacing>();if(facing==null)facing=view.gameObject.AddComponent<MotionFacing>();
            facing.Move(position,0,normal);
        }
    }
    public sealed class MotionFacing:MonoBehaviour
    {
        private bool initialized;
        private Vector3 heading=Vector3.forward;
        public void Move(Vector3 position,float modelHeading,Vector3 normal)
        {
            Vector3 direction=position-transform.position;direction.y=0;
            if(initialized && direction.sqrMagnitude>0.0000001f)heading=direction.normalized;
            transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(heading,normal),normal)*Quaternion.Euler(0,modelHeading,0);
            transform.position=position;initialized=true;
        }
    }
}
