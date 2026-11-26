using System;
using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        // Presentation plane clears the large planet models; simulation coordinates remain unchanged.
        private const float ShipDisplayHeight = 2.3f;
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
        private int supplyTask, selectedTechnology;
        private bool portPickerOpen;
        private long supplyEditPort;
        private int supplyEditResource=-1;
        private string supplyFormError;
        public int SpaceWorkspace { get { return spacePanelTab; } }
        public int SupplyWorkspace { get { return supplyTask; } }
        public void SetSpaceWorkspace(int section,int task=0)
        {
            ShowSpacePanel(section);
            if(section==0)fleetTask=Mathf.Clamp(task,0,3);
            if(section==2)supplyTask=Mathf.Clamp(task,0,1);
            if(section==3)invasionTask=Mathf.Clamp(task,0,2);
        }
        public int RenderedShipCount { get { int n=0; foreach(var t in shipViews.Values) if(t.gameObject.activeSelf) n++; return n; } }
        public void ShowSpacePanel(int tab) { CloseCommandMenu();contextOpen=true;panelOpen=true;showingDiplomacy=false;showingSpacePanel=true; spacePanelTab=Mathf.Clamp(tab,0,3);presentationSection=spacePanelTab==1?4:spacePanelTab==2?5:3; sidebarScroll=Vector2.zero; }
        public void StartSpaceFixture()
        {
            if (!simulation.State.MilitaryTestFixture && !simulation.State.SpaceTestFixture) priorSession = simulation;
            simulation=SpaceFixture.Create(); foodRates.Clear(); selectedShips.Clear(); shipGroups.Clear();
            BuildSpace(); SetView(0); showingSpacePanel=true; spacePanelTab=0; chosenPort=0; Notify(simulation.State.LastMessage);
        }
        public bool SelectShip(long id, bool additive=false)
        {
            if(!simulation.State.Ships.Exists(s=>s.Id==id && s.OwnerId==PlayerFactionId && !s.Destroyed && !s.Consumed)) return false;
            CloseCommandMenu();selectedAsteroidId=0;
            if(!additive) selectedShips.Clear(); if(!selectedShips.Contains(id)) selectedShips.Add(id);
            contextOpen=true;panelOpen=false;showingDiplomacy=false;showingSpacePanel=true;presentationSection=3;spacePanelTab=0;return true;
        }
        public bool RequestSpaceOrder(CommandKind kind, long actor=0, long target=0, int planet=1, int value=0, int amount=0, int minimum=0, int reserve=0, int priority=1, bool flag=false, long destination=0)
        { return SubmitIntent(new SimCommand {Kind=kind, ActorId=actor, DestinationId=destination, TargetId=target, PlanetId=planet, IntValue=value, Amount=amount, Minimum=minimum, Reserve=reserve, X=priority, BoolValue=flag}); }
        private void UpdateShipViews()
        {
            foreach(var view in shipViews.Values) view.gameObject.SetActive(false);
            foreach(var ship in simulation.State.Ships)
            {
                if(!simulation.IsShipVisible(PlayerFactionId,ship)) continue;
                Transform view;
                if(!shipViews.TryGetValue(ship.Id,out view))
                {
                    view=new GameObject("Schiff "+ship.Id).transform;view.SetParent(spaceRoot,false);BuildShipModel(view,ship.Kind);
                    Part(view,"Fraktionsmarkierung",new Vector3(0,0.11f,0.35f),new Vector3(0.25f,0.02f,0.08f),DiplomaticColor(ship.OwnerId));
                    shipViews[ship.Id]=view;
                }
                view.gameObject.SetActive(true); UpdateMotionPose(view,SpacePosition(ship.X,ship.Y,ShipDisplayHeight),0);
                view.localScale=Vector3.one*.75f;
            }
            selectedShips.RemoveAll(id=>!simulation.State.Ships.Exists(s=>s.Id==id && !s.Destroyed && !s.Consumed));
        }
        private void HandleSpaceEvents()
        {
            if(openPlanetId!=0) return;
            var e=Event.current; var point=new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y);
            if(suppressShipSelectionUntilUp){if(e.type==EventType.MouseUp)suppressShipSelectionUntilUp=false;return;}
            if(spaceMoveTargetMode)return;
            if(e.type==EventType.KeyDown)
            {
                for(int i=1;i<=3;i++) if(e.keyCode==KeyCode.Alpha1+i-1)
                { if(e.control) shipGroups[i]=new List<long>(selectedShips); else if(shipGroups.ContainsKey(i)) { selectedShips.Clear(); foreach(long id in shipGroups[i]) SelectShip(id,true); } }
            }
            if(e.type==EventType.MouseDown && e.button==0 && IsWorldPoint(point)) shipSelectionStart=point;
            if(e.type!=EventType.MouseUp || e.button!=0 || !shipSelectionStart.HasValue) return;
            var start=shipSelectionStart.Value; shipSelectionStart=null;
            if(!IsWorldPoint(point))return;
            bool drag=Vector2.Distance(start,point)>8; if(!e.shift) selectedShips.Clear(); if(!drag && PickAsteroid(point))return;
            var box=Rect.MinMaxRect(Mathf.Min(start.x,point.x)-8,Mathf.Min(start.y,point.y)-8,Mathf.Max(start.x,point.x)+8,Mathf.Max(start.y,point.y)+8);
            foreach(var ship in simulation.State.Ships)
            {
                if(ship.OwnerId!=PlayerFactionId || ship.Destroyed || ship.Consumed) continue;
                Vector3 screen=sceneCamera.WorldToScreenPoint(SpacePosition(ship.X,ship.Y,ShipDisplayHeight));
                if(drag?box.Contains(screen):Vector2.Distance(screen,point)<20) { SelectShip(ship.Id,true); if(!drag) break; }
            }
        }
        public bool IsShipMovePending {get{return spaceMoveTargetMode;}}
        public void BeginShipMove()
        {
            if(openPlanetId!=0 || selectedShips.Count==0)return;
            spaceMoveTargetMode=true;shipSelectionStart=null;
        }
        public bool RequestShipMoveAtScreen(Vector2 screenPoint)
        {
            if(openPlanetId!=0 || selectedShips.Count==0 || !IsWorldPoint(screenPoint))return false;
            int target=0;float nearest=float.MaxValue;
            foreach(var p in simulation.State.Planets)
            {
                if(!simulation.IsPlanetKnown(PlayerFactionId,p.Id))continue;
                float distance=Vector2.Distance(sceneCamera.WorldToScreenPoint(planetViews[p.Id].position),screenPoint);
                if(distance<Mathf.Max(24,PlanetScreenRadius(p.Id)+8) && distance<nearest){target=p.Id;nearest=distance;}
            }
            bool accepted=false;
            if(target!=0)
            {
                spaceTarget=target;
                foreach(long id in selectedShips)accepted=RequestSpaceOrder(CommandKind.FlyShip,id,planet:target) || accepted;
            }
            else
            {
                Ray ray=sceneCamera.ScreenPointToRay(screenPoint);float distance;
                if(new Plane(Vector3.up,Vector3.up*ShipDisplayHeight).Raycast(ray,out distance))
                {
                    var point=ray.GetPoint(distance);
                    foreach(long id in selectedShips)accepted=SubmitIntent(new SimCommand {Kind=CommandKind.MoveShipPosition,ActorId=id,PlanetId=1,X=Mathf.RoundToInt(point.x/SpaceDistanceScale),Y=Mathf.RoundToInt(point.z/SpaceDistanceScale)}) || accepted;
                }
            }
            if(accepted && spaceMoveTargetMode){spaceMoveTargetMode=false;suppressShipSelectionUntilUp=true;shipSelectionStart=null;}
            return accepted;
        }
        private bool HandleSpacePointer()
        {
            if(spaceMoveTargetMode && Input.GetMouseButtonDown(0) && selectedShips.Count>0)
                return RequestShipMoveAtScreen(Input.mousePosition);
            if(Input.GetMouseButtonDown(0) && PickAsteroid(Input.mousePosition))return true;
            if(Input.GetMouseButtonDown(0)) foreach(var ship in simulation.State.Ships)
            {
                if(ship.OwnerId!=PlayerFactionId || ship.Destroyed || ship.Consumed) continue;
                var screen=sceneCamera.WorldToScreenPoint(SpacePosition(ship.X,ship.Y,ShipDisplayHeight));
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
                var screen=sceneCamera.WorldToScreenPoint(SpacePosition(ship.X,ship.Y,ShipDisplayHeight));
                if(!sceneCamera.pixelRect.Contains(screen)) continue;
                string name=Definitions.Ships[(int)ship.Kind].Name+" #"+ship.Id;
                Vector2 size=smallStyle.CalcSize(new GUIContent(name));
                var labelRect = new Rect(screen.x/scale-size.x*.5f-4,(Screen.height-screen.y)/scale-size.y-12,size.x+8,size.y+4);
                while(labelRects.Exists(rect=>rect.Overlaps(labelRect))) labelRect.y-=labelRect.height+2;
                labelRects.Add(labelRect);
                Fill(labelRect,new Color(0.015f,0.035f,0.05f,0.88f));
                GUI.Label(labelRect,name,smallStyle);
                if(selectedShips.Contains(ship.Id) && (ship.Order==ShipOrder.Fly || ship.Order==ShipOrder.Guard))
                    GUI.Label(new Rect(screen.x/scale-50,(Screen.height-screen.y)/scale+8,260,UiRowHeight(30)),"→ "+simulation.FindPlanet(ship.TargetPlanetId).Name,smallStyle);
                if(selectedShips.Contains(ship.Id) && ship.Order==ShipOrder.MovePosition)
                    GUI.Label(new Rect(screen.x/scale-50,(Screen.height-screen.y)/scale+8,260,UiRowHeight(30)),"→ "+ship.GoalX+" / "+ship.GoalY,smallStyle);
            }
            foreach(var sight in simulation.State.SpaceSightings)
            {
                if(sight.FactionId!=PlayerFactionId || sight.ShipId==0) continue;
                var current=simulation.State.Ships.Find(s=>s.Id==sight.ShipId);
                if(current!=null && simulation.IsShipVisible(PlayerFactionId,current)) continue;
                var screen=sceneCamera.WorldToScreenPoint(SpacePosition(sight.X,sight.Y,ShipDisplayHeight));
                if(sceneCamera.pixelRect.Contains(screen)) GUI.Label(new Rect(screen.x/scale,(Screen.height-screen.y)/scale,260,UiRowHeight(30)*2),"Letzte Sichtung #"+sight.ShipId+"\n"+FormatTime(sight.Tick*Definitions.TickSeconds),smallStyle);
            }
        }
        private void DrawSpacePanel()
        {
            PrepareWorkspaceStyle();
            int next=DrawWorkspaceTabs(spacePanelTab,new[]{"Flotte","Forschung","Versorgung","Raumkampf"});
            if(next!=spacePanelTab)ShowSpacePanel(next);
            GUILayout.Space(14);
            if(simulation.State.SpaceTestFixture)
            {
                GUILayout.Label("PRÜFKARTE · separate Testsitzung",smallStyle);
                if(priorSession!=null && GUILayout.Button("Zur laufenden Kolonie zurück",buttonStyle)) { RestorePreviousSession(); return; }
            }
            if(spacePanelTab==0) DrawFleetPanel(); else if(spacePanelTab==1) DrawResearchPanel(); else if(spacePanelTab==2) DrawSupplyPanel(); else DrawInvasionPanel();
        }
        private void PrepareWorkspaceStyle()
        {
            if(spaceTabStyle==null)spaceTabStyle=new GUIStyle(buttonStyle);
            spaceTabStyle.fontSize=FontUnitSize(17);spaceTabStyle.wordWrap=true;spaceTabStyle.alignment=TextAnchor.MiddleCenter;
            spaceTabStyle.padding=new RectOffset(8,8,8,8);spaceTabStyle.fixedWidth=0;spaceTabStyle.stretchWidth=true;
            spaceTabStyle.normal.background=buttonTexture;spaceTabStyle.hover.background=hoverTexture;
        }
        private int DrawWorkspaceTabs(int selected,string[] labels)
        {
            PrepareWorkspaceStyle();
            float longest=0;
            foreach(string label in labels)foreach(string word in label.Split(' '))longest=Mathf.Max(longest,spaceTabStyle.CalcSize(new GUIContent(word)).x);
            int columns=longest*2+12>SideWidth-52?1:2;
            for(int i=0;i<labels.Length;i+=columns)
            {
                GUILayout.BeginHorizontal();
                for(int j=i;j<Mathf.Min(i+columns,labels.Length);j++)
                    if(GUILayout.Button(labels[j],selected==j?selectedButtonStyle:spaceTabStyle,GUILayout.MinHeight(UiRowHeight(42)),GUILayout.ExpandWidth(true)))selected=j;
                if(columns==2 && i+1>=labels.Length)GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            return selected;
        }
        private bool WorkspaceButton(string label,bool selected=false)
        {return GUILayout.Button(label,selected?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight(36)));}
        private void WorkspaceHeading(string title,string hint=null)
        {GUILayout.Space(10);GUILayout.Label(title,headingStyle);if(!string.IsNullOrEmpty(hint))GUILayout.Label(hint,smallStyle);GUILayout.Space(6);}
        private BuildingState ChoosePort()
        {
            var ports=new List<BuildingState>();
            foreach(var p in simulation.State.Planets) if(p.OwnerId==PlayerFactionId) foreach(var b in p.Buildings)
            {
                if(b.Kind==BuildingKind.Spaceport && b.Built && !b.Destroyed && !b.Deconstructing && GameSimulation.BuildingOwner(p,b)==PlayerFactionId)ports.Add(b);
            }
            BuildingState selected=ports.Find(b=>b.Id==chosenPort);
            if(selected==null && ports.Count>0){selected=ports[0];chosenPort=selected.Id;}
            WorkspaceHeading("RAUMHAFEN");
            if(selected==null) GUILayout.Label(AtmosphereSurface?"Eine Raumhafen-Plattform für Importe bauen.":"Einen Raumhafen an das Straßennetz bauen.",mutedStyle);
            else
            {
                if(WorkspaceButton(simulation.FindPlanet(selected.PlanetId).Name+" · Hafen #"+selected.Id+(ports.Count>1?"  ▾":""),true) && ports.Count>1)portPickerOpen=!portPickerOpen;
                if(portPickerOpen && ports.Count>1)foreach(var port in ports)
                    if(WorkspaceButton(simulation.FindPlanet(port.PlanetId).Name+" · Hafen #"+port.Id,port.Id==chosenPort)){chosenPort=port.Id;selected=port;portPickerOpen=false;}
                GUILayout.Label(selected.Status,smallStyle);GUILayout.Label("Verfügbar: "+GoodsText(selected.Inventory),smallStyle);
            }
            return selected;
        }
        private void DrawFleetPanel()
        {
            int next=DrawWorkspaceTabs(fleetTask,new[]{"Schiffe","Aufträge","Schiffbau","Abbau"});
            if(next!=fleetTask){fleetTask=next;sidebarScroll=Vector2.zero;}
            GUILayout.Space(12);
            if(fleetTask==3){miningPanel=true;DrawMiningPanel();return;}
            if(fleetTask==0)
            {
            WorkspaceHeading("EIGENE SCHIFFE","Schiff auswählen; Umschalt ergänzt die Auswahl.");
            bool any=false;
            foreach(var s in simulation.State.Ships)
            {
                if(s.OwnerId!=PlayerFactionId || s.Destroyed || s.Consumed) continue;
                any=true;
                if(WorkspaceButton(Definitions.Ships[(int)s.Kind].Name+" #"+s.Id,selectedShips.Contains(s.Id))) {SelectShip(s.Id,Event.current.shift);panelOpen=true;}
                if(selectedShips.Contains(s.Id)) { GUILayout.Label(s.Status,bodyStyle); GUILayout.Label("An Bord: "+GoodsText(s.Cargo),smallStyle);GUILayout.Space(6); }
            }
            if(!any)GUILayout.Label("Noch keine Schiffe. Unter Schiffbau einen eigenen Raumhafen wählen.",mutedStyle);
            if(!any && WorkspaceButton("Zum Schiffbau")){fleetTask=2;sidebarScroll=Vector2.zero;}
            if(selectedShips.Count>0 && WorkspaceButton("Befehle für die Auswahl …",true))OpenSelectionCommandMenu(Input.mousePosition);
            GUILayout.Space(12);GUILayout.Label("Ein Schiff auswählen. Rechtsklick fliegt zum Ziel oder greift einen Gegner an. Strg + Rechtsklick öffnet weitere Befehle.",smallStyle);return;
            }
            if(fleetTask==1)
            {
            GUILayout.Label("FLOTTENAUFTRAG",headingStyle);
            if(selectedShips.Count==0){GUILayout.Label("Wähle zuerst ein Schiff oder eine Gruppe aus.",mutedStyle);if(WorkspaceButton("Schiffe auswählen"))fleetTask=0;return;}
            GUILayout.Label(selectedShips.Count+" ausgewählte Schiffe",smallStyle);
            GUILayout.Label("ZIELWELT",headingStyle);
            foreach(var p in simulation.State.Planets)
                if(simulation.IsPlanetKnown(PlayerFactionId,p.Id) && WorkspaceButton(p.Name,spaceTarget==p.Id)) spaceTarget=p.Id;
            var targetWorld=simulation.FindPlanet(spaceTarget);
            if(targetWorld!=null && !simulation.IsPlanetKnown(PlayerFactionId,spaceTarget))targetWorld=null;
            bool enabled=GUI.enabled;GUI.enabled=enabled && targetWorld!=null && simulation.IsPlanetKnown(PlayerFactionId,spaceTarget);
            WorkspaceHeading("AKTION",targetWorld==null?"Zielwelt auswählen":selectedShips.Count+" Schiffe → "+targetWorld.Name);
            if(WorkspaceButton("Zum Ziel fliegen",true)) foreach(long id in selectedShips) RequestSpaceOrder(CommandKind.FlyShip,id,planet:spaceTarget);
            if(WorkspaceButton("Zielwelt bewachen")) foreach(long id in selectedShips) RequestSpaceOrder(CommandKind.GuardPlanet,id,planet:spaceTarget);
            if(selectedShips.Exists(id=>simulation.State.Ships.Exists(s=>s.Id==id && s.Kind==ShipKind.ColonyShip)) && WorkspaceButton("Hier Kolonie gründen")) foreach(long id in selectedShips) if(simulation.State.Ships.Exists(s=>s.Id==id && s.Kind==ShipKind.ColonyShip))RequestSpaceOrder(CommandKind.Colonize,id,planet:spaceTarget);
            if(WorkspaceButton("Zieloberfläche öffnen")) SetView(spaceTarget);
            GUI.enabled=enabled;
            if(selectedShips.Count>1 && WorkspaceButton("Erstes ausgewähltes Schiff eskortieren")) for(int i=1;i<selectedShips.Count;i++) RequestSpaceOrder(CommandKind.EscortShip,selectedShips[i],selectedShips[0]);
            return;
            }
            GUILayout.Space(12); var port=ChoosePort(); if(port==null) return;
            if(port.ShipBuildKind>=0) GUILayout.Label("Bauauftrag: "+Definitions.Ships[port.ShipBuildKind].Name,bodyStyle);
            else for(int i=0;i<=(int)ShipKind.OrbitalYard;i++)
            {
                var d=Definitions.Ships[i]; bool unlocked=simulation.HasTechnology(PlayerFactionId,d.Required);
                WorkspaceHeading(d.Name);GUILayout.Label(GoodsText(d.Cost)+" · "+d.Crew+" Bewohner · "+d.Seconds+" s",smallStyle);
                bool wasEnabled=GUI.enabled;GUI.enabled=wasEnabled && unlocked;
                if(WorkspaceButton(unlocked?d.Name+" bauen":"Benötigt "+Definitions.Technologies[(int)d.Required].Name)) RequestSpaceOrder(CommandKind.BuildShip,target:port.Id,planet:port.PlanetId,value:i);
                GUI.enabled=wasEnabled;
            }
        }
        private void DrawResearchPanel()
        {
            var r=simulation.State.Factions.Find(f=>f.Id==PlayerFactionId).Research;
            WorkspaceHeading("FORSCHUNG",r.Status);
            BuildingState lab=null;
            foreach(var p in simulation.State.Planets) if(p.OwnerId==PlayerFactionId) foreach(var b in p.Buildings)
                if(b.Kind==BuildingKind.Laboratory && b.Built && !b.Destroyed && !b.Deconstructing && GameSimulation.BuildingOwner(p,b)==PlayerFactionId)
                { if(lab==null) lab=b; if(openPlanetId==p.Id) lab=b; }
            if(lab==null)GUILayout.Label("Ein Forschungslabor errichten. Bereits erzielter Fortschritt bleibt bei Laborverlust erhalten.",mutedStyle);
            else GUILayout.Label("Forschungsstandort: "+simulation.FindPlanet(lab.PlanetId).Name,smallStyle);
            if(r.Active>=0)
            {
                var active=Definitions.Technologies[r.Active];
                GUILayout.Label(active.Name+" · "+Mathf.Clamp(Mathf.RoundToInt((float)(r.Progress/active.Seconds*100)),0,100)+" %",bodyStyle);
            }
            WorkspaceHeading("TECHNOLOGIE AUSWÄHLEN");
            foreach(var d in Definitions.Technologies)
            {
                string state=r.Completed.Contains(d.Id)?" · Erforscht":r.Active==(int)d.Id?" · Läuft":"";
                if(WorkspaceButton(d.Name+state,selectedTechnology==(int)d.Id))selectedTechnology=(int)d.Id;
            }
            var selected=Definitions.Technologies[Mathf.Clamp(selectedTechnology,0,Definitions.Technologies.Length-1)];
            WorkspaceHeading(selected.Name);GUILayout.Label(GoodsText(selected.Cost)+" · "+selected.Seconds+" s",bodyStyle);
            if(selected.Prerequisites.Length>0)
            {var names=new List<string>();foreach(var prerequisite in selected.Prerequisites)names.Add(Definitions.Technologies[(int)prerequisite].Name);GUILayout.Label("Voraussetzung: "+string.Join(", ",names),smallStyle);}
            string error=simulation.ResearchError(PlayerFactionId,selected.Id);
            if(error!=null)GUILayout.Label(error,smallStyle);
            bool wasEnabled=GUI.enabled;GUI.enabled=wasEnabled && lab!=null && error==null && (r.Active<0 || r.Active==(int)selected.Id);
            if(WorkspaceButton(r.Active==(int)selected.Id?"An diesem Labor fortsetzen":"Forschung beginnen",true))RequestSpaceOrder(CommandKind.StartResearch,target:lab.Id,planet:lab.PlanetId,value:(int)selected.Id);
            GUI.enabled=wasEnabled;
        }
        private void DrawSupplyPanel()
        {
            int next=DrawWorkspaceTabs(supplyTask,new[]{"Hafenregeln","Transport"});
            if(next!=supplyTask){supplyTask=next;sidebarScroll=Vector2.zero;supplyFormError=null;}
            var port=ChoosePort(); if(port==null) return;
            WorkspaceHeading("WARE");
            freightResource=DrawWorkspaceTabs(freightResource,new[]{"Nahrung","Erz","Metall","Bauteile","Kristalle","Elektronik"});
            int r=freightResource;
            if(supplyEditPort!=port.Id || supplyEditResource!=r)
            {
                supplyEditPort=port.Id;supplyEditResource=r;supplyMinimum=port.Supply.Minimum[r].ToString();supplyTarget=port.Supply.Target[r].ToString();supplyReserve=port.Supply.ExportReserve[r].ToString();supplyPriority=port.Supply.Priority[r];supplyFormError=null;
            }
            if(supplyTask==0){DrawSupplyRuleEditor(port,r);return;}
            DrawTransportEditor(port,r);
        }
        private void DrawSupplyRuleEditor(BuildingState port,int r)
        {
            WorkspaceHeading("AUTOMATISCHE VERSORGUNG","Lokale Kuriere füllen den Hafen. Das automatische Netz ergänzt fehlende Waren.");
            GUILayout.Label("Aktuell: Minimum "+port.Supply.Minimum[r]+" / Ziel "+port.Supply.Target[r]+" / Exportreserve "+port.Supply.ExportReserve[r],smallStyle);
            GUILayout.Label("Zugesagt: "+port.ReservedIncoming[r]+" · reservierter Export: "+port.ReservedOutgoing[r],smallStyle);
            GUILayout.Space(10);GUILayout.Label("Mindestbestand · Nachschub anfordern unter",smallStyle);supplyMinimum=GUILayout.TextField(supplyMinimum,4,GUILayout.MinHeight(UiRowHeight(32)));
            GUILayout.Label("Zielbestand · bis zu diesem Wert auffüllen",smallStyle);supplyTarget=GUILayout.TextField(supplyTarget,4,GUILayout.MinHeight(UiRowHeight(32)));
            GUILayout.Label("Exportreserve · im Quellhafen zurückbehalten",smallStyle);supplyReserve=GUILayout.TextField(supplyReserve,4,GUILayout.MinHeight(UiRowHeight(32)));
            WorkspaceHeading("PRIORITÄT");supplyPriority=DrawWorkspaceTabs(supplyPriority,new[]{"Niedrig","Normal","Hoch"});
            int minimum,target,reserve;
            if(WorkspaceButton("Versorgungsregel übernehmen",true))
            {
                if(!int.TryParse(supplyMinimum,out minimum) || !int.TryParse(supplyTarget,out target) || !int.TryParse(supplyReserve,out reserve) || minimum<0 || target<minimum || reserve<0)supplyFormError="Ganze Zahlen eingeben. Der Zielbestand muss mindestens dem Mindestbestand entsprechen.";
                else{supplyFormError=null;RequestSpaceOrder(CommandKind.SetSupplyPolicy,target:port.Id,planet:port.PlanetId,value:r,amount:target,minimum:minimum,reserve:reserve,priority:supplyPriority);}
            }
            if(supplyFormError!=null)GUILayout.Label(supplyFormError,bodyStyle);
        }
        private void DrawTransportEditor(BuildingState port,int r)
        {
            WorkspaceHeading("FRACHTER AUSWÄHLEN");bool any=false;
            foreach(var ship in simulation.State.Ships) if(ship.OwnerId==PlayerFactionId && ship.Kind==ShipKind.Freighter && !ship.Destroyed && !ship.Consumed)
            {
                any=true;if(WorkspaceButton("Frachter #"+ship.Id,selectedShips.Contains(ship.Id))) { selectedShips.Clear(); selectedShips.Add(ship.Id); }
                if(selectedShips.Contains(ship.Id))GUILayout.Label(ship.Status+"\nAn Bord: "+GoodsText(ship.Cargo),smallStyle);
            }
            if(!any){GUILayout.Label("Noch kein Frachter vorhanden. Einen Frachter im Schiffbau beauftragen.",mutedStyle);if(WorkspaceButton("Zum Schiffbau"))SetSpaceWorkspace(0,2);return;}
            WorkspaceHeading("ZIELKOLONIE");
            foreach(var p in simulation.State.Planets) if(p.OwnerId==PlayerFactionId && WorkspaceButton(p.Name,spaceTarget==p.Id)) spaceTarget=p.Id;
            GUILayout.Label("Menge je Fahrt · 1 bis 40",smallStyle); freightAmount=GUILayout.TextField(freightAmount,3,GUILayout.MinHeight(UiRowHeight(32)));
            int amount;bool amountValid=int.TryParse(freightAmount,out amount) && amount>0 && amount<=40;
            var carriers=selectedShips.FindAll(id=>simulation.State.Ships.Exists(s=>s.Id==id && s.OwnerId==PlayerFactionId && s.Kind==ShipKind.Freighter && !s.Destroyed && !s.Consumed));
            if(carriers.Count==0)GUILayout.Label("Einen Frachter aus der Liste auswählen.",smallStyle);
            if(!amountValid)GUILayout.Label("Für eine Lieferung eine ganze Zahl zwischen 1 und 40 eingeben.",smallStyle);
            bool enabled=GUI.enabled;GUI.enabled=enabled && carriers.Count>0 && amountValid && simulation.State.Planets.Exists(p=>p.Id==spaceTarget && p.OwnerId==PlayerFactionId);
            if(WorkspaceButton("Einmalige Lieferung",true)) foreach(long id in carriers) RequestSpaceOrder(CommandKind.AssignFreight,id,port.Id,spaceTarget,r,amount);
            if(WorkspaceButton("Feste Route einrichten")) foreach(long id in carriers) RequestSpaceOrder(CommandKind.SetTradeRoute,id,port.Id,spaceTarget,r,amount);
            GUI.enabled=enabled && carriers.Count>0;
            if(WorkspaceButton("Automatischem Netz zuordnen")) foreach(long id in carriers) RequestSpaceOrder(CommandKind.SetAutoSupply,id,flag:true);
            if(WorkspaceButton("Auftrag / Route abbrechen")) foreach(long id in carriers) RequestSpaceOrder(CommandKind.CancelFreight,id);
            GUI.enabled=enabled;
            GUILayout.Label("Geladene Ware bleibt bei Abbruch an Bord und wird zu einem eigenen Hafen mit freiem Platz zurückgebracht.",smallStyle);
        }
    }
}
