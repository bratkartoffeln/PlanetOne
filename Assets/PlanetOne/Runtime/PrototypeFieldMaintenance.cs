using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private Material smokeMaterial;
        private float SurfaceClock => (float)(simulation.State.Tick * Definitions.TickSeconds + simulation.State.Accumulator);
        private void WorkshopBody(Transform root,Color faction)
        {
            ArmourPanel(root,"Werkstattkabine",new Vector3(0,.39f,.15f),new Vector3(.39f,.23f,.28f),faction);
            ArmourPanel(root,"Werkzeugschrank",new Vector3(0,.4f,-.15f),new Vector3(.4f,.20f,.26f),Industrial);
            for(int i=0;i<3;i++)Part(root,"Schubladengriff",new Vector3(.205f,.34f+i*.05f,-.15f),new Vector3(.012f,.014f,.12f),Steel);
            Rod(root,"Hebebock",new Vector3(-.14f,.49f,-.22f),new Vector3(-.14f,.61f,.05f),.055f,Steel);
            Part(root,"Arbeitsleuchte",new Vector3(.14f,.52f,.12f),Vector3.one*.07f,Amber,PrimitiveType.Sphere);
        }
        private void UpdateWear(Transform view,int stage,InfantryStride stride,bool building)
        {
            if(view.TryGetComponent<WeaponRecoil>(out var recoil))recoil.Advance(AnimationDelta);
            var wear=view.GetComponent<FieldWear>();
            if(wear==null)
            {
                if(stage==0)return;
                wear=view.gameObject.AddComponent<FieldWear>();
                if(stride!=null)
                    wear.Patch=Part(stride.Arms[0],"Feldverband",new Vector3(0,-.04f,0),new Vector3(.078f,.048f,.08f),new Color(.78f,.69f,.56f));
                else
                {
                    wear.Patch=ArmourPanel(view,"Ruß und Blechschaden",building?new Vector3(.16f,.18f,-.32f):new Vector3(.19f,.31f,-.13f),
                        building?new Vector3(.24f,.28f,.07f):new Vector3(.14f,.07f,.19f),new Color(.16f,.12f,.09f));
                    wear.Patch.localRotation=Quaternion.Euler(7,5,-12);
                    var particles=ModelParticles(view,"Schadenspartikel",48);
                    var main=particles.main; main.simulationSpace=ParticleSystemSimulationSpace.World;
                    if(smokeMaterial==null){smokeMaterial=new Material(particleMaterial);smokeMaterial.SetFloat("_DstBlend",10);}
                    particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=smokeMaterial;
                    wear.Particles=particles;
                    wear.Vent=building?new Vector3(.16f,.36f,-.18f):new Vector3(.15f,.36f,-.19f);
                }
            }
            wear.Advance(stage,AnimationDelta,building);
        }
        private void DrawServiceMarks(PlanetState planet,float scale)
        {
            var color=Color.Lerp(new Color(.35f,.84f,.9f),Color.white,.5f+.5f*Mathf.Cos(SurfaceClock*7));
            color.a=.3f+.7f*(.5f+.5f*Mathf.Cos(SurfaceClock*7));
            foreach(var unit in planet.Units)
                if(!unit.Destroyed && unit.ActiveRepairTarget!=0 && simulation.CanSeeEntity(planet,PlayerFactionId,unit.Id) &&
                    simulation.CanSeeEntity(planet,PlayerFactionId,unit.ActiveRepairTarget))
                    DrawWrench(CombatActor(unit.ActiveRepairTarget,planet.Id),color,scale);
            foreach(var b in planet.Buildings)
                if((b.ConstructionStarted && !b.Built && !b.Destroyed && !b.Deconstructing || b.RestoreWorkerId!=0) &&
                    simulation.CanSeeEntity(planet,PlayerFactionId,b.Id) && buildingViews.TryGetValue(b.Id,out var view))
                    DrawWrench(view,color,scale);
        }
        private void DrawWrench(Transform view,Color color,float scale)
        {
            if(view==null || !view.gameObject.activeInHierarchy)return;
            var geometry=view.GetComponent<UnitSelectionGeometry>();
            if(geometry==null)geometry=view.gameObject.AddComponent<UnitSelectionGeometry>();
            if(!geometry.TryBounds(out var bounds))return;
            var screen=sceneCamera.WorldToScreenPoint(new Vector3(bounds.center.x,bounds.max.y+.22f,bounds.center.z));
            if(screen.z<=0 || !IsWorldPoint(screen))return;
            var at=new Vector2(screen.x,Screen.height-screen.y)/scale;
            var joint=at+new Vector2(2,-2);
            DrawMapLine(at+new Vector2(-6,6),joint,color,3);
            DrawMapLine(joint,joint+new Vector2(0,-5),color,3);
            DrawMapLine(joint,joint+new Vector2(5,0),color,3);
        }
    }
    internal sealed class FieldWear:MonoBehaviour
    {
        internal Transform Patch;
        internal ParticleSystem Particles;
        internal Vector3 Vent;
        private float accumulator;
        private int emitted;
        internal void Advance(int stage,float dt,bool building)
        {
            Patch.gameObject.SetActive(stage>0);
            if(Particles==null)return;
            if(stage==0){if(Particles.particleCount>0)Particles.Clear();accumulator=0;return;}
            if(dt<=0)return;
            Particles.Simulate(dt,false,false,false);
            accumulator+=dt*(stage==1?3:stage==2?8:17);
            while(accumulator>=1)
            {
                accumulator--;emitted++;
                bool spark=stage==1 || emitted%6==0, flame=stage==3 && emitted%2==0 && !spark;
                float phase=emitted*2.39996f, size=building?1:transform.lossyScale.x;
                var drift=new Vector3(Mathf.Cos(phase),0,Mathf.Sin(phase));
                var puff=new ParticleSystem.EmitParams{
                    position=transform.TransformPoint(Vent)+drift*.04f*size,
                    velocity=(Vector3.up*(spark?.65f:flame?.35f:.29f)+drift*(spark?.38f:.06f))*size,
                    startSize=(spark?.06f:flame?.19f:.25f)*size,startLifetime=spark?.34f:flame?.6f:1.45f,
                    startColor=spark?new Color(1,.83f,.32f):flame?new Color(1,.28f,.045f):new Color(.18f,.17f,.16f,.8f)};
                Particles.Emit(puff,1);
            }
        }
    }
    internal sealed class WeaponRecoil:MonoBehaviour
    {
        private Transform turret;
        private Transform[] hull;
        private Vector3 rest,direction;
        private Vector3[] hullRest;
        private float time;
        internal void Fire(Transform pivot,bool mobile)
        {
            if(turret==null)
            {
                turret=pivot;rest=pivot.localPosition;
                var parts=new List<Transform>();
                if(mobile)foreach(Transform child in transform)
                    if(child!=pivot && child.GetComponent<MeshFilter>()!=null)parts.Add(child);
                hull=parts.ToArray();hullRest=new Vector3[hull.Length];
                for(int i=0;i<hull.Length;i++)hullRest[i]=hull[i].localPosition;
            }
            direction=transform.InverseTransformDirection(pivot.forward);time=.19f;Pose();
        }
        internal void Advance(float dt){if(time<=0)return;time=Mathf.Max(0,time-dt);Pose();}
        private void Pose()
        {
            float t=time/.19f;Vector3 kick=-direction*(t*t);
            turret.localPosition=rest+kick*.085f;
            for(int i=0;i<hull.Length;i++)hull[i].localPosition=hullRest[i]+kick*.028f;
        }
    }
}
