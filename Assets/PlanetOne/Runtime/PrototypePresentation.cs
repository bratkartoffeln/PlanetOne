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
        private float TopHeight { get { return 44; } }
        private float BottomHeight { get { return CurrentLayout == InterfaceLayout.OrbitalBridge ? 165 : 72; } }
        private float SideWidth { get { return 320; } }
        private bool SidebarVisible { get { return CurrentLayout == InterfaceLayout.CommandCenter || contextOpen; } }
        private Rect SidebarRect(float width, float height)
        { return new Rect(width - SideWidth - (CurrentLayout == InterfaceLayout.OpenSpace ? 12 : 0), TopHeight + 8, SideWidth, height - TopHeight - BottomHeight - 16); }
        public Rect WorldViewport
        {
            get
            {
                float s = UiScale, left = CurrentLayout == InterfaceLayout.OrbitalBridge ? 200 * s : 0;
                float right = CurrentLayout == InterfaceLayout.CommandCenter ? SideWidth * s : 0;
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
            contextOpen=layout==InterfaceLayout.CommandCenter;
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
            var planet=openPlanetId==0?null:simulation.FindPlanet(openPlanetId);
            int[] stock=new int[6];int power=0;
            foreach(var p in simulation.State.Planets)
            {
                if(p.OwnerId!=PlayerFactionId || (planet!=null && p.Id!=planet.Id))continue;
                power+=p.EnergyProduced-p.EnergyUsed;
                foreach(var building in p.Buildings)if(GameSimulation.BuildingOwner(p,building)==PlayerFactionId)for(int r=0;r<stock.Length;r++)stock[r]+=building.Inventory[r];
            }
            string[] labels={"Energie "+power,"Nahrung "+stock[0],"Metall "+stock[2],"Bauteile "+stock[3],"Kristalle "+stock[4]};
            BuildingKind[] icons={BuildingKind.SolarPlant,BuildingKind.Farm,BuildingKind.Smelter,BuildingKind.Workshop,BuildingKind.CrystalMine};
            for(int i=0;i<labels.Length;i++)
            {
                float x=12+i*158;GUI.DrawTexture(new Rect(x,7,37,29),BuildingIcon(icons[i]),ScaleMode.ScaleToFit);
                GUI.Label(new Rect(x+42,11,116,24),labels[i],bodyStyle);
            }
            float right=width-365;
            if(Button(new Rect(right,7,76,29),simulation.State.Paused?"Weiter":"Pause",simulation.State.Paused))TogglePause();
            if(Button(new Rect(right+80,7,45,29),simulation.State.TimeScale+"×",false))SetSimulationSpeed(simulation.State.TimeScale==1?2:1);
            if(Button(new Rect(right+130,7,132,29),"Einstellungen",false))SetSettingsVisible(true);
            if(Button(new Rect(right+266,7,89,29),"Menü",false)){menuOpen=true;selectionStart=null;shipSelectionStart=null;}
        }
        private void DrawSectionNavigation()
        {
            string[] labels={"Übersicht","Bauen","Einheiten","Flotte","Forschung","Logistik","Diplomatie"};
            GUILayout.BeginHorizontal();
            for(int i=0;i<3;i++)if(GUILayout.Button(labels[i],presentationSection==i?selectedButtonStyle:buttonStyle,GUILayout.Height(29)))OpenPresentationSection(i);
            GUILayout.EndHorizontal();
            for(int row=0;row<2;row++)
            {GUILayout.BeginHorizontal();for(int i=3+row*2;i<5+row*2;i++)if(GUILayout.Button(labels[i],presentationSection==i?selectedButtonStyle:buttonStyle,GUILayout.Height(29)))OpenPresentationSection(i);GUILayout.EndHorizontal();}
            GUILayout.Space(10);
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
            Rect deck=CurrentLayout==InterfaceLayout.OpenSpace?new Rect((width-600)/2,y+4,600,BottomHeight-12):new Rect(0,y,width,BottomHeight);
            DrawPanel(deck);
            if(CurrentLayout==InterfaceLayout.OpenSpace)
            {
                string[] names={"Bauen","Flotte","Forschung","Diplomatie","Details"};int[] sections={1,3,4,6,0};
                for(int i=0;i<5;i++)if(Button(new Rect(deck.x+10+i*116,deck.y+12,110,34),names[i],false))
                {if(i==4)contextOpen=!contextOpen;else OpenPresentationSection(sections[i]);}
                if(Time.unscaledTime<notificationUntil)GUI.Label(new Rect(18,y-32,width-36,28),notification,bodyStyle);
                return;
            }
            if(CurrentLayout==InterfaceLayout.OrbitalBridge)
            {
                DrawRadar(new Rect(16,y+12,175,140));
                GUI.Label(new Rect(210,y+12,400,26),openPlanetId==0?"FLOTTENKOMMANDO":"KOLONIEKOMMANDO",headingStyle);
                var selected=openPlanetId==0?null:FindSelected(simulation.FindPlanet(openPlanetId));
                if(selected!=null)
                {
                    GUI.DrawTexture(new Rect(210,y+42,120,80),BuildingIcon(selected.Kind),ScaleMode.ScaleToFit);
                    GUI.Label(new Rect(340,y+45,280,46),Definitions.Get(selected.Kind).Name+"\n"+selected.Status,bodyStyle);
                }
                else GUI.Label(new Rect(210,y+43,410,46),"Objekt auswählen, um Zustand und Versorgung zu prüfen.",bodyStyle);
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
            if(string.IsNullOrEmpty(message))message="WASD Kamera · Mausrad Zoom · Q/E Drehen · Mittlere Maustaste Neigen · Pos1 Zurücksetzen";
            GUI.Label(new Rect(CurrentLayout==InterfaceLayout.OrbitalBridge?210:32,y+BottomHeight-26,width-(CurrentLayout==InterfaceLayout.OrbitalBridge?240:64),24),message,smallStyle);
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
                for(int ring=1;ring<=4;ring++)for(int i=0;i<48;i++)
                {float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;DrawMapLine(rect.center+new Vector2(Mathf.Cos(a)*rect.width,Mathf.Sin(a)*rect.height)*ring*0.1f,rect.center+new Vector2(Mathf.Cos(b)*rect.width,Mathf.Sin(b)*rect.height)*ring*0.1f,new Color(0.05f,0.3f,0.34f),0.7f);}
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
                var map=new Rect(rect.x+8,rect.y+24,rect.width-16,rect.height-32);
                if(p.OwnerId==PlayerFactionId && reliefMap!=null)
                {
                    GUI.DrawTexture(map,reliefMap);
                    for(int y=0;y<p.Height;y++)for(int x=0;x<p.Width;x++)if(p.Roads[y*p.Width+x])
                        Fill(new Rect(map.x+(x+(y%2)*0.5f)/p.Width*map.width,map.y+(1-y/(float)p.Height)*map.height,3,2),new Color(0.2f,0.47f,0.48f));
                }
                foreach(var b in p.Buildings)
                {
                    // Radar never adds a new source of intelligence about foreign colonies.
                    if(p.OwnerId!=PlayerFactionId)continue;
                    if(GameSimulation.BuildingOwner(p,b)!=PlayerFactionId && !simulation.IsCellVisible(p,PlayerFactionId,b.X,b.Y))continue;
                    Rect dot=new Rect(map.x+(b.X+(b.Y%2)*0.5f)/p.Width*map.width-3,map.y+(1-b.Y/(float)p.Height)*map.height-3,7,7);
                    Fill(dot,DiplomaticColor(GameSimulation.BuildingOwner(p,b)));
                    if(GUI.Button(dot,GUIContent.none,GUIStyle.none)){SelectBuilding(b.Id);surfaceFocus=SurfacePosition(b.X,0,b.Y);UpdateCamera();}
                }
            }
            GUI.Label(new Rect(rect.x+6,rect.y+3,rect.width-12,18),"TAKTISCHE ÜBERSICHT",smallStyle);
        }
        private void DrawMapLine(Vector2 a,Vector2 b,Color color,float width)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)));
            for(int i=0;i<=steps;i++){Vector2 p=Vector2.Lerp(a,b,i/(float)steps);Fill(new Rect(p.x,p.y,Mathf.Max(1,width),Mathf.Max(1,width)),color);}
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
        private GUIStyle panelStyle;
        private void DrawPanel(Rect rect)
        {
            Color tint=CurrentLayout==InterfaceLayout.OrbitalBridge?new Color(0.64f,0.8f,1):new Color(0.78f,0.85f,0.9f);
            Color old=GUI.color;GUI.color=tint;
            if(panelStyle==null){panelStyle=new GUIStyle();panelStyle.normal.background=panelTexture;panelStyle.border=new RectOffset(12,12,12,12);}
            GUI.Box(rect,GUIContent.none,panelStyle);GUI.color=old;
        }
        private void PreparePresentationSkin()
        {
            if(panelTexture==null)
            {
                panelTexture=CreateInterfaceTexture(false);buttonTexture=CreateInterfaceTexture(true);hoverTexture=CreateInterfaceTexture(true);
            }
            buttonStyle.normal.background=buttonTexture;buttonStyle.hover.background=hoverTexture;
            buttonStyle.border=new RectOffset(8,8,8,8);selectedButtonStyle.border=buttonStyle.border;
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
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Metallrahmen",wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                float grain=((x*13+y*71)%17)/1700f;
                int edge=Mathf.Min(Mathf.Min(x,63-x),Mathf.Min(y,63-y));
                float value=0.055f+(64-y)/1600f+grain;
                if(edge<2)value=0.42f;if(edge>=2 && edge<5)value=0.16f;if(edge==5)value=0.30f;
                float alpha=(x+y<6 || (63-x)+y<6 || x+(63-y)<6 || (63-x)+(63-y)<6)?0:1;
                texture.SetPixel(x,y,new Color(value*0.65f,value*0.90f,value*1.12f,alpha));
            }
            texture.Apply();return texture;
        }
    }
}
