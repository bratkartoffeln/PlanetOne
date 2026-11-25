using UnityEngine;
using PlanetOne.Core;
namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private Mesh reliefMesh;
        private MeshCollider reliefCollider;
        private Texture2D reliefMap;
        private const float ReliefStep=0.25f;
        // Presentation-only relief, deterministic from the stable world identity and dimensions.
        // The existing impassable terrain still controls movement; height does not add hidden combat bonuses.
        private float ReliefSample(float x,float z)
        {
            var p=simulation.FindPlanet(openPlanetId);if(p==null)return 0;
            if(PlanetEnvironment.Airborne(p))return 3;
            float warp=Mathf.PerlinNoise(x*0.09f+p.Id,z*0.09f)*3;
            float broad=Mathf.PerlinNoise(x*0.13f+p.Id*13,z*0.13f+7);
            float folded=1-Mathf.Abs(Mathf.PerlinNoise(x*0.23f+warp+17,z*0.23f-warp+p.Id)*2-1);
            float ridge=Mathf.Pow(Mathf.PerlinNoise(x*.055f+37+p.Id,z*.068f+19),3)*6;
            if(p.Kind==PlanetKind.Ocean)return (float)PlanetEnvironment.OceanHeight(x,z);
            if(p.Kind==PlanetKind.Desert)return .18f+broad*.45f+Mathf.Pow(folded,3)*.55f+ridge*.6f;
            if(p.Kind==PlanetKind.Ice)return .2f+broad*.5f+ridge*1.6f+folded*.18f;
            if(p.Kind==PlanetKind.Temperate)return broad*.65f+ridge*.7f+folded*.15f;
            return broad*.35f+ridge+folded*folded*.7f;
        }
        private float ReliefHeight(float x,float z)
        {
            float sx=Mathf.Floor(x/ReliefStep)*ReliefStep,sz=Mathf.Floor(z/ReliefStep)*ReliefStep;
            float u=(x-sx)/ReliefStep,v=(z-sz)/ReliefStep;
            float a=ReliefSample(sx,sz),b=ReliefSample(sx+ReliefStep,sz),c=ReliefSample(sx,sz+ReliefStep),d=ReliefSample(sx+ReliefStep,sz+ReliefStep);
            return u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
        }
        private void BuildRelief(PlanetState planet)
        {
            const int padding=10;
            int columns=(planet.Width+padding*2)*4,rows=(planet.Height+padding*2)*4;
            var vertices=new Vector3[(columns+1)*(rows+1)];var triangles=new int[columns*rows*6];
            for(int z=0;z<=rows;z++)for(int x=0;x<=columns;x++)
            {float wx=x*ReliefStep-padding,wz=z*ReliefStep-padding;vertices[z*(columns+1)+x]=new Vector3(wx,ReliefSample(wx,wz),wz);}
            int n=0;for(int z=0;z<rows;z++)for(int x=0;x<columns;x++)
            {int a=z*(columns+1)+x,b=a+1,c=a+columns+1,d=c+1;triangles[n++]=a;triangles[n++]=c;triangles[n++]=b;triangles[n++]=b;triangles[n++]=c;triangles[n++]=d;}
            reliefMesh=new Mesh{name="Berge und Täler",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};reliefMesh.vertices=vertices;reliefMesh.triangles=triangles;reliefMesh.RecalculateNormals();reliefMesh.RecalculateBounds();
            var terrain=new GameObject("Durchgehendes Relief");terrain.transform.SetParent(surfaceRoot,false);
            terrain.AddComponent<MeshFilter>().sharedMesh=reliefMesh;
            var terrainRenderer=terrain.AddComponent<MeshRenderer>();terrainRenderer.sharedMaterial=BiomeMaterial(planet.Kind);
            terrainRenderer.enabled=!PlanetEnvironment.Airborne(planet);
            reliefCollider=terrain.AddComponent<MeshCollider>();reliefCollider.sharedMesh=reliefMesh;
            if(planet.Kind==PlanetKind.Ocean || PlanetEnvironment.Airborne(planet))BuildEnvironmentLayer(planet,vertices,triangles);
            if(reliefMap!=null)Destroy(reliefMap);
            reliefMap=new Texture2D(96,80,TextureFormat.RGB24,false){name="Kolonieradar",filterMode=FilterMode.Bilinear};
            for(int y=0;y<80;y++)for(int x=0;x<96;x++)
            {
                float wx=x/95f*planet.Width,wz=y/79f*planet.Height*0.866f;
                float h=ReliefSample(wx,wz),slope=ReliefSample(wx+0.2f,wz)-h;
                reliefMap.SetPixel(x,y,BiomeRadarColor(planet.Kind,h,slope));
            }
            reliefMap.Apply();
            for(int y=0;y<planet.Height;y++)for(int x=0;x<planet.Width;x++)
            {
                var center=SurfacePosition(x,0,y);var outline=MakeHexOutline("Wabengrenze",surfaceRoot,new Color(0.62f,0.57f,0.43f),0.009f).GetComponent<LineRenderer>();
                outline.positionCount=24;
                outline.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;outline.receiveShadows=false;
                for(int j=0;j<24;j++)
                {
                    Vector3 point=center+Vector3.Lerp(HexCorner(j/4,1),HexCorner((j/4+1)%6,1),(j%4)/4f);
                    point.y=ReliefHeight(point.x,point.z)+0.025f;outline.SetPosition(j,point);
                }
                outline.gameObject.AddComponent<ReliefGridVisibility>();
            }
            if(PlanetEnvironment.Airborne(planet)){Physics.SyncTransforms();return;}
            var random=new System.Random(planet.Id*727+81);
            for(int i=0;i<240;i++)
            {
                float x=(float)random.NextDouble()*(planet.Width+14)-7,z=(float)random.NextDouble()*(planet.Height+10)-5;
                if(x>-0.7f && x<planet.Width && z>-0.6f && z<planet.Height*0.866f)continue;
                if(planet.Kind==PlanetKind.Ocean && ReliefHeight(x,z)<.15f)continue;
                float size=0.5f+(float)random.NextDouble()*1.7f;
                if(planet.Kind==PlanetKind.Temperate || planet.Kind==PlanetKind.Ocean)BuildTree(surfaceRoot,new Vector3(x,ReliefHeight(x,z),z),size*.6f,i);
                else Rock(surfaceRoot,new Vector3(x,ReliefHeight(x,z)+size*0.12f,z),new Vector3(size,size*0.9f,size*1.2f),i);
            }
            Physics.SyncTransforms();
        }
        private void ConformMarker(Transform marker,double x,double y)
        {
            marker.localPosition=Vector3.zero;var line=marker.GetComponent<LineRenderer>();line.positionCount=24;
            Vector3 center=SurfacePosition(x,0,y);
            for(int i=0;i<24;i++)
            {var point=center+Vector3.Lerp(HexCorner(i/4,0.96f),HexCorner((i/4+1)%6,0.96f),(i%4)/4f);point.y=ReliefHeight(point.x,point.z)+0.06f;line.SetPosition(i,point);}
        }
        private Transform BuildRoad(int x,int y,PlanetState p)
        {
            var root=new GameObject("Straßenverbindung").transform;root.SetParent(surfaceRoot,false);
            Vector3 center=SurfacePosition(x,0.035f,y);
            var vertices=new System.Collections.Generic.List<Vector3>();var indices=new System.Collections.Generic.List<int>();
            for(int d=0;d<6;d++)
            {
                var next=HexGrid.Neighbor(x,y,d);
                if(next.X<0||next.Y<0||next.X>=p.Width||next.Y>=p.Height||!p.Roads[next.Y*p.Width+next.X])continue;
                Vector3 end=SurfacePosition(next.X,0.035f,next.Y);
                AddRoadRibbon(vertices,indices,center,Vector3.Lerp(center,end,0.51f),0.09f,0.035f);
            }
            if(vertices.Count==0)AddRoadRibbon(vertices,indices,center-Vector3.right*0.12f,center+Vector3.right*0.12f,0.09f,0.035f);
            var mesh=new Mesh{name="Geländestraße"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            MeshPart("Straße",root,mesh,Vector3.zero,Vector3.one,new Color(0.34f,0.35f,0.34f));root.gameObject.AddComponent<OwnedSurfaceMesh>().Mesh=mesh;
            return root;
        }
        private void AddRoadRibbon(System.Collections.Generic.List<Vector3> vertices,System.Collections.Generic.List<int> indices,Vector3 a,Vector3 b,float width,float height)
        {
            Vector3 side=Vector3.Cross(Vector3.up,(b-a).normalized)*width;int start=vertices.Count;
            for(int i=0;i<=8;i++)
            {
                Vector3 point=Vector3.Lerp(a,b,i/8f),left=point-side,right=point+side;
                left.y=ReliefHeight(left.x,left.z)+height;right.y=ReliefHeight(right.x,right.z)+height;vertices.Add(left);vertices.Add(right);
                if(i<8){int n=start+i*2;indices.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}
            }
        }
    }
    public sealed class ReliefGridVisibility:MonoBehaviour
    {
        private LineRenderer line;
        private void Awake(){line=GetComponent<LineRenderer>();}
        private void Update(){line.enabled=Shader.GetGlobalFloat("_PlanetOneGrid")>0.5f;}
    }
    public sealed class OwnedSurfaceMesh:MonoBehaviour
    {
        public Mesh Mesh;
        private void OnDestroy(){if(Mesh!=null)Destroy(Mesh);}
    }
}
