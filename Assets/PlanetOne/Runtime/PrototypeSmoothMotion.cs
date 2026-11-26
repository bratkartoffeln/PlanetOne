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
            if(directions.Count==1)AddCurvedRibbon(vertices,indices,directions[0],center,center,.56f);
            else for(int a=0;a<directions.Count;a++)for(int b=a+1;b<directions.Count;b++)AddCurvedRibbon(vertices,indices,directions[a],center,directions[b],.56f);
            var mesh=new Mesh{name="Runde Straßenkurven"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            MeshPart("Fahrbahn",root,mesh,Vector3.zero,Vector3.one,new Color(.29f,.31f,.32f));root.gameObject.AddComponent<OwnedSurfaceMesh>().Mesh=mesh;
            // Junction interiors stay unmarked; two-arm curves share the courier centreline.
            if(directions.Count<=2)
            {
                var marks=new List<Vector3>();var triangles=new List<int>();
                var a=directions[0];var b=directions.Count==2?directions[1]:center;
                for(int dash=0;dash<3;dash++)
                {
                    float t0=(dash+.12f)/3,t1=(dash+.64f)/3;
                    var start=RoadBend(a,center,b,t0);var end=RoadBend(a,center,b,t1);
                    AddCurvedRibbon(marks,triangles,start,(start+end)*.5f,end,.018f);
                }
                for(int i=0;i<marks.Count;i++)marks[i]+=Vector3.up*.008f;
                var marking=new Mesh{name="Zweispurige Straße · Mittellinie"};marking.SetVertices(marks);marking.SetTriangles(triangles,0);marking.RecalculateNormals();
                var marker=MeshPart("Fahrspurtrennung",root,marking,Vector3.zero,Vector3.one,new Color(.78f,.76f,.58f));
                marker.gameObject.AddComponent<OwnedSurfaceMesh>().Mesh=marking;
            }
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
            var point=GameSimulation.GroundCourierPosition(shipment);
            Vector3 position=new Vector3((float)point.X,0,(float)point.Y);
            position.y=ReliefHeight(position.x,position.z)+(AtmosphereSurface?.55f:.055f);return position;
        }
        private void UpdateShipmentPose(Transform view,ShipmentState shipment)
        {
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
