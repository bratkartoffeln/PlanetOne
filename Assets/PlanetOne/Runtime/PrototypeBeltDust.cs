using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private readonly List<Mesh> beltDustMeshes=new List<Mesh>();
        private Material beltDustMaterial;
        public float BeltDustVisibility {get{return beltDustMaterial==null?0:beltDustMaterial.GetFloat("_Visibility");}}
        private void BuildBeltDust(AsteroidBelt belt)
        {
            if(beltDustMaterial==null)beltDustMaterial=new Material(Resources.Load<Shader>("AsteroidDust"));
            const int segments=192;
            var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[segments*6];
            for(int i=0;i<=segments;i++)
            {
                double angle=i*OrbitalMotion.Tau/segments;
                for(int edge=0;edge<2;edge++)
                {
                    double radius=edge==0?belt.InnerRadius:belt.OuterRadius;
                    vertices[i*2+edge]=SpacePosition(System.Math.Cos(angle)*radius,System.Math.Sin(angle)*radius,-.12f);
                    uv[i*2+edge]=new Vector2((float)i/segments,edge);
                }
                if(i==segments)continue;int start=i*6,v=i*2;
                triangles[start]=v;triangles[start+1]=v+2;triangles[start+2]=v+1;
                triangles[start+3]=v+1;triangles[start+4]=v+2;triangles[start+5]=v+3;
            }
            var mesh=new Mesh {name="Weicher Asteroidenstaubring",vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateBounds();beltDustMeshes.Add(mesh);
            var ring=new GameObject("Asteroidenstaub");ring.transform.SetParent(spaceRoot,false);ring.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=ring.AddComponent<MeshRenderer>();renderer.sharedMaterial=beltDustMaterial;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        private void UpdateBeltDust()
        {
            // The map-wide cue recedes smoothly near individual rocks. It never changes deposits.
            if(beltDustMaterial!=null)beltDustMaterial.SetFloat("_Visibility",Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,32,spaceZoom)));
        }
        public void ShowBeltOverview()
        {
            if(openPlanetId!=0)return;spaceFocus=Vector3.zero;
            float extent=12;foreach(var belt in simulation.State.AsteroidBelts)extent=Mathf.Max(extent,(float)belt.OuterRadius*SpaceDistanceScale);
            spaceZoom=extent*.82f;panelOpen=false;contextOpen=false;UpdateCamera();UpdateBeltDust();
        }
        public void InspectAsteroid(long id)
        {
            if(openPlanetId!=0)return;var a=simulation.State.Asteroids.Find(v=>v.Id==id);if(a==null)return;
            SelectAsteroid(id);SetSpaceFocus(asteroidViews[id].position);spaceZoom=.25f;UpdateCamera();UpdateBeltDust();
        }
    }
}
