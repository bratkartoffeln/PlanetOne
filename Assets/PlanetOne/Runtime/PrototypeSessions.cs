using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using PlanetOne.Core;
using PlanetOne.Platform;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public enum SessionPage { Home, NewGame, Load, Save, Guide, Fixtures }

    public sealed partial class PrototypeController
    {
        private sealed class SessionSlot { public string Id; }
        private readonly ConditionalWeakTable<GameSimulation,SessionSlot> sessionSlots=new ConditionalWeakTable<GameSimulation,SessionSlot>();
        private SaveSlots saveSlots;
        private List<SaveSlotInfo> slotCatalog=new List<SaveSlotInfo>();
        private SessionPage sessionPage;
        private Vector2 sessionScroll;
        private bool hasSession, settingsReturnToMenu;
        private string sessionError, seedInput="", pendingTitle, pendingDetail;
        private int newScenario, guidePage;
        private Action pendingSessionAction;
        public bool HasActiveSession {get{return hasSession;}}
        public bool HasSessionConfirmation {get{return pendingSessionAction!=null;}}
        public SessionPage CurrentSessionPage {get{return sessionPage;}}
        public string ActiveSlotId {get{SessionSlot slot;return sessionSlots.TryGetValue(simulation,out slot)?slot.Id:null;}}
        public string SessionError {get{return sessionError;}}

        private void InitializeSessions()
        {
            saveSlots=new SaveSlots(Application.persistentDataPath);
            hasSession=PrototypeSmokeRunner.IsRequested;
            if(!hasSession)ShowSessionPage(SessionPage.Home);
            UpdateCamera();
        }
        public void ConfigureSessionTestPath(string directory,bool titleScreen=false)
        {
            if(!PrototypeSmokeRunner.IsRequested)throw new InvalidOperationException("Only available in explicit smoke mode.");
            saveSlots=new SaveSlots(directory);smokeSavePath=null;sessionSlots.Remove(simulation);RefreshSlots();
            if(titleScreen){ReplaceSession(GameSimulation.CreateDefault(),0);priorSession=null;hasSession=false;ShowSessionPage(SessionPage.Home);UpdateCamera();}
        }
        private void AssignSlot(string id)
        {sessionSlots.Remove(simulation);if(id!=null)sessionSlots.Add(simulation,new SessionSlot{Id=id});}
        private void RefreshSlots()
        {
            try{slotCatalog=saveSlots.List();}
            catch(Exception error){slotCatalog.Clear();sessionError="Speicherplätze konnten nicht gelesen werden: "+error.Message;}
        }
        public void ShowSessionPage(SessionPage page)
        {
            SetMenuVisible(true);settingsOpen=false;sessionPage=page;sessionScroll=Vector2.zero;sessionError=null;pendingSessionAction=null;
            if(page==SessionPage.Save || page==SessionPage.Load)RefreshSlots();
        }
        private void ConfirmSessionAction(string title,string detail,Action action)
        {pendingTitle=title;pendingDetail=detail;pendingSessionAction=action;sessionError=null;}
        public void ResolveSessionConfirmation(bool confirm)
        {var action=pendingSessionAction;pendingSessionAction=null;if(confirm && action!=null)action();}
        private void CloseSessionMenu()
        {if(!hasSession){ShowSessionPage(SessionPage.Home);return;}pendingSessionAction=null;menuOpen=false;commandDismissFrame=Time.frameCount;}
        private void CloseSettings()
        {settingsOpen=false;if(settingsReturnToMenu || !hasSession)ShowSessionPage(SessionPage.Home);else commandDismissFrame=Time.frameCount;}
        public bool RestorePreviousSession()
        {if(priorSession==null)return false;var previous=priorSession;priorSession=null;ReplaceSession(previous,0);CloseSessionMenu();return true;}
        private bool HandleSessionEscape()
        {
            if(!menuOpen)return false;
            if(pendingSessionAction!=null)ResolveSessionConfirmation(false);
            else if(sessionPage!=SessionPage.Home)ShowSessionPage(SessionPage.Home);
            else CloseSessionMenu();
            return true;
        }
        public bool RequestNewSession(int scenario,string seed)
        {
            int parsed;
            if(scenario<0 || scenario>2){sessionError="Unbekannte Partie.";return false;}
            if(string.IsNullOrWhiteSpace(seed))parsed=Environment.TickCount;
            else if(!int.TryParse(seed,NumberStyles.Integer,CultureInfo.InvariantCulture,out parsed))
            {sessionError="Der Startwert muss eine ganze Zahl zwischen -2147483648 und 2147483647 sein.";return false;}
            Action start=()=>StartSession(scenario,parsed);
            if(hasSession)ConfirmSessionAction("Neue Partie beginnen?","Ungespeicherter Fortschritt der laufenden Partie geht verloren. Gespeicherte Partien bleiben erhalten.",start);
            else start();
            return true;
        }
        private void StartSession(int scenario,int seed)
        {
            GameSimulation next=scenario==0?GameSimulation.CreateReferenceScenario(seed):scenario==1?GameSimulation.CreateDiplomacyScenario(seed):GameSimulation.CreateAiScenario(seed);
            ReplaceSession(next,0);priorSession=null;guidePage=0;ShowSessionPage(SessionPage.Guide);
        }
        private void ReplaceSession(GameSimulation next,int view)
        {
            simulation=next;hasSession=true;foodRates.Clear();lastSimulationMessage=simulation.State.LastMessage;
            controlGroups.Clear();shipGroups.Clear();CloseCommandMenu();CancelPlacement();CancelSpacePan();
            militaryTargetMode=null;spaceMoveTargetMode=false;showingSpacePanel=false;showingMilitary=false;showingDiplomacy=false;panelOpen=false;
            BuildSpace();SetView(view);sessionError=null;
        }
        public bool SaveSessionSlot(string id,bool overwrite=false)
        {
            if(!hasSession){sessionError="Zuerst eine Partie beginnen oder laden.";return false;}
            try
            {saveSlots.Save(id,simulation.State,overwrite);AssignSlot(id);sessionError=null;Notify("Gespeichert in "+saveSlots.Inspect(id).Label+". F5 aktualisiert diesen Platz.");RefreshSlots();return true;}
            catch(Exception error){sessionError="Speichern fehlgeschlagen: "+error.Message;Notify(sessionError);return false;}
        }
        public void RequestSaveSessionSlot(string id)
        {
            try
            {
                var info=saveSlots.Inspect(id);
                if(info.ReadOnly){sessionError="Bisherige Spielstände bleiben unverändert. Einen neuen Platz wählen.";return;}
                if(info.Exists || info.BackupExists)
                    ConfirmSessionAction(info.Label+" überschreiben?","Der aktuelle Stand ersetzt diesen Speicherplatz. Die letzte gültige Sicherung bleibt erhalten.",()=>{if(SaveSessionSlot(id,true))CloseSessionMenu();});
                else if(SaveSessionSlot(id))CloseSessionMenu();
            }
            catch(Exception error){sessionError=error.Message;}
        }
        public void RequestLoadSessionSlot(string id,bool backup=false)
        {
            Action load=()=>{if(LoadSessionSlot(id,backup))CloseSessionMenu();};
            if(hasSession)ConfirmSessionAction(backup?"Sicherung wiederherstellen?":"Spielstand laden?","Ungespeicherter Fortschritt wird verworfen. "+(backup?"Die vorherige Sicherung wird ausdrücklich geladen; die Hauptdatei bleibt zunächst unverändert.":"Der gewählte Speicherplatz wird geladen."),load);
            else load();
        }
        public bool LoadSessionSlot(string id,bool backup=false)
        {
            var previous=simulation;int previousView=openPlanetId;bool previousActive=hasSession;bool replaced=false;
            try
            {
                var next=new GameSimulation(saveSlots.Load(id,backup));
                replaced=true;ReplaceSession(next,0);AssignSlot(saveSlots.IsReadOnly(id)?null:id);priorSession=null;
                Notify(backup?"Sicherung geladen. F5 speichert sie als aktuellen Stand.":"Spielstand geladen. F5 speichert den weiteren Fortschritt.");return true;
            }
            catch(Exception error)
            {
                if(replaced){ReplaceSession(previous,previousView);hasSession=previousActive;}
                sessionError="Laden fehlgeschlagen; laufende Partie bleibt erhalten: "+error.Message;Notify(sessionError);return false;
            }
        }
        private bool QuickSaveSession()
        {
            if(!hasSession)return false;
            string id=ActiveSlotId;
            if(id!=null)return SaveSessionSlot(id,true);
            id=saveSlots.FirstEmpty();
            if(id!=null)return SaveSessionSlot(id);
            ShowSessionPage(SessionPage.Save);sessionError="Alle acht Plätze sind belegt. Einen Platz auswählen und das Überschreiben bestätigen.";return false;
        }
        private bool QuickLoadSession(bool backup)
        {
            if(ActiveSlotId==null){ShowSessionPage(SessionPage.Load);return false;}
            ShowSessionPage(SessionPage.Load);RequestLoadSessionSlot(ActiveSlotId,backup);return true;
        }
        private void LaunchFixture(Action start)
        {
            Action launch=()=>{bool hadSession=hasSession;start();if(!hadSession)priorSession=null;hasSession=true;UpdateCamera();CloseSessionMenu();};
            if(hasSession)ConfirmSessionAction("Testszenario öffnen?","Separate Prüfkarte mit vorbereiteten Beständen. Die vorherige Partie bleibt über „Zur vorherigen Partie zurück“ erreichbar.",launch);else launch();
        }
        private void DrawSessionMenu()
        {
            GUILayout.Label("PLANETONE",titleStyle);
            GUILayout.Label("AUFBAU · RAUMFAHRT · DIPLOMATIE",smallStyle);GUILayout.Space(14);
            if(pendingSessionAction!=null)
            {
                GUILayout.Label(pendingTitle,headingStyle);GUILayout.Space(14);GUILayout.Label(pendingDetail,bodyStyle);GUILayout.Space(24);
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Abbrechen",buttonStyle,GUILayout.Height(44)))ResolveSessionConfirmation(false);
                if(GUILayout.Button("Bestätigen",buttonStyle,GUILayout.Height(44)))ResolveSessionConfirmation(true);
                GUILayout.EndHorizontal();return;
            }
            sessionScroll=GUILayout.BeginScrollView(sessionScroll);
            switch(sessionPage)
            {
                case SessionPage.Home: DrawSessionHome();break;
                case SessionPage.NewGame: DrawNewSession();break;
                case SessionPage.Save: case SessionPage.Load: DrawSaveSlots();break;
                case SessionPage.Guide: DrawStartGuide();break;
                case SessionPage.Fixtures: DrawSessionFixtures();break;
            }
            GUILayout.EndScrollView();
            if(sessionError!=null){GUILayout.Space(8);GUILayout.Label(sessionError,bodyStyle);}
            GUILayout.Space(10);
            if(sessionPage!=SessionPage.Home && GUILayout.Button("Zurück zum Menü",buttonStyle,GUILayout.Height(34)))ShowSessionPage(SessionPage.Home);
            GUILayout.Label("Version "+Application.version+(hasSession?" · Spielzeit hält im Menü an":" · Einzelspieler"),smallStyle);
        }
        private void DrawSessionHome()
        {
            if(hasSession)
            {
                if(GUILayout.Button("Partie fortsetzen",selectedButtonStyle,GUILayout.Height(46)))CloseSessionMenu();
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Speichern …",buttonStyle,GUILayout.Height(40)))ShowSessionPage(SessionPage.Save);
                if(GUILayout.Button("Laden …",buttonStyle,GUILayout.Height(40)))ShowSessionPage(SessionPage.Load);
                GUILayout.EndHorizontal();
                GUILayout.Label(ActiveSlotId==null?"Noch kein Speicherplatz zugeordnet. F5 nutzt einen freien Platz.":"Aktiver Speicherplatz: "+ActiveSlotId.Substring(5)+" · F5 speichern · F9 laden",smallStyle);
            }
            else if(GUILayout.Button("Gespeicherte Partie laden",buttonStyle,GUILayout.Height(44)))ShowSessionPage(SessionPage.Load);
            GUILayout.Space(14);
            if(GUILayout.Button("Neue Partie …",buttonStyle,GUILayout.Height(46)))ShowSessionPage(SessionPage.NewGame);
            if(GUILayout.Button("Starthilfe · die ersten Schritte",buttonStyle,GUILayout.Height(40)))ShowSessionPage(SessionPage.Guide);
            if(GUILayout.Button("Einstellungen",buttonStyle,GUILayout.Height(40)))SetSettingsVisible(true);
            if(priorSession!=null && GUILayout.Button("Zur vorherigen Partie zurück",buttonStyle,GUILayout.Height(34)))
            RestorePreviousSession();
            GUILayout.Space(16);
            if(GUILayout.Button("Testszenarien …",buttonStyle,GUILayout.Height(32)))ShowSessionPage(SessionPage.Fixtures);
            GUILayout.Label("Prüfkarten sind getrennt von regulären Partien.",smallStyle);GUILayout.Space(12);
            if(GUILayout.Button("Spiel beenden",buttonStyle,GUILayout.Height(34)))
            {if(hasSession)ConfirmSessionAction("Spiel beenden?","Nicht gespeicherter Fortschritt geht verloren.",()=>Application.Quit());else Application.Quit();}
        }
        private static readonly string[] ScenarioNames={"Drei Kolonien · Empfohlen","Diplomatische Partie","Gegen einen Computer"};
        private static readonly string[] ScenarioDescriptions={
            "Ein Spieler, zwei unabhängige KI, drei Startkolonien und zwei Kristallmonde. Friedlicher Anfang; errichte das Sprungtor allein oder gemeinsam.",
            "Drei Fraktionen mit friedlichem Start. Erkunde, handle und schließe Verträge. Ein gemeinsamer Torsieg ist möglich.",
            "Ein Spieler gegen eine eigenständige KI. Beginn im Krieg; Aufbau und Gegenwehr stehen im Mittelpunkt."};
        private void DrawNewSession()
        {
            GUILayout.Label("NEUE PARTIE",headingStyle);GUILayout.Space(10);
            for(int i=0;i<ScenarioNames.Length;i++)if(GUILayout.Button(ScenarioNames[i],newScenario==i?selectedButtonStyle:buttonStyle,GUILayout.Height(40)))newScenario=i;
            GUILayout.Space(12);GUILayout.Label(ScenarioDescriptions[newScenario],bodyStyle);GUILayout.Space(14);
            GUILayout.Label("Startwert · leer lassen für Zufall",headingStyle);seedInput=GUILayout.TextField(seedInput,12,GUILayout.Height(30));
            GUILayout.Label("Gleicher Startwert und gleiche Partie erzeugen denselben Ausgangszustand. Jede Fraktion bezahlt ihren Aufbau aus endlichen Vorräten.",smallStyle);
            GUILayout.Space(16);if(GUILayout.Button("Partie beginnen",selectedButtonStyle,GUILayout.Height(44)))RequestNewSession(newScenario,seedInput);
        }
        private void DrawSaveSlots()
        {
            bool saving=sessionPage==SessionPage.Save;
            GUILayout.Label(saving?"SPIEL SPEICHERN":"SPIELSTAND LADEN",headingStyle);
            GUILayout.Label(saving?"Acht unabhängige Plätze. Belegte Plätze erfordern eine Bestätigung.":"Sicherungen enthalten den vorherigen gültigen Stand und werden nur auf Wunsch geladen.",smallStyle);GUILayout.Space(10);
            foreach(var info in slotCatalog)
            {
                if(saving && info.ReadOnly)continue;
                GUILayout.Label(info.Label+(ActiveSlotId==info.Id?" · AKTIV":""),headingStyle);
                string detail=info.Valid?info.SavedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm")+" · Spielzeit "+TimeSpan.FromSeconds(info.Tick*Definitions.TickSeconds).ToString(@"hh\:mm\:ss")+" · Startwert "+info.Seed:info.Exists?"Nicht ladbar: "+info.Error:info.BackupExists?"Nur Sicherung vorhanden":"Freier Speicherplatz";
                GUILayout.Label(detail,smallStyle);GUILayout.BeginHorizontal();
                if(saving){if(GUILayout.Button(info.Exists || info.BackupExists?"Hier überschreiben …":"Hier speichern",buttonStyle,GUILayout.Height(32)))RequestSaveSessionSlot(info.Id);}
                else
                {
                    bool enabled=GUI.enabled;GUI.enabled=enabled && info.Valid;
                    if(GUILayout.Button("Laden",buttonStyle,GUILayout.Height(32)))RequestLoadSessionSlot(info.Id);
                    GUI.enabled=enabled && info.BackupValid;
                    if(GUILayout.Button("Sicherung laden",buttonStyle,GUILayout.Height(32)))RequestLoadSessionSlot(info.Id,true);
                    GUI.enabled=enabled;
                }
                GUILayout.EndHorizontal();if(info.BackupError!=null)GUILayout.Label("Sicherung nicht ladbar: "+info.BackupError,smallStyle);GUILayout.Space(14);
            }
        }
        private static readonly string[] GuideTitles={"1 · Orientierung und Zeit","2 · Grundversorgung aufbauen","3 · Raumfahrt und Nachschub","4 · Partner, Konflikte und Ziel"};
        private static readonly string[] GuideTexts={
            "Linksklick wählt ein Objekt. Kurzer Rechtsklick zeigt seine Befehle; rechts ziehen verschiebt die Kamera. Q/E dreht, das Mausrad zoomt. Die Leertaste pausiert. Im Menü steht die Spielzeit still. Öffne Aster über die Planetenkarte und „Oberfläche öffnen“.",
            "Baue zuerst Hydroponik für Nahrung und eine Solaranlage für Energie. Mine, Schmelze und Werkstatt liefern Erz, Metall und Bauteile. Bauflächen brauchen erreichbare Straßen; Material fährt tatsächlich zur Baustelle. Wähle ein Gebäude und öffne „Details“, um fehlende Versorgung zu sehen.",
            "Ein versorgtes Labor ermöglicht Forschung; Raumtransport schaltet den Weg zu weiteren Kolonien frei. Der Raumhafen baut Schiffe aus bezahlten Materialien. Flotte trennt Schiffe, Aufträge, Schiffbau und Abbau. Frachter transportieren echte Waren, keine globalen Bestände.",
            "Diplomatie erlaubt Handel, Zugangsrechte und Koalitionen. Die Drei-Kolonien-Partie beginnt friedlich. Für den Sieg brauchst du Forschung, gelieferte Komponenten und ein gesichertes Sprungtor. Verbündete können gemeinsam gewinnen. F5 speichert; im Menü kannst du einen anderen Speicherplatz wählen."};
        private void DrawStartGuide()
        {
            GUILayout.Label("STARTHILFE",headingStyle);GUILayout.Space(14);GUILayout.Label(GuideTitles[guidePage],headingStyle);GUILayout.Space(12);GUILayout.Label(GuideTexts[guidePage],bodyStyle);GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("‹ Vorheriger Schritt",buttonStyle,GUILayout.Height(34)))guidePage=(guidePage+3)%4;
            if(GUILayout.Button("Nächster Schritt ›",buttonStyle,GUILayout.Height(34)))guidePage=(guidePage+1)%4;
            GUILayout.EndHorizontal();GUILayout.Space(12);GUILayout.Label("Vier kurze Schritte. Jederzeit über das Spielmenü erreichbar.",smallStyle);
            if(hasSession && GUILayout.Button(guidePage==0?"Eigene Kolonie öffnen":guidePage==1?"Baukatalog öffnen":guidePage==2?"Forschung öffnen":"Diplomatie öffnen",selectedButtonStyle,GUILayout.Height(42)))OpenGuideDestination(guidePage);
            if(hasSession && GUILayout.Button("Zur Partie",buttonStyle,GUILayout.Height(36)))CloseSessionMenu();
        }
        public void OpenGuideDestination(int page)
        {
            if(!hasSession)return;CloseSessionMenu();
            if(page<3)
            {var planet=simulation.State.Planets.Find(p=>p.OwnerId==PlayerFactionId);if(planet==null)return;SetView(planet.Id);if(page>0)OpenPresentationSection(page==1?0:4);}
            else OpenPresentationSection(6);
        }
        private void DrawSessionFixtures()
        {
            GUILayout.Label("TESTSZENARIEN",headingStyle);GUILayout.Label("Separate Prüfkarten mit endlichen vorbereiteten Anlagen und Beständen. Kein regulärer Spielstart.",bodyStyle);GUILayout.Space(14);
            if(GUILayout.Button("Aufbau / Raumfahrt",buttonStyle,GUILayout.Height(40)))LaunchFixture(StartSpaceFixture);
            if(GUILayout.Button("Bodengefecht",buttonStyle,GUILayout.Height(40)))LaunchFixture(StartMilitaryFixture);
            if(GUILayout.Button("Invasion",buttonStyle,GUILayout.Height(40)))LaunchFixture(StartInvasionFixture);
            if(GUILayout.Button("Verträge / Sprungtor",buttonStyle,GUILayout.Height(40)))LaunchFixture(StartDiplomacyFixture);
            if(GUILayout.Button("Planetentypen / Luftfracht",buttonStyle,GUILayout.Height(40)))LaunchFixture(StartPlanetEnvironmentFixture);
            if(GUILayout.Button("Asteroidenabbau",buttonStyle,GUILayout.Height(40)))LaunchFixture(StartAsteroidFixture);
        }
    }
}
