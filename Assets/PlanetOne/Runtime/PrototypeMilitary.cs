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
            if (!additive) selectedUnits.Clear();
            if (!selectedUnits.Contains(id)) selectedUnits.Add(id);
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
                if (u.Destroyed || !simulation.CanSeeEntity(p, PlayerFactionId, u.Id)) continue;
                var screen = sceneCamera.WorldToScreenPoint(new Vector3((float)u.X, 0.3f, (float)u.Y));
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
            if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
            {
                foreach (long id in selectedUnits)
                    RequestUnitOrder(id, nearest != null && nearest.OwnerId != PlayerFactionId ? CommandKind.AttackTarget : CommandKind.MoveUnit,
                        hoverX, hoverY, nearest == null ? 0 : nearest.Id);
                return true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (nearest != null && nearest.OwnerId == PlayerFactionId)
                { SelectUnit(nearest.Id, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)); return true; }
            }
            return false;
        }

        private void HandleSelectionEvents()
        {
            if (openPlanetId == 0 || placement.HasValue || placingRoad || removingRoad || militaryTargetMode.HasValue) return;
            var e = Event.current;
            if (e.button != 0) return;
            // Read queued mouse-event positions instead of the frame's final pointer position.
            // Very short drags can deliver down and up within the same rendered frame.
            Vector2 point = new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y);
            if (e.type == EventType.MouseDown && sceneCamera.pixelRect.Contains(point)) selectionStart = point;
            if (e.type == EventType.MouseUp && selectionStart.HasValue)
            {
                Vector2 start = selectionStart.Value, end = point; selectionStart = null;
                if (Vector2.Distance(start, end) > 12)
                {
                    if (!e.shift) selectedUnits.Clear();
                    var p = simulation.FindPlanet(openPlanetId);
                    foreach (var u in p.Units)
                    {
                        var screen = sceneCamera.WorldToScreenPoint(new Vector3((float)u.X, 0.3f, (float)u.Y));
                        if (screen.x >= Math.Min(start.x, end.x) && screen.x <= Math.Max(start.x, end.x) &&
                            screen.y >= Math.Min(start.y, end.y) && screen.y <= Math.Max(start.y, end.y)) SelectUnit(u.Id, true);
                    }
                    e.Use();
                }
            }
        }

        private void UpdateMilitaryViews(PlanetState p)
        {
            foreach (var u in p.Units)
            {
                Transform t;
                bool visible = !u.Destroyed && simulation.CanSeeEntity(p, PlayerFactionId, u.Id);
                if (!unitViews.TryGetValue(u.Id, out t))
                {
                    if (!visible) continue;
                    t = new GameObject(Definitions.Get(u.Kind).Name).transform; t.SetParent(surfaceRoot, false);
                    Color color = u.OwnerId == PlayerFactionId ? Cyan : Danger;
                    bool infantry = u.Kind == UnitKind.Infantry || u.Kind == UnitKind.AntiTank;
                    MakePrimitive(infantry ? PrimitiveType.Capsule : PrimitiveType.Cube, "Verband", t,
                        new Vector3(0, 0.23f, 0), infantry ? new Vector3(0.25f, 0.25f, 0.25f) : new Vector3(0.55f, 0.28f, 0.4f), color);
                    if (!infantry) MakePrimitive(PrimitiveType.Cube, "Rollenmarkierung", t, new Vector3(0.15f, 0.45f, 0),
                        new Vector3(u.Kind == UnitKind.Artillery ? 0.75f : 0.4f, 0.09f, 0.1f), u.Kind == UnitKind.Engineer ? Amber : Ink);
                    unitViews[u.Id] = t;
                }
                t.gameObject.SetActive(visible);
                if (visible) t.position = new Vector3((float)u.X, 0.1f, (float)u.Y);
            }
            selectedUnits.RemoveAll(id => !p.Units.Exists(u => u.Id == id && !u.Destroyed));
        }

        private void DrawMilitaryLabels(float scale)
        {
            var p = simulation.FindPlanet(openPlanetId);
            if (simulation.State.MilitaryTestFixture)
                GUI.Label(new Rect(20, TopHeight + 12, 730, 28), "M3 TESTFIXTURE · endliche Bestände · keine Fraktions-KI", headingStyle);
            foreach (var u in p.Units)
            {
                if (u.Destroyed || !simulation.CanSeeEntity(p, PlayerFactionId, u.Id)) continue;
                var screen = sceneCamera.WorldToScreenPoint(new Vector3((float)u.X, 0.6f, (float)u.Y));
                if (!sceneCamera.pixelRect.Contains(screen)) continue;
                float x = screen.x / scale, y = (Screen.height - screen.y) / scale;
                string marker = selectedUnits.Contains(u.Id) ? "▶ " : "";
                GUI.Label(new Rect(x - 48, y - 25, 155, 24), marker + Definitions.Get(u.Kind).Name, smallStyle);
                Fill(new Rect(x - 18, y - 3, 36, 4), Border);
                Fill(new Rect(x - 18, y - 3, 36 * (float)(u.Health / Definitions.Get(u.Kind).Health), 4), u.OwnerId == PlayerFactionId ? Cyan : Danger);
                if (selectedUnits.Contains(u.Id) && u.Order != UnitOrder.Hold)
                {
                    var goal = sceneCamera.WorldToScreenPoint(new Vector3(u.GoalX, 0.15f, u.GoalY));
                    GUI.Label(new Rect(goal.x / scale - 8, (Screen.height - goal.y) / scale - 12, 80, 24), "× Ziel", bodyStyle);
                }
            }
            foreach (var s in p.Sightings)
                if (s.FactionId == PlayerFactionId && !simulation.IsCellVisible(p, PlayerFactionId, s.X, s.Y))
                {
                    var point = sceneCamera.WorldToScreenPoint(new Vector3(s.X, 0.2f, s.Y));
                    if (sceneCamera.pixelRect.Contains(point)) GUI.Label(new Rect(point.x / scale - 38, (Screen.height - point.y) / scale, 140, 25), "? Letzte Sichtung", smallStyle);
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
            GUILayout.Space(10); GUILayout.Label("BODENTRUPPEN", headingStyle);
            GUILayout.Label("Klick / Umschalt: auswählen · Ziehen: Rahmen\nRechtsklick: Bewegen / Angriff · H: Halten\nStrg+1–3: Gruppe speichern · 1–3: auswählen", smallStyle);
            if (simulation.State.MilitaryTestFixture)
            {
                GUILayout.Label("TESTFIXTURE: vorbereitete, endliche Armeen", smallStyle);
                if (GUILayout.Button("Angriff Nord auslösen", buttonStyle, GUILayout.Height(28))) MilitaryFixture.OrderAttack(simulation, true);
                if (GUILayout.Button("Angriff Süd auslösen", buttonStyle, GUILayout.Height(28))) MilitaryFixture.OrderAttack(simulation, false);
                if (priorSession != null && GUILayout.Button("Zur laufenden Kolonie zurück", buttonStyle, GUILayout.Height(28)))
                { simulation = priorSession; priorSession = null; foodRates.Clear(); BuildSpace(); SetView(0); return; }
            }
            GUILayout.Space(8);
            foreach (var u in p.Units)
            {
                if (u.Destroyed || u.OwnerId != PlayerFactionId) continue;
                if (GUILayout.Button(Definitions.Get(u.Kind).Name + " · " + u.Health.ToString("0"), selectedUnits.Contains(u.Id) ? selectedButtonStyle : buttonStyle, GUILayout.Height(28))) SelectUnit(u.Id);
            }
            GUILayout.Label(selectedUnits.Count + " ausgewählt", headingStyle);
            if (selectedUnits.Count > 0)
            {
                var u = p.Units.Find(v => v.Id == selectedUnits[0]);
                GUILayout.Label(u.Status + "\nMunition " + u.Ammunition + " · Rationen " + u.Food + " · Bauteile " + u.Parts +
                    "\nReichweite " + Definitions.Get(u.Kind).Range + " · Sicht " + Definitions.Get(u.Kind).Sight +
                    (simulation.HasCover(p, u) ? " · Deckung" : ""), smallStyle);
                if (GUILayout.Button("Stellung halten", buttonStyle, GUILayout.Height(28))) foreach (long id in selectedUnits) RequestUnitOrder(id, CommandKind.HoldPosition);
                TargetButton("Rückzug: Ziel wählen", CommandKind.RetreatUnit);
                TargetButton("Angriff: Einheit / Gebäude wählen", CommandKind.AttackTarget);
                TargetButton("Artillerie: Position wählen", CommandKind.AttackPosition);
                TargetButton("Nachschub: Depot / Einheit wählen", CommandKind.ResupplyUnit);
                TargetButton("Reparatur: eigenes Ziel wählen", CommandKind.RepairTarget);
                if (militaryTargetMode.HasValue) GUILayout.Label("Jetzt das Ziel auf der Karte anklicken.", bodyStyle);
            }
            GUILayout.Space(10); GUILayout.Label("REKRUTIERUNG", headingStyle);
            foreach (var b in p.Buildings)
                if (b.Built && !b.Deconstructing && (b.Kind == BuildingKind.Barracks || b.Kind == BuildingKind.VehicleFactory)) DrawRecruitment(b);
        }

        private void TargetButton(string label, CommandKind kind)
        { if (GUILayout.Button(label, militaryTargetMode == kind ? selectedButtonStyle : buttonStyle, GUILayout.Height(28))) militaryTargetMode = kind; }

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
                if (GUILayout.Button(d.Name + " · " + d.Metal + " M / " + d.Parts + " B", buttonStyle, GUILayout.Height(28))) RequestRecruitment(b.Id, kind);
                GUILayout.Label(d.Food + " Nahrung · " + d.Crew + " Bewohner · " + d.Seconds + " s", smallStyle);
            }
        }
    }
}
