using System;
using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private readonly Dictionary<long, Transform> unitViews = new Dictionary<long, Transform>();
        private readonly List<long> selectedUnits = new List<long>();
        private readonly Dictionary<int, List<long>> controlGroups = new Dictionary<int, List<long>>();
        private bool showingMilitary;
        private CommandKind? militaryTargetMode;
        private GameSimulation priorSession;
        private Vector2? selectionStart;
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
        public bool RequestRecruitment(long buildingId, UnitKind kind)
        { return SubmitIntent(new SimCommand { PlanetId = openPlanetId, TargetId = buildingId, Kind = CommandKind.RecruitUnit, IntValue = (int)kind }); }

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
            UnitState nearest = null; float best = 24;
            foreach (var u in p.Units)
            {
                if (u.Destroyed || !simulation.CanSeeEntity(p, PlayerFactionId, u.Id) || !IsSurfacePointVisible(u.X,u.Y,0.3f)) continue;
                var screen = sceneCamera.WorldToScreenPoint(SurfacePosition(u.X, 0.3f, u.Y));
                float distance = Vector2.Distance(Input.mousePosition, screen);
                if (distance < best) { nearest = u; best = distance; }
            }
            if (Input.GetMouseButtonDown(0) && militaryTargetMode.HasValue && selectedUnits.Count > 0)
            {
                long target = nearest == null ? 0 : nearest.Id;
                if (target == 0) { var b = BuildingAt(p, hoverX, hoverY); if (b != null) target = b.Id; }
                foreach (long id in selectedUnits) RequestUnitOrder(id, militaryTargetMode.Value, hoverX, hoverY, target);
                militaryTargetMode = null; return true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (nearest != null && nearest.OwnerId == PlayerFactionId)
                { SelectUnit(nearest.Id, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)); return true; }
            }
            return false;
        }

        private bool RequestSurfaceOrderAtScreen(Vector2 point)
        {
            if(openPlanetId==0 || selectedUnits.Count==0 || !IsWorldPoint(point))return false;
            var p=simulation.FindPlanet(openPlanetId);var cell=SurfaceCellAtScreen(point);
            if(cell.X<0 || cell.Y<0 || cell.X>=p.Width || cell.Y>=p.Height)return false;
            UnitState nearest=null;float best=22;
            foreach(var unit in p.Units) {
                if(unit.Destroyed || !simulation.CanSeeEntity(p,PlayerFactionId,unit.Id) || !IsSurfacePointVisible(unit.X,unit.Y,.3f))continue;
                Vector2 screen=sceneCamera.WorldToScreenPoint(SurfacePosition(unit.X,.3f,unit.Y));
                float distance=Vector2.Distance(point,screen);if(distance<best){best=distance;nearest=unit;}
            }
            bool accepted=false;
            foreach(long id in selectedUnits)accepted=RequestUnitOrder(id,nearest!=null && nearest.OwnerId!=PlayerFactionId?CommandKind.AttackTarget:CommandKind.MoveUnit,cell.X,cell.Y,nearest==null?0:nearest.Id) || accepted;
            return accepted;
        }

        private void HandleSelectionEvents()
        {
            if (openPlanetId == 0 || placement.HasValue || placingRoad || removingRoad || planningSupply || militaryTargetMode.HasValue) return;
            var e = Event.current;
            if (e.button != 0) return;
            // Read queued mouse-event positions instead of the frame's final pointer position.
            // Very short drags can deliver down and up within the same rendered frame.
            Vector2 point = new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y);
            if (e.type == EventType.MouseDown && IsWorldPoint(point)) selectionStart = point;
            if (e.type == EventType.MouseUp && selectionStart.HasValue)
            {
                Vector2 start = selectionStart.Value, end = point; selectionStart = null;
                if(!IsWorldPoint(end))return;
                if (Vector2.Distance(start, end) > 12)
                {
                    if (!e.shift) selectedUnits.Clear();
                    var p = simulation.FindPlanet(openPlanetId);
                    foreach (var u in p.Units)
                    {
                        if(!IsSurfacePointVisible(u.X,u.Y,0.3f))continue;
                        var screen = sceneCamera.WorldToScreenPoint(SurfacePosition(u.X, 0.3f, u.Y));
                        if (screen.x >= Math.Min(start.x, end.x) && screen.x <= Math.Max(start.x, end.x) &&
                            screen.y >= Math.Min(start.y, end.y) && screen.y <= Math.Max(start.y, end.y)) SelectUnit(u.Id, true);
                    }
                    e.Use();
                }
            }
        }

        private void UpdateMilitaryViews(PlanetState p)
        {
            foreach(var view in unitViews.Values) if(view!=null) view.gameObject.SetActive(false);
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
                t.gameObject.SetActive(visible);
                if (visible) UpdateGroundPose(t,u);
            }
            selectedUnits.RemoveAll(id => !p.Units.Exists(u => u.Id == id && !u.Destroyed));
        }

        private void DrawMilitaryLabels(float scale)
        {
            var p = simulation.FindPlanet(openPlanetId);
            var labelRects=new List<Rect>();
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
                string marker = selectedUnits.Contains(u.Id) ? "▶ " : "";
                string label=marker+Definitions.Get(u.Kind).Name;Vector2 size=smallStyle.CalcSize(new GUIContent(label));
                var labelRect=new Rect(x-size.x*.5f,y-size.y-6,size.x+4,size.y+4);
                while(labelRects.Exists(r=>r.Overlaps(labelRect))) labelRect.y-=labelRect.height+2;
                labelRects.Add(labelRect);
                GUI.Label(labelRect,label,smallStyle);
                Fill(new Rect(x - 18, y - 3, 36, 4), Border);
                Fill(new Rect(x - 18, y - 3, 36 * (float)(u.Health / Definitions.Get(u.Kind).Health), 4), DiplomaticColor(u.OwnerId));
                if (selectedUnits.Contains(u.Id) && u.Order != UnitOrder.Hold)
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
            if (selectionStart.HasValue && Input.GetMouseButton(0))
            {
                var a = selectionStart.Value; Vector2 b = Input.mousePosition;
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
                if(militaryHelpOpen)GUILayout.Label("Klick wählt aus; Umschalt ergänzt. Ziehen erzeugt einen Auswahlrahmen. Rechtsklick bewegt oder greift an; Strg + Rechtsklick öffnet weitere Befehle. H hält die Stellung. Strg+1–3 speichert eine Gruppe, 1–3 wählt sie aus.",smallStyle);
                return;
            }
            var u=p.Units.Find(v=>selectedUnits.Contains(v.Id) && !v.Destroyed && v.OwnerId==PlayerFactionId);
            if(u==null){GUILayout.Label("Zuerst eigene Einheiten auswählen.",mutedStyle);if(WorkspaceButton("Einheiten auswählen"))militaryTask=0;return;}
            GUILayout.Label(selectedUnits.Count+" ausgewählt · "+Definitions.Get(u.Kind).Name,headingStyle);
            GUILayout.Label(u.Status,bodyStyle);
            GUILayout.Label("Munition "+u.Ammunition+" · Rationen "+u.Food+" · Bauteile "+u.Parts+"\nReichweite "+Definitions.Get(u.Kind).Range+" · Sicht "+Definitions.Get(u.Kind).Sight+(simulation.HasCover(p,u)?" · Deckung":""),smallStyle);
            if(WorkspaceButton("Objektbefehle öffnen …",true))OpenSelectionCommandMenu(Input.mousePosition);
            if(WorkspaceButton("Stellung halten"))foreach(long id in selectedUnits)RequestUnitOrder(id,CommandKind.HoldPosition);
            TargetButton("Rückzugsziel wählen",CommandKind.RetreatUnit);
            TargetButton("Angriffsziel wählen",CommandKind.AttackTarget);
            if(p.Units.Exists(v=>selectedUnits.Contains(v.Id) && v.Kind==UnitKind.Artillery))TargetButton("Artillerieziel wählen",CommandKind.AttackPosition);
            TargetButton("Nachschubquelle wählen",CommandKind.ResupplyUnit);
            if(p.Units.Exists(v=>selectedUnits.Contains(v.Id) && v.Kind==UnitKind.Engineer))TargetButton("Reparaturziel wählen",CommandKind.RepairTarget);
            if(militaryTargetMode.HasValue)GUILayout.Label("Jetzt das Ziel auf der Karte anklicken. Escape bricht ab.",bodyStyle);
        }

        private void TargetButton(string label, CommandKind kind)
        { if (WorkspaceButton(label,militaryTargetMode==kind)) militaryTargetMode = kind; }

        private void DrawRecruitment(BuildingState b)
        {
            if (Definitions.IsTower(b.Kind)) GUILayout.Label("Munition: " + b.Ammunition + " / " + Definitions.TowerAmmoCapacity +
                "\nÖrtlicher Energiepuffer: " + b.EnergyStored.ToString("0.0") + " / " + Definitions.Get(b.Kind).EnergyStorageCapacity +
                "\nReichweite: " + Definitions.GetTower(b.Kind).Range, smallStyle);
            if (!b.Built || b.Deconstructing || (b.Kind != BuildingKind.Barracks && b.Kind != BuildingKind.VehicleFactory)) return;
            GUILayout.Label(Definitions.Get(b.Kind).Name + " · " + b.Status, smallStyle);
            if (b.RecruitKind >= 0) { GUILayout.Label("Auftrag: " + Definitions.Get((UnitKind)b.RecruitKind).Name, smallStyle); return; }
            foreach (UnitKind kind in Enum.GetValues(typeof(UnitKind)))
            {
                if (Definitions.Producer(kind) != b.Kind) continue;
                var d = Definitions.Get(kind);
                if (WorkspaceButton(d.Name + " · " + d.Metal + " Metall / " + d.Parts + " Bauteile")) RequestRecruitment(b.Id, kind);
                GUILayout.Label(d.Food + " Nahrung · " + d.Crew + " Bewohner · " + d.Seconds + " s", smallStyle);
            }
        }
    }
}
