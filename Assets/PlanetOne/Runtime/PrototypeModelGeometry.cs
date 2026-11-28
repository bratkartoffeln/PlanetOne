using System.Collections.Generic;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private readonly Dictionary<string,Mesh> modelMeshes=new Dictionary<string,Mesh>();

        private Transform MeshPart(string name,Transform parent,Mesh mesh,Vector3 position,Vector3 size,Color color)
        {
            var part=new GameObject(name).transform;part.SetParent(parent,false);
            part.localPosition=position;part.localScale=size;
            part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=ArtMaterial(name,color,false);
            return part;
        }

        private Transform ArmourPanel(Transform parent,string name,Vector3 at,Vector3 size,Color color)
        {
            Mesh mesh;
            if(!modelMeshes.TryGetValue("armour",out mesh))
            {
                var outline=new[]{new Vector2(-.36f,-.5f),new Vector2(-.5f,-.34f),new Vector2(-.5f,.34f),
                    new Vector2(-.36f,.5f),new Vector2(.36f,.5f),new Vector2(.5f,.34f),new Vector2(.5f,-.34f),new Vector2(.36f,-.5f)};
                float[] heights={-.5f,-.32f,.22f,.5f},scales={.88f,1,1,.81f};
                var vertices=new List<Vector3>();var faces=new List<int>();
                for(int edge=0;edge<outline.Length;edge++)
                {
                    var a=outline[edge];var b=outline[(edge+1)%outline.Length];
                    for(int level=0;level<3;level++)
                    {
                        int first=vertices.Count;
                        vertices.Add(new Vector3(a.x*scales[level],heights[level],a.y*scales[level]));
                        vertices.Add(new Vector3(b.x*scales[level],heights[level],b.y*scales[level]));
                        vertices.Add(new Vector3(b.x*scales[level+1],heights[level+1],b.y*scales[level+1]));
                        vertices.Add(new Vector3(a.x*scales[level+1],heights[level+1],a.y*scales[level+1]));
                        faces.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
                    }
                    int cap=vertices.Count;
                    vertices.Add(Vector3.up*.5f);vertices.Add(new Vector3(a.x*.81f,.5f,a.y*.81f));vertices.Add(new Vector3(b.x*.81f,.5f,b.y*.81f));
                    vertices.Add(Vector3.down*.5f);vertices.Add(new Vector3(b.x*.88f,-.5f,b.y*.88f));vertices.Add(new Vector3(a.x*.88f,-.5f,a.y*.88f));
                    faces.AddRange(new[]{cap,cap+1,cap+2,cap+3,cap+4,cap+5});
                }
                mesh=new Mesh{name="Gefaste Modulschale"};mesh.SetVertices(vertices);mesh.SetTriangles(faces,0);mesh.RecalculateNormals();mesh.RecalculateBounds();modelMeshes["armour"]=mesh;
            }
            return MeshPart(name,parent,mesh,at,size,color);
        }

        // The scout's wing root extends forward along the fuselage; the tip sweeps aft.
        private Transform ScoutWing(Transform parent,int side)
        {
            if(!modelMeshes.TryGetValue("scoutWing",out var mesh))
            {
                Vector2[] plan={new Vector2(.12f,-.32f),new Vector2(.39f,-.37f),new Vector2(.46f,-.25f),new Vector2(.15f,.26f)};
                var vertices=new List<Vector3>();var faces=new List<int>();
                for(int edge=0;edge<4;edge++)
                {
                    var a=plan[edge];var b=plan[(edge+1)%4];
                    var topA=new Vector3(a.x,.024f,a.y);var topB=new Vector3(b.x,.024f,b.y);
                    int first=vertices.Count;
                    var bottomA=topA-Vector3.up*.046f;var bottomB=topB-Vector3.up*.046f;
                    vertices.AddRange(new[]{topA,new Vector3(.265f,.047f,-.17f),topB,
                        topB,bottomA,topA,bottomB,bottomB,new Vector3(.265f,-.022f,-.17f),bottomA});
                    foreach(int index in new[]{0,1,2,3,4,5,3,6,4,7,8,9})faces.Add(first+index);
                }
                mesh=new Mesh{name="Pfeilflügel"};mesh.SetVertices(vertices);mesh.SetTriangles(faces,0);mesh.RecalculateNormals();mesh.RecalculateBounds();modelMeshes["scoutWing"]=mesh;
            }
            return MeshPart("Flügelverkleidung",parent,mesh,Vector3.zero,new Vector3(side,1,1),Hull);
        }

        // A closed, bevelled hull with a tapered bow. Forward is always local +Z.
        private Transform HullPart(Transform parent,Vector3 position,Vector3 size,Color color)
        {
            Mesh mesh;
            if(!modelMeshes.TryGetValue("hull",out mesh))
            {
                var vertices=new List<Vector3>();var indices=new List<int>();
                float[] z={-0.5f,-0.35f,0.22f,0.5f},width={0.7f,1,1,0.2f};
                Vector2[] ring={new Vector2(-0.35f,-0.5f),new Vector2(0.35f,-0.5f),new Vector2(0.5f,-0.2f),new Vector2(0.5f,0.2f),new Vector2(0.35f,0.5f),new Vector2(-0.35f,0.5f),new Vector2(-0.5f,0.2f),new Vector2(-0.5f,-0.2f)};
                for(int r=0;r<4;r++)for(int i=0;i<8;i++)vertices.Add(new Vector3(ring[i].x*width[r],ring[i].y,z[r]));
                for(int r=0;r<3;r++)for(int i=0;i<8;i++)
                {int a=r*8+i,b=r*8+(i+1)%8,c=a+8,d=b+8;indices.AddRange(new[]{a,b,c,b,d,c});}
                for(int i=1;i<7;i++){indices.AddRange(new[]{0,i+1,i,24,24+i,25+i});}
                mesh=new Mesh{name="Geschlossener Panzerkörper"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();modelMeshes["hull"]=mesh;
            }
            return MeshPart("Rumpfpanzerung",parent,mesh,position,size,color);
        }

        private Transform Rock(Transform parent,Vector3 position,Vector3 size,int seed)
        {
            string key="rock"+(seed%7);Mesh mesh;
            if(!modelMeshes.TryGetValue(key,out mesh))
            {
                float t=(1+Mathf.Sqrt(5))/2;
                Vector3[] points={new Vector3(-1,t,0),new Vector3(1,t,0),new Vector3(-1,-t,0),new Vector3(1,-t,0),new Vector3(0,-1,t),new Vector3(0,1,t),new Vector3(0,-1,-t),new Vector3(0,1,-t),new Vector3(t,0,-1),new Vector3(t,0,1),new Vector3(-t,0,-1),new Vector3(-t,0,1)};
                int[] faces={0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
                var random=new System.Random(83+seed%7);
                for(int i=0;i<points.Length;i++)points[i]=points[i].normalized*(0.4f+(float)random.NextDouble()*0.22f);
                var vertices=new Vector3[faces.Length];var indices=new int[faces.Length];
                for(int i=0;i<faces.Length;i++){vertices[i]=points[faces[i]];indices[i]=i;}
                mesh=new Mesh{name="Gebrochener Fels"};mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();modelMeshes[key]=mesh;
            }
            var rock=MeshPart("Fels",parent,mesh,position,size,new Color(0.55f,0.48f,0.40f));
            var planet=simulation.FindPlanet(openPlanetId);
            if(planet!=null && planet.Kind==PlanetOne.Core.PlanetKind.Ice)rock.GetComponent<Renderer>().sharedMaterial=BiomeMaterial(planet.Kind);
            rock.localRotation=Quaternion.Euler(seed%17,seed*137.5f,seed%23);return rock;
        }
    }
}
