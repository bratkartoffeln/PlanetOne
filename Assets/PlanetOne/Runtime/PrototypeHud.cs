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
        private bool HasSelection {get{return openPlanetId==0?StarSelected || selectedShips.Count>0 || simulation.FindPlanet(selectedPlanetId)!=null:selectedBuildingId!=0 || selectedUnits.Count>0;}}
        private bool SelectionCardVisible {get{return CurrentLayout!=InterfaceLayout.CommandCenter && contextOpen && !panelOpen && HasSelection;}}
        private Rect SelectionCardRect(float width){return new Rect(width-302,TopHeight+22,286,244);}
        public bool IsContextCardOpen {get{return SelectionCardVisible;}}
        public Rect ContextCardScreenRect {get{var r=SelectionCardRect(Screen.width/UiScale);return new Rect(r.x*UiScale,Screen.height-r.yMax*UiScale,r.width*UiScale,r.height*UiScale);}}
        public void CloseContextPanel(){panelOpen=false;contextOpen=false;}

        private void DrawPresentationTop(float width)
        {
            DrawPanel(new Rect(0,0,width,TopHeight));
            string place=openPlanetId==0?"Sonnensystem":simulation.FindPlanet(openPlanetId).Name;
            if(openPlanetId==0)
            {if(HudButton(new Rect(8,5,190,34),place,HudIcon.Planet,false,"Bekannte Welten ansehen"))OpenPresentationSection(0);}
            else
            {
                if(HudButton(new Rect(8,5,34,34),"",HudIcon.Back,false,"Zurück zum Sonnensystem"))SetView(0);
                if(HudButton(new Rect(46,5,152,34),place,HudIcon.Planet,false,"Kolonieübersicht · Produktion · Versorgung"))OpenPresentationSection(0);
            }
            int[] stock=new int[6];int power=0;
            foreach(var p in simulation.State.Planets)
            {
                if(p.OwnerId!=PlayerFactionId || openPlanetId!=0 && p.Id!=openPlanetId)continue;
                power+=p.EnergyProduced-p.EnergyUsed;
                foreach(var b in p.Buildings)if(GameSimulation.BuildingOwner(p,b)==PlayerFactionId)for(int r=0;r<6;r++)stock[r]+=b.Inventory[r];
            }
            string[] labels={"Energie "+(power>0?"+":"")+power,"Metall "+stock[2],"Bauteile "+stock[3],"Nahrung "+stock[0],"Kristalle "+stock[4]};
            HudIcon[] icons={HudIcon.Energy,HudIcon.Metal,HudIcon.Parts,HudIcon.Food,HudIcon.Crystal};
            float cell=Mathf.Min(145,(width-454)/5);
            for(int i=0;i<5;i++)
            {
                float x=208+i*cell;Color old=GUI.color;GUI.color=i==0?Amber:i==4?Cyan:Ink;GUI.DrawTexture(new Rect(x,11,22,22),HudSymbol(icons[i]));GUI.color=old;
                GUI.Label(new Rect(x+28,13,cell-30,22),new GUIContent(labels[i],i==0?"Energieüberschuss: Erzeugung minus Verbrauch":"Eigener verfügbarer Bestand in den lokalen Lagern"),bodyStyle);
            }
            float right=width-230;
            if(HudButton(new Rect(right,5,106,34),simulation.State.Paused?"Weiter":"Pause",simulation.State.Paused?HudIcon.Play:HudIcon.Pause,simulation.State.Paused,"Leertaste: Spiel pausieren oder fortsetzen"))TogglePause();
            if(Button(new Rect(right+110,5,36,34),simulation.State.TimeScale+"×",false))SetSimulationSpeed(simulation.State.TimeScale==1?2:1);
            if(HudButton(new Rect(right+150,5,34,34),"",HudIcon.Settings,false,"Darstellung und Layout ändern"))SetSettingsVisible(true);
            if(HudButton(new Rect(right+188,5,34,34),"",HudIcon.Menu,false,"Spielmenü · Speichern · Laden · Neue Partie"))SetMenuVisible(true);
        }

        private void DrawSidebar(float width,float height)
        {
            if(!SidebarVisible)return;
            Rect area=SidebarRect(width,height);DrawPanel(area);
            bool fixedBar=CurrentLayout==InterfaceLayout.CommandCenter;
            float top=area.y+12;
            GUI.Label(new Rect(area.x+16,top,area.width-62,26),panelOpen?SectionNames[presentationSection].ToUpperInvariant():"AUSWAHL",headingStyle);
            if(panelOpen && HudButton(new Rect(area.xMax-42,top-4,30,30),"",HudIcon.Close,false,"Zur Auswahl zurück")){panelOpen=false;contextOpen=true;}
            top+=34;
            if(fixedBar && !panelOpen){DrawRadar(new Rect(area.x+14,top,area.width-28,112));top+=124;}
            float footer=fixedBar?174:0;
            Rect content=new Rect(area.x+14,top,area.width-28,area.yMax-top-footer-12);
            GUILayout.BeginArea(content);
            sidebarScroll=GUILayout.BeginScrollView(sidebarScroll,false,false);
            GUILayout.BeginVertical(GUILayout.Width(content.width-16));
            DrawHudContent();
            GUILayout.EndVertical();GUILayout.EndScrollView();GUILayout.EndArea();
            if(fixedBar)
            {
                int[] sections={1,3,4,6};HudIcon[] icons={HudIcon.Build,HudIcon.Fleet,HudIcon.Research,HudIcon.Diplomacy};
                for(int i=0;i<4;i++)if(HudButton(new Rect(area.x+12,area.yMax-footer+i*40,area.width-24,36),SectionNames[sections[i]],icons[i],presentationSection==sections[i]))OpenPresentationSection(sections[i]);
            }
        }

        private void DrawHudContent()
        {
            if(!panelOpen)
            {
                DrawSelectionSummary(new Rect(0,0,252,160),false);GUILayout.Space(168);
                if(GUILayout.Button("Objektbefehle öffnen",selectedButtonStyle,GUILayout.Height(38)))OpenSelectionCommandMenu(Input.mousePosition);
                GUILayout.Label("Rechtsklick: passende Befehle\nRechts ziehen: Ansicht verschieben",smallStyle);
                if(openPlanetId==0 && selectedShips.Count==0 && !StarSelected)
                {
                    GUILayout.Space(18);GUILayout.Label("BEKANNTE WELTEN",smallStyle);
                    foreach(var world in simulation.State.Planets)
                        if(simulation.IsPlanetKnown(PlayerFactionId,world.Id) && GUILayout.Button(world.Name,world.Id==selectedPlanetId?selectedButtonStyle:buttonStyle,GUILayout.Height(32)))SelectPlanet(world.Id);
                }
                return;
            }
            if(showingDiplomacy){DrawDiplomacyPanel();return;}
            if(showingSpacePanel && panelOpen){DrawSpacePanel();return;}
            if(openPlanetId==0)
            {
                if(StarSelected){DrawStarDetails();return;}
                if(selectedShips.Count>0 && !panelOpen){DrawSelectionSummary(new Rect(0,0,252,190),false);GUILayout.Space(195);if(GUILayout.Button("Flotte verwalten",buttonStyle,GUILayout.Height(34)))OpenPresentationSection(3);return;}
                var p=simulation.FindPlanet(selectedPlanetId);
                if(p!=null && simulation.IsPlanetKnown(PlayerFactionId,p.Id))
                {
                    GUILayout.Label(p.Name.ToUpperInvariant(),headingStyle);
                    Rect portrait=GUILayoutUtility.GetRect(160,76);GUI.DrawTexture(portrait,PlanetPortrait(p),ScaleMode.ScaleToFit);
                    GUILayout.Label(PlanetEnvironment.Name(p.Kind)+(p.ParentId>0?" · Mond":""),mutedStyle);
                    GUILayout.Label(PlanetEnvironment.StellarAu(simulation.State,p).ToString("0.00")+" AE · "+PlanetEnvironment.RadiusEarths(p.Kind).ToString("0.00")+" Erdradien",smallStyle);
                    int owner=KnownPlanetOwner(p);GUILayout.Label(owner==PlayerFactionId?"Eigene Kolonie":owner==0?"Unbesiedelt":owner<0?"Besitz unbekannt":"Fremde Kolonie",bodyStyle);
                    if(owner==PlayerFactionId)GUILayout.Label(p.Population+" Bewohner · "+p.Buildings.Count+" Gebäude",smallStyle);
                    GUILayout.Space(6);if(GUILayout.Button(PlanetEnvironment.Airborne(p)?"Atmosphäre öffnen  →":"Oberfläche öffnen  →",selectedButtonStyle,GUILayout.Height(38)))OpenSelectedPlanet();
                }
                GUILayout.Space(18);GUILayout.Label("BEKANNTE WELTEN",smallStyle);
                foreach(var world in simulation.State.Planets)
                    if(simulation.IsPlanetKnown(PlayerFactionId,world.Id) && GUILayout.Button(world.Name,world.Id==selectedPlanetId?selectedButtonStyle:buttonStyle,GUILayout.Height(31)))SelectPlanet(world.Id);
                return;
            }
            var planet=simulation.FindPlanet(openPlanetId);
            if(showingMilitary){DrawMilitaryPanel(planet);return;}
            if(showingDetails)
            {
                var b=FindSelected(planet);
                if(b!=null){Rect image=GUILayoutUtility.GetRect(160,92);GUI.DrawTexture(image,BuildingIcon(b.Kind),ScaleMode.ScaleToFit);DrawBuildingDetails(b);}
                else GUILayout.Label("Ein Gebäude auswählen, um seinen Betrieb zu verwalten.",bodyStyle);
                return;
            }
            if(presentationSection==0)
            {
                DrawColonySummary(planet);GUILayout.Space(12);
                if(GUILayout.Button("Bodentruppen verwalten",buttonStyle,GUILayout.Height(34)))OpenPresentationSection(2);
                if(GUILayout.Button("Versorgung verwalten",buttonStyle,GUILayout.Height(34)))OpenPresentationSection(5);
                return;
            }
            if(planet.OwnerId==PlayerFactionId){GUILayout.Label("BAUEN",headingStyle);GUILayout.Space(8);DrawBuildTools();}
            else {GUILayout.Label("Fremde Oberfläche",headingStyle);GUILayout.Label("Eigene Truppen können hier über ihre Auswahl befehligt werden.",bodyStyle);}
        }

        private void DrawPresentationNavigation(float width,float height)
        {
            if(CurrentLayout!=InterfaceLayout.OrbitalBridge)return;
            DrawPanel(new Rect(0,TopHeight,200,height-TopHeight-BottomHeight));
            GUILayout.BeginArea(new Rect(12,TopHeight+16,176,height-TopHeight-BottomHeight-32));
            navigationScroll=GUILayout.BeginScrollView(navigationScroll,false,false);
            if(GUILayout.Button("← Sonnensystem",treeStyle,GUILayout.Height(30)))SetView(0);
            GUILayout.Space(12);GUILayout.Label("KOLONIEN",headingStyle);
            foreach(var p in simulation.State.Planets)
            {
                if(!simulation.IsPlanetKnown(PlayerFactionId,p.Id))continue;
                if(GUILayout.Button(new GUIContent(p.Name,HudSymbol(HudIcon.Planet)),(openPlanetId==p.Id || openPlanetId==0 && selectedPlanetId==p.Id)?selectedButtonStyle:treeStyle,GUILayout.Height(34)))
                {if(openPlanetId==0)SelectPlanet(p.Id);else SetView(p.Id);}
                if(openPlanetId!=p.Id || p.OwnerId!=PlayerFactionId)continue;
                foreach(var b in p.Buildings)
                {if(GameSimulation.BuildingOwner(p,b)!=PlayerFactionId)continue;if(GUILayout.Button("    "+Definitions.Get(b.Kind).Name,treeStyle,GUILayout.Height(25))){SelectBuilding(b.Id);surfaceFocus=SurfacePosition(b.X,0,b.Y);UpdateCamera();}}
            }
            GUILayout.Space(20);GUILayout.Label("FLOTTE",headingStyle);
            bool any=false;foreach(var ship in simulation.State.Ships)
            {if(ship.OwnerId!=PlayerFactionId || ship.Destroyed || ship.Consumed)continue;any=true;if(GUILayout.Button(new GUIContent(Definitions.Ships[(int)ship.Kind].Name,HudSymbol(HudIcon.Fleet)),treeStyle,GUILayout.Height(34))){SetView(0);SelectShip(ship.Id);}}
            if(!any)GUILayout.Label("Noch keine Schiffe\nBau im Raumhafen",smallStyle);
            GUILayout.Space(20);if(GUILayout.Button("Bodentruppen",treeStyle,GUILayout.Height(30)))OpenPresentationSection(2);
            if(GUILayout.Button("Versorgung",treeStyle,GUILayout.Height(30)))OpenPresentationSection(5);
            GUILayout.EndScrollView();GUILayout.EndArea();
        }

        private void DrawPresentationBottom(float width,float height)
        {
            float y=height-BottomHeight;
            if(CurrentLayout==InterfaceLayout.OpenSpace)
            {
                var dock=new Rect((width-Mathf.Min(980,width-32))/2,y+5,Mathf.Min(980,width-32),BottomHeight-10);DrawPanel(dock);
                int[] sections={1,3,4,6};HudIcon[] icons={HudIcon.Build,HudIcon.Fleet,HudIcon.Research,HudIcon.Diplomacy};
                float navWidth=(dock.width-24)/4;
                for(int i=0;i<4;i++)if(HudButton(new Rect(dock.x+8+i*navWidth,dock.y+5,navWidth-4,30),SectionNames[sections[i]],icons[i],panelOpen && presentationSection==sections[i]))OpenPresentationSection(sections[i]);
                DrawContextCommands(new Rect(dock.x+12,dock.y+44,dock.width-24,46),false);
            }
            else if(CurrentLayout==InterfaceLayout.OrbitalBridge)
            {
                DrawPanel(new Rect(0,y,200,BottomHeight));DrawRadar(new Rect(12,y+12,176,BottomHeight-24));
                DrawPanel(new Rect(204,y,width-608,BottomHeight));DrawSelectionSummary(new Rect(216,y+12,width-632,BottomHeight-28),true);
                DrawPanel(new Rect(width-400,y,400,BottomHeight));GUI.Label(new Rect(width-384,y+10,360,24),"BEFEHLE",headingStyle);
                DrawContextCommands(new Rect(width-386,y+40,372,94),true);
            }
            else
            {
                DrawPanel(new Rect(0,y,width-SideWidth,BottomHeight));
                Texture portrait=CurrentPortrait();if(portrait!=null)GUI.DrawTexture(new Rect(10,y+7,80,BottomHeight-14),portrait,ScaleMode.ScaleToFit);
                DrawContextCommands(new Rect(100,y+14,width-SideWidth-116,38),false);
            }
            string message=Time.unscaledTime<notificationUntil?notification:placement.HasValue || placingRoad || removingRoad?"Bauplatz wählen · Rechtsklick / Esc: abbrechen":spaceMoveTargetMode?"Flugziel anklicken · Rechtsklick / Esc: abbrechen":militaryTargetMode.HasValue?"Befehlsziel anklicken · Rechtsklick / Esc: abbrechen":ScenarioOutcomeText;
            if(!string.IsNullOrEmpty(message))
            {
                float x=CurrentLayout==InterfaceLayout.OrbitalBridge?216:18;
                var notice=new Rect(x,y-36,Mathf.Min(width-x-320,700),28);Fill(notice,new Color(0.01f,0.025f,0.04f,0.9f));GUI.Label(new Rect(notice.x+10,notice.y+4,notice.width-20,24),message,smallStyle);
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
            int cols=grid?2:4;float w=(rect.width-(cols-1)*6)/cols,h=grid?42:rect.height;
            for(int i=0;i<count;i++)
            {
                var a=actions[i];bool enabled=GUI.enabled;GUI.enabled=enabled && a.Disabled==null;
                if(HudButton(new Rect(rect.x+(i%cols)*(w+6),rect.y+(i/cols)*(h+6),w,h),a.Label,a.Icon,false,a.Disabled??a.Hint))
                {ShowCommandMenu(context,Input.mousePosition);InvokeContextCommand(a.Id);}
                GUI.enabled=enabled;
            }
            if(HudButton(new Rect(rect.x+3%cols*(w+6),rect.y+3/cols*(h+6),w,h),"Alle Befehle …",HudIcon.Menu))OpenSelectionCommandMenu(Input.mousePosition);
        }

        private void DrawSelectionCard(float width,float height)
        {
            if(!SelectionCardVisible)return;Rect card=SelectionCardRect(width);Fill(card,new Color(.02f,.04f,.055f,.95f));DrawPanel(card);
            DrawSelectionSummary(new Rect(card.x+14,card.y+14,card.width-28,158),false);
            if(HudButton(new Rect(card.xMax-35,card.y+8,27,27),"",HudIcon.Close,false,"Auswahlkarte schließen"))CloseContextPanel();
            string action=openPlanetId==0 && selectedShips.Count==0?"Oberfläche öffnen":"Objektbefehle öffnen";
            if(StarSelected)action="Sterndetails";
            var selectedWorld=simulation.FindPlanet(selectedPlanetId);
            if(openPlanetId==0 && selectedShips.Count==0 && selectedWorld!=null && PlanetEnvironment.Airborne(selectedWorld))action="Atmosphäre öffnen";
            if(HudButton(new Rect(card.x+14,card.yMax-54,card.width-28,38),action,StarSelected?HudIcon.Info:openPlanetId==0 && selectedShips.Count==0?HudIcon.Planet:HudIcon.Info))
            {if(StarSelected)OpenStarDetails();else if(openPlanetId==0 && selectedShips.Count==0)OpenSelectedPlanet();else OpenSelectionCommandMenu(Input.mousePosition);}
        }

        private int KnownPlanetOwner(PlanetState p)
        {if(simulation.IsPlanetVisible(PlayerFactionId,p.Id))return p.OwnerId;var s=simulation.State.SpaceSightings.Find(v=>v.FactionId==PlayerFactionId && v.PlanetId==p.Id);return s==null?-1:s.KnownOwnerId;}
        private Texture CurrentPortrait()
        {
            if(openPlanetId==0)
            {if(StarSelected)return StarPortrait();var ship=simulation.State.Ships.Find(s=>selectedShips.Contains(s.Id) && !s.Destroyed && !s.Consumed);if(ship!=null)return ShipPortrait(ship.Kind);var p=simulation.FindPlanet(selectedPlanetId);return p==null?null:PlanetPortrait(p);}
            var planet=simulation.FindPlanet(openPlanetId);var b=FindSelected(planet);if(b!=null)return BuildingIcon(b.Kind);
            var u=planet.Units.Find(v=>selectedUnits.Contains(v.Id)&&!v.Destroyed);return u==null?null:ObjectPortrait("unit"+u.Kind+"-"+planet.Kind,r=>BuildVehicleModel(r,u.Kind,Cyan),0.7f);
        }
        private void DrawSelectionSummary(Rect rect,bool wide)
        {
            string title=openPlanetId==0?"Sonnensystem":"Kolonie",type="Objekt auswählen",status="",extra="";float health=-1;
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
                if(b!=null){title=Definitions.Get(b.Kind).Name;type=b.Built?"Gebäude":"Baustelle";health=(float)(b.Health/Definitions.BuildingHealth(b.Kind));status=b.Status;extra="Energie  "+Definitions.Get(b.Kind).EnergyUse+"    Arbeit  "+Definitions.Get(b.Kind).WorkerUse;}
                else if(u!=null){title=Definitions.Get(u.Kind).Name;type=selectedUnits.Count+" ausgewählt";health=(float)(u.Health/Definitions.Get(u.Kind).Health);status=u.Order==UnitOrder.Hold?"Hält Position":"Unterwegs";extra="Befehle unten oder per Rechtsklick";}
                else {title=p.Name;status="Gebäude oder Einheit auswählen";extra=p.OwnerId==PlayerFactionId?p.Population+" Bewohner":"Fremde Oberfläche";}
            }
            float imageWidth=wide?146:86;Texture portrait=CurrentPortrait();if(portrait!=null)GUI.DrawTexture(new Rect(rect.x,rect.y+2,imageWidth,wide?116:68),portrait,ScaleMode.ScaleToFit);
            float left=rect.x+imageWidth+12;
            GUI.Label(new Rect(left,rect.y+3,rect.width-imageWidth-24,44),title,headingStyle);
            GUI.Label(new Rect(left,rect.y+(wide?40:46),rect.width-imageWidth-20,22),type,smallStyle);
            float detailsY=wide?rect.y+68:rect.y+88;
            GUI.Label(new Rect(wide?left:rect.x,detailsY,wide?rect.width-imageWidth-12:rect.width,36),status,bodyStyle);
            GUI.Label(new Rect(wide?left:rect.x,detailsY+(wide?30:37),wide?rect.width-imageWidth-12:rect.width,24),extra,smallStyle);
            if(health>=0){float barY=wide?rect.yMax-5:rect.y+74;float barWidth=wide?rect.width-imageWidth-80:rect.width-46;float barX=wide?left:rect.x;Fill(new Rect(barX,barY,barWidth,5),Border);Fill(new Rect(barX,barY,barWidth*Mathf.Clamp01(health),5),new Color(0.26f,0.85f,0.4f));GUI.Label(new Rect(barX+barWidth+7,barY-6,45,20),Mathf.RoundToInt(health*100)+" %",smallStyle);}
        }
        private Texture2D PlanetPortrait(PlanetState planet){return ObjectPortrait("planet"+planet.Id+"-"+planet.ParentId,r=>CreateCelestial(planet,r,Vector3.zero,1.5f),1);}
        private Texture2D ShipPortrait(ShipKind kind){return ObjectPortrait("ship"+kind,r=>BuildShipModel(r,kind),0.75f);}
        private Texture2D ObjectPortrait(string key,Action<Transform> create,float size)
        {
            Texture2D image;if(objectPortraits.TryGetValue(key,out image))return image;
            var root=new GameObject("Objektporträt").transform;root.position=Vector3.one*2500;var cameraObject=new GameObject("Porträtkamera");var camera=cameraObject.AddComponent<Camera>();var rt=RenderTexture.GetTemporary(192,128,24);var previous=RenderTexture.active;
            var portraitMaterials=new List<Material>();
            try
            {create(root);foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.sharedMaterial.shader.name=="PlanetOne/CelestialSurface"){var material=new Material(renderer.sharedMaterial);material.SetVector("_SunDirection",new Vector4(1,1,2,0));renderer.sharedMaterial=material;portraitMaterials.Add(material);}foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;camera.orthographicSize=size;camera.aspect=1.5f;camera.nearClipPlane=0.1f;camera.farClipPlane=15;camera.transform.position=root.position+new Vector3(2,2.4f,3);camera.transform.LookAt(root.position+Vector3.up*0.1f);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(192,128,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,192,128),0,0);image.Apply();objectPortraits[key]=image;return image;}
            finally{RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);root.gameObject.SetActive(false);Destroy(root.gameObject);Destroy(cameraObject);foreach(var material in portraitMaterials)Destroy(material);}
        }
    }
}
