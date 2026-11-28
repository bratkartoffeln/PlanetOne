using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlanetOne.Core
{
    public enum ShipKind { Freighter, ColonyShip, SurveyShip, OrbitalYard, Scout, Corvette, EscortFrigate, TorpedoShip, LandingTransport }
    public enum ShipOrder { Hold, Fly, Guard, Escort, Freight, MovePosition, Attack, Retreat, Repair, Bombard, Mine }
    public enum FreightPhase { Collect, Loading, Deliver, Unloading }
    public enum Technology { Transport, Colonization, CrystalMining, Electronics, Sensors }

    [DataContract, Serializable]
    public sealed partial class ShipState
    {
        [DataMember] public long Id;
        [DataMember] public int OwnerId;
        [DataMember] public ShipKind Kind;
        [DataMember] public double X, Y;
        [DataMember] public double GoalX, GoalY;
        [DataMember] public ShipOrder Order;
        [DataMember] public int TargetPlanetId;
        [DataMember] public long EscortId;
        [DataMember] public bool Destroyed, Consumed, AutoSupply;
        [DataMember] public int Crew;
        [DataMember] public int[] Cargo = new int[Definitions.ResourceCount];
        [DataMember] public FreightJob Freight;
        [DataMember] public MiningJob Mining;
        [DataMember] public bool TrafficGoalSet;
        [DataMember] public double TrafficAnchorX, TrafficAnchorY, TrafficOffsetX, TrafficOffsetY;
        [DataMember] public double TrafficYieldX, TrafficYieldY;
        [DataMember] public long TrafficYieldUntil, TrafficYieldFor;
        [DataMember] public int TrafficWaitTicks;
        [DataMember] public string Status = "Bereit";
    }
    [DataContract, Serializable]
    public sealed class FreightJob
    {
        [DataMember] public long SourceId, TargetId;
        [DataMember] public long TreatyId;
        [DataMember] public bool PaymentLeg;
        [DataMember] public ResourceKind Resource;
        [DataMember] public int Amount;
        [DataMember] public FreightPhase Phase;
        [DataMember] public double Progress;
    }
    [DataContract, Serializable]
    public sealed class SupplyPolicy
    {
        [DataMember] public int[] Minimum = new int[Definitions.ResourceCount];
        [DataMember] public int[] Target = new int[Definitions.ResourceCount];
        [DataMember] public int[] ExportReserve = new int[] { 24, 16, 20, 12, 0, 0, 0 };
        [DataMember] public int[] Priority = new int[] { 2, 1, 1, 1, 1, 1, 1 };
        [DataMember] public bool[] Replenishing = new bool[Definitions.ResourceCount];
    }
    [DataContract, Serializable]
    public sealed class TradeRoute
    {
        [DataMember] public long Id, ShipId, SourceId, TargetId;
        [DataMember] public ResourceKind Resource;
        [DataMember] public int Amount;
        [DataMember] public bool Enabled = true;
    }
    [DataContract, Serializable]
    public sealed class ResearchState
    {
        [DataMember] public List<Technology> Completed = new List<Technology>();
        [DataMember] public int Active = -1;
        [DataMember] public long LaboratoryId;
        [DataMember] public bool Paid;
        [DataMember] public double Progress;
        [DataMember] public string Status = "Kein Forschungsauftrag";
    }
    [DataContract, Serializable]
    public sealed class SpaceSighting
    {
        [DataMember] public int FactionId, PlanetId, KnownOwnerId;
        [DataMember] public long ShipId, Tick;
        [DataMember] public ShipKind Kind;
        [DataMember] public double X, Y;
    }
    public sealed class ShipDefinition
    {
        public readonly string Name;
        public readonly int[] Cost, ColonyCargo;
        public readonly int Crew, Capacity;
        public readonly double Seconds, Speed, Sight;
        public readonly Technology Required;
        public ShipDefinition(string name, int[] cost, int crew, int capacity, double seconds, double speed, double sight, Technology required, int[] cargo = null)
        { Name = name; Cost = cost; Crew = crew; Capacity = capacity; Seconds = seconds; Speed = speed; Sight = sight; Required = required; ColonyCargo = cargo ?? new int[Definitions.ResourceCount]; }
    }
    public sealed class TechnologyDefinition
    {
        public readonly Technology Id;
        public readonly string Name;
        public readonly Technology[] Prerequisites;
        public readonly int[] Cost;
        public readonly double Seconds;
        public TechnologyDefinition(Technology id, string name, double seconds, int[] cost, params Technology[] prerequisites)
        { Id = id; Name = name; Seconds = seconds; Cost = cost; Prerequisites = prerequisites; }
    }
    public static partial class Definitions
    {
        public const double SpaceTransferSeconds = 0.5; // per item, one berth per port
        public const double DockDistance = 0.25;
        public static readonly ShipDefinition[] Ships = {
            new ShipDefinition("Frachter", new[] { 4,0,16,6,0,0, 0 }, 2, 40, 12, 6, 5, Technology.Transport),
            new ShipDefinition("Kolonieschiff", new[] { 44,0,100,32,0,0, 0 }, 12, 140, 20, 5, 6, Technology.Colonization, new[] {40,0,80,20,0,0,0}),
            new ShipDefinition("Sensorschiff", new[] {4,0,16,6,0,4,0}, 2, 0, 14, 8, 12, Technology.Sensors),
            new ShipDefinition("Orbitalwerft", new[] {8,0,30,12,0,0,0}, 4, 160, 24, 0, 8, Technology.Transport),
            new ShipDefinition("Aufklärer", new[] {2,0,12,4,0,0,0}, 1, 0, 10, 9, 12, Technology.Transport),
            new ShipDefinition("Korvette", new[] {4,0,20,8,0,0,0}, 2, 0, 16, 7, 7, Technology.Transport),
            new ShipDefinition("Eskortfregatte", new[] {6,0,32,14,0,0,0}, 3, 0, 24, 5, 8, Technology.Transport),
            new ShipDefinition("Torpedoschiff", new[] {4,0,28,12,0,0,0}, 2, 0, 22, 4, 7, Technology.Transport),
            new ShipDefinition("Landungstransporter", new[] {4,0,24,10,0,0,0}, 2, 24, 18, 5, 6, Technology.Transport)
        };
        public static readonly TechnologyDefinition[] Technologies = {
            new TechnologyDefinition(Technology.Transport, "Raumtransport", 12, new[] {0,0,4,2,0,0,0}),
            new TechnologyDefinition(Technology.Colonization, "Koloniemodule", 16, new[] {0,0,6,3,0,0,0}, Technology.Transport),
            new TechnologyDefinition(Technology.CrystalMining, "Kristallabbau", 14, new[] {0,0,4,2,0,0,0}, Technology.Transport),
            new TechnologyDefinition(Technology.Electronics, "Elektronik", 20, new[] {0,0,4,2,4,0,0}, Technology.CrystalMining, Technology.Colonization),
            new TechnologyDefinition(Technology.Sensors, "Fortgeschrittene Sensoren", 20, new[] {0,0,0,4,0,4,0}, Technology.Electronics)
        };
        public static void ValidateTechnologies(TechnologyDefinition[] definitions)
        {
            var nodes = new Dictionary<Technology, TechnologyDefinition>();
            foreach (var d in definitions)
                if (d == null || nodes.ContainsKey(d.Id) || !Enum.IsDefined(typeof(Technology), d.Id) || d.Seconds <= 0 || d.Cost == null || d.Cost.Length != Definitions.ResourceCount)
                    throw new ArgumentException("Ungültige Technologiedefinition.");
                else { foreach (int n in d.Cost) if (n < 0) throw new ArgumentException("Negative Forschungskosten."); nodes.Add(d.Id, d); }
            var visiting = new HashSet<Technology>(); var visited = new HashSet<Technology>();
            foreach (var d in definitions) VisitTechnology(d.Id, nodes, visiting, visited);
        }
        private static void VisitTechnology(Technology id, Dictionary<Technology, TechnologyDefinition> nodes, HashSet<Technology> visiting, HashSet<Technology> visited)
        {
            if (visited.Contains(id)) return;
            if (!nodes.ContainsKey(id) || !visiting.Add(id)) throw new ArgumentException("Fehlender oder zyklischer Technologieverweis.");
            foreach (var prerequisite in nodes[id].Prerequisites) VisitTechnology(prerequisite, nodes, visiting, visited);
            visiting.Remove(id); visited.Add(id);
        }
    }
}
