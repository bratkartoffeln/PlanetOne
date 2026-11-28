using System.Collections;
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
        private const int ReliefPadding=10;
        private float[] reliefHeightSamples;
        private PlanetState reliefSamplePlanet;
        private PlanetKind reliefSampleKind;
        private int reliefSamplePlanetId,reliefSampleWidth,reliefSampleHeight,reliefSampleColumns,reliefSampleRows;
        // Presentation-only relief, deterministic from the stable world identity and dimensions.
        // The existing impassable terrain still controls movement; height does not add hidden combat bonuses.
        private float ReliefSample(float x,float z)
        {
            var p=simulation.FindPlanet(openPlanetId);if(p==null)return 0;
            if(PlanetEnvironment.Airborne(p))return 3;
            float warp=(Mathf.PerlinNoise(x*.035f+p.Id,z*.038f+21)-.5f)*5;
            float broad=Mathf.PerlinNoise((x+warp)*.046f+p.Id*13,(z-warp)*.043f+7);
            float detail=Mathf.PerlinNoise(x*.13f+17,z*.12f+p.Id);
            float folded=(Mathf.Sin((x+warp)*.36f+z*.13f)+1)*.5f;
            float ridge=Mathf.SmoothStep(0,1,Mathf.PerlinNoise(x*.034f+37+p.Id,z*.04f+19));
            ridge=ridge*ridge*2.6f;
            if(p.Kind==PlanetKind.Ocean)return (float)PlanetEnvironment.OceanHeight(x,z);
            if(p.Kind==PlanetKind.Desert)return .12f+broad*.35f+folded*.25f+ridge*.7f+detail*.06f;
            if(p.Kind==PlanetKind.Ice)return .2f+broad*.5f+ridge*1.45f+detail*.08f;
            if(p.Kind==PlanetKind.Temperate)return .08f+broad*.7f+ridge*.75f+detail*.07f;
            return .1f+broad*.3f+ridge*1.35f+detail*.13f;
        }
        private float ReliefHeight(float x,float z)
        {
            float sx=Mathf.Floor(x/ReliefStep)*ReliefStep,sz=Mathf.Floor(z/ReliefStep)*ReliefStep;
            float u=(x-sx)/ReliefStep,v=(z-sz)/ReliefStep;
            var planet=simulation.FindPlanet(openPlanetId);
            if(reliefHeightSamples!=null && planet==reliefSamplePlanet && planet.Id==reliefSamplePlanetId && planet.Kind==reliefSampleKind && planet.Width==reliefSampleWidth && planet.Height==reliefSampleHeight)
            {
                int ix=Mathf.RoundToInt((sx+ReliefPadding)/ReliefStep),iz=Mathf.RoundToInt((sz+ReliefPadding)/ReliefStep);
                if(ix>=0&&iz>=0&&ix<reliefSampleColumns&&iz<reliefSampleRows)
                {
                    int index=iz*(reliefSampleColumns+1)+ix;
                    float cachedA=reliefHeightSamples[index],cachedB=reliefHeightSamples[index+1],cachedC=reliefHeightSamples[index+reliefSampleColumns+1],cachedD=reliefHeightSamples[index+reliefSampleColumns+2];
                    return u+v<=1?cachedA+(cachedB-cachedA)*u+(cachedC-cachedA)*v:cachedD+(cachedC-cachedD)*(1-u)+(cachedB-cachedD)*(1-v);
                }
            }
            float a=ReliefSample(sx,sz),b=ReliefSample(sx+ReliefStep,sz),c=ReliefSample(sx,sz+ReliefStep),d=ReliefSample(sx+ReliefStep,sz+ReliefStep);
            return u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
        }
        private void BuildRelief(PlanetState planet)
        {
            var steps=BuildReliefSteps(planet);while(steps.MoveNext()){}
        }
        private IEnumerator BuildReliefSteps(PlanetState planet)
        {
            reliefSamplePlanet=null;reliefHeightSamples=null;
            const int padding=ReliefPadding;
            int columns=(planet.Width+padding*2)*4,rows=(planet.Height+padding*2)*4;
            var vertices=new Vector3[(columns+1)*(rows+1)];var triangles=new int[columns*rows*6];
            var samples=new float[vertices.Length];
            for(int z=0;z<=rows;z++)
            {
                for(int x=0;x<=columns;x++)
                {float wx=x*ReliefStep-padding,wz=z*ReliefStep-padding,height=ReliefSample(wx,wz);int index=z*(columns+1)+x;vertices[index]=new Vector3(wx,height,wz);samples[index]=height;}
                if((z+1)%8==0)yield return null;
            }
            reliefHeightSamples=samples;reliefSamplePlanet=planet;reliefSampleKind=planet.Kind;reliefSamplePlanetId=planet.Id;
            reliefSampleWidth=planet.Width;reliefSampleHeight=planet.Height;reliefSampleColumns=columns;reliefSampleRows=rows;
            int n=0;for(int z=0;z<rows;z++)
            {
                for(int x=0;x<columns;x++)
                {int a=z*(columns+1)+x,b=a+1,c=a+columns+1,d=c+1;triangles[n++]=a;triangles[n++]=c;triangles[n++]=b;triangles[n++]=b;triangles[n++]=c;triangles[n++]=d;}
                if((z+1)%16==0)yield return null;
            }
            reliefMesh=new Mesh{name="Berge und Täler",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};reliefMesh.vertices=vertices;reliefMesh.triangles=triangles;reliefMesh.RecalculateNormals();reliefMesh.RecalculateBounds();
            var terrain=new GameObject("Durchgehendes Relief");terrain.transform.SetParent(surfaceRoot,false);
            terrain.AddComponent<MeshFilter>().sharedMesh=reliefMesh;
            var terrainRenderer=terrain.AddComponent<MeshRenderer>();terrainRenderer.sharedMaterial=BiomeMaterial(planet.Kind);
            terrainRenderer.enabled=!PlanetEnvironment.Airborne(planet);
            reliefCollider=terrain.AddComponent<MeshCollider>();reliefCollider.sharedMesh=reliefMesh;
            if(planet.Kind==PlanetKind.Ocean || PlanetEnvironment.Airborne(planet))BuildEnvironmentLayer(planet,vertices,triangles);
            yield return null;
            if(reliefMap!=null)Destroy(reliefMap);
            reliefMap=new Texture2D(96,80,TextureFormat.RGB24,false){name="Kolonieradar",filterMode=FilterMode.Bilinear};
            for(int y=0;y<80;y++)
            {
                for(int x=0;x<96;x++)
                {
                    float wx=x/95f*planet.Width,wz=y/79f*planet.Height*0.866f;
                    float h=ReliefSample(wx,wz),slope=ReliefSample(wx+0.2f,wz)-h;
                    reliefMap.SetPixel(x,y,BiomeRadarColor(planet.Kind,h,slope));
                }
                if((y+1)%16==0)yield return null;
            }
            reliefMap.Apply();
            yield return null;
            for(int y=0;y<planet.Height;y++)for(int x=0;x<planet.Width;x++)
            {
                var center=SurfacePosition(x,0,y);var outline=MakeHexOutline("Wabengrenze",surfaceRoot,new Color(.23f,.30f,.27f),0.006f).GetComponent<LineRenderer>();
                outline.positionCount=24;
                outline.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;outline.receiveShadows=false;
                for(int j=0;j<24;j++)
                {
                    Vector3 point=center+Vector3.Lerp(HexCorner(j/4,1),HexCorner((j/4+1)%6,1),(j%4)/4f);
                    point.y=ReliefHeight(point.x,point.z)+0.025f;outline.SetPosition(j,point);
                }
                outline.gameObject.AddComponent<ReliefGridVisibility>();
                if((y*planet.Width+x+1)%32==0)yield return null;
            }
            if(PlanetEnvironment.Airborne(planet)){Physics.SyncTransforms();yield break;}
            var random=new System.Random(planet.Id*727+81);
            for(int i=0;i<240;i++)
            {
                if(i>0&&i%16==0)yield return null;
                float x=(float)random.NextDouble()*(planet.Width+14)-7,z=(float)random.NextDouble()*(planet.Height+10)-5;
                if(x>-0.7f && x<planet.Width && z>-0.6f && z<planet.Height*0.866f)continue;
                if(planet.Kind==PlanetKind.Ocean && ReliefHeight(x,z)<.15f)continue;
                float size=.4f+(float)random.NextDouble()*1.55f;
                if(planet.Kind==PlanetKind.Temperate || planet.Kind==PlanetKind.Ocean)
                {
                    float woodland=Mathf.PerlinNoise(x*.18f+planet.Id,z*.18f+12);
                    if(woodland>.4f)BuildTree(surfaceRoot,new Vector3(x,ReliefHeight(x,z),z),size*.67f,i);
                    else if(i%3==0)BuildShrub(surfaceRoot,new Vector3(x,ReliefHeight(x,z),z),size*.45f,i);
                }
                else Rock(surfaceRoot,new Vector3(x,ReliefHeight(x,z)+size*.09f,z),new Vector3(size,size*.58f,size*1.2f),i);
            }
            BuildGroundCover(planet);
            yield return null;
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
