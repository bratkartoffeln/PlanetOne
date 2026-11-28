using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private TrackField trackField;
        private void StampTracks(Transform vehicle)
        {
            if(trackField==null || trackField.transform.parent!=surfaceRoot)
            {
                var root=ColonyGroup(surfaceRoot,"Kettenspuren",Vector3.zero);
                trackField=root.gameObject.AddComponent<TrackField>();trackField.Initialize();
            }
            if(!vehicle.TryGetComponent<FootprintSample>(out var sample))sample=vehicle.gameObject.AddComponent<FootprintSample>();
            var at=vehicle.position;var delta=Vector3.ProjectOnPlane(at-sample.Last,Vector3.up);
            if(!sample.Ready || delta.magnitude>1 || AnimationDelta==0){sample.Ready=true;sample.Last=at;return;}
            float scale=vehicle.lossyScale.x, step=Mathf.Max(.08f,scale*.16f);
            if(delta.magnitude<step)return;
            var forward=delta.normalized;
            var right=Vector3.ProjectOnPlane(vehicle.right,Vector3.up).normalized;
            int count=Mathf.FloorToInt(delta.magnitude/step);
            for(int i=0;i<count;i++)
            {
                sample.Last+=forward*step;
                for(int side=-1;side<=1;side+=2)
                {
                    var center=sample.Last+right*(side*.26f*scale);
                    var w=right*.067f*scale;var h=forward*step*.39f;
                    trackField.Add(TrackContact(center-w-h),TrackContact(center-w+h),TrackContact(center+w+h),TrackContact(center+w-h),SurfaceClock);
                }
            }
            sample.Last.y=at.y;
        }
        private Vector3 TrackContact(Vector3 point){point.y=ReliefHeight(point.x,point.z)+.009f;return point;}
    }
    internal sealed class FootprintSample:MonoBehaviour {internal bool Ready;internal Vector3 Last;}
    internal sealed class TrackField:MonoBehaviour
    {
        private struct Mark {internal Vector3 A,B,C,D;internal float Born;}
        private readonly Queue<Mark> marks=new Queue<Mark>();
        private readonly List<Vector3> vertices=new List<Vector3>();
        private readonly List<Vector2> uv=new List<Vector2>();
        private readonly List<Color32> colors=new List<Color32>();
        private readonly List<int> indices=new List<int>();
        private Mesh mesh;private Material material;private float nextRefresh;
        internal void Initialize()
        {
            mesh=new Mesh{name="Vergängliche Kettenabdrücke"};mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=gameObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            material=new Material(Resources.Load<Shader>("ModelParticle"));material.SetFloat("_DstBlend",10);
            renderer.sharedMaterial=material;
        }
        internal void Add(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float now)
        {
            if(marks.Count>=2048)marks.Dequeue();
            marks.Enqueue(new Mark{A=a,B=b,C=c,D=d,Born=now});
        }
        internal void Refresh(float now)
        {
            if(now<nextRefresh || marks.Count==0)return;
            nextRefresh=now+.10f;
            while(marks.Count>0 && now-marks.Peek().Born>=18)marks.Dequeue();
            vertices.Clear();colors.Clear();uv.Clear();indices.Clear();mesh.Clear();
            foreach(var m in marks)
            {
                int start=vertices.Count;
                vertices.Add(m.A);vertices.Add(m.B);vertices.Add(m.C);vertices.Add(m.D);
                var ink=new Color32(43,34,27,(byte)(135*Mathf.Clamp01((18-now+m.Born)/8)));
                for(int i=0;i<4;i++){colors.Add(ink);uv.Add(new Vector2(.5f,.5f));}
                indices.Add(start);indices.Add(start+1);indices.Add(start+2);
                indices.Add(start);indices.Add(start+2);indices.Add(start+3);
            }
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
        }
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
