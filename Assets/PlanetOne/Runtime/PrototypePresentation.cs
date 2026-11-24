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
        private float BottomHeight { get { return CurrentLayout == InterfaceLayout.OrbitalBridge ? 160 : CurrentLayout==InterfaceLayout.OpenSpace?88:72; } }
        private float SideWidth { get { return CurrentLayout==InterfaceLayout.CommandCenter?300:380; } }
        private bool SidebarVisible { get { return CurrentLayout == InterfaceLayout.CommandCenter || panelOpen; } }
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
            if(SidebarVisible && SidebarRect(Screen.width/UiScale,Screen.height/UiScale).Contains(guiPoint))return false;
            return !SelectionCardVisible || !SelectionCardRect(Screen.width/UiScale).Contains(guiPoint);
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
            contextOpen=true;panelOpen=false;
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
        public void SetMenuVisible(bool visible) { menuOpen=visible; selectionStart=null; shipSelectionStart=null; }
        public void OpenPresentationSection(int section)
        {
            if(section<0 || section>6)throw new ArgumentOutOfRangeException("section");
            presentationSection=section;
            contextOpen=true;panelOpen=true;showingDetails=false;showingDiplomacy=false;showingSpacePanel=false;showingMilitary=false;
            spaceMoveTargetMode=false;militaryTargetMode=null;
            sidebarScroll=Vector2.zero;
            if(section==2) showingMilitary=true;
            if(section>=3 && section<=5) { showingSpacePanel=true; spacePanelTab=section==3?0:section==4?1:2; }
            if(section==6) showingDiplomacy=true;
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
                    if(GUI.Button(dot,GUIContent.none,buttonStyle))SelectPlanet(p.Id);
                    Fill(new Rect(dot.x+2,dot.y+2,6,6),p.Id==selectedPlanetId?Color.white:Cyan);
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
            float dialogHeight=settingsOpen?540:650;
            Rect box=new Rect((width-660)/2,(height-dialogHeight)/2,660,dialogHeight);DrawPanel(box);
            GUILayout.BeginArea(new Rect(box.x+26,box.y+22,608,dialogHeight-44));
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
                if(priorSession!=null && GUILayout.Button("Zur vorherigen Partie zurück",buttonStyle,GUILayout.Height(34)))
                {simulation=priorSession;priorSession=null;foodRates.Clear();BuildSpace();SetView(0);menuOpen=false;}
                GUILayout.Space(22);GUILayout.Label("NEUE PARTIE · laufendes Spiel vorher speichern",smallStyle);
                if(GUILayout.Button("Diplomatische Partie",buttonStyle,GUILayout.Height(40))){StartDiplomacyScenario();menuOpen=false;}
                if(GUILayout.Button("Gegen Computer spielen",buttonStyle,GUILayout.Height(40))){StartAiScenario();menuOpen=false;}
                GUILayout.Space(12);GUILayout.Label("TESTSZENARIEN · separate Sitzung",smallStyle);
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Aufbau / Raumfahrt",buttonStyle,GUILayout.Height(32))){StartSpaceFixture();menuOpen=false;}
                if(GUILayout.Button("Bodengefecht",buttonStyle,GUILayout.Height(32))){StartMilitaryFixture();menuOpen=false;}
                GUILayout.EndHorizontal();GUILayout.BeginHorizontal();
                if(GUILayout.Button("Invasion",buttonStyle,GUILayout.Height(32))){StartInvasionFixture();menuOpen=false;}
                if(GUILayout.Button("Verträge / Sprungtor",buttonStyle,GUILayout.Height(32))){StartDiplomacyFixture();menuOpen=false;}
                GUILayout.EndHorizontal();
                GUILayout.Space(10);if(GUILayout.Button("Spiel beenden",buttonStyle,GUILayout.Height(32)))Application.Quit();
            }
            GUILayout.EndArea();
        }
        private GUIStyle panelStyle;
        private void DrawPanel(Rect rect)
        {
            Color tint=CurrentLayout==InterfaceLayout.OrbitalBridge?new Color(0.72f,0.86f,1):Color.white;
            Color old=GUI.color;GUI.color=tint;
            if(panelStyle==null){panelStyle=new GUIStyle();panelStyle.normal.background=panelTexture;panelStyle.border=new RectOffset(8,8,8,8);}
            GUI.Box(rect,GUIContent.none,panelStyle);GUI.color=old;
        }
        private void PreparePresentationSkin()
        {
            if(panelTexture==null)
            {
                panelTexture=CreateInterfaceTexture(0);buttonTexture=CreateInterfaceTexture(1);hoverTexture=CreateInterfaceTexture(2);
            }
            buttonStyle.normal.background=buttonTexture;buttonStyle.hover.background=hoverTexture;
            buttonStyle.border=new RectOffset(8,8,8,8);selectedButtonStyle.border=buttonStyle.border;
            buttonStyle.normal.textColor=Ink;buttonStyle.hover.textColor=Color.white;
            buttonStyle.active.background=hoverTexture;buttonStyle.active.textColor=Cyan;
            buttonStyle.onNormal.background=hoverTexture;buttonStyle.onNormal.textColor=Cyan;buttonStyle.onHover.background=hoverTexture;buttonStyle.onHover.textColor=Color.white;
            selectedButtonStyle.normal.background=hoverTexture;
            selectedButtonStyle.normal.textColor=Cyan;
            if(buildStyle==null)
            {
                buildStyle=new GUIStyle(buttonStyle){imagePosition=ImagePosition.ImageAbove,alignment=TextAnchor.MiddleCenter,fontSize=12,fixedWidth=120,padding=new RectOffset(5,5,6,6)};
                selectedBuildStyle=new GUIStyle(buildStyle);selectedBuildStyle.normal.textColor=Cyan;selectedBuildStyle.normal.background=hoverTexture;
                centerStyle=new GUIStyle(bodyStyle){alignment=TextAnchor.MiddleCenter,fontSize=12};
                treeStyle=new GUIStyle(buttonStyle){imagePosition=ImagePosition.ImageLeft,padding=new RectOffset(7,4,4,4),fontSize=12};treeStyle.normal.background=null;
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
        private static Texture2D CreateInterfaceTexture(int kind)
        {
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Metallrahmen",wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                float grain=((x*13+y*71)%17)/5500f;
                int edge=Mathf.Min(Mathf.Min(x,63-x),Mathf.Min(y,63-y));
                float value=(kind==0?0.032f:kind==1?0.045f:0.08f)+(64-y)/2400f+grain;
                if(edge==0)value=kind==2?0.45f:0.25f;if(edge==1)value=0.045f;
                float alpha=(x+y<6 || (63-x)+y<6 || x+(63-y)<6 || (63-x)+(63-y)<6)?0:1;
                texture.SetPixel(x,y,new Color(value*(kind==2?0.35f:0.64f),value*0.93f,value*1.14f,alpha));
            }
            texture.Apply();return texture;
        }
    }
}
