namespace PlanetOne.Core
{
    public static partial class SaveCodec
    {
        private static void MigrateOrbits(GameState state)
        {
            Require(state.Planets != null && state.Tick >= 0, "Umlaufbahnen fehlen.");
            double seconds = state.Tick * Definitions.TickSeconds;
            foreach (var planet in state.Planets)
            {
                Require(planet != null && Finite(planet.OrbitRadius) && planet.OrbitRadius > 0 &&
                    Finite(planet.OrbitPeriodSeconds) && planet.OrbitPeriodSeconds > 0 && Finite(planet.OrbitPhase), "Ungültige alte Umlaufbahn.");
                double currentAngle = OrbitalMotion.Normalize(planet.OrbitPhase + seconds * OrbitalMotion.Tau / planet.OrbitPeriodSeconds);
                planet.OrbitPeriodSeconds = OrbitalMotion.Period(planet.OrbitRadius, planet.ParentId >= 0);
                planet.OrbitPhase = OrbitalMotion.Normalize(currentAngle - (seconds % planet.OrbitPeriodSeconds) * OrbitalMotion.Tau / planet.OrbitPeriodSeconds);
                // A circle is the e=0 Kepler special case: loading never teleports a world or its cargo.
                planet.OrbitEccentricity = 0;
                planet.OrbitPeriapsis = 0;
            }
            state.Version = 9;
        }
    }
}
