using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public static partial class SaveCodec
    {
        private static void MigrateSpace(GameState state)
        {
            state.Ships = new List<ShipState>(); state.Routes = new List<TradeRoute>();
            state.SpaceSightings = new List<SpaceSighting>(); state.SpaceLostTotals = new int[Definitions.ResourceCount]; state.SpaceTestFixture = false;
            foreach (var f in state.Factions) f.Research = new ResearchState();
            foreach (var p in state.Planets)
            {
                foreach (var b in p.Buildings) { b.ShipBuildKind = -1; b.ShipBuildPaid = false; b.ShipBuildProgress = 0; b.Supply = new SupplyPolicy(); }
                if (p.ParentId >= 0 && p.OwnerId == 0 && p.Width == 18 && p.Height == 14 && p.Terrain[9 * 18 + 13] == 2) p.Terrain[9 * 18 + 13] = 3;
            }
            state.Version = 4;
        }
        private static void AddSpaceReservations(GameState state, PlanetState p, Dictionary<long, int[]> outgoing, Dictionary<long, int[]> incoming)
        {
            Require(state.Ships != null, "Schiffsliste fehlt.");
            foreach (var ship in state.Ships)
            {
                Require(ship != null, "Schiff fehlt."); if(ship.Mining!=null) {
                    Require(ship.Mining.Amount>0 && ship.Mining.Amount<=Definitions.Ships[0].Capacity, "Invalid mining reservation.");
                    if(incoming.ContainsKey(ship.Mining.PortId))incoming[ship.Mining.PortId][(int)ResourceKind.Ore]+=ship.Mining.Amount;
                }
                var job = ship.Freight; if (job == null) continue;
                Require(Enum.IsDefined(typeof(ResourceKind), job.Resource) && job.Amount > 0, "Ungültige Raumfracht.");
                if (job.Phase <= FreightPhase.Loading && outgoing.ContainsKey(job.SourceId)) outgoing[job.SourceId][(int)job.Resource] += job.Amount;
                if (incoming.ContainsKey(job.TargetId)) incoming[job.TargetId][(int)job.Resource] += job.Amount;
            }
        }
        private static void ValidateSpace(GameState state, HashSet<int> factions, HashSet<long> entities)
        {
            Require(state.Ships != null && state.Routes != null && state.SpaceSightings != null, "Raumzustand fehlt."); Goods(state.SpaceLostTotals);
            var buildings = new Dictionary<long, BuildingState>();
            foreach (var p in state.Planets) foreach (var b in p.Buildings)
            {
                buildings.Add(b.Id, b);
                Require(b.ShipBuildKind >= -1 && b.ShipBuildKind < Definitions.Ships.Length && Finite(b.ShipBuildProgress) && b.ShipBuildProgress >= 0, "Ungültiger Schiffsbau.");
                Require(b.ShipBuildKind < 0 ? !b.ShipBuildPaid && b.ShipBuildProgress == 0 : b.Kind == BuildingKind.Spaceport && b.Built && !b.Deconstructing, "Schiffsbau ohne Raumhafen.");
                if (b.ShipBuildKind >= 0) Require(b.ShipBuildProgress <= Definitions.Ships[b.ShipBuildKind].Seconds && (b.ShipBuildProgress == 0 || b.ShipBuildPaid), "Ungültiger bezahlter Baufortschritt.");
                Require(b.Supply != null, "Versorgungsregeln fehlen."); Goods(b.Supply.Minimum); Goods(b.Supply.Target); Goods(b.Supply.ExportReserve); Goods(b.Supply.Priority);
                Require(b.Supply.Replenishing != null && b.Supply.Replenishing.Length == Definitions.ResourceCount, "Nachfüllzustand fehlt.");
                for (int r = 0; r < Definitions.ResourceCount; r++) Require(b.Supply.Minimum[r] <= b.Supply.Target[r] && b.Supply.Target[r] <= 200 && b.Supply.ExportReserve[r] <= 200 && b.Supply.Priority[r] <= 2, "Ungültige Versorgungsregel.");
            }
            foreach (var f in state.Factions)
            {
                var research = f.Research;
                Require(research != null && research.Completed != null && research.Active >= -1 && research.Active < Definitions.Technologies.Length && Finite(research.Progress) && research.Progress >= 0, "Ungültige Forschung.");
                var completed = new HashSet<Technology>();
                foreach (var t in research.Completed) Require(Enum.IsDefined(typeof(Technology), t) && completed.Add(t), "Ungültige Freischaltung.");
                foreach (var t in completed) foreach (var prerequisite in Definitions.Technologies[(int)t].Prerequisites) Require(completed.Contains(prerequisite), "Freischaltung ohne Voraussetzung.");
                if (research.Active < 0) Require(!research.Paid && research.Progress == 0, "Verwaister Forschungsfortschritt.");
                else
                {
                    Require(!completed.Contains((Technology)research.Active) && research.Progress < Definitions.Technologies[research.Active].Seconds && (research.Progress == 0 || research.Paid), "Ungültiger Forschungsfortschritt.");
                    foreach (var prerequisite in Definitions.Technologies[research.Active].Prerequisites) Require(completed.Contains(prerequisite), "Forschung ohne Voraussetzung.");
                }
            }
            var ships = new Dictionary<long, ShipState>();
            foreach (var ship in state.Ships)
            {
                Require(ship != null && ship.Id > 0 && entities.Add(ship.Id) && factions.Contains(ship.OwnerId) && Enum.IsDefined(typeof(ShipKind), ship.Kind) && Enum.IsDefined(typeof(ShipOrder), ship.Order), "Ungültige Schiffskennung.");
                Require(Finite(ship.X) && Finite(ship.Y) && Math.Abs(ship.X) <= 10000 && Math.Abs(ship.Y) <= 10000 && ship.Crew >= 0, "Ungültiger Schiffsstandort."); Goods(ship.Cargo);
                Require(Finite(ship.GoalX) && Finite(ship.GoalY) && Math.Abs(ship.GoalX) <= 200 && Math.Abs(ship.GoalY) <= 200 && ship.Crew <= Definitions.Ships[(int)ship.Kind].Crew, "Ungültiges Flugziel oder Besatzung.");
                Require(Finite(ship.TrafficAnchorX) && Finite(ship.TrafficAnchorY) && Finite(ship.TrafficOffsetX) && Finite(ship.TrafficOffsetY) &&
                    Finite(ship.TrafficYieldX) && Finite(ship.TrafficYieldY) && ship.TrafficYieldUntil>=0 && ship.TrafficYieldFor>=0 && ship.TrafficWaitTicks>=0,
                    "Ungültiger Raumverkehrszustand.");
                Require(Math.Abs(ship.TrafficAnchorX)<=10000 && Math.Abs(ship.TrafficAnchorY)<=10000 &&
                    Math.Abs(ship.TrafficYieldX)<=10000 && Math.Abs(ship.TrafficYieldY)<=10000 &&
                    Math.Abs(ship.TrafficOffsetX)<=10000 && Math.Abs(ship.TrafficOffsetY)<=10000,
                    "Raumverkehrsziel außerhalb des Spielraums.");
                int total = 0; foreach (int n in ship.Cargo) total += n;
                Require(total <= Definitions.Ships[(int)ship.Kind].Capacity && !(ship.Destroyed && ship.Consumed), "Überladener oder ungültiger Schiffszustand.");
                Require(!(ship.Destroyed || ship.Consumed) || total == 0 && ship.Crew == 0 && ship.Freight == null, "Entferntes Schiff besitzt Ladung.");
                if (ship.Order == ShipOrder.Guard || ship.Order == ShipOrder.Fly) Require(state.Planets.Exists(p => p.Id == ship.TargetPlanetId), "Schiffsziel fehlt.");
                if (ship.Freight != null)
                {
                    var j = ship.Freight;
                    Require(ship.Kind == ShipKind.Freighter && ship.Order == ShipOrder.Freight && Enum.IsDefined(typeof(FreightPhase), j.Phase) && j.Amount > 0 && j.Amount <= Definitions.Ships[0].Capacity && Finite(j.Progress) && j.Progress >= 0 && j.Progress <= j.Amount * Definitions.SpaceTransferSeconds, "Ungültiger Frachtauftrag.");
                    Require(buildings.ContainsKey(j.TargetId) && buildings[j.TargetId].Kind == BuildingKind.Spaceport, "Frachtziel fehlt.");
                    if (j.Phase <= FreightPhase.Loading) Require(buildings.ContainsKey(j.SourceId) && buildings[j.SourceId].Kind == BuildingKind.Spaceport && total == 0, "Frachtquelle fehlt oder Ladung dupliziert.");
                    else Require(ship.Cargo[(int)j.Resource] == j.Amount && total == j.Amount, "Frachtmanifest stimmt nicht.");
                }
                else Require(ship.Order != ShipOrder.Freight, "Frachtbewegung ohne Auftrag.");
                ships.Add(ship.Id, ship);
            }
            for(int i=0;i<state.Ships.Count;i++)
            {
                var first=state.Ships[i];if(first.Destroyed || first.Consumed || first.TransportPhase!=TransportPhase.Space)continue;
                for(int j=i+1;j<state.Ships.Count;j++)
                {
                    var second=state.Ships[j];if(second.Destroyed || second.Consumed || second.TransportPhase!=TransportPhase.Space)continue;
                    double dx=first.X-second.X,dy=first.Y-second.Y,radius=Definitions.ShipRadius(first.Kind)+Definitions.ShipRadius(second.Kind);
                    Require(dx*dx+dy*dy>=radius*radius-.000001,"Raumschiffkörper überlagern sich.");
                }
            }
            foreach (var ship in state.Ships) if (ship.Order == ShipOrder.Escort)
            {
                var seen = new HashSet<long> { ship.Id }; var current = ship;
                while (current.Order == ShipOrder.Escort)
                { Require(ships.ContainsKey(current.EscortId) && seen.Add(current.EscortId), "Ungültige Eskortkette."); current = ships[current.EscortId]; }
            }
            var routeShips = new HashSet<long>();
            foreach (var route in state.Routes)
                Require(route != null && route.Id > 0 && entities.Add(route.Id) && ships.ContainsKey(route.ShipId) && routeShips.Add(route.ShipId) && Enum.IsDefined(typeof(ResourceKind), route.Resource) && route.Amount > 0 && route.Amount <= Definitions.Ships[0].Capacity, "Ungültige Handelsroute.");
            var sightings = new HashSet<string>();
            foreach (var s in state.SpaceSightings)
                Require(s != null && factions.Contains(s.FactionId) && s.Tick >= 0 && s.Tick <= state.Tick && Finite(s.X) && Finite(s.Y) && (s.KnownOwnerId == 0 || factions.Contains(s.KnownOwnerId)) &&
                    (s.PlanetId > 0 && s.ShipId == 0 && state.Planets.Exists(p => p.Id == s.PlanetId) || s.PlanetId == 0 && s.ShipId > 0) && sightings.Add(s.FactionId + ":" + s.PlanetId + ":" + s.ShipId), "Ungültige Raumsichtung.");
        }
    }
}
