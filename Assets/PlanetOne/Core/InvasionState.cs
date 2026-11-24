using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlanetOne.Core
{
    public enum TransportPhase { Space, Descending, Landed, Ascending }
    public sealed partial class ShipState
    {
        [DataMember] public double Health = 100;
        [DataMember] public int Ammunition;
        [DataMember] public double Cooldown, ActionProgress;
        [DataMember] public long CombatTargetId;
        [DataMember] public TransportPhase TransportPhase;
        [DataMember] public int LandingX, LandingY;
        [DataMember] public int ProductionKind = -1;
        [DataMember] public double ProductionProgress;
        [DataMember] public bool ProductionPaid;
        [DataMember] public List<UnitState> Troops = new List<UnitState>();
    }
    [DataContract, Serializable]
    public sealed class BeachheadState
    {
        [DataMember] public long Id;
        [DataMember] public int OwnerId, PlanetId, X, Y;
        [DataMember] public int Food, Parts;
        [DataMember] public double Progress;
        [DataMember] public double Health = 100;
        [DataMember] public bool Destroyed;
    }
    public sealed class SpaceWeaponDefinition
    {
        public readonly double Health, Range, Interval;
        public readonly int Damage, Ammo;
        public SpaceWeaponDefinition(double health, double range, int damage, double interval, int ammo)
        { Health=health; Range=range; Damage=damage; Interval=interval; Ammo=ammo; }
    }
    public static partial class Definitions
    {
        public const double TransitionSeconds=8, CaptureSeconds=8, CaptureRadius=2.5, OrbitalRadius=3;
        public const double ShieldRadius=3, BombardSeconds=6, BeachheadSeconds=6;
        public const int TroopCapacity=4, BeachheadParts=4;
        public static readonly SpaceWeaponDefinition[] SpaceWeapons = {
            new SpaceWeaponDefinition(100,0,0,1,0), new SpaceWeaponDefinition(160,0,0,1,0),
            new SpaceWeaponDefinition(100,0,0,1,0), new SpaceWeaponDefinition(400,0,0,1,0),
            new SpaceWeaponDefinition(100,2,4,1,24), new SpaceWeaponDefinition(140,3,14,1,32),
            new SpaceWeaponDefinition(240,4,20,1.2,40), new SpaceWeaponDefinition(140,5,35,2.5,16),
            new SpaceWeaponDefinition(180,0,0,1,0)
        };
    }
}
