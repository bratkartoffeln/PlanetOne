using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private bool supplyOverlay,planningSupply;
        private long supplyStart;
        private List<GridCell> supplyPreview;
        private string supplyError;
        private readonly List<Rect> supplyLabelRects=new List<Rect>();
        internal bool SupplyOverlayVisible {get{return supplyOverlay;}}
        internal bool SupplyPlanning {get{return planningSupply;}}
        internal Transform CourierView(long id){Transform view;return shipmentViews.TryGetValue(id,out view)?view:null;}
        public void ToggleSupplyOverlay(){supplyOverlay=!supplyOverlay;}
        public void BeginSupplyConnection()
        {
            CancelPlacement();if(openPlanetId==0||AtmosphereSurface)return;
            planningSupply=true;supplyStart=selectedBuildingId;
            Notify(supplyStart==0?"Startgebäude anklicken · Esc: abbrechen":"Zielgebäude oder neuen Bauplatz anklicken · Esc: abbrechen");
        }
        internal bool SelectSupplyEndpoint(int x,int y)
        {
            var p=simulation.FindPlanet(openPlanetId);if(!planningSupply||p==null)return false;
            if(supplyStart==0)
            {
                var b=p.Buildings.Find(v=>v.X==x&&v.Y==y&&!v.Destroyed&&GameSimulation.BuildingOwner(p,v)==PlayerFactionId);
                if(b==null){Notify("Ein eigenes Startgebäude wählen.");return false;}
                supplyStart=b.Id;supplyError=null;return true;
            }
            var path=simulation.PlanSupplyConnection(p.Id,PlayerFactionId,supplyStart,x,y,out supplyError);
            if(path==null){Notify(supplyError);return false;}
            bool accepted=SubmitIntent(new SimCommand{Kind=CommandKind.ConnectSupply,PlanetId=p.Id,ActorId=supplyStart,X=x,Y=y});
            if(accepted){planningSupply=false;supplyPreview=null;supplyStart=0;}
            return accepted;
        }
        private void DrawSupplyControls()
        {
            if(GUILayout.Button(supplyOverlay?"Logistikansicht aus · L":"Logistikansicht · L",buttonStyle,GUILayout.MinHeight(UiRowHeight())))ToggleSupplyOverlay();

        }
        private bool HandleSupplyPointer(int x,int y)
        {
            if(!planningSupply)return false;
            PreviewSupplyEndpoint(x,y);
            if(Input.GetMouseButtonDown(0))SelectSupplyEndpoint(x,y);
            return true;
        }
        internal bool PreviewSupplyEndpoint(int x,int y)
        {
            hoverX=x;hoverY=y;
            supplyPreview=supplyStart==0?null:simulation.PlanSupplyConnection(openPlanetId,PlayerFactionId,supplyStart,x,y,out supplyError);
            return supplyPreview!=null;
        }
        private void SupplySegment(Vector3 a,Vector3 b,Color color,float scale,bool arrow=false)
        {
            var x=sceneCamera.WorldToScreenPoint(a);var y=sceneCamera.WorldToScreenPoint(b);if(x.z<=0||y.z<=0)return;
            var from=new Vector2(x.x/scale,(Screen.height-x.y)/scale);var to=new Vector2(y.x/scale,(Screen.height-y.y)/scale);
            DrawMapLine(from,to,color,1.5f/scale);
            if(arrow&&(to-from).sqrMagnitude>100)
            {
                var tip=Vector2.Lerp(from,to,.62f);var direction=(to-from).normalized;var side=new Vector2(-direction.y,direction.x);
                DrawMapLine(tip,tip-direction*6/scale+side*3/scale,color,1.5f/scale);
                DrawMapLine(tip,tip-direction*6/scale-side*3/scale,color,1.5f/scale);
            }
        }
        private void SupplyLine(List<GridCell> path,Color color,float scale,bool arrows=false)
        {
            if(path==null)return;
            for(int i=1;i<path.Count;i++)SupplySegment(SurfacePosition(path[i-1].X,.12f,path[i-1].Y),SurfacePosition(path[i].X,.12f,path[i].Y),color,scale,arrows);
        }
        private void SupplyLabel(Vector3 position,string text,Color color,float scale)
        {
            var screen=sceneCamera.WorldToScreenPoint(position);if(screen.z<=0||!IsWorldPoint(screen))return;
            var size=smallStyle.CalcSize(new GUIContent(text));
            var rect=new Rect(screen.x/scale-size.x*.5f-4,(Screen.height-screen.y)/scale-size.y-10,size.x+8,size.y+4);
            for(int i=0;i<8&&supplyLabelRects.Exists(other=>other.Overlaps(rect));i++)rect.y-=rect.height+3;
            if(supplyLabelRects.Exists(other=>other.Overlaps(rect))||rect.y<64/scale)return;
            supplyLabelRects.Add(rect);color.a=1;
            Fill(rect,new Color(.03f,.06f,.07f,.7f));
            var old=GUI.color;GUI.color=color;GUI.Label(new Rect(rect.x+4,rect.y+2,size.x,size.y),text,smallStyle);GUI.color=old;
        }
        private void DrawSupplyNetwork(float scale)
        {
            supplyLabelRects.Clear();
            if(openPlanetId==0)return;var p=simulation.FindPlanet(openPlanetId);if(p.OwnerId!=PlayerFactionId)return;
            int waiting=0,moving=0,pending=0;
            foreach(var shipment in p.Shipments)
            {
                var source=p.Buildings.Find(b=>b.Id==shipment.SourceId);var target=p.Buildings.Find(b=>b.Id==shipment.TargetId);
                if(source==null||GameSimulation.BuildingOwner(p,source)!=PlayerFactionId)continue;
                bool blocked=shipment.RouteBlocked||shipment.Traffic!=null&&shipment.Traffic.WaitTicks>8;
                if(blocked)waiting++;else moving++;
                bool relevant=supplyOverlay||shipment.SourceId==selectedBuildingId||shipment.TargetId==selectedBuildingId;
                Color color=blocked?new Color(1,.55f,.2f,.85f):new Color(.3f,.9f,.8f,.65f);
                if(relevant)
                {
                    SupplyLine(shipment.Path,color,scale,true);
                    var at=ShipmentRoadPosition(shipment);
                    string label=GameSimulation.IsConstructionVehicle(shipment)?"Baufahrzeug":shipment.Amount>0?shipment.Amount+" "+Definitions.ResourceName(shipment.Resource):"Leerfahrt";
                    SupplyLabel(at,label+(blocked?" · wartet":""),color,scale);
                }
                if(shipment.Phase==ShipmentPhase.Loading||shipment.Phase==ShipmentPhase.Unloading)
                {
                    var b=shipment.Phase==ShipmentPhase.Loading?source:target;
                    if(b!=null)
                    {
                        float progress=Mathf.Clamp01((float)(shipment.PhaseProgress/Definitions.TransferSeconds));
                        SupplyLabel(SurfacePosition(b.X,.6f,b.Y),(shipment.Phase==ShipmentPhase.Loading?"↑ Lädt ":"↓ Entlädt ")+shipment.Amount,Amber,scale);
                        var screen=sceneCamera.WorldToScreenPoint(SurfacePosition(b.X,.25f+.25f*(shipment.Phase==ShipmentPhase.Loading?progress:1-progress),b.Y));
                        if(screen.z>0&&IsWorldPoint(screen))Fill(new Rect(screen.x/scale-4,(Screen.height-screen.y)/scale-4,8,8),Amber);
                    }
                }
            }
            foreach(var delivery in p.Deliveries)
                if(supplyOverlay||delivery.SourceId==selectedBuildingId||delivery.TargetId==selectedBuildingId)
                {
                    var origin=p.Buildings.Find(b=>b.Id==delivery.SourceId);
                    if(origin==null||GameSimulation.BuildingOwner(p,origin)!=PlayerFactionId)continue;
                    pending++;
                    var path=simulation.SupplyPath(p.Id,delivery.SourceId,delivery.TargetId);
                    SupplyLine(path,new Color(1,.65f,.25f,.4f),scale);
                    if(path==null)
                    {
                        var destination=p.Buildings.Find(b=>b.Id==delivery.TargetId);
                        if(destination!=null)SupplyLabel(SurfacePosition(destination.X,.6f,destination.Y),"Zufahrt versperrt",Amber,scale);
                    }
                }
            if(selectedBuildingId!=0&&!supplyOverlay)
            {
                var hq=p.Buildings.Find(b=>b.Kind==BuildingKind.Headquarters&&GameSimulation.BuildingOwner(p,b)==PlayerFactionId);
                if(hq!=null&&p.Buildings.Exists(b=>b.Id==selectedBuildingId&&GameSimulation.BuildingOwner(p,b)==PlayerFactionId))SupplyLine(simulation.SupplyPath(p.Id,hq.Id,selectedBuildingId),new Color(.3f,.8f,.85f,.3f),scale);
            }
            if(planningSupply)
            {
                SupplyLine(supplyPreview,new Color(.3f,1,.5f,.9f),scale);
                if(supplyStart!=0&&supplyPreview==null)
                {
                    var b=p.Buildings.Find(v=>v.Id==supplyStart);
                    if(b!=null)SupplySegment(SurfacePosition(b.X,.2f,b.Y),SurfacePosition(hoverX,.2f,hoverY),new Color(1,.25f,.2f,.8f),scale);
                }
            }
            if(supplyOverlay||planningSupply)
            {
                var viewport=WorldViewport;
                GUI.Label(new Rect(viewport.x/scale+12,(Screen.height-viewport.yMax)/scale+12,650,30),planningSupply?
                    (supplyStart==0?"VERBINDUNG · Startgebäude wählen":supplyError??"VERBINDUNG · Zielgebäude oder Bauplatz wählen"):
                    "LOGISTIK · "+moving+" aktiv · "+waiting+" warten · "+pending+" Lieferungen offen · L: aus",smallStyle);
            }
        }
    }
}
