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
