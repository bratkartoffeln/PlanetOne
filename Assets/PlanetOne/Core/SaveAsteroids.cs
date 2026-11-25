using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public static partial class SaveCodec
    {
        private static void MigrateAsteroids(GameState state)
        {
            // Old worlds retain their geometry, stocks, identifiers and orders. No deposits are injected.
            state.AsteroidBelts=new List<AsteroidBelt>();state.Asteroids=new List<AsteroidState>();state.SpaceMinedOre=0;state.Version=11;
        }
        private static void ValidateAsteroids(GameState state,HashSet<long> entities)
        {
            Require(state.AsteroidBelts!=null && state.AsteroidBelts.Count<=1 && state.Asteroids!=null && state.Asteroids.Count<=128,"Asteroidendaten fehlen oder sind zu groß.");
            var claims=new Dictionary<long,int>();int mined=0;
            foreach(var belt in state.AsteroidBelts)
                Require(belt!=null && Finite(belt.InnerRadius) && Finite(belt.OuterRadius) && belt.InnerRadius>0 && belt.OuterRadius>belt.InnerRadius && belt.OuterRadius<=180,"Ungültiger Asteroidengürtel.");
            foreach(var a in state.Asteroids)
            {
                Require(a!=null && a.Id>0 && entities.Add(a.Id),"Ungültige Asteroidenkennung.");
                Require(Finite(a.OrbitRadius) && state.AsteroidBelts.Exists(b=>a.OrbitRadius>=b.InnerRadius && a.OrbitRadius<=b.OuterRadius) && Finite(a.Phase) && a.Phase>=0 && a.Phase<OrbitalMotion.Tau && Finite(a.Inclination) && Math.Abs(a.Inclination)<=.1 && Finite(a.DiameterKm) && a.DiameterKm>0 && a.DiameterKm<=100,"Ungültige Asteroidenbahn oder Größe.");
                Require(a.InitialOre>0 && a.InitialOre<=100000 && a.RemainingOre>=0 && a.RemainingOre<=a.InitialOre && a.ReservedOre>=0 && a.ReservedOre<=a.RemainingOre,"Ungültiger Asteroidenbestand.");
                claims.Add(a.Id,0);mined+=a.InitialOre-a.RemainingOre;
            }
            Require(state.SpaceMinedOre==mined,"Asteroidenabbau stimmt nicht mit den endlichen Vorkommen überein.");
            foreach(var ship in state.Ships)
            {
                if(ship.Mining==null){Require(ship.Order!=ShipOrder.Mine,"Abbaubewegung ohne Auftrag.");continue;}
                var j=ship.Mining;
                Require(!ship.Destroyed && !ship.Consumed && ship.Kind==ShipKind.Freighter && ship.TransportPhase==TransportPhase.Space && ship.Order==ShipOrder.Mine && ship.Freight==null && claims.ContainsKey(j.AsteroidId),"Ungültiges Bergbauschiff.");
                Require(j.Amount>0 && j.Amount<=Definitions.Ships[0].Capacity && j.Extracted>=0 && j.Extracted<j.Amount && Finite(j.Progress) && j.Progress>=0 && j.Progress<AsteroidGeneration.SecondsPerOre,"Ungültiger Abbaufortschritt.");
                int total=0;foreach(int n in ship.Cargo)total+=n;
                Require(ship.Cargo[(int)ResourceKind.Ore]==j.Extracted && total==j.Extracted && state.Planets.Exists(p=>p.Buildings.Exists(b=>b.Id==j.PortId && b.Kind==BuildingKind.Spaceport)),"Abbauladung oder Rücknahmehafen fehlt.");
                claims[j.AsteroidId]+=j.Amount-j.Extracted;
            }
            foreach(var a in state.Asteroids)Require(a.ReservedOre==claims[a.Id],"Abbaureservierungen stimmen nicht.");
        }
    }
}
