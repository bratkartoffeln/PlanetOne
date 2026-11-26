using System;
using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        // These are presentation snapshots, never a second command queue or game state.
        private enum CommandSubject { None, Star, Planet, Building, Units, Ships, Space, Ground, Asteroid, ForeignShip, ForeignUnit }
        private sealed class CommandContext
        {
            public GameSimulation Simulation;
            public CommandSubject Subject;
            public int World, X, Y;
            public long Id;
            public string Title;
            public long[] Actors;
        }
        private sealed class ContextAction
        {
            public string Id, Label, Hint, Disabled;
            public HudIcon Icon;
            public Action Run;
            public bool Danger;
        }
        private CommandContext commandContext;
        private Rect commandMenuRect;
        private Vector2 commandMenuAnchor, commandMenuScroll;
        private string commandPage;
        private int commandKeyboardIndex, commandDismissFrame = -1;
        private bool keepCommandMenu;
        private int fleetTask;
        public bool IsCommandMenuOpen { get { return commandContext != null; } }
        public Rect CommandMenuScreenRect { get { return new Rect(commandMenuRect.x*UiScale,Screen.height-commandMenuRect.yMax*UiScale,commandMenuRect.width*UiScale,commandMenuRect.height*UiScale); } }
        public string[] CommandMenuLabels { get { return MenuActions().ConvertAll(a=>a.Label).ToArray(); } }
        public void CloseCommandMenu() { commandContext=null;commandPage=null;commandMenuScroll=Vector2.zero;commandKeyboardIndex=0;selectionStart=null;shipSelectionStart=null; }
        private CommandContext NewContext(CommandSubject subject,long id,string title)
        { return new CommandContext {Simulation=simulation,Subject=subject,Id=id,World=openPlanetId,Title=title,Actors=(openPlanetId==0?selectedShips:selectedUnits).ToArray()}; }
        private CommandContext SelectionCommands()
        {
            if(openPlanetId==0)
            {
                if(selectedShips.Count>0)return NewContext(CommandSubject.Ships,selectedShips[0],selectedShips.Count==1?ShipTitle(selectedShips[0]):selectedShips.Count+" Schiffe");
                if(selectedAsteroidId!=0)return NewContext(CommandSubject.Asteroid,selectedAsteroidId,"Asteroid #"+selectedAsteroidId);
                if(StarSelected)return NewContext(CommandSubject.Star,0,"Zentralstern");
                var p=simulation.FindPlanet(selectedPlanetId);
                if(p!=null && simulation.IsPlanetKnown(PlayerFactionId,p.Id))return NewContext(CommandSubject.Planet,p.Id,p.Name);
            }
            else
            {
                var p=simulation.FindPlanet(openPlanetId);var b=FindSelected(p);
                if(b!=null && !b.Destroyed && simulation.CanSeeEntity(p,PlayerFactionId,b.Id))return NewContext(CommandSubject.Building,b.Id,Definitions.Get(b.Kind).Name+" #"+b.Id);
                if(selectedUnits.Count>0){var unit=p.Units.Find(u=>u.Id==selectedUnits[0]);return NewContext(CommandSubject.Units,selectedUnits[0],selectedUnits.Count==1 && unit!=null?Definitions.Get(unit.Kind).Name+" #"+unit.Id:selectedUnits.Count+" Bodeneinheiten");}
            }
            return NewContext(CommandSubject.None,0,openPlanetId==0?"Sonnensystem":simulation.FindPlanet(openPlanetId).Name);
        }
        private string ShipTitle(long id)
        { var s=simulation.State.Ships.Find(v=>v.Id==id);return s==null?"Schiff":Definitions.Ships[(int)s.Kind].Name+" #"+id; }
        public bool OpenSelectionCommandMenu(Vector2 point)
        { if(settingsOpen || menuOpen)return false;return ShowCommandMenu(SelectionCommands(),point); }
        private bool ShowCommandMenu(CommandContext context,Vector2 point)
        {
            CloseCommandMenu();commandContext=context;
            commandMenuAnchor=new Vector2(point.x/UiScale,(Screen.height-point.y)/UiScale);
            CancelSpacePan();selectionStart=null;shipSelectionStart=null;
            PositionCommandMenu(MenuActions().Count);return true;
        }
        public bool OpenContextMenuAtScreen(Vector2 point)
        {
            if(!IsWorldPoint(point))return false;
            CommandContext c;
            if(openPlanetId==0)
            {
                ShipState hit=null;float best=22;
                foreach(var s in simulation.State.Ships)
                {
                    if(!simulation.IsShipVisible(PlayerFactionId,s))continue;
                    Vector3 screen=sceneCamera.WorldToScreenPoint(SpacePosition(s.X,s.Y,ShipDisplayHeight));float d=Vector2.Distance(screen,point);
                    if(screen.z>0 && d<best){hit=s;best=d;}
                }
                if(hit!=null)
                {
                    if(hit.OwnerId==PlayerFactionId){if(!selectedShips.Contains(hit.Id))SelectShip(hit.Id);c=SelectionCommands();}
                    else c=NewContext(CommandSubject.ForeignShip,hit.Id,ShipTitle(hit.Id));
                    return ShowCommandMenu(c,point);
                }
                int planet=0;best=float.MaxValue;
                foreach(var p in simulation.State.Planets)
                {
                    if(!simulation.IsPlanetKnown(PlayerFactionId,p.Id))continue;
                    Vector3 screen=sceneCamera.WorldToScreenPoint(planetViews[p.Id].position);float d=Vector2.Distance(screen,point);
                    if(screen.z>0 && d<Mathf.Max(24,PlanetScreenRadius(p.Id)+8) && d<best){planet=p.Id;best=d;}
                }
                if(planet!=0)
                {
                    c=NewContext(CommandSubject.Planet,planet,simulation.FindPlanet(planet).Name);
                    if(selectedShips.Count==0)SelectPlanet(planet);
                    return ShowCommandMenu(c,point);
                }
                foreach(var a in simulation.State.Asteroids)
                {
                    Vector3 screen=sceneCamera.WorldToScreenPoint(asteroidViews[a.Id].position);
                    if(screen.z>0 && Vector2.Distance(screen,point)<Mathf.Max(12,AsteroidScreenRadius(a.Id)+5))
                        return ShowCommandMenu(NewContext(CommandSubject.Asteroid,a.Id,"Asteroid #"+a.Id),point);
                }
                if(SelectStarAtScreen(point))return ShowCommandMenu(SelectionCommands(),point);
                c=NewContext(CommandSubject.Space,0,"Flugziel");
                float distance;var ray=sceneCamera.ScreenPointToRay(point);
                if(!new Plane(Vector3.up,Vector3.up*ShipDisplayHeight).Raycast(ray,out distance))return false;
                var position=ray.GetPoint(distance);c.X=Mathf.RoundToInt(position.x/SpaceDistanceScale);c.Y=Mathf.RoundToInt(position.z/SpaceDistanceScale);
            }
            else
            {
                var p=simulation.FindPlanet(openPlanetId);var cell=SurfaceCellAtScreen(point);
                if(cell.X<0 || cell.Y<0 || cell.X>=p.Width || cell.Y>=p.Height)return false;
                UnitState hit=null;float best=24;
                foreach(var u in p.Units)
                {
                    if(u.Destroyed || !simulation.CanSeeEntity(p,PlayerFactionId,u.Id) || !IsSurfacePointVisible(u.X,u.Y,.3f))continue;
                    float d=Vector2.Distance(sceneCamera.WorldToScreenPoint(SurfacePosition(u.X,.3f,u.Y)),point);
                    if(d<best){hit=u;best=d;}
                }
                if(hit!=null)
                {
                    if(hit.OwnerId==PlayerFactionId){if(!selectedUnits.Contains(hit.Id))SelectUnit(hit.Id);c=SelectionCommands();}
                    else c=NewContext(CommandSubject.ForeignUnit,hit.Id,Definitions.Get(hit.Kind).Name);
                }
                else
                {
                    BuildingState b=null;RaycastHit rayHit;long picked;
                    if(Physics.Raycast(sceneCamera.ScreenPointToRay(point),out rayHit,300) && buildingPickIds.TryGetValue(rayHit.collider.GetInstanceID(),out picked))b=p.Buildings.Find(v=>v.Id==picked);
                    if(b==null)b=BuildingAt(p,cell.X,cell.Y);
                    if(b!=null && !b.Destroyed && simulation.CanSeeEntity(p,PlayerFactionId,b.Id))
                    {
                        if(GameSimulation.BuildingOwner(p,b)==PlayerFactionId && selectedUnits.Count==0)SelectBuilding(b.Id);
                        c=NewContext(CommandSubject.Building,b.Id,Definitions.Get(b.Kind).Name+" #"+b.Id);
                    }
                    else c=NewContext(CommandSubject.Ground,0,"Gelände · "+cell.X+" / "+cell.Y);
                }
                c.X=cell.X;c.Y=cell.Y;
            }
            return ShowCommandMenu(c,point);
        }
        private bool ContextValid(CommandContext c)
        {
            if(c==null || c.Simulation!=simulation || c.World!=openPlanetId)return false;
            if(c.Subject==CommandSubject.Planet)return simulation.IsPlanetKnown(PlayerFactionId,(int)c.Id);
            if(c.Subject==CommandSubject.Asteroid)return simulation.State.Asteroids.Exists(a=>a.Id==c.Id);
            if(c.Subject==CommandSubject.Building){var p=simulation.FindPlanet(c.World);return p!=null && p.Buildings.Exists(b=>b.Id==c.Id && !b.Destroyed && simulation.CanSeeEntity(p,PlayerFactionId,b.Id));}
            if(c.Subject==CommandSubject.ForeignShip)return simulation.State.Ships.Exists(s=>s.Id==c.Id && simulation.IsShipVisible(PlayerFactionId,s));
            if(c.Subject==CommandSubject.ForeignUnit){var p=simulation.FindPlanet(c.World);return p!=null && p.Units.Exists(u=>u.Id==c.Id && !u.Destroyed && simulation.CanSeeEntity(p,PlayerFactionId,u.Id));}
            return true;
        }
        private long[] LiveActors(CommandContext c)
        {
            var result=new List<long>();foreach(long id in c.Actors)
            {
                if(c.World==0){if(simulation.State.Ships.Exists(s=>s.Id==id && s.OwnerId==PlayerFactionId && !s.Destroyed && !s.Consumed))result.Add(id);}
                else {var p=simulation.FindPlanet(c.World);if(p!=null && p.Units.Exists(u=>u.Id==id && u.OwnerId==PlayerFactionId && !u.Destroyed))result.Add(id);}
            }
            return result.ToArray();
        }
        private void AddAction(List<ContextAction> list,string id,string label,HudIcon icon,Action run,string hint="",string disabled=null,bool danger=false)
        { list.Add(new ContextAction {Id=id,Label=label,Icon=icon,Run=run,Hint=hint,Disabled=disabled,Danger=danger}); }
        private List<ContextAction> MenuActions() { return BuildContextActions(commandContext,commandPage); }
        private void CommandSubpage(string page){commandPage=page;commandMenuScroll=Vector2.zero;commandKeyboardIndex=0;keepCommandMenu=true;}
        private void OpenObjectDetails(CommandContext c)
        {
            if(c.Subject==CommandSubject.Building)SelectBuilding(c.Id);
            else if(c.Subject==CommandSubject.Star){OpenStarDetails();return;}
            else if(c.Subject==CommandSubject.Ships){OpenPresentationSection(3);return;}
            else if(c.Subject==CommandSubject.Units){OpenPresentationSection(2);return;}
            panelOpen=true;contextOpen=true;sidebarScroll=Vector2.zero;
        }
        private void TargetUnits(CommandKind kind,long[] actors)
        {selectedUnits.Clear();selectedUnits.AddRange(actors);militaryTargetMode=kind;panelOpen=false;Notify("Ziel auf der Karte wählen · Esc: abbrechen");}
        private List<ContextAction> BuildContextActions(CommandContext c,string page=null)
        {
            var list=new List<ContextAction>();if(c==null)return list;
            if(!ContextValid(c)){AddAction(list,"unavailable","Objekt nicht mehr verfügbar",HudIcon.Info,()=>{},disabled:"Ansicht oder Sichtkontakt hat sich geändert");return list;}
            long[] actors=LiveActors(c);string noActors=actors.Length==0?"Zuerst eigene Einheiten auswählen":null;
            if(c.Subject==CommandSubject.Building)
            {
                var p=simulation.FindPlanet(c.World);var b=p.Buildings.Find(v=>v.Id==c.Id);bool own=GameSimulation.BuildingOwner(p,b)==PlayerFactionId;
                if(!own)
                {
                    bool hostile=simulation.AtWar(PlayerFactionId,GameSimulation.BuildingOwner(p,b));
                    AddAction(list,"attack","Gebäude angreifen",HudIcon.Units,()=>{foreach(long id in actors)RequestUnitOrder(id,CommandKind.AttackTarget,b.X,b.Y,b.Id);},"Ausgewählte Truppen",noActors??(!hostile?"Kein Krieg mit dieser Fraktion":null));
                    var infantry=Array.FindAll(actors,id=>p.Units.Exists(u=>u.Id==id && u.Kind==UnitKind.Infantry));
                    AddAction(list,"capture","Gebäude sichern",HudIcon.Diplomacy,()=>{foreach(long id in infantry)RequestInvasionOrder(CommandKind.CaptureBuilding,id,b.Id,p.Id);},"Mit Infanterie vor Ort",!hostile?"Kein Krieg mit dieser Fraktion":infantry.Length==0?"Infanterie auswählen":null);
                    return list;
                }
                if(page=="confirm")
                {
                    AddAction(list,"confirm",b.Built?"Rückbau bestätigen":"Bauabbruch bestätigen",HudIcon.Close,()=>RequestBuildingOperation(c.World,b.Id,b.Built?CommandKind.DemolishBuilding:CommandKind.CancelConstruction),"Rückgewinnung: "+GoodsText(GameSimulation.GetRecoveryPreview(b)),b.Deconstructing?"Rückbau läuft bereits":null,true);
                    AddAction(list,"back","Zurück",HudIcon.Back,()=>CommandSubpage(null),"Gebäude behalten");return list;
                }
                if(page=="priority")
                {
                    string[] labels={"Niedrig","Normal","Hoch"};for(int i=0;i<3;i++){int priority=i;AddAction(list,"priority-"+i,labels[i]+(b.Priority==i?" · aktiv":""),HudIcon.Supply,()=>RequestBuildingOperation(c.World,b.Id,CommandKind.SetBuildingPriority,priority),"Betrieb und Warenversorgung");}
                    AddAction(list,"back","Zurück",HudIcon.Back,()=>CommandSubpage(null));return list;
                }
                if(b.Built && !b.Deconstructing)
                {
                    var engineers=Array.FindAll(actors,id=>p.Units.Exists(u=>u.Id==id && u.Kind==UnitKind.Engineer));
                    if(engineers.Length>0)AddAction(list,"repair-here","Dieses Gebäude reparieren",HudIcon.Build,()=>{foreach(long id in engineers)RequestUnitOrder(id,CommandKind.RepairTarget,b.X,b.Y,b.Id);},"Mit ausgewählten Pionieren",b.Health>=Definitions.BuildingHealth(b.Kind)?"Gebäude ist unbeschädigt":null);
                    if(actors.Length>0 && b.Kind==BuildingKind.MilitaryDepot)AddAction(list,"resupply-here","Hier Nachschub holen",HudIcon.Supply,()=>{foreach(long id in actors)RequestUnitOrder(id,CommandKind.ResupplyUnit,b.X,b.Y,b.Id);},"Für ausgewählte Bodeneinheiten");
                    if(b.Kind==BuildingKind.Spaceport){AddAction(list,"production","Schiffe bauen",HudIcon.Fleet,()=>{chosenPort=b.Id;fleetTask=2;OpenPresentationSection(3);},"Bauaufträge an diesem Raumhafen");AddAction(list,"supply","Fracht und Versorgung",HudIcon.Supply,()=>{chosenPort=b.Id;OpenPresentationSection(5);});}
                    else if(b.Kind==BuildingKind.Laboratory)AddAction(list,"production","Forschung öffnen",HudIcon.Research,()=>OpenPresentationSection(4));
                    else if(b.Kind==BuildingKind.Barracks || b.Kind==BuildingKind.VehicleFactory)AddAction(list,"production","Truppen ausbilden",HudIcon.Units,()=>OpenObjectDetails(c),"Rekrutierung an diesem Gebäude");
                    else if(b.Kind==BuildingKind.JumpGate)AddAction(list,"project","Sprungtorprojekt",HudIcon.Diplomacy,()=>OpenPresentationSection(6),"Projekt, Mitglieder und Aktivierung");
                    if(b.Kind!=BuildingKind.Headquarters){AddAction(list,"toggle",b.Enabled?"Betrieb pausieren":"Betrieb fortsetzen",b.Enabled?HudIcon.Pause:HudIcon.Play,()=>RequestBuildingOperation(c.World,b.Id,CommandKind.SetBuildingEnabled,0,!b.Enabled));AddAction(list,"priority","Versorgungspriorität",HudIcon.Supply,()=>CommandSubpage("priority"),new[]{"Niedrig","Normal","Hoch"}[Mathf.Clamp(b.Priority,0,2)]);}
                }
                AddAction(list,"details","Details und Lager",HudIcon.Info,()=>OpenObjectDetails(c),"Zustand, Waren und Produktionsdaten");
                if(b.Kind!=BuildingKind.Headquarters && !b.Deconstructing)AddAction(list,"demolish",b.Built?"Gebäude zurückbauen …":"Bauauftrag abbrechen …",HudIcon.Close,()=>CommandSubpage("confirm"),"Rückgewinnung vor Ort · mit Bestätigung",danger:true);
                return list;
            }
            if(c.Subject==CommandSubject.Units)
            {
                AddAction(list,"move","Bewegen",HudIcon.Back,()=>TargetUnits(CommandKind.MoveUnit,actors),"Danach Ziel anklicken",noActors);
                AddAction(list,"hold","Position halten",HudIcon.Stop,()=>{foreach(long id in actors)RequestUnitOrder(id,CommandKind.HoldPosition);},"H · aktuelle Bewegung beenden",noActors);
                AddAction(list,"attack","Angreifen",HudIcon.Units,()=>TargetUnits(CommandKind.AttackTarget,actors),"Feindliche Einheit oder Gebäude wählen",noActors);
                AddAction(list,"retreat","Zurückziehen",HudIcon.Back,()=>TargetUnits(CommandKind.RetreatUnit,actors),"Sicheren Zielort wählen",noActors);
                var p=simulation.FindPlanet(c.World);
                if(Array.Exists(actors,id=>p.Units.Exists(u=>u.Id==id && u.Kind==UnitKind.Artillery)))AddAction(list,"bombard","Artilleriefeuer",HudIcon.Units,()=>TargetUnits(CommandKind.AttackPosition,Array.FindAll(actors,id=>p.Units.Exists(u=>u.Id==id && u.Kind==UnitKind.Artillery))),"Position wählen");
                if(Array.Exists(actors,id=>p.Units.Exists(u=>u.Id==id && u.Kind==UnitKind.Engineer)))AddAction(list,"repair","Reparieren",HudIcon.Build,()=>TargetUnits(CommandKind.RepairTarget,Array.FindAll(actors,id=>p.Units.Exists(u=>u.Id==id && u.Kind==UnitKind.Engineer))),"Eigenes beschädigtes Ziel wählen");
                AddAction(list,"resupply","Nachschub holen",HudIcon.Supply,()=>TargetUnits(CommandKind.ResupplyUnit,actors),"Depot oder Versorger wählen",noActors);
                AddAction(list,"details","Truppendetails",HudIcon.Info,()=>OpenObjectDetails(c));return list;
            }
            if(c.Subject==CommandSubject.Ships)
            {
                AddAction(list,"move","Flugziel wählen",HudIcon.Back,()=>{selectedShips.Clear();selectedShips.AddRange(actors);BeginShipMove();},"Ziel danach direkt anklicken",noActors);
                AddAction(list,"details","Flottenaufträge",HudIcon.Fleet,()=>{fleetTask=1;OpenPresentationSection(3);},"Zielwelt, Bewachung und Kolonisierung",noActors);
                if(Array.Exists(actors,id=>simulation.State.Ships.Exists(s=>s.Id==id && s.Kind==ShipKind.Freighter)))AddAction(list,"supply","Fracht verwalten",HudIcon.Supply,()=>OpenPresentationSection(5),"Routen, Waren und Versorgung");
                AddAction(list,"combat","Raumkampf / Landung",HudIcon.Units,()=>ShowSpacePanel(3),"Reparatur, Transport und Gefechte",noActors);
                AddAction(list,"focus","Auswahl zentrieren",HudIcon.Planet,()=>{var s=simulation.State.Ships.Find(v=>v.Id==actors[0]);SetSpaceFocus(SpacePosition(s.X,s.Y));},disabled:noActors);return list;
            }
            if(c.Subject==CommandSubject.Planet)
            {
                int planet=(int)c.Id;var p=simulation.FindPlanet(planet);
                if(actors.Length>0)
                {
                    AddAction(list,"fly","Nach "+p.Name+" fliegen",HudIcon.Back,()=>{foreach(long id in actors)RequestSpaceOrder(CommandKind.FlyShip,id,planet:planet);},actors.Length+" ausgewählte Schiffe");
                    AddAction(list,"guard","Orbit bewachen",HudIcon.Fleet,()=>{foreach(long id in actors)RequestSpaceOrder(CommandKind.GuardPlanet,id,planet:planet);},"Mit ausgewählter Flotte");
                    var colony=Array.FindAll(actors,id=>simulation.State.Ships.Exists(s=>s.Id==id && s.Kind==ShipKind.ColonyShip));
                    if(colony.Length>0)AddAction(list,"colonize","Kolonie gründen",HudIcon.Build,()=>RequestSpaceOrder(CommandKind.Colonize,colony[0],planet:planet),"Kolonieschiff im Zielorbit benötigt",KnownPlanetOwner(p)!=0?"Welt ist nicht als unbesiedelt bekannt":null);
                }
                AddAction(list,"open",PlanetEnvironment.Airborne(p)?"Atmosphäre öffnen":"Oberfläche öffnen",HudIcon.Planet,()=>SetView(planet));
                AddAction(list,"focus","Planet zentrieren",HudIcon.Planet,()=>FocusPlanet(planet));
                AddAction(list,"overview","Kolonieübersicht",HudIcon.Info,()=>{SetView(planet);OpenPresentationSection(0);},disabled:KnownPlanetOwner(p)!=PlayerFactionId?"Nur für eigene Kolonien":null);return list;
            }
            if(c.Subject==CommandSubject.Asteroid)
            {
                AddAction(list,"inspect","Brocken ansehen",HudIcon.Info,()=>InspectAsteroid(c.Id));
                AddAction(list,"approach","Zur Erkundung anfliegen",HudIcon.Back,()=>{foreach(long id in actors)RequestAsteroidApproach(id,c.Id);},"Bestand vor Ort aufklären",noActors);
                AddAction(list,"mining","Abbau planen",HudIcon.Supply,()=>SelectAsteroid(c.Id),"Frachter und Rücknahmehafen wählen");return list;
            }
            if(c.Subject==CommandSubject.ForeignShip || c.Subject==CommandSubject.ForeignUnit)
            {
                bool ship=c.Subject==CommandSubject.ForeignShip;int owner=ship?simulation.State.Ships.Find(s=>s.Id==c.Id).OwnerId:simulation.FindPlanet(c.World).Units.Find(u=>u.Id==c.Id).OwnerId;
                AddAction(list,"attack","Ziel angreifen",HudIcon.Units,()=>{foreach(long id in actors){if(ship)RequestInvasionOrder(CommandKind.AttackShip,id,c.Id);else RequestUnitOrder(id,CommandKind.AttackTarget,c.X,c.Y,c.Id);}},"Nur bei erklärtem Krieg",noActors??(!simulation.AtWar(PlayerFactionId,owner)?"Kein Krieg mit dieser Fraktion":null));
                AddAction(list,"diplomacy","Diplomatie öffnen",HudIcon.Diplomacy,()=>OpenPresentationSection(6));return list;
            }
            if(c.Subject==CommandSubject.Space && actors.Length>0)AddAction(list,"move-here","Hierhin fliegen",HudIcon.Back,()=>{foreach(long id in actors)SubmitIntent(new SimCommand {Kind=CommandKind.MoveShipPosition,ActorId=id,PlanetId=1,X=c.X,Y=c.Y});},actors.Length+" Schiffe · Ziel "+c.X+" / "+c.Y);
            if(c.Subject==CommandSubject.Ground && actors.Length>0)
            {
                AddAction(list,"move-here","Hierhin bewegen",HudIcon.Back,()=>{foreach(long id in actors)RequestUnitOrder(id,CommandKind.MoveUnit,c.X,c.Y);});
                AddAction(list,"retreat-here","Hierhin zurückziehen",HudIcon.Back,()=>{foreach(long id in actors)RequestUnitOrder(id,CommandKind.RetreatUnit,c.X,c.Y);});
                AddAction(list,"hold","Position halten",HudIcon.Stop,()=>{foreach(long id in actors)RequestUnitOrder(id,CommandKind.HoldPosition);});return list;
            }
            if(c.Subject==CommandSubject.Star){AddAction(list,"details","Sterninformationen",HudIcon.Info,OpenStarDetails);AddAction(list,"focus","Stern zentrieren",HudIcon.Planet,()=>SetSpaceFocus(Vector3.zero));return list;}
            if(openPlanetId!=0 && simulation.FindPlanet(openPlanetId).OwnerId==PlayerFactionId)AddAction(list,"build","Gebäude bauen",HudIcon.Build,()=>OpenPresentationSection(1),"Katalog öffnen und Bauplatz wählen");
            AddAction(list,"overview",openPlanetId==0?"Weltenübersicht":"Kolonieübersicht",HudIcon.Planet,()=>OpenPresentationSection(0));
            AddAction(list,"manage",openPlanetId==0?"Flotte verwalten":"Bodentruppen",openPlanetId==0?HudIcon.Fleet:HudIcon.Units,()=>OpenPresentationSection(openPlanetId==0?3:2));
            AddAction(list,"supply","Versorgung",HudIcon.Supply,()=>OpenPresentationSection(5));return list;
        }
        public bool InvokeContextCommand(string id)
        {
            if(settingsOpen || menuOpen || !IsCommandMenuOpen)return false;
            var action=MenuActions().Find(a=>a.Id==id);if(action==null || action.Disabled!=null)return false;
            keepCommandMenu=false;action.Run();if(!keepCommandMenu)CloseCommandMenu();return true;
        }
        private float CommandRowHeight {get{return FontUnitSize(18)+FontUnitSize(15)*3+24;}}
        private float CommandHeaderHeight {get{return FontUnitSize(20)*2+24;}}
        private void PositionCommandMenu(int count)
        {
            float width=Screen.width/UiScale,height=Screen.height/UiScale,w=Mathf.Min(440+(TextScale-1)*150,width-16),h=Mathf.Min(CommandHeaderHeight+40+count*CommandRowHeight,height-TopHeight-20);
            commandMenuRect=new Rect(Mathf.Clamp(commandMenuAnchor.x+8,8,width-w-8),Mathf.Clamp(commandMenuAnchor.y+8,TopHeight+8,height-h-8),w,h);
        }
        private bool HandleCommandMenuPointer()
        {
            if(!IsCommandMenuOpen)return commandDismissFrame==Time.frameCount;
            if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))CommandPointerDown(Input.mousePosition);
            return true;
        }
        public bool CommandPointerDown(Vector2 point)
        {
            if(!IsCommandMenuOpen)return false;
            if(!CommandMenuScreenRect.Contains(point)){CloseCommandMenu();commandDismissFrame=Time.frameCount;}
            return true;
        }
        private void DrawCommandMenu()
        {
            if(!IsCommandMenuOpen)return;
            var actions=MenuActions();PositionCommandMenu(actions.Count);
            var e=Event.current;
            if(e.type==EventType.KeyDown)
            {
                if(e.keyCode==KeyCode.UpArrow || e.keyCode==KeyCode.DownArrow)
                {commandKeyboardIndex=(commandKeyboardIndex+(e.keyCode==KeyCode.UpArrow?-1:1)+actions.Count)%actions.Count;commandMenuScroll.y=Mathf.Max(0,commandKeyboardIndex*CommandRowHeight-(commandMenuRect.height-160));e.Use();}
                else if(e.keyCode==KeyCode.Return || e.keyCode==KeyCode.KeypadEnter){InvokeContextCommand(actions[Mathf.Clamp(commandKeyboardIndex,0,actions.Count-1)].Id);e.Use();return;}
            }
            Fill(new Rect(commandMenuRect.x+4,commandMenuRect.y+5,commandMenuRect.width,commandMenuRect.height),new Color(0,0,0,.5f));
            Fill(commandMenuRect,new Color(.025f,.05f,.065f,.99f));DrawPanel(commandMenuRect);
            Fill(new Rect(commandMenuRect.x,commandMenuRect.y,3,commandMenuRect.height),Cyan);
            GUI.Label(new Rect(commandMenuRect.x+16,commandMenuRect.y+10,commandMenuRect.width-60,CommandHeaderHeight-16),commandContext.Title,headingStyle);
            if(HudButton(new Rect(commandMenuRect.xMax-39,commandMenuRect.y+7,30,30),"",HudIcon.Close)){CloseCommandMenu();return;}
            Rect area=new Rect(commandMenuRect.x+10,commandMenuRect.y+CommandHeaderHeight,commandMenuRect.width-20,commandMenuRect.height-CommandHeaderHeight-34);
            commandMenuScroll=GUI.BeginScrollView(area,commandMenuScroll,new Rect(0,0,area.width-18,actions.Count*CommandRowHeight));
            string invoke=null;
            for(int i=0;i<actions.Count;i++)
            {
                var a=actions[i];Rect row=new Rect(0,i*CommandRowHeight,area.width-24,CommandRowHeight-6);bool enabled=GUI.enabled;GUI.enabled=enabled && a.Disabled==null;
                if(GUI.Button(row,GUIContent.none,i==commandKeyboardIndex?selectedButtonStyle:buttonStyle))invoke=a.Id;
                GUI.enabled=enabled;Color previous=GUI.color;GUI.color=a.Disabled!=null?Muted:a.Danger?Danger:Cyan;GUI.DrawTexture(new Rect(10,row.y+10,22,22),HudSymbol(a.Icon));GUI.color=previous;
                GUI.Label(new Rect(42,row.y+6,row.width-54,FontUnitSize(18)+8),a.Label,a.Disabled!=null?mutedStyle:bodyStyle);
                GUI.Label(new Rect(42,row.y+FontUnitSize(18)+14,row.width-54,FontUnitSize(15)*3+6),a.Disabled??a.Hint,smallStyle);
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(commandMenuRect.x+16,commandMenuRect.yMax-FontUnitSize(15)-10,commandMenuRect.width-32,FontUnitSize(15)+6),"Esc: schließen   ·   ↑ ↓ / Enter: auswählen",smallStyle);
            if(invoke!=null)InvokeContextCommand(invoke);
        }
    }
}
