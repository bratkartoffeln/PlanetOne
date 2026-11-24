using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlanetOne.Core
{
    public enum TreatyKind { Ceasefire, NonAggression, Trade, Access, Defense, Coalition, JointOperation, Surrender }
    public enum TreatyPhase { Proposed, Accepted, Active, Terminating, Expired, Rejected, Withdrawn, Breached }
    [Flags] public enum AccessRight { None=0, CivilTransit=1, MilitaryTransit=2, Landing=4, Stationing=8, Repair=16, Supply=32 }
    public enum JointGoal { Defend, Escort, Invade }
    public enum FactionStatus { Active, Surrendered, Eliminated }
    [DataContract, Serializable]
    public sealed class TreatyTerms
    {
        [DataMember] public TreatyKind Kind;
        [DataMember] public List<int> Members = new List<int>();
        [DataMember] public long CoalitionId;
        [DataMember] public int DurationTicks=12000, NoticeTicks=1200, PlanetId;
        [DataMember] public AccessRight Rights;
        [DataMember] public long SourcePort, TargetPort;
        [DataMember] public ResourceKind Goods=ResourceKind.Metal, Payment=ResourceKind.Parts;
        [DataMember] public int Amount=8, PaymentAmount=2;
        [DataMember] public JointGoal Goal;
        [DataMember] public int FutureOwner;
        [DataMember] public long EscortTarget;
    }
    [DataContract, Serializable]
    public sealed class TreatyState
    {
        [DataMember] public long Id, CreatedTick, ChangedTick, StartTick, EndTick;
        [DataMember] public int Proposer, LeavingFaction;
        [DataMember] public TreatyTerms Terms=new TreatyTerms();
        [DataMember] public TreatyPhase Phase;
        [DataMember] public List<int> Approvals=new List<int>();
        [DataMember] public string Reason="Angebot wartet auf Zustimmung";
        [DataMember] public int Delivered, PaymentDelivered;
        [DataMember] public List<JointPledge> Pledges=new List<JointPledge>();
        [DataMember] public OperationPhase OperationPhase;
    }
    [DataContract, Serializable]
    public sealed class JointPledge
    {
        [DataMember] public int FactionId;
        [DataMember] public long ShipId, ReadyTick;
        [DataMember] public bool Arrived;
    }
    [DataContract, Serializable]
    public sealed class EvacuationState
    {
        [DataMember] public int Guest, Host, PlanetId;
        [DataMember] public long UntilTick;
        [DataMember] public List<long> Units=new List<long>();
        [DataMember] public List<long> Ships=new List<long>();
    }
    [DataContract, Serializable]
    public sealed class DiplomacyEvent
    {
        [DataMember] public long TreatyId, Tick;
        [DataMember] public int Actor, Other, TrustDelta;
        [DataMember] public string Key, Reason;
    }
    [DataContract, Serializable]
    public sealed class GateProject
    {
        [DataMember] public long BuildingId;
        [DataMember] public int OwnerId;
        [DataMember] public List<int> Participants=new List<int>();
        [DataMember] public int SecureTicks;
        [DataMember] public bool Activating, Complete;
        [DataMember] public string Reason="Komponenten werden angeliefert";
    }
    public static partial class Definitions
    {
        public const int EvacuationTicks=2400, GateSecureTicks=1200, DiplomacyPlanTicks=400;
        public static readonly int[] GateComponents={12,0,40,24,8,8};
        public static readonly Technology[] GateResearch={Technology.Electronics,Technology.Sensors};
        public static string TreatyName(TreatyKind k) { return new[]{"Waffenstillstand","Nichtangriff","Handel","Zugang und Nutzung","Verteidigungspakt","Koalition","Gemeinsame Operation","Kapitulation mit Restbesitzübergabe"}[(int)k]; }
        public static string TreatyStatus(TreatyPhase p) { return new[]{"Vorgeschlagen","Angenommen","Aktiv","Gekündigt","Beendet","Abgelehnt","Zurückgezogen","Gebrochen"}[(int)p]; }
    }
}
