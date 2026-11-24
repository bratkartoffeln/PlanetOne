using System;
using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private readonly Dictionary<long, Transform> shipViews = new Dictionary<long, Transform>();
        private readonly List<long> selectedShips = new List<long>();
        private readonly Dictionary<int, List<long>> shipGroups = new Dictionary<int, List<long>>();
        private bool showingSpacePanel;
        private int spacePanelTab, spaceTarget = 3, freightResource = 2;
        private long chosenPort;
        private string freightAmount = "20", supplyMinimum = "8", supplyTarget = "24", supplyReserve = "12";
        private int supplyPriority = 1;
        private Vector2? shipSelectionStart;
        private GUIStyle spaceTabStyle;
        public int RenderedShipCount { get { int n=0; foreach(var t in shipViews.Values) if(t.gameObject.activeSelf) n++; return n; } }
        public void ShowSpacePanel(int tab) { showingSpacePanel=true; spacePanelTab=Mathf.Clamp(tab,0,3); sidebarScroll=Vector2.zero; }
        public void StartSpaceFixture()
        {
            if (!simulation.State.MilitaryTestFixture && !simulation.State.SpaceTestFixture) priorSession = simulation;
            simulation=SpaceFixture.Create(); foodRates.Clear(); selectedShips.Clear(); shipGroups.Clear();
            BuildSpace(); SetView(0); showingSpacePanel=true; spacePanelTab=0; chosenPort=0; Notify(simulation.State.LastMessage);
        }
        public bool SelectShip(long id, bool additive=false)
        {
            if(!simulation.State.Ships.Exists(s=>s.Id==id && s.OwnerId==PlayerFactionId && !s.Destroyed && !s.Consumed)) return false;
            if(!additive) selectedShips.Clear(); if(!selectedShips.Contains(id)) selectedShips.Add(id);
            showingSpacePanel=true; spacePanelTab=0; return true;
        }
        public bool RequestSpaceOrder(CommandKind kind, long actor=0, long target=0, int planet=1, int value=0, int amount=0, int minimum=0, int reserve=0, int priority=1, bool flag=false)
        { return SubmitIntent(new SimCommand {Kind=kind, ActorId=actor, TargetId=target, PlanetId=planet, IntValue=value, Amount=amount, Minimum=minimum, Reserve=reserve, X=priority, BoolValue=flag}); }
        private void UpdateShipViews()
        {
            foreach(var view in shipViews.Values) view.gameObject.SetActive(false);
            foreach(var ship in simulation.State.Ships)
            {
                if(!simulation.IsShipVisible(PlayerFactionId,ship)) continue;
                Transform view;
                if(!shipViews.TryGetValue(ship.Id,out view))
                {
                    view=MakePrimitive(ship.Kind==ShipKind.ColonyShip?PrimitiveType.Capsule:PrimitiveType.Cube,"Schiff "+ship.Id,spaceRoot,Vector3.zero,new Vector3(0.32f,0.2f,0.65f),ship.OwnerId==PlayerFactionId?Cyan:Danger);
                    shipViews[ship.Id]=view;
                }
                view.gameObject.SetActive(true); view.position=new Vector3((float)ship.X,0.6f,(float)ship.Y);
                view.localScale=selectedShips.Contains(ship.Id)?new Vector3(0.5f,0.3f,0.9f):new Vector3(0.32f,0.2f,0.65f);
            }
            selectedShips.RemoveAll(id=>!simulation.State.Ships.Exists(s=>s.Id==id && !s.Destroyed && !s.Consumed));
        }
        private void HandleSpaceEvents()
        {
            if(openPlanetId!=0) return;
            var e=Event.current; var point=new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y);
            if(e.type==EventType.KeyDown)
            {
                for(int i=1;i<=3;i++) if(e.keyCode==KeyCode.Alpha1+i-1)
                { if(e.control) shipGroups[i]=new List<long>(selectedShips); else if(shipGroups.ContainsKey(i)) { selectedShips.Clear(); foreach(long id in shipGroups[i]) SelectShip(id,true); } }
            }
            if(e.type==EventType.MouseDown && e.button==0 && sceneCamera.pixelRect.Contains(point)) shipSelectionStart=point;
            if(e.type!=EventType.MouseUp || e.button!=0 || !shipSelectionStart.HasValue) return;
            var start=shipSelectionStart.Value; shipSelectionStart=null;
            bool drag=Vector2.Distance(start,point)>8; if(!e.shift) selectedShips.Clear();
            var box=Rect.MinMaxRect(Mathf.Min(start.x,point.x)-8,Mathf.Min(start.y,point.y)-8,Mathf.Max(start.x,point.x)+8,Mathf.Max(start.y,point.y)+8);
            foreach(var ship in simulation.State.Ships)
            {
                if(ship.OwnerId!=PlayerFactionId || ship.Destroyed || ship.Consumed) continue;
                Vector3 screen=sceneCamera.WorldToScreenPoint(new Vector3((float)ship.X,0.6f,(float)ship.Y));
                if(drag?box.Contains(screen):Vector2.Distance(screen,point)<20) { SelectShip(ship.Id,true); if(!drag) break; }
            }
        }
        private bool HandleSpacePointer()
        {
            if(Input.GetMouseButtonDown(1) && selectedShips.Count>0)
            {
                foreach(var p in simulation.State.Planets)
                {
                    if(!simulation.IsPlanetKnown(PlayerFactionId,p.Id)) continue;
                    var screen=sceneCamera.WorldToScreenPoint(planetViews[p.Id].position);
                    if(Vector2.Distance(screen,Input.mousePosition)>35) continue;
                    spaceTarget=p.Id; foreach(long id in selectedShips) RequestSpaceOrder(CommandKind.FlyShip,id,planet:p.Id); return true;
                }
                Ray ray=sceneCamera.ScreenPointToRay(Input.mousePosition); float distance;
                if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out distance))
                {
                    var point=ray.GetPoint(distance);
                    foreach(long id in selectedShips) SubmitIntent(new SimCommand {Kind=CommandKind.MoveShipPosition,ActorId=id,PlanetId=1,X=Mathf.RoundToInt(point.x),Y=Mathf.RoundToInt(point.z)});
                    return true;
                }
            }
            if(Input.GetMouseButtonDown(0)) foreach(var ship in simulation.State.Ships)
            {
                if(ship.OwnerId!=PlayerFactionId || ship.Destroyed || ship.Consumed) continue;
                var screen=sceneCamera.WorldToScreenPoint(new Vector3((float)ship.X,0.6f,(float)ship.Y));
                if(Vector2.Distance(screen,Input.mousePosition)<20) return true;
            }
            return false;
        }
        private void DrawShipLabels(float scale)
        {
            var labelRects = new List<Rect>();
            foreach(var ship in simulation.State.Ships)
            {
                if(!simulation.IsShipVisible(PlayerFactionId,ship)) continue;
                var screen=sceneCamera.WorldToScreenPoint(new Vector3((float)ship.X,0.6f,(float)ship.Y));
                if(!sceneCamera.pixelRect.Contains(screen)) continue;
                var labelRect = new Rect(screen.x/scale-50,(Screen.height-screen.y)/scale-28,160,16);
                while(labelRects.Exists(rect=>rect.Overlaps(labelRect))) labelRect.y-=17;
                labelRects.Add(labelRect);
                GUI.Label(labelRect,Definitions.Ships[(int)ship.Kind].Name+" #"+ship.Id,smallStyle);
                if(selectedShips.Contains(ship.Id) && (ship.Order==ShipOrder.Fly || ship.Order==ShipOrder.Guard))
                    GUI.Label(new Rect(screen.x/scale-50,(Screen.height-screen.y)/scale+8,180,24),"→ "+simulation.FindPlanet(ship.TargetPlanetId).Name,smallStyle);
                if(selectedShips.Contains(ship.Id) && ship.Order==ShipOrder.MovePosition)
                    GUI.Label(new Rect(screen.x/scale-50,(Screen.height-screen.y)/scale+8,180,24),"→ "+ship.GoalX+" / "+ship.GoalY,smallStyle);
            }
            foreach(var sight in simulation.State.SpaceSightings)
            {
                if(sight.FactionId!=PlayerFactionId || sight.ShipId==0) continue;
                var current=simulation.State.Ships.Find(s=>s.Id==sight.ShipId);
                if(current!=null && simulation.IsShipVisible(PlayerFactionId,current)) continue;
                var screen=sceneCamera.WorldToScreenPoint(new Vector3((float)sight.X,0.6f,(float)sight.Y));
                if(sceneCamera.pixelRect.Contains(screen)) GUI.Label(new Rect(screen.x/scale,(Screen.height-screen.y)/scale,170,40),"Letzte Sichtung #"+sight.ShipId+"\n"+FormatTime(sight.Tick*Definitions.TickSeconds),smallStyle);
            }
        }
        private void DrawSpacePanel()
        {
            if(spaceTabStyle==null) spaceTabStyle=new GUIStyle(GUI.skin.button) { fontSize=11, wordWrap=true, padding=new RectOffset(3,3,4,4) };
            spacePanelTab=GUILayout.Toolbar(spacePanelTab,new[]{"Flotte","Technik","Fracht","Krieg"},spaceTabStyle,GUILayout.Width(208)); GUILayout.Space(10);
            if(simulation.State.SpaceTestFixture)
            {
                GUILayout.Label("M4 DEMONSTRATION · regulär finanzierter Aufbau",smallStyle);
                if(priorSession!=null && GUILayout.Button("Zur laufenden Kolonie zurück",buttonStyle)) { simulation=priorSession; priorSession=null; foodRates.Clear(); BuildSpace(); SetView(0); return; }
            }
            if(spacePanelTab==0) DrawFleetPanel(); else if(spacePanelTab==1) DrawResearchPanel(); else if(spacePanelTab==2) DrawSupplyPanel(); else DrawInvasionPanel();
        }
        private BuildingState ChoosePort()
        {
            GUILayout.Label("RAUMHAFEN",headingStyle);
            BuildingState selected=null;
            foreach(var p in simulation.State.Planets) if(p.OwnerId==PlayerFactionId) foreach(var b in p.Buildings)
            {
                if(b.Kind!=BuildingKind.Spaceport || !b.Built || b.Deconstructing) continue;
                if(chosenPort==0) chosenPort=b.Id;
                if(GUILayout.Button(p.Name+" · #"+b.Id,chosenPort==b.Id?selectedButtonStyle:buttonStyle)) chosenPort=b.Id;
                if(chosenPort==b.Id) selected=b;
            }
            if(selected==null) GUILayout.Label("Einen Raumhafen an das Straßennetz bauen.",mutedStyle);
            else { GUILayout.Label(selected.Status,smallStyle); GUILayout.Label(GoodsText(selected.Inventory),smallStyle); }
            return selected;
        }
        private void DrawFleetPanel()
        {
            GUILayout.Label("SCHIFFE",headingStyle);
            foreach(var s in simulation.State.Ships)
            {
                if(s.OwnerId!=PlayerFactionId || s.Destroyed || s.Consumed) continue;
                if(GUILayout.Button(Definitions.Ships[(int)s.Kind].Name+" #"+s.Id,selectedShips.Contains(s.Id)?selectedButtonStyle:buttonStyle,GUILayout.Height(28))) SelectShip(s.Id,Event.current.shift);
                if(selectedShips.Contains(s.Id)) { GUILayout.Label(s.Status,smallStyle); GUILayout.Label("An Bord: "+GoodsText(s.Cargo),smallStyle); }
            }
            GUILayout.Label("ZIELWELT",headingStyle);
            foreach(var p in simulation.State.Planets)
                if(simulation.IsPlanetKnown(PlayerFactionId,p.Id) && GUILayout.Button(p.Name,spaceTarget==p.Id?selectedButtonStyle:buttonStyle)) spaceTarget=p.Id;
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Hinflug",buttonStyle)) foreach(long id in selectedShips) RequestSpaceOrder(CommandKind.FlyShip,id,planet:spaceTarget);
            if(GUILayout.Button("Bewachen",buttonStyle)) foreach(long id in selectedShips) RequestSpaceOrder(CommandKind.GuardPlanet,id,planet:spaceTarget);
            GUILayout.EndHorizontal();
            if(GUILayout.Button("Hier Kolonie gründen",buttonStyle)) foreach(long id in selectedShips) RequestSpaceOrder(CommandKind.Colonize,id,planet:spaceTarget);
            if(selectedShips.Count>1 && GUILayout.Button("Erstes ausgewähltes Schiff eskortieren",buttonStyle)) for(int i=1;i<selectedShips.Count;i++) RequestSpaceOrder(CommandKind.EscortShip,selectedShips[i],selectedShips[0]);
            if(GUILayout.Button("Zieloberfläche öffnen",buttonStyle)) SetView(spaceTarget);
            GUILayout.Space(12); var port=ChoosePort(); if(port==null) return;
            if(port.ShipBuildKind>=0) GUILayout.Label("Bauauftrag: "+Definitions.Ships[port.ShipBuildKind].Name,bodyStyle);
            else for(int i=0;i<=(int)ShipKind.OrbitalYard;i++)
            {
                var d=Definitions.Ships[i]; bool unlocked=simulation.HasTechnology(PlayerFactionId,d.Required);
                GUILayout.Label(d.Name+": "+GoodsText(d.Cost)+" · "+d.Crew+" Bewohner · "+d.Seconds+" s",smallStyle);
                GUI.enabled=unlocked;
                if(GUILayout.Button(unlocked?d.Name+" bauen":"Benötigt "+Definitions.Technologies[(int)d.Required].Name,buttonStyle)) RequestSpaceOrder(CommandKind.BuildShip,target:port.Id,planet:port.PlanetId,value:i);
                GUI.enabled=true;
            }
        }
        private void DrawResearchPanel()
        {
            var r=simulation.State.Factions.Find(f=>f.Id==PlayerFactionId).Research;
            GUILayout.Label(r.Status,bodyStyle);
            BuildingState lab=null;
            foreach(var p in simulation.State.Planets) if(p.OwnerId==PlayerFactionId) foreach(var b in p.Buildings)
                if(b.Kind==BuildingKind.Laboratory && b.Built && !b.Deconstructing)
                { if(lab==null) lab=b; if(openPlanetId==p.Id) lab=b; }
            if(lab==null) { GUILayout.Label("Ein Forschungslabor errichten. Laufender Fortschritt bleibt bei Laborverlust erhalten.",mutedStyle); return; }
            GUILayout.Label("Forschungsstandort: "+simulation.FindPlanet(lab.PlanetId).Name,smallStyle);
            foreach(var d in Definitions.Technologies)
            {
                string error=simulation.ResearchError(PlayerFactionId,d.Id);
                GUILayout.Space(8); GUILayout.Label(d.Name,headingStyle); GUILayout.Label(GoodsText(d.Cost)+" · "+d.Seconds+" s",smallStyle);
                if(error!=null) GUILayout.Label(error,smallStyle);
                GUI.enabled=error==null && (r.Active<0 || r.Active==(int)d.Id);
                if(GUILayout.Button(r.Active==(int)d.Id?"An diesem Labor fortsetzen":"Forschung beginnen",buttonStyle)) RequestSpaceOrder(CommandKind.StartResearch,target:lab.Id,planet:lab.PlanetId,value:(int)d.Id);
                GUI.enabled=true;
            }
        }
        private void DrawSupplyPanel()
        {
            var port=ChoosePort(); if(port==null) return;
            GUILayout.Label("WARE",headingStyle);
            freightResource=GUILayout.SelectionGrid(freightResource,new[]{"Nahrung","Erz","Metall","Bauteile","Kristalle","Elektronik"},2,spaceTabStyle,GUILayout.Width(208));
            int r=freightResource;
            GUILayout.Label("Aktuell: Minimum "+port.Supply.Minimum[r]+" / Ziel "+port.Supply.Target[r]+" / Exportreserve "+port.Supply.ExportReserve[r],smallStyle);
            GUILayout.Label("Zugesagt: "+port.ReservedIncoming[r]+" · reservierter Export: "+port.ReservedOutgoing[r],smallStyle);
            GUILayout.Label("Minimum / Ziel / Exportreserve",smallStyle);
            GUILayout.BeginHorizontal(); supplyMinimum=GUILayout.TextField(supplyMinimum,4,GUILayout.Width(65)); supplyTarget=GUILayout.TextField(supplyTarget,4,GUILayout.Width(65)); supplyReserve=GUILayout.TextField(supplyReserve,4,GUILayout.Width(65)); GUILayout.EndHorizontal();
            supplyPriority=GUILayout.Toolbar(supplyPriority,new[]{"Niedrig","Normal","Hoch"},spaceTabStyle,GUILayout.Width(208));
            int minimum,target,reserve,amount;
            if(GUILayout.Button("Versorgungsregel speichern",buttonStyle) && int.TryParse(supplyMinimum,out minimum) && int.TryParse(supplyTarget,out target) && int.TryParse(supplyReserve,out reserve)) RequestSpaceOrder(CommandKind.SetSupplyPolicy,target:port.Id,planet:port.PlanetId,value:r,amount:target,minimum:minimum,reserve:reserve,priority:supplyPriority);
            GUILayout.Label("Lokale Kuriere füllen den Hafen. Das automatische Netz ergänzt fehlende Ware; Exportreserve bleibt im Quellhafen.",smallStyle);
            GUILayout.Space(10); GUILayout.Label("FRACHTER",headingStyle);
            foreach(var ship in simulation.State.Ships) if(ship.OwnerId==PlayerFactionId && ship.Kind==ShipKind.Freighter && !ship.Destroyed)
            {
                if(GUILayout.Button("Frachter #"+ship.Id,selectedShips.Contains(ship.Id)?selectedButtonStyle:buttonStyle)) { selectedShips.Clear(); selectedShips.Add(ship.Id); }
                GUILayout.Label(ship.Status,smallStyle);
            }
            GUILayout.Label("Zielkolonie",headingStyle);
            foreach(var p in simulation.State.Planets) if(p.OwnerId==PlayerFactionId && GUILayout.Button(p.Name,spaceTarget==p.Id?selectedButtonStyle:buttonStyle)) spaceTarget=p.Id;
            GUILayout.Label("Menge je Fahrt (max. 40)",smallStyle); freightAmount=GUILayout.TextField(freightAmount,3);
            if(GUILayout.Button("Einmalige Lieferung",buttonStyle) && int.TryParse(freightAmount,out amount)) foreach(long id in selectedShips) RequestSpaceOrder(CommandKind.AssignFreight,id,port.Id,spaceTarget,r,amount);
            if(GUILayout.Button("Feste Route einrichten",buttonStyle) && int.TryParse(freightAmount,out amount)) foreach(long id in selectedShips) RequestSpaceOrder(CommandKind.SetTradeRoute,id,port.Id,spaceTarget,r,amount);
            if(GUILayout.Button("Automatischem Netz zuordnen",buttonStyle)) foreach(long id in selectedShips) RequestSpaceOrder(CommandKind.SetAutoSupply,id,flag:true);
            if(GUILayout.Button("Auftrag / Route abbrechen",buttonStyle)) foreach(long id in selectedShips) RequestSpaceOrder(CommandKind.CancelFreight,id);
            GUILayout.Label("Geladene Ware bleibt bei Abbruch an Bord und wird zu einem eigenen Hafen mit freiem Platz zurückgebracht.",smallStyle);
        }
    }
}
