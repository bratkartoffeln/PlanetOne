using System;
using System.Collections.Generic;
using System.IO;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    /// <summary>Presentation and input adapter. All gameplay state lives in GameSimulation.</summary>
    public sealed partial class PrototypeController : MonoBehaviour
    {
        private static readonly Color Background = new Color(0.025f, 0.041f, 0.065f);
        private static readonly Color Panel = new Color(0.043f, 0.065f, 0.093f);
        private static readonly Color Border = new Color(0.13f, 0.21f, 0.27f);
        private static readonly Color Ink = new Color(0.86f, 0.93f, 0.95f);
        private static readonly Color Muted = new Color(0.61f, 0.70f, 0.77f);
        private static readonly Color Cyan = new Color(0.37f, 0.83f, 0.82f);
        private static readonly Color Amber = new Color(0.94f, 0.66f, 0.32f);
        private static readonly Color Danger = new Color(0.94f, 0.37f, 0.36f);

        private GameSimulation simulation;
        private Camera sceneCamera;
        private Transform spaceRoot;
        private Transform surfaceRoot;
        private Transform ghost;
        private Transform selectedMarker;
        private readonly Dictionary<int, Transform> planetViews = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Transform> orbitViews = new Dictionary<int, Transform>();
        private readonly Dictionary<long, Transform> buildingViews = new Dictionary<long, Transform>();
        private readonly Dictionary<long, Renderer> buildingIndicators = new Dictionary<long, Renderer>();
        private readonly Dictionary<int, long> buildingPickIds = new Dictionary<int, long>();
        private readonly Dictionary<long, Transform> shipmentViews = new Dictionary<long, Transform>();
        private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private readonly List<long> obsoleteShipments = new List<long>();
        private readonly List<long> obsoleteBuildings = new List<long>();
        private readonly Dictionary<int, FoodRateSample> foodRates = new Dictionary<int, FoodRateSample>();
        private Transform[] roadViews;
        private int openPlanetId;
        private long selectedBuildingId;
        private BuildingKind? placement;
        private bool placingRoad;
        private bool removingRoad;
        private bool showingDetails;
        private int hoverX = -1;
        private int hoverY = -1;
        private string placementReason;
        private string notification;
        private string lastSimulationMessage;
        private float notificationUntil;
        private float spaceZoom = 18f;
        private float surfaceZoom = 8f;
        private Vector3 surfaceFocus;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private Shader worldShader;
        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle mutedStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;
        private Vector2 sidebarScroll;
        private string smokeSavePath;

        private sealed class FoodRateSample
        {
            public long Tick;
            public int Produced;
            public double PerMinute;
            public bool HasMeasurement;
        }

        public GameSimulation Simulation { get { return simulation; } }
        public int OpenPlanetId { get { return openPlanetId; } }
        public long SelectedBuildingId { get { return selectedBuildingId; } }
        public int RenderedBuildingCount { get { return buildingViews.Count; } }
        internal Transform ShipmentModel(long id){return shipmentViews[id];}
        public int RenderedShipmentCount { get { int count=0;foreach(var view in shipmentViews.Values)if(view.gameObject.activeInHierarchy)count++;return count; } }
        public int PlayerFactionId { get { return simulation.State.PlayerFactionId; } }
        public static string SavePath { get { return Path.Combine(Application.persistentDataPath, "prototype-save.json"); } }
        public string ActiveSavePath { get { return smokeSavePath ?? (ActiveSlotId!=null?saveSlots.PathFor(ActiveSlotId):(simulation.State.DiplomacyScenario && simulation.State.SpaceTestFixture ? Path.Combine(Application.persistentDataPath, "diplomacy-fixture-save.json") : simulation.State.DiplomacyScenario ? Path.Combine(Application.persistentDataPath, "diplomacy-save.json") : simulation.State.AiScenario ? Path.Combine(Application.persistentDataPath, "ai-scenario-save.json") : simulation.State.InvasionTestFixture ? Path.Combine(Application.persistentDataPath, "invasion-fixture-save.json") : simulation.State.SpaceTestFixture ? Path.Combine(Application.persistentDataPath, "space-fixture-save.json") : simulation.State.MilitaryTestFixture ? Path.Combine(Application.persistentDataPath, "military-fixture-save.json") : SavePath)); } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindFirstObjectByType<PrototypeController>() == null)
                new GameObject("PlanetOne").AddComponent<PrototypeController>();
        }

        private void Awake()
        {
            LoadPresentationSettings();
            simulation = GameSimulation.CreateDefault();
            Application.targetFrameRate = 60;
            worldShader = Resources.Load<Shader>("PlanetOneSurface");
            if (worldShader == null) worldShader = Shader.Find("Unlit/Color");
            var cameraObject = new GameObject("Prototype camera");
            cameraObject.transform.SetParent(transform, false);
            sceneCamera = cameraObject.AddComponent<Camera>();
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = Background;
            sceneCamera.orthographic = true;
            sceneCamera.nearClipPlane = 0.1f;
            sceneCamera.farClipPlane = 1400f;
            sceneCamera.tag = "MainCamera";
            sceneCamera.cullingMask = ~((1 << 30) | (1 << 29));
            var sun=new GameObject("Sonnenlicht").AddComponent<Light>();sun.transform.SetParent(transform,false);
            sun.type=LightType.Directional;sun.intensity=1.35f;sun.color=new Color(1,0.91f,0.79f);sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(0.32f,0.39f,0.47f);RenderSettings.ambientEquatorColor=new Color(0.18f,0.20f,0.23f);RenderSettings.ambientGroundColor=new Color(0.11f,0.09f,0.06f);
            QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=70;QualitySettings.shadowResolution=ShadowResolution.High;
            var backdrop = new GameObject("Vollbild-Hintergrund").AddComponent<Camera>();
            backdrop.transform.SetParent(transform,false);backdrop.depth=-100;backdrop.cullingMask=0;
            backdrop.clearFlags=CameraClearFlags.SolidColor;backdrop.backgroundColor=Background;
            BuildSpace();
            SetView(0);
            Notify("Willkommen. Öffne einen Planeten, um seine Kolonie aufzubauen.");
            InitializeSessions();
            if (PrototypeSmokeRunner.IsRequested) gameObject.AddComponent<PrototypeSmokeRunner>();
        }

        private void Update()
        {
            if (simulation == null) return;
            Shader.SetGlobalFloat("_PlanetOneSeconds",(float)(simulation.State.Tick*Definitions.TickSeconds));
            HandleKeyboard();
            if(hasSession && !menuOpen && !settingsOpen)simulation.Advance(Time.unscaledDeltaTime);
            UpdateFoodRates();
            if (!string.IsNullOrEmpty(simulation.State.LastMessage) && simulation.State.LastMessage != lastSimulationMessage)
            {
                lastSimulationMessage = simulation.State.LastMessage;
                Notify(lastSimulationMessage);
            }
            UpdateSpace();
            if(surfaceBuildSteps!=null)AdvanceSurfaceBuild();
            else if (openPlanetId != 0 && surfaceRoot!=null) UpdateSurface();
            if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height || sceneCamera.pixelRect!=WorldViewport) UpdateCamera();
            HandleWorldInput();
            UpdateViewFlight();
            UpdatePlanetFocus();
            UpdateCombatVisuals();
            UpdateOrbitLineWidths();
        }

        private void OnDestroy()
        {
            if(!PrototypeSmokeRunner.IsRequested&&Application.isFocused)Cursor.visible=true;
            if (hexMesh != null) Destroy(hexMesh);
            foreach(var mesh in beltDustMeshes)Destroy(mesh);if(beltDustMaterial!=null)Destroy(beltDustMaterial);
            foreach(var mesh in modelMeshes.Values)Destroy(mesh);
            foreach(var material in celestialMaterials.Values)Destroy(material);
            foreach(var icon in hudIcons.Values)Destroy(icon);
            foreach(var portrait in objectPortraits.Values)Destroy(portrait);
            if(starfieldMesh!=null)Destroy(starfieldMesh);if(starfieldMaterial!=null)Destroy(starfieldMaterial);
            if(atmosphereMaterial!=null)Destroy(atmosphereMaterial);if(coronaMaterial!=null)Destroy(coronaMaterial);
            foreach (var material in materials.Values) Destroy(material);
            foreach(var material in artMaterials.Values)Destroy(material);
            foreach(var icon in buildingIcons.Values)Destroy(icon);
            if(domeGlass!=null)Destroy(domeGlass);
            if(trailMaterial!=null)Destroy(trailMaterial);
            if(reliefMesh!=null)Destroy(reliefMesh);
            if(reliefMap!=null)Destroy(reliefMap);
            if(bridgePanelTexture!=null)Destroy(bridgePanelTexture);if(glassPanelTexture!=null)Destroy(glassPanelTexture);
            if(panelTexture!=null)Destroy(panelTexture);if(buttonTexture!=null)Destroy(buttonTexture);if(hoverTexture!=null)Destroy(hoverTexture);
        }

        public bool HandleShortcut(KeyCode key)
        {
            if(viewFlightActive){if(key==KeyCode.Escape){FinishViewFlight();return true;}return false;}
            if(IsCommandMenuOpen){if(key==KeyCode.Escape){CloseCommandMenu();return true;}return false;}
            if (key==KeyCode.Escape) { if(settingsOpen)CloseSettings(); else if(HandleSessionEscape()){} else if(placement.HasValue || placingRoad || removingRoad || planningSupply || militaryTargetMode.HasValue || spaceMoveTargetMode) {CancelPlacement();militaryTargetMode=null;spaceMoveTargetMode=false;} else if(panelOpen){panelOpen=false;contextOpen=true;} else if(SelectionCardVisible)contextOpen=false; else ShowSessionPage(SessionPage.Home); selectionStart=null; shipSelectionStart=null; return true; }
            if(settingsOpen || menuOpen)return false;
            if(key==KeyCode.L && openPlanetId!=0){ToggleSupplyOverlay();return true;}
            if(key==KeyCode.Space){TogglePause();return true;}
            if(key==KeyCode.F5)return SaveGame();
            if(key==KeyCode.F9)return LoadGame();
            if(key==KeyCode.Tab && openPlanetId!=0)return BeginViewFlight(0);
            return false;
        }
        private void HandleKeyboard()
        {
            if(Input.GetKeyDown(KeyCode.Escape)){HandleShortcut(KeyCode.Escape);return;}
            if(settingsOpen || menuOpen || IsCommandMenuOpen || viewFlightActive)return;
            HandleOrbitCamera();
            if(Input.GetKeyDown(KeyCode.Space))HandleShortcut(KeyCode.Space);
            if(Input.GetKeyDown(KeyCode.L))HandleShortcut(KeyCode.L);
            if(Input.GetKeyDown(KeyCode.F5))HandleShortcut(KeyCode.F5);
            if(Input.GetKeyDown(KeyCode.F9))HandleShortcut(KeyCode.F9);
            if(Input.GetKeyDown(KeyCode.Tab))HandleShortcut(KeyCode.Tab);
            HandleMilitaryKeyboard();
            if (openPlanetId != 0 || !spacePanCaptured)
            {
                Vector3 pan = Vector3.zero;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) pan.z += 1;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) pan.z -= 1;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) pan.x -= 1;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) pan.x += 1;
                if (pan.sqrMagnitude > 0)
                {
                    var delta=Quaternion.Euler(0,WorldCameraYaw,0)*pan.normalized*Time.unscaledDeltaTime*8f;
                    if(openPlanetId==0)SetSpaceFocus(spaceFocus+delta);
                    else
                    {
                        SetSurfaceFocus(surfaceFocus+delta);
                    }
                    UpdateCamera();
                }
            }
        }

        private void HandleWorldInput()
        {
            if(HandleCommandMenuPointer())return;
            if(HandleSpacePanInput())return;
            if (!IsWorldPoint(Input.mousePosition))
            {
                if (ghost != null) ghost.gameObject.SetActive(false);
                if (placementModel != null) placementModel.gameObject.SetActive(false);
                return;
            }
            float wheel = Input.mouseScrollDelta.y;
            if (Math.Abs(wheel) > 0.001f)
            {
                if (openPlanetId == 0) {CancelPlanetFocus();spaceZoom = Mathf.Clamp(spaceZoom * Mathf.Pow(0.85f,wheel), .25f, 200f);}
                else surfaceZoom = Mathf.Clamp(surfaceZoom - wheel * 0.5f, 3f, 24f);
                UpdateCamera();
            }
            if (openPlanetId == 0)
            {
                if (HandleSpacePointer()) return;
                if (!Input.GetMouseButtonDown(0)) return;
                int chosen = 0;
                float nearest = float.MaxValue;
                foreach (var pair in planetViews)
                {
                    if (!simulation.IsPlanetKnown(PlayerFactionId, pair.Key)) continue;
                    Vector3 screen = sceneCamera.WorldToScreenPoint(pair.Value.position);
                    float distance = Vector2.Distance(new Vector2(screen.x, screen.y), Input.mousePosition);
                    if (distance < Mathf.Max(24,PlanetScreenRadius(pair.Key)+8) && distance < nearest) { nearest = distance; chosen = pair.Key; }
                }
                if (chosen != 0)
                {SelectPlanet(chosen);if(lastClickedPlanet==chosen && Time.unscaledTime-lastPlanetClick<0.35f)OpenSelectedPlanet();lastClickedPlanet=chosen;lastPlanetClick=Time.unscaledTime;}
                else SelectStarAtScreen(Input.mousePosition);
                return;
            }

            var hoverCell = SurfaceCellAtScreen(Input.mousePosition);
            hoverX = hoverCell.X; hoverY = hoverCell.Y;
            PlanetState planetState = simulation.FindPlanet(openPlanetId);
            bool inside = hoverX >= 0 && hoverY >= 0 && hoverX < planetState.Width && hoverY < planetState.Height;
            if(HandleSupplyPointer(hoverX,hoverY))return;
            if (!placement.HasValue && !placingRoad && !removingRoad && HandleMilitaryPointer(planetState, inside)) return;
            if (placement.HasValue || placingRoad || removingRoad)
            {
                bool valid = ValidatePlacementTarget(planetState,hoverX,hoverY,inside);
                UpdatePlacementPreview(hoverX,hoverY,inside,valid);
                if (Input.GetMouseButtonDown(0))
                {
                    if (!valid) { Notify(placementReason); return; }
                    if (placingRoad || removingRoad) RequestRoadChange(openPlanetId, hoverX, hoverY, removingRoad);
                    else RequestConstruction(openPlanetId, placement.Value, hoverX, hoverY);
                    if (simulation.State.Paused) Notify("Bauauftrag vorgemerkt – mit Leertaste fortsetzen.");
                }
            }
            else
            {
                ghost.gameObject.SetActive(false);
            }
        }

        private void BuildSpace()
        {
            ResetPlanetFocus();
            if (spaceRoot != null) {spaceRoot.gameObject.SetActive(false);Destroy(spaceRoot.gameObject);}
            spaceRoot = new GameObject("Sonnensystem").transform;
            spaceRoot.SetParent(transform, false);
            planetViews.Clear(); shipViews.Clear(); selectedShips.Clear();
            if(simulation.FindPlanet(selectedPlanetId)==null || !simulation.IsPlanetKnown(PlayerFactionId,selectedPlanetId))
            {
                var known=simulation.State.Planets.Find(p=>p.OwnerId==PlayerFactionId) ?? simulation.State.Planets.Find(p=>simulation.IsPlanetKnown(PlayerFactionId,p.Id));
                selectedPlanetId=known==null?0:known.Id;
            }
            orbitViews.Clear();
            var star=MakePrimitive(PrimitiveType.Sphere,"Stern",spaceRoot,Vector3.zero,Vector3.one*StarDiameter,new Color(1,0.50f,0.055f));CreateStarCorona(star);
            spaceFocus=Vector3.zero;BuildSpaceBackdrop();
            float maxOrbit = 12f;
            foreach (PlanetState planet in simulation.State.Planets)
            {
                bool isMoon = planet.ParentId > 0;
                Transform body = CreateCelestial(planet,spaceRoot,Vector3.zero,PlanetDiameter(planet));
                planetViews[planet.Id] = body;
                var orbit = MakeLine("Umlaufbahn " + planet.Name, spaceRoot, new Color(0.13f,0.27f,0.36f), 0.018f, true);
                orbit.positionCount = 96;
                for (int i = 0; i < 96; i++)
                {
                    var orbitPoint=OrbitalMotion.PositionAtMeanAnomaly(planet,i*OrbitalMotion.Tau/96);
                    orbit.SetPosition(i,SpacePosition(orbitPoint.X,orbitPoint.Y));
                }
                orbitViews[planet.Id] = orbit.transform;
                if (!isMoon && simulation.IsPlanetKnown(PlayerFactionId,planet.Id)) maxOrbit = Mathf.Max(maxOrbit, (float)(planet.OrbitRadius*(1+planet.OrbitEccentricity)));
            }
            BuildAsteroidViews();
            spaceZoom = maxOrbit*SpaceDistanceScale*0.73f+2;
            UpdateSpace();
        }

        private void UpdateSpace()
        {
            UpdateAsteroidViews();
            UpdateShipViews();
            UpdateInvasionViews();
            foreach (PlanetState planet in simulation.State.Planets)
            {
                bool known = simulation.IsPlanetKnown(PlayerFactionId, planet.Id);
                planetViews[planet.Id].gameObject.SetActive(known); orbitViews[planet.Id].gameObject.SetActive(known);
                SimPoint point = simulation.GetOrbitPosition(planet.Id);
                planetViews[planet.Id].position = SpacePosition(point.X,point.Y);
                planetViews[planet.Id].rotation=PlanetAxialRotation(planet.Id,simulation.State.Tick*Definitions.TickSeconds,planet.Kind);
                celestialMaterials[planet.Id].SetVector("_SunDirection",new Vector4(-(float)point.X,2,-(float)point.Y,0));
                if (planet.ParentId > 0)
                {
                    SimPoint parent = simulation.GetOrbitPosition(planet.ParentId);
                    orbitViews[planet.Id].position = SpacePosition(parent.X,parent.Y);
                }
            }
        }

        public void SetView(int planetId)
        {
            if (planetId != 0 && simulation.FindPlanet(planetId) == null) return;
            if (planetId != 0 && !simulation.IsPlanetKnown(PlayerFactionId, planetId)) { Notify("Oberfläche ist noch unbekannt."); return; }
            if(planetId!=0 && openPlanetId==0 && !switchingFlight){returnSpaceFocus=spaceFocus;returnSpaceZoom=spaceZoom;}
            if(planetId==0 && openPlanetId!=0){spaceFocus=returnSpaceFocus;spaceZoom=returnSpaceZoom;}
            if(!switchingFlight){viewFlightActive=false;ResetPlanetFocus();}
            surfaceBuildSteps=null;
            ClearCombatVisuals();CloseCommandMenu();openPlanetId = planetId;
            CancelSpacePan();spaceMoveTargetMode=false;suppressShipSelectionUntilUp=false;
            if(planetId!=0)selectedPlanetId=planetId;
            panelOpen=false;contextOpen=true;presentationSection=planetId==0?0:1;
            showingSpacePanel=false;showingDiplomacy=false;showingMilitary=false;
            selectedBuildingId = 0;
            showingDetails = false;
            CancelPlacement();
            spaceRoot.gameObject.SetActive(planetId == 0);
            if (surfaceRoot != null) Destroy(surfaceRoot.gameObject);
            if(reliefMesh!=null)Destroy(reliefMesh);reliefCollider=null;
            surfaceRoot = null;
            ghost = null;
            selectedMarker = null;
            buildingViews.Clear();
            buildingIndicators.Clear();
            buildingPickIds.Clear();
            shipmentViews.Clear();
            unitViews.Clear(); selectedUnits.Clear(); militaryTargetMode = null;
            sidebarScroll = Vector2.zero;
            if (planetId != 0)
            {
                PlanetState planet = simulation.FindPlanet(planetId);
                var headquarters=planet.Buildings.Find(b=>b.Kind==BuildingKind.Headquarters);
                surfaceFocus = SurfacePosition(headquarters==null?8:headquarters.X,0,headquarters==null?6:headquarters.Y);
                surfaceZoom = 6.2f;
                BuildSurface(planet);
            }
            UpdateCamera();
        }

        private void BuildSurface(PlanetState planet)
        {
            var steps=BuildSurfaceSteps(planet);
            if(buildSurfaceInFrames){surfaceBuildSteps=steps;return;}
            while(steps.MoveNext()){}
        }

        private System.Collections.IEnumerator surfaceBuildSteps;
        private bool buildSurfaceInFrames;
        private double surfaceBuildSliceMilliseconds;
        private void AdvanceSurfaceBuild()
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            do {if(!surfaceBuildSteps.MoveNext()){surfaceBuildSteps=null;break;}}while(watch.Elapsed.TotalMilliseconds<4);
            surfaceBuildSliceMilliseconds=Math.Max(surfaceBuildSliceMilliseconds,watch.Elapsed.TotalMilliseconds);
            if(viewFlightActive)LastFlightSurfaceBuildMilliseconds=(Time.realtimeSinceStartup-flightBuildStarted)*1000;
        }

        private System.Collections.IEnumerator BuildSurfaceSteps(PlanetState planet)
        {
            surfaceRoot = new GameObject("Oberfläche " + planet.Name).transform;
            surfaceRoot.SetParent(transform, false);
            roadViews = new Transform[planet.Width * planet.Height];
            roadVisualHash=0;
            var relief=BuildReliefSteps(planet);while(relief.MoveNext())yield return relief.Current;
            for (int y = 0; y < planet.Height; y++)
            for (int x = 0; x < planet.Width; x++)
            {
                int terrain = planet.Terrain[y * planet.Width + x];
                if (terrain == 1)
                {
                    if(planet.Kind==PlanetKind.Temperate || planet.Kind==PlanetKind.Ocean)BuildTree(surfaceRoot,SurfacePosition(x,0,y),.9f,x+y);
                    else for(int i=0;i<3;i++)Rock(surfaceRoot,SurfacePosition(x+(i-1)*0.21f,0.1f,y+i*0.09f),new Vector3(0.48f,0.45f+i*0.17f,0.6f),x+y*planet.Width+i);
                }
                if (terrain == 3)
                {
                    for(int i=0;i<4;i++)
                    {var crystal=HullPart(surfaceRoot,SurfacePosition(x+(i-1.5f)*0.13f,0.19f,y),new Vector3(0.16f,0.16f,0.4f+i*0.08f),new Color(0.6f,0.4f,0.9f));crystal.localRotation=Quaternion.Euler(-70+i*9,0,i*21);}
                }
                if (terrain == 2)
                {
                    for(int i=0;i<4;i++)Rock(surfaceRoot,SurfacePosition(x+(i-1.5f)*0.13f,0.03f,y+(i%2)*0.17f),new Vector3(0.28f,0.22f,0.3f),x+y+i);
                }
                if((y*planet.Width+x)%16==15)yield return null;
            }
            ghost = MakeHexOutline("Bauvorschau", surfaceRoot, Cyan, 0.055f);
            selectedMarker = MakeHexOutline("Auswahl", surfaceRoot, Cyan, 0.045f);
            ghost.gameObject.SetActive(false);
            selectedMarker.gameObject.SetActive(false);
            var pendingBuildings=planet.Buildings.ToArray();
            for(int i=0;i<pendingBuildings.Length;i++)
            {
                var building=pendingBuildings[i];buildingViews[building.Id]=BuildBuilding(building);
                if(i%4==3)yield return null;
            }
            UpdateSurface();
        }

        private void UpdateSurface()
        {
            PlanetState planet = simulation.FindPlanet(openPlanetId);
            UpdateGroundTrails(planet);
            obsoleteBuildings.Clear();
            foreach (long id in buildingViews.Keys) obsoleteBuildings.Add(id);
            foreach (BuildingState building in planet.Buildings)
            {
                obsoleteBuildings.Remove(building.Id);
                Transform view;
                if(IsFortification(building.Kind)&&buildingViews.TryGetValue(building.Id,out view))
                {
                    int shape=WallShape(planet,building.X,building.Y,GameSimulation.BuildingOwner(planet,building)),old;
                    if(!wallShapes.TryGetValue(building.Id,out old)||old!=shape)
                    {
                        var collider=view.GetComponent<Collider>();if(collider!=null)buildingPickIds.Remove(collider.GetInstanceID());
                        view.gameObject.SetActive(false);Destroy(view.gameObject);buildingViews.Remove(building.Id);buildingIndicators.Remove(building.Id);
                    }
                }
                if (!buildingViews.TryGetValue(building.Id, out view))
                {
                    view = BuildBuilding(building);
                    buildingViews[building.Id] = view;
                    if(IsFortification(building.Kind))wallShapes[building.Id]=WallShape(planet,building.X,building.Y,GameSimulation.BuildingOwner(planet,building));
                }
                UpdateConstructionVisual(view,building);
                view.gameObject.SetActive(simulation.IsCellVisible(planet,PlayerFactionId,building.X,building.Y) || GameSimulation.BuildingOwner(planet,building)==PlayerFactionId);
                Renderer indicator;
                if (buildingIndicators.TryGetValue(building.Id, out indicator))
                    indicator.sharedMaterial = MaterialFor(building.Deconstructing ? Amber : (!building.Enabled ? Muted : (building.CycleActive ? Cyan : BuildingColor(building.Kind))));
            }
            foreach (long id in obsoleteBuildings)
            {
                Transform removed = buildingViews[id];
                Collider collider = removed.GetComponent<Collider>();
                if (collider != null) buildingPickIds.Remove(collider.GetInstanceID());
                Destroy(removed.gameObject);
                buildingViews.Remove(id);
                buildingIndicators.Remove(id);
                if (selectedBuildingId == id) selectedBuildingId = 0;
            }
            visibleCouriers.Clear();
            obsoleteShipments.Clear();
            foreach (long id in shipmentViews.Keys) obsoleteShipments.Add(id);
            foreach (ShipmentState shipment in planet.Shipments)
            {
                Transform view;
                if(GameSimulation.GroundCourierDocked(shipment))
                {
                    if(shipmentViews.TryGetValue(shipment.Id,out view)){view.gameObject.SetActive(false);view.GetComponent<VehicleModelSize>().DisplayOffset=Vector2.zero;}
                    obsoleteShipments.Remove(shipment.Id);continue;
                }
                if (!shipmentViews.TryGetValue(shipment.Id, out view))
                {
                    view=new GameObject("Gütertransporter").transform;view.SetParent(surfaceRoot,false);
                    BuildGroundLogisticsModel(view,shipment);
                    shipmentViews[shipment.Id] = view;
                }
                obsoleteShipments.Remove(shipment.Id);
                view.gameObject.SetActive(true);
                UpdateShipmentPose(view,shipment);
                visibleCouriers.Add(shipment);
            }
            SeparateCourierModels();
            foreach (long id in obsoleteShipments)
            {
                Destroy(shipmentViews[id].gameObject);
                shipmentViews.Remove(id);
            }
            BuildingState selected = FindSelected(planet);
            selectedMarker.gameObject.SetActive(selected != null);
            if (selected != null) ConformMarker(selectedMarker,selected.X,selected.Y);
            UpdateMilitaryViews(planet);
        }

        private Transform BuildBuilding(BuildingState building)
        {
            var root = new GameObject(Definitions.Get(building.Kind).Name).transform;
            root.SetParent(surfaceRoot, false);
            root.localPosition = SurfacePosition(building.X, 0, building.Y);
            float ground=root.localPosition.y;
            for(int i=0;i<6;i++){Vector3 corner=root.position+HexCorner(i,0.89f);ground=Mathf.Max(ground,ReliefHeight(corner.x,corner.z));}
            root.localPosition=new Vector3(root.localPosition.x,ground+0.015f,root.localPosition.z);
            for(int i=0;i<6&&!IsFortification(building.Kind);i++)
            {Vector3 foot=HexCorner(i,0.79f),world=root.position+foot;float depth=root.position.y-ReliefHeight(world.x,world.z);if(depth>0.05f)Rod(root,"Fundamentstütze",foot-Vector3.up*depth,foot+Vector3.up*0.04f,0.1f,Steel);}
            BoxCollider pickCollider = root.gameObject.AddComponent<BoxCollider>();
            pickCollider.center = new Vector3(0, 0.55f, 0);
            pickCollider.size = new Vector3(0.90f, 1.1f, 0.90f);
            buildingPickIds[pickCollider.GetInstanceID()] = building.Id;

            Transform status = MakePrimitive(PrimitiveType.Cube, "Betriebsanzeige", root, new Vector3(0, 0.17f, -0.39f), new Vector3(0.36f, 0.055f, 0.055f), Cyan);
            buildingIndicators[building.Id] = status.GetComponent<Renderer>();
            if(AtmosphereSurface)BuildFloatingPlatform(root);
            if(IsFortification(building.Kind))BuildConnectedWall(root,building.Kind,building.X,building.Y,GameSimulation.BuildingOwner(simulation.FindPlanet(openPlanetId),building));
            else PopulateBuildingModel(root,building.Kind);
            AttachConstructionVisual(root,building);
            return root;
        }

        private void PopulateBuildingModel(Transform root,BuildingKind kind)
        {
            BuildDetailedModel(root,kind);
        }

        private void UpdateCamera()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            sceneCamera.pixelRect = WorldViewport;
            if (openPlanetId == 0)
            {
                sceneCamera.transform.position = spaceFocus+Quaternion.Euler(Mathf.Clamp(cameraPitch-15,25,70),cameraYaw,0)*new Vector3(0,0,-Mathf.Max(300,spaceZoom*3));
                sceneCamera.transform.LookAt(spaceFocus);
                sceneCamera.orthographicSize = spaceZoom;
            }
            else
            {
                sceneCamera.transform.position = surfaceFocus + Quaternion.Euler(cameraPitch,WorldCameraYaw,0)*new Vector3(0,0,-30);
                sceneCamera.transform.LookAt(surfaceFocus);
                sceneCamera.orthographicSize = surfaceZoom;
            }
            UpdateSpaceBackdropCamera();
        }

        private float UiScale { get { return Mathf.Min(presentation.Scale, Mathf.Min(Screen.width / 1200f, Screen.height / 820f)); } }

        private void OnGUI()
        {
            if (simulation == null) return;
            if(!settingsOpen && !menuOpen && !IsCommandMenuOpen && commandDismissFrame!=Time.frameCount) { HandleSelectionEvents(); HandleSpaceEvents(); }
            EnsureStyles();
            float scale = UiScale;
            Matrix4x4 original = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            PreparePresentationSkin();
            hoveredProductionTip=null;
            productionPointerInContent=false;
            bool previousEnabled=GUI.enabled;
            GUI.enabled=previousEnabled && !viewFlightActive && !settingsOpen && !menuOpen && !IsCommandMenuOpen && commandDismissFrame!=Time.frameCount;
            if(hasSession)
            {
            if (openPlanetId == 0) { DrawPlanetNameplates(scale); DrawShipLabels(scale); DrawAsteroidLabels(scale); }
            else { DrawBuildingLabels(scale); DrawMilitaryLabels(scale); }
            DrawUnitSelectionFrames(scale);
            DrawOrderArrows(scale);
            DrawSupplyNetwork(scale);
            DrawPresentationTop(width);
            DrawPresentationNavigation(width,height);
            DrawSidebar(width, height);
            DrawPresentationBottom(width, height);
            DrawSelectionCard(width,height);
            }
            GUI.enabled=previousEnabled;
            DrawViewFlight(width,height);
            DrawPresentationDialogs(width,height);
            DrawCommandMenu();
            if(GUI.enabled && !menuOpen && !settingsOpen && !IsCommandMenuOpen) ProductionTooltip(width,height);
            DrawWorldPointer(scale);
            GUI.matrix = original;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) {ApplyTypography();return;}
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = Ink;
            headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = true };
            headingStyle.normal.textColor = Ink;
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            bodyStyle.normal.textColor = Ink;
            mutedStyle = new GUIStyle(bodyStyle); mutedStyle.normal.textColor = Muted;
            smallStyle = new GUIStyle(bodyStyle) { fontSize = 11 }; smallStyle.normal.textColor = Muted;
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 10, 4, 4) };
            buttonStyle.normal.textColor = Ink;
            selectedButtonStyle = new GUIStyle(buttonStyle); selectedButtonStyle.normal.textColor = Cyan;
            ApplyTypography();
        }


        private int ProductionColumns {get{return Mathf.Clamp((int)(SidebarContentWidth/(124*TextScale)),2,3);}}
        private Rect ProductionRect(int columns)
        {
            float w=Mathf.Floor((SidebarContentWidth-columns*5)/columns);
            return GUILayoutUtility.GetRect(w,72+(smallStyle.lineHeight+4)*3,GUILayout.Width(w));
        }
        private void DrawBuildButton(BuildingKind kind,int columns,int[] stock)
        {
            var definition=Definitions.Get(kind);
            var missing=new int[stock.Length];
            for(int r=0;r<stock.Length;r++) missing[r]=Math.Max(0,definition.Cost[r]-stock[r]);
            string locked=simulation.BuildingTechnologyError(kind,PlayerFactionId);
            Rect tile=ProductionRect(columns);
            BuildingState active=null;int jobs=0;
            foreach(var site in simulation.FindPlanet(openPlanetId).Buildings)
                if(site.Kind==kind && !site.Built && !site.Destroyed && !site.Deconstructing)
                {active=site;jobs++;}
            if(productionPointerInContent && jobs==1 && Event.current.type==EventType.MouseDown && Event.current.button==1 && tile.Contains(Event.current.mousePosition))
            {
                RequestBuildingOperation(openPlanetId,active.Id,active.Enabled?CommandKind.SetBuildingEnabled:CommandKind.CancelConstruction,0,false);
                Event.current.Use();
            }
            bool selected=placement==kind;
            bool stopped=active!=null && (!active.Enabled || active.Status=="Baufahrzeug: Anfahrt unterbrochen");
            float progress=active==null?0:(float)(active.BuildProgress/Math.Max(.01,definition.BuildSeconds));
            int[] costs=definition.Cost;
            if(active!=null)
            {
                var outstanding=LocalShortfall(active,costs,active.ConstructionStarted);
                bool waiting=Array.Exists(outstanding,n=>n>0);
                stopped|=waiting;
                if(waiting)missing=outstanding;
            }
            string state=active!=null?(active.Enabled?Mathf.FloorToInt(progress*100)+" %":"Pause"):selected?"Platzieren":null;
            var tooltip=locked??(active==null?"Bauplatz wählen":active.Status);
            if(ProductionTile(tile,definition.Name,BuildingIcon(kind),costs,missing,tooltip,selected,jobs,state,progress,stopped,locked!=null))
            {                CancelPlacement();selectedBuildingId=0;
                if(jobs==1 && !active.Enabled) RequestBuildingOperation(openPlanetId,active.Id,CommandKind.SetBuildingEnabled,0,true);
                else if(locked!=null) Notify(locked);
                else if(!selected) placement=kind;
            }
        }

        private static readonly Color ShortageRed=new Color(1,.34f,.29f);
        private static readonly HudIcon[] CostIcons={HudIcon.Food,HudIcon.Ore,HudIcon.Metal,HudIcon.Parts,HudIcon.Crystal,HudIcon.Electronics};
        private static int[] LocalShortfall(BuildingState site,int[] cost,bool paid)
        {
            var missing=new int[cost.Length];
            if(!paid)for(int r=0;r<cost.Length;r++) missing[r]=Math.Max(0,cost[r]-site.Inventory[r]+site.ReservedOutgoing[r]);
            return missing;
        }

        private GUIStyle productionNameStyle, productionTipStyle;
        private string hoveredProductionTip;
        private bool productionPointerInContent;
        private bool ProductionTile(Rect tile,string name,Texture2D portrait,int[] cost,int[] missing,string detail,
            bool selected,int count,string state,float progress,bool stopped,bool locked)
        {
            bool shortage=Array.Exists(missing,n=>n>0);
            bool hot=productionPointerInContent && tile.Contains(Event.current.mousePosition);
            Fill(tile,locked?Color.gray:selected?Cyan:hot?Amber:Border);
            var inside=new Rect(tile.x+2,tile.y+2,tile.width-4,tile.height-4);
            Fill(inside,new Color(.065f,.09f,.105f));
            if(productionNameStyle==null)
            {
                productionNameStyle=new GUIStyle(smallStyle){alignment=TextAnchor.MiddleCenter,wordWrap=true,padding=new RectOffset()};
                productionNameStyle.normal.background=null;
                productionNameStyle.normal.textColor=Ink;
            }
            productionNameStyle.fontSize=FontUnitSize(14);
            float line=smallStyle.lineHeight+4;
            float titleHeight=productionNameStyle.lineHeight*2+2;
            string status=locked?"Forschung":count>0?(count>1?"×"+count+"  ":"")+state:null;
            float statusHeight=string.IsNullOrEmpty(status)?0:line;
            var title=new Rect(inside.x+2,inside.y+1,inside.width-4,titleHeight);
            var art=new Rect(inside.x+3,title.yMax+1,inside.width-6,inside.yMax-title.yMax-line-statusHeight-3);
            var tint=GUI.color;
            if(locked) GUI.color=Color.gray;
            GUI.DrawTexture(art,portrait,ScaleMode.ScaleToFit);GUI.color=tint;
            if(locked)Fill(inside,new Color(.23f,.23f,.23f,.78f));
            else if(shortage)Fill(inside,new Color(.72f,.045f,.035f,.34f));
            if(stopped) Fill(inside,new Color(.65f,.055f,.045f,.32f));
            if(count>0)
            {
                Fill(new Rect(inside.x,inside.y,inside.width*(stopped?Mathf.Max(.035f,progress):Mathf.Clamp01(progress)),inside.height),
                    stopped?new Color(.86f,.075f,.06f,.64f):new Color(.06f,.58f,.46f,.48f));
            }
            GUI.Label(title,name,productionNameStyle);
            if(statusHeight>0)
                GUI.Label(new Rect(inside.x+2,art.yMax,inside.width-4,statusHeight),status,productionNameStyle);
            var entries=Array.FindAll(cost,n=>n>0).Length;
            float slot=inside.width/Math.Max(1,entries),x=inside.x+2,y=inside.yMax-smallStyle.lineHeight-4;
            for(int r=0;r<cost.Length;r++)
            {
                if(cost[r]<=0)continue;
                float icon=Mathf.Min(smallStyle.lineHeight,slot*.43f);
                GUI.color=locked?Color.gray:missing[r]>0?ShortageRed:Color.white;
                GUI.DrawTexture(new Rect(x,y+2,icon,icon),HudSymbol(CostIcons[r]));
                GUI.Label(new Rect(x+icon+1,y,slot-icon,smallStyle.lineHeight+4),cost[r].ToString(),smallStyle);
                GUI.color=tint;x+=slot;
            }
            string tip=name+"\nKosten: "+GoodsText(cost);
            if(shortage)tip+="\nFehlt: "+GoodsText(missing);
            if(!string.IsNullOrEmpty(detail))tip+="\n"+detail;
            if(hot && Event.current.type==EventType.Repaint) hoveredProductionTip=tip;
            bool clicked=GUI.Button(tile,GUIContent.none,GUIStyle.none);
            return clicked && !locked;
        }

        private void ProductionTooltip(float width,float height)
        {
            if(Event.current.type!=EventType.Repaint || hoveredProductionTip==null) return;
            if(productionTipStyle==null)productionTipStyle=new GUIStyle(smallStyle){wordWrap=true,padding=new RectOffset(),alignment=TextAnchor.UpperLeft};
            productionTipStyle.fontSize=FontUnitSize(13);
            var text=new GUIContent(hoveredProductionTip);
            const float padX=18,padY=14;
            float w=Mathf.Min(width-16,Mathf.Clamp(productionTipStyle.CalcSize(text).x+2*padX,210,365));
            float h=productionTipStyle.CalcHeight(text,w-2*padX)+2*padY;
            Vector2 mouse=Event.current.mousePosition;
            float x=mouse.x>=w+16?mouse.x-w-12:mouse.x+12;
            var box=new Rect(Mathf.Clamp(x,4,width-w-4),Mathf.Clamp(mouse.y+12,4,height-h-4),w,h);
            DrawPanel(box);GUI.Label(new Rect(box.x+padX,box.y+padY,w-2*padX,h-2*padY),text,productionTipStyle);
        }
        private void DrawConstructionOrders(PlanetState planet)
        {
            bool first=true;
            foreach(var site in planet.Buildings)
            {
                if(site.Destroyed||site.Built||site.Deconstructing||GameSimulation.BuildingOwner(planet,site)!=PlayerFactionId)continue;
                if(first){GUILayout.Label("BAUAUFTRÄGE",headingStyle);first=false;}
                var def=Definitions.Get(site.Kind);
                float progress=Mathf.Clamp01((float)(site.BuildProgress/Math.Max(.01,def.BuildSeconds)));
                GUILayout.Label(def.Name+" · "+(site.Enabled?(site.Status??"Material angefordert"):"Angehalten"),bodyStyle);
                Rect bar=GUILayoutUtility.GetRect(1,5,GUILayout.ExpandWidth(true));
                Fill(bar,new Color(.16f,.22f,.24f));Fill(new Rect(bar.x,bar.y,bar.width*progress,bar.height),site.Enabled?Cyan:ShortageRed);
                GUILayout.BeginHorizontal();
                if(GUILayout.Button(site.Enabled?"Anhalten":"Fortsetzen",buttonStyle))
                    RequestBuildingOperation(planet.Id,site.Id,CommandKind.SetBuildingEnabled,0,!site.Enabled);
                if(!site.Enabled && GUILayout.Button("Abbrechen",buttonStyle))
                    RequestBuildingOperation(planet.Id,site.Id,CommandKind.CancelConstruction);
                GUILayout.EndHorizontal();
                Rect row=GUILayoutUtility.GetLastRect();
                if(Event.current.type==EventType.MouseDown && Event.current.button==1 && row.Contains(Event.current.mousePosition))
                {
                    RequestBuildingOperation(planet.Id,site.Id,site.Enabled?CommandKind.SetBuildingEnabled:CommandKind.CancelConstruction,0,false);
                    Event.current.Use();
                }
            }
        }
        private void DrawBuildingDetails(BuildingState building)
        {
            if(GameSimulation.BuildingOwner(simulation.FindPlanet(building.PlanetId),building)!=PlayerFactionId)
            { GUILayout.Label(Definitions.Get(building.Kind).Name+" · fremde Anlage",headingStyle);GUILayout.Label("Mit Infanterie vor Ort sichern.",bodyStyle);return; }
            bool factory=building.Built && !building.Deconstructing && (building.Kind==BuildingKind.VehicleFactory || building.Kind==BuildingKind.Barracks);
            if(factory)DrawRecruitment(building);
            if(building.Built && !building.Deconstructing && building.Kind==BuildingKind.Spaceport)DrawPortProduction(building);
            DrawSupplyControls();
            if(!factory)GUILayout.Label(Definitions.Get(building.Kind).Name.ToUpperInvariant(), headingStyle);
            GUILayout.Label(building.Status ?? (building.Built ? "Betriebsbereit" : "Im Bau"), mutedStyle);
            GUILayout.Label("Struktur: " + building.Health.ToString("0") + " / " + Definitions.BuildingHealth(building.Kind), smallStyle);
            if(!factory)DrawRecruitment(building);
            if (!building.Built && !building.Deconstructing && !building.ConstructionStarted) DrawMaterialNeeds(building, Definitions.Get(building.Kind).Cost);
            if (!building.Built)
                GUILayout.Label("Baufortschritt  " + Mathf.RoundToInt((float)(100 * building.BuildProgress / Math.Max(0.01, Definitions.Get(building.Kind).BuildSeconds))) + " %", smallStyle);
            GUILayout.Space(6);
            BuildingDefinition definition = Definitions.Get(building.Kind);
            GUILayout.Label("Betrieb: " + definition.EnergyUse + " Energie · " + definition.WorkerUse + " Arbeit", smallStyle);
            int stored = 0;
            foreach (int amount in building.Inventory) stored += amount;
            GUILayout.Label("Lager belegt: " + stored + " / " + GameSimulation.StorageCapacity(building), smallStyle);
            if (building.ReservedReturnSpace > 0)
                GUILayout.Label("Für Rückfracht freigehalten: " + building.ReservedReturnSpace, smallStyle);
            if (building.Kind == BuildingKind.Mine)
            {
                PlanetState planet = simulation.FindPlanet(building.PlanetId);
                GUILayout.Label("Erz im Vorkommen: " + planet.OreRemaining[building.Y * planet.Width + building.X], smallStyle);
            }
            for (int r = 0; r < building.Inventory.Length; r++)
            {
                if (building.Inventory[r] == 0 && building.ReservedIncoming[r] == 0) continue;
                string incoming = building.ReservedIncoming[r] > 0 ? "  (+" + building.ReservedIncoming[r] + " unterwegs)" : "";
                GUILayout.Label(Definitions.ResourceName((ResourceKind)r) + ": " + building.Inventory[r] + incoming, smallStyle);
            }
            if (building.Deconstructing)
            {
                GUILayout.Space(10);
                GUILayout.Label(AtmosphereSurface?"Rückgewinnung wartet auf eine Frachtdrohne und freien Lagerplatz.":"Rückgewinnung wartet hier auf Abholung. Ein erreichbarer Lagerplatz und eine Versorgungsverbindung werden benötigt.", smallStyle);
                return;
            }
            if (building.Built && building.Kind != BuildingKind.Headquarters)
            {
                GUILayout.Space(13);
                if (GUILayout.Button(building.Enabled ? "Betrieb pausieren" : "Betrieb fortsetzen", buttonStyle, GUILayout.MinHeight(UiRowHeight(30))))
                    RequestBuildingOperation(openPlanetId, building.Id, CommandKind.SetBuildingEnabled, 0, !building.Enabled);
                GUILayout.Label("Priorität für Betrieb und Versorgung", smallStyle);
                GUILayout.BeginHorizontal();
                string[] priorities = { "Niedrig", "Normal", "Hoch" };
                for (int i = 0; i < priorities.Length; i++)
                    if (GUILayout.Button(priorities[i], building.Priority == i ? selectedButtonStyle : buttonStyle, GUILayout.MinHeight(UiRowHeight(28))))
                        RequestBuildingOperation(openPlanetId, building.Id, CommandKind.SetBuildingPriority, i);
                GUILayout.EndHorizontal();
            }
            if (building.Kind != BuildingKind.Headquarters)
            {
                GUILayout.Space(14);
                int[] recovery = GameSimulation.GetRecoveryPreview(building);
                GUILayout.Label("Rückgewinnung vor Ort: " + GoodsText(recovery), smallStyle);
                GUILayout.Label("Lagerware bleibt erhalten. Bereits eingesetztes Baumaterial wird anteilig zurückgewonnen und muss abgeholt werden.", smallStyle);
                if (GUILayout.Button(building.Built ? "Gebäude zurückbauen" : "Bauauftrag abbrechen", buttonStyle, GUILayout.Height(31)))
                { ShowCommandMenu(NewContext(CommandSubject.Building,building.Id,Definitions.Get(building.Kind).Name+" #"+building.Id),Input.mousePosition);CommandSubpage("confirm"); }
            }
        }

        private void DrawColonySummary(PlanetState planet)
        {
            GUILayout.Label("KOLONIE", headingStyle);
            GUILayout.Label(PlanetEnvironment.Name(planet.Kind)+" · "+planet.Width+" × "+planet.Height+" Waben",mutedStyle);
            if(PlanetEnvironment.Airborne(planet))GUILayout.Label("Schwebende Plattformen · lokale Luftfracht · Rohstoffe per Raumhafen",smallStyle);
            else GUILayout.Label("Klimarichtwert: "+(PlanetEnvironment.EquilibriumKelvin(simulation.State,planet)+PlanetEnvironment.GreenhouseKelvin(planet.Kind)-273.15).ToString("0")+" °C · Atmosphäre im Welttyp berücksichtigt",smallStyle);
            DrawMetric("Bevölkerung / Wohnraum", planet.Population + " / " + planet.HousingCapacity);
            DrawMetric("Arbeit belegt / verfügbar", planet.WorkersUsed + " / " + planet.WorkersTotal);
            DrawMetric("Energie belegt / erzeugt", planet.EnergyUsed + " / " + planet.EnergyProduced);
            GUILayout.Label("Energiespeicher  " + planet.EnergyStored.ToString("0") + " / " + planet.EnergyCapacity.ToString("0"), smallStyle);
            GUILayout.Space(7);
            FoodRateSample sample;
            string production = foodRates.TryGetValue(planet.Id, out sample) && sample.HasMeasurement ? sample.PerMinute.ToString("0.0") : "–";
            GUILayout.Label("Nahrung / min: +" + production + " erzeugt · −" + planet.FoodDemandPerMinute.ToString("0.0") + " Bedarf", smallStyle);
            string supply = planet.FoodShortageSeconds > 0 ? "Nahrung fehlt · Produktivität " + (planet.Productivity * 100).ToString("0") + " %" : (planet.Population >= planet.HousingCapacity ? "Versorgt · Wohnraum vollständig belegt" : "Versorgt · Bevölkerung kann wachsen");
            Color previousColor = GUI.contentColor;
            GUI.contentColor = planet.FoodShortageSeconds > 0 ? Amber : Cyan;
            GUILayout.Label(supply, smallStyle);
            GUI.contentColor = previousColor;
            GUILayout.Space(10);
            for (int r = 0; r < 6; r++)
            {
                int total = 0;
                foreach (BuildingState building in planet.Buildings) total += building.Inventory[r];
                if (r >= 4 && total == 0) continue;
                DrawMetric(Definitions.ResourceName((ResourceKind)r), total.ToString());
            }
            GUILayout.Label("Lokale Lager · " + planet.Shipments.Count + " laufende Transporte", smallStyle);
        }

        private void DrawBuildTools()
        {
            var planet=simulation.FindPlanet(openPlanetId);if(planet==null)return;
            int columns=ProductionColumns;
            var stock=new int[6];for(int r=0;r<stock.Length;r++) stock[r]=GameSimulation.FreeBuildingStock(planet,PlayerFactionId,r);

            int column=0;
            foreach(BuildingKind kind in Enum.GetValues(typeof(BuildingKind)))
            {
                if(kind==BuildingKind.Headquarters)continue;
                string name=kind.ToString();
                bool military=name.Contains("Tower") || kind==BuildingKind.Wall || kind==BuildingKind.Gate || kind==BuildingKind.Barracks || name.Contains("Vehicle") || name.Contains("Depot");
                bool basic=kind==BuildingKind.Farm || kind==BuildingKind.SolarPlant || kind==BuildingKind.Housing || kind==BuildingKind.Warehouse;
                if(buildCategory!=(military?2:basic?0:1))continue;
                if(!PlanetEnvironment.AllowsBuilding(planet.Kind,kind))continue;
                if(column%columns==0)GUILayout.BeginHorizontal();
                DrawBuildButton(kind,columns,stock);column++;
                if(column%columns==0)GUILayout.EndHorizontal();
            }
            if(column%columns!=0)GUILayout.EndHorizontal();

            DrawConstructionOrders(planet);
            GUILayout.Space(4);
            if(GUILayout.Button(supplyOverlay?"Logistikansicht aus · L":"Logistikansicht · L",buttonStyle))ToggleSupplyOverlay();
            if(AtmosphereSurface) {
                GUILayout.Label("LUFTFRACHT · 4 Frachtdrohnen, je 8 Waren. Automatische Flugkorridore zwischen Plattformen. Erz und Metall per Raumhafen importieren.",smallStyle);
                return;
            }
            GUILayout.Space(7);
            if (removingRoad) GUILayout.Label("Eine Verbindungslinie anklicken. Lieferungen nutzen einen Umweg oder warten mit ihrer Fracht.", smallStyle);
            else if (placingRoad) GUILayout.Label("Freie Felder verbinden. Versorgungsverbindungen sind vorläufig kostenfrei. Rechtsklick beendet die Planung.", smallStyle);
            else if (placement.HasValue) GUILayout.Label("Freien erreichbaren Bauplatz wählen. Transporter und Baufahrzeuge finden automatisch den Weg.", smallStyle);
        }

        private void DrawMetric(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, mutedStyle);
            GUILayout.Label(value, bodyStyle, GUILayout.Width(65));
            GUILayout.EndHorizontal();
        }

        private static string GoodsText(int[] goods)
        {
            var parts = new List<string>();
            for (int r = 0; r < goods.Length; r++)
                if (goods[r] > 0) parts.Add(goods[r] + " " + Definitions.ResourceName((ResourceKind)r));
            return parts.Count == 0 ? "keine" : string.Join(", ", parts.ToArray());
        }

        private void UpdateFoodRates()
        {
            foreach (PlanetState planet in simulation.State.Planets)
            {
                FoodRateSample sample;
                if (!foodRates.TryGetValue(planet.Id, out sample))
                {
                    foodRates[planet.Id] = new FoodRateSample { Tick = simulation.State.Tick, Produced = planet.FoodProducedTotal };
                    continue;
                }
                double seconds = (simulation.State.Tick - sample.Tick) * Definitions.TickSeconds;
                if (seconds < 10) continue;
                sample.PerMinute = (planet.FoodProducedTotal - sample.Produced) * 60.0 / seconds;
                sample.HasMeasurement = true;
                sample.Tick = simulation.State.Tick;
                sample.Produced = planet.FoodProducedTotal;
            }
        }


        private void DrawBuildingLabels(float scale)
        {
            PlanetState planet = simulation.FindPlanet(openPlanetId);
            foreach (BuildingState building in planet.Buildings)
            {
                if ((building.Built && !building.Deconstructing) || building.Id == selectedBuildingId) continue;
                Vector3 screen = sceneCamera.WorldToScreenPoint(SurfacePosition(building.X, 1, building.Y));
                if (!sceneCamera.pixelRect.Contains(screen)) continue;
                GUI.Label(new Rect(screen.x / scale - 60, (Screen.height - screen.y) / scale - 23, 140, 24), building.Deconstructing ? "RÜCKBAU" : "IM BAU", smallStyle);
            }
        }

        public void ConfigureSmokeSavePath(string path)
        {
            if (!PrototypeSmokeRunner.IsRequested) throw new InvalidOperationException("Smoke storage is only enabled in explicit smoke mode.");
            smokeSavePath = Path.GetFullPath(path);
        }

        public bool SaveGame()
        {
            if(smokeSavePath==null)return QuickSaveSession();
            try
            {
                PlanetOne.Platform.SnapshotStore.Save(ActiveSavePath,simulation.State);
                Notify("Spiel gespeichert. F9 lädt diesen Stand.");return true;
            }
            catch(Exception error){Notify("Speichern fehlgeschlagen: "+error.Message);Debug.LogWarning("Speichern: "+error.Message);return false;}
        }

        public bool LoadGame(){return smokeSavePath==null?QuickLoadSession(false):LoadSnapshot(false);}
        public bool LoadBackupGame(){return smokeSavePath==null?QuickLoadSession(true):LoadSnapshot(true);}
        private bool LoadSnapshot(bool backup)
        {
            GameSimulation previous=simulation;int previousView=openPlanetId;bool replaced=false;
            try
            {
                string path=ActiveSavePath+(backup?".bak":"");
                if(!File.Exists(path)){Notify(backup?"Noch keine Sicherung vorhanden. Sie entsteht beim zweiten Speichern.":"Noch kein Spielstand vorhanden. Mit F5 speichern.");return false;}
                var replacement=new GameSimulation(PlanetOne.Platform.SnapshotStore.Load(path));
                simulation=replacement;replaced=true;foodRates.Clear();lastSimulationMessage=simulation.State.LastMessage;
                BuildSpace();
                SetView(previousView!=0 && simulation.FindPlanet(previousView)!=null && simulation.IsPlanetKnown(simulation.State.PlayerFactionId,previousView)?previousView:0);
                Notify(backup?"Letzte gültige Sicherung geladen. Mit F5 als aktuellen Spielstand speichern.":"Spielstand geladen. Alle Kolonien und Lieferungen wurden wiederhergestellt.");return true;
            }
            catch(Exception error)
            {
                if(replaced){simulation=previous;foodRates.Clear();lastSimulationMessage=previous.State.LastMessage;BuildSpace();SetView(previousView);}
                Notify("Laden fehlgeschlagen; laufendes Spiel bleibt erhalten: "+error.Message);Debug.LogWarning("Laden: "+error.Message);return false;
            }
        }

        private void TogglePause() { SetSimulationPaused(!simulation.State.Paused); }
        private void CancelPlacement() { planningSupply=false;supplyStart=0;supplyPreview=null;supplyError=null;placement = null; placingRoad = false; removingRoad = false; placementReason = null; if (ghost != null) ghost.gameObject.SetActive(false); if (placementModel != null) placementModel.gameObject.SetActive(false); }
        private void Notify(string message) { notification = message; notificationUntil = Time.unscaledTime + 8f; }

        private BuildingState FindSelected(PlanetState planet)
        {
            foreach (BuildingState building in planet.Buildings) if (building.Id == selectedBuildingId) return building;
            return null;
        }

        public bool SelectBuilding(long id)
        {
            if (openPlanetId == 0) return false;
            PlanetState planet = simulation.FindPlanet(openPlanetId);
            foreach (BuildingState building in planet.Buildings)
            {
                if (building.Id != id) continue;
                if(building.Destroyed || !simulation.CanSeeEntity(planet,PlayerFactionId,id))return false;
                CloseCommandMenu();selectedUnits.Clear();
                CancelPlacement();
                contextOpen=true;panelOpen=false;
                showingSpacePanel=false;showingDiplomacy=false;showingMilitary=false;presentationSection=1;
                selectedBuildingId = id;
                if(GameSimulation.BuildingOwner(planet,building)==PlayerFactionId && building.Built && !building.Deconstructing &&
                    (building.Kind==BuildingKind.VehicleFactory || building.Kind==BuildingKind.Barracks || building.Kind==BuildingKind.Spaceport))
                {panelOpen=true;contextOpen=false;UpdateCamera();}
                showingDetails = true;
                sidebarScroll = Vector2.zero;
                return true;
            }
            selectedBuildingId = 0;
            if(id==0){selectedUnits.Clear();militaryTargetMode=null;selectionStart=null;contextOpen=false;}
            return id == 0;
        }

        public bool IsRoadRendered(int x, int y)
        {
            if (openPlanetId == 0 || roadViews == null) return false;
            PlanetState planet = simulation.FindPlanet(openPlanetId);
            return x >= 0 && y >= 0 && x < planet.Width && y < planet.Height && roadViews[y * planet.Width + x] != null;
        }

        /// <summary>The same validated intent path is used by pointer input and integration checks.</summary>
        public bool RequestConstruction(int planetId, BuildingKind kind, int x, int y)
        {
            return SubmitIntent(new SimCommand { Kind = CommandKind.PlaceBuilding, PlanetId = planetId, Building = kind, X = x, Y = y });
        }

        public bool RequestRoadChange(int planetId, int x, int y, bool remove)
        {
            return SubmitIntent(new SimCommand { Kind = remove ? CommandKind.RemoveRoad : CommandKind.PlaceRoad, PlanetId = planetId, X = x, Y = y });
        }

        public bool RequestBuildingOperation(int planetId, long buildingId, CommandKind operation, int intValue = 0, bool boolValue = false)
        {
            if (operation != CommandKind.SetBuildingEnabled && operation != CommandKind.SetBuildingPriority &&
                operation != CommandKind.CancelConstruction && operation != CommandKind.DemolishBuilding)
                throw new ArgumentException("This operation is not a building command.", "operation");
            return SubmitIntent(new SimCommand { Kind = operation, PlanetId = planetId, TargetId = buildingId, IntValue = intValue, BoolValue = boolValue });
        }

        public bool SetSimulationPaused(bool paused)
        {
            return SubmitIntent(new SimCommand { Kind = CommandKind.SetPaused, BoolValue = paused });
        }

        public bool SetSimulationSpeed(int speed)
        {
            return SubmitIntent(new SimCommand { Kind = CommandKind.SetTimeScale, IntValue = speed });
        }

        private bool SubmitIntent(SimCommand command)
        {
            command.Id = simulation.NextCommandId();
            command.IssuerId = PlayerFactionId;
            bool queued = simulation.Enqueue(command);
            if(queued)TrackOrderArrow(command);
            Notify(simulation.State.LastMessage);
            return queued;
        }

        private static BuildingState BuildingAt(PlanetState planet, int x, int y)
        {
            foreach (BuildingState building in planet.Buildings) if (building.X == x && building.Y == y) return building;
            return null;
        }

        private Material MaterialFor(Color color)
        {
            Material material;
            if (!materials.TryGetValue(color, out material))
            {
                material = new Material(worldShader) { color = color };
                materials[color] = material;
            }
            return material;
        }

        private Transform MakePrimitive(PrimitiveType shape, string objectName, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject item = GameObject.CreatePrimitive(shape);
            item.name = objectName;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = ArtMaterial(objectName,color,shape==PrimitiveType.Sphere);
            Collider collider = item.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return item.transform;
        }

        private LineRenderer MakeLine(string objectName, Transform parent, Color color, float width, bool loop)
        {
            var item = new GameObject(objectName);
            item.transform.SetParent(parent, false);
            var line = item.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.widthMultiplier = width;
            line.sharedMaterial = MaterialFor(color);
            line.startColor = Color.white;
            line.endColor = Color.white;
            return line;
        }

        private static Color BuildingColor(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.Headquarters: return Cyan;
                case BuildingKind.Mine: return new Color(0.73f, 0.54f, 0.31f);
                case BuildingKind.Smelter: return new Color(0.80f, 0.40f, 0.27f);
                case BuildingKind.SolarPlant: return new Color(0.32f, 0.58f, 0.79f);
                case BuildingKind.Workshop: return new Color(0.56f, 0.49f, 0.70f);
                case BuildingKind.Farm: return new Color(0.44f, 0.68f, 0.40f);
                case BuildingKind.Housing: return new Color(0.64f, 0.72f, 0.76f);
                default: return new Color(0.52f, 0.63f, 0.65f);
            }
        }

        private static string FormatTime(double seconds) { return TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss"); }
        private bool Button(Rect rect, string text, bool selected) { return GUI.Button(rect, text, selected ? selectedButtonStyle : buttonStyle); }
        private static void Fill(Rect rect, Color color) { Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous; }
    }
}
