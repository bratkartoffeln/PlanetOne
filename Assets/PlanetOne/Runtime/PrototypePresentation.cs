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
        { public int Layout; public bool Grid = true; public float Scale = 1; public float TextScale=1; }
        private PresentationSettings presentation = new PresentationSettings();
        private bool settingsOpen, menuOpen, contextOpen = true;
        private string settingsError;
        private string presentationPath;
        private Texture2D panelTexture, buttonTexture, hoverTexture, bridgePanelTexture, glassPanelTexture;
        private Vector2 settingsScroll;
        private int settingsPage;
        private Vector2 navigationScroll;
        private int presentationSection, buildCategory;
        private GUIStyle buildStyle, selectedBuildStyle;
        private static readonly string[] LayoutNames = { "Kommandozentrale", "Orbitale Brücke", "Freier Weltraum" };
        private static readonly string[] LayoutDescriptions = {
            "Feste rechte Verwaltung, direkte Bauauswahl und kompakte Befehle.",
            "Kolonien links, Objektdetails rechts und ein breites Befehlsdeck unten.",
            "Große Spielfläche, schwebende Werkzeugleiste und ausblendbare Details." };
        public InterfaceLayout CurrentLayout { get { return (InterfaceLayout)presentation.Layout; } }
        private float TopHeight { get { return Mathf.Max(56,FontUnitSize(18)+28); } }
        private float BottomHeight { get { return CurrentLayout==InterfaceLayout.OrbitalBridge?Mathf.Max(190,FontUnitSize(18)*5+88):CurrentLayout==InterfaceLayout.OpenSpace?Mathf.Max(112,FontUnitSize(17)+96):Mathf.Max(86,FontUnitSize(18)*2+40); } }
        private float SideWidth { get { return CurrentLayout==InterfaceLayout.CommandCenter&&!panelOpen?Mathf.Clamp(Screen.width/UiScale*.23f,318,380)+Mathf.Max(0,TextScale-1)*100:Mathf.Clamp(Screen.width/UiScale*.36f,430,570); } }
        private bool SidebarVisible { get { return CurrentLayout == InterfaceLayout.CommandCenter || panelOpen; } }
        private Rect SidebarRect(float width, float height)
        { return new Rect(width - SideWidth - (CurrentLayout == InterfaceLayout.OpenSpace ? 12 : 0), TopHeight + 8, SideWidth, height - TopHeight - BottomHeight - 16); }
        public Rect WorldViewport
        {
            get
            {
                if(!hasSession)return new Rect(0,0,Screen.width,Screen.height);
                float s = UiScale, left = CurrentLayout == InterfaceLayout.OrbitalBridge ? NavigationWidth * s : 0;
                float right = CurrentLayout == InterfaceLayout.CommandCenter ? SideWidth * s : 0;
                return new Rect(left, BottomHeight*s, Mathf.Max(1,Screen.width-left-right), Mathf.Max(1,Screen.height-(TopHeight+BottomHeight)*s));
            }
        }
        public bool IsWorldPoint(Vector2 screenPoint)
        {
            if (viewFlightActive || settingsOpen || menuOpen || IsCommandMenuOpen || commandDismissFrame==Time.frameCount || !WorldViewport.Contains(screenPoint)) return false;
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
                if(float.IsNaN(presentation.TextScale)||float.IsInfinity(presentation.TextScale)||presentation.TextScale==0)presentation.TextScale=1;
                presentation.TextScale=Mathf.Clamp(presentation.TextScale,.9f,1.4f);
                Shader.SetGlobalFloat("_PlanetOneGrid",presentation.Grid?1:0);
            }
            catch (Exception e) { presentation = new PresentationSettings(); settingsError="Einstellungen konnten nicht geladen werden: "+e.Message; }
        }
        public bool SetInterfaceLayout(InterfaceLayout layout, bool persist = true)
        {
            if ((int)layout<0 || (int)layout>2) return false;
            CloseCommandMenu();presentation.Layout=(int)layout; sidebarScroll=Vector2.zero;
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
        public void SetSettingsVisible(bool visible) { CloseCommandMenu();if(visible){settingsReturnToMenu=menuOpen || !hasSession;settingsOpen=true;menuOpen=false;}else CloseSettings();selectionStart=null; shipSelectionStart=null;CancelSpacePan(); }
        public void SetMenuVisible(bool visible) { CloseCommandMenu();menuOpen=visible;if(visible){sessionPage=SessionPage.Home;pendingSessionAction=null;sessionError=null;} selectionStart=null; shipSelectionStart=null;CancelSpacePan(); }
        public void OpenPresentationSection(int section)
        {
            if(section<0 || section>6)throw new ArgumentOutOfRangeException("section");
            CloseCommandMenu();CancelPlacement();
            presentationSection=section;
            contextOpen=true;panelOpen=true;showingDetails=false;showingDiplomacy=false;showingSpacePanel=false;showingMilitary=false;
            spaceMoveTargetMode=false;militaryTargetMode=null;
            sidebarScroll=Vector2.zero;
            if(section==2) showingMilitary=true;
            if(section>=3 && section<=5) { showingSpacePanel=true; spacePanelTab=section==3?0:section==4?1:2; }
            if(section==6) showingDiplomacy=true;
            UpdateCamera();
        }
        private void DrawRadar(Rect rect)
        {
            Fill(rect,new Color(0.015f,0.035f,0.05f));
            string caption=openPlanetId==0?"SYSTEMKARTE":"KOLONIEKARTE";
            float captionHeight=smallStyle.CalcHeight(new GUIContent(caption),rect.width-12);
            var map=new Rect(rect.x+8,rect.y+captionHeight+9,rect.width-16,Mathf.Max(12,rect.height-captionHeight-17));
            if(openPlanetId==0)
            {
                float extent=1;
                foreach(var p in simulation.State.Planets)if(simulation.IsPlanetKnown(PlayerFactionId,p.Id))extent=Mathf.Max(extent,(float)p.OrbitRadius+3);
                for(int ring=1;ring<=4;ring++)for(int i=0;i<48;i++)
                {float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;DrawMapLine(map.center+new Vector2(Mathf.Cos(a)*map.width,Mathf.Sin(a)*map.height)*ring*0.1f,map.center+new Vector2(Mathf.Cos(b)*map.width,Mathf.Sin(b)*map.height)*ring*0.1f,new Color(0.05f,0.3f,0.34f),0.7f);}
                foreach(var p in simulation.State.Planets)
                {
                    if(!simulation.IsPlanetKnown(PlayerFactionId,p.Id))continue;
                    var pos=simulation.GetOrbitPosition(p.Id);
                    Rect dot=new Rect(map.center.x+(float)pos.X/extent*map.width*0.42f-5,map.center.y-(float)pos.Y/extent*map.height*0.42f-5,10,10);
                    if(GUI.Button(dot,GUIContent.none,buttonStyle))SelectPlanet(p.Id);
                    Fill(new Rect(dot.x+2,dot.y+2,6,6),p.Id==selectedPlanetId?Color.white:Cyan);
                }
                Fill(new Rect(map.center.x-3,map.center.y-3,6,6),Amber);
            }
            else
            {
                var p=simulation.FindPlanet(openPlanetId);
                if(p.OwnerId==PlayerFactionId && reliefMap!=null)
                {
                    GUI.DrawTexture(map,reliefMap);
                    for(int y=0;y<p.Height;y++)for(int x=0;x<p.Width;x++)if(supplyOverlay && p.Roads[y*p.Width+x])
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
            GUI.Label(new Rect(rect.x+6,rect.y+3,rect.width-12,captionHeight),caption,smallStyle);
        }
        private void DrawMapLine(Vector2 a,Vector2 b,Color color,float width)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)));
            for(int i=0;i<=steps;i++){Vector2 p=Vector2.Lerp(a,b,i/(float)steps);Fill(new Rect(p.x,p.y,Mathf.Max(1,width),Mathf.Max(1,width)),color);}
        }
        private void DrawPresentationDialogs(float width,float height)
        {
            if(!settingsOpen && !menuOpen)return;
            Fill(new Rect(0,0,width,height),new Color(0,0,0,.72f));
            float dialogWidth=Mathf.Min(1000,width-48),dialogHeight=Mathf.Min(760,height-48);
            Rect box=new Rect((width-dialogWidth)/2,(height-dialogHeight)/2,dialogWidth,dialogHeight);DrawPanel(box);
            GUILayout.BeginArea(new Rect(box.x+28,box.y+28,box.width-56,box.height-56));
            if(settingsOpen)DrawReadableSettings();else DrawSessionMenu();
            GUILayout.EndArea();
        }
        private void DrawReadableSettings()
        {
            GUILayout.Label("EINSTELLUNGEN",titleStyle);GUILayout.Space(16);
            settingsPage=GUILayout.SelectionGrid(settingsPage,new[]{"Layout", "Schrift und Größe", "Anzeige"},3,buttonStyle,GUILayout.MinHeight(UiRowHeight(44)));
            GUILayout.Space(16);settingsScroll=GUILayout.BeginScrollView(settingsScroll);
            if(settingsPage==0)
            {
                for(int i=0;i<3;i++)
                {
                    GUILayout.BeginVertical(GUI.skin.box);GUILayout.Space(8);
                    if(GUILayout.Button(((char)('A'+i))+"   "+LayoutNames[i],presentation.Layout==i?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight(48))))SetInterfaceLayout((InterfaceLayout)i);
                    GUILayout.Label(LayoutDescriptions[i],bodyStyle);GUILayout.Space(8);GUILayout.EndVertical();GUILayout.Space(12);
                }
            }
            else if(settingsPage==1)
            {
                GUILayout.Label("Schriftgröße",headingStyle);
                GUILayout.Label("Texte unabhängig von der Oberfläche vergrößern. Die Vorschau wird sofort aktualisiert.",bodyStyle);GUILayout.Space(12);
                GUILayout.Label(Mathf.RoundToInt(TextScale*100)+" %",headingStyle);
                float next=Mathf.Round(GUILayout.HorizontalSlider(TextScale,.9f,1.4f)*20)/20;
                if(!Mathf.Approximately(next,TextScale))SetTextScale(next);
                GUILayout.BeginHorizontal();foreach(float factor in new[]{1f,1.15f,1.3f,1.4f})if(GUILayout.Button(Mathf.RoundToInt(factor*100)+" %",Mathf.Approximately(TextScale,factor)?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight())))SetTextScale(factor);GUILayout.EndHorizontal();
                GUILayout.Space(22);GUILayout.Label("ASTER · Kolonie bereit",headingStyle);GUILayout.Label("Nahrung und Energie versorgen deine Kolonie. Wähle ein Gebäude, um Details und passende Befehle zu sehen.",bodyStyle);GUILayout.Label("Hinweise, Bestände und Statusmeldungen bleiben ebenfalls lesbar.",smallStyle);
                GUILayout.Space(28);GUILayout.Label("Oberflächengröße",headingStyle);GUILayout.BeginHorizontal();
                float[] scales={.85f,1,1.2f};string[] labels={"Kompakt","Normal","Groß"};
                for(int i=0;i<scales.Length;i++)if(GUILayout.Button(labels[i],Mathf.Approximately(presentation.Scale,scales[i])?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight()))) {presentation.Scale=scales[i];UpdateCamera();SavePresentationSettings();}
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("WELTDARSTELLUNG",headingStyle);GUILayout.Space(18);
                bool grid=GUILayout.Toggle(presentation.Grid," Wabengrenzen anzeigen",GUILayout.MinHeight(UiRowHeight()));
                if(grid!=presentation.Grid){presentation.Grid=grid;Shader.SetGlobalFloat("_PlanetOneGrid",grid?1:0);SavePresentationSettings();}
                GUILayout.Space(20);GUILayout.Label("Kamera: rechts ziehen zum Verschieben, Q/E oder mittlere Maustaste zum Drehen. Mausrad zum Zoomen.",bodyStyle);
            }
            GUILayout.EndScrollView();if(settingsError!=null)GUILayout.Label(settingsError,bodyStyle);GUILayout.Space(12);
            if(GUILayout.Button("Zurück",buttonStyle,GUILayout.MinHeight(UiRowHeight(44))))CloseSettings();
        }
        private GUIStyle panelStyle;
        private void DrawPanel(Rect rect)
        {
            if(panelStyle==null)panelStyle=new GUIStyle();
            bool bridge=CurrentLayout==InterfaceLayout.OrbitalBridge,glass=CurrentLayout==InterfaceLayout.OpenSpace;
            Texture2D background=bridge?bridgePanelTexture:glass?glassPanelTexture:panelTexture;
            int border=bridge?18:14;ApplyChromeStates(panelStyle,background,background,border);
            GUI.Box(rect,GUIContent.none,panelStyle);
            if(bridge && rect.width>130)
            {
                for(int i=0;i<3;i++)DrawMapLine(new Vector2(rect.x+14+i*12,rect.y+14),new Vector2(rect.x+25+i*12,rect.y+3),i==1?Cyan:new Color(.25f,.48f,.64f),3);
                Fill(new Rect(rect.x+62,rect.y+4,Mathf.Min(rect.width-92,96),2),new Color(.23f,.44f,.59f));
            }
        }
        private void PreparePresentationSkin()
        {
            if(panelTexture==null)
            {
                panelTexture=CreateInterfaceTexture(0);buttonTexture=CreateInterfaceTexture(1);hoverTexture=CreateInterfaceTexture(2);bridgePanelTexture=CreateInterfaceTexture(3);glassPanelTexture=CreateInterfaceTexture(4);
            }
            ApplyChromeStates(buttonStyle,buttonTexture,hoverTexture,10);
            ApplyChromeStates(selectedButtonStyle,hoverTexture,hoverTexture,10);
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
            ApplyChromeStates(buildStyle,buttonTexture,hoverTexture,10);
            ApplyChromeStates(selectedBuildStyle,hoverTexture,hoverTexture,10);
            ApplyChromeStates(treeStyle,null,hoverTexture,10);
            GUI.skin.button=buttonStyle;GUI.skin.label=bodyStyle;
            GUI.skin.toggle.fontSize=14;GUI.skin.toggle.normal.textColor=Ink;
            GUI.skin.textField.fontSize=13;ApplyChromeStates(GUI.skin.textField,buttonTexture,hoverTexture,10);
            GUI.skin.textField.normal.textColor=Ink;GUI.skin.textField.focused.textColor=Color.white;
            ApplyChromeStates(GUI.skin.verticalScrollbar,panelTexture,panelTexture,14);
            ApplyChromeStates(GUI.skin.verticalScrollbarThumb,buttonTexture,hoverTexture,10);
            ApplyChromeStates(GUI.skin.horizontalScrollbar,panelTexture,panelTexture,14);
            ApplyChromeStates(GUI.skin.horizontalScrollbarThumb,buttonTexture,hoverTexture,10);
            GUI.skin.box=panelStyle??GUI.skin.box;ApplyTypography();
        }
        private static void ApplyChromeStates(GUIStyle style,Texture2D normal,Texture2D active,int border)
        {
            style.border=new RectOffset(border,border,border,border);
            style.normal.background=normal;style.hover.background=active;style.active.background=active;style.focused.background=active;
            style.onNormal.background=active;style.onHover.background=active;style.onActive.background=active;style.onFocused.background=active;
        }
        private static Texture2D CreateInterfaceTexture(int kind)
        {
            const int size=128;bool bridge=kind==3,glass=kind==4,button=kind==1||kind==2;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="HUD frame "+kind,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float radius=button?8:bridge?14:10,half=size*.5f;
                var position=new Vector2(x+.5f-half,y+.5f-half);
                var q=new Vector2(Mathf.Abs(position.x),Mathf.Abs(position.y))-Vector2.one*(half-radius);
                var outer=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0));
                // One rounded contour governs both transparency and every nested frame band.
                float edge=radius-outer.magnitude-Mathf.Min(Mathf.Max(q.x,q.y),0);
                Vector2 normal=outer.sqrMagnitude>0?outer.normalized:q.x>q.y?Vector2.right:Vector2.up;
                normal=new Vector2(normal.x*Mathf.Sign(position.x),normal.y*Mathf.Sign(position.y));
                float lighting=.5f+.5f*Vector2.Dot(normal,new Vector2(-.7071068f,.7071068f));
                float grain=((x*13+y*71)%17)/1700f;
                float t=y/(float)(size-1),v=(button?.065f:.025f)+t*(button?.065f:.028f)+grain;
                Color color=new Color(v*.65f,v*.88f,v*1.1f,1);
                int rim=button?4:bridge?12:glass?3:8;
                if(edge<rim)
                {
                    float metal=edge<1?.12f:edge<2?.48f:edge<3?.21f:edge<rim-2?.15f+lighting*.1f:edge<rim-1?.04f:.26f+lighting*.1f;
                    color=bridge?new Color(metal*.58f,metal*.9f,metal*1.26f,1):new Color(metal*.85f,metal,metal*1.08f,1);
                    if(glass || kind==2)
                    {
                        bool highlight=edge>=1 && edge<2;
                        float light=.88f+lighting*.12f;
                        color=new Color(.1f,(highlight?.7f:.23f)*light,(highlight?.8f:.3f)*light,1);
                    }
                }
                if(kind==2 && edge>=rim)color=new Color(v*.52f,v*1.5f,v*1.8f,1);
                color.a=Mathf.Clamp01(edge+.5f);texture.SetPixel(x,y,color);
            }
            texture.Apply();return texture;
        }
    }
}
