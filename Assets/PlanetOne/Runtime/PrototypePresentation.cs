using System;
using System.IO;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public enum InterfaceLayout { CommandCenter, OrbitalBridge, OpenSpace }

    public sealed partial class PrototypeController
    {
        [Serializable] private sealed class PresentationSettings
        { public int Layout; public bool Grid = true; public float Scale = 1; }
        private PresentationSettings presentation = new PresentationSettings();
        private bool settingsOpen, menuOpen, contextOpen = true;
        private string settingsError;
        private string presentationPath;
        private Texture2D panelTexture, buttonTexture, hoverTexture;
        private Vector2 navigationScroll;
        private int presentationSection, buildCategory;
        private GUIStyle buildStyle, selectedBuildStyle;
        private static readonly string[] LayoutNames = { "Kommandozentrale", "Orbitale Brücke", "Freier Weltraum" };
        private static readonly string[] LayoutDescriptions = {
            "Feste rechte Verwaltung, direkte Bauauswahl und kompakte Befehle.",
            "Kolonien links, Objektdetails rechts und ein breites Befehlsdeck unten.",
            "Große Spielfläche, schwebende Werkzeugleiste und ausblendbare Details." };
        public InterfaceLayout CurrentLayout { get { return (InterfaceLayout)presentation.Layout; } }
        private float TopHeight { get { return 112; } }
        private float BottomHeight { get { return CurrentLayout == InterfaceLayout.OrbitalBridge ? 170 : 92; } }
        private float SideWidth { get { return 320; } }
        private bool SidebarVisible { get { return CurrentLayout != InterfaceLayout.OpenSpace || contextOpen; } }
        private Rect SidebarRect(float width, float height)
        { return new Rect(width - SideWidth - (CurrentLayout == InterfaceLayout.OpenSpace ? 12 : 0), TopHeight + 8, SideWidth, height - TopHeight - BottomHeight - 16); }
        public Rect WorldViewport
        {
            get
            {
                float s = UiScale, left = CurrentLayout == InterfaceLayout.OrbitalBridge ? 200 * s : 0;
                float right = CurrentLayout == InterfaceLayout.OpenSpace ? 0 : SideWidth * s;
                return new Rect(left, BottomHeight*s, Mathf.Max(1,Screen.width-left-right), Mathf.Max(1,Screen.height-(TopHeight+BottomHeight)*s));
            }
        }
        public bool IsWorldPoint(Vector2 screenPoint)
        {
            if (settingsOpen || menuOpen || !WorldViewport.Contains(screenPoint)) return false;
            var guiPoint = new Vector2(screenPoint.x/UiScale, (Screen.height-screenPoint.y)/UiScale);
            return !SidebarVisible || !SidebarRect(Screen.width/UiScale,Screen.height/UiScale).Contains(guiPoint);
        }
        private void LoadPresentationSettings()
        {
            if (presentationPath == null) presentationPath = Path.Combine(Application.persistentDataPath,"presentation.json");
            try
            {
                if (File.Exists(presentationPath)) presentation = JsonUtility.FromJson<PresentationSettings>(File.ReadAllText(presentationPath)) ?? new PresentationSettings();
                if(presentation.Layout < 0 || presentation.Layout > 2) presentation.Layout=0;
                if(float.IsNaN(presentation.Scale) || float.IsInfinity(presentation.Scale)) presentation.Scale=1;
                presentation.Scale=Mathf.Clamp(presentation.Scale,0.85f,1.2f);
                Shader.SetGlobalFloat("_PlanetOneGrid",presentation.Grid?1:0);
            }
            catch (Exception e) { presentation = new PresentationSettings(); settingsError="Einstellungen konnten nicht geladen werden: "+e.Message; }
        }
        public bool SetInterfaceLayout(InterfaceLayout layout, bool persist = true)
        {
            if ((int)layout<0 || (int)layout>2) return false;
            presentation.Layout=(int)layout; sidebarScroll=Vector2.zero;
            contextOpen=layout!=InterfaceLayout.OpenSpace;
            selectionStart=null; shipSelectionStart=null;
            if(sceneCamera!=null) UpdateCamera();
            if(persist) SavePresentationSettings();
            return true;
        }
        private void SavePresentationSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(presentationPath));
                string temp=presentationPath+".tmp";
                File.WriteAllText(temp,JsonUtility.ToJson(presentation,true));
                if(File.Exists(presentationPath)) File.Replace(temp,presentationPath,null); else File.Move(temp,presentationPath);
                settingsError=null;
            }
            catch(Exception e) { settingsError="Einstellungen nicht gespeichert: "+e.Message; }
        }
        public void ConfigurePresentationTestPath(string path) { presentationPath=path; LoadPresentationSettings(); UpdateCamera(); }
        public void ReloadPresentationSettings() { LoadPresentationSettings(); UpdateCamera(); }
        public void SetSettingsVisible(bool visible) { settingsOpen=visible; selectionStart=null; shipSelectionStart=null; }
        private void DrawPresentationTop(float width)
        {
            DrawPanel(new Rect(0,0,width,TopHeight));
            GUI.Label(new Rect(18,10,220,28),"PLANETONE",titleStyle);
            GUI.Label(new Rect(19,39,235,21),LayoutNames[presentation.Layout]+" · "+Application.version,smallStyle);
            var p=openPlanetId==0?null:simulation.FindPlanet(openPlanetId);
            string place=p==null?"SONNENSYSTEM":p.Name.ToUpperInvariant();
            GUI.Label(new Rect(258,10,240,24),place,headingStyle);
            string status="Zeit "+FormatTime(simulation.State.Tick*Definitions.TickSeconds);
            if(p!=null && p.OwnerId==PlayerFactionId) status=p.Population+" Bewohner · Energie "+p.EnergyUsed+" / "+p.EnergyProduced;
            GUI.Label(new Rect(258,37,360,23),status,bodyStyle);
            float x=width-470;
            if(Button(new Rect(x,16,95,34),simulation.State.Paused?"Weiter":"Pause",simulation.State.Paused)) TogglePause();
            if(Button(new Rect(x+101,16,55,34),simulation.State.TimeScale+"×",false)) SetSimulationSpeed(simulation.State.TimeScale==1?2:1);
            if(Button(new Rect(x+162,16,140,34),"Einstellungen",settingsOpen)) SetSettingsVisible(!settingsOpen);
            if(Button(new Rect(x+308,16,142,34),"Spielmenü",menuOpen)) { menuOpen=!menuOpen; selectionStart=null; shipSelectionStart=null; }
            string[] tabs={"Übersicht","Bauen","Einheiten","Flotte","Forschung","Logistik","Diplomatie"};
            float item=(width-36)/tabs.Length;
            for(int i=0;i<tabs.Length;i++)
                if(Button(new Rect(18+i*item,73,item-6,30),tabs[i],false)) OpenPresentationSection(i);
        }
        public void OpenPresentationSection(int section)
        {
            if(section<0 || section>6)throw new ArgumentOutOfRangeException("section");
            presentationSection=section;
            contextOpen=true; showingDetails=false; showingDiplomacy=false; showingSpacePanel=false; showingMilitary=false;
            sidebarScroll=Vector2.zero;
            if(section==2) showingMilitary=true;
            if(section>=3 && section<=5) { showingSpacePanel=true; spacePanelTab=section==3?0:section==4?1:2; }
            if(section==6) showingDiplomacy=true;
        }
        private void DrawPresentationBottom(float width,float height)
        {
            float y=height-BottomHeight;
            Rect deck=CurrentLayout==InterfaceLayout.OpenSpace?new Rect(16,y+4,width-32,BottomHeight-12):new Rect(0,y,width,BottomHeight);
            DrawPanel(deck);
            if(CurrentLayout==InterfaceLayout.OrbitalBridge)
            {
                DrawRadar(new Rect(16,y+12,175,140));
                GUI.Label(new Rect(210,y+12,400,26),openPlanetId==0?"FLOTTENKOMMANDO":"KOLONIEKOMMANDO",headingStyle);
                var selected=openPlanetId==0?null:FindSelected(simulation.FindPlanet(openPlanetId));
                GUI.Label(new Rect(210,y+43,410,46),selected==null?"Objekt auswählen, um Zustand und Versorgung zu prüfen.":Definitions.Get(selected.Kind).Name+"\n"+selected.Status,bodyStyle);
            }
            float left=CurrentLayout==InterfaceLayout.OrbitalBridge?650:32;
            float commandsY=CurrentLayout==InterfaceLayout.OrbitalBridge?y+18:y+10;
            float available=width-left-24, buttonWidth=Mathf.Min(150,(available-18)/4);
            if(Button(new Rect(left,commandsY,buttonWidth,30),"System",false))SetView(0);
            if(Button(new Rect(left+buttonWidth+6,commandsY,buttonWidth,30),"Auswahl",false)){contextOpen=true;showingDetails=true;showingSpacePanel=false;showingDiplomacy=false;}
            if(Button(new Rect(left+2*(buttonWidth+6),commandsY,buttonWidth,30),"Bau beenden",false))CancelPlacement();
            if(CurrentLayout==InterfaceLayout.OpenSpace)
            {
                if(Button(new Rect(left+3*(buttonWidth+6),commandsY,buttonWidth,30),SidebarVisible?"Details aus":"Details ein",false))contextOpen=!contextOpen;
            }
            else if(Button(new Rect(left+3*(buttonWidth+6),commandsY,buttonWidth,30),"Versorgung",false))OpenPresentationSection(5);
            string message=Time.unscaledTime<notificationUntil?notification:placementReason;
            if(string.IsNullOrEmpty(message))message="WASD Kamera · Mausrad Zoom · Leertaste Pause · F5/F9 Speichern/Laden · Esc Menü";
            GUI.Label(new Rect(CurrentLayout==InterfaceLayout.OrbitalBridge?210:32,y+BottomHeight-43,width-(CurrentLayout==InterfaceLayout.OrbitalBridge?240:64),38),message,bodyStyle);
        }
        private void DrawPresentationNavigation(float width,float height)
        {
            if(CurrentLayout!=InterfaceLayout.OrbitalBridge)return;
            DrawPanel(new Rect(0,TopHeight,194,height-TopHeight-BottomHeight));
            GUILayout.BeginArea(new Rect(12,TopHeight+14,170,height-TopHeight-BottomHeight-28));
            navigationScroll=GUILayout.BeginScrollView(navigationScroll);
            GUILayout.Label("KOLONIEN",headingStyle);
            foreach(var p in simulation.State.Planets)
                if(simulation.IsPlanetKnown(PlayerFactionId,p.Id) && GUILayout.Button(p.Name,openPlanetId==p.Id?selectedButtonStyle:buttonStyle,GUILayout.Height(34)))SetView(p.Id);
            GUILayout.Space(18);GUILayout.Label("FLOTTEN",headingStyle);
            foreach(var ship in simulation.State.Ships)
                if(ship.OwnerId==PlayerFactionId && !ship.Destroyed && !ship.Consumed && GUILayout.Button("#"+ship.Id+" · "+Definitions.Ships[(int)ship.Kind].Name,buttonStyle,GUILayout.Height(35)))
                {SetView(0);SelectShip(ship.Id,false);OpenPresentationSection(3);}
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        private void DrawRadar(Rect rect)
        {
            Fill(rect,new Color(0.015f,0.035f,0.05f));
            if(openPlanetId==0)
            {
                float extent=1;
                foreach(var p in simulation.State.Planets)if(simulation.IsPlanetKnown(PlayerFactionId,p.Id))extent=Mathf.Max(extent,(float)p.OrbitRadius+3);
                foreach(var p in simulation.State.Planets)
                {
                    if(!simulation.IsPlanetKnown(PlayerFactionId,p.Id))continue;
                    var pos=simulation.GetOrbitPosition(p.Id);
                    Rect dot=new Rect(rect.center.x+(float)pos.X/extent*rect.width*0.42f-5,rect.center.y-(float)pos.Y/extent*rect.height*0.42f-5,10,10);
                    if(GUI.Button(dot,GUIContent.none,buttonStyle))SetView(p.Id);
                }
                Fill(new Rect(rect.center.x-3,rect.center.y-3,6,6),Amber);
            }
            else
            {
                var p=simulation.FindPlanet(openPlanetId);
                foreach(var b in p.Buildings)
                {
                    // Radar never adds a new source of intelligence about foreign colonies.
                    if(p.OwnerId!=PlayerFactionId)continue;
                    if(GameSimulation.BuildingOwner(p,b)!=PlayerFactionId && !simulation.IsCellVisible(p,PlayerFactionId,b.X,b.Y))continue;
                    Rect dot=new Rect(rect.x+8+b.X/(float)p.Width*(rect.width-20),rect.y+8+(1-b.Y/(float)p.Height)*(rect.height-20),7,7);
                    if(GUI.Button(dot,GUIContent.none,buttonStyle))SelectBuilding(b.Id);
                }
            }
            GUI.Label(new Rect(rect.x+6,rect.y+3,rect.width-12,18),"TAKTISCHE ÜBERSICHT",smallStyle);
        }
        private void DrawPresentationDialogs(float width,float height)
        {
            if(!settingsOpen && !menuOpen)return;
            Fill(new Rect(0,0,width,height),new Color(0,0,0,0.76f));
            Rect box=new Rect((width-660)/2,(height-540)/2,660,540);DrawPanel(box);
            GUILayout.BeginArea(new Rect(box.x+26,box.y+22,608,496));
            if(settingsOpen)
            {
                GUILayout.Label("EINSTELLUNGEN · DARSTELLUNG",titleStyle);GUILayout.Space(18);
                for(int i=0;i<3;i++)
                {
                    if(GUILayout.Button(((char)('A'+i))+" · "+LayoutNames[i],presentation.Layout==i?selectedButtonStyle:buttonStyle,GUILayout.Height(42)))SetInterfaceLayout((InterfaceLayout)i);
                    GUILayout.Label(LayoutDescriptions[i],bodyStyle);GUILayout.Space(12);
                }
                bool grid=GUILayout.Toggle(presentation.Grid,"Wabengrenzen anzeigen");
                if(grid!=presentation.Grid){presentation.Grid=grid;Shader.SetGlobalFloat("_PlanetOneGrid",grid?1:0);SavePresentationSettings();}
                GUILayout.Label("Oberflächengröße",headingStyle);
                GUILayout.BeginHorizontal();
                float[] scales={0.85f,1,1.2f};string[] labels={"Kompakt","Normal","Groß"};
                for(int i=0;i<scales.Length;i++)if(GUILayout.Button(labels[i],Mathf.Approximately(presentation.Scale,scales[i])?selectedButtonStyle:buttonStyle,GUILayout.Height(30)))
                {presentation.Scale=scales[i];UpdateCamera();SavePresentationSettings();}
                GUILayout.EndHorizontal();
                if(settingsError!=null)GUILayout.Label(settingsError,bodyStyle);
                GUILayout.Space(12);if(GUILayout.Button("Zurück zum Spiel",buttonStyle,GUILayout.Height(36)))settingsOpen=false;
            }
            else
            {
                GUILayout.Label("PLANETONE",titleStyle);GUILayout.Label("SPIELMENÜ",headingStyle);GUILayout.Space(20);
                if(GUILayout.Button("Fortsetzen",buttonStyle,GUILayout.Height(40)))menuOpen=false;
                if(GUILayout.Button("Spiel speichern",buttonStyle,GUILayout.Height(40))){SaveGame();menuOpen=false;}
                if(GUILayout.Button("Spiel laden",buttonStyle,GUILayout.Height(40))){LoadGame();menuOpen=false;}
                if(GUILayout.Button("Einstellungen",buttonStyle,GUILayout.Height(40))){menuOpen=false;settingsOpen=true;}
                GUILayout.Space(22);GUILayout.Label("NEUE PARTIE · laufendes Spiel vorher speichern",smallStyle);
                if(GUILayout.Button("Diplomatische Partie",buttonStyle,GUILayout.Height(40))){StartDiplomacyScenario();menuOpen=false;}
                if(GUILayout.Button("Gegen Computer spielen",buttonStyle,GUILayout.Height(40))){StartAiScenario();menuOpen=false;}
                GUILayout.Space(20);if(GUILayout.Button("Spiel beenden",buttonStyle,GUILayout.Height(36)))Application.Quit();
            }
            GUILayout.EndArea();
        }
        private void DrawPanel(Rect rect)
        {
            Color tint=CurrentLayout==InterfaceLayout.OrbitalBridge?new Color(0.14f,0.22f,0.31f):new Color(0.13f,0.17f,0.20f);
            Color old=GUI.color;GUI.color=tint;
            GUI.DrawTexture(rect,panelTexture??Texture2D.whiteTexture);GUI.color=old;
            Fill(new Rect(rect.x,rect.y,rect.width,1),Border);
            Fill(new Rect(rect.x,rect.y,2,rect.height),CurrentLayout==InterfaceLayout.OrbitalBridge?Cyan:Border);
        }
        private void PreparePresentationSkin()
        {
            if(panelTexture==null)
            {
                panelTexture=CreateInterfaceTexture(false);buttonTexture=CreateInterfaceTexture(true);hoverTexture=CreateInterfaceTexture(true);
            }
            buttonStyle.normal.background=buttonTexture;buttonStyle.hover.background=hoverTexture;
            buttonStyle.normal.textColor=Ink;buttonStyle.hover.textColor=Color.white;
            buttonStyle.active.background=hoverTexture;buttonStyle.active.textColor=Cyan;
            selectedButtonStyle.normal.background=hoverTexture;
            selectedButtonStyle.normal.textColor=Cyan;
            if(buildStyle==null)
            {
                buildStyle=new GUIStyle(buttonStyle){imagePosition=ImagePosition.ImageAbove,alignment=TextAnchor.MiddleCenter,fontSize=11,fixedWidth=112,padding=new RectOffset(4,4,5,5)};
                selectedBuildStyle=new GUIStyle(buildStyle);selectedBuildStyle.normal.textColor=Cyan;
            }
            GUI.skin.button=buttonStyle;GUI.skin.label=bodyStyle;
            GUI.skin.toggle.fontSize=14;GUI.skin.toggle.normal.textColor=Ink;
            GUI.skin.textField.fontSize=13;GUI.skin.textField.normal.background=buttonTexture;
            GUI.skin.textField.focused.background=hoverTexture;GUI.skin.textField.normal.textColor=Ink;GUI.skin.textField.focused.textColor=Color.white;
            GUI.skin.verticalScrollbar.normal.background=panelTexture;
            GUI.skin.verticalScrollbarThumb.normal.background=buttonTexture;
            GUI.skin.horizontalScrollbar.normal.background=panelTexture;
            GUI.skin.horizontalScrollbarThumb.normal.background=buttonTexture;
        }
        private static Texture2D CreateInterfaceTexture(bool button)
        {
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Gebürstetes Interface",wrapMode=TextureWrapMode.Repeat};
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                float grain=((x*13+y*71)%17)/1700f;
                float value=button?0.12f+(64-y)/1400f+grain:0.75f+grain;
                if(button && (x==0||y==0||x==63||y==63))value=0.3f;
                texture.SetPixel(x,y,new Color(value*0.78f,value*0.95f,value*1.12f,1));
            }
            texture.Apply();return texture;
        }
    }
}
