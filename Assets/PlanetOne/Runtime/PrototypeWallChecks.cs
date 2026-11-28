using System.Collections;
using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunWallChecks()
        {
            controller.StartMilitaryFixture();controller.SetSimulationPaused(true);controller.SetView(1);controller.CloseContextPanel();
            var sim=controller.Simulation;var planet=sim.FindPlanet(1);var ring=new List<BuildingState>();
            // Explicit geometry fixture; never injected into a normal game economy.
            for(int i=0;i<6;i++)
            {
                var cell=HexGrid.Neighbor(22,16,i);var kind=i==0?BuildingKind.Gate:BuildingKind.Wall;
                var b=new BuildingState{Id=sim.State.NextEntityId++,PlanetId=1,Kind=kind,X=cell.X,Y=cell.Y,Built=true,Health=Definitions.BuildingHealth(kind)};
                planet.Buildings.Add(b);ring.Add(b);
            }
            var focus=HexGrid.ToWorld(22,16);controller.SetSurfaceFocus(new Vector3((float)focus.X,0,(float)focus.Y));yield return null;yield return null;
            foreach(var b in ring)
            {
                var view=controller.ConstructionView(b.Id);int arms=0;
                foreach(var child in view.GetComponentsInChildren<Transform>())if(child.name=="Verbundene Mauer")
                {arms++;Require(child.Find("Mauerfeld").localScale.x<=.12f,"Connected fortification remains narrower than a building");}
                Require(arms==2&&view.Find("Fundament")==null,"Each ring segment connects both neighbours without a broad building foundation");
            }
            yield return Capture("connected-walls-gate.png");Require(captureFailure==null,"Closed wall ring and aligned gate captured");
            var removed=ring[2];planet.Buildings.Remove(removed);yield return null;yield return null;
            var adjacent=controller.ConstructionView(ring[1].Id);int remaining=0;
            foreach(var child in adjacent.GetComponentsInChildren<Transform>())if(child.name=="Verbundene Mauer")remaining++;
            Require(remaining==1,"Removing a neighbour rebuilds the adjacent wall connection");
            foreach(var kind in new[]{BuildingKind.GunTower,BuildingKind.AntiTankTower,BuildingKind.FlakTower,BuildingKind.ArtilleryTower})
            {
                var tower=new BuildingState{Id=sim.State.NextEntityId++,PlanetId=1,Kind=kind,X=removed.X,Y=removed.Y,Built=true,Health=Definitions.BuildingHealth(kind)};
                planet.Buildings.Add(tower);yield return null;yield return null;
                var view=controller.ConstructionView(tower.Id);
                var collar=view.Find("Maueranschluss");var weapon=view.Find("Turret");
                Require(collar.Find("Verbundene Mauer/Mauerfeld")!=null && weapon!=null,"Wall meets the fixed base of "+kind);
                float wallTop=float.MinValue;
                foreach(var wall in new[]{collar,controller.ConstructionView(ring[1].Id),controller.ConstructionView(ring[3].Id)})
                    foreach(var panel in wall.GetComponentsInChildren<MeshRenderer>())
                        wallTop=Mathf.Max(wallTop,panel.bounds.max.y);
                var movingParts=weapon.GetComponentsInChildren<MeshRenderer>();
                float underside=float.MaxValue;
                for(int yaw=0;yaw<360;yaw+=15)
                {
                    weapon.localRotation=Quaternion.Euler(0,yaw,0);
                    foreach(var part in movingParts)underside=Mathf.Min(underside,part.bounds.min.y);
                }
                Require(movingParts.Length>0 && underside>wallTop+.05f,"Rotating assembly clears both adjacent walls through 360 degrees: "+kind);
                if(kind==BuildingKind.AntiTankTower)
                {
                    float zoom=Camera.main.orthographicSize;
                    controller.SetSurfaceFocus(view.position);Camera.main.orthographicSize=1.4f;
                    controller.CaptureWorld(System.IO.Path.Combine(outputDirectory,"tower-wall-clearance.png"));
                    controller.SetSurfaceFocus(new Vector3((float)focus.X,0,(float)focus.Y));Camera.main.orthographicSize=zoom;
                }
                planet.Buildings.Remove(tower);yield return null;
            }
            controller.PreviewConstructionAtCell(BuildingKind.Wall,removed.X,removed.Y);yield return null;
            int previewArms=0;foreach(var child in controller.PlacementPreviewView.GetComponentsInChildren<Transform>())if(child.name=="Verbundene Mauer")previewArms++;
            Require(previewArms==2,"Wall placement preview connects the actual neighbouring segments");
            yield return Capture("connected-wall-preview.png");Require(captureFailure==null,"Connected wall preview captured");controller.EndConstructionPreview();
        }
    }
}
