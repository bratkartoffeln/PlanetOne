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
        private GUIStyle sessionCardStyle;
        private string selectedSaveSlot;
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
            if(page==SessionPage.Save || page==SessionPage.Load)
            {
                RefreshSlots();
                if(selectedSaveSlot==null || !slotCatalog.Exists(s=>s.Id==selectedSaveSlot && (page!=SessionPage.Save || !s.ReadOnly)))
                    selectedSaveSlot=ActiveSlotId ?? (slotCatalog.Count>0?slotCatalog[0].Id:null);
            }
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
        private void DrawSessionMenu(float width)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("PLANETONE",titleStyle);
            GUILayout.Label(hasSession?"SPIELMENÜ · PARTIE PAUSIERT":"EINZELSPIELER · DEIN WEG ZU DEN STERNEN",smallStyle);
            GUILayout.EndVertical();
            if(hasSession && GUILayout.Button(new GUIContent("Fortsetzen",HudSymbol(HudIcon.Play)),selectedButtonStyle,GUILayout.Width(190),GUILayout.MinHeight(UiRowHeight(44))))CloseSessionMenu();
            GUILayout.EndHorizontal();GUILayout.Space(20);
            if(pendingSessionAction!=null)
            {
                sessionScroll=GUILayout.BeginScrollView(sessionScroll);
                BeginSessionCard();
                SessionHeading(pendingTitle,HudIcon.Info);
                GUILayout.Space(12);GUILayout.Label(pendingDetail,bodyStyle);
                EndSessionCard();
                GUILayout.EndScrollView();GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Abbrechen · Esc",buttonStyle,GUILayout.MinHeight(UiRowHeight(46))))ResolveSessionConfirmation(false);
                if(GUILayout.Button(ConfirmationActionLabel(),selectedButtonStyle,GUILayout.MinHeight(UiRowHeight(46))))ResolveSessionConfirmation(true);
                GUILayout.EndHorizontal();return;
            }
            DrawSessionNavigation(width);
            GUILayout.Space(12);
            sessionScroll=GUILayout.BeginScrollView(sessionScroll);
            switch(sessionPage)
            {
                case SessionPage.Home: DrawSessionHome();break;
                case SessionPage.NewGame: DrawNewSession();break;
                case SessionPage.Save: case SessionPage.Load: DrawSaveSlots();break;
                case SessionPage.Guide: DrawStartGuide(width-GUI.skin.verticalScrollbar.fixedWidth-8);break;
                case SessionPage.Fixtures: DrawSessionFixtures();break;
            }
            GUILayout.EndScrollView();
            if(sessionError!=null){GUILayout.Space(10);GUILayout.Label(sessionError,bodyStyle);}
            GUILayout.Space(14);
            GUILayout.BeginHorizontal();
            if(SessionButton("Einstellungen",HudIcon.Settings))SetSettingsVisible(true);
            if(SessionButton("Spiel beenden",HudIcon.Close))
            {if(hasSession)ConfirmSessionAction("Spiel beenden?","Nicht gespeicherter Fortschritt geht verloren.",()=>Application.Quit());else Application.Quit();}
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Version "+Application.version,smallStyle);
            GUILayout.FlexibleSpace();
            string footer=hasSession?"Esc · zurück zur Partie":"Lokale Partien";GUILayout.Label(footer,smallStyle,GUILayout.Width(smallStyle.CalcSize(new GUIContent(footer)).x+8));
            GUILayout.EndHorizontal();
        }
        private void BeginSessionCard()
        {
            if(sessionCardStyle==null)sessionCardStyle=new GUIStyle{padding=new RectOffset(18,18,16,16),margin=new RectOffset(0,0,0,12)};
            ApplyChromeStates(sessionCardStyle,panelTexture,panelTexture,14);
            GUILayout.BeginVertical(sessionCardStyle);
        }
        private static void EndSessionCard(){GUILayout.EndVertical();}
        private void SessionHeading(string text,HudIcon icon)
        {
            GUILayout.BeginHorizontal();
            Color previous=GUI.color;GUI.color=Cyan;
            GUILayout.Label(HudSymbol(icon),GUILayout.Width(28),GUILayout.Height(28));GUI.color=previous;
            GUILayout.Label(text,headingStyle);GUILayout.EndHorizontal();
        }
        private bool SessionButton(string text,HudIcon icon,bool primary=false)
        {return GUILayout.Button(new GUIContent(text,HudSymbol(icon)),primary?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight(44)));}
        private void DrawSessionNavigation(float width)
        {
            var pages=hasSession?new[]{SessionPage.Home,SessionPage.NewGame,SessionPage.Load,SessionPage.Save,SessionPage.Guide,SessionPage.Fixtures}:
                new[]{SessionPage.Home,SessionPage.NewGame,SessionPage.Load,SessionPage.Guide,SessionPage.Fixtures};
            string[] labels=hasSession?new[]{"Übersicht","Neue Partie","Spiel laden","Spiel speichern","Starthilfe","Prüfkarten"}:
                new[]{"Übersicht","Neue Partie","Spiel laden","Starthilfe","Prüfkarten"};
            int selected=Array.IndexOf(pages,sessionPage);
            int next=DrawWorkspaceTabs(selected,labels,width);
            if(next!=selected){ShowSessionPage(pages[next]);GUIUtility.ExitGUI();}
        }
        private string ConfirmationActionLabel()
        {
            if(pendingTitle.Contains("überschreiben"))return "Überschreiben";
            if(pendingTitle.Contains("wiederherstellen"))return "Sicherung laden";
            if(pendingTitle.Contains("laden"))return "Spielstand laden";
            if(pendingTitle.Contains("beenden"))return "Spiel beenden";
            if(pendingTitle.Contains("Testszenario"))return "Prüfkarte öffnen";
            return "Partie beginnen";
        }
        private void DrawSessionHome()
        {
            BeginSessionCard();
            SessionHeading(hasSession?"Deine laufende Partie":"Baue deine erste Kolonie",hasSession?HudIcon.Planet:HudIcon.Build);
            GUILayout.Space(10);
            if(hasSession)
            {
                GUILayout.Label("Wirtschaft, Flotten und Forschung warten auf deine nächsten Befehle.",bodyStyle);GUILayout.Space(16);
                if(SessionButton("Partie fortsetzen",HudIcon.Play,true))CloseSessionMenu();
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                if(SessionButton("Speichern",HudIcon.Supply))ShowSessionPage(SessionPage.Save);
                if(SessionButton("Laden",HudIcon.Back))ShowSessionPage(SessionPage.Load);
                GUILayout.EndHorizontal();
                GUILayout.Space(12);GUILayout.Label(ActiveSlotId==null?"F5 speichert erstmals auf einem freien Platz.":"Speicherplatz "+ActiveSlotId.Substring(5)+" · F5 speichern · F9 laden",smallStyle);
            }
            else
            {
                GUILayout.Label("Erschließe Planeten, verbinde deine Kolonien und erreiche die Sterne – allein oder mit Verbündeten.",bodyStyle);GUILayout.Space(16);
                if(SessionButton("Neue Partie auswählen",HudIcon.Play,true))ShowSessionPage(SessionPage.NewGame);
                if(SessionButton("Gespeicherte Partie laden",HudIcon.Supply))ShowSessionPage(SessionPage.Load);
            }
            EndSessionCard();
            BeginSessionCard();SessionHeading("Gut vorbereitet starten",HudIcon.Info);
            GUILayout.Label("Die Starthilfe zeigt dir Orientierung, Versorgung, Raumfahrt und das gemeinsame Spielziel.",bodyStyle);GUILayout.Space(12);
            if(SessionButton("Starthilfe öffnen",HudIcon.Info))ShowSessionPage(SessionPage.Guide);
            EndSessionCard();
            if(priorSession!=null && SessionButton("Zur vorherigen Partie zurück",HudIcon.Back))RestorePreviousSession();
        }
        private static readonly string[] ScenarioNames={"Drei Kolonien · Empfohlen","Diplomatische Partie","Gegen einen Computer"};
        private static readonly string[] ScenarioDescriptions={
            "Ein Spieler, zwei unabhängige KI, drei Startkolonien und zwei Kristallmonde. Friedlicher Anfang; errichte das Sprungtor allein oder gemeinsam.",
            "Drei Fraktionen mit friedlichem Start. Erkunde, handle und schließe Verträge. Ein gemeinsamer Torsieg ist möglich.",
            "Ein Spieler gegen eine eigenständige KI. Beginn im Krieg; Aufbau und Gegenwehr stehen im Mittelpunkt."};
        private void DrawNewSession()
        {
            SessionHeading("Wähle deine Partie",HudIcon.Planet);GUILayout.Space(14);
            HudIcon[] icons={HudIcon.Planet,HudIcon.Diplomacy,HudIcon.Units};
            for(int i=0;i<ScenarioNames.Length;i++)if(SessionButton(ScenarioNames[i],icons[i],newScenario==i))newScenario=i;
            GUILayout.Space(14);BeginSessionCard();
            GUILayout.Label(ScenarioNames[newScenario],headingStyle);GUILayout.Space(8);
            GUILayout.Label(ScenarioDescriptions[newScenario],bodyStyle);GUILayout.Space(16);
            GUILayout.Label("Startwert (optional)",headingStyle);
            seedInput=GUILayout.TextField(seedInput,12,GUILayout.MinHeight(UiRowHeight(36)));
            GUILayout.Label("Leer für Zufall. Mit demselben Startwert lässt sich diese Ausgangslage wiederholen.",smallStyle);
            EndSessionCard();
            if(SessionButton("Partie beginnen",HudIcon.Play,true))RequestNewSession(newScenario,seedInput);
        }
        private void DrawSaveSlots()
        {
            bool saving=sessionPage==SessionPage.Save;
            SessionHeading(saving?"Spiel speichern":"Spielstand laden",HudIcon.Supply);
            GUILayout.Label("Wähle einen Speicherplatz. Seine Angaben und Aktionen erscheinen darunter.",smallStyle);GUILayout.Space(14);
            for(int row=0;row<SaveSlots.Count/2;row++)
            {
                GUILayout.BeginHorizontal();
                for(int column=0;column<2;column++)
                {
                    string id=SaveSlots.Id(row*2+column+1);var slot=slotCatalog.Find(s=>s.Id==id);if(slot==null)continue;
                    string status=slot.Valid?"Belegt":slot.Exists?"Beschädigt":slot.BackupExists?"Sicherung":"Frei";
                    if(GUILayout.Button((row*2+column+1)+" · "+status+(ActiveSlotId==id?" · Aktiv":""),selectedSaveSlot==id?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight(42)))){selectedSaveSlot=id;GUIUtility.ExitGUI();}
                }
                GUILayout.EndHorizontal();
            }
            if(!saving && slotCatalog.Exists(s=>s.ReadOnly))
            {
                GUILayout.Space(12);GUILayout.Label("Bisherige Spielstände",smallStyle);
                foreach(var old in slotCatalog)if(old.ReadOnly)
                    if(GUILayout.Button(old.Label,selectedSaveSlot==old.Id?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight(38)))){selectedSaveSlot=old.Id;GUIUtility.ExitGUI();}
            }
            GUILayout.Space(16);
            var info=slotCatalog.Find(s=>s.Id==selectedSaveSlot);
            if(info==null){GUILayout.Label("Kein Speicherplatz ausgewählt.",bodyStyle);return;}
            BeginSessionCard();
            GUILayout.Label(info.Label+(ActiveSlotId==info.Id?" · AKTIV":""),headingStyle);GUILayout.Space(8);
            if(info.Valid)
            {
                GUILayout.Label("Gespeichert am "+info.SavedUtc.ToLocalTime().ToString("dd.MM.yyyy · HH:mm"),bodyStyle);
                GUILayout.Label("Spielzeit "+TimeSpan.FromSeconds(info.Tick*Definitions.TickSeconds).ToString(@"hh\:mm\:ss")+" · Startwert "+info.Seed,smallStyle);
            }
            else GUILayout.Label(info.Exists?"Nicht ladbar: "+info.Error:info.BackupExists?"Die Hauptdatei fehlt. Eine Sicherung ist vorhanden.":"Dieser Platz ist frei.",bodyStyle);
            if(info.ReadOnly)GUILayout.Label("Beim weiteren Speichern bleibt diese bisherige Datei erhalten.",smallStyle);
            if(info.BackupError!=null)GUILayout.Label("Sicherung nicht ladbar: "+info.BackupError,smallStyle);
            else if(info.BackupValid)GUILayout.Label("Eine vorherige gültige Sicherung ist verfügbar.",smallStyle);
            GUILayout.Space(14);
            if(saving)
            {if(SessionButton(info.Exists || info.BackupExists?"Hier überschreiben …":"Hier speichern",HudIcon.Supply,true))RequestSaveSessionSlot(info.Id);}
            else
            {
                bool enabled=GUI.enabled;GUI.enabled=enabled && info.Valid;
                if(SessionButton("Ausgewählten Stand laden",HudIcon.Play,true))RequestLoadSessionSlot(info.Id);
                GUI.enabled=enabled && info.BackupValid;
                if(SessionButton("Vorherige Sicherung laden",HudIcon.Back))RequestLoadSessionSlot(info.Id,true);
                GUI.enabled=enabled;
            }
            EndSessionCard();
        }
        private static readonly string[] GuideTitles={"1 · Orientierung und Zeit","2 · Grundversorgung aufbauen","3 · Raumfahrt und Nachschub","4 · Partner, Konflikte und Ziel"};
        private static readonly string[] GuideTexts={
            "Linksklick wählt ein Objekt. Rechtsklick bewegt gewählte Einheiten oder greift einen Gegner an. Strg + Rechtsklick öffnet weitere Befehle und Diplomatie; rechts ziehen verschiebt die Kamera. Q/E dreht, das Mausrad zoomt. Die Leertaste pausiert. Im Menü steht die Spielzeit still. Ein Doppelklick auf einen Planeten startet den Anflug zur Oberfläche.",
            "Baue zuerst Hydroponik für Nahrung und eine Solaranlage für Energie. Mine, Schmelze und Werkstatt liefern Erz, Metall und Bauteile. Transporter finden selbst befahrbare Wege zur Baustelle und zwischen Gebäuden. Häufige Fahrten hinterlassen langsam sichtbare Spuren. L zeigt Lieferwege und Engpässe; Waren bleiben tatsächlich im Fahrzeug. Wähle ein Gebäude und öffne „Details“, um fehlende Versorgung zu sehen.",
            "Ein versorgtes Labor ermöglicht Forschung; Raumtransport schaltet den Weg zu weiteren Kolonien frei. Der Raumhafen baut Schiffe aus bezahlten Materialien. Flotte trennt Schiffe, Aufträge, Schiffbau und Abbau. Frachter transportieren echte Waren, keine globalen Bestände.",
            "Diplomatie erlaubt Handel, Zugangsrechte und Koalitionen. Die Drei-Kolonien-Partie beginnt friedlich. Für den Sieg brauchst du Forschung, gelieferte Komponenten und ein gesichertes Sprungtor. Verbündete können gemeinsam gewinnen. F5 speichert; im Menü kannst du einen anderen Speicherplatz wählen."};
        private void DrawStartGuide(float width)
        {
            SessionHeading("Starthilfe",HudIcon.Info);GUILayout.Space(14);
            string[] topics={"Orientierung","Versorgung","Raumfahrt","Diplomatie"};
            guidePage=DrawWorkspaceTabs(guidePage,topics,width);
            GUILayout.Space(16);BeginSessionCard();
            GUILayout.Label(GuideTitles[guidePage],headingStyle);GUILayout.Space(12);GUILayout.Label(GuideTexts[guidePage],bodyStyle);GUILayout.Space(18);
            if(hasSession && SessionButton(guidePage==0?"Eigene Kolonie öffnen":guidePage==1?"Baukatalog öffnen":guidePage==2?"Forschung öffnen":"Diplomatie öffnen",guidePage==1?HudIcon.Build:guidePage==2?HudIcon.Research:guidePage==3?HudIcon.Diplomacy:HudIcon.Planet,true))OpenGuideDestination(guidePage);
            EndSessionCard();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("‹ Zurück",buttonStyle,GUILayout.MinHeight(UiRowHeight(38))))guidePage=(guidePage+3)%4;
            if(GUILayout.Button("Weiter ›",buttonStyle,GUILayout.MinHeight(UiRowHeight(38))))guidePage=(guidePage+1)%4;
            GUILayout.EndHorizontal();
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
            SessionHeading("Prüfkarten",HudIcon.Units);GUILayout.Space(10);
            GUILayout.Label("Einzelne Spielbereiche mit vorbereiteten Anlagen ausprobieren. Reguläre Partien findest du unter „Neue Partie“.",bodyStyle);GUILayout.Space(16);
            BeginSessionCard();GUILayout.Label("AUFBAU & WELTEN",smallStyle);
            if(SessionButton("Aufbau und Raumfahrt",HudIcon.Build))LaunchFixture(StartSpaceFixture);
            if(SessionButton("Planetentypen und Luftfracht",HudIcon.Planet))LaunchFixture(StartPlanetEnvironmentFixture);
            if(SessionButton("Asteroidenabbau",HudIcon.Metal))LaunchFixture(StartAsteroidFixture);
            EndSessionCard();
            BeginSessionCard();GUILayout.Label("KONFLIKT & ZUSAMMENARBEIT",smallStyle);
            if(SessionButton("Bodengefecht",HudIcon.Units))LaunchFixture(StartMilitaryFixture);
            if(SessionButton("Invasion",HudIcon.Fleet))LaunchFixture(StartInvasionFixture);
            if(SessionButton("Verträge und Sprungtor",HudIcon.Diplomacy))LaunchFixture(StartDiplomacyFixture);
            EndSessionCard();
        }
    }
}
