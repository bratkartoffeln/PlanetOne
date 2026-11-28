using System.Collections.Generic;

namespace PlanetOne.Core
{
    public enum CombatVisualKind { SmallArms, Cannon, Artillery, Missile, Flak, Orbital, Explosion }

    // Ephemeral, observer-filtered facts. These never enter savegames or drive simulation rules.
    public sealed class CombatPresentationEvent
    {
        public long Sequence { get; internal set; }
        public long Tick { get; internal set; }
        public CombatVisualKind Kind { get; internal set; }
        public int SourceWorld { get; internal set; }
        public long SourceId { get; internal set; }
        public double SourceX { get; internal set; }
        public double SourceY { get; internal set; }
        public int TargetWorld { get; internal set; }
        public long TargetId { get; internal set; }
        public double TargetX { get; internal set; }
        public double TargetY { get; internal set; }
        public bool SourceVisible { get; internal set; }
        public bool TargetKnown { get; internal set; }
        public bool ImpactVisible { get; internal set; }
        public int AmmunitionSpent { get; internal set; }
    }

    public sealed partial class GameSimulation
    {
        private readonly List<CombatPresentationEvent> combatPresentationEvents = new List<CombatPresentationEvent>();
        private IReadOnlyList<CombatPresentationEvent> combatPresentationView;
        private long combatPresentationSequence;
        public IReadOnlyList<CombatPresentationEvent> CombatPresentationEvents
        { get { if(combatPresentationView==null)combatPresentationView=combatPresentationEvents.AsReadOnly();return combatPresentationView; } }
        public long CombatPresentationSequence { get { return combatPresentationSequence; } }

        private bool CombatPointVisible(int world, long entity, double x, double y)
        {
            int observer = State.PlayerFactionId;
            if (world == 0)
            {
                var ship = State.Ships.Find(s => s.Id == entity);
                return ship != null ? IsShipVisible(observer, ship) : SpacePointVisible(observer, new SimPoint(x, y));
            }
            var planet = FindPlanet(world);
            if (planet == null) return false;
            if (CanSeeEntity(planet, observer, entity)) return true;
            var transport = State.Ships.Find(s => s.Id == entity && !s.Destroyed && !s.Consumed && s.TransportPhase != TransportPhase.Space);
            if (transport != null && transport.OwnerId == observer) return true;
            var beachhead = State.Beachheads.Find(h => h.Id == entity && !h.Destroyed);
            if (beachhead != null && beachhead.OwnerId == observer) return true;
            return IsCellVisible(planet, observer, x, y);
        }

        private void RecordCombatShot(CombatVisualKind kind, int sourceWorld, long sourceId, double sx, double sy,
            int targetWorld, long targetId, double tx, double ty, bool knownDestination = false, int ammunitionSpent = 1)
        {
            bool sourceVisible = CombatPointVisible(sourceWorld, sourceId, sx, sy);
            bool impactVisible = CombatPointVisible(targetWorld, targetId, tx, ty);
            if (!sourceVisible && !impactVisible) return;
            bool targetKnown = impactVisible || knownDestination;
            AddCombatPresentation(new CombatPresentationEvent {
                Kind = kind, SourceVisible = sourceVisible, ImpactVisible = impactVisible, TargetKnown = targetKnown,
                SourceWorld = sourceVisible ? sourceWorld : -1, SourceId = sourceVisible ? sourceId : 0,
                SourceX = sourceVisible ? sx : 0, SourceY = sourceVisible ? sy : 0,
                TargetWorld = targetKnown ? targetWorld : -1, TargetId = impactVisible ? targetId : 0,
                TargetX = targetKnown ? tx : 0, TargetY = targetKnown ? ty : 0,
                AmmunitionSpent = sourceVisible ? ammunitionSpent : 0
            });
        }

        private void RecordCombatExplosion(int world, long entity, double x, double y)
        {
            if (!CombatPointVisible(world, entity, x, y)) return;
            AddCombatPresentation(new CombatPresentationEvent { Kind = CombatVisualKind.Explosion,
                SourceWorld = -1, TargetWorld = world, TargetId = entity, TargetX = x, TargetY = y,
                TargetKnown = true, ImpactVisible = true });
        }

        private void AddCombatPresentation(CombatPresentationEvent item)
        {
            item.Sequence = ++combatPresentationSequence; item.Tick = State.Tick;
            if (combatPresentationEvents.Count == 512) combatPresentationEvents.RemoveAt(0);
            combatPresentationEvents.Add(item);
        }

        private static CombatVisualKind UnitShotKind(UnitKind kind)
        {
            return kind == UnitKind.Artillery ? CombatVisualKind.Artillery : kind == UnitKind.AntiAir ? CombatVisualKind.Flak :
                kind == UnitKind.AntiTank ? CombatVisualKind.Missile : kind == UnitKind.Infantry ? CombatVisualKind.SmallArms : CombatVisualKind.Cannon;
        }

        private void RecordShotAtTransport(PlanetState planet, long source, double x, double y, CombatVisualKind kind, ShipState ship)
        { RecordCombatShot(kind, planet.Id, source, x, y, planet.Id, ship.Id, ship.LandingX, ship.LandingY); }
    }
}
