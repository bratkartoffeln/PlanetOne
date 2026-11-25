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
        public int RenderedShipmentCount { get { return shipmentViews.Count; } }
        public int PlayerFactionId { get { return simulation.State.PlayerFactionId; } }
        public static string SavePath { get { return Path.Combine(Application.persistentDataPath, "prototype-save.json"); } }
        public string ActiveSavePath { get { return smokeSavePath ?? (simulation.State.DiplomacyScenario && simulation.State.SpaceTestFixture ? Path.Combine(Application.persistentDataPath, "diplomacy-fixture-save.json") : simulation.State.DiplomacyScenario ? Path.Combine(Application.persistentDataPath, "diplomacy-save.json") : simulation.State.AiScenario ? Path.Combine(Application.persistentDataPath, "ai-scenario-save.json") : simulation.State.InvasionTestFixture ? Path.Combine(Application.persistentDataPath, "invasion-fixture-save.json") : simulation.State.SpaceTestFixture ? Path.Combine(Application.persistentDataPath, "space-fixture-save.json") : simulation.State.MilitaryTestFixture ? Path.Combine(Application.persistentDataPath, "military-fixture-save.json") : SavePath); } }

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
            sceneCamera.farClipPlane = 300f;
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
            if (PrototypeSmokeRunner.IsRequested) gameObject.AddComponent<PrototypeSmokeRunner>();
        }

        private void Update()
        {
            if (simulation == null) return;
            Shader.SetGlobalFloat("_PlanetOneSeconds",(float)(simulation.State.Tick*Definitions.TickSeconds));
            HandleKeyboard();
            simulation.Advance(Time.unscaledDeltaTime);
            UpdateFoodRates();
            if (!string.IsNullOrEmpty(simulation.State.LastMessage) && simulation.State.LastMessage != lastSimulationMessage)
            {
                lastSimulationMessage = simulation.State.LastMessage;
                Notify(lastSimulationMessage);
            }
            UpdateSpace();
            if (openPlanetId != 0) UpdateSurface();
            if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height) UpdateCamera();
            HandleWorldInput();
        }

        private void OnDestroy()
        {
            if (hexMesh != null) Destroy(hexMesh);
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
            if(reliefMesh!=null)Destroy(reliefMesh);
            if(reliefMap!=null)Destroy(reliefMap);
            if(panelTexture!=null)Destroy(panelTexture);if(buttonTexture!=null)Destroy(buttonTexture);if(hoverTexture!=null)Destroy(hoverTexture);
        }

        private void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { if(settingsOpen) settingsOpen=false; else if(menuOpen) menuOpen=false; else if(placement.HasValue || placingRoad || removingRoad || militaryTargetMode.HasValue || spaceMoveTargetMode) {CancelPlacement();militaryTargetMode=null;spaceMoveTargetMode=false;} else if(panelOpen){panelOpen=false;contextOpen=true;} else if(SelectionCardVisible)contextOpen=false; else menuOpen=true; selectionStart=null; shipSelectionStart=null; return; }
            if(settingsOpen || menuOpen) return;
            HandleOrbitCamera();
            if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
            if (Input.GetKeyDown(KeyCode.F5)) SaveGame();
            if (Input.GetKeyDown(KeyCode.F9)) LoadGame();
            if (Input.GetKeyDown(KeyCode.Escape)) CancelPlacement();
            if (Input.GetKeyDown(KeyCode.Tab) && openPlanetId != 0) SetView(0);
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
                    var delta=Quaternion.Euler(0,cameraYaw,0)*pan.normalized*Time.unscaledDeltaTime*8f;
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
            if(HandleSpacePanInput())return;
            if (!IsWorldPoint(Input.mousePosition))
            {
                if (ghost != null) ghost.gameObject.SetActive(false);
                return;
            }
            float wheel = Input.mouseScrollDelta.y;
            if (Math.Abs(wheel) > 0.001f)
            {
                if (openPlanetId == 0) spaceZoom = Mathf.Clamp(spaceZoom - wheel * 1.2f, 6f, 90f);
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
                return;
            }

            Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
            var hoverCell = SurfaceCellAtScreen(Input.mousePosition);
            hoverX = hoverCell.X; hoverY = hoverCell.Y;
            PlanetState planetState = simulation.FindPlanet(openPlanetId);
            bool inside = hoverX >= 0 && hoverY >= 0 && hoverX < planetState.Width && hoverY < planetState.Height;
            if (!placement.HasValue && !placingRoad && !removingRoad && HandleMilitaryPointer(planetState, inside)) return;
            if (placement.HasValue || placingRoad || removingRoad)
            {
                bool valid = inside && planetState.OwnerId == PlayerFactionId;
                placementReason = valid ? null : "Hier ist kein eigener Bauplatz.";
                if (valid && placement.HasValue)
                {
                    placementReason = simulation.ValidatePlacement(openPlanetId, placement.Value, hoverX, hoverY, PlayerFactionId);
                    valid = placementReason == null;
                }
                if (valid && placingRoad)
                {
                    int index = hoverY * planetState.Width + hoverX;
                    valid = planetState.Terrain[index] == 0 && BuildingAt(planetState, hoverX, hoverY) == null && !planetState.Roads[index];
                    if (!valid) placementReason = "Straße benötigt freies Gelände ohne Gebäude, Fels oder Erzvorkommen.";
                }
                if (valid && removingRoad)
                {
                    valid = planetState.Roads[hoverY * planetState.Width + hoverX];
                    if (!valid) placementReason = "Eine vorhandene Straße zum Entfernen auswählen.";
                }
                ghost.gameObject.SetActive(inside);
                ConformMarker(ghost,hoverX,hoverY);
                ghost.GetComponent<LineRenderer>().sharedMaterial = MaterialFor(valid ? (removingRoad ? Amber : Cyan) : Danger);
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
                if (Input.GetMouseButtonDown(0))
                {
                    RaycastHit buildingHit;
                    long pickedId;
                    if (Physics.Raycast(ray, out buildingHit, 300f) && buildingPickIds.TryGetValue(buildingHit.collider.GetInstanceID(), out pickedId))
                        SelectBuilding(pickedId);
                    else
                    {
                        BuildingState building = inside ? BuildingAt(planetState, hoverX, hoverY) : null;
                        SelectBuilding(building == null ? 0 : building.Id);
                    }
                }
            }
        }

        private void BuildSpace()
        {
            if (spaceRoot != null) {spaceRoot.gameObject.SetActive(false);Destroy(spaceRoot.gameObject);}
            spaceRoot = new GameObject("Sonnensystem").transform;
            spaceRoot.SetParent(transform, false);
            planetViews.Clear(); shipViews.Clear(); selectedShips.Clear();
            if(!simulation.IsPlanetKnown(PlayerFactionId,selectedPlanetId))
            {
                var known=simulation.State.Planets.Find(p=>p.OwnerId==PlayerFactionId) ?? simulation.State.Planets.Find(p=>simulation.IsPlanetKnown(PlayerFactionId,p.Id));
                selectedPlanetId=known==null?0:known.Id;
            }
            orbitViews.Clear();
            var star=MakePrimitive(PrimitiveType.Sphere,"Stern",spaceRoot,Vector3.zero,Vector3.one*8.6f,new Color(1,0.50f,0.055f));CreateStarCorona(star);
            spaceFocus=Vector3.zero;BuildSpaceBackdrop();
            float maxOrbit = 12f;
            foreach (PlanetState planet in simulation.State.Planets)
            {
                bool isMoon = planet.ParentId > 0;
                Transform body = CreateCelestial(planet,spaceRoot,Vector3.zero,planet.Kind==PlanetKind.GasGiant?7.8f:isMoon?1.1f:3.5f*(float)PlanetEnvironment.RadiusEarths(planet.Kind));
                planetViews[planet.Id] = body;
                var orbit = MakeLine("Umlaufbahn " + planet.Name, spaceRoot, new Color(0.13f,0.27f,0.36f), 0.018f, true);
                orbit.positionCount = 96;
                for (int i = 0; i < 96; i++)
                {
                    var orbitPoint=OrbitalMotion.PositionAtMeanAnomaly(planet,i*OrbitalMotion.Tau/96);
                    orbit.SetPosition(i,new Vector3((float)orbitPoint.X,0,(float)orbitPoint.Y));
                }
                orbitViews[planet.Id] = orbit.transform;
                if (!isMoon && simulation.IsPlanetKnown(PlayerFactionId,planet.Id)) maxOrbit = Mathf.Max(maxOrbit, (float)(planet.OrbitRadius*(1+planet.OrbitEccentricity)));
            }
            spaceZoom = maxOrbit*0.73f+2;
            UpdateSpace();
        }

        private void UpdateSpace()
        {
            UpdateShipViews();
            UpdateInvasionViews();
            foreach (PlanetState planet in simulation.State.Planets)
            {
                bool known = simulation.IsPlanetKnown(PlayerFactionId, planet.Id);
                planetViews[planet.Id].gameObject.SetActive(known); orbitViews[planet.Id].gameObject.SetActive(known);
                SimPoint point = simulation.GetOrbitPosition(planet.Id);
                planetViews[planet.Id].position = new Vector3((float)point.X, 0, (float)point.Y);
                planetViews[planet.Id].rotation=PlanetAxialRotation(planet.Id,simulation.State.Tick*Definitions.TickSeconds,planet.Kind);
                celestialMaterials[planet.Id].SetVector("_SunDirection",new Vector4(-(float)point.X,2,-(float)point.Y,0));
                if (planet.ParentId > 0)
                {
                    SimPoint parent = simulation.GetOrbitPosition(planet.ParentId);
                    orbitViews[planet.Id].position = new Vector3((float)parent.X, 0, (float)parent.Y);
                }
            }
        }

        public void SetView(int planetId)
        {
            if (planetId != 0 && simulation.FindPlanet(planetId) == null) return;
            if (planetId != 0 && !simulation.IsPlanetKnown(PlayerFactionId, planetId)) { Notify("Oberfläche ist noch unbekannt."); return; }
            openPlanetId = planetId;
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
            surfaceRoot = new GameObject("Oberfläche " + planet.Name).transform;
            surfaceRoot.SetParent(transform, false);
            roadViews = new Transform[planet.Width * planet.Height];
            roadVisualHash=0;BuildRelief(planet);
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
            }
            ghost = MakeHexOutline("Bauvorschau", surfaceRoot, Cyan, 0.055f);
            selectedMarker = MakeHexOutline("Auswahl", surfaceRoot, Cyan, 0.045f);
            ghost.gameObject.SetActive(false);
            selectedMarker.gameObject.SetActive(false);
            UpdateSurface();
        }

        private void UpdateSurface()
        {
            PlanetState planet = simulation.FindPlanet(openPlanetId);
            RefreshRoadVisuals(planet);
            for (int i = 0; i < planet.Roads.Length; i++)
            {
                if (!planet.Roads[i])
                {
                    if (roadViews[i] != null) Destroy(roadViews[i].gameObject);
                    roadViews[i] = null;
                    continue;
                }
                if (roadViews[i] == null)
                    roadViews[i] = BuildRoad(i%planet.Width,i/planet.Width,planet);
            }
            obsoleteBuildings.Clear();
            foreach (long id in buildingViews.Keys) obsoleteBuildings.Add(id);
            foreach (BuildingState building in planet.Buildings)
            {
                obsoleteBuildings.Remove(building.Id);
                Transform view;
                if (!buildingViews.TryGetValue(building.Id, out view))
                {
                    view = BuildBuilding(building);
                    buildingViews[building.Id] = view;
                }
                float height = building.Built ? 1f : Mathf.Lerp(0.18f, 0.95f, (float)(building.BuildProgress / Math.Max(0.01, Definitions.Get(building.Kind).BuildSeconds)));
                view.localScale = new Vector3(1, height, 1);
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
            obsoleteShipments.Clear();
            foreach (long id in shipmentViews.Keys) obsoleteShipments.Add(id);
            foreach (ShipmentState shipment in planet.Shipments)
            {
                Transform view;
                if (!shipmentViews.TryGetValue(shipment.Id, out view))
                {
                    view=new GameObject("Gütertransporter").transform;view.SetParent(surfaceRoot,false);
                    BuildVehicleModel(view,UnitKind.Engineer,Amber,true);view.localScale=Vector3.one*(AtmosphereSurface?.8f:.42f);
                    shipmentViews[shipment.Id] = view;
                }
                obsoleteShipments.Remove(shipment.Id);
                SimPoint position = simulation.GetShipmentPosition(shipment);
                UpdateGroundPose(view,position.X,position.Y);
            }
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
            for(int i=0;i<6;i++)
            {Vector3 foot=HexCorner(i,0.79f),world=root.position+foot;float depth=root.position.y-ReliefHeight(world.x,world.z);if(depth>0.05f)Rod(root,"Fundamentstütze",foot-Vector3.up*depth,foot+Vector3.up*0.04f,0.1f,Steel);}
            BoxCollider pickCollider = root.gameObject.AddComponent<BoxCollider>();
            pickCollider.center = new Vector3(0, 0.55f, 0);
            pickCollider.size = new Vector3(0.90f, 1.1f, 0.90f);
            buildingPickIds[pickCollider.GetInstanceID()] = building.Id;

            Transform status = MakePrimitive(PrimitiveType.Cube, "Betriebsanzeige", root, new Vector3(0, 0.17f, -0.39f), new Vector3(0.36f, 0.055f, 0.055f), Cyan);
            buildingIndicators[building.Id] = status.GetComponent<Renderer>();
            if(AtmosphereSurface)BuildFloatingPlatform(root);
            PopulateBuildingModel(root,building.Kind);
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
                sceneCamera.transform.position = spaceFocus+Quaternion.Euler(Mathf.Clamp(cameraPitch-15,25,70),cameraYaw,0)*new Vector3(0,0,-56);
                sceneCamera.transform.LookAt(spaceFocus);
                sceneCamera.orthographicSize = spaceZoom;
            }
            else
            {
                sceneCamera.transform.position = surfaceFocus + Quaternion.Euler(cameraPitch,cameraYaw,0)*new Vector3(0,0,-30);
                sceneCamera.transform.LookAt(surfaceFocus);
                sceneCamera.orthographicSize = surfaceZoom;
            }
            UpdateSpaceBackdropCamera();
        }

        private float UiScale { get { return Mathf.Min(presentation.Scale, Mathf.Min(Screen.width / 1200f, Screen.height / 820f)); } }

        private void OnGUI()
        {
            if (simulation == null) return;
            if(!settingsOpen && !menuOpen) { HandleSelectionEvents(); HandleSpaceEvents(); }
            EnsureStyles();
            float scale = UiScale;
            Matrix4x4 original = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            PreparePresentationSkin();
            bool previousEnabled=GUI.enabled;
            GUI.enabled=previousEnabled && !settingsOpen && !menuOpen;
            if (openPlanetId == 0) { DrawPlanetNameplates(scale); DrawShipLabels(scale); }
            else { DrawBuildingLabels(scale); DrawMilitaryLabels(scale); }
            DrawPresentationTop(width);
            DrawPresentationNavigation(width,height);
            DrawSidebar(width, height);
            DrawPresentationBottom(width, height);
            DrawSelectionCard(width,height);
            GUI.enabled=previousEnabled;
            DrawPresentationDialogs(width,height);
            GUI.matrix = original;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
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
        }


        private void DrawBuildButton(BuildingKind kind)
        {
            BuildingDefinition definition = Definitions.Get(kind);
            string cost = "";
            if (definition.Cost[(int)ResourceKind.Metal] > 0) cost += definition.Cost[(int)ResourceKind.Metal] + " M";
            if (definition.Cost[(int)ResourceKind.Parts] > 0) cost += "  " + definition.Cost[(int)ResourceKind.Parts] + " B";
            if (GUILayout.Button(new GUIContent(definition.Name + "\n" + cost, BuildingIcon(kind),definition.Name+" bauen · "+cost+" · Bauplatz anschließend in der Welt wählen"), placement == kind ? selectedBuildStyle : buildStyle, GUILayout.Height(104)))
            {
                placement = placement == kind ? (BuildingKind?)null : kind;
                placingRoad = false;
                removingRoad = false;
                selectedBuildingId = 0;
            }
        }

        private void DrawBuildingDetails(BuildingState building)
        {
            if(GameSimulation.BuildingOwner(simulation.FindPlanet(building.PlanetId),building)!=PlayerFactionId)
            { GUILayout.Label(Definitions.Get(building.Kind).Name+" · fremde Anlage",headingStyle);GUILayout.Label("Mit Infanterie vor Ort sichern.",bodyStyle);return; }
            GUILayout.Label(Definitions.Get(building.Kind).Name.ToUpperInvariant(), headingStyle);
            GUILayout.Label(building.Status ?? (building.Built ? "Betriebsbereit" : "Im Bau"), mutedStyle);
            GUILayout.Label("Struktur: " + building.Health.ToString("0") + " / " + Definitions.BuildingHealth(building.Kind), smallStyle);
            DrawRecruitment(building);
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
                GUILayout.Label(AtmosphereSurface?"Rückgewinnung wartet auf eine Frachtdrohne und freien Lagerplatz.":"Rückgewinnung wartet hier auf Abholung. Ein erreichbarer Lagerplatz und eine Straße werden benötigt.", smallStyle);
                return;
            }
            if (building.Built && building.Kind != BuildingKind.Headquarters)
            {
                GUILayout.Space(13);
                if (GUILayout.Button(building.Enabled ? "Betrieb pausieren" : "Betrieb fortsetzen", buttonStyle, GUILayout.Height(30)))
                    RequestBuildingOperation(openPlanetId, building.Id, CommandKind.SetBuildingEnabled, 0, !building.Enabled);
                GUILayout.Label("Priorität für Betrieb und Versorgung", smallStyle);
                GUILayout.BeginHorizontal();
                string[] priorities = { "Niedrig", "Normal", "Hoch" };
                for (int i = 0; i < priorities.Length; i++)
                    if (GUILayout.Button(priorities[i], building.Priority == i ? selectedButtonStyle : buttonStyle, GUILayout.Height(28)))
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
                    RequestBuildingOperation(openPlanetId, building.Id, building.Built ? CommandKind.DemolishBuilding : CommandKind.CancelConstruction);
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
            string[] categories={"Aufbau","Industrie","Militär"};
            buildCategory=GUILayout.Toolbar(buildCategory,categories,buttonStyle);
            GUILayout.Space(8);
            int column=0;
            foreach(BuildingKind kind in Enum.GetValues(typeof(BuildingKind)))
            {
                if(kind==BuildingKind.Headquarters)continue;
                string name=kind.ToString();
                bool military=name.Contains("Tower") || kind==BuildingKind.Wall || kind==BuildingKind.Gate || kind==BuildingKind.Barracks || name.Contains("Vehicle") || name.Contains("Depot");
                bool basic=kind==BuildingKind.Farm || kind==BuildingKind.SolarPlant || kind==BuildingKind.Housing || kind==BuildingKind.Warehouse;
                if(buildCategory!=(military?2:basic?0:1))continue;
                if(!PlanetEnvironment.AllowsBuilding(planet.Kind,kind))continue;
                if(column%2==0)GUILayout.BeginHorizontal();
                DrawBuildButton(kind);column++;
                if(column%2==0)GUILayout.EndHorizontal();
            }
            if(column%2!=0)GUILayout.EndHorizontal();
            GUILayout.Space(4);
            if(AtmosphereSurface) {
                GUILayout.Label("LUFTFRACHT · 4 Frachtdrohnen, je 8 Waren. Automatische Flugkorridore zwischen Plattformen. Erz und Metall per Raumhafen importieren.",smallStyle);
                return;
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Straße bauen", placingRoad ? selectedButtonStyle : buttonStyle, GUILayout.Height(30)))
            {
                bool next = !placingRoad; CancelPlacement(); placingRoad = next; selectedBuildingId = 0;
            }
            if (GUILayout.Button("Entfernen", removingRoad ? selectedButtonStyle : buttonStyle, GUILayout.Height(30)))
            {
                bool next = !removingRoad; CancelPlacement(); removingRoad = next; selectedBuildingId = 0;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(7);
            if (removingRoad) GUILayout.Label("Straße anklicken. Unterbrochene Lieferungen warten oder nutzen einen Umweg; ihre Fracht bleibt erhalten.", smallStyle);
            else if (placingRoad) GUILayout.Label("Freie Felder verbinden. Straßen sind in dieser Fassung kostenfrei. Rechtsklick beendet den Straßenbau.", smallStyle);
            else if (placement.HasValue) GUILayout.Label("Freies Feld an einer Straße wählen. Mine nur auf Erz. Rechtsklick beendet den Bau.", smallStyle);
            else GUILayout.Label("Metall M · Bauteile B. Alle Baumaterialien werden tatsächlich angeliefert.", smallStyle);
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
            try
            {
                string savePath = ActiveSavePath;
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                string json = SaveCodec.ToJson(simulation.State);
                SaveCodec.FromJson(json); // Validate before replacing the last usable file.
                string temporary = savePath + ".tmp";
                File.WriteAllText(temporary, json);
                if (File.Exists(savePath))
                {
                    try { File.Replace(temporary, savePath, savePath + ".bak"); }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(savePath, savePath + ".bak", true);
                        File.Copy(temporary, savePath, true);
                        File.Delete(temporary);
                    }
                }
                else File.Move(temporary, savePath);
                Notify("Spiel gespeichert. F9 lädt diesen Stand.");
                return true;
            }
            catch (Exception exception)
            {
                Notify("Speichern fehlgeschlagen: " + exception.Message);
                Debug.LogException(exception);
                return false;
            }
        }

        public bool LoadGame()
        {
            try
            {
                if (!File.Exists(ActiveSavePath)) { Notify("Noch kein Spielstand vorhanden. Mit F5 speichern."); return false; }
                GameState restored = SaveCodec.FromJson(File.ReadAllText(ActiveSavePath));
                var replacement = new GameSimulation(restored);
                int previousView = openPlanetId;
                simulation = replacement;
                foodRates.Clear();
                lastSimulationMessage = simulation.State.LastMessage;
                BuildSpace();
                SetView(previousView != 0 && simulation.FindPlanet(previousView) != null ? previousView : 0);
                Notify("Spielstand geladen. Alle Kolonien und Lieferungen wurden wiederhergestellt.");
                return true;
            }
            catch (Exception exception)
            {
                Notify("Laden fehlgeschlagen; laufendes Spiel bleibt erhalten: " + exception.Message);
                Debug.LogException(exception);
                return false;
            }
        }

        private void TogglePause() { SetSimulationPaused(!simulation.State.Paused); }
        private void CancelPlacement() { placement = null; placingRoad = false; removingRoad = false; placementReason = null; if (ghost != null) ghost.gameObject.SetActive(false); }
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
                CancelPlacement();
                contextOpen=true;panelOpen=false;
                showingSpacePanel=false;showingDiplomacy=false;showingMilitary=false;presentationSection=1;
                selectedBuildingId = id;
                showingDetails = true;
                sidebarScroll = Vector2.zero;
                return true;
            }
            selectedBuildingId = 0;
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
