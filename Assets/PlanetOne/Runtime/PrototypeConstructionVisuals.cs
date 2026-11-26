using System;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private Transform placementModel;
        private int placementModelKind=-999,placementModelX=-1,placementModelY=-1,placementRoadShape;
        private bool placementModelValid;
        private Material validPlacementMaterial,invalidPlacementMaterial;
        public bool HasPlacementModelPreview {get{return placementModel!=null && placementModel.gameObject.activeInHierarchy;}}
        public bool PlacementModelPreviewValid {get{return HasPlacementModelPreview && placementModelValid;}}
        public int PlacementPreviewRendererCount {get{return placementModel==null?0:placementModel.GetComponentsInChildren<Renderer>().Length;}}
        public string PlacementPreviewReason {get{return placementReason;}}
        internal Transform PlacementPreviewView {get{return placementModel;}}
        internal Transform ConstructionView(long id){Transform view;return buildingViews.TryGetValue(id,out view)?view:null;}
        public bool PreviewConstructionAtCell(BuildingKind? kind,int x,int y)
        {
            var planet=simulation.FindPlanet(openPlanetId);
            if(planet==null || kind.HasValue&&!Enum.IsDefined(typeof(BuildingKind),kind.Value))return false;
            placement=kind;placingRoad=!kind.HasValue;removingRoad=false;hoverX=x;hoverY=y;
            bool inside=x>=0&&y>=0&&x<planet.Width&&y<planet.Height;
            bool valid=ValidatePlacementTarget(planet,x,y,inside);
            UpdatePlacementPreview(x,y,inside,valid);return valid;
        }
        internal void EndConstructionPreview(){CancelPlacement();}
        private bool ValidatePlacementTarget(PlanetState planet,int x,int y,bool inside)
        {
            placementReason=null;
            if(!inside || planet.OwnerId!=PlayerFactionId)placementReason="Hier ist kein eigener Bauplatz.";
            else if(placement.HasValue)placementReason=simulation.ValidatePlacement(openPlanetId,placement.Value,x,y,PlayerFactionId);
            else if(PlanetEnvironment.Airborne(planet))placementReason="Gasstationen nutzen Frachtdrohnen; Straßen sind hier nicht möglich.";
            else if(placingRoad)
            {
                int index=y*planet.Width+x;
                if(planet.Terrain[index]!=0 || BuildingAt(planet,x,y)!=null || planet.Roads[index])
                    placementReason="Straße benötigt freies Gelände ohne Gebäude, Fels oder Erzvorkommen.";
            }
            else if(removingRoad&&!planet.Roads[y*planet.Width+x])placementReason="Eine vorhandene Straße zum Entfernen auswählen.";
            return placementReason==null;
        }
        private void UpdatePlacementPreview(int x,int y,bool inside,bool valid)
        {
            if(ghost==null)return;
            ghost.gameObject.SetActive(inside);
            if(!inside){if(placementModel!=null)placementModel.gameObject.SetActive(false);return;}
            ConformMarker(ghost,x,y);
            Color tint=valid?new Color(.333f,.937f,.569f):new Color(.945f,.271f,.271f);
            ghost.GetComponent<LineRenderer>().sharedMaterial=MaterialFor(tint);
            int kind=placement.HasValue?(int)placement.Value:removingRoad?-2:-1;
            int shape=0;var planet=simulation.FindPlanet(openPlanetId);
            if(kind<0)for(int i=0;i<6;i++)
            {var cell=HexGrid.Neighbor(x,y,i);if(cell.X>=0&&cell.Y>=0&&cell.X<planet.Width&&cell.Y<planet.Height&&planet.Roads[cell.Y*planet.Width+cell.X])shape|=1<<i;}
            bool rebuild=placementModel==null || placementModelKind!=kind || kind<0&&(placementModelX!=x||placementModelY!=y||placementRoadShape!=shape);
            if(rebuild)
            {
                if(placementModel!=null){placementModel.gameObject.SetActive(false);Destroy(placementModel.gameObject);}
                if(kind>=0)
                {
                    placementModel=new GameObject("Gebäudevorschau").transform;placementModel.SetParent(ghost,false);
                    if(AtmosphereSurface)BuildFloatingPlatform(placementModel);
                    PopulateBuildingModel(placementModel,(BuildingKind)kind);
                }
                else {placementModel=BuildPreviewRoadMesh(x,y);placementModel.SetParent(ghost,false);}
                foreach(var collider in placementModel.GetComponentsInChildren<Collider>()){collider.enabled=false;Destroy(collider);}
                foreach(var renderer in placementModel.GetComponentsInChildren<Renderer>())
                {renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}
                placementModelKind=kind;placementModelX=x;placementModelY=y;placementRoadShape=shape;
            }
            placementModel.gameObject.SetActive(true);
            if(kind>=0)
            {
                var at=SurfacePosition(x,0,y);float ground=at.y;
                for(int i=0;i<6;i++){var corner=at+HexCorner(i,.89f);ground=Mathf.Max(ground,ReliefHeight(corner.x,corner.z));}
                placementModel.localPosition=new Vector3(at.x,ground+.015f,at.z);
            }
            if(validPlacementMaterial==null)
            {
                var shader=Resources.Load<Shader>("PlacementPreview");
                validPlacementMaterial=new Material(shader){color=new Color(.333f,.937f,.569f,.42f)};
                invalidPlacementMaterial=new Material(shader){color=new Color(.945f,.271f,.271f,.44f)};
                var resources=gameObject.AddComponent<PlacementPreviewResources>();resources.Valid=validPlacementMaterial;resources.Invalid=invalidPlacementMaterial;
            }
            if(rebuild || placementModelValid!=valid)
                foreach(var renderer in placementModel.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=valid?validPlacementMaterial:invalidPlacementMaterial;
            placementModelValid=valid;
        }
        private void AttachConstructionVisual(Transform root,BuildingState building)
        {
            if(building.Built)return;
            var originals=root.GetComponentsInChildren<Renderer>();
            var construction=ColonyGroup(root,"Baustellenausstattung",Vector3.zero);
            var frame=ColonyGroup(construction,"Montagerahmen",Vector3.zero);
            for(int i=0;i<6;i++)
            {
                var foot=HexCorner(i,.78f);var next=HexCorner((i+1)%6,.78f);
                Rod(frame,"Montagestütze",foot+Vector3.up*.05f,foot+Vector3.up*.76f,.018f,Industrial);
                Rod(frame,"Montageriegel",foot+Vector3.up*.72f,next+Vector3.up*.72f,.016f,Steel);
                if(i%2==0)Rod(frame,"Montageverstrebung",foot+Vector3.up*.07f,next+Vector3.up*.69f,.012f,Steel);
            }
            var stacks=new Transform[4];
            for(int i=0;i<stacks.Length;i++)
            {
                stacks[i]=ColonyGroup(construction,"Angeliefertes Baumaterial",new Vector3(-.27f+i*.18f,0,-.34f));
                ColonyCrate(stacks[i],new Vector3(0,.09f,0),new Vector3(.145f,.13f,.15f),i%2==0?Industrial:Hull);
            }
            var spark=Part(construction,"Schweißanzeige",new Vector3(.3f,.4f,0),Vector3.one*.045f,Amber,PrimitiveType.Sphere);
            var visual=root.gameObject.AddComponent<ColonyConstructionVisual>();visual.Initialize(originals,construction,frame,stacks,spark);UpdateConstructionVisual(root,building);
        }
        private void UpdateConstructionVisual(Transform view,BuildingState building)
        {
            view.localScale=Vector3.one;
            var visual=view.GetComponent<ColonyConstructionVisual>();
            if(visual!=null)visual.Apply(building,simulation.State.Tick*Definitions.TickSeconds);
        }
        public string ConstructionStageText(long buildingId)
        {
            foreach(var planet in simulation.State.Planets)
            {var building=planet.Buildings.Find(b=>b.Id==buildingId);if(building!=null)return ColonyConstructionVisual.StageName(building);}
            return null;
        }
    }
    public sealed class ColonyConstructionVisual:MonoBehaviour
    {
        private Renderer[] originals;
        private Transform construction,frame,spark;
        private Transform[] stacks;
        private float highest;
        private bool finished;
        private double previousProgress,lastWorkTime=-100;
        public void Initialize(Renderer[] model,Transform stage,Transform scaffolding,Transform[] materialStacks,Transform welding)
        {
            originals=model;construction=stage;frame=scaffolding;stacks=materialStacks;spark=welding;
            foreach(var renderer in originals)highest=Mathf.Max(highest,transform.InverseTransformPoint(renderer.bounds.max).y);
            highest=Mathf.Max(.5f,highest);
        }
        public static string StageName(BuildingState building)
        {
            if(building.Built)return "Betriebsbereit";
            if(!building.ConstructionStarted)return "Materialanlieferung / Bauvorbereitung";
            float fraction=(float)(building.BuildProgress/Math.Max(.01,Definitions.Get(building.Kind).BuildSeconds));
            return fraction<.2f?"Fundament":fraction<.6f?"Tragwerk und Leitungen":"Verkleidung und Montage";
        }
        public void Apply(BuildingState building,double time)
        {
            if(originals==null || finished)return;
            var definition=Definitions.Get(building.Kind);
            float fraction=building.Built?1:Mathf.Clamp01((float)(building.BuildProgress/Math.Max(.01,definition.BuildSeconds)));
            construction.gameObject.SetActive(!building.Built);
            if(building.BuildProgress>previousProgress+.0001)lastWorkTime=time;
            previousProgress=building.BuildProgress;
            foreach(var renderer in originals)
            {
                if(renderer==null)continue;
                string label=renderer.name;
                bool foundation=label.Contains("Fundament") || label.Contains("Betriebsanzeige");
                bool structure=label.Contains("Rahmen") || label.Contains("Stütze") || label.Contains("Mast") || label.Contains("Pylon") || label.Contains("Sturz");
                float y=transform.InverseTransformPoint(renderer.bounds.center).y;
                renderer.enabled=building.Built || foundation&&building.ConstructionStarted || structure&&fraction>=.2f || fraction>=.6f&&y<=Mathf.Lerp(.08f,highest,(fraction-.6f)/.4f);
            }
            if(building.Built){finished=true;Destroy(construction.gameObject);return;}
            frame.gameObject.SetActive(building.ConstructionStarted && fraction<.95f);
            int total=0,delivered=0;
            for(int i=0;i<definition.Cost.Length;i++){total+=definition.Cost[i];delivered+=Math.Min(definition.Cost[i],Math.Max(0,building.Inventory[i]));}
            int visible=total==0?0:Mathf.CeilToInt(delivered/(float)total*stacks.Length);
            for(int i=0;i<stacks.Length;i++)stacks[i].gameObject.SetActive(!building.ConstructionStarted&&i<visible);
            spark.gameObject.SetActive(building.ConstructionStarted && time-lastWorkTime<.2 && Math.Sin(time*35)>.1);
            spark.localPosition=new Vector3(Mathf.Sin((float)time*.8f)*.32f,.12f+fraction*highest,Mathf.Cos((float)time*.8f)*.32f);
        }
    }
    public sealed class PlacementPreviewResources:MonoBehaviour
    {
        public Material Valid,Invalid;
        private void OnDestroy(){if(Valid!=null)Destroy(Valid);if(Invalid!=null)Destroy(Invalid);}
    }
}
