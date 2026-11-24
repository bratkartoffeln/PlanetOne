using System;

namespace PlanetOne.Core
{
    /// <summary>Scaled two-body Kepler orbits. Angles are radians, radii are semi-major axes.</summary>
    public static class OrbitalMotion
    {
        public const double Tau = Math.PI * 2;
        public const double StellarReferenceRadius = 10, StellarReferencePeriod = 7200;
        public const double LunarReferenceRadius = 3, LunarReferencePeriod = 21600;

        public static double Period(double semiMajorAxis, bool moon)
        {
            return (moon ? LunarReferencePeriod : StellarReferencePeriod) *
                Math.Pow(semiMajorAxis / (moon ? LunarReferenceRadius : StellarReferenceRadius), 1.5);
        }

        public static double Normalize(double angle)
        { double value = angle % Tau; return value < 0 ? value + Tau : value; }

        public static double EccentricAnomaly(double meanAnomaly, double eccentricity)
        {
            double mean = Normalize(meanAnomaly), eccentric = mean;
            // e <= 0.6 is validated on load. Newton converges rapidly in that range.
            for (int i = 0; i < 12; i++)
            {
                double correction = (eccentric - eccentricity * Math.Sin(eccentric) - mean) /
                    (1 - eccentricity * Math.Cos(eccentric));
                eccentric -= correction;
                if (Math.Abs(correction) < 1e-13) break;
            }
            return eccentric;
        }

        public static SimPoint PositionAtMeanAnomaly(PlanetState planet, double meanAnomaly)
        {
            double eccentric = EccentricAnomaly(meanAnomaly, planet.OrbitEccentricity);
            double x = planet.OrbitRadius * (Math.Cos(eccentric) - planet.OrbitEccentricity);
            double y = planet.OrbitRadius * Math.Sqrt(1 - planet.OrbitEccentricity * planet.OrbitEccentricity) * Math.Sin(eccentric);
            double c = Math.Cos(planet.OrbitPeriapsis), s = Math.Sin(planet.OrbitPeriapsis);
            return new SimPoint(x * c - y * s, x * s + y * c);
        }

        public static SimPoint Position(PlanetState planet, double seconds)
        { return PositionAtMeanAnomaly(planet, planet.OrbitPhase + (seconds % planet.OrbitPeriodSeconds) * Tau / planet.OrbitPeriodSeconds); }
    }
}
