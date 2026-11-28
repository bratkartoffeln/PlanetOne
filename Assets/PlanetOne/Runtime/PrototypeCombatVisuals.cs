using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private sealed class CombatFlight
        {
            public CombatPresentationEvent Fact;
            public Transform Head;
            public LineRenderer Trail;
            public Vector3 Start, End;
            public float Age, Duration, Arc;
        }
        private sealed class CombatBurst
        {
            public Transform Root, Core;
            public LineRenderer[] Sparks;
            public Vector3[] Directions;
            public float Age, Duration, Size;
            public bool Smoke;
            public ParticleSystem Particles;
        }
        private sealed class CombatAim
        {
            public int World;
            public long Target;
            public Vector3 Position;
            public double Until;
        }
        private readonly List<CombatFlight> combatFlights=new List<CombatFlight>();
        private readonly List<CombatBurst> combatBursts=new List<CombatBurst>();
        private readonly Dictionary<long,CombatAim> combatAims=new Dictionary<long,CombatAim>();
        private readonly List<long> expiredCombatAims=new List<long>();
        private GameSimulation combatObservedSimulation;
        private long combatSequence;
        private int combatWorld=-1;
        private Transform combatEffectsRoot;
        public int ActiveCombatVisualCount {get{return combatFlights.Count+combatBursts.Count;}}
        public long LastPresentedCombatSequence {get{return combatSequence;}}
        public int ActiveCombatProjectileCount {get{return combatFlights.Count;}}

        private void ClearCombatVisuals()
        {
            if(combatEffectsRoot!=null){combatEffectsRoot.gameObject.SetActive(false);Destroy(combatEffectsRoot.gameObject);}
            combatEffectsRoot=null;combatFlights.Clear();combatBursts.Clear();combatAims.Clear();
            combatWorld=openPlanetId;combatObservedSimulation=simulation;
            combatSequence=simulation==null?0:simulation.CombatPresentationSequence;
        }

        private void UpdateCombatVisuals()
        {
            if(simulation==null || sceneCamera==null)return;
            if(combatObservedSimulation!=simulation || combatWorld!=openPlanetId)ClearCombatVisuals();
            float dt=AnimationDelta;
            UpdateCombatTurrets(dt);
            foreach(var fact in simulation.CombatPresentationEvents)
            {
                if(fact.Sequence<=combatSequence)continue;
                combatSequence=fact.Sequence;
                // Only current, observed facts are animated. Viewing an old battlefield never replays combat.
                if(simulation.State.Tick-fact.Tick<=4)PresentCombatFact(fact);
            }
            for(int i=combatFlights.Count-1;i>=0;i--)
            {
                var shot=combatFlights[i];shot.Age+=dt;
                float t=Mathf.Clamp01(shot.Age/shot.Duration),previous=Mathf.Max(0,t-.16f);
                shot.Head.position=FlightPoint(shot,t);
                Vector3 direction=shot.Head.position-FlightPoint(shot,previous);
                if(direction.sqrMagnitude>.0001f)shot.Head.rotation=Quaternion.LookRotation(direction);
                shot.Trail.SetPosition(0,FlightPoint(shot,previous));shot.Trail.SetPosition(1,shot.Head.position);
                if(t<1)continue;
                if(shot.Fact.ImpactVisible && shot.Fact.TargetWorld==openPlanetId)
                    CombatImpact(shot.End,shot.Fact.Kind==CombatVisualKind.Artillery || shot.Fact.Kind==CombatVisualKind.Orbital?.65f:.24f,false);
                Destroy(shot.Head.gameObject);Destroy(shot.Trail.gameObject);combatFlights.RemoveAt(i);
            }
            for(int i=combatBursts.Count-1;i>=0;i--)
            {
                var burst=combatBursts[i];burst.Age+=dt;float t=burst.Age/burst.Duration;
                if(t>=1){Destroy(burst.Root.gameObject);combatBursts.RemoveAt(i);continue;}
                if(dt>0)burst.Particles.Simulate(dt,false,false,false);
                float growth=Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI);
                // The shot exists at age zero, including when a simulation step is immediately paused.
                // Start its flash with real geometry; elapsed animation time must not be required for visibility.
                float birthFlash=.65f*Mathf.Clamp01(1-t/.2f);
                burst.Core.localScale=Vector3.one*burst.Size*(burst.Smoke?.25f+growth*1.4f:Mathf.Max(growth,birthFlash));
                burst.Core.localPosition=Vector3.up*(burst.Smoke?t*burst.Size*.7f:0);
                for(int j=0;j<burst.Sparks.Length;j++)
                {
                    var line=burst.Sparks[j];Vector3 travel=burst.Directions[j]*burst.Size*(.08f+.92f*t)*(burst.Smoke?2.2f:1.3f);
                    line.widthMultiplier=burst.Size*.08f*(1-t);
                    line.SetPosition(0,travel*.7f);line.SetPosition(1,travel);
                }
            }
        }

        private static Vector3 FlightPoint(CombatFlight shot,float t)
        {return Vector3.Lerp(shot.Start,shot.End,t)+Vector3.up*(4*shot.Arc*t*(1-t));}

        private Transform CombatEffectRoot()
        {
            if(combatEffectsRoot==null){combatEffectsRoot=new GameObject("Beobachtete Gefechte").transform;combatEffectsRoot.SetParent(transform,false);}
            return combatEffectsRoot;
        }

        private Transform CombatActor(long id,int world)
        {
            Transform actor;
            if(world==0)return shipViews.TryGetValue(id,out actor) && actor!=null && actor.gameObject.activeInHierarchy?actor:null;
            if(world!=openPlanetId)return null;
            if(unitViews.TryGetValue(id,out actor) && actor!=null && actor.gameObject.activeInHierarchy)return actor;
            if(buildingViews.TryGetValue(id,out actor) && actor!=null && actor.gameObject.activeInHierarchy)return actor;
            return invasionViews.TryGetValue(id,out actor) && actor!=null && actor.gameObject.activeInHierarchy?actor:null;
        }

        private Vector3 CombatPosition(int world,double x,double y,long entity=0)
        {
            var actor=CombatActor(entity,world);
            if(actor!=null)return actor.position+Vector3.up*(world==0?.16f:.35f);
            return world==0?SpacePosition(x,y,ShipDisplayHeight):SurfacePosition(x,.4f,y);
        }

        private Vector3 CombatOtherViewAnchor(int world,Vector3 localPoint)
        {
            Transform planet;
            if(openPlanetId==0 && world>0 && planetViews.TryGetValue(world,out planet))
                return planet.position+Vector3.up*(planet.localScale.x*.5f);
            return localPoint+Vector3.up*5;
        }

        private void PresentCombatFact(CombatPresentationEvent fact)
        {
            bool source=fact.SourceVisible && fact.SourceWorld==openPlanetId;
            bool target=fact.TargetKnown && fact.TargetWorld==openPlanetId;
            if(fact.Kind==CombatVisualKind.Explosion)
            {if(target && fact.ImpactVisible)CombatImpact(CombatPosition(fact.TargetWorld,fact.TargetX,fact.TargetY,fact.TargetId),1.1f,true);return;}
            if(!source && !target)return;
            Vector3 end=target?CombatPosition(fact.TargetWorld,fact.TargetX,fact.TargetY,fact.TargetId):Vector3.zero;
            Vector3 start=source?CombatPosition(fact.SourceWorld,fact.SourceX,fact.SourceY,fact.SourceId):CombatOtherViewAnchor(fact.SourceWorld,end);
            if(source)
            {
                var actor=CombatActor(fact.SourceId,fact.SourceWorld);
                if(target)SetCombatAim(fact.SourceId,end,fact.TargetWorld,fact.TargetId);
                if(actor!=null)
                {
                    var turret=actor.Find("Turret");
                    // The first visible shot must leave a barrel facing its observed destination.
                    if(target && turret!=null)TurnCombatTurret(turret,end,360);
                    var muzzle=turret!=null?turret.Find("MuzzlePosition"):actor.Find("MuzzlePosition");
                    if(muzzle!=null)start=muzzle.position;
                    if(fact.SourceWorld>0 && turret!=null)
                    {
                        var recoil=actor.GetComponent<WeaponRecoil>();
                        if(recoil==null)recoil=actor.gameObject.AddComponent<WeaponRecoil>();
                        recoil.Fire(turret,unitViews.ContainsKey(fact.SourceId));
                    }
                }
                CombatImpact(start,.15f,false,.14f,target?(end-start).normalized:actor!=null?actor.forward:Vector3.up);
            }
            // An unseen attacker supplies no direction, muzzle location or identity to the renderer.
            if(!source && fact.SourceWorld<0)
            {if(target && fact.ImpactVisible)CombatImpact(end,.25f,false);return;}
            if(!target)
            {
                if(!fact.TargetKnown)return;
                // Cross-view orbital shots leave/enter the local sky without inventing another world's coordinates.
                end=CombatOtherViewAnchor(fact.TargetWorld,start);
            }
            if(combatFlights.Count>=64)return;
            bool artillery=fact.Kind==CombatVisualKind.Artillery,missile=fact.Kind==CombatVisualKind.Missile;
            Color color=missile?new Color(1,.58f,.15f):fact.Kind==CombatVisualKind.Orbital?new Color(.35f,.8f,1):new Color(1,.85f,.4f);
            var head=MakePrimitive(PrimitiveType.Sphere,"Geschoss",CombatEffectRoot(),start,Vector3.one*(missile?.09f:.055f),color);
            head.GetComponent<Renderer>().sharedMaterial=MaterialFor(color);
            var trail=MakeLine("Leuchtspur",CombatEffectRoot(),color,missile?.045f:.025f,false);
            trail.useWorldSpace=true;trail.positionCount=2;trail.SetPosition(0,start);trail.SetPosition(1,start);
            float duration=fact.Kind==CombatVisualKind.SmallArms?.12f:Mathf.Clamp(Vector3.Distance(start,end)/(missile?9:18),.16f,.8f);
            if(artillery)duration=Mathf.Max(duration,.65f);
            combatFlights.Add(new CombatFlight {Fact=fact,Head=head,Trail=trail,Start=start,End=end,Duration=duration,Arc=artillery?Mathf.Min(3,Vector3.Distance(start,end)*.3f):0});
        }

        private void CombatImpact(Vector3 point,float size,bool explosion,float duration=0,Vector3? direction=null)
        {
            if(combatBursts.Count>=96)return;
            var root=new GameObject(explosion?"Bestätigte Zerstörung":"Schussreaktion").transform;root.SetParent(CombatEffectRoot(),false);root.position=point;
            var core=MakePrimitive(PrimitiveType.Sphere,"Feuerkern",root,Vector3.zero,Vector3.one*size*.25f,new Color(1,.63f,.13f));
            core.GetComponent<Renderer>().sharedMaterial=MaterialFor(explosion?new Color(1,.31f,.06f):new Color(1,.8f,.36f));
            int count=explosion?10:5;var sparks=new LineRenderer[count];var directions=new Vector3[count];
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;
                directions[i]=new Vector3(Mathf.Cos(angle),.25f+(i%3)*.27f,Mathf.Sin(angle)).normalized;
                sparks[i]=MakeLine("Funke",root,new Color(1,.46f,.1f),size*.05f,false);sparks[i].positionCount=2;
            }
            float life=duration>0?duration:explosion?.85f:.25f;
            var particles=ModelParticles(root,explosion?"Explosionsglut":direction.HasValue?"Mündungsfunken":"Trefferfunken",48);
            int grains=explosion?42:direction.HasValue?12:18;
            for(int i=0;i<grains;i++)
            {
                // A spherical spiral spreads sparks without consuming simulation randomness.
                float y=1-(2*i+1f)/grains,r=Mathf.Sqrt(1-y*y),a=i*2.39996f;
                var ray=new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);
                if(direction.HasValue)ray=(direction.Value+ray*.55f).normalized;
                else if(openPlanetId!=0)ray.y=Mathf.Abs(ray.y);
                var spark=new ParticleSystem.EmitParams{position=ray*size*(.12f+i%4*.09f),
                    velocity=ray*(size/life)*(1.1f+i%5*.34f),startLifetime=life*(.5f+i%5*.11f),
                    startSize=size*(explosion?.23f:.34f),startColor=Color.Lerp(new Color(1,.26f,.045f),new Color(1,.94f,.58f),i%6/5f)};
                particles.Emit(spark,1);
            }
            combatBursts.Add(new CombatBurst {Root=root,Core=core,Sparks=sparks,Directions=directions,Size=size,Smoke=explosion,Duration=life,Particles=particles});
        }

        private void SetCombatAim(long actor,Vector3 position,int world,long target)
        {combatAims[actor]=new CombatAim {Position=position,World=world,Target=target,Until=simulation.State.Tick*Definitions.TickSeconds+3};}

        private void UpdateCombatTurrets(float dt)
        {
            // Explicit attack orders may aim before their next shot, but only at currently observable targets.
            if(openPlanetId>0)
            {
                var p=simulation.FindPlanet(openPlanetId);
                foreach(var u in p.Units)
                {
                    if(!u.Destroyed && u.Kind==UnitKind.Tank && (u.Order==UnitOrder.Move || u.Order==UnitOrder.Retreat) &&
                        !combatAims.ContainsKey(u.Id) && unitViews.TryGetValue(u.Id,out var view) && view.gameObject.activeSelf)
                    {
                        var pivot=view.Find("Turret");
                        if(pivot!=null)pivot.localRotation=Quaternion.RotateTowards(pivot.localRotation,Quaternion.identity,dt*110);
                    }
                    if(u.Destroyed || u.Order!=UnitOrder.Attack || !simulation.CanSeeEntity(p,PlayerFactionId,u.Id) || !simulation.CanSeeEntity(p,PlayerFactionId,u.TargetId))continue;
                    var target=CombatActor(u.TargetId,p.Id);if(target!=null)SetCombatAim(u.Id,target.position+Vector3.up*.35f,p.Id,u.TargetId);
                }
            }
            else foreach(var ship in simulation.State.Ships)
            {
                if(ship.Order!=ShipOrder.Attack || !simulation.IsShipVisible(PlayerFactionId,ship))continue;
                var enemy=simulation.State.Ships.Find(s=>s.Id==ship.CombatTargetId);
                if(enemy!=null && simulation.IsShipVisible(PlayerFactionId,enemy))SetCombatAim(ship.Id,CombatPosition(0,enemy.X,enemy.Y,enemy.Id),0,enemy.Id);
            }
            double now=simulation.State.Tick*Definitions.TickSeconds;
            expiredCombatAims.Clear();
            foreach(var item in combatAims)
            {
                if(item.Value.Until<now){expiredCombatAims.Add(item.Key);continue;}
                if(item.Value.World!=openPlanetId)continue;
                var actor=CombatActor(item.Key,openPlanetId);if(actor==null)continue;
                var turret=actor.Find("Turret");if(turret==null)continue;
                var target=CombatActor(item.Value.Target,openPlanetId);
                if(target!=null)item.Value.Position=target.position+Vector3.up*.35f;
                TurnCombatTurret(turret,item.Value.Position,dt*210);
            }
            foreach(long id in expiredCombatAims)combatAims.Remove(id);
        }

        private static void TurnCombatTurret(Transform turret,Vector3 destination,float degrees)
        {
            Vector3 local=turret.parent.InverseTransformDirection(destination-turret.position);local.y=0;
            if(local.sqrMagnitude>.00001f)turret.localRotation=Quaternion.RotateTowards(turret.localRotation,Quaternion.LookRotation(local),degrees);
        }
        public Quaternion CombatTurretRotation(long id)
        {var actor=CombatActor(id,openPlanetId);var turret=actor==null?null:actor.Find("Turret");return turret==null?Quaternion.identity:turret.rotation;}
    }
}
