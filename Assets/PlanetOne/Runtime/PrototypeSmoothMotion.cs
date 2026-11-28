using System;
using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private static Vector3 RoadBend(Vector3 entry,Vector3 center,Vector3 exit,float t)
        {float u=1-t;return u*u*entry+2*u*t*center+t*t*exit;}
        private Transform BuildCurvedRoad(int x,int y,PlanetState planet)
        {
            var root=new GameObject("Straße · gerundete Verbindung").transform;root.SetParent(surfaceRoot,false);
            var vertices=new List<Vector3>();var indices=new List<int>();var directions=new List<Vector3>();
            Vector3 center=SurfacePosition(x,0,y);
            for(int i=0;i<6;i++)
            {
                var next=HexGrid.Neighbor(x,y,i);if(next.X<0||next.Y<0||next.X>=planet.Width||next.Y>=planet.Height)continue;
                if(planet.Roads[next.Y*planet.Width+next.X] || planet.Buildings.Exists(b=>b.X==next.X && b.Y==next.Y && (GameSimulation.BuildingOwner(planet,b)==PlayerFactionId || simulation.IsCellVisible(planet,PlayerFactionId,b.X,b.Y))))
                    directions.Add(Vector3.Lerp(center,SurfacePosition(next.X,0,next.Y),.51f));
            }
            if(directions.Count==0){directions.Add(center-Vector3.right*.18f);directions.Add(center+Vector3.right*.18f);}
            if(directions.Count==1)AddCurvedRibbon(vertices,indices,directions[0],center,center,(float)GameSimulation.RoadHalfWidth);
            else for(int a=0;a<directions.Count;a++)for(int b=a+1;b<directions.Count;b++)AddCurvedRibbon(vertices,indices,directions[a],center,directions[b],(float)GameSimulation.RoadHalfWidth);
            var mesh=new Mesh{name="Runde Straßenkurven"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            MeshPart("Fahrbahn",root,mesh,Vector3.zero,Vector3.one,new Color(.27f,.25f,.22f));root.gameObject.AddComponent<OwnedSurfaceMesh>().Mesh=mesh;
            return root;
        }
        private void AddCurvedRibbon(List<Vector3> vertices,List<int> indices,Vector3 a,Vector3 center,Vector3 b,float width)
        {
            int first=vertices.Count;const int segments=18;
            for(int i=0;i<=segments;i++)
            {
                float t=i/(float)segments;Vector3 point=RoadBend(a,center,b,t);
                Vector3 tangent=2*((1-t)*(center-a)+t*(b-center));if(tangent.sqrMagnitude<.00001f)tangent=b-a;
                var side=Vector3.Cross(Vector3.up,tangent.normalized)*width;Vector3 left=point-side,right=point+side;
                left.y=ReliefHeight(left.x,left.z)+.045f;right.y=ReliefHeight(right.x,right.z)+.045f;vertices.Add(left);vertices.Add(right);
                if(i<segments){int n=first+i*2;indices.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}
            }
        }
        private Transform BuildPreviewRoadMesh(int x,int y){return BuildCurvedRoad(x,y,simulation.FindPlanet(openPlanetId));}
        internal Vector3 ShipmentRoadPosition(ShipmentState shipment)
        {
            var point=GameSimulation.CourierDisplayPosition(shipment);
            Vector3 position=new Vector3((float)point.X,0,(float)point.Y);
            position.y=ReliefHeight(position.x,position.z)+(AtmosphereSurface?.55f:.055f);return position;
        }
        private readonly List<ShipmentState> visibleCouriers=new List<ShipmentState>();
        private readonly List<CourierFootprint> drawnCouriers=new List<CourierFootprint>();
        private struct CourierFootprint { public Vector2 Center, Heading; public float Radius; }

        // Solve on the interpolated render positions. Clear simulation positions at the two
        // ends of a tick do not guarantee clear models halfway through the frame.
        private void SeparateCourierModels()
        {
            visibleCouriers.Sort((a,b)=>{int priority=(b.Phase==ShipmentPhase.Building).CompareTo(a.Phase==ShipmentPhase.Building);return priority!=0?priority:a.Id.CompareTo(b.Id);});
            drawnCouriers.Clear();
            foreach (var shipment in visibleCouriers)
            {
                var view=shipmentViews[shipment.Id];
                var size=view.GetComponent<VehicleModelSize>();
                var basePosition=new Vector2(view.position.x,view.position.z);
                var desired=shipment.Phase==ShipmentPhase.Building ? basePosition : basePosition+size.DisplayOffset*Mathf.Exp(-10*Time.unscaledDeltaTime);
                int edge=Mathf.Clamp((int)shipment.DistanceTravelled,0,shipment.Path.Count-2);
                Vector2 heading=Vector2.up;
                if (shipment.Path.Count>=2)
                {
                    var a=shipment.Path[edge]; var b=shipment.Path[edge+1];
                    var start=HexGrid.ToWorld(a.X,a.Y);var end=HexGrid.ToWorld(b.X,b.Y);
                    heading=new Vector2((float)(end.X-start.X),(float)(end.Y-start.Y)).normalized;
                }
                var center=desired;
                foreach (var other in drawnCouriers)
                {
                    float gap=size.VisualRadius+other.Radius+.02f;
                    if ((center-other.Center).sqrMagnitude>=gap*gap) continue;
                    // Queue behind a vehicle travelling in the same direction. At crossings
                    // and in counterflow, keep the route heading and separate sideways.
                    var axis=Vector2.Dot(heading,other.Heading)>.85f ? -heading : new Vector2(heading.y,-heading.x);
                    float positive=ClearanceAlong(desired,axis,size.VisualRadius);
                    float negative=ClearanceAlong(desired,-axis,size.VisualRadius);
                    center=desired+(positive<=negative+.001f ? axis*positive : -axis*negative);
                    break;
                }
                size.DisplayOffset=center-basePosition;
                if(size.DisplayOffset.sqrMagnitude>.00000001f)
                    view.position=new Vector3(center.x,ReliefHeight(center.x,center.y)+(AtmosphereSurface?.55f:.055f),center.y);
                drawnCouriers.Add(new CourierFootprint{Center=center,Heading=heading,Radius=size.VisualRadius});
            }
        }

        private float ClearanceAlong(Vector2 origin,Vector2 axis,float radius)
        {
            // Move to the end of any overlapping model footprint on this ray. Each
            // move clears at least one circle permanently (at most four couriers).
            float offset=0;
            for(int pass=0;pass<drawnCouriers.Count;pass++)
            {
                float before=offset;
                foreach(var other in drawnCouriers)
                {
                    var delta=other.Center-origin;
                    float along=Vector2.Dot(delta,axis);
                    float gap=radius+other.Radius+.021f;
                    float square=gap*gap-(delta.sqrMagnitude-along*along);
                    if(square<=0) continue;
                    float half=Mathf.Sqrt(square);
                    if(offset>along-half && offset<along+half) offset=along+half;
                }
                if(offset==before) break;
            }
            return offset;
        }
        private void UpdateShipmentPose(Transform view,ShipmentState shipment)
        {
            var cargo=view.Find(AtmosphereSurface?"Frachtcontainer":"Ladebehälter");
            if(cargo!=null)cargo.gameObject.SetActive(shipment.Amount>0&&shipment.Phase!=ShipmentPhase.Loading);
            Vector3 position=ShipmentRoadPosition(shipment);UpdateSurfaceMotion(view,position);
            if(GameSimulation.IsConstructionVehicle(shipment))
            {
                var arm=view.Find("Kranarm");
                if(arm!=null)
                {
                    var crane=arm.GetComponent<ConstructionArmPose>();
                    if(crane==null)crane=arm.gameObject.AddComponent<ConstructionArmPose>();
                    crane.SetWorking(shipment.Phase==ShipmentPhase.Building,(float)(simulation.State.Tick*Definitions.TickSeconds));
                }
            }
        }
        private void UpdateSurfaceMotion(Transform view,Vector3 position)
        {
            float dx=ReliefHeight(position.x+.12f,position.z)-ReliefHeight(position.x-.12f,position.z);
            float dz=ReliefHeight(position.x,position.z+.12f)-ReliefHeight(position.x,position.z-.12f);
            var facing=view.GetComponent<MotionFacing>();if(facing==null)facing=view.gameObject.AddComponent<MotionFacing>();
            facing.MoveTick(position,0,AtmosphereSurface?Vector3.up:new Vector3(-dx,.24f,-dz).normalized,simulation.State.Tick,MovementInterpolation);
        }
    }
    internal sealed class ConstructionArmPose:MonoBehaviour
    {
        private Quaternion restRotation;
        private void Awake(){restRotation=transform.localRotation;}
        internal void SetWorking(bool working,float time)
        {transform.localRotation=Quaternion.AngleAxis(working?Mathf.Sin(time*2)*22:0,Vector3.up)*restRotation;}
    }
}
