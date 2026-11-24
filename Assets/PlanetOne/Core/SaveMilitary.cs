using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public static partial class SaveCodec
    {
        private static void MigrateMilitary(GameState state)
        {
            Require(state.Planets != null, "Alte Weltliste fehlt.");
            foreach (var p in state.Planets)
            {
                Require(p != null && p.Buildings != null, "Alter Koloniezustand fehlt.");
                p.Units = new List<UnitState>(); p.Sightings = new List<SightingState>();
                p.GatePermissions = new List<GatePermission>(); p.LostTotals = new int[6];
                foreach (var b in p.Buildings)
                {
                    b.Health = Definitions.BuildingHealth(b.Kind); b.Destroyed = false;
                    b.Operational = false; b.Ammunition = 0; b.FireCooldown = 0;
                    b.RecruitKind = -1; b.RecruitPaid = false; b.RecruitProgress = 0;
                }
            }
            state.MilitaryTestFixture = false; state.Version = 3;
        }

        private static void ValidateMilitary(GameState state, PlanetState p, HashSet<int> factions, HashSet<long> entities)
        {
            Goods(p.LostTotals);
            Require(p.Units != null && p.Sightings != null && p.GatePermissions != null && p.Units.Count <= 10000, "Militärzustand fehlt oder ist zu groß.");
            foreach (var b in p.Buildings)
            {
                Require(Finite(b.Health) && b.Health >= 0 && b.Health <= Definitions.BuildingHealth(b.Kind) &&
                    b.Destroyed == (b.Health == 0) && (!b.Destroyed || b.Deconstructing), "Ungültiger Gebäudeschaden.");
                Require(b.Ammunition >= 0 && b.Ammunition <= Definitions.TowerAmmoCapacity &&
                    Finite(b.FireCooldown) && b.FireCooldown >= 0 && Finite(b.RecruitProgress) && b.RecruitProgress >= 0,
                    "Ungültige Militärproduktion.");
                Require(b.RecruitKind == -1 || (Enum.IsDefined(typeof(UnitKind), b.RecruitKind) &&
                    Definitions.Producer((UnitKind)b.RecruitKind) == b.Kind && b.Built && !b.Deconstructing &&
                    b.RecruitProgress <= Definitions.Get((UnitKind)b.RecruitKind).Seconds), "Ungültiger Rekrutierungsauftrag.");
                Require(b.RecruitKind != -1 || (!b.RecruitPaid && b.RecruitProgress == 0), "Verwaiste Rekrutierung.");
            }
            foreach (var u in p.Units)
            {
                Require(u != null && u.Id > 0 && entities.Add(u.Id) && factions.Contains(u.OwnerId) &&
                    Enum.IsDefined(typeof(UnitKind), u.Kind) && Enum.IsDefined(typeof(UnitOrder), u.Order) &&
                    Enum.IsDefined(typeof(TargetLayer), u.Layer), "Ungültige Einheit.");
                var d = Definitions.Get(u.Kind);
                Require(Finite(u.X) && Finite(u.Y) && u.X >= 0 && u.Y >= 0 && u.X <= p.Width - 1 && u.Y <= p.Height - 1 &&
                    Finite(u.Health) && u.Health >= 0 && u.Health <= d.Health && u.Destroyed == (u.Health == 0), "Ungültiger Einheitenort oder Schaden.");
                Require(u.Ammunition >= 0 && u.Ammunition <= d.Ammo && u.Food >= 0 && u.Food <= 4 && u.Parts >= 0 && u.Parts <= 8 &&
                    (u.Kind == UnitKind.Engineer || u.Parts == 0) && Finite(u.Cooldown) && u.Cooldown >= 0 &&
                    Finite(u.RationTime) && u.RationTime >= 0 && u.RationTime < Definitions.RationSeconds + 0.000001,
                    "Ungültige Einheitenvorräte.");
                Require(u.Layer == TargetLayer.Ground || state.MilitaryTestFixture, "Übergangsziel benötigt eine ausgewiesene P12-Testfixture.");
                Require(u.Path != null && Finite(u.PathDistance) && u.PathDistance >= 0 &&
                    u.PathDistance <= Math.Max(0, u.Path.Count - 1), "Ungültiger Bodenweg.");
                Require(u.GoalX >= 0 && u.GoalY >= 0 && u.GoalX < p.Width && u.GoalY < p.Height, "Ungültiges Bodenziel.");
                for (int i = 0; i < u.Path.Count; i++)
                {
                    var c = u.Path[i];
                    Require(c.X >= 0 && c.Y >= 0 && c.X < p.Width && c.Y < p.Height, "Bodenweg außerhalb der Karte.");
                    if (i > 0) Require(HexGrid.Adjacent(c, u.Path[i - 1]), "Bodenweg überspringt Felder.");
                }
                if (u.Path.Count > 0)
                {
                    int i = Math.Min((int)u.PathDistance, u.Path.Count - 1);
                    var a = u.Path[i]; var b = u.Path[Math.Min(i + 1, u.Path.Count - 1)]; double t = u.PathDistance - i;
                    Require(Math.Abs(u.X - (a.X + (b.X - a.X) * t)) < 0.000001 &&
                        Math.Abs(u.Y - (a.Y + (b.Y - a.Y) * t)) < 0.000001, "Bodenweg und Einheitenposition widersprechen sich.");
                }
            }
            var sightings = new HashSet<string>();
            foreach (var s in p.Sightings)
                Require(s != null && factions.Contains(s.FactionId) && s.EntityId > 0 && s.EntityId < state.NextEntityId &&
                    s.Tick >= 0 && s.Tick <= state.Tick && s.X >= 0 && s.Y >= 0 && s.X < p.Width && s.Y < p.Height &&
                    sightings.Add(s.FactionId + ":" + s.EntityId), "Ungültige letzte Sichtung.");
            var gates = new HashSet<string>();
            foreach (var g in p.GatePermissions)
                Require(g != null && factions.Contains(g.FactionId) &&
                    p.Buildings.Exists(b => b.Id == g.GateId && b.Kind == BuildingKind.Gate) &&
                    gates.Add(g.FactionId + ":" + g.GateId), "Ungültiges Torrecht.");
        }
    }
}
