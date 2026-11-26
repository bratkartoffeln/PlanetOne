using System;
using System.Collections;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
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
            sim.State.Paused=false;sim.Step();sim.State.Paused=true;
            yield return null;yield return null;
            Require(source.Ammunition==3 && target.Health<Definitions.Get(target.Kind).Health,"Visual fixture fired a real round and dealt actual simulation damage");
            Require(controller.ActiveCombatProjectileCount==1 && controller.LastPresentedCombatSequence==sim.CombatPresentationSequence,"Each actual observed round creates one projectile");
            Require(Quaternion.Angle(chassis,tank.rotation)<.01f,"Aiming the gun leaves the vehicle chassis unchanged");
            Vector3 direction=controller.UnitView(target.Id).position-tank.position;direction.y=0;
            Require(Vector3.Dot(tank.Find("Turret").forward,direction.normalized)>.95f,"Independent turret faces the actual observed opponent");
            string frozen=SaveCodec.ToJson(sim.State);yield return null;yield return null;
            Require(SaveCodec.ToJson(sim.State)==frozen && controller.ActiveCombatProjectileCount==1,"Paused projectile rendering cannot modify simulation or consume ammunition");
            var effectRoot=controller.transform.Find("Beobachtete Gefechte");
            Require(VisibleCombatBurstGeometry(effectRoot.Find("Schussreaktion")),"Paused first-frame muzzle flash has nonzero visible mesh and spark geometry");
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
            yield return Capture("combat-confirmed-explosion.png");Require(captureFailure==null,captureFailure??"Actual destruction effect captured");
            controller.SetView(0);yield return null;
            Require(controller.ActiveCombatVisualCount==0,"Changing world clears previous effects instead of replaying combat");
            controller.SetView(1);yield return null;
            Require(controller.ActiveCombatVisualCount==0,"Returning to a battlefield does not replay recorded shots");

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
