using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        public const float SpaceDistanceScale=2.2f,StarDiameter=8.6f;
        private readonly Dictionary<long,Transform> asteroidViews=new Dictionary<long,Transform>();
        private long selectedAsteroidId;
        private int asteroidPage;
        private bool miningPanel;
        public bool IsMiningPanelOpen {get{return miningPanel && panelOpen && showingSpacePanel && spacePanelTab==0 && fleetTask==3;}}
        public void StartAsteroidFixture()
        {
            if(!simulation.State.SpaceTestFixture)priorSession=simulation;
            simulation=SpaceFixture.Create(2);simulation.State.LastMessage="Prüfkarte: Asteroidengürtel mit regulär bezahltem Frachter und Bergbaudrohnen.";
            foodRates.Clear();shipGroups.Clear();BuildSpace();SetView(0);miningPanel=true;
            SelectShip(simulation.State.Ships.Find(s=>s.Kind==ShipKind.Freighter).Id);SelectAsteroid(simulation.State.Asteroids[0].Id);
        }
        public static Vector3 SpacePosition(double x,double y,float height=0) {return new Vector3((float)x*SpaceDistanceScale,height,(float)y*SpaceDistanceScale);}
        public static float PlanetDiameter(PlanetState planet)
        {
            if(planet.ParentId>0)return .28f;
            return Mathf.Min(StarDiameter*.5f,planet.Kind==PlanetKind.GasGiant?4.1f:1.2f*(float)PlanetEnvironment.RadiusEarths(planet.Kind));
        }
        internal Transform StarView {get{return spaceRoot.Find("Stern");}}
        internal float CoronaSymmetry(float pan,float yaw)
        {
            var origin=new Vector3(0,-10000,0);var probe=GameObject.CreatePrimitive(PrimitiveType.Sphere);probe.layer=28;
            probe.transform.position=origin;probe.transform.localScale=Vector3.one*StarDiameter*1.9f;
            probe.GetComponent<Renderer>().sharedMaterial=coronaMaterial;
            var cameraObject=new GameObject("Isolierte Koronaprüfung");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            var rt=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(512,512,TextureFormat.RGB24,false);
            var old=RenderTexture.active;
            try
            {
                camera.orthographic=true;camera.orthographicSize=16;camera.aspect=1;camera.nearClipPlane=.1f;camera.farClipPlane=100;
                camera.cullingMask=1<<28;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.targetTexture=rt;
                var rotation=Quaternion.Euler(0,yaw,0);camera.transform.SetPositionAndRotation(origin+rotation*new Vector3(pan,0,-30),rotation);
                camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,512,512),0,0);pixels.Apply();
                var centre=camera.WorldToViewportPoint(origin);int x=Mathf.RoundToInt(centre.x*512),y=Mathf.RoundToInt(centre.y*512);
                float difference=0,total=0;
                for(int dy=-70;dy<70;dy+=2)for(int dx=1;dx<110;dx+=2)
                {float left=pixels.GetPixel(x-dx,y+dy).r,right=pixels.GetPixel(x+dx-1,y+dy).r;difference+=Mathf.Abs(left-right);total+=left+right;}
                return difference/Mathf.Max(.001f,total);
            }
            finally {RenderTexture.active=old;camera.targetTexture=null;rt.Release();probe.SetActive(false);Destroy(probe);Destroy(cameraObject);Destroy(rt);Destroy(pixels);}
        }
        public Transform AsteroidView(long id) {Transform view;return asteroidViews.TryGetValue(id,out view)?view:null;}
        public void SelectAsteroid(long id)
        {
            if(!simulation.State.Asteroids.Exists(a=>a.Id==id))return;
            CloseCommandMenu();selectedAsteroidId=id;asteroidPage=simulation.State.Asteroids.FindIndex(a=>a.Id==id)/4;miningPanel=true;fleetTask=3;ShowSpacePanel(0);
        }
        public void FocusAsteroid(long id)
        {
            var a=simulation.State.Asteroids.Find(v=>v.Id==id);if(a==null)return;
            SelectAsteroid(id);var p=simulation.GetAsteroidPosition(a);SetSpaceFocus(SpacePosition(p.X,p.Y));spaceZoom=7;UpdateCamera();
        }
        public bool RequestAsteroidApproach(long ship,long asteroid)
        {
            var a=simulation.State.Asteroids.Find(v=>v.Id==asteroid);if(a==null)return false;
            var p=simulation.GetAsteroidPosition(a);
            return SubmitIntent(new SimCommand {Kind=CommandKind.MoveShipPosition,ActorId=ship,PlanetId=1,X=Mathf.RoundToInt((float)p.X),Y=Mathf.RoundToInt((float)p.Y)});
        }
        private void BuildAsteroidViews()
        {
            foreach(var mesh in beltDustMeshes)Destroy(mesh);beltDustMeshes.Clear();
            asteroidViews.Clear();selectedAsteroidId=0;asteroidPage=0;miningPanel=false;
            foreach(var a in simulation.State.Asteroids)
            {
                // Larger individual rocks remain well below the smallest moon's diameter.
                float size=Mathf.Min(.11f,(.012f+(float)a.DiameterKm*.0048f)*1.8f);
                var view=Rock(spaceRoot,Vector3.zero,new Vector3(size,size*.7f,size*.9f),(int)a.Id);
                view.name="Asteroid "+a.Id;view.GetComponent<Renderer>().sharedMaterial=ArtMaterial("Fels",new Color(.43f,.39f,.34f),false);
                asteroidViews[a.Id]=view;
            }
            foreach(var belt in simulation.State.AsteroidBelts)BuildBeltDust(belt);
        }
        private void UpdateAsteroidViews()
        {
            UpdateBeltDust();
            foreach(var a in simulation.State.Asteroids)
            {
                var p=simulation.GetAsteroidPosition(a);var view=asteroidViews[a.Id];
                view.position=SpacePosition(p.X,p.Y,(float)(p.Y*System.Math.Sin(a.Inclination))*SpaceDistanceScale);
                view.rotation=Quaternion.Euler((float)(simulation.State.Tick*Definitions.TickSeconds*.6+a.Id%37),a.Id%360,17);
            }
        }
        private bool PickAsteroid(Vector2 point)
        {
            long closest=0;float best=float.MaxValue;
            foreach(var a in simulation.State.Asteroids)
            {var screen=sceneCamera.WorldToScreenPoint(asteroidViews[a.Id].position);float d=Vector2.Distance(screen,point);if(screen.z>0 && d<Mathf.Max(12,AsteroidScreenRadius(a.Id)+5) && d<best){closest=a.Id;best=d;}}
            if(closest==0)return false;SelectAsteroid(closest);return true;
        }
        private float AsteroidScreenRadius(long id)
        {
            var size=asteroidViews[id].GetComponent<Renderer>().bounds.size;
            return Mathf.Max(size.x,Mathf.Max(size.y,size.z))*sceneCamera.pixelHeight/(4*sceneCamera.orthographicSize);
        }
        private void DrawAsteroidLabels(float scale)
        {
            foreach(var a in simulation.State.Asteroids)
            {
                var point=sceneCamera.WorldToScreenPoint(asteroidViews[a.Id].position);if(point.z<0 || !sceneCamera.pixelRect.Contains(point))continue;
                var pos=new Vector2(point.x/scale,(Screen.height-point.y)/scale);bool selected=a.Id==selectedAsteroidId;
                float radius=Mathf.Max(4,AsteroidScreenRadius(a.Id)/scale+2);
                if(selected || spaceZoom<50)DrawTargetBrackets(new Rect(pos.x-radius,pos.y-radius,radius*2,radius*2),selected?Amber:new Color(.42f,.36f,.24f,.6f));
                if(selected)
                {
                    string label="Asteroid #"+a.Id+(simulation.IsAsteroidVisible(PlayerFactionId,a)?" · "+a.RemainingOre+" Erz":" · unerkundet");
                    Vector2 size=smallStyle.CalcSize(new GUIContent(label));
                    GUI.Label(new Rect(pos.x+radius+6,pos.y-size.y*.5f,size.x+6,size.y+4),label,smallStyle);
                }
            }
        }
        private void DrawMiningPanel()
        {
            miningPanel=true;
            WorkspaceHeading("Asteroidenabbau","Vorkommen auswählen, erkunden und einen Frachter mit Heimathafen beauftragen.");
            if(simulation.State.Asteroids.Count==0){GUILayout.Label("In diesem Sternsystem ist kein abbaubarer Gürtel kartiert.",smallStyle);return;}
            foreach(var belt in simulation.State.AsteroidBelts)GUILayout.Label("Gürtel · "+(belt.InnerRadius/10).ToString("0.0")+"–"+(belt.OuterRadius/10).ToString("0.0")+" AE",smallStyle);
            int pages=(simulation.State.Asteroids.Count+3)/4;
            GUILayout.BeginHorizontal();if(WorkspaceButton("‹ Zurück"))asteroidPage=(asteroidPage+pages-1)%pages;
            GUILayout.Label((asteroidPage+1)+" / "+pages,smallStyle);if(WorkspaceButton("Weiter ›"))asteroidPage=(asteroidPage+1)%pages;GUILayout.EndHorizontal();
            for(int i=asteroidPage*4;i<Mathf.Min(asteroidPage*4+4,simulation.State.Asteroids.Count);i++)
            {
                var a=simulation.State.Asteroids[i];bool known=simulation.IsAsteroidVisible(PlayerFactionId,a);
                if(WorkspaceButton("#"+a.Id+" · "+(known?a.RemainingOre+" Erz":"erkunden"),selectedAsteroidId==a.Id))selectedAsteroidId=a.Id;
            }
            var selected=simulation.State.Asteroids.Find(a=>a.Id==selectedAsteroidId);if(selected==null){GUILayout.Label("Wähle ein Vorkommen aus der Liste.",smallStyle);return;}
            GUILayout.Label("Asteroid #"+selected.Id+" · "+selected.DiameterKm.ToString("0.0")+" km",smallStyle);
            if(WorkspaceButton("Vorkommen ansehen"))FocusAsteroid(selected.Id);
            if(WorkspaceButton("Brocken aus der Nähe"))InspectAsteroid(selected.Id);
            bool canExplore=GUI.enabled;GUI.enabled=canExplore && selectedShips.Count>0;
            if(WorkspaceButton("Ausgewählte Schiffe hinschicken"))foreach(long id in selectedShips)RequestAsteroidApproach(id,selected.Id);
            GUI.enabled=canExplore;
            bool visible=simulation.IsAsteroidVisible(PlayerFactionId,selected);
            GUILayout.Label(visible?"Verfügbar: "+(selected.RemainingOre-selected.ReservedOre)+" Erz":"Für Bestandsdaten zunächst erkunden.",smallStyle);
            var port=ChoosePort();bool before=GUI.enabled;
            GUI.enabled=before && visible && port!=null && selectedShips.Exists(id=>simulation.State.Ships.Exists(s=>s.Id==id && s.Kind==ShipKind.Freighter && s.Mining==null && s.Freight==null));
            if(WorkspaceButton("Erz abbauen und heimbringen"))foreach(long id in selectedShips)
                if(simulation.State.Ships.Exists(s=>s.Id==id && s.Kind==ShipKind.Freighter))RequestSpaceOrder(CommandKind.MineAsteroid,id,selected.Id,port.PlanetId,amount:40,destination:port.Id);
            GUI.enabled=before;
            if(WorkspaceButton("Abbau / Fracht abbrechen"))foreach(long id in selectedShips)RequestSpaceOrder(CommandKind.CancelFreight,id);
            GUILayout.Label("Frachter mit Bergbaudrohnen · 40 Erz pro Flug · endliches Vorkommen. Rückflug und Hafenentladung sind Teil des Auftrags.",smallStyle);
        }
    }
}
