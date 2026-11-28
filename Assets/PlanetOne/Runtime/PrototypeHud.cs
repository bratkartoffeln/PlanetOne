using System;
using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private bool panelOpen,spaceMoveTargetMode,suppressShipSelectionUntilUp;
        private GUIStyle centerStyle,treeStyle;
        private readonly Dictionary<string,Texture2D> objectPortraits=new Dictionary<string,Texture2D>();
        private static readonly string[] SectionNames={"Übersicht","Bauen","Bodentruppen","Flotte","Forschung","Versorgung","Diplomatie"};
        private static readonly string[] BuildTabNames={"Basis","Industrie","Militär"};
        private static readonly int[] SidebarSections={1,3,4,6};
        private static readonly string[] SidebarTabNames={SectionNames[1],SectionNames[3],SectionNames[4],SectionNames[6]};
        private GUIStyle tabLabel;
        private float[] tabWidths=Array.Empty<float>();

        private int TabColumns(string[] labels,float width)
        {
            if(tabLabel==null)
            {
                tabLabel=new GUIStyle(GUIStyle.none){alignment=TextAnchor.MiddleCenter};
                tabLabel.hover.textColor=tabLabel.active.textColor=tabLabel.focused.textColor=Ink;
            }
            tabLabel.fontSize=FontUnitSize(16);
            if(tabWidths.Length<labels.Length)tabWidths=new float[labels.Length];
            float total=0,widest=1;
            for(int i=0;i<labels.Length;i++)
            {
                tabWidths[i]=tabLabel.CalcSize(new GUIContent(labels[i])).x+14;
                total+=tabWidths[i];
                widest=Mathf.Max(widest,tabWidths[i]);
            }
            if(total<=width)return labels.Length;
            int columns=Mathf.Max(1,Mathf.FloorToInt(width/widest));
            int rows=Mathf.CeilToInt((float)labels.Length/columns);
            return Mathf.CeilToInt((float)labels.Length/rows);
        }

        private int DrawTabStrip(Rect rect,int selected,string[] labels,int columns)
        {
            float h=UiRowHeight();
            float x=rect.x,extra=0;
            for(int i=0;i<labels.Length;i++)
            {
                int row=i/columns;
                if(i%columns==0)
                {
                    int count=Mathf.Min(columns,labels.Length-i);
                    float used=0;
                    for(int j=i;j<i+count;j++)used+=tabWidths[j];
                    extra=(rect.width-used)/count;
                    x=rect.x;
                }
                var cell=new Rect(x,rect.y+row*h,tabWidths[i]+extra,h);
                x=cell.xMax;
                bool active=i==selected;
                bool hover=GUI.enabled && cell.Contains(Event.current.mousePosition);
                var face=new Rect(cell.x+1,cell.y+(active?0:4),cell.width-2,cell.height-(active?0:4));
                Fill(face,active?new Color(.085f,.20f,.23f):hover?new Color(.12f,.17f,.20f):new Color(.065f,.09f,.11f));
                Fill(new Rect(cell.x,cell.yMax-2,cell.width,2),active?Cyan:Border);
                tabLabel.normal.textColor=active?Ink:hover?Color.white:Muted;
                if(GUI.Button(face,labels[i],tabLabel))selected=i;
            }
            return selected;
        }

        private int DrawWorkspaceTabs(int selected,string[] labels,float width=0)
        {
            int columns=TabColumns(labels,width>0?width:SidebarContentWidth);
            float h=UiRowHeight()*Mathf.CeilToInt((float)labels.Length/columns);
            var rect=GUILayoutUtility.GetRect(1,h,GUILayout.ExpandWidth(true));
            return DrawTabStrip(rect,selected,labels,columns);
        }

        private int DrawSidebarTabs(Rect area,ref float top,int selected,string[] labels)
        {
            float width=area.width-24;
            int columns=TabColumns(labels,width);
            float h=UiRowHeight()*Mathf.CeilToInt((float)labels.Length/columns);
            var rect=new Rect(area.x+12,top,width,h);
            top+=h+6;
            return DrawTabStrip(rect,selected,labels,columns);
        }
        private bool HasSelection {get{return openPlanetId==0?StarSelected || selectedShips.Count>0 || simulation.FindPlanet(selectedPlanetId)!=null:selectedBuildingId!=0 || selectedUnits.Count>0;}}
        private bool SelectionCardVisible {get{return !viewFlightActive && CurrentLayout!=InterfaceLayout.CommandCenter && !(CurrentLayout==InterfaceLayout.OrbitalBridge&&(selectedUnits.Count>0||selectedShips.Count>0)) && contextOpen && !panelOpen && HasSelection;}}
        private float SelectionSummaryHeight {get{return 132+FontUnitSize(18)*4;}}
        private Rect SelectionCardRect(float width){float w=Mathf.Min(430,346+(TextScale-1)*140);return new Rect(width-w-18,TopHeight+22,w,SelectionSummaryHeight+UiRowHeight()*2+50);}
        public bool IsContextCardOpen {get{return SelectionCardVisible;}}
        public Rect ContextCardScreenRect {get{var r=SelectionCardRect(Screen.width/UiScale);return new Rect(r.x*UiScale,Screen.height-r.yMax*UiScale,r.width*UiScale,r.height*UiScale);}}
        public void CloseContextPanel(){panelOpen=false;contextOpen=false;UpdateCamera();}

        private void DrawPresentationTop(float width)
        {
            DrawPanel(new Rect(0,0,width,TopHeight));
            float control=UiRowHeight(),top=(TopHeight-control)/2;
            string place=openPlanetId==0?"Sonnensystem":simulation.FindPlanet(openPlanetId).Name;
            if(openPlanetId!=0 && HudButton(new Rect(8,top,38,control),"",HudIcon.Back))BeginViewFlight(0);
            float locationX=openPlanetId==0?8:50;
            float locationWidth=Mathf.Clamp(locationX+bodyStyle.CalcSize(new GUIContent(place)).x+50,190,width*.24f);
            if(HudButton(new Rect(locationX,top,locationWidth-locationX,control),place,HudIcon.Planet))OpenPresentationSection(0);
            int[] stock=new int[6];int power=0;
            foreach(var planet in simulation.State.Planets)
            {
                if(planet.OwnerId!=PlayerFactionId || openPlanetId!=0 && planet.Id!=openPlanetId)continue;
                power+=planet.EnergyProduced-planet.EnergyUsed;
                foreach(var building in planet.Buildings)for(int r=0;r<6;r++)stock[r]+=GameSimulation.FreeSupplyStock(planet,PlayerFactionId,building,r);
            }
            string[] names={"Energie","Metall","Bauteile","Nahrung","Kristalle"};
            string[] values={(power>0?"+":"")+power,stock[2].ToString(),stock[3].ToString(),stock[0].ToString(),stock[4].ToString()};
            HudIcon[] icons={HudIcon.Energy,HudIcon.Metal,HudIcon.Parts,HudIcon.Food,HudIcon.Crystal};
            string pauseCaption=simulation.State.Paused?"Weiter":"Pause",speedCaption=simulation.State.TimeScale+"×";
            float pauseWidth=Mathf.Max(108,bodyStyle.CalcSize(new GUIContent(pauseCaption)).x+50);
            float speedWidth=Mathf.Max(48,buttonStyle.CalcSize(new GUIContent(speedCaption)).x+12);
            float right=width-(pauseWidth+speedWidth+40+40+18)-8;
            var labels=new string[5];var cells=new float[5];float needed=0,available=right-locationWidth-22;
            for(int i=0;i<5;i++){labels[i]=names[i]+" "+values[i];cells[i]=bodyStyle.CalcSize(new GUIContent(labels[i])).x+44;needed+=cells[i];}
            // Keep the selected font size; the symbol identifies any resource whose long name cannot fit.
            while(needed>available)
            {
                int best=-1;float saving=0;
                for(int i=0;i<5;i++)if(labels[i]!=values[i]){float delta=cells[i]-(bodyStyle.CalcSize(new GUIContent(values[i])).x+44);if(delta>saving){best=i;saving=delta;}}
                if(best<0)break;labels[best]=values[best];cells[best]-=saving;needed-=saving;
            }
            float extra=Mathf.Max(0,(available-needed)/5),resourceX=locationWidth+12;
            for(int i=0;i<5;i++)
            {
                float x=resourceX,cell=cells[i]+extra;Color old=GUI.color;GUI.color=i==0?Amber:i==4?Cyan:Ink;
                GUI.DrawTexture(new Rect(x,TopHeight/2-12,24,24),HudSymbol(icons[i]));GUI.color=old;
                LabelFitted(new Rect(x+30,TopHeight/2-TextLineHeight/2,cell-38,TextLineHeight),labels[i],bodyStyle);resourceX+=cell;
            }
            if(HudButton(new Rect(right,top,pauseWidth,control),pauseCaption,simulation.State.Paused?HudIcon.Play:HudIcon.Pause,simulation.State.Paused))TogglePause();
            float speedX=right+pauseWidth+6;
            if(Button(new Rect(speedX,top,speedWidth,control),speedCaption,false))SetSimulationSpeed(simulation.State.TimeScale==1?2:1);
            if(HudButton(new Rect(speedX+speedWidth+6,top,40,control),"",HudIcon.Settings))SetSettingsVisible(true);
            if(HudButton(new Rect(speedX+speedWidth+52,top,40,control),"",HudIcon.Menu))SetMenuVisible(true);
        }

        private void DrawSidebar(float width,float height)
        {
            if(!SidebarVisible)return;
            Rect area=SidebarRect(width,height);DrawPanel(area);
            bool fixedBar=CurrentLayout==InterfaceLayout.CommandCenter;
            float top=area.y+12;
            GUI.Label(new Rect(area.x+16,top,area.width-62,FontUnitSize(20)+12),panelOpen?SectionNames[presentationSection].ToUpperInvariant():openPlanetId==0?"SONNENSYSTEM":simulation.FindPlanet(openPlanetId).Name.ToUpperInvariant(),headingStyle);
            if(panelOpen && HudButton(new Rect(area.xMax-42,top-4,30,30),"",HudIcon.Close,false,"Zur Auswahl zurück")){panelOpen=false;contextOpen=true;UpdateCamera();}
            top+=FontUnitSize(20)+22;
            bool buildPanel=openPlanetId!=0 && simulation.FindPlanet(openPlanetId).OwnerId==PlayerFactionId &&
                ((!panelOpen && fixedBar) || (panelOpen && presentationSection==1 && !showingDetails && !showingMilitary && !showingDiplomacy && !showingSpacePanel));
            if(fixedBar)
            {
                int selected=buildPanel?0:panelOpen && !showingDetails?Array.IndexOf(SidebarSections,presentationSection):-1;
                int next=DrawSidebarTabs(area,ref top,selected,SidebarTabNames);
                if(next!=selected){OpenPresentationSection(SidebarSections[next]);GUIUtility.ExitGUI();}
            }
            if(buildPanel)
            {
                int next=DrawSidebarTabs(area,ref top,buildCategory,BuildTabNames);
                if(next!=buildCategory)SelectBuildCategory(next);
            }
            Rect content=new Rect(area.x+14,top,area.width-28,area.yMax-top-12);
            productionPointerInContent=GUI.enabled && content.Contains(Event.current.mousePosition) &&
                Event.current.mousePosition.x < content.xMax-GUI.skin.verticalScrollbar.fixedWidth;
            GUILayout.BeginArea(content);
            sidebarContentWidth=content.width-GUI.skin.verticalScrollbar.fixedWidth-8;
            sidebarScroll.x=0;
            sidebarScroll=GUILayout.BeginScrollView(sidebarScroll,false,true,GUIStyle.none,GUI.skin.verticalScrollbar);
            GUILayout.BeginVertical(GUILayout.Width(sidebarContentWidth));
            DrawHudContent();
            GUILayout.EndVertical();GUILayout.EndScrollView();GUILayout.EndArea();
        }

        internal void SelectBuildCategory(int category)
        {
            buildCategory=category;sidebarScroll=Vector2.zero;
        }
        private void DrawHudContent()
        {
            if(!panelOpen)
            {
                if(CurrentLayout==InterfaceLayout.CommandCenter && openPlanetId!=0 && simulation.FindPlanet(openPlanetId).OwnerId==PlayerFactionId){DrawBuildTools();return;}
                DrawSelectionSummary(new Rect(0,0,SideWidth-60,SelectionSummaryHeight),false);GUILayout.Space(SelectionSummaryHeight+12);
                if(GUILayout.Button("Objektbefehle öffnen",selectedButtonStyle,GUILayout.MinHeight(UiRowHeight(38))))OpenSelectionCommandMenu(Input.mousePosition);
                GUILayout.Label("Rechtsklick: bewegen / angreifen\nStrg + Rechtsklick: weitere Befehle\nRechts ziehen: Ansicht verschieben",smallStyle);
                if(openPlanetId==0 && selectedShips.Count==0 && !StarSelected)
                {
                    GUILayout.Space(18);GUILayout.Label("BEKANNTE WELTEN",smallStyle);
                    foreach(var world in simulation.State.Planets)
                        if(simulation.IsPlanetKnown(PlayerFactionId,world.Id) && GUILayout.Button(world.Name,world.Id==selectedPlanetId?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight(32))))NavigateToPlanet(world.Id);
                }
                return;
            }
            if(showingDiplomacy){DrawDiplomacyPanel();return;}
            if(showingSpacePanel && panelOpen){DrawSpacePanel();return;}
            if(openPlanetId==0)
            {
                if(StarSelected){DrawStarDetails();return;}
                if(selectedShips.Count>0 && !panelOpen){DrawSelectionSummary(new Rect(0,0,252,190),false);GUILayout.Space(195);if(GUILayout.Button("Flotte verwalten",buttonStyle,GUILayout.MinHeight(UiRowHeight(34))))OpenPresentationSection(3);return;}
                var p=simulation.FindPlanet(selectedPlanetId);
                if(p!=null && simulation.IsPlanetKnown(PlayerFactionId,p.Id))
                {
                    GUILayout.Label(p.Name.ToUpperInvariant(),headingStyle);
                    Rect portrait=GUILayoutUtility.GetRect(160,76);GUI.DrawTexture(portrait,PlanetPortrait(p),ScaleMode.ScaleToFit);
                    GUILayout.Label(PlanetEnvironment.Name(p.Kind)+(p.ParentId>0?" · Mond":""),mutedStyle);
                    GUILayout.Label(PlanetEnvironment.StellarAu(simulation.State,p).ToString("0.00")+" AE · "+PlanetEnvironment.RadiusEarths(p.Kind).ToString("0.00")+" Erdradien",smallStyle);
                    int owner=KnownPlanetOwner(p);GUILayout.Label(owner==PlayerFactionId?"Eigene Kolonie":owner==0?"Unbesiedelt":owner<0?"Besitz unbekannt":"Fremde Kolonie",bodyStyle);
                    if(owner==PlayerFactionId)GUILayout.Label(p.Population+" Bewohner · "+p.Buildings.Count+" Gebäude",smallStyle);
                    GUILayout.Space(6);if(GUILayout.Button(PlanetEnvironment.Airborne(p)?"Atmosphäre öffnen  →":"Oberfläche öffnen  →",selectedButtonStyle,GUILayout.MinHeight(UiRowHeight(38))))OpenSelectedPlanet();
                }
                GUILayout.Space(18);GUILayout.Label("BEKANNTE WELTEN",smallStyle);
                foreach(var world in simulation.State.Planets)
                    if(simulation.IsPlanetKnown(PlayerFactionId,world.Id) && GUILayout.Button(world.Name,world.Id==selectedPlanetId?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight(31))))NavigateToPlanet(world.Id);
                return;
            }
            var planet=simulation.FindPlanet(openPlanetId);
            if(showingMilitary){DrawMilitaryPanel(planet);return;}
            if(showingDetails)
            {
                var b=FindSelected(planet);
                if(b!=null){if(b.Kind!=BuildingKind.VehicleFactory && b.Kind!=BuildingKind.Barracks && b.Kind!=BuildingKind.Spaceport){Rect image=GUILayoutUtility.GetRect(160,92);GUI.DrawTexture(image,BuildingIcon(b.Kind),ScaleMode.ScaleToFit);}DrawBuildingDetails(b); }
                else GUILayout.Label("Ein Gebäude auswählen, um seinen Betrieb zu verwalten.",bodyStyle);
                return;
            }
            if(presentationSection==0)
            {
                DrawColonySummary(planet);GUILayout.Space(12);
                if(GUILayout.Button("Bodentruppen verwalten",buttonStyle,GUILayout.MinHeight(UiRowHeight(34))))OpenPresentationSection(2);
                if(GUILayout.Button("Versorgung verwalten",buttonStyle,GUILayout.MinHeight(UiRowHeight(34))))OpenPresentationSection(5);
                return;
            }
            if(planet.OwnerId==PlayerFactionId){DrawBuildTools();}
            else {GUILayout.Label("Fremde Oberfläche",headingStyle);GUILayout.Label("Eigene Truppen können hier über ihre Auswahl befehligt werden.",bodyStyle);}
        }

        private void DrawPresentationNavigation(float width,float height)
        {
            if(CurrentLayout!=InterfaceLayout.OrbitalBridge)return;
            DrawPanel(new Rect(0,TopHeight,NavigationWidth,height-TopHeight-BottomHeight));
            GUILayout.BeginArea(new Rect(16,TopHeight+22,NavigationWidth-32,height-TopHeight-BottomHeight-44));
            navigationScroll=GUILayout.BeginScrollView(navigationScroll,false,false);
            if(GUILayout.Button("← Sonnensystem",treeStyle,GUILayout.MinHeight(UiRowHeight(30))))BeginViewFlight(0);
            GUILayout.Space(12);GUILayout.Label("KOLONIEN",headingStyle);
            foreach(var p in simulation.State.Planets)
            {
                if(!simulation.IsPlanetKnown(PlayerFactionId,p.Id))continue;
                if(GUILayout.Button(new GUIContent(p.Name,HudSymbol(HudIcon.Planet)),(openPlanetId==p.Id || openPlanetId==0 && selectedPlanetId==p.Id)?selectedButtonStyle:treeStyle,GUILayout.MinHeight(UiRowHeight(34))))
                {NavigateToPlanet(p.Id);}
                if(openPlanetId!=p.Id || p.OwnerId!=PlayerFactionId)continue;
                foreach(var b in p.Buildings)
                {if(GameSimulation.BuildingOwner(p,b)!=PlayerFactionId)continue;if(GUILayout.Button("    "+Definitions.Get(b.Kind).Name,treeStyle,GUILayout.MinHeight(UiRowHeight(25)))){SelectBuilding(b.Id);surfaceFocus=SurfacePosition(b.X,0,b.Y);UpdateCamera();}}
            }
            GUILayout.Space(20);GUILayout.Label("FLOTTE",headingStyle);
            bool any=false;foreach(var ship in simulation.State.Ships)
            {if(ship.OwnerId!=PlayerFactionId || ship.Destroyed || ship.Consumed)continue;any=true;if(GUILayout.Button(new GUIContent(Definitions.Ships[(int)ship.Kind].Name,HudSymbol(HudIcon.Fleet)),treeStyle,GUILayout.MinHeight(UiRowHeight(34)))){if(openPlanetId!=0)BeginViewFlight(0);SelectShip(ship.Id);}}
            if(!any)GUILayout.Label("Noch keine Schiffe\nBau im Raumhafen",smallStyle);
            GUILayout.Space(20);if(GUILayout.Button("Bodentruppen",treeStyle,GUILayout.MinHeight(UiRowHeight(30))))OpenPresentationSection(2);
            if(GUILayout.Button("Versorgung",treeStyle,GUILayout.MinHeight(UiRowHeight(30))))OpenPresentationSection(5);
            GUILayout.EndScrollView();GUILayout.EndArea();
        }

        private void DrawPresentationBottom(float width,float height)
        {
            float y=height-BottomHeight;
            if(CurrentLayout!=InterfaceLayout.OrbitalBridge)
            {
                var radar=new Rect(14,y-176,220,164);DrawPanel(radar);DrawRadar(new Rect(radar.x+8,radar.y+8,radar.width-16,radar.height-16));
            }
            if(CurrentLayout==InterfaceLayout.OpenSpace)
            {
                float w=Mathf.Min(width-60,760+(TextScale-1)*160);var dock=new Rect((width-w)/2,y+8,w,BottomHeight-20);DrawPanel(dock);
                int[] sections={1,3,4,6};HudIcon[] icons={HudIcon.Build,HudIcon.Fleet,HudIcon.Research,HudIcon.Diplomacy};float cell=(dock.width-24)/4;
                for(int i=0;i<4;i++)if(HudButton(new Rect(dock.x+12+i*cell,dock.y+9,cell-6,dock.height-18),SectionNames[sections[i]],icons[i],panelOpen && presentationSection==sections[i],null,true))OpenPresentationSection(sections[i]);
            }
            else if(CurrentLayout==InterfaceLayout.OrbitalBridge)
            {
                float commands=Mathf.Clamp(width*.3f,390,460),left=NavigationWidth;
                DrawPanel(new Rect(0,y,left,BottomHeight));DrawRadar(new Rect(18,y+20,left-36,BottomHeight-40));
                DrawPanel(new Rect(left+4,y,width-left-commands-8,BottomHeight));DrawSelectionSummary(new Rect(left+20,y+22,width-left-commands-40,BottomHeight-48),true);
                DrawPanel(new Rect(width-commands,y,commands,BottomHeight));GUI.Label(new Rect(width-commands+22,y+16,commands-44,TextLineHeight+4),"BEFEHLE",headingStyle);
                DrawContextCommands(new Rect(width-commands+20,y+FontUnitSize(20)+30,commands-40,BottomHeight-FontUnitSize(20)-54),true);
            }
            else
            {
                float w=width-SideWidth;DrawPanel(new Rect(0,y,w,BottomHeight));
                Texture portrait=CurrentPortrait();if(portrait!=null)GUI.DrawTexture(new Rect(12,y+8,88,BottomHeight-16),portrait,ScaleMode.ScaleToFit);
                DrawContextCommands(new Rect(108,y+16,w-126,BottomHeight-32),false);
            }
            string message=Time.unscaledTime<notificationUntil?notification:placement.HasValue || placingRoad || removingRoad?(placementReason??"Bauplatz wählen · Rechtsklick / Esc: abbrechen"):spaceMoveTargetMode?"Flugziel anklicken · Esc: abbrechen":militaryTargetMode.HasValue?"Befehlsziel anklicken · Esc: abbrechen":ScenarioOutcomeText;
            if(!string.IsNullOrEmpty(message))
            {
                float x=CurrentLayout==InterfaceLayout.OrbitalBridge?NavigationWidth+16:248,w=Mathf.Max(200,Mathf.Min(width-x-SideWidth-24,780));
                float h=smallStyle.CalcHeight(new GUIContent(message),w-24)+16;var notice=new Rect(x,y-h-10,w,h);DrawPanel(notice);GUI.Label(new Rect(notice.x+12,notice.y+8,notice.width-24,notice.height-16),message,smallStyle);
            }
        }

        private void DrawContextCommands(Rect rect,bool grid)
        {
            if(placement.HasValue || placingRoad || removingRoad || militaryTargetMode.HasValue || spaceMoveTargetMode)
            {
                GUI.Label(new Rect(rect.x,rect.y,rect.width*.62f,rect.height),"Ziel auf der Karte wählen",bodyStyle);
                if(HudButton(new Rect(rect.x+rect.width*.64f,rect.y,rect.width*.36f,38),"Abbrechen",HudIcon.Close)){CancelPlacement();militaryTargetMode=null;spaceMoveTargetMode=false;}return;
            }
            var context=SelectionCommands();var actions=BuildContextActions(context);int count=Mathf.Min(3,actions.Count);
            int slots=count+1,cols=grid?Mathf.Min(2,slots):slots,rows=Mathf.CeilToInt(slots/(float)cols);
            float w=(rect.width-(cols-1)*6)/cols,h=(rect.height-(rows-1)*6)/rows;
            for(int i=0;i<count;i++)
            {
                var a=actions[i];bool enabled=GUI.enabled;GUI.enabled=enabled && a.Disabled==null;
                if(HudButton(new Rect(rect.x+(i%cols)*(w+6),rect.y+(i/cols)*(h+6),w,h),a.Label,a.Icon,false,a.Disabled??a.Hint))
                {ShowCommandMenu(context,Input.mousePosition);InvokeContextCommand(a.Id);}
                GUI.enabled=enabled;
            }
            if(HudButton(new Rect(rect.x+count%cols*(w+6),rect.y+count/cols*(h+6),w,h),"Weitere …",HudIcon.Menu))OpenSelectionCommandMenu(Input.mousePosition);
        }

        private void DrawSelectionCard(float width,float height)
        {
            if(!SelectionCardVisible)return;Rect card=SelectionCardRect(width);DrawPanel(card);
            DrawSelectionSummary(new Rect(card.x+18,card.y+18,card.width-36,SelectionSummaryHeight),false,30);
            if(HudButton(new Rect(card.xMax-43,card.y+12,30,30),"",HudIcon.Close))CloseContextPanel();
            var actions=BuildContextActions(SelectionCommands());float row=UiRowHeight();
            for(int i=0;i<Mathf.Min(2,actions.Count);i++)
            {
                var action=actions[i];bool enabled=GUI.enabled;GUI.enabled=enabled && action.Disabled==null;
                if(HudButton(new Rect(card.x+18,card.yMax-22-(2-i)*(row+6),card.width-36,row),action.Label,action.Icon,i==0))
                {ShowCommandMenu(SelectionCommands(),Input.mousePosition);InvokeContextCommand(action.Id);}
                GUI.enabled=enabled;
            }
        }

        private int KnownPlanetOwner(PlanetState p)
        {if(simulation.IsPlanetVisible(PlayerFactionId,p.Id))return p.OwnerId;var s=simulation.State.SpaceSightings.Find(v=>v.FactionId==PlayerFactionId && v.PlanetId==p.Id);return s==null?-1:s.KnownOwnerId;}
        private Texture CurrentPortrait()
        {
            if(openPlanetId==0)
            {if(StarSelected)return StarPortrait();var ship=simulation.State.Ships.Find(s=>selectedShips.Contains(s.Id) && !s.Destroyed && !s.Consumed);if(ship!=null)return ShipPortrait(ship.Kind);var p=simulation.FindPlanet(selectedPlanetId);return p==null?null:PlanetPortrait(p);}
            var planet=simulation.FindPlanet(openPlanetId);var b=FindSelected(planet);if(b!=null)return BuildingIcon(b.Kind);
            var u=planet.Units.Find(v=>selectedUnits.Contains(v.Id)&&!v.Destroyed);return u==null?null:UnitPortrait(u.Kind);
        }
        private void DrawSelectionSummary(Rect rect,bool wide,float titleReserve=0)
        {
            string title=openPlanetId==0?"Sonnensystem":"Kolonie",type="Objekt auswählen",status="",extra="",progressName="Hülle";float health=-1;
            if(openPlanetId==0)
            {
                var ship=simulation.State.Ships.Find(s=>selectedShips.Contains(s.Id)&&!s.Destroyed&&!s.Consumed);
                if(ship!=null){title=Definitions.Ships[(int)ship.Kind].Name;type="Eigene Flotte · #"+ship.Id;status=ship.Status;extra="Besatzung  "+ship.Crew;health=(float)(ship.Health/Definitions.SpaceWeapons[(int)ship.Kind].Health);}
                else if(StarSelected){title="Zentralstern";type=StellarReference.SpectralType+" · Hauptreihenstern";status="Ca. "+StellarReference.EffectiveTemperatureKelvin.ToString("N0")+" K · 1 Sonnenmasse";extra="1 Sonnenradius · 1 Sonnenleuchtkraft";}
                else {var p=simulation.FindPlanet(selectedPlanetId);if(p!=null){title=p.Name;type=PlanetEnvironment.Name(p.Kind);int owner=KnownPlanetOwner(p);status=owner==PlayerFactionId?"Kolonie aktiv · "+p.Population+" Bewohner":owner==0?"Unbesiedelt":owner<0?"Besitz unbekannt":"Fremde Kolonie";extra=PlanetEnvironment.StellarAu(simulation.State,p).ToString("0.00")+" AE · "+PlanetEnvironment.RadiusEarths(p.Kind).ToString("0.00")+" Erdradien";}}
            }
            else
            {
                var p=simulation.FindPlanet(openPlanetId);var b=FindSelected(p);var u=p.Units.Find(v=>selectedUnits.Contains(v.Id)&&!v.Destroyed);
                if(b!=null){title=Definitions.Get(b.Kind).Name;type=b.Built?"Gebäude":"Baustelle";health=b.Built?(float)(b.Health/Definitions.BuildingHealth(b.Kind)):(float)(b.BuildProgress/Definitions.Get(b.Kind).BuildSeconds);progressName=b.Built?"Zustand":"Bau";status=b.Status;extra="Energie  "+Definitions.Get(b.Kind).EnergyUse+"    Arbeit  "+Definitions.Get(b.Kind).WorkerUse;}
                else if(u!=null){title=Definitions.Get(u.Kind).Name;type=selectedUnits.Count+" ausgewählt";health=(float)(u.Health/Definitions.Get(u.Kind).Health);status=u.Order==UnitOrder.Hold?"Hält Position":"Unterwegs";extra="Rechtsklick: Auftrag · Strg: Kontextmenü";}
                else {title=p.Name;status="Gebäude oder Einheit auswählen";extra=p.OwnerId==PlayerFactionId?p.Population+" Bewohner":"Fremde Oberfläche";}
            }
            float imageWidth=wide?Mathf.Min(154,rect.width*.29f):84;
            Texture portrait=CurrentPortrait();if(portrait!=null)GUI.DrawTexture(new Rect(rect.x,rect.y+4,imageWidth,wide?Mathf.Min(122,rect.height-26):78),portrait,ScaleMode.ScaleToFit);
            float left=rect.x+imageWidth+12,tw=rect.width-imageWidth-20;
            float titleWidth=Mathf.Max(60,tw-titleReserve);title=WrapObjectTitle(title,titleWidth);
            float titleH=headingStyle.CalcHeight(new GUIContent(title),titleWidth);GUI.Label(new Rect(left,rect.y,titleWidth,titleH),title,headingStyle);
            float typeH=smallStyle.CalcHeight(new GUIContent(type),tw);GUI.Label(new Rect(left,rect.y+titleH+4,tw,typeH),type,smallStyle);
            float detailsY=wide?rect.y+titleH+typeH+8:rect.y+Mathf.Max(88,titleH+typeH+12);
            float dx=wide?left:rect.x,dw=wide?tw:rect.width;
            if(wide)status=CompactHudCaption(status,dw,bodyStyle.CalcHeight(new GUIContent("Ag"),dw),bodyStyle);
            float statusH=bodyStyle.CalcHeight(new GUIContent(status),dw);GUI.Label(new Rect(dx,detailsY,dw,statusH),status,bodyStyle);
            float extraY=detailsY+statusH+3,extraH=smallStyle.CalcHeight(new GUIContent(extra),dw);GUI.Label(new Rect(dx,extraY,dw,extraH),extra,smallStyle);
            if(health>=0)
            {
                string progress=progressName+" "+Mathf.RoundToInt(Mathf.Clamp01(health)*100)+" %";
                float valueWidth=smallStyle.CalcSize(new GUIContent(progress)).x+4;
                float barY=extraY+extraH+smallStyle.fontSize*.5f+7,bw=Mathf.Max(40,dw-valueWidth-10);
                Fill(new Rect(dx,barY,bw,7),Border);Fill(new Rect(dx,barY,bw*Mathf.Clamp01(health),7),progressName=="Bau"?Amber:new Color(.25f,.88f,.42f));
                LabelFitted(new Rect(dx+bw+10,barY-smallStyle.fontSize/2,valueWidth,smallStyle.fontSize+6),progress,smallStyle);
            }
        }
        private Texture2D PlanetPortrait(PlanetState planet){return ObjectPortrait("planet"+planet.Id+"-"+planet.ParentId,r=>CreateCelestial(planet,r,Vector3.zero,1.5f),1);}
        private Texture2D UnitPortrait(UnitKind kind){return ObjectPortrait("unit"+kind+"-"+AtmosphereSurface,r=>BuildVehicleModel(r,kind,Cyan),.64f,true);}
        private Texture2D ShipPortrait(ShipKind kind){return ObjectPortrait("ship"+kind,r=>BuildShipModel(r,kind),0.75f,true);}
        // From this azimuth, local +Z (the front of a unit) projects toward the lower right.
        private const float UnitPortraitYaw=-150f;
        private Texture2D ObjectPortrait(string key,Action<Transform> create,float size,bool catalogue=false)
        {
            Texture2D image;if(objectPortraits.TryGetValue(key,out image))return image;
            var root=new GameObject("Objektporträt").transform;root.position=Vector3.one*2500;var cameraObject=new GameObject("Porträtkamera");var camera=cameraObject.AddComponent<Camera>();var rt=RenderTexture.GetTemporary(192,128,24);var previous=RenderTexture.active;
            var portraitMaterials=new List<Material>();
            try
            {create(root);foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.sharedMaterial.shader.name=="PlanetOne/CelestialSurface"){var material=new Material(renderer.sharedMaterial);material.SetVector("_SunDirection",new Vector4(1,1,2,0));renderer.sharedMaterial=material;portraitMaterials.Add(material);}foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;camera.orthographicSize=size;camera.aspect=1.5f;camera.nearClipPlane=0.1f;camera.farClipPlane=15;camera.transform.position=root.position+(catalogue?CatalogueCameraOffset(UnitPortraitYaw):new Vector3(2,2.4f,3));camera.transform.LookAt(root.position+Vector3.up*(catalogue?.24f:.1f));if(catalogue)FrameCatalogueModel(camera,root);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(192,128,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,192,128),0,0);image.Apply();objectPortraits[key]=image;return image;}
            finally{RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);root.gameObject.SetActive(false);Destroy(root.gameObject);Destroy(cameraObject);foreach(var material in portraitMaterials)Destroy(material);}
        }
    }
}
