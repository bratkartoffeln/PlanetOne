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
        private void ConfigureEdgeFog(Material material,PlanetState planet)
        {
            float lastRow=(planet.Height-1)*(float)HexGrid.RowHeight;
            material.SetVector("_SurfaceLimits",new Vector4(-.5f,-.58f,planet.Width,lastRow+.58f));
            material.SetColor("_SurfaceFogColor",sceneCamera.backgroundColor);
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
            ConfigureEdgeFog(material,p);
            var renderer=layer.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            if(gas){renderer.receiveShadows=false;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            layer.AddComponent<OwnedSurfaceMesh>().Mesh=mesh;
            if(!gas)layer.AddComponent<OwnedBiomeMaterial>().Material=material;
        }
        private void BuildTree(Transform parent,Vector3 position,float size,int variation)
        {
            var root=new GameObject("Flora").transform;root.SetParent(parent,false);root.localPosition=position;root.localScale=Vector3.one*size;
            root.localRotation=Quaternion.Euler(0,variation*137.5f,0);
            bool conifer=variation%4==0;
            float height=.8f+(variation%7)*.065f;
            Rod(root,"Baumrinde",Vector3.zero,Vector3.up*height,.038f,new Color(.22f,.17f,.105f));
            if(conifer)
            {
                for(int layer=0;layer<4;layer++)
                    MeshPart("Blattwerk",root,FoliageMesh(variation,true),new Vector3(0,.3f+layer*.22f,0),new Vector3(.64f-layer*.1f,.55f,.64f-layer*.1f),new Color(.13f,.27f+.015f*(variation%5),.18f));
                return;
            }
            for(int i=0;i<5;i++)
            {
                float a=(variation*37+i*137)*Mathf.Deg2Rad;
                var crown=new Vector3(Mathf.Sin(a)*.24f,height*.72f+i*.055f,Mathf.Cos(a)*.24f);
                Rod(root,"Ast",new Vector3(0,height*.48f,0),crown,.019f,new Color(.24f,.18f,.11f));
                MeshPart("Blattwerk",root,FoliageMesh(variation+i,false),crown,new Vector3(.58f,.51f,.54f),new Color(.17f+.018f*(variation%4),.31f+.018f*(i%3),.105f+.012f*(variation%5)));
            }
        }
        private Mesh FoliageMesh(int variation,bool conifer)
        {
            string key=(conifer?"pine":"leaves")+(variation%5);Mesh mesh;if(modelMeshes.TryGetValue(key,out mesh))return mesh;
            const int rings=8,sides=10;var vertices=new Vector3[(rings+1)*(sides+1)];var triangles=new int[rings*sides*6];int index=0;
            for(int ring=0;ring<=rings;ring++)for(int side=0;side<=sides;side++)
            {
                float latitude=ring*Mathf.PI/rings,angle=side*Mathf.PI*2/sides;
                float radius=conifer?(1-ring/(float)rings)*.65f:Mathf.Sin(latitude)*.55f;
                float rough=1+Mathf.Sin(side*2.7f+ring*3.1f+variation)*.13f;
                float y=conifer?ring/(float)rings-.25f:Mathf.Cos(latitude)*.52f;
                vertices[ring*(sides+1)+side]=new Vector3(Mathf.Cos(angle)*radius*rough,y,Mathf.Sin(angle)*radius*rough);
                if(ring<rings&&side<sides)
                {
                    int a=ring*(sides+1)+side,b=a+1,c=a+sides+1,d=c+1;
                    triangles[index++]=a;triangles[index++]=conifer?c:b;triangles[index++]=conifer?b:c;
                    triangles[index++]=b;triangles[index++]=conifer?c:d;triangles[index++]=conifer?d:c;
                }
            }
            mesh=new Mesh{name=conifer?"Gestufte Nadelkrone":"Unregelmäßiges Blattwerk"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();modelMeshes[key]=mesh;return mesh;
        }
        private void BuildShrub(Transform parent,Vector3 position,float size,int variation)
        {
            MeshPart("Blattwerk",parent,FoliageMesh(variation,false),position+Vector3.up*size*.2f,new Vector3(size,size*.6f,size),new Color(.25f,.34f,.14f));
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
            HullPart(root,new Vector3(0,.18f,0),new Vector3(.43f,.20f,.64f),Hull);
            ArmourPanel(root,courier?"Frachtcontainer":"Geschützte Flugkapsel",new Vector3(0,.32f,0),new Vector3(.29f,.22f,.40f),courier?Industrial:color);
            HullPart(root,new Vector3(0,.265f,.23f),new Vector3(.22f,.12f,.24f),Canopy);
            for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2) {
                var at=new Vector3(side*.34f,.15f,end*.23f);
                Rod(root,"Ausleger",new Vector3(0,.18f,end*.16f),at,.055f,Steel);
                Part(root,"Schwebetriebwerk",at,new Vector3(.24f,.06f,.24f),Steel,PrimitiveType.Cylinder);
                  Part(root,"Düsenkragen",at+Vector3.up*.057f,new Vector3(.19f,.019f,.19f),Hull,PrimitiveType.Cylinder);
                Part(root,"Antrieb",at-Vector3.up*.08f,new Vector3(.15f,.10f,.15f),Cyan,PrimitiveType.Sphere);
            }
            if(!courier && kind==UnitKind.MobileWorkshop)WorkshopBody(root,color);
            else if(!courier && kind==UnitKind.Engineer)Rod(root,"Werkzeugarm",new Vector3(0,.35f,.1f),new Vector3(0,.36f,.58f),.045f,Steel);
            else if(!courier)
            {
                var turret=new GameObject("Turret").transform;turret.SetParent(root,false);turret.localPosition=new Vector3(0,.39f,.05f);
                HullPart(turret,Vector3.zero,new Vector3(.24f,.12f,.22f),color);
                Rod(turret,"Geschütz",new Vector3(0,.02f,.05f),new Vector3(0,.03f,.53f),.045f,Steel);
                var muzzle=new GameObject("MuzzlePosition").transform;muzzle.SetParent(turret,false);muzzle.localPosition=new Vector3(0,.03f,.53f);
            }
        }
    }
    public sealed class OwnedBiomeMaterial:MonoBehaviour
    {
        public Material Material;
        private void OnDestroy(){if(Material!=null)Destroy(Material);}
    }
}
