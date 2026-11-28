using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private string landingX="4", landingY="9";
        private int invasionTask;
        public int InvasionWorkspace {get{return invasionTask;}}
        private readonly Dictionary<long,Transform> invasionViews=new Dictionary<long,Transform>();
        private readonly Dictionary<int,LineRenderer> dangerRings=new Dictionary<int,LineRenderer>();
        private readonly Dictionary<long,LineRenderer> shieldRings=new Dictionary<long,LineRenderer>();
        private readonly Dictionary<long,Transform> landingFootprints=new Dictionary<long,Transform>();
        public void StartInvasionFixture()
        {
            if(!simulation.State.InvasionTestFixture && !simulation.State.MilitaryTestFixture && !simulation.State.SpaceTestFixture) priorSession=simulation;
            simulation=InvasionFixture.Create();foodRates.Clear();selectedShips.Clear();shipGroups.Clear();
            BuildSpace();SetView(0);showingSpacePanel=true;spacePanelTab=3;spaceTarget=2;Notify(simulation.State.LastMessage);
        }
        public bool RequestInvasionOrder(CommandKind kind,long actor,long target=0,int planet=2,int x=0,int y=0,int value=0,int amount=0)
        { return SubmitIntent(new SimCommand {Kind=kind,ActorId=actor,TargetId=target,PlanetId=planet,X=x,Y=y,IntValue=value,Amount=amount}); }
        private void DrawInvasionPanel()
        {
            int next=DrawWorkspaceTabs(invasionTask,new[]{"Manöver","Raumkampf","Landung"});
            if(next!=invasionTask){invasionTask=next;sidebarScroll=Vector2.zero;}
            if(simulation.State.InvasionTestFixture)
            {
                GUILayout.Label("M5 PRÜFKARTE · endliche Testbestände",smallStyle);
                if(priorSession!=null && GUILayout.Button("Zur laufenden Kolonie zurück",buttonStyle)) {RestorePreviousSession();return;}
            }
            WorkspaceHeading("SCHIFF AUSWÄHLEN");
            DrawInvasionNotice();
            foreach(var s in simulation.State.Ships)
                if(s.OwnerId==PlayerFactionId && !s.Destroyed && !s.Consumed && WorkspaceButton(Definitions.Ships[(int)s.Kind].Name+" #"+s.Id,selectedShips.Contains(s.Id)))
                { SelectShip(s.Id);spacePanelTab=3;panelOpen=true; }
            ShipState chosen=selectedShips.Count==0?null:simulation.State.Ships.Find(s=>s.Id==selectedShips[0] && s.OwnerId==PlayerFactionId && !s.Destroyed && !s.Consumed);
            if(chosen==null) {GUILayout.Label("Wähle ein eigenes Schiff, um passende Manöver, Kampf- oder Landungsaufträge zu öffnen.",smallStyle);return;}
            GUILayout.Label("Hülle "+chosen.Health.ToString("0")+" · Munition "+chosen.Ammunition+"\n"+chosen.Status,bodyStyle);
            WorkspaceHeading("ZIELWELT");
            foreach(var p in simulation.State.Planets) if(simulation.IsPlanetKnown(PlayerFactionId,p.Id) && WorkspaceButton(p.Name,spaceTarget==p.Id)) spaceTarget=p.Id;
            bool targetKnown=simulation.IsPlanetKnown(PlayerFactionId,spaceTarget);
            bool enabled=GUI.enabled;GUI.enabled=enabled && targetKnown;
            if(WorkspaceButton("Zieloberfläche ansehen")){SetView(spaceTarget);showingSpacePanel=false;showingMilitary=true;militaryTask=2;}
            GUI.enabled=enabled;
            if(invasionTask==0)
            {
                WorkspaceHeading("FLUG UND RÜCKZUG");GUI.enabled=enabled && targetKnown;
                if(WorkspaceButton("Anflug / Sammlung am Ziel",true))RequestSpaceOrder(CommandKind.FlyShip,chosen.Id,planet:spaceTarget);
                GUI.enabled=enabled;
                if(WorkspaceButton("Rückzug zur Heimatwelt"))RequestInvasionOrder(CommandKind.RetreatShip,chosen.Id,planet:1);
                GUILayout.Label("Ringe markieren den nahen Orbitalbereich. Ferne Gegner verhindern keine Landung.",smallStyle);return;
            }
            if(invasionTask==1)
            {
                WorkspaceHeading("SICHTBARE GEGNER");bool enemyFound=false;
                foreach(var enemy in simulation.State.Ships)if(simulation.IsShipVisible(PlayerFactionId,enemy) && simulation.AtWar(PlayerFactionId,enemy.OwnerId))
                {enemyFound=true;if(WorkspaceButton("Angriff: "+Definitions.Ships[(int)enemy.Kind].Name+" #"+enemy.Id))RequestInvasionOrder(CommandKind.AttackShip,chosen.Id,enemy.Id);}
                if(!enemyFound)GUILayout.Label("Kein aufgeklärtes feindliches Schiff in Sicht.",smallStyle);
                WorkspaceHeading("REPARATUR");bool yardFound=false;
                foreach(var yard in simulation.State.Ships)if(!yard.Destroyed && simulation.HasAccess(PlayerFactionId,yard.OwnerId,yard.TargetPlanetId,AccessRight.Repair) && simulation.IsShipVisible(PlayerFactionId,yard) && yard.Kind==ShipKind.OrbitalYard && yard.Id!=chosen.Id)
                {yardFound=true;if(WorkspaceButton("Reparatur an Werft #"+yard.Id))RequestInvasionOrder(CommandKind.RepairShip,chosen.Id,yard.Id);}
                if(!yardFound)GUILayout.Label("Keine sichtbare Werft mit Reparaturzugang.",smallStyle);
                if(chosen.Kind==ShipKind.OrbitalYard)
                {
                    WorkspaceHeading("KRIEGSSCHIFFBAU","Werftbestand: "+GoodsText(chosen.Cargo));
                    for(int k=(int)ShipKind.Scout;k<Definitions.Ships.Length;k++)if(WorkspaceButton(Definitions.Ships[k].Name+" bauen"))RequestInvasionOrder(CommandKind.BuildWarship,chosen.Id,value:k);
                }
                if(chosen.Kind==ShipKind.TorpedoShip)
                {int x,y;bool valid=DrawLandingCoordinates(out x,out y);GUI.enabled=enabled && targetKnown && valid;if(WorkspaceButton("Orbitalfeuer vorbereiten"))RequestInvasionOrder(CommandKind.OrbitalBombard,chosen.Id,planet:spaceTarget,x:x,y:y);GUI.enabled=enabled;}
                return;
            }
            if(chosen.Kind!=ShipKind.LandingTransport){GUILayout.Label("Für Truppentransport und Landung einen Landungstransporter auswählen.",smallStyle);return;}
            string phase=chosen.TransportPhase==TransportPhase.Space?"Im Raum":chosen.TransportPhase==TransportPhase.Descending?"Abstieg":chosen.TransportPhase==TransportPhase.Ascending?"Aufstieg":"Gelandet";
            WorkspaceHeading("LANDUNG UND EVAKUIERUNG",phase+" · "+chosen.Troops.Count+" / 4 Truppenplätze");GUILayout.Label("An Bord: "+GoodsText(chosen.Cargo),smallStyle);
            int landingCellX,landingCellY;bool validLanding=DrawLandingCoordinates(out landingCellX,out landingCellY);
            GUI.enabled=enabled && targetKnown && validLanding;
            if(WorkspaceButton("Abstieg an diesen Koordinaten",true))RequestInvasionOrder(CommandKind.LandTransport,chosen.Id,planet:spaceTarget,x:landingCellX,y:landingCellY);
            GUI.enabled=enabled;
            if(WorkspaceButton("Aufstieg / Evakuierung"))RequestInvasionOrder(CommandKind.LaunchTransport,chosen.Id,planet:chosen.TargetPlanetId);
            WorkspaceHeading("TRUPPEN ENTLADEN");
            foreach(var troop in chosen.Troops)if(!troop.Destroyed && WorkspaceButton(Definitions.Get(troop.Kind).Name+" #"+troop.Id+" entladen"))RequestInvasionOrder(CommandKind.UnloadTroop,chosen.Id,troop.Id,chosen.TargetPlanetId);
            if(chosen.Troops.Count==0)GUILayout.Label("Keine Truppen an Bord. Eigene Bodentruppen auf einer Oberfläche auswählen und über Landung verladen.",smallStyle);
            WorkspaceHeading("VORRÄTE LADEN");
            foreach(var p in simulation.State.Planets)foreach(var b in p.Buildings)if(b.Kind==BuildingKind.Spaceport && !b.Destroyed && GameSimulation.BuildingOwner(p,b)==PlayerFactionId)
            {
                GUILayout.Label(p.Name+" · Hafen #"+b.Id,smallStyle);
                if(WorkspaceButton("8 Nahrung laden"))RequestInvasionOrder(CommandKind.LoadShipSupplies,chosen.Id,b.Id,p.Id,value:0,amount:8);
                if(WorkspaceButton("8 Bauteile laden"))RequestInvasionOrder(CommandKind.LoadShipSupplies,chosen.Id,b.Id,p.Id,value:3,amount:8);
            }
            GUILayout.Label("Flak bedroht Transporter während Auf- und Abstieg.",smallStyle);
        }
        private bool DrawLandingCoordinates(out int x,out int y)
        {
            WorkspaceHeading("ZIEL AUF DER OBERFLÄCHE","Wabenkoordinaten des Landepunkts oder Orbitalziels.");
            GUILayout.Label("X-Koordinate",smallStyle);landingX=GUILayout.TextField(landingX,4,GUILayout.MinHeight(UiRowHeight(32)));
            GUILayout.Label("Y-Koordinate",smallStyle);landingY=GUILayout.TextField(landingY,4,GUILayout.MinHeight(UiRowHeight(32)));
            bool validX=int.TryParse(landingX,out x),validY=int.TryParse(landingY,out y);bool valid=validX && validY && x>=0 && y>=0;
            if(!valid)GUILayout.Label("Gültige, nichtnegative Wabenkoordinaten eingeben.",smallStyle);
            return valid;
        }
        private void DrawGroundInvasionPanel(PlanetState p)
        {
            GUILayout.Label("LANDUNG UND SICHERUNG",headingStyle);
            DrawInvasionNotice();
            if(GUILayout.Button("Flotten- und Landungsbefehle",buttonStyle)) {showingSpacePanel=true;spacePanelTab=3;spaceTarget=p.Id;sidebarScroll=Vector2.zero;}
            foreach(var s in simulation.State.Ships) if(!s.Destroyed && s.OwnerId==PlayerFactionId && s.Kind==ShipKind.LandingTransport)
                if(GUILayout.Button("Auswahl in Träger #"+s.Id+" verladen",buttonStyle)) foreach(long id in selectedUnits) RequestInvasionOrder(CommandKind.LoadTroop,s.Id,id,p.Id);
            if(selectedUnits.Count==0) return;
            var u=p.Units.Find(v=>v.Id==selectedUnits[0]); if(u==null) return;
            if(u.Kind==UnitKind.Engineer && GUILayout.Button("Hier Landekopf errichten (4 Bauteile)",buttonStyle)) RequestInvasionOrder(CommandKind.EstablishBeachhead,u.Id,planet:p.Id,x:HexGrid.CellAt(u.X,u.Y).X,y:HexGrid.CellAt(u.X,u.Y).Y);
            foreach(var b in p.Buildings)
            {
                if(!simulation.IsCellVisible(p,PlayerFactionId,b.X,b.Y)) continue;
                if(u.Kind==UnitKind.Engineer && b.Destroyed && b.Kind==BuildingKind.Headquarters && GUILayout.Button("Verwaltungsruine reparieren",buttonStyle)) RequestInvasionOrder(CommandKind.RestoreHeadquarters,u.Id,b.Id,p.Id);
                if(u.Kind==UnitKind.Infantry && !b.Destroyed && simulation.AtWar(PlayerFactionId,GameSimulation.BuildingOwner(p,b)) && GUILayout.Button("Sichern: "+Definitions.Get(b.Kind).Name+" ("+b.X+"/"+b.Y+")",buttonStyle)) RequestInvasionOrder(CommandKind.CaptureBuilding,u.Id,b.Id,p.Id);
                if(b.CapturingFaction==PlayerFactionId) GUILayout.Label("Sicherung: "+b.CaptureProgress.ToString("0.0")+" / "+Definitions.CaptureSeconds+" s\n"+b.Status,smallStyle);
            }
        }
        private void UpdateInvasionViews()
        {
            foreach(var p in simulation.State.Planets)
            {
                LineRenderer ring;
                if(!dangerRings.TryGetValue(p.Id,out ring) || ring==null)
                {ring=MakeLine("Naher Orbitalbereich",spaceRoot,Amber,0.035f,true);ring.positionCount=64;dangerRings[p.Id]=ring;for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*(float)Definitions.OrbitalRadius*SpaceDistanceScale,0.1f,Mathf.Sin(a)*(float)Definitions.OrbitalRadius*SpaceDistanceScale));}}
                var pos=simulation.GetOrbitPosition(p.Id);ring.transform.position=SpacePosition(pos.X,pos.Y);ring.gameObject.SetActive(simulation.IsPlanetKnown(PlayerFactionId,p.Id) && (selectedShips.Count>0 || showingSpacePanel && spacePanelTab==3));
            }
            foreach(var view in invasionViews.Values) if(view!=null) view.gameObject.SetActive(false);
            foreach(var ring in shieldRings.Values) if(ring!=null) ring.gameObject.SetActive(false);
            foreach(var footprint in landingFootprints.Values) if(footprint!=null) footprint.gameObject.SetActive(false);
            if(openPlanetId==0 || surfaceRoot==null) return;
            var planet=simulation.FindPlanet(openPlanetId);
            if(planet==null)return; // A replacement session may not contain the previously open world.
            foreach(var b in planet.Buildings) if(b.Kind==BuildingKind.ShieldGenerator && b.Operational && !b.Destroyed && simulation.CanSeeEntity(planet,PlayerFactionId,b.Id))
            {
                LineRenderer ring;
                if(!shieldRings.TryGetValue(b.Id,out ring)||ring==null)
                {ring=MakeLine("Aktiver Schildbereich",surfaceRoot,GameSimulation.BuildingOwner(planet,b)==PlayerFactionId?Cyan:Danger,0.045f,true);ring.positionCount=64;shieldRings[b.Id]=ring;for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*(float)Definitions.ShieldRadius,0.2f,Mathf.Sin(a)*(float)Definitions.ShieldRadius));}}
                ring.transform.position=SurfacePosition(b.X,0,b.Y);ring.gameObject.SetActive(true);
            }
            foreach(var s in simulation.State.Ships) if(!s.Destroyed && s.TargetPlanetId==openPlanetId && s.TransportPhase!=TransportPhase.Space && (s.OwnerId==PlayerFactionId || simulation.IsCellVisible(planet,PlayerFactionId,s.LandingX,s.LandingY)))
            {
                float height=s.TransportPhase==TransportPhase.Landed?0.5f:s.TransportPhase==TransportPhase.Descending?3*(1-(float)(s.ActionProgress/Definitions.TransitionSeconds)):3*(float)(s.ActionProgress/Definitions.TransitionSeconds);
                InvasionMarker(s.Id,SurfacePosition(s.LandingX,height,s.LandingY),new Vector3(1.1f,0.3f,1.1f),DiplomaticColor(s.OwnerId));
                Transform footprint;
                if(!landingFootprints.TryGetValue(s.Id,out footprint)||footprint==null)
                {
                    footprint=new GameObject("Reservierte Landewaben").transform;footprint.SetParent(surfaceRoot,false);landingFootprints[s.Id]=footprint;
                    for(int i=0;i<7;i++) MakeHexOutline("Landewabe",footprint,DiplomaticColor(s.OwnerId),0.03f);
                }
                int index=0;
                foreach(var cell in HexGrid.LandingCells(s.LandingX,s.LandingY)) footprint.GetChild(index++).localPosition=SurfacePosition(cell.X,0.12f,cell.Y);
                footprint.gameObject.SetActive(true);
            }
            foreach(var h in simulation.State.Beachheads) if(!h.Destroyed && h.PlanetId==openPlanetId && (h.OwnerId==PlayerFactionId || simulation.IsCellVisible(planet,PlayerFactionId,h.X,h.Y)))
                InvasionMarker(h.Id,SurfacePosition(h.X,0.25f,h.Y),new Vector3(0.7f,0.5f,0.7f),h.Progress<Definitions.BeachheadSeconds?Amber:Cyan);
        }
        private void InvasionMarker(long id,Vector3 position,Vector3 size,Color color)
        {
            Transform view;if(!invasionViews.TryGetValue(id,out view)||view==null){view=MakeHex("Landung / Landekopf",surfaceRoot,position,size,color);invasionViews[id]=view;}
            view.gameObject.SetActive(true);view.position=position;
        }
        private void DrawInvasionNotice()
        {
            var entry=simulation.State.Events.FindLast(e=>e.FactionId==PlayerFactionId && (e.Code=="Descent" || e.Code=="EnemyDescent" || e.Code=="OrbitalWarning" || e.Code=="ShipUnderAttack" || e.Code=="PlanetCaptured"));
            if(entry==null) return;
            GUILayout.Label(entry.Message,smallStyle);
            if(GUILayout.Button("Zum gemeldeten Ort",buttonStyle)) {SetView(entry.PlanetId);showingSpacePanel=false;showingMilitary=true;}
        }
    }
}
