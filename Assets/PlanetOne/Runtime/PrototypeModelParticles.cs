using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private Material particleMaterial;
        private ParticleSystem ModelParticles(Transform parent,string label,int capacity)
        {
            var root=ColonyGroup(parent,label,Vector3.zero);
            var ps=root.gameObject.AddComponent<ParticleSystem>();
            ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=false;main.maxParticles=capacity;
            main.scalingMode=ParticleSystemScalingMode.Hierarchy;
            var emission=ps.emission;emission.enabled=false;
            var shape=ps.shape;shape.enabled=false;
            var size=ps.sizeOverLifetime;size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.05f));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();
            if(particleMaterial==null)particleMaterial=new Material(Resources.Load<Shader>("ModelParticle"));
            renderer.sharedMaterial=particleMaterial;renderer.minParticleSize=0;renderer.maxParticleSize=.12f;
            ps.Pause(false);
            return ps;
        }
    }

    internal sealed class ShipExhaust:MonoBehaviour
    {
        internal ParticleSystem Plume;
        private Vector3 previous;
        private bool sampled;
        private float carry;
        internal void Advance(Vector3 position,float dt)
        {
            float travel=sampled?Vector3.Distance(previous,position):0;
            previous=position;sampled=true;
            if(dt<=0)return;
            Plume.Simulate(dt,false,false,false);
            if(travel<=.000001f){carry=0;return;}
            carry+=dt*48;
            while(carry>=1)
            {
                carry--;
                for(int side=-1;side<=1;side+=2)
                {
                    var flame=new ParticleSystem.EmitParams{position=new Vector3(side*.26f,0,-.72f),
                        velocity=Vector3.back*(1.5f+Mathf.Clamp01(travel/dt)*1.4f),startLifetime=.27f,startSize=.17f,
                        startColor=side<0?new Color(.12f,.65f,1):new Color(.17f,.75f,1)};
                    Plume.Emit(flame,1);
                }
            }
        }
    }
}
