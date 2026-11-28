using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private float AnimationDelta => hasSession && !menuOpen && !settingsOpen && !simulation.State.Paused
            ? Mathf.Min(.1f,Time.unscaledDeltaTime)*(float)simulation.State.TimeScale : 0;

        private void LoadingDoor(Transform root,Vector3 at,float width,float height,int side)
        {
            Part(root,"Schleusenöffnung",at,new Vector3(width*1.06f,height*1.05f,.02f),Canopy);
            var shutter=Part(root,"Motorgitter",at+Vector3.forward*(side*.015f),new Vector3(width,height,.015f),Steel);
            shutter.gameObject.AddComponent<RollingDoor>().Initialize();
            Part(root,"Torleuchte",at+new Vector3(width*.42f,height*.52f,side*.018f),new Vector3(.034f,.021f,.018f),Amber);
        }

        private void UpdateColonyActivity(PlanetState planet)
        {
            float dt=AnimationDelta;
            float time=(float)(simulation.State.Tick*Definitions.TickSeconds+simulation.State.Accumulator);
            foreach(var b in planet.Buildings)
            {
                if(!b.Built || b.Destroyed || !buildingViews.TryGetValue(b.Id,out var view) || !view.gameObject.activeSelf)continue;
                var rig=view.GetComponent<ColonyActivity>();
                UpdateWear(view,Definitions.DamageStage(b.Health,Definitions.BuildingHealth(b.Kind)),null,true);
                if(rig.Flag!=null)rig.Flag.Wave(time+b.Id%7);
                bool gate=b.Kind==BuildingKind.Gate;
                bool open=false;
                foreach(var s in planet.Shipments)
                {
                    if(s.Path.Count==0 || s.Phase==ShipmentPhase.Building)continue;
                    if(gate)
                    {
                        if(GameSimulation.BuildingOwner(planet,b)!=planet.OwnerId)continue;
                        int edge=Mathf.Clamp((int)s.DistanceTravelled,0,s.Path.Count-1);
                        for(int i=Mathf.Max(0,edge-1);i<=Mathf.Min(s.Path.Count-1,edge+1);i++)
                            if(s.Path[i].X==b.X && s.Path[i].Y==b.Y && System.Math.Abs(i-s.DistanceTravelled)<1)open=true;
                    }
                    else
                    {
                        var start=s.Path[0];var end=s.Path[s.Path.Count-1];
                        open=(start.X==b.X && start.Y==b.Y && s.DistanceTravelled<.85) ||
                            (end.X==b.X && end.Y==b.Y && s.Path.Count-1-s.DistanceTravelled<1.0);
                    }
                    if(open)break;
                }
                if(gate && !open)
                    foreach(var u in planet.Units)
                    {
                        if(u.Destroyed || !unitViews.TryGetValue(u.Id,out var unit) || !unit.gameObject.activeSelf)continue;
                        var delta=unit.position-view.position;float distance=delta.x*delta.x+delta.z*delta.z;
                        if(distance>1 || !simulation.CanTraverse(planet,u.OwnerId,u.Kind,b.X,b.Y,u.Id))continue;
                        if(distance<.28f){open=true;break;}
                        if(u.Path.Count<2)continue;
                        var next=u.Path[Mathf.Clamp((int)u.PathDistance+1,1,u.Path.Count-1)];
                        if(GameSimulation.GroundPointSegmentDistance(HexGrid.ToWorld(b.X,b.Y),GameSimulation.GroundUnitPosition(u),HexGrid.ToWorld(next.X,next.Y))<.39)
                        {open=true;break;}
                    }
                foreach(var door in rig.Doors)door.Drive(open && !b.Deconstructing,dt);
                float phase=Mathf.Repeat(time+b.Id*.37f,open?.55f:2.2f);
                Color signal=b.Deconstructing?Amber:!b.Enabled?Muted:phase>(open?.28f:.23f)?Trim:
                    open?Amber:b.CycleActive?Cyan:BuildingColor(b.Kind);
                var material=MaterialFor(signal);
                foreach(var lamp in rig.Lamps)lamp.sharedMaterial=material;
            }
        }
    }

    internal sealed class ColonyActivity:MonoBehaviour
    {
        internal RollingDoor[] Doors;
        internal Renderer[] Lamps;
        internal ColonyFlag Flag;
    }

    internal sealed class RollingDoor:MonoBehaviour
    {
        private Vector3 closed,size;
        private float open;
        internal void Initialize(){closed=transform.localPosition;size=transform.localScale;}
        internal void Drive(bool wanted,float dt)
        {
            float next=Mathf.MoveTowards(open,wanted?1:0,dt*(wanted?5:2.7f));
            if(next==open)return;
            open=next;
            float travel=size.y*.96f*Mathf.SmoothStep(0,1,open);
            transform.localScale=new Vector3(size.x,size.y-travel,size.z);
            transform.localPosition=closed+Vector3.up*(travel*.5f);
        }
    }

    internal sealed class ColonyFlag:MonoBehaviour
    {
        private const int Segments=8;
        private readonly Vector3[] vertices=new Vector3[(Segments+1)*2];
        private Mesh cloth;
        internal void Initialize(Material material)
        {
            cloth=new Mesh{name="Fahnentuch"};cloth.MarkDynamic();
            int[] triangles=new int[Segments*12];
            for(int i=0;i<Segments;i++)
            {
                int a=i*2;
                int[] quad={a,a+1,a+2,a+1,a+3,a+2,a+2,a+1,a,a+2,a+3,a+1};
                System.Array.Copy(quad,0,triangles,i*12,12);
            }
            Wave(0);cloth.triangles=triangles;
            // Both faces share vertices, so assign the fabric normal explicitly.
            var normals=new Vector3[vertices.Length];for(int i=0;i<normals.Length;i++)normals[i]=Vector3.back;
            cloth.normals=normals;
            gameObject.AddComponent<MeshFilter>().sharedMesh=cloth;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        internal void Wave(float time)
        {
            for(int i=0;i<=Segments;i++)
            {
                float u=i/(float)Segments;
                float ripple=u*(Mathf.Sin(time*4.5f-u*8)*.037f+Mathf.Sin(time*7-u*13)*.012f);
                vertices[i*2]=new Vector3(u*.23f,-u*u*.024f,ripple);
                vertices[i*2+1]=vertices[i*2]+new Vector3(0,-.112f,ripple*.22f);
            }
            cloth.vertices=vertices;cloth.RecalculateBounds();
        }
        private void OnDestroy(){if(cloth!=null)Destroy(cloth);}
    }

    internal sealed class InfantryStride:MonoBehaviour
    {
        internal int Injury;
        internal Transform Torso,Muzzle,BarrelTip;
        internal readonly Transform[] Hips=new Transform[2],Knees=new Transform[2],Feet=new Transform[2],Arms=new Transform[2];
        private Vector3 last;
        private bool initialized;
        private float phase,weight;
        internal void Pose(Vector3 position,float dt)
        {
            float travel=initialized?Vector3.Distance(position,last)/transform.lossyScale.x:0;
            last=position;initialized=true;
            if(dt<=0)return;
            float step=Mathf.Min(travel*18,dt*13);
            phase=Mathf.Repeat(phase+step,2*Mathf.PI);
            weight=Mathf.MoveTowards(weight,step>.0001f?1:0,dt*9);
            float hipsY=.216f-Injury*.004f-weight*(.017f+.005f*Mathf.Cos(phase*2));
            Torso.localPosition=Vector3.up*(hipsY-.016f);
            Torso.localRotation=Quaternion.Euler(4*weight+Injury*3,4*weight*Mathf.Cos(phase),Injury*(2+weight*Mathf.Sin(phase)*2));
            for(int i=0;i<2;i++)
            {
                float angle=phase+i*Mathf.PI;
                float swing=Mathf.Sin(angle)*weight;
                float bend=Mathf.Max(0,Mathf.Cos(angle))*weight;
                // Solve the two leg links for a planted sole on the stance half of the stride.
                float reach=.077f*swing*(i==0?1-Injury*.21f:1),down=hipsY-.016f-.057f*bend;
                float halfKnee=Mathf.Acos(Mathf.Clamp01(Mathf.Sqrt(reach*reach+down*down)/.20f))*Mathf.Rad2Deg;
                float thigh=-Mathf.Atan2(reach,down)*Mathf.Rad2Deg-halfKnee;
                var hip=Hips[i].localPosition;hip.y=hipsY;Hips[i].localPosition=hip;
                Hips[i].localRotation=Quaternion.Euler(thigh,0,0);
                Knees[i].localRotation=Quaternion.Euler(2*halfKnee,0,0);
                Feet[i].localRotation=Quaternion.Euler(-thigh-2*halfKnee+12*bend,0,0);
                Arms[i].localRotation=Quaternion.Euler((i==1?13:25)*swing,0,0);
            }
            Muzzle.position=BarrelTip.position;
        }
    }
}
