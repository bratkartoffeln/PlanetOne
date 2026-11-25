using PlanetOne.Core;
using UnityEngine;
namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        public void StartPlanetEnvironmentFixture()
        {
            if(!simulation.State.SpaceTestFixture && !simulation.State.MilitaryTestFixture && !simulation.State.InvasionTestFixture)priorSession=simulation;
            simulation=PlanetEnvironmentFixture.Create();foodRates.Clear();controlGroups.Clear();shipGroups.Clear();
            showingSpacePanel=false;showingMilitary=false;BuildSpace();SetView(0);Notify(simulation.State.LastMessage);
        }
        private readonly System.Collections.Generic.Dictionary<PlanetKind,Material> biomeMaterials=new System.Collections.Generic.Dictionary<PlanetKind,Material>();
        private bool AtmosphereSurface {get {var p=simulation.FindPlanet(openPlanetId);return p!=null && PlanetEnvironment.Airborne(p);}}
        private Material BiomeMaterial(PlanetKind kind)
        {
            Material material;if(biomeMaterials.TryGetValue(kind,out material))return material;
            material=new Material(Resources.Load<Shader>("BiomeTerrain"));
            material.SetTexture("_MainTex",Resources.Load<Texture2D>("BiomeSurfaceAtlas"));
            material.SetTexture("_RockTex",Resources.Load<Texture2D>("SurfaceAtlas"));
            material.SetFloat("_Biome",(float)kind);biomeMaterials[kind]=material;return material;
        }
        private Color BiomeRadarColor(PlanetKind kind,float height,float slope)
        {
            Color color=kind==PlanetKind.Ice?new Color(.65f,.8f,.9f):kind==PlanetKind.Desert?new Color(.65f,.4f,.17f):kind==PlanetKind.GasGiant?new Color(.64f,.52f,.36f):new Color(.16f,.35f,.18f);
            if(kind==PlanetKind.Ocean && height<=0)color=new Color(.025f,.19f,.35f);
            return color*Mathf.Clamp(.65f+height*.12f+slope,.4f,1.3f);
        }
        private void BuildEnvironmentLayer(PlanetState p,Vector3[] terrainVertices,int[] triangles)
        {
            bool gas=PlanetEnvironment.Airborne(p);var vertices=(Vector3[])terrainVertices.Clone();
            for(int i=0;i<vertices.Length;i++)vertices[i].y=gas?-4+Mathf.PerlinNoise(vertices[i].x*.12f,vertices[i].z*.12f)*1.5f:0;
            var mesh=new Mesh{name=gas?"Wolkendecke unter den Stationen":"Ozean",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var layer=new GameObject(mesh.name);layer.transform.SetParent(surfaceRoot,false);layer.AddComponent<MeshFilter>().sharedMesh=mesh;
            var material=gas?BiomeMaterial(PlanetKind.GasGiant):new Material(Resources.Load<Shader>("OceanSurface"));
            var renderer=layer.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            if(gas){renderer.receiveShadows=false;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            layer.AddComponent<OwnedSurfaceMesh>().Mesh=mesh;
            if(!gas)layer.AddComponent<OwnedBiomeMaterial>().Material=material;
        }
        private void BuildTree(Transform parent,Vector3 position,float size,int variation)
        {
            var root=new GameObject("Flora").transform;root.SetParent(parent,false);root.localPosition=position;root.localScale=Vector3.one*size;
            Rod(root,"Stamm",Vector3.zero,Vector3.up*.9f,.09f,new Color(.22f,.17f,.10f));
            for(int i=0;i<3;i++) {
                float a=(variation*37+i*120)*Mathf.Deg2Rad;
                Part(root,"Pflanzreihe",new Vector3(Mathf.Sin(a)*.18f,.65f+i*.16f,Mathf.Cos(a)*.18f),new Vector3(.6f,.6f,.55f),new Color(.18f,.32f,.11f),PrimitiveType.Sphere);
            }
        }
        private void BuildFloatingPlatform(Transform root)
        {
            var deck=Part(root,"Schwebendes Fundament",new Vector3(0,-.07f,0),new Vector3(1.12f,.10f,1.12f),Steel,PrimitiveType.Cylinder);
            for(int i=0;i<4;i++) {
                float a=(45+i*90)*Mathf.Deg2Rad;var offset=new Vector3(Mathf.Sin(a)*.45f,-.27f,Mathf.Cos(a)*.45f);
                Part(root,"Stabilisator",offset,new Vector3(.22f,.25f,.22f),Hull,PrimitiveType.Cylinder);
                Part(root,"Antrieb",offset-Vector3.up*.16f,new Vector3(.14f,.13f,.14f),Cyan,PrimitiveType.Sphere);
            }
        }
        private void BuildAirVehicle(Transform root,UnitKind kind,Color color,bool courier)
        {
            HullPart(root,new Vector3(0,.18f,0),new Vector3(.43f,.20f,.64f),color);
            Part(root,courier?"Frachtcontainer":"Geschützte Flugkapsel",new Vector3(0,.32f,0),new Vector3(.29f,.22f,.40f),Hull);
            for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2) {
                var at=new Vector3(side*.34f,.15f,end*.23f);
                Rod(root,"Ausleger",new Vector3(0,.18f,end*.16f),at,.055f,Steel);
                Part(root,"Schwebetriebwerk",at,new Vector3(.24f,.06f,.24f),Steel,PrimitiveType.Cylinder);
                Part(root,"Antrieb",at-Vector3.up*.08f,new Vector3(.15f,.10f,.15f),Cyan,PrimitiveType.Sphere);
            }
            if(!courier)Rod(root,kind==UnitKind.Engineer?"Werkzeugarm":"Geschütz",new Vector3(0,.35f,.1f),new Vector3(0,.36f,.58f),.045f,Steel);
        }
    }
    public sealed class OwnedBiomeMaterial:MonoBehaviour
    {
        public Material Material;
        private void OnDestroy(){if(Material!=null)Destroy(Material);}
    }
}
