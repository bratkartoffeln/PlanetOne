using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private void BuildGroundUnitModel(Transform root,UnitKind kind,Color color)
        {
            BuildVehicleModel(root,kind,color);
            FitVehicleBody(root,(float)GameSimulation.GroundUnitRadius(kind),1);
        }
        private void BuildGroundLogisticsModel(Transform root,ShipmentState shipment)
        {
            BuildVehicleModel(root,UnitKind.Engineer,Amber,!GameSimulation.IsConstructionVehicle(shipment));
            FitVehicleBody(root,(float)GameSimulation.GroundCourierRadius(shipment),AtmosphereSurface?.8f:.42f);
        }
        private void FitVehicleBody(Transform root,float physicalRadius,float preferredScale)
        {
            root.localScale=Vector3.one*preferredScale;
            // Surface vehicles tilt on relief. A spherical envelope also contains every such pitch/roll;
            // atmospheric craft stay level and need only the planar yaw envelope.
            float radius=ModelMotionRadius(root,true,!AtmosphereSurface);
            if(radius>.000001f)root.localScale*=Mathf.Min(1,physicalRadius*.98f/radius);
            var size=root.gameObject.AddComponent<VehicleModelSize>();
            size.PreferredScale=preferredScale;size.AppliedScale=root.localScale.x;
            size.PhysicalRadius=physicalRadius;size.BeforeRadius=radius;
            size.AfterRadius=radius*(size.AppliedScale/preferredScale);
        }

        internal Transform CreateVehicleGeometryProbe(UnitKind kind,int logisticsKind,int x,int y)
        {
            var root=new GameObject("Verkehrsprüfung · "+(logisticsKind==0?kind.ToString():logisticsKind==1?"Kurier":"Baufahrzeug")).transform;
            if(logisticsKind==0)BuildGroundUnitModel(root,kind,Cyan);
            else BuildGroundLogisticsModel(root,new ShipmentState{Airborne=AtmosphereSurface,Phase=logisticsKind==1?ShipmentPhase.InTransit:ShipmentPhase.BuilderOutbound});
            root.position=SurfacePosition(x,AtmosphereSurface?.55f:.035f,y);return root;
        }

        internal static float ModelMotionRadius(Transform root,bool includeAnimatedSweep,bool spherical)
        {
            float radius=0;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh==null)continue;
                Transform pivotTransform=null;
                if(includeAnimatedSweep)for(var ancestor=filter.transform;ancestor!=null && ancestor!=root;ancestor=ancestor.parent)
                    if(ancestor.name=="Turret" || ancestor.name=="Kranarm"){pivotTransform=ancestor;break;}
                Vector3 pivot=pivotTransform==null?Vector3.zero:root.InverseTransformPoint(pivotTransform.position);
                foreach(var vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point=root.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    float planar=pivotTransform==null?new Vector2(point.x,point.z).magnitude:
                        new Vector2(pivot.x,pivot.z).magnitude+new Vector2(point.x-pivot.x,point.z-pivot.z).magnitude;
                    radius=Mathf.Max(radius,spherical?Mathf.Sqrt(planar*planar+point.y*point.y):planar);
                }
            }
            var scale=root.lossyScale;
            return radius*Mathf.Max(Mathf.Abs(scale.x),spherical?Mathf.Max(Mathf.Abs(scale.y),Mathf.Abs(scale.z)):Mathf.Abs(scale.z));
        }
    }

    internal sealed class VehicleModelSize:MonoBehaviour
    {
        internal float PreferredScale,AppliedScale,PhysicalRadius,BeforeRadius,AfterRadius;
    }
}
