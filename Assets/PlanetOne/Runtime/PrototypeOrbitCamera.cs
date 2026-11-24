using UnityEngine;
namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private float cameraYaw,cameraPitch=50;
        public float CameraYaw {get{return cameraYaw;}}
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
            if(Input.GetKeyDown(KeyCode.Home))SetOrbitCamera(0,50);
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
