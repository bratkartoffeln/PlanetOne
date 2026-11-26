using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunColonyVisualChecks()
        {
            controller.SetMenuVisible(false);controller.SetSettingsVisible(false);
            controller.SetInterfaceLayout(InterfaceLayout.OrbitalBridge);controller.SetTextScale(1);
            controller.StartReferenceScenario();controller.SetSimulationPaused(true);controller.SetView(1);controller.CloseContextPanel();
            foreach(var planet in controller.Simulation.State.Planets)
                Require(planet.Width==40&&planet.Height==32,"New colony keeps 40 by 32 simulation cells: "+planet.Name);
            controller.ExportBuildingCatalogue(outputDirectory);
            var renderedModels=new HashSet<string>();
            foreach(BuildingKind kind in Enum.GetValues(typeof(BuildingKind)))
            {
                string path=Path.Combine(outputDirectory,"building-"+kind+".png");
                Require(File.Exists(path)&&new FileInfo(path).Length>256,"Actual textured building model rendered: "+kind);
                Require(renderedModels.Add(Convert.ToBase64String(File.ReadAllBytes(path))),"Each building catalogue render is distinct: "+kind);
            }
            yield return null;
            string beforePreview=SaveCodec.ToJson(controller.Simulation.State);
            var preview=gameObject.AddComponent<ColonyPreviewCapture>();preview.Controller=controller;
            preview.Kind=BuildingKind.SolarPlant;preview.X=10;preview.Y=5;
            Require(controller.PreviewConstructionAtCell(preview.Kind,preview.X,preview.Y),"Free connected site accepts the real building preview");
            Require(controller.PlacementPreviewRendererCount>6&&controller.PlacementModelPreviewValid,"Green placement preview contains the actual multipart building");
            foreach(var collider in controller.PlacementPreviewView.GetComponentsInChildren<Collider>())
                Require(!collider.enabled,"Preview geometry cannot intercept world selection");
            yield return Capture("colony-building-preview-valid.png");Require(captureFailure==null,captureFailure??"Green building preview captured");
            preview.X=8;preview.Y=6;
            Require(!controller.PreviewConstructionAtCell(preview.Kind,preview.X,preview.Y)&&controller.HasPlacementModelPreview&&!controller.PlacementModelPreviewValid&&!string.IsNullOrEmpty(controller.PlacementPreviewReason),"Occupied headquarters site shows an invalid model and a reason");
            yield return Capture("colony-building-preview-invalid.png");Require(captureFailure==null,captureFailure??"Red building preview captured");
            preview.Kind=null;preview.X=9;preview.Y=5;
            Require(controller.PreviewConstructionAtCell(null,preview.X,preview.Y)&&controller.PlacementPreviewRendererCount>0,"Free road site previews actual curved road geometry");
            yield return Capture("colony-road-preview-valid.png");Require(captureFailure==null,captureFailure??"Green road preview captured");
            preview.X=8;preview.Y=6;
            Require(!controller.PreviewConstructionAtCell(null,preview.X,preview.Y)&&!controller.PlacementModelPreviewValid,"Occupied road target uses the same placement rejection as world input");
            yield return Capture("colony-road-preview-invalid.png");Require(captureFailure==null,captureFailure??"Red road preview captured");
            preview.enabled=false;Destroy(preview);
            Require(!controller.PreviewConstructionAtCell(BuildingKind.SolarPlant,-1,-1)&&!controller.HasPlacementModelPreview&&!controller.PlacementPreviewView.gameObject.activeSelf,"Leaving the buildable map hides the actual model as well as its outline");
            Require(controller.PreviewConstructionAtCell(BuildingKind.SolarPlant,10,5)&&controller.HasPlacementModelPreview,"Returning to a valid site restores the current model preview");
            controller.EndConstructionPreview();Require(!controller.HasPlacementModelPreview&&!controller.PlacementPreviewView.gameObject.activeSelf,"Cancelling placement hides the real model immediately");
            Require(SaveCodec.ToJson(controller.Simulation.State)==beforePreview,"All model previews leave orders, stock and simulation time untouched");

            // This is a regular paid building order in a fresh scenario, without fixture stock or progress edits.
            var simulation=controller.Simulation;var colony=simulation.FindPlanet(1);
            Require(controller.RequestConstruction(1,BuildingKind.SolarPlant,10,5),"Construction animation starts with a normal validated order");
            AdvanceSeconds(Definitions.TickSeconds);
            var site=colony.Buildings.Find(b=>b.Kind==BuildingKind.SolarPlant&&b.X==10&&b.Y==5);
            Require(site!=null&&!site.Built&&!site.ConstructionStarted,"New site awaits real material delivery");
            controller.SelectBuilding(site.Id);yield return null;
            yield return Capture("colony-construction-delivery.png");Require(captureFailure==null,captureFailure??"Material preparation captured");
            bool builderSeen=false,foundationSeen=false,frameSeen=false,claddingSeen=false,finishedSeen=false;
            for(int step=0;step<960&&!finishedSeen;step++)
            {
                AdvanceSeconds(.25);
                var builder=colony.Shipments.Find(s=>s.TargetId==site.Id&&GameSimulation.IsConstructionVehicle(s));
                if(!builderSeen&&builder!=null&&builder.Phase==ShipmentPhase.BuilderOutbound)
                {
                    builderSeen=true;Require(builder.Amount==0&&!site.ConstructionStarted,"A real unloaded builder travels to the supplied site before work starts");
                    yield return null;yield return Capture("colony-construction-builder-arrival.png");Require(captureFailure==null,captureFailure??"Actual builder approach captured");
                }
                float fraction=(float)(site.BuildProgress/Definitions.Get(site.Kind).BuildSeconds);
                string stageFile=null;
                if(site.ConstructionStarted&&!foundationSeen&&fraction<.2f){foundationSeen=true;stageFile="foundation";}
                else if(!frameSeen&&fraction>=.2f&&fraction<.6f){frameSeen=true;stageFile="structure";}
                else if(!claddingSeen&&fraction>=.6f&&!site.Built){claddingSeen=true;stageFile="cladding";}
                else if(site.Built){finishedSeen=true;stageFile="completed";}
                if(stageFile==null)continue;
                Require(builder!=null&&(site.Built?builder.Phase==ShipmentPhase.BuilderReturning:builder.Phase==ShipmentPhase.Building),"Construction stage follows the real builder assignment: "+stageFile);
                yield return null;
                var view=controller.ConstructionView(site.Id);
                Require(view!=null&&view.localScale==Vector3.one,"Construction assembles model components without flattening the building: "+stageFile);
                yield return Capture("colony-construction-"+stageFile+".png");Require(captureFailure==null,captureFailure??("Real construction stage captured: "+stageFile));
            }
            Require(builderSeen&&foundationSeen&&frameSeen&&claddingSeen&&finishedSeen,"Paid builder journey exposes foundation, structure, cladding and completion");
            Require(controller.ConstructionStageText(site.Id)=="Betriebsbereit","Completed construction reports the operational state");

            // Explicit environment fixture sets its own colonies/start stock; no normal save is edited.
            controller.StartPlanetEnvironmentFixture();controller.SetSimulationPaused(true);
            var environments=controller.Simulation;
            Require(environments.State.SpaceTestFixture,"Biome render checks use the visibly labelled environment fixture");
            string fixtureBefore=SaveCodec.ToJson(environments.State);
            var kinds=new HashSet<PlanetKind>();
            foreach(var planet in environments.State.Planets)
            {
                kinds.Add(planet.Kind);controller.SetView(planet.Id);controller.CloseContextPanel();
                controller.SetSurfaceFocus(new Vector3(planet.Width*.5f,0,planet.Height*.433f));controller.SetOrbitCamera(18,52);
                Camera.main.orthographicSize=15;yield return null;yield return null;
                Require(controller.RenderedBuildingCount==planet.Buildings.Count,"Biome view renders the actual colony: "+planet.Kind);
                yield return Capture("colony-biome-"+planet.Kind+"-40x32.png");Require(captureFailure==null,captureFailure??("Larger natural terrain captured: "+planet.Kind));
                if(PlanetEnvironment.Airborne(planet))
                {
                    Require(!controller.PreviewConstructionAtCell(null,10,8)&&controller.PlacementPreviewReason.Contains("Frachtdrohnen"),"Atmospheric platforms explicitly reject a road preview");
                    controller.EndConstructionPreview();
                }
            }
            Require(kinds.Count==6,"All six planetary environments have an actual rendered view");
            Require(SaveCodec.ToJson(environments.State)==fixtureBefore,"Biome cameras and rejected previews preserve all fixture simulation state");
            controller.SetView(0);
        }
    }

    // Only installed by the explicit graphics smoke: keep its chosen cursor target stable during screen capture.
    public sealed class ColonyPreviewCapture:MonoBehaviour
    {
        public PrototypeController Controller;
        public BuildingKind? Kind;
        public int X,Y;
        private void LateUpdate(){if(Controller!=null)Controller.PreviewConstructionAtCell(Kind,X,Y);}
    }
}
