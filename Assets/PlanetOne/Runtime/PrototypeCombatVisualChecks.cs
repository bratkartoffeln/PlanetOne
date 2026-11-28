using System;
using System.Collections;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunGroundTurnChecks()
        {
            controller.StartMilitaryFixture();controller.SetSimulationPaused(true);
            var sim=controller.Simulation;var planet=sim.FindPlanet(1);
            sim.State.Relations[0].Relation=RelationKind.Peace;
            var tank=planet.Units.Find(u=>u.Kind==UnitKind.Tank && u.OwnerId==1);
            var parked=planet.Units.Find(u=>u.Kind==UnitKind.Tank && u.OwnerId==2);
            tank.X=tank.GoalX=18;tank.Y=tank.GoalY=20;parked.X=parked.GoalX=22;parked.Y=parked.GoalY=20;
            for(int y=18;y<=22;y++)for(int x=17;x<=27;x++)planet.Terrain[y*planet.Width+x]=0;
            var center=HexGrid.ToWorld(22,20);
            controller.SetSurfaceFocus(new Vector3((float)center.X,0,(float)center.Y));controller.SetOrbitCamera(-30,65);
            Camera.main.orthographicSize=3.8f;
            controller.SelectUnit(tank.Id);controller.CloseContextPanel();
            yield return null;yield return null;
            controller.RequestUnitOrder(tank.Id,CommandKind.MoveUnit,26,20);
            controller.SetSimulationPaused(false);
            float deadline=Time.realtimeSinceStartup+16;
            bool rounded=false,interpolated=false;
            while(Time.realtimeSinceStartup<deadline && tank.X<25.98)
            {
                yield return null;
                var view=controller.UnitView(tank.Id);var at=GameSimulation.GroundUnitPosition(tank);
                if(tank.Path.Count>2 && tank.PathDistance>.6)rounded=true;
                Vector3 bar;
                if(controller.TryUnitBarAnchor(tank.Id,out bar) &&
                    Vector2.Distance(new Vector2((float)at.X,(float)at.Y),new Vector2(view.position.x,view.position.z))>.003f &&
                    Vector3.Distance(bar,Camera.main.WorldToScreenPoint(view.position+Vector3.up*.6f))<.01f)
                    interpolated=true;
            }
            controller.SetSimulationPaused(true);
            var tracks=controller.UnitView(tank.Id).parent.GetComponentInChildren<TrackField>();
            Require(rounded && tank.X>=25.98 && tracks.GetComponent<MeshFilter>().sharedMesh.vertexCount>=80,"Tank leaves paired tread marks along its completed detour");
            Require(interpolated,"Health bar follows the rendered tank between simulation ticks");
            yield return Capture("tank-turn-tracks-hud.png");Require(captureFailure==null,"Rounded tank tracks and selected unit HUD captured");
        }

        private IEnumerator RunCombatVisualChecks()
        {
            if(!controller.HasActiveSession)controller.RequestNewSession(0,"918");
            controller.SetMenuVisible(false);controller.SetSettingsVisible(false);
            controller.StartMilitaryFixture();controller.SetSimulationPaused(true);controller.CloseContextPanel();
            var sim=controller.Simulation;var planet=sim.FindPlanet(1);
            var source=planet.Units.Find(u=>u.OwnerId==1 && u.Kind==UnitKind.Tank);
            var target=planet.Units.Find(u=>u.OwnerId==2 && u.Kind==UnitKind.Tank);
            // Clearly isolated finite fixture: all other weapons are disabled, no presentation-only shot is fabricated.
            foreach(var u in planet.Units)u.Ammunition=0;
            foreach(var b in planet.Buildings){b.Enabled=false;b.Ammunition=0;}
            source.X=12;source.Y=15;source.GoalX=12;source.GoalY=15;source.Ammunition=4;
            target.X=14;target.Y=15;target.GoalX=14;target.GoalY=15;target.Health=Definitions.Get(target.Kind).Health;
            SaveCodec.Validate(sim.State);
            controller.SetSurfaceFocus(new Vector3(13.5f,0,15*(float)HexGrid.RowHeight));controller.SetOrbitCamera(0,48);
            yield return null;yield return new WaitForSecondsRealtime(.7f);
            var tank=controller.UnitView(source.Id);Quaternion chassis=tank.rotation;
            Require(tank.Find("Turret")!=null && tank.Find("Turret/MuzzlePosition")!=null,"Combat vehicle has a separate turret and physical muzzle");
            Vector3 turretRest=tank.Find("Turret").localPosition;
            var hull=tank.Find("Kettenschürze");Vector3 hullRest=hull.localPosition;
            sim.State.Paused=false;sim.Step();sim.State.Paused=true;
            yield return null;yield return null;
            Require(Vector3.Distance(turretRest,tank.Find("Turret").localPosition)>.05f &&
                Vector3.Distance(hullRest,hull.localPosition)>.015f,"Real shot recoils both turret and chassis geometry");
            Require(source.Ammunition==3 && target.Health<Definitions.Get(target.Kind).Health,"Visual fixture fired a real round and dealt actual simulation damage");
            Require(controller.ActiveCombatProjectileCount==1 && controller.LastPresentedCombatSequence==sim.CombatPresentationSequence,"Each actual observed round creates one projectile");
            Require(Quaternion.Angle(chassis,tank.rotation)<.01f,"Aiming the gun leaves the vehicle chassis unchanged");
            Vector3 direction=controller.UnitView(target.Id).position-tank.position;direction.y=0;
            Require(Vector3.Dot(tank.Find("Turret").forward,direction.normalized)>.95f,"Independent turret faces the actual observed opponent");
            string frozen=SaveCodec.ToJson(sim.State);yield return null;yield return null;
            Require(SaveCodec.ToJson(sim.State)==frozen && controller.ActiveCombatProjectileCount==1,"Paused projectile rendering cannot modify simulation or consume ammunition");
            var effectRoot=controller.transform.Find("Beobachtete Gefechte");
            Require(VisibleCombatBurstGeometry(effectRoot.Find("Schussreaktion")),"Paused first-frame muzzle flash has nonzero visible mesh and spark geometry");
            Require(effectRoot.Find("Schussreaktion/Mündungsfunken").GetComponent<ParticleSystem>().particleCount>0,"An observed shot emits particles at its muzzle");
            var projectile=effectRoot.Find("Geschoss");Vector3 origin=projectile.position;
            controller.SetSimulationPaused(false);float motionDeadline=Time.realtimeSinceStartup+2;
            do {yield return null;} while(projectile!=null && Vector3.Distance(origin,projectile.position)<=.005f && Time.realtimeSinceStartup<motionDeadline);
            controller.SetSimulationPaused(true);
            Require(projectile!=null && Vector3.Distance(origin,projectile.position)>.005f,"Projectile visibly travels between rendered frames");
            yield return Capture("combat-projectile-turret.png");Require(captureFailure==null,captureFailure??"Real projectile and independently aimed turret captured");

            // A second real round is lethal; the presentation cannot invent a destruction.
            source.Cooldown=0;target.Health=1;
            sim.State.Paused=false;sim.Step();sim.State.Paused=true;
            yield return null;yield return null;
            Require(target.Destroyed && sim.CombatPresentationEvents[sim.CombatPresentationEvents.Count-1].Kind==CombatVisualKind.Explosion,"Explosion follows actual lethal combat damage");
            Require(VisibleCombatBurstGeometry(controller.transform.Find("Beobachtete Gefechte/Bestätigte Zerstörung")),"Paused first-frame confirmed explosion has visible mesh and spark geometry");
            Require(controller.transform.Find("Beobachtete Gefechte/Bestätigte Zerstörung/Explosionsglut").GetComponent<ParticleSystem>().particleCount>0,"A confirmed explosion contains live particles");
            yield return Capture("combat-confirmed-explosion.png");Require(captureFailure==null,captureFailure??"Actual destruction effect captured");
            controller.SetView(0);yield return null;
            Require(controller.ActiveCombatVisualCount==0,"Changing world clears previous effects instead of replaying combat");
            controller.SetView(1);yield return null;
            Require(controller.ActiveCombatVisualCount==0,"Returning to a battlefield does not replay recorded shots");

            // A real powered gun tower fires at a visible fixture unit.
            controller.StartMilitaryFixture();controller.SetSimulationPaused(true);
            sim=controller.Simulation;planet=sim.FindPlanet(1);
            var tower=planet.Buildings.Find(b=>b.Kind==BuildingKind.GunTower);
            var intruder=planet.Units.Find(u=>u.OwnerId==2);intruder.X=tower.X+1;intruder.Y=tower.Y;
            tower.Priority=2;tower.Ammunition=6;
            foreach(var gun in planet.Units)gun.Ammunition=0;
            yield return null;yield return null;
            var battery=controller.ConstructionView(tower.Id).Find("Turret");
            var rest=battery.localPosition;
            sim.State.Paused=false;sim.Step();sim.State.Paused=true;yield return null;yield return null;
            Require(tower.Ammunition==5 && (battery.localPosition-rest).magnitude>.05f,"Real defensive tower shot recoils the weapon mount");
            var damaged=planet.Units.Find(u=>u.OwnerId==1 && u.Kind==UnitKind.Tank);
            damaged.X=16;damaged.Y=15;damaged.Health=30;tower.Health=32;
            sim.State.Relations[0].Relation=RelationKind.Peace;
            controller.SetSurfaceFocus(controller.UnitView(damaged.Id).position);
            controller.RequestUnitOrder(damaged.Id,CommandKind.MoveUnit,23,15);
            controller.SetSimulationPaused(false);yield return new WaitForSecondsRealtime(1.3f);
            controller.SetSimulationPaused(true);
            var tracks=controller.UnitView(damaged.Id).parent.GetComponentInChildren<TrackField>();
            Require(tracks!=null && tracks.GetComponent<MeshFilter>().sharedMesh.vertexCount>=8,"Tracked movement leaves paired ground prints");
            int vehicleParticles=controller.UnitView(damaged.Id).GetComponent<FieldWear>().Particles.particleCount;
            int towerParticles=controller.ConstructionView(tower.Id).GetComponent<FieldWear>().Particles.particleCount;
            Require(vehicleParticles>0 && towerParticles>0,"Critical vehicles and buildings emit damage particles: "+vehicleParticles+" / "+towerParticles);
            controller.SetSurfaceFocus(controller.UnitView(damaged.Id).position);Camera.main.orthographicSize=1.6f;
            controller.CaptureWorld(System.IO.Path.Combine(outputDirectory,"damaged-tank-tracks.png"));
            sim.State.Paused=false;sim.Step((int)(20/Definitions.TickSeconds));sim.State.Paused=true;
            yield return null;yield return null;
            Require(tracks.GetComponent<MeshFilter>().sharedMesh.vertexCount==0,"Old track impressions have faded from the terrain");

            var turn=RunGroundTurnChecks();while(turn.MoveNext())yield return turn.Current;
            controller.StartAsteroidFixture();controller.SetSimulationPaused(true);controller.CloseContextPanel();yield return null;
            var space=controller.Simulation;
            foreach(var asteroid in space.State.Asteroids)
            {
                var view=controller.AsteroidView(asteroid.Id);var bounds=view.GetComponent<Renderer>().bounds.size;
                Require(Mathf.Max(bounds.x,Mathf.Max(bounds.y,bounds.z))<.14f,"Enlarged rock remains below half the smallest moon: "+asteroid.Id);
                float original=.012f+(float)asteroid.DiameterKm*.0048f;
                Require(Mathf.Abs(view.localScale.x-Mathf.Min(.11f,original*1.8f))<.00001f,"Asteroid geometry is enlarged 1.8 times with a moon-safe cap: "+asteroid.Id);
            }
            float zoom=Camera.main.orthographicSize;
            foreach(float size in new[]{.25f,7f,55f,110f})
            {
                Camera.main.orthographicSize=size;yield return null;yield return null;
                foreach(var world in space.State.Planets)
                    Require(Mathf.Abs(controller.OrbitScreenWidth(world.Id)-PrototypeController.OrbitLinePixels)<.001f,"Orbit stroke keeps its screen width at zoom "+size+" / "+world.Name);
            }
            Camera.main.orthographicSize=zoom;
            controller.InspectAsteroid(space.State.Asteroids[0].Id);yield return null;
            yield return Capture("asteroid-enlarged-detail.png");Require(captureFailure==null,captureFailure??"Enlarged asteroid captured at close range");
            controller.SetView(0);
        }

        private static bool VisibleCombatBurstGeometry(Transform burst)
        {
            if(burst==null || !burst.gameObject.activeInHierarchy)return false;
            var core=burst.Find("Feuerkern");if(core==null)return false;
            var renderer=core.GetComponent<Renderer>();
            if(renderer==null || !renderer.enabled || renderer.bounds.size.sqrMagnitude<.0001f)return false;
            var bounds=renderer.bounds;var camera=Camera.main;
            var centre=camera.WorldToScreenPoint(bounds.center);
            var edge=camera.WorldToScreenPoint(bounds.center+camera.transform.up*bounds.extents.magnitude);
            if(centre.z<=0 || !camera.pixelRect.Contains(centre) || Vector2.Distance(centre,edge)<1)return false;
            foreach(var spark in burst.GetComponentsInChildren<LineRenderer>())
                if(spark.enabled && spark.positionCount>1 && spark.widthMultiplier>0 && Vector3.Distance(spark.GetPosition(0),spark.GetPosition(1))>.0001f)return true;
            return false;
        }
    }
}
