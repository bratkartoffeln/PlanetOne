using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private readonly Dictionary<int,Transform> trailViews=new Dictionary<int,Transform>();
        private readonly Dictionary<int,int> trailShapes=new Dictionary<int,int>();
        private PlanetState trailPlanet;
        private Transform trailRoot;
        private long trailTick=-1;
        private Material trailMaterial;
        internal int RenderedTrailCount {get{return trailViews.Count;}}
        private void UpdateGroundTrails(PlanetState planet)
        {
            if(trailPlanet!=planet||trailRoot!=surfaceRoot)
            {foreach(var view in trailViews.Values)if(view!=null)Destroy(view.gameObject);trailViews.Clear();trailShapes.Clear();trailPlanet=planet;trailRoot=surfaceRoot;trailTick=-1;}
            if(AtmosphereSurface)return;
            if(trailTick==simulation.State.Tick)return;trailTick=simulation.State.Tick;
            var shapes=new Dictionary<int,int>();var strengths=new Dictionary<int,float>();
            foreach(var trail in planet.Trails)
            {
                float strength=(float)trail.Strength(simulation.State.Tick);if(strength<.025f)continue;
                AddTrailEnd(planet,trail.From,trail.To,strength,shapes,strengths);AddTrailEnd(planet,trail.To,trail.From,strength,shapes,strengths);
            }
            var removed=new List<int>();foreach(var entry in trailViews)if(!shapes.ContainsKey(entry.Key))removed.Add(entry.Key);
            foreach(int key in removed){if(trailViews[key]!=null)Destroy(trailViews[key].gameObject);trailViews.Remove(key);trailShapes.Remove(key);}
            if(trailMaterial==null)trailMaterial=new Material(Resources.Load<Shader>("GroundTrail"));
            foreach(var entry in shapes)
            {
                Transform view;int old;
                if(!trailViews.TryGetValue(entry.Key,out view)||view==null||!trailShapes.TryGetValue(entry.Key,out old)||old!=entry.Value)
                {
                    if(view!=null)Destroy(view.gameObject);
                    view=BuildTrailMesh(planet,entry.Key,entry.Value);trailViews[entry.Key]=view;trailShapes[entry.Key]=entry.Value;
                }
                var properties=new MaterialPropertyBlock();
                Color color=planet.Kind==PlanetKind.Ice?new Color(.38f,.43f,.45f):planet.Kind==PlanetKind.Desert?new Color(.35f,.24f,.13f):new Color(.19f,.14f,.08f);
                color.a=Mathf.Lerp(.06f,.48f,strengths[entry.Key]);properties.SetColor("_Color",color);view.GetComponent<MeshRenderer>().SetPropertyBlock(properties);
            }
        }
        private static void AddTrailEnd(PlanetState p,int from,int to,float strength,Dictionary<int,int> shapes,Dictionary<int,float> strengths)
        {
            if(p.Buildings.Exists(b=>!b.Destroyed&&b.X==from%p.Width&&b.Y==from/p.Width))return;
            for(int d=0;d<6;d++)
            {
                var n=HexGrid.Neighbor(from%p.Width,from/p.Width,d);if(n.X!=to%p.Width||n.Y!=to/p.Width)continue;
                int shape;float prior;shapes.TryGetValue(from,out shape);strengths.TryGetValue(from,out prior);shapes[from]=shape|(1<<d);strengths[from]=Mathf.Max(prior,strength);break;
            }
        }
        private Transform BuildTrailMesh(PlanetState p,int cell,int shape)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var ends=new List<Vector3>();
            var center=SurfacePosition(cell%p.Width,0,cell/p.Width);
            for(int d=0;d<6;d++)if((shape&(1<<d))!=0){var n=HexGrid.Neighbor(cell%p.Width,cell/p.Width,d);ends.Add(Vector3.Lerp(center,SurfacePosition(n.X,0,n.Y),.51f));}
            if(ends.Count==1)AddCurvedRibbon(vertices,triangles,ends[0],center,center,.19f);
            else for(int a=0;a<ends.Count;a++)for(int b=a+1;b<ends.Count;b++)AddCurvedRibbon(vertices,triangles,ends[a],center,ends[b],.19f);
            var uv=new List<Vector2>();for(int i=0;i<vertices.Count;i++)uv.Add(new Vector2((i%38)/2f/18f,i%2));
            var mesh=new Mesh{name="Befahrene Bodenspur"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var root=new GameObject("Nutzungsspur").transform;root.SetParent(surfaceRoot,false);root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=root.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=trailMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            root.gameObject.AddComponent<OwnedSurfaceMesh>().Mesh=mesh;return root;
        }
    }
}
