using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlanetOne.Core
{
    public enum AiProfile { Balanced, Expansion, Security, Technology }
    public enum OperationPhase { Proposed, Preparing, Ready, Executing, Completed, Suspended, Aborting, Cancelled, Failed }

    [DataContract, Serializable]
    public sealed class AiContact
    {
        [DataMember] public long Id, Tick;
        [DataMember] public int PlanetId, OwnerId, X, Y;
        [DataMember] public BuildingKind Kind;
    }
    [DataContract, Serializable]
    public sealed class AiDecision
    {
        [DataMember] public long Tick, CommandId;
        [DataMember] public string Goal, Reason, Alternative, Action;
    }
    [DataContract, Serializable]
    public sealed class AiOperation
    {
        [DataMember] public int TargetPlanetId, HomePlanetId, LandingX, LandingY;
        [DataMember] public OperationPhase Phase;
        [DataMember] public long ChangedTick, RetryAfter;
        [DataMember] public string Reason = "Noch kein Angriffsziel";
        [DataMember] public List<long> Units = new List<long>();
        [DataMember] public List<long> Ships = new List<long>();
        [DataMember] public int FoodReserved, PartsReserved;
        [DataMember] public bool LandingChosen;
        [DataMember] public List<GridCell> RejectedLandings = new List<GridCell>();
    }
    [DataContract, Serializable]
    public sealed class AiState
    {
        [DataMember] public int FactionId;
        [DataMember] public AiProfile Profile;
        [DataMember] public long NextPlanTick;
        [DataMember] public int ScoutWaypoint;
        [DataMember] public AiOperation Operation = new AiOperation();
        [DataMember] public List<AiContact> Contacts = new List<AiContact>();
        [DataMember] public List<AiDecision> Decisions = new List<AiDecision>();
    }
    public static partial class Definitions
    {
        public const int AiPlanTicks = 40;
        public const int AiDecisionLimit = 80;
        public const int AiMetalReserve = 16, AiPartsReserve = 4;
        public const int AiRetryTicks = 1200;
    }

    // Detached knowledge passed to the planner. No GameState/GameSimulation reference.
    public sealed class AiWorld
    {
        public PlanetKind Kind;
        public int AtmosphericHydrogen;
        public int Id, OwnerId, Width, Height, Population, Housing, Workers, WorkersUsed, Energy, EnergyDemand;
        public bool Current;
        public double X, Y;
        public int[] Terrain, Ore;
        public bool[] Roads, Visible;
        public List<BuildingState> Buildings = new List<BuildingState>();
        public List<UnitState> Units = new List<UnitState>();
        public List<BeachheadState> Beachheads = new List<BeachheadState>();
        public int Shipments, PendingDeliveries;
    }
    public sealed class AiObservation
    {
        public long TradeFreighterId;
        public int FactionId, Seed;
        public long Tick;
        public ResearchState Research;
        public List<int> Enemies = new List<int>();
        public List<AiWorld> Worlds = new List<AiWorld>();
        public List<ShipState> Ships = new List<ShipState>();
    }
    public sealed class AiIntent
    {
        public SimCommand Command;
        public string Goal, Reason, Alternative;
        public AiIntent(string goal, string reason, string alternative, SimCommand command = null)
        { Goal=goal; Reason=reason; Alternative=alternative; Command=command; }
    }
}
