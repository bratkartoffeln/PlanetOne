using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlanetOne.Core
{
    [DataContract, Serializable]
    public sealed class AsteroidBelt
    {
        [DataMember] public double InnerRadius, OuterRadius;
    }
    [DataContract, Serializable]
    public sealed class AsteroidState
    {
        [DataMember] public long Id;
        [DataMember] public double OrbitRadius, Phase, Inclination, DiameterKm;
        [DataMember] public int InitialOre, RemainingOre, ReservedOre;
    }
    [DataContract, Serializable]
    public sealed class MiningJob
    {
        [DataMember] public long AsteroidId, PortId;
        [DataMember] public int Amount, Extracted;
        [DataMember] public double Progress;
    }
    public static class AsteroidGeneration
    {
        public const double SecondsPerOre = 1.5;
        // Stable across runtimes. A seed is a system, not a fresh roll on load or view changes.
        private static uint Next(ref uint value) { value ^= value << 13; value ^= value >> 17; value ^= value << 5; return value; }
        private static double Sample(ref uint value) { return Next(ref value) / 4294967296.0; }
        public static void Populate(GameState state)
        {
            if(state.Tick!=0 || state.Ships.Count!=0)throw new InvalidOperationException("Gürtel nur bei der Systemerzeugung anlegen.");
            state.AsteroidBelts.Clear();state.Asteroids.Clear();
            uint rng=unchecked((uint)state.Seed*747796405u+2891336453u);if(rng==0)rng=1;
            if(Sample(ref rng)>=.65)return;
            double inner=22,outer=40;
            foreach(var p in state.Planets)if(p.ParentId<0)
            {
                if(p.Kind==PlanetKind.GasGiant)outer=Math.Min(outer,p.OrbitRadius*(1-p.OrbitEccentricity)-5);
                else inner=Math.Max(inner,p.OrbitRadius*(1+p.OrbitEccentricity)+5);
            }
            if(outer-inner<3)return;
            state.AsteroidBelts.Add(new AsteroidBelt {InnerRadius=inner,OuterRadius=outer});
            for(int i=0;i<32;i++)
            {
                double radius=inner+(outer-inner)*Sample(ref rng),phase=OrbitalMotion.Tau*Sample(ref rng);
                double inclination=(Sample(ref rng)-.5)*.06,diameter=.2+Sample(ref rng)*7.8;
                int ore=80+(int)(Sample(ref rng)*241);
                state.Asteroids.Add(new AsteroidState {Id=state.NextEntityId++,OrbitRadius=radius,Phase=phase,Inclination=inclination,DiameterKm=diameter,InitialOre=ore,RemainingOre=ore});
            }
        }
        public static SimPoint Position(AsteroidState asteroid,double seconds)
        {
            double angle=asteroid.Phase+seconds%OrbitalMotion.Period(asteroid.OrbitRadius,false)*OrbitalMotion.Tau/OrbitalMotion.Period(asteroid.OrbitRadius,false);
            return new SimPoint(Math.Cos(angle)*asteroid.OrbitRadius,Math.Sin(angle)*asteroid.OrbitRadius);
        }
    }
}
