using System;
using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private readonly Dictionary<long, Transform> unitViews = new Dictionary<long, Transform>();
        private readonly HashSet<long> removedUnitViews=new HashSet<long>();
        private readonly List<long> selectedUnits = new List<long>();
        private readonly Dictionary<int, List<long>> controlGroups = new Dictionary<int, List<long>>();
        private bool showingMilitary;
        private CommandKind? militaryTargetMode;
        private GameSimulation priorSession;
        private Vector2? selectionStart;
        private Vector2 selectionCurrent;
        private int militaryTask;
        private bool militaryHelpOpen;
        public int MilitaryWorkspace {get{return militaryTask;}}
        public void SetMilitaryWorkspace(int task){OpenPresentationSection(2);militaryTask=Mathf.Clamp(task,0,3);}
        public int RenderedUnitCount { get { int count = 0; foreach (var t in unitViews.Values) if (t.gameObject.activeSelf) count++; return count; } }
        public int SelectedUnitCount { get { return selectedUnits.Count; } }

        public void StartMilitaryFixture()
        {
            if (!simulation.State.MilitaryTestFixture && !simulation.State.SpaceTestFixture) priorSession = simulation;
            simulation = MilitaryFixture.Create(); foodRates.Clear(); controlGroups.Clear();
            BuildSpace(); SetView(1); showingMilitary = true;
            Notify(simulation.State.LastMessage);
        }

        public bool SelectUnit(long id, bool additive = false)
        {
            if (openPlanetId == 0) return false;
            var p = simulation.FindPlanet(openPlanetId);
            var u = p.Units.Find(v => v.Id == id && !v.Destroyed && v.OwnerId == PlayerFactionId);
            if (u == null) return false;
            CloseCommandMenu();
            if (!additive) selectedUnits.Clear();
            if (!selectedUnits.Contains(id)) selectedUnits.Add(id);
            contextOpen=true;panelOpen=false;showingDiplomacy=false;showingSpacePanel=false;presentationSection=2;
            selectedBuildingId = 0; showingMilitary = true; CancelPlacement(); return true;
        }

        public bool RequestUnitOrder(long unitId, CommandKind kind, int x = 0, int y = 0, long target = 0)
        { return SubmitIntent(new SimCommand { PlanetId = openPlanetId, ActorId = unitId, Kind = kind, X = x, Y = y, TargetId = target }); }
        public bool RequestRecruitment(long buildingId, UnitKind kind, bool cancel=false)
        { return SubmitIntent(new SimCommand { PlanetId = openPlanetId, TargetId = buildingId, Kind = cancel?CommandKind.CancelRecruitUnit:CommandKind.RecruitUnit, IntValue = (int)kind }); }

        private void HandleMilitaryKeyboard()
        {
            if (openPlanetId == 0) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { militaryTargetMode = null; selectedUnits.Clear(); }
            if (Input.GetKeyDown(KeyCode.H)) foreach (long id in selectedUnits) RequestUnitOrder(id, CommandKind.HoldPosition);
            for (int i = 1; i <= 3; i++)
            {
                if (!Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i))) continue;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) controlGroups[i] = new List<long>(selectedUnits);
                else
                {
                    List<long> group;
                    if (controlGroups.TryGetValue(i, out group)) { selectedUnits.Clear(); foreach (long id in group) SelectUnit(id, true); }
                }
            }
        }

        private bool HandleMilitaryPointer(PlanetState p, bool inside)
        {
            if (!inside) return false;
            if (Input.GetMouseButtonDown(0) && militaryTargetMode.HasValue && selectedUnits.Count > 0)
            {
                long target = PointerTargetAtScreen(Input.mousePosition).Id;
                if (target == 0) { var b = BuildingAt(p, hoverX, hoverY); if (b != null) target = b.Id; }
                foreach (long id in selectedUnits) RequestUnitOrder(id, militaryTargetMode.Value, hoverX, hoverY, target);
                militaryTargetMode = null; return true;
            }
            return false;
        }

        private void HandleSelectionEvents()
        {
            if (openPlanetId == 0 || placement.HasValue || placingRoad || removingRoad || planningSupply || militaryTargetMode.HasValue) return;
            var e = Event.current;
            if (e.button != 0) return;
            // Read queued mouse-event positions instead of the frame's final pointer position.
            // Very short drags can deliver down and up within the same rendered frame.
            Vector2 point = new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y);
            if (e.type == EventType.MouseDown && IsWorldPoint(point)) BeginSurfaceSelection(point);
            if(e.type==EventType.MouseDrag&&selectionStart.HasValue){ContinueSurfaceSelection(point);e.Use();}
            if (e.type == EventType.MouseUp && selectionStart.HasValue)
            {
                EndSurfaceSelection(point,e.shift);e.Use();
            }
        }
        internal bool BeginSurfaceSelection(Vector2 point)
        {if(openPlanetId==0||!IsWorldPoint(point))return false;selectionStart=point;selectionCurrent=point;return true;}
        internal void ContinueSurfaceSelection(Vector2 point){if(selectionStart.HasValue)selectionCurrent=point;}
        internal bool EndSurfaceSelection(Vector2 point,bool additive=false)
        {
            if(!selectionStart.HasValue)return false;var start=selectionStart.Value;selectionStart=null;
            if(!IsWorldPoint(point))return false;var p=simulation.FindPlanet(openPlanetId);
            bool drag=Vector2.Distance(start,point)>12;
            if(!additive)selectedUnits.Clear();
            if(!drag)
            {
                var hit=PointerTargetAtScreen(point);
                if(hit.Id!=0 && !hit.Building && hit.Owner==PlayerFactionId)return SelectUnit(hit.Id,additive);
                return additive || SelectBuilding(hit.Building?hit.Id:0);
            }
            foreach(var u in p.Units)
            {
                if(u.Destroyed||u.OwnerId!=PlayerFactionId||!IsSurfacePointVisible(u.X,u.Y,.3f))continue;
                Vector2 screen=UnitSelectionScreenRect(u.Id,false).center;
                bool hit=screen.x>=Math.Min(start.x,point.x)&&screen.x<=Math.Max(start.x,point.x)&&screen.y>=Math.Min(start.y,point.y)&&screen.y<=Math.Max(start.y,point.y);
                if(hit)SelectUnit(u.Id,true);
            }
            if(selectedUnits.Count==0&&!additive)SelectBuilding(0);
            return true;
        }

        private void UpdateMilitaryViews(PlanetState p)
        {
            removedUnitViews.Clear();
            foreach(long id in unitViews.Keys)removedUnitViews.Add(id);
            foreach (var u in p.Units)
            {
                Transform t;
                bool visible = !u.Destroyed && simulation.CanSeeEntity(p, PlayerFactionId, u.Id);
                if (!unitViews.TryGetValue(u.Id, out t))
                {
                    if (!visible) continue;
                    t = new GameObject(Definitions.Get(u.Kind).Name).transform; t.SetParent(surfaceRoot, false);
                    Color color = DiplomaticColor(u.OwnerId);
                    BuildGroundUnitModel(t,u.Kind,color);
                    unitViews[u.Id] = t;
                }
                removedUnitViews.Remove(u.Id);
                if(t.gameObject.activeSelf!=visible)t.gameObject.SetActive(visible);
                if (visible)
                {
                    int stage=Definitions.DamageStage(u.Health,Definitions.Get(u.Kind).Health);
                    var stride=t.GetComponent<InfantryStride>();
                    if(stride!=null)stride.Injury=stage;
                    UpdateGroundPose(t,u);
                    UpdateWear(t,stage,stride,false);
                    if(!AtmosphereSurface && Definitions.Producer(u.Kind)==BuildingKind.VehicleFactory)StampTracks(t);
                }
                else if(t.TryGetComponent<FootprintSample>(out var previous))previous.Ready=false;
            }
            foreach(long id in removedUnitViews){Destroy(unitViews[id].gameObject);unitViews.Remove(id);}
            selectedUnits.RemoveAll(id => !p.Units.Exists(u => u.Id == id && !u.Destroyed));
        }

        private void DrawMilitaryLabels(float scale)
        {
            var p = simulation.FindPlanet(openPlanetId);
            DrawServiceMarks(p,scale);
            var labelRects=showTerrainLabels?new List<Rect>():null;
            if (simulation.State.MilitaryTestFixture)
            {
                string banner="M3 PRÜFKARTE · endliche Bestände · keine Fraktions-KI";
                float x=WorldViewport.x/scale+18,width=Mathf.Min(730,WorldViewport.width/scale-36);
                float h=smallStyle.CalcHeight(new GUIContent(banner),width-20)+14;
                var bounds=new Rect(x,TopHeight+12,width,h);DrawPanel(bounds);
                GUI.Label(new Rect(bounds.x+10,bounds.y+7,bounds.width-20,bounds.height-14),banner,smallStyle);
            }
            foreach (var u in p.Units)
            {
                if (u.Destroyed || !simulation.CanSeeEntity(p, PlayerFactionId, u.Id) || !IsSurfacePointVisible(u.X,u.Y,0.6f)) continue;
                var screen = sceneCamera.WorldToScreenPoint(SurfacePosition(u.X, 0.6f, u.Y));
                if (!sceneCamera.pixelRect.Contains(screen)) continue;
                float x = screen.x / scale, y = (Screen.height - screen.y) / scale;
                if(showTerrainLabels)
                {
                    string marker = selectedUnits.Contains(u.Id) ? "▶ " : "";
                    string label=marker+Definitions.Get(u.Kind).Name;Vector2 size=smallStyle.CalcSize(new GUIContent(label));
                    var labelRect=new Rect(x-size.x*.5f,y-size.y-6,size.x+4,size.y+4);
                    while(labelRects.Exists(r=>r.Overlaps(labelRect))) labelRect.y-=labelRect.height+2;
                    labelRects.Add(labelRect);
                    GUI.Label(labelRect,label,smallStyle);
                }
                Fill(new Rect(x - 18, y - 3, 36, 4), Border);
                Fill(new Rect(x - 18, y - 3, 36 * (float)(u.Health / Definitions.Get(u.Kind).Health), 4), DiplomaticColor(u.OwnerId));
                if (showTerrainLabels && selectedUnits.Contains(u.Id) && u.Order != UnitOrder.Hold)
                {
                    var goal = sceneCamera.WorldToScreenPoint(SurfacePosition(u.GoalX, 0.15f, u.GoalY));
                    GUI.Label(new Rect(goal.x / scale - 8, (Screen.height - goal.y) / scale - 12, 120,UiRowHeight(30)), "× Ziel", bodyStyle);
                }
            }
            foreach (var s in p.Sightings)
                if (s.FactionId == PlayerFactionId && !simulation.IsCellVisible(p, PlayerFactionId, s.X, s.Y))
                {
                    var point = sceneCamera.WorldToScreenPoint(SurfacePosition(s.X, 0.2f, s.Y));
                    if (IsWorldPoint(point)) GUI.Label(new Rect(point.x / scale - 38, (Screen.height - point.y) / scale, 240,UiRowHeight(30)), "? Letzte Sichtung", smallStyle);
                }
            if (selectionStart.HasValue)
            {
                var a = selectionStart.Value; Vector2 b = selectionCurrent;
                var rect = new Rect(Math.Min(a.x, b.x) / scale, (Screen.height - Math.Max(a.y, b.y)) / scale,
                    Math.Abs(a.x - b.x) / scale, Math.Abs(a.y - b.y) / scale);
                Fill(rect, new Color(0.3f, 0.8f, 0.8f, 0.15f));
            }
        }

        private void DrawMilitaryPanel(PlanetState p)
        {
            int next=DrawWorkspaceTabs(militaryTask,new[]{"Einheiten","Befehle","Landung","Rekrutierung"});
            if(next!=militaryTask){militaryTask=next;sidebarScroll=Vector2.zero;}
            WorkspaceHeading("BODENTRUPPEN · "+p.Name);
            if (simulation.State.MilitaryTestFixture)
            {
                GUILayout.Label("PRÜFKARTE · vorbereitete, endliche Armeen", smallStyle);
                if (WorkspaceButton("Angriff Nord auslösen")) MilitaryFixture.OrderAttack(simulation, true);
                if (WorkspaceButton("Angriff Süd auslösen")) MilitaryFixture.OrderAttack(simulation, false);
                if (priorSession != null && WorkspaceButton("Zur laufenden Kolonie zurück"))
                { RestorePreviousSession(); return; }
            }
            if(militaryTask==2){DrawGroundInvasionPanel(p);return;}
            if(militaryTask==3)
            {
                WorkspaceHeading("REKRUTIERUNG","Eine Kaserne bildet Infanterie aus; eine Fahrzeugfabrik baut Fahrzeuge.");
                bool producer=false;
                foreach(var b in p.Buildings)
                    if(b.Built && !b.Destroyed && !b.Deconstructing && GameSimulation.BuildingOwner(p,b)==PlayerFactionId && (b.Kind==BuildingKind.Barracks || b.Kind==BuildingKind.VehicleFactory))
                    {producer=true;DrawRecruitment(b);GUILayout.Space(12);}
                if(!producer)GUILayout.Label("Noch keine eigene betriebsbereite Ausbildungsstätte auf dieser Welt.",mutedStyle);
                return;
            }
            if(militaryTask==0)
            {
                bool any=false;
                foreach(var unit in p.Units)
                {
                    if(unit.Destroyed || unit.OwnerId!=PlayerFactionId)continue;any=true;
                    if(WorkspaceButton(Definitions.Get(unit.Kind).Name+" #"+unit.Id+" · "+unit.Health.ToString("0")+" Struktur",selectedUnits.Contains(unit.Id)))
                    {SelectUnit(unit.Id,Event.current.shift);panelOpen=true;militaryTask=1;}
                }
                if(!any){GUILayout.Label("Keine eigenen Bodentruppen auf dieser Welt.",mutedStyle);if(WorkspaceButton("Rekrutierung öffnen"))militaryTask=3;}
                GUILayout.Space(10);if(WorkspaceButton(militaryHelpOpen?"Bedienhinweise schließen":"Auswahl und Gruppen …"))militaryHelpOpen=!militaryHelpOpen;
                if(militaryHelpOpen)GUILayout.Label("Klick wählt aus; Umschalt ergänzt. Ziehen erzeugt einen Auswahlrahmen. Rechtsklick bewegt oder greift an; Strg + Rechtsklick öffnet weitere Befehle. F3 schaltet Typ- und Ladehinweise um. H hält die Stellung. Strg+1–3 speichert eine Gruppe, 1–3 wählt sie aus.",smallStyle);
                return;
            }
            var u=p.Units.Find(v=>selectedUnits.Contains(v.Id) && !v.Destroyed && v.OwnerId==PlayerFactionId);
            if(u==null){GUILayout.Label("Zuerst eigene Einheiten auswählen.",mutedStyle);if(WorkspaceButton("Einheiten auswählen"))militaryTask=0;return;}
            GUILayout.Label(selectedUnits.Count+" ausgewählt · "+Definitions.Get(u.Kind).Name,headingStyle);
            GUILayout.Label(u.Status,bodyStyle);
            if(u.Kind==UnitKind.MobileWorkshop)GUILayout.Label("Repariert im Stand eigene Fahrzeuge im Umkreis von zwei Feldern. Ersatzteile am Militärdepot nachladen.",smallStyle);
            GUILayout.Label("Munition "+u.Ammunition+" · Rationen "+u.Food+" · Bauteile "+u.Parts+"\nReichweite "+Definitions.Get(u.Kind).Range+" · Sicht "+Definitions.Get(u.Kind).Sight+(simulation.HasCover(p,u)?" · Deckung":""),smallStyle);
            if(WorkspaceButton("Objektbefehle öffnen …",true))OpenSelectionCommandMenu(Input.mousePosition);
            if(WorkspaceButton("Stellung halten"))foreach(long id in selectedUnits)RequestUnitOrder(id,CommandKind.HoldPosition);
            TargetButton("Rückzugsziel wählen",CommandKind.RetreatUnit);
            TargetButton("Angriffsziel wählen",CommandKind.AttackTarget);
            if(p.Units.Exists(v=>selectedUnits.Contains(v.Id) && v.Kind==UnitKind.Artillery))TargetButton("Artillerieziel wählen",CommandKind.AttackPosition);
            TargetButton("Nachschubquelle wählen",CommandKind.ResupplyUnit);
            if(p.Units.Exists(v=>selectedUnits.Contains(v.Id) && Definitions.CanRepair(v.Kind)))TargetButton("Reparaturziel wählen",CommandKind.RepairTarget);
            if(militaryTargetMode.HasValue)GUILayout.Label("Jetzt das Ziel auf der Karte anklicken. Escape bricht ab.",bodyStyle);
        }

        private void TargetButton(string label, CommandKind kind)
        { if (WorkspaceButton(label,militaryTargetMode==kind)) militaryTargetMode = kind; }

        private void DrawMaterialNeeds(BuildingState building, int[] cost)
        {
            var missing = new int[cost.Length];
            var planned = new int[cost.Length];
            for (int r=0; r<cost.Length; r++)
            {
                int local = Math.Max(0,building.Inventory[r]-building.ReservedOutgoing[r]);
                int needed = Math.Max(0,cost[r]-local);
                planned[r] = Math.Min(needed,building.ReservedIncoming[r]);
                missing[r] = needed-planned[r];
            }
            if (Array.Exists(missing,n=>n>0))
                GUILayout.Label("Fehlt: "+GoodsText(missing),headingStyle);
            if (Array.Exists(planned,n=>n>0))
                GUILayout.Label("Lieferung eingeplant: "+GoodsText(planned),smallStyle);
        }

        private void DrawRecruitment(BuildingState b)
        {
            if (Definitions.IsTower(b.Kind)) GUILayout.Label("Munition: " + b.Ammunition + " / " + Definitions.TowerAmmoCapacity +
                "\nÖrtlicher Energiepuffer: " + b.EnergyStored.ToString("0.0") + " / " + Definitions.Get(b.Kind).EnergyStorageCapacity +
                "\nReichweite: " + Definitions.GetTower(b.Kind).Range, smallStyle);
            if (!b.Built || b.Deconstructing || (b.Kind != BuildingKind.Barracks && b.Kind != BuildingKind.VehicleFactory)) return;
            GUILayout.Label(Definitions.Get(b.Kind).Name.ToUpperInvariant(),headingStyle);
            int used=GameSimulation.RecruitmentCount(simulation.FindPlanet(b.PlanetId),b.Kind,PlayerFactionId);
            bool full=used>=Definitions.PlanetRecruitmentLimit;
            GUILayout.Label((b.Kind==BuildingKind.Barracks?"Fußtruppen":"Fahrzeuge")+" · "+used+" / "+Definitions.PlanetRecruitmentLimit+" Aufträge auf diesem Planeten"+(full?" · voll":""),smallStyle);
            int columns=ProductionColumns, index=0;
            foreach(UnitKind kind in Enum.GetValues(typeof(UnitKind)))
            {
                if(Definitions.Producer(kind)!=b.Kind)continue;
                if(index%columns==0)GUILayout.BeginHorizontal();
                var def=Definitions.Get(kind);
                bool active=b.RecruitKind==(int)kind;
                int waiting=b.RecruitQueue==null?0:b.RecruitQueue.FindAll(v=>v==kind).Count;
                var missing=LocalShortfall(b,def.Cost,active && b.RecruitPaid);
                float progress=active?Mathf.Clamp01((float)(b.RecruitProgress/def.Seconds)):0;
                bool stopped=active && (!b.Operational || !b.Enabled || Array.Exists(missing,n=>n>0) ||
                    (!b.RecruitPaid && simulation.FindPlanet(b.PlanetId).Population-def.Crew<4) || progress>=1);
                var tile=ProductionRect(columns);
                string detail=def.Crew+" Bewohner · "+def.Seconds+" s"+(active?"\n"+b.Status:"");
                detail+="\nLinksklick: +1 · Rechtsklick: −1";
                if(productionPointerInContent && Event.current.type==EventType.MouseDown && Event.current.button==1 && tile.Contains(Event.current.mousePosition))
                {
                    RequestRecruitment(b.Id,kind,true);
                    Event.current.Use();
                }
                if(ProductionTile(tile,def.Name,UnitPortrait(kind),def.Cost,missing,detail,false,waiting+(active?1:0),
                    active?(stopped?"Wartet":Mathf.FloorToInt(progress*100)+" %"):"Wartet",progress,stopped,false))
                    RequestRecruitment(b.Id,kind);
                if(++index%columns==0)GUILayout.EndHorizontal();
            }
            if(index%columns!=0)GUILayout.EndHorizontal();
            if(b.RecruitKind>=0)
            {
                GUILayout.Label("In Produktion · "+Definitions.Get((UnitKind)b.RecruitKind).Name,headingStyle);
                GUILayout.Label(b.Status,smallStyle);
                if(b.RecruitQueue!=null && b.RecruitQueue.Count>0)
                {
                    var names=new List<string>();
                    foreach(var kind in b.RecruitQueue)names.Add(Definitions.Get(kind).Name);
                    GUILayout.Label("Danach · "+string.Join(" → ",names.ToArray()),smallStyle);
                }
                if(GUILayout.Button(b.Enabled?"Produktion anhalten":"Produktion fortsetzen",buttonStyle))
                    RequestBuildingOperation(b.PlanetId,b.Id,CommandKind.SetBuildingEnabled,0,!b.Enabled);
            }
        }
    }
}
