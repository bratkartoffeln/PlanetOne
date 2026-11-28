using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PlanetOne.Core
{
    /// <summary>Portable JSON snapshot codec. File locations and atomic writes belong to the platform adapter.</summary>
    public static partial class SaveCodec
    {
        public const int CurrentVersion = 14;
        private const int MaximumJsonLength = 4 * 1024 * 1024;

        public static string ToJson(GameState state)
        {
            Validate(state);
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(GameState)).WriteObject(stream, state);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static GameState FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > MaximumJsonLength)
                throw new InvalidDataException("Der Spielstand ist leer oder zu groß.");
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var state = (GameState)new DataContractJsonSerializer(typeof(GameState)).ReadObject(stream);
                    if (state != null && state.Version == 1) MigrateVersionOne(state);
                    if (state != null && state.Version == 2) MigrateMilitary(state);
                    if (state != null && state.Version == 3) MigrateSpace(state);
                    if (state != null && state.Version == 4) MigrateInvasion(state);
                    // Odd-row coordinates preserve all former cardinal paths and their progress.
                    if (state != null && state.Version == 5) state.Version = 6;
                    if (state != null && state.Version == 6)
                    { state.ComputerFactions = new List<AiState>(); state.AiScenario = false; state.Version = 7; }
                    if(state!=null && state.Version==7) MigrateDiplomacy(state);
                    if(state!=null && state.Version==8) MigrateOrbits(state);
                    if(state!=null && state.Version==9) MigrateEnvironments(state);
                    if(state!=null && state.Version==10) MigrateAsteroids(state);
                    if(state!=null && state.Version==11) MigrateConstructionVehicles(state);
                    if(state!=null && state.Version==12) state.Version=13;
                    if(state!=null && state.Version==13) MigrateAtmosphere(state);
                    if(state!=null&&state.Planets!=null)foreach(var planet in state.Planets)if(planet.Trails==null)planet.Trails=new List<GroundTrailState>();
                    Validate(state);
                    return state;
                }
            }
            catch (InvalidDataException) { throw; }
            catch (Exception error) when (error is System.Runtime.Serialization.SerializationException ||
                error is System.Xml.XmlException || error is FormatException || error is ArgumentException || error is OverflowException)
            { throw new InvalidDataException("Der Spielstand ist beschädigt oder unlesbar.", error); }
        }

        public static void Validate(GameState state)
        {
            Require(state != null, "Spielzustand fehlt.");
            Require(state.Version == CurrentVersion, "Nicht unterstützte Spielstandversion.");
            ValidateAi(state);
            ValidateDiplomacy(state);
            Require(state.Tick >= 0 && state.NextEntityId > 0 && state.NextCommand > 0, "Ungültige Zeit oder Kennungen.");
            Require(Finite(state.Accumulator) && state.Accumulator >= 0 && state.Accumulator < Definitions.TickSeconds + 0.000001,
                "Ungültiger Simulationsrest.");
            Require(Finite(state.TimeScale) && state.TimeScale > 0 && state.TimeScale <= 16, "Ungültiges Spieltempo.");
            Require(state.Planets != null && state.Planets.Count > 0 && state.Planets.Count <= 32, "Ungültige Weltliste.");
            Require(state.PendingCommands != null && state.ProcessedCommandIds != null, "Befehlszustand fehlt.");
            Require(state.Factions != null && state.Factions.Count > 0 && state.Factions.Count <= 64 &&
                state.Relations != null && state.CommandResults != null && state.Events != null && state.NextEventId > 0,
                "Fraktions- oder Ereigniszustand fehlt.");
            var factionIds = new HashSet<int>();
            foreach (var faction in state.Factions)
                Require(faction != null && faction.Id > 0 && factionIds.Add(faction.Id), "Ungültige Fraktion.");
            Require(state.Factions.Exists(f => f.Id == state.PlayerFactionId && f.IsPlayer), "Spielerfraktion fehlt.");
            var relationPairs = new HashSet<string>();
            foreach (var relation in state.Relations)
                Require(relation != null && relation.FirstId < relation.SecondId && factionIds.Contains(relation.FirstId) &&
                    factionIds.Contains(relation.SecondId) && Enum.IsDefined(typeof(RelationKind), relation.Relation) &&
                    relationPairs.Add(relation.FirstId + ":" + relation.SecondId), "Ungültige Fraktionsbeziehung.");
            Require(state.Relations.Count == state.Factions.Count * (state.Factions.Count - 1) / 2, "Fraktionsbeziehungen sind unvollständig.");
            Require(state.CommandResults.Count <= Definitions.EventHistoryLimit && state.Events.Count <= Definitions.EventHistoryLimit,
                "Ereignisprotokoll ist zu groß.");
            var eventIds = new HashSet<long>();
            foreach (var entry in state.Events)
                Require(entry != null && entry.Id > 0 && entry.Id < state.NextEventId && eventIds.Add(entry.Id) &&
                    entry.Tick >= 0 && entry.Tick <= state.Tick && !string.IsNullOrEmpty(entry.Code), "Ungültiges Ereignis.");
            foreach (var result in state.CommandResults)
                Require(result != null && result.CommandId > 0 && result.Tick >= 0 && result.Tick <= state.Tick &&
                    !string.IsNullOrEmpty(result.Code), "Ungültige Befehlsrückmeldung.");
            var planetIds = new HashSet<int>();
            var entityIds = new HashSet<long>();
            foreach (var p in state.Planets)
            {
                Require(p != null && p.Id > 0 && planetIds.Add(p.Id), "Doppelte oder ungültige Planetenkennung.");
                Require(p.OwnerId == 0 || factionIds.Contains(p.OwnerId), "Planetenbesitzer ist unbekannt.");
                Require(p.Population >= 0 && p.Population <= 2000 && p.HousingCapacity >= 0 && p.WorkersTotal >= 0 &&
                    p.WorkersTotal <= p.Population && p.WorkersUsed >= 0 && p.WorkersUsed <= p.WorkersTotal,
                    "Ungültige Bevölkerung oder Arbeitskräfte.");
                Require(Finite(p.Productivity) && p.Productivity >= Definitions.MinimumProductivity && p.Productivity <= 1 &&
                    Finite(p.FoodDebt) && p.FoodDebt >= 0 && p.FoodDebt <= 1.000001 &&
                    Finite(p.FoodShortageSeconds) && p.FoodShortageSeconds >= 0 &&
                    Finite(p.PopulationGrowthProgress) && p.PopulationGrowthProgress >= 0 &&
                    Finite(p.FoodDemandPerMinute) && p.FoodDemandPerMinute >= 0 &&
                    p.FoodConsumedTotal >= 0 && p.FoodProducedTotal >= 0, "Ungültige Versorgungsdaten.");
                Require(Finite(p.EnergyStored) && p.EnergyStored >= 0 && Finite(p.EnergyCapacity) && p.EnergyCapacity >= 0,
                    "Ungültiger Energiespeicher.");
                Goods(p.ProducedTotals); Goods(p.ConsumedTotals); Goods(p.RecoveredTotals);
                Require(p.Width > 0 && p.Height > 0 && p.Width <= 128 && p.Height <= 128, "Ungültige Oberflächengröße.");
                Require(Enum.IsDefined(typeof(PlanetKind),p.Kind),"Ungültiger Planetentyp.");
                Require(p.AtmosphericHydrogen>=0 && p.AtmosphericHydrogen<=PlanetEnvironment.InitialHydrogen(state.Seed,p),"Ungültiger atmosphärischer Fördervorrat.");
                Require(Finite(p.OrbitRadius) && p.OrbitRadius > 0 && Finite(p.OrbitPeriodSeconds) &&
                    p.OrbitPeriodSeconds > 0 && Finite(p.OrbitPhase), "Ungültige Umlaufbahn.");
                Require(Finite(p.OrbitEccentricity) && p.OrbitEccentricity >= 0 && p.OrbitEccentricity <= 0.6 &&
                    Finite(p.OrbitPeriapsis) &&
                    System.Math.Abs(p.OrbitPeriodSeconds / OrbitalMotion.Period(p.OrbitRadius, p.ParentId >= 0) - 1) < 1e-9,
                    "Ungültige Kepler-Bahnparameter.");
                Require(p.Terrain != null && p.OreRemaining != null && p.Roads != null &&
                    p.Terrain.Length == p.Width * p.Height && p.OreRemaining.Length == p.Terrain.Length &&
                    p.Roads.Length == p.Terrain.Length, "Unvollständiges Gelände.");
                for (int i = 0; i < p.Terrain.Length; i++)
                    Require(p.Terrain[i] >= 0 && p.Terrain[i] <= 4 && p.OreRemaining[i] >= 0 &&
                        (!PlanetEnvironment.Airborne(p) || (!p.Roads[i] && p.Terrain[i]==0 && p.OreRemaining[i]==0)), "Ungültiger Geländezustand.");
                Require(p.Buildings != null && p.Deliveries != null && p.Shipments != null, "Koloniezustand fehlt.");
                Require(p.Trails!=null&&p.Trails.Count<=p.Terrain.Length*3,"Ungültige Fahrspuren.");
                var trailKeys=new HashSet<long>();
                foreach(var trail in p.Trails)
                    Require(trail!=null&&trail.From>=0&&trail.To>trail.From&&trail.To<p.Terrain.Length&&
                        HexGrid.Adjacent(new GridCell(trail.From%p.Width,trail.From/p.Width),new GridCell(trail.To%p.Width,trail.To/p.Width))&&
                        Finite(trail.Wear)&&trail.Wear>=0&&trail.Wear<=1&&trail.LastTick>=0&&trail.LastTick<=state.Tick&&
                        trailKeys.Add((long)trail.From*p.Terrain.Length+trail.To)&&!PlanetEnvironment.Airborne(p),"Beschädigte Fahrspur.");
                Require(p.Buildings.Count <= p.Width * p.Height && p.Shipments.Count <= Definitions.CouriersPerColony,
                    "Zu viele Gebäude oder Transporte.");
                var cells = new HashSet<int>();
                var buildings = new Dictionary<long, BuildingState>();
                foreach (var b in p.Buildings)
                {
                    Require(b != null && b.Id > 0 && entityIds.Add(b.Id) && b.PlanetId == p.Id,
                        "Ungültige Gebäudekennung.");
                    Require(b.X >= 0 && b.Y >= 0 && b.X < p.Width && b.Y < p.Height &&
                        cells.Add(b.Y * p.Width + b.X), "Überlappende oder ungültige Gebäude.");
                    Require(Enum.IsDefined(typeof(BuildingKind), b.Kind), "Unbekannter Gebäudetyp.");
                    Require(PlanetEnvironment.AllowsBuilding(p.Kind,b.Kind),"Gebäude für diesen Planetentyp ungeeignet.");
                    Require(b.Priority >= 0 && b.Priority <= 2 && b.CapacityOverride >= 0 && b.CapacityOverride <= 4096 &&
                        b.ReservedReturnSpace >= 0 && b.LegacyCapacityAllowance >= 0 && b.LegacyCapacityAllowance <= 4096 &&
                        Finite(b.EnergyStored) && b.EnergyStored >= 0 &&
                        b.EnergyStored <= Definitions.Get(b.Kind).EnergyStorageCapacity + 0.000001,
                        "Ungültige Betriebsdaten.");
                    Require(!b.Deconstructing || (!b.Enabled && !b.CycleActive), "Rückbau betreibt weiterhin Produktion.");
                    Goods(b.Inventory); Goods(b.ReservedOutgoing); Goods(b.ReservedIncoming);
                    Require(Finite(b.BuildProgress) && b.BuildProgress >= 0 && Finite(b.CycleProgress) &&
                        b.CycleProgress >= 0 && b.ProductionReservedSpace >= 0, "Ungültiger Produktionsfortschritt.");
                    int used = b.ProductionReservedSpace + b.ReservedReturnSpace;
                    for (int r = 0; r < Definitions.ResourceCount; r++)
                    {
                        Require(b.ReservedOutgoing[r] <= b.Inventory[r], "Überbuchter Quellbestand.");
                        used += b.Inventory[r] + b.ReservedIncoming[r];
                    }
                    Require(used <= GameSimulation.StorageCapacity(b), "Überbuchtes Lager.");
                    buildings.Add(b.Id, b);
                }
                ValidateMilitary(state, p, factionIds, entityIds);
                var expectedOutgoing = new Dictionary<long, int[]>();
                var expectedIncoming = new Dictionary<long, int[]>();
                var expectedReturn = new Dictionary<long, int>();
                foreach (var b in p.Buildings) { expectedOutgoing[b.Id] = new int[Definitions.ResourceCount]; expectedIncoming[b.Id] = new int[Definitions.ResourceCount]; expectedReturn[b.Id] = 0; }
                foreach (var delivery in p.Deliveries)
                {
                    Require(delivery != null && delivery.Id > 0 && entityIds.Add(delivery.Id), "Ungültiger Lieferauftrag.");
                    DeliveryReferences(buildings, delivery.SourceId, delivery.TargetId, delivery.Resource, delivery.Amount);
                    expectedOutgoing[delivery.SourceId][(int)delivery.Resource] += delivery.Amount;
                    expectedIncoming[delivery.TargetId][(int)delivery.Resource] += delivery.Amount;
                }
                int builders = 0;
                var staffedSites = new HashSet<long>();
                foreach (var shipment in p.Shipments)
                {
                    Require(shipment != null && shipment.Id > 0 && entityIds.Add(shipment.Id) &&
                        Enum.IsDefined(typeof(ShipmentPhase), shipment.Phase), "Ungültiger Transport.");
                    bool builder = GameSimulation.IsConstructionVehicle(shipment);
                    DeliveryReferences(buildings, shipment.SourceId, shipment.TargetId, shipment.Resource,
                        shipment.Phase == ShipmentPhase.Returning || builder ? 1 : shipment.Amount);
                    if (builder)
                    {
                        Require(++builders <= Definitions.ConstructionSlotsPerColony && shipment.Amount == 0 &&
                            shipment.PhaseProgress == 0 && !shipment.RecoveryReserved &&
                            buildings[shipment.SourceId].Kind == BuildingKind.Headquarters,
                            "Ungültiger Baufahrzeugeinsatz oder duplizierte Ladung.");
                        if (shipment.Phase != ShipmentPhase.BuilderReturning)
                            Require(staffedSites.Add(shipment.TargetId) && !buildings[shipment.TargetId].Built &&
                                !buildings[shipment.TargetId].Deconstructing, "Baustelle doppelt oder ungültig besetzt.");
                    }
                    Require(shipment.Phase != ShipmentPhase.Returning || shipment.Amount == 0, "Rückfahrt enthält duplizierte Ladung.");
                    Require(shipment.Amount <= Definitions.CourierCapacity && shipment.Path != null && shipment.Path.Count > 0,
                        "Ungültige Fracht oder Strecke.");
                    Require(Finite(shipment.PassingOffset) && shipment.PassingOffset>=0 && shipment.PassingOffset<=GameSimulation.RoadLaneOffset,"Ungültiger Ausweichabstand.");
                    Require(Finite(shipment.DistanceTravelled) && shipment.DistanceTravelled >= 0 &&
                        shipment.DistanceTravelled <= shipment.Path.Count - 1 && Finite(shipment.PhaseProgress) &&
                        shipment.PhaseProgress >= 0, "Ungültiger Transportfortschritt.");
                    for (int i = 0; i < shipment.Path.Count; i++)
                    {
                        var cell = shipment.Path[i];
                        Require(cell.X >= 0 && cell.Y >= 0 && cell.X < p.Width && cell.Y < p.Height, "Weg verlässt Oberfläche.");
                        if (i > 0) Require(HexGrid.Adjacent(cell, shipment.Path[i - 1]),
                            "Transportweg überspringt Felder.");
                    }
                    var last = shipment.Path[shipment.Path.Count - 1];
                    Require(!shipment.BuilderTurnPending||shipment.Phase==ShipmentPhase.BuilderReturning,"Ungültiges Wendemanöver des Baufahrzeugs.");
                    var destination = buildings[shipment.Phase == ShipmentPhase.Returning || shipment.Phase == ShipmentPhase.Recovering || shipment.Phase == ShipmentPhase.BuilderReturning&&!shipment.BuilderTurnPending
                        ? shipment.SourceId : shipment.TargetId];
                    Require(shipment.RouteBlocked || (last.X == destination.X && last.Y == destination.Y), "Transportweg passt nicht zum Auftrag.");
                    if (builder)
                    {
                        Require(shipment.Phase != ShipmentPhase.Building ||
                            Math.Abs(shipment.DistanceTravelled - GameSimulation.ShipmentArrivalDistance(shipment)) < 0.000000001,
                            "Baufahrzeug baut vor seiner Ankunft.");
                        continue;
                    }
                    if (shipment.Phase == ShipmentPhase.Loading)
                        expectedOutgoing[shipment.SourceId][(int)shipment.Resource] += shipment.Amount;
                    if (shipment.Phase != ShipmentPhase.Returning && shipment.Phase != ShipmentPhase.Recovering)
                        expectedIncoming[shipment.TargetId][(int)shipment.Resource] += shipment.Amount;
                    if (shipment.Phase == ShipmentPhase.Recovering && shipment.RecoveryReserved)
                        expectedIncoming[shipment.SourceId][(int)shipment.Resource] += shipment.Amount;
                    if (shipment.Phase == ShipmentPhase.InTransit || shipment.Phase == ShipmentPhase.Unloading)
                        expectedReturn[shipment.SourceId] += shipment.Amount;
                }
                AddSpaceReservations(state, p, expectedOutgoing, expectedIncoming);
                foreach (var b in p.Buildings)
                {
                    Require(expectedReturn[b.Id] == b.ReservedReturnSpace, "Falsche Rücknahmereservierung.");
                    for (int r = 0; r < Definitions.ResourceCount; r++)
                        Require(expectedOutgoing[b.Id][r] == b.ReservedOutgoing[r] && expectedIncoming[b.Id][r] == b.ReservedIncoming[r],
                            "Verwaiste oder falsche Reservierung.");
                }
            }
            ValidateSpace(state, factionIds, entityIds);
            ValidateAsteroids(state, entityIds);
            ValidateInvasion(state, factionIds, entityIds);
            foreach(var treaty in state.Treaties)Require(entityIds.Add(treaty.Id),"Vertragskennung kollidiert mit einer bestehenden Entität.");
            foreach (long id in entityIds) Require(id < state.NextEntityId, "Nächste Entitätskennung ist bereits belegt.");
            foreach (var p in state.Planets)
            {
                var chain = new HashSet<int>(); var current = p;
                while (current.ParentId != -1)
                {
                    Require(chain.Add(current.Id), "Zyklische Umlaufbahn.");
                    current = state.Planets.Find(other => other.Id == current.ParentId);
                    Require(current != null, "Bezugskörper fehlt.");
                }
            }
            var commandIds = new HashSet<long>();
            foreach (long id in state.ProcessedCommandIds)
                Require(id > 0 && id < state.NextCommand && commandIds.Add(id), "Doppelte ausgeführte Befehle.");
            foreach (var command in state.PendingCommands)
                Require(command != null && command.Id > 0 && command.Id < state.NextCommand && commandIds.Add(command.Id),
                    "Doppelte oder ungültige vorgemerkte Befehle.");
        }

        private static void MigrateConstructionVehicles(GameState state)
        {
            Require(state.Planets != null, "Alte Weltliste fehlt.");
            // Preserve the fraction already built and paid material. A real free courier must
            // reach an unfinished site before its existing construction can continue.
            foreach (var planet in state.Planets)
            {
                Require(planet != null && planet.Buildings != null, "Alter Koloniezustand fehlt.");
                foreach (var building in planet.Buildings)
                    if (building != null && !building.Built && building.ConstructionStarted)
                        building.BuildProgress *= Definitions.ConstructionDurationMultiplier;
            }
            state.Version = 13;
        }

        private static void DeliveryReferences(Dictionary<long, BuildingState> buildings, long source,
            long target, ResourceKind resource, int amount)
        {
            Require(source != target && buildings.ContainsKey(source) && buildings.ContainsKey(target) &&
                Enum.IsDefined(typeof(ResourceKind), resource) && amount > 0, "Ungültige Lieferreferenz.");
        }

        private static void MigrateVersionOne(GameState state)
        {
            Require(state.Planets != null, "Alte Weltliste fehlt.");
            state.PlayerFactionId = 1; state.NextEventId = 1;
            state.Factions = new List<FactionState> { new FactionState { Id = 1, Name = "Koloniekonsortium", IsPlayer = true } };
            foreach (var p in state.Planets)
            {
                Require(p != null && p.Buildings != null && p.Shipments != null, "Alter Koloniezustand fehlt.");
                if (p.OwnerId > 0 && !state.Factions.Exists(f => f.Id == p.OwnerId))
                    state.Factions.Add(new FactionState { Id = p.OwnerId, Name = "Fraktion " + p.OwnerId });
                p.Population = p.OwnerId == 0 ? 0 : 16; p.HousingCapacity = p.Population;
                p.WorkersTotal = (int)(p.Population * Definitions.WorkforceFraction); p.WorkersUsed = 0;
                p.Productivity = 1; p.FoodDebt = 0; p.FoodShortageSeconds = 0; p.PopulationGrowthProgress = 0;
                p.FoodDemandPerMinute = p.Population * Definitions.FoodPerPersonPerSecond * 60;
                p.FoodConsumedTotal = 0; p.FoodProducedTotal = 0; p.EnergyStored = 0; p.EnergyCapacity = 0;
                p.ProducedTotals = new int[Definitions.ResourceCount]; p.ConsumedTotals = new int[Definitions.ResourceCount]; p.RecoveredTotals = new int[Definitions.ResourceCount];
                foreach (var b in p.Buildings)
                {
                    Require(b != null, "Altes Gebäude fehlt.");
                    b.Enabled = true; b.Priority = b.Kind == BuildingKind.Farm ? 2 : 1;
                    b.Deconstructing = false; b.CapacityOverride = 0; b.EnergyStored = 0;
                    b.ReservedReturnSpace = 0; b.LegacyCapacityAllowance = 0;
                }
                foreach (var shipment in p.Shipments)
                {
                    Require(shipment != null && shipment.Path != null, "Alter Transport fehlt.");
                    // Version 1 stored empty return paths in the outward orientation.
                    if (shipment.Phase == ShipmentPhase.Returning) shipment.Path.Reverse();
                    if (shipment.Phase == ShipmentPhase.InTransit || shipment.Phase == ShipmentPhase.Unloading)
                    {
                        var source = p.Buildings.Find(b => b.Id == shipment.SourceId);
                        Require(source != null, "Alte Frachtquelle fehlt.");
                        source.ReservedReturnSpace += shipment.Amount;
                    }
                    shipment.RecoveryReserved = false; shipment.RouteBlocked = false;
                }
                foreach (var b in p.Buildings)
                {
                    Require(b.Inventory != null && b.ReservedIncoming != null, "Alte Warenbestände fehlen.");
                    int used = b.ProductionReservedSpace + b.ReservedReturnSpace;
                    foreach (int amount in b.Inventory) used += amount;
                    foreach (int amount in b.ReservedIncoming) used += amount;
                    // Preserve legacy full depots without erasing real in-flight cargo. This allowance
                    // shrinks with every released slot and cannot be replenished by new production.
                    b.LegacyCapacityAllowance = Math.Max(0, used - Definitions.Get(b.Kind).Capacity);
                }
            }
            if (!state.Factions.Exists(f => f.Id == 2)) state.Factions.Add(new FactionState { Id = 2, Name = "Freie Stationen" });
            state.Factions.Sort((a, b) => a.Id.CompareTo(b.Id));
            state.Relations = new List<FactionRelation>();
            for (int a = 0; a < state.Factions.Count; a++)
                for (int b = a + 1; b < state.Factions.Count; b++)
                    state.Relations.Add(new FactionRelation { FirstId = state.Factions[a].Id, SecondId = state.Factions[b].Id, Relation = RelationKind.Peace });
            state.CommandResults = new List<CommandResult>(); state.Events = new List<SimulationEvent>();
            state.Version = 2;
            state.LastMessage = "Spielstand übernommen. Bevölkerung und Versorgung sind nun aktiv; vorhandene Waren bleiben erhalten.";
        }

        private static void Goods(int[] goods)
        {
            Require(goods != null && goods.Length == Definitions.ResourceCount, "Unvollständiger Warenbestand.");
            foreach (int value in goods) Require(value >= 0 && value <= 1000000, "Ungültige Warenmenge.");
        }

        private static bool Finite(double number) { return !double.IsNaN(number) && !double.IsInfinity(number); }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidDataException(message); }
    }
}
