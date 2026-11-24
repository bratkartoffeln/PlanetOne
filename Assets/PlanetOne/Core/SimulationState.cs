using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlanetOne.Core
{
    public enum ResourceKind { Food, Ore, Metal, Parts, Crystals, Electronics }
    public enum BuildingKind { Headquarters, Mine, Smelter, SolarPlant, Warehouse, Workshop, Farm, Housing,
        Barracks, VehicleFactory, MilitaryDepot, GunTower, AntiTankTower, FlakTower, ArtilleryTower, Wall, Gate, Spaceport, Laboratory, CrystalMine, ElectronicsFactory, ShieldGenerator, OrbitalBattery }
    public enum CommandKind { PlaceBuilding, PlaceRoad, RemoveRoad, CancelConstruction, DemolishBuilding,
        SetBuildingEnabled, SetBuildingPriority, SetPaused, SetTimeScale,
        RecruitUnit, MoveUnit, AttackTarget, HoldPosition, RetreatUnit, AttackPosition, ResupplyUnit, RepairTarget, BuildShip, FlyShip, GuardPlanet, EscortShip, Colonize, StartResearch, SetSupplyPolicy, AssignFreight, CancelFreight, SetAutoSupply, SetTradeRoute, MoveShipPosition,
        AttackShip, RetreatShip, RepairShip, BuildWarship, LoadTroop, UnloadTroop, LandTransport, LaunchTransport, LoadShipSupplies, EstablishBeachhead, CaptureBuilding, OrbitalBombard, RestoreHeadquarters }
    public enum ShipmentPhase { Loading, InTransit, Unloading, Returning, Recovering }
    public enum RelationKind { Peace, War }

    [DataContract, Serializable]
    public sealed class FactionState
    {
        [DataMember] public int Id;
        [DataMember] public string Name;
        [DataMember] public bool IsPlayer;
        [DataMember] public ResearchState Research = new ResearchState();
    }

    [DataContract, Serializable]
    public sealed class FactionRelation
    {
        [DataMember] public int FirstId;
        [DataMember] public int SecondId;
        [DataMember] public RelationKind Relation;
    }

    [DataContract, Serializable]
    public sealed class CommandResult
    {
        [DataMember] public long CommandId;
        [DataMember] public long Tick;
        [DataMember] public bool Success;
        [DataMember] public string Code;
        [DataMember] public string Message;
    }

    [DataContract, Serializable]
    public sealed class SimulationEvent
    {
        [DataMember] public long Id;
        [DataMember] public long Tick;
        [DataMember] public int FactionId;
        [DataMember] public int PlanetId;
        [DataMember] public long EntityId;
        [DataMember] public string Code;
        [DataMember] public string Message;
    }

    [DataContract, Serializable]
    public struct GridCell
    {
        [DataMember] public int X;
        [DataMember] public int Y;
        public GridCell(int x, int y) { X = x; Y = y; }
    }

    public struct SimPoint
    {
        public double X;
        public double Y;
        public SimPoint(double x, double y) { X = x; Y = y; }
    }

    [DataContract, Serializable]
    public sealed class SimCommand
    {
        [DataMember] public long Id;
        [DataMember] public int IssuerId = 1;
        [DataMember] public CommandKind Kind;
        [DataMember] public int PlanetId;
        [DataMember] public int X;
        [DataMember] public int Y;
        [DataMember] public BuildingKind Building;
        [DataMember] public long TargetId;
        [DataMember] public int IntValue;
        [DataMember] public bool BoolValue;
        [DataMember] public long ActorId;
        [DataMember] public int Amount, Minimum, Reserve;
    }

    [DataContract, Serializable]
    public sealed class GameState
    {
        [DataMember] public int Version = 7;
        [DataMember] public bool AiScenario;
        [DataMember] public List<AiState> ComputerFactions = new List<AiState>();
        [DataMember] public bool InvasionTestFixture;
        [DataMember] public List<BeachheadState> Beachheads = new List<BeachheadState>();
        [DataMember] public List<ShipState> Ships = new List<ShipState>();
        [DataMember] public List<TradeRoute> Routes = new List<TradeRoute>();
        [DataMember] public List<SpaceSighting> SpaceSightings = new List<SpaceSighting>();
        [DataMember] public int[] SpaceLostTotals = new int[6];
        [DataMember] public bool SpaceTestFixture;
        [DataMember] public bool MilitaryTestFixture;
        [DataMember] public int Seed;
        [DataMember] public long Tick;
        [DataMember] public long NextEntityId = 100;
        [DataMember] public long NextCommand = 1;
        [DataMember] public bool Paused;
        [DataMember] public double TimeScale = 1;
        [DataMember] public double Accumulator;
        [DataMember] public int PlayerFactionId = 1;
        [DataMember] public long NextEventId = 1;
        [DataMember] public List<FactionState> Factions = new List<FactionState>();
        [DataMember] public List<FactionRelation> Relations = new List<FactionRelation>();
        [DataMember] public List<CommandResult> CommandResults = new List<CommandResult>();
        [DataMember] public List<SimulationEvent> Events = new List<SimulationEvent>();
        [DataMember] public List<PlanetState> Planets = new List<PlanetState>();
        [DataMember] public List<SimCommand> PendingCommands = new List<SimCommand>();
        [DataMember] public List<long> ProcessedCommandIds = new List<long>();
        [DataMember] public string LastMessage = "Kolonie bereit. Baue Straßen, Energie und eine Erzverarbeitung.";
    }

    [DataContract, Serializable]
    public sealed class PlanetState
    {
        [DataMember] public int Id;
        [DataMember] public string Name;
        [DataMember] public int ParentId = -1;
        [DataMember] public int OwnerId;
        [DataMember] public double OrbitRadius;
        [DataMember] public double OrbitPeriodSeconds;
        [DataMember] public double OrbitPhase;
        [DataMember] public int Width = 18;
        [DataMember] public int Height = 14;
        // Terrain: 0 = level ground, 1 = rock, 2 = ore deposit.
        [DataMember] public int[] Terrain;
        [DataMember] public int[] OreRemaining;
        [DataMember] public bool[] Roads;
        [DataMember] public List<BuildingState> Buildings = new List<BuildingState>();
        [DataMember] public List<DeliveryOrder> Deliveries = new List<DeliveryOrder>();
        [DataMember] public List<ShipmentState> Shipments = new List<ShipmentState>();
        [DataMember] public int EnergyProduced;
        [DataMember] public int EnergyUsed;
        [DataMember] public int WorkersTotal = 16;
        [DataMember] public int WorkersUsed;
        [DataMember] public int Population = 16;
        [DataMember] public int HousingCapacity = 16;
        [DataMember] public double Productivity = 1;
        [DataMember] public double FoodDemandPerMinute;
        [DataMember] public int FoodConsumedTotal;
        [DataMember] public int FoodProducedTotal;
        [DataMember] public double FoodShortageSeconds;
        [DataMember] public double PopulationGrowthProgress;
        [DataMember] public double FoodDebt;
        [DataMember] public double EnergyStored;
        [DataMember] public double EnergyCapacity;
        [DataMember] public int[] ProducedTotals = new int[6];
        [DataMember] public int[] ConsumedTotals = new int[6];
        [DataMember] public int[] RecoveredTotals = new int[6];
        [DataMember] public int[] LostTotals = new int[6];
        [DataMember] public List<UnitState> Units = new List<UnitState>();
        [DataMember] public List<SightingState> Sightings = new List<SightingState>();
        [DataMember] public List<GatePermission> GatePermissions = new List<GatePermission>();
    }

    [DataContract, Serializable]
    public sealed class BuildingState
    {
        // Zero inherits strategic ownership until a contested occupation fixes a local owner.
        [DataMember] public int OwnerId;
        [DataMember] public int CapturingFaction;
        [DataMember] public double CaptureProgress;
        [DataMember] public long RestoreWorkerId;
        [DataMember] public double RestoreProgress;
        [DataMember] public long Id;
        [DataMember] public int PlanetId;
        [DataMember] public int X;
        [DataMember] public int Y;
        [DataMember] public BuildingKind Kind;
        [DataMember] public bool Built;
        [DataMember] public bool ConstructionStarted;
        [DataMember] public double BuildProgress;
        [DataMember] public bool CycleActive;
        [DataMember] public double CycleProgress;
        [DataMember] public int ProductionReservedSpace;
        [DataMember] public int[] Inventory = new int[6];
        [DataMember] public int[] ReservedOutgoing = new int[6];
        [DataMember] public int[] ReservedIncoming = new int[6];
        [DataMember] public string Status = "Baustelle";
        [DataMember] public bool Enabled = true;
        [DataMember] public int Priority = 1;
        [DataMember] public bool Deconstructing;
        [DataMember] public int CapacityOverride;
        [DataMember] public double EnergyStored;
        [DataMember] public int ReservedReturnSpace;
        [DataMember] public int LegacyCapacityAllowance;
        [DataMember] public bool PowerBlocked;
        [DataMember] public double Health = 160;
        [DataMember] public bool Destroyed;
        [DataMember] public bool Operational;
        [DataMember] public int Ammunition;
        [DataMember] public double FireCooldown;
        [DataMember] public int RecruitKind = -1;
        [DataMember] public bool RecruitPaid;
        [DataMember] public double RecruitProgress;
        [DataMember] public int ShipBuildKind = -1;
        [DataMember] public bool ShipBuildPaid;
        [DataMember] public double ShipBuildProgress;
        [DataMember] public SupplyPolicy Supply = new SupplyPolicy();
    }

    [DataContract, Serializable]
    public sealed class DeliveryOrder
    {
        [DataMember] public long Id;
        [DataMember] public long SourceId;
        [DataMember] public long TargetId;
        [DataMember] public ResourceKind Resource;
        [DataMember] public int Amount;
    }

    [DataContract, Serializable]
    public sealed class ShipmentState
    {
        [DataMember] public long Id;
        [DataMember] public long SourceId;
        [DataMember] public long TargetId;
        [DataMember] public ResourceKind Resource;
        [DataMember] public int Amount;
        [DataMember] public ShipmentPhase Phase;
        [DataMember] public List<GridCell> Path = new List<GridCell>();
        [DataMember] public double DistanceTravelled;
        [DataMember] public double PhaseProgress;
        [DataMember] public bool RecoveryReserved;
        [DataMember] public bool RouteBlocked;
    }
}
