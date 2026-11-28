using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlanetOne.Core
{
    public enum UnitKind { Infantry, AntiTank, Tank, Artillery, Engineer, AntiAir, MobileWorkshop }
    public enum UnitOrder { Hold, Move, Attack, Retreat, Bombard, Resupply, Repair }
    public enum TargetLayer { Ground, LandingTransition, Space }

    [DataContract, Serializable]
    public sealed class UnitState
    {
        [DataMember] public long Id;
        [DataMember] public int OwnerId;
        [DataMember] public UnitKind Kind;
        [DataMember] public double X;
        [DataMember] public double Y;
        [DataMember] public double Health;
        [DataMember] public int Ammunition;
        [DataMember] public int Food;
        [DataMember] public int Parts;
        [DataMember] public double Cooldown;
        [DataMember] public double RationTime;
        [IgnoreDataMember] public long ActiveRepairTarget;
        [DataMember] public bool Destroyed;
        [DataMember] public UnitOrder Order;
        [DataMember(EmitDefaultValue=false)] public int InfantrySlot;
        [DataMember] public int GoalX;
        [DataMember] public int GoalY;
        [DataMember] public long TargetId;
        [DataMember] public List<GridCell> Path = new List<GridCell>();
        [DataMember] public double PathDistance;
        [DataMember] public bool DirectPath;
        [DataMember(EmitDefaultValue=false)] public bool RoundedPath;
        [DataMember] public bool RecenterToCell;
        [DataMember] public string Status = "Hält Stellung";
        // P12 will supply real carriers. Only explicitly marked fixtures may create these targets now.
        [DataMember] public TargetLayer Layer;
        [DataMember] public GroundTrafficState Traffic;
    }

    [DataContract, Serializable]
    public sealed class SightingState
    {
        [DataMember] public int FactionId;
        [DataMember] public long EntityId;
        [DataMember] public int X;
        [DataMember] public int Y;
        [DataMember] public long Tick;
    }

    [DataContract, Serializable]
    public sealed class GatePermission
    {
        [DataMember] public int FactionId;
        [DataMember] public long GateId;
    }

    public sealed class UnitDefinition
    {
        public readonly string Name;
        public readonly int Metal, Parts, Food, Crew, Ammo, Damage;
        public readonly double Health, Speed, Range, Sight, Seconds, Interval;
        public readonly bool Indirect, AntiAir;
        public UnitDefinition(string name, int metal, int parts, int food, int crew,
            double seconds, double health, double speed, double range, double sight,
            int damage, int ammo, double interval, bool indirect = false, bool antiAir = false)
        {
            Name = name; Metal = metal; Parts = parts; Food = food; Crew = crew;
            Seconds = seconds; Health = health; Speed = speed; Range = range; Sight = sight;
            Damage = damage; Ammo = ammo; Interval = interval; Indirect = indirect; AntiAir = antiAir;
        }
        public int[] Cost { get { return new[] { Food, 0, Metal, Parts, 0, 0, 0 }; } }
    }

    public sealed class TowerDefinition
    {
        public readonly double Range, Interval;
        public readonly int Damage;
        public readonly bool Indirect, AntiAir;
        public readonly UnitKind AttackKind;
        internal TowerDefinition(double range, double interval, int damage, UnitKind attackKind, bool indirect = false, bool antiAir = false)
        { Range = range; Interval = interval; Damage = damage; AttackKind = attackKind; Indirect = indirect; AntiAir = antiAir; }
    }

    public static partial class Definitions
    {
        public const int AmmoPerPart = 8;
        public const int TowerAmmoCapacity = 24;
        public const double RepairPerPart = 16;
        public const double RationSeconds = 30;
        public const double CoverDamageFactor = 0.6;
        public const double BuildingSight = 4;
        public const double SupplyTransferSeconds = 1;
        public const double AreaFireRadius = 0.8;
        public const double TankAntiArmorFactor = 1.8;
        public const double TankInfantryFactor = 0.35;
        public const double TankOtherDamageFactor = 0.75;
        public const double UnfedReloadFactor = 1.5;
        private static readonly TowerDefinition[] Towers = {
            new TowerDefinition(4.5, 1, 12, UnitKind.Infantry),
            new TowerDefinition(4.5, 1, 24, UnitKind.AntiTank),
            new TowerDefinition(6, 1, 12, UnitKind.AntiAir, antiAir: true),
            new TowerDefinition(8, 2.5, 35, UnitKind.Artillery, indirect: true)
        };
        public static TowerDefinition GetTower(BuildingKind kind)
        { if (!IsTower(kind)) throw new ArgumentOutOfRangeException("kind"); return Towers[(int)kind - (int)BuildingKind.GunTower]; }
        private static readonly UnitDefinition[] Units = {
            new UnitDefinition("Linieninfanterie", 4, 1, 2, 1, 4, 65, 2.1, 3.5, 6, 9, 16, 0.8),
            new UnitDefinition("Panzerabwehrtrupp", 5, 2, 2, 1, 5, 60, 1.7, 4.5, 6, 22, 12, 1.6),
            new UnitDefinition("Kampfpanzer", 12, 4, 2, 2, 8, 180, 1.6, 4, 5, 20, 16, 1.2),
            new UnitDefinition("Mobile Artillerie", 10, 4, 2, 2, 9, 75, 1.1, 9, 3.5, 38, 8, 2.5, true),
            new UnitDefinition("Pionier und Versorgung", 6, 3, 2, 1, 6, 85, 1.8, 2, 5, 3, 8, 1.5),
            new UnitDefinition("Mobile Flugabwehr", 8, 3, 2, 1, 7, 90, 1.6, 6, 7, 25, 16, 1.2, false, true),
            new UnitDefinition("Mobile Werkstatt", 10, 8, 2, 2, 9, 130, 1.45, 0, 5, 0, 0, 1)
        };
        public static UnitDefinition Get(UnitKind kind)
        {
            if ((int)kind < 0 || (int)kind >= Units.Length) throw new ArgumentOutOfRangeException("kind");
            return Units[(int)kind];
        }
        public static bool IsTower(BuildingKind kind)
        { return kind >= BuildingKind.GunTower && kind <= BuildingKind.ArtilleryTower; }
        public static BuildingKind Producer(UnitKind kind)
        { return kind == UnitKind.Infantry || kind == UnitKind.AntiTank ? BuildingKind.Barracks : BuildingKind.VehicleFactory; }
        public static bool CanRepair(UnitKind kind)
        { return kind == UnitKind.Engineer || kind == UnitKind.MobileWorkshop; }
        public static int DamageStage(double health, double maximum)
        { double fraction = health / maximum; return fraction <= .25 ? 3 : fraction <= .50 ? 2 : fraction <= .75 ? 1 : 0; }
        private static readonly double[] ConditionRates = { 1, .9, .75, .55 };
        public static double GroundSpeed(UnitState unit)
        { var d = Get(unit.Kind); return d.Speed * ConditionRates[DamageStage(unit.Health,d.Health)]; }
        public static double ProductionRate(BuildingState b)
        { return ConditionRates[DamageStage(b.Health,BuildingHealth(b.Kind))]; }
        public const int WorkshopStarterParts = 4;
        public const double WorkshopRadius = 2;
        public const int PlanetRecruitmentLimit = 20;
        public static double BuildingHealth(BuildingKind kind)
        { return kind == BuildingKind.Wall ? 200 : kind == BuildingKind.Gate ? 160 : 160; }
    }
}
