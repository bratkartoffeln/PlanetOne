using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        public BuildingState FindBuilding(long id)
        { foreach (var p in State.Planets) { var b = p.Buildings.Find(v => v.Id == id); if (b != null) return b; } return null; }
        public bool HasTechnology(int faction, Technology technology)
        { var f = State.Factions.Find(v => v.Id == faction); return f != null && f.Research.Completed.Contains(technology); }
        public string BuildingTechnologyError(BuildingKind kind, int faction)
        {
            if (kind == BuildingKind.CrystalMine && !HasTechnology(faction, Technology.CrystalMining)) return "Forschung Kristallabbau fehlt.";
            if (kind == BuildingKind.ElectronicsFactory && !HasTechnology(faction, Technology.Electronics)) return "Forschung Elektronik fehlt.";
            return null;
        }
        public string ResearchError(int faction, Technology technology)
        {
            if (!Enum.IsDefined(typeof(Technology), technology)) return "Unbekannte Technologie.";
            if (HasTechnology(faction, technology)) return "Bereits erforscht.";
            foreach (var requirement in Definitions.Technologies[(int)technology].Prerequisites)
                if (!HasTechnology(faction, requirement)) return "Voraussetzung: " + Definitions.Technologies[(int)requirement].Name;
            return null;
        }
        private bool PortOwned(BuildingState b, int owner)
        { return b != null && b.Kind == BuildingKind.Spaceport && b.Built && !b.Deconstructing && !b.Destroyed && BuildingOwner(FindPlanet(b.PlanetId),b) == owner; }
        private void ApplySpace(SimCommand c)
        {
            var p = FindPlanet(c.PlanetId); var b = FindBuilding(c.TargetId);
            var ship = State.Ships.Find(s => s.Id == c.ActorId && s.OwnerId == c.IssuerId && !s.Destroyed && !s.Consumed);
            string error = null;
            if (c.Kind == CommandKind.StartResearch)
            {
                var research = State.Factions.Find(f => f.Id == c.IssuerId).Research;
                error = ResearchError(c.IssuerId, (Technology)c.IntValue);
                if (b == null || b.Kind != BuildingKind.Laboratory || !b.Built || b.Deconstructing || BuildingOwner(FindPlanet(b.PlanetId),b) != c.IssuerId) error = "Eigenes fertiges Labor benötigt.";
                else if (research.Active >= 0 && research.Active != c.IntValue) error = "Laufende Forschung zuerst abschließen.";
                if (error == null) { research.Active = c.IntValue; research.LaboratoryId = b.Id; research.Status = "Material wird angeliefert"; }
            }
            else if (c.Kind == CommandKind.BuildShip)
            {
                if (!PortOwned(b, c.IssuerId) || b.ShipBuildKind >= 0) error = "Freier eigener Raumhafen benötigt.";
                else if (!Enum.IsDefined(typeof(ShipKind), c.IntValue)) error = "Unbekannter Schiffstyp.";
                else if (c.IntValue > (int)ShipKind.OrbitalYard) error = "Militärschiffe benötigen eine Orbitalwerft.";
                else if (!HasTechnology(c.IssuerId, Definitions.Ships[c.IntValue].Required)) error = "Schiffstechnologie fehlt.";
                else { b.ShipBuildKind = c.IntValue; b.ShipBuildPaid = false; b.ShipBuildProgress = 0; }
            }
            else if (c.Kind == CommandKind.SetSupplyPolicy)
            {
                if (!PortOwned(b, c.IssuerId)) error = "Eigener Raumhafen benötigt.";
                else if (c.IntValue < 0 || c.IntValue >= 6 || c.Minimum < 0 || c.Amount < c.Minimum || c.Amount > 200 || c.Reserve < 0 || c.Reserve > 200 || c.X < 0 || c.X > 2) error = "Ungültige Versorgungswerte.";
                else { int r = c.IntValue; b.Supply.Minimum[r] = c.Minimum; b.Supply.Target[r] = c.Amount; b.Supply.ExportReserve[r] = c.Reserve; b.Supply.Priority[r] = c.X; }
            }
            else if (ship == null) error = "Eigenes aktives Schiff benötigt.";
            else if (ship.Kind == ShipKind.OrbitalYard || ship.TransportPhase != TransportPhase.Space) error = "Station oder gelandeter Träger kann keinen Raumflugauftrag ausführen.";
            else if (c.Kind == CommandKind.CancelFreight)
            { CancelFreight(ship); ship.AutoSupply = false; foreach (var route in State.Routes) if (route.ShipId == ship.Id) route.Enabled = false; }
            else if (c.Kind == CommandKind.SetAutoSupply)
            { if (ship.Kind != ShipKind.Freighter) error = "Frachter benötigt."; else { ship.AutoSupply = c.BoolValue; foreach (var route in State.Routes) if (route.ShipId == ship.Id) route.Enabled = false; } }
            else if (c.Kind == CommandKind.AssignFreight || c.Kind == CommandKind.SetTradeRoute)
            {
                var target = p.Buildings.Find(v => PortOwned(v, c.IssuerId));
                if (!PortOwned(b, c.IssuerId) || target == null || b.PlanetId == p.Id || c.IntValue < 0 || c.IntValue >= 6 || c.Amount <= 0 || c.Amount > Definitions.Ships[0].Capacity || ship.Kind != ShipKind.Freighter)
                    error = "Gültige eigene Häfen, Ware und Frachtmenge benötigt.";
                else if (c.Kind == CommandKind.SetTradeRoute)
                {
                    State.Routes.RemoveAll(r => r.ShipId == ship.Id);
                    State.Routes.Add(new TradeRoute { Id = State.NextEntityId++, ShipId = ship.Id, SourceId = b.Id, TargetId = target.Id, Resource = (ResourceKind)c.IntValue, Amount = c.Amount });
                    ship.AutoSupply = false;
                }
                else if (!TryFreight(ship, b, target, c.IntValue, c.Amount)) error = "Kein freier Frachter, exportierbarer Bestand oder Zielplatz.";
            }
            else if (ship.Freight != null || Total(ship.Cargo) > 0 && ship.Kind != ShipKind.ColonyShip && ship.Kind != ShipKind.LandingTransport) error = "Frachtauftrag zuerst abschließen oder abbrechen.";
            else if (c.Kind == CommandKind.Colonize) error = Colonize(ship, p);
            else if (c.Kind == CommandKind.MoveShipPosition)
            {
                if (Math.Abs((long)c.X) > 200 || Math.Abs((long)c.Y) > 200) error = "Flugziel außerhalb des Sternsystems.";
                else { ship.GoalX = c.X; ship.GoalY = c.Y; ship.Order = ShipOrder.MovePosition; ship.AutoSupply = false; }
            }
            else if (c.Kind == CommandKind.FlyShip || c.Kind == CommandKind.GuardPlanet)
            {
                if (!IsPlanetKnown(c.IssuerId, p.Id)) error = "Zielwelt ist unbekannt.";
                else { ship.Order = c.Kind == CommandKind.FlyShip ? ShipOrder.Fly : ShipOrder.Guard; ship.TargetPlanetId = p.Id; ship.AutoSupply = false; }
            }
            else if (c.Kind == CommandKind.EscortShip)
            {
                var target = State.Ships.Find(s => s.Id == c.TargetId && s.OwnerId == c.IssuerId && !s.Destroyed && !s.Consumed);
                if (target == null || target.Id == ship.Id || EscortCycle(ship.Id, target)) error = "Ungültiges Eskortziel.";
                else { ship.Order = ShipOrder.Escort; ship.EscortId = target.Id; ship.AutoSupply = false; }
            }
            else error = "Unbekannter Raumbefehl.";
            Report(c, error == null, error == null ? "SpaceOrderAccepted" : "InvalidSpaceOrder", error ?? "Auftrag angenommen.");
        }
        private bool EscortCycle(long ownId, ShipState target)
        {
            var seen = new HashSet<long> { ownId };
            while (target != null) { if (!seen.Add(target.Id)) return true; target = target.Order == ShipOrder.Escort ? State.Ships.Find(s => s.Id == target.EscortId) : null; }
            return false;
        }
        private string Colonize(ShipState ship, PlanetState p)
        {
            if (ship.Kind != ShipKind.ColonyShip || p.OwnerId != 0 || p.Buildings.Count != 0) return "Unbesiedelte Welt und Kolonieschiff benötigt.";
            if (!IsPlanetVisible(ship.OwnerId, p.Id) || Distance(ship.X, ship.Y, GetOrbitPosition(p.Id)) > Definitions.DockDistance) return "Kolonieschiff muss die aufgeklärte Zielwelt erreichen.";
            int x = 8, y = 6;
            if (p.Terrain[y * p.Width + x] != 0) return "Gründungsfläche ist blockiert.";
            var headquarters = new BuildingState { Id = State.NextEntityId++, PlanetId = p.Id, Kind = BuildingKind.Headquarters, Built = true, X = x, Y = y, Health = Definitions.BuildingHealth(BuildingKind.Headquarters), Inventory = (int[])ship.Cargo.Clone() };
            p.OwnerId = ship.OwnerId; p.Population = ship.Crew; p.HousingCapacity = 16; p.WorkersTotal = (int)(p.Population * Definitions.WorkforceFraction);
            p.Buildings.Add(headquarters);
            for (int rx = 2; rx <= 15; rx++) if (p.Terrain[6 * p.Width + rx] == 0 && rx != x) p.Roads[6 * p.Width + rx] = true;
            for (int ry = 2; ry <= 11; ry++) if (p.Terrain[ry * p.Width + x] == 0 && ry != y) p.Roads[ry * p.Width + x] = true;
            ship.Cargo = new int[6]; ship.Crew = 0; ship.Consumed = true; ship.Order = ShipOrder.Hold; ship.Status = "In Kolonie umgewandelt";
            Emit(ship.OwnerId, p.Id, ship.Id, "ColonyFounded", "Kolonie auf " + p.Name + " gegründet; Schiff und Ausrüstung wurden übertragen.");
            return null;
        }
        private void PlanSpaceInputs(PlanetState p, BuildingState b)
        {
            var desired = new int[6];
            if (b.ShipBuildKind >= 0 && !b.ShipBuildPaid) Array.Copy(Definitions.Ships[b.ShipBuildKind].Cost, desired, 6);
            var research = State.Factions.Find(f => f.Id == p.OwnerId).Research;
            if (research.Active >= 0 && research.LaboratoryId == b.Id && !research.Paid)
                for (int r = 0; r < 6; r++) desired[r] = Math.Max(desired[r], Definitions.Technologies[research.Active].Cost[r]);
            if (b.Kind == BuildingKind.Spaceport)
                for (int r = 0; r < 6; r++) desired[r] = Math.Max(desired[r], b.Supply.Target[r]);
            if (b.Kind == BuildingKind.Spaceport)
                foreach (var yard in State.Ships)
                    if (!yard.Destroyed && yard.Kind == ShipKind.OrbitalYard && yard.OwnerId == BuildingOwner(p,b) && yard.TargetPlanetId == p.Id)
                        for (int r=0;r<6;r++) desired[r]=Math.Max(desired[r], yard.ProductionKind>=0 && !yard.ProductionPaid ? Math.Max(0,Definitions.Ships[yard.ProductionKind].Cost[r]-yard.Cargo[r]) : r==3?8:0);
            for (int r = 0; r < 6; r++) if (desired[r] > 0) PlanInput(p, b, r, desired[r]);
        }
        private bool Pay(PlanetState p, BuildingState b, int[] cost)
        {
            for (int r = 0; r < 6; r++) if (b.Inventory[r] - b.ReservedOutgoing[r] < cost[r]) return false;
            for (int r = 0; r < 6; r++) { b.Inventory[r] -= cost[r]; p.ConsumedTotals[r] += cost[r]; }
            return true;
        }
        private void TickSpace()
        {
            foreach (var p in State.Planets) foreach (var b in p.Buildings)
            {
                if (!b.Operational || b.Deconstructing || b.ShipBuildKind < 0) continue;
                var d = Definitions.Ships[b.ShipBuildKind];
                if (!b.ShipBuildPaid)
                {
                    if (p.Population - d.Crew < 4) { b.Status = "Zu wenig Besatzung; vier Bewohner bleiben zurück"; continue; }
                    if (!Pay(p, b, d.Cost)) { b.Status = "Wartet auf Schiffsmaterial"; continue; }
                    p.Population -= d.Crew; p.WorkersTotal = Math.Min(p.WorkersTotal, p.Population); p.WorkersUsed = Math.Min(p.WorkersUsed, p.WorkersTotal); b.ShipBuildPaid = true;
                }
                b.ShipBuildProgress += Definitions.TickSeconds * p.Productivity; b.Status = "Schiffsbau " + (int)(100 * b.ShipBuildProgress / d.Seconds) + "%";
                if (b.ShipBuildProgress + 0.0000001 < d.Seconds) continue;
                var pos = GetOrbitPosition(p.Id);
                State.Ships.Add(new ShipState { Id = State.NextEntityId++, OwnerId = p.OwnerId, Kind = (ShipKind)b.ShipBuildKind, X = pos.X, Y = pos.Y, Crew = d.Crew, Cargo = (int[])d.ColonyCargo.Clone(), TargetPlanetId = p.Id, Order = ShipOrder.Guard, Health = Definitions.SpaceWeapons[b.ShipBuildKind].Health });
                for (int r = 0; r < 6; r++) p.ConsumedTotals[r] -= d.ColonyCargo[r];
                b.ShipBuildKind = -1; b.ShipBuildPaid = false; b.ShipBuildProgress = 0; b.Status = "Schiff fertiggestellt";
            }
            foreach (var f in State.Factions)
            {
                var research = f.Research; if (research.Active < 0) continue;
                var b = FindBuilding(research.LaboratoryId);
                if (b == null || b.Deconstructing || BuildingOwner(FindPlanet(b.PlanetId),b) != f.Id) { research.Status = "Forschung pausiert: neues Labor zuweisen"; continue; }
                if (!b.Operational) { research.Status = "Forschung pausiert: " + b.Status; continue; }
                var d = Definitions.Technologies[research.Active];
                if (!research.Paid) { if (!Pay(FindPlanet(b.PlanetId), b, d.Cost)) { research.Status = "Versuchsmaterial wird angeliefert"; continue; } research.Paid = true; }
                research.Progress += Definitions.TickSeconds * FindPlanet(b.PlanetId).Productivity; research.Status = d.Name + " " + (int)(100 * research.Progress / d.Seconds) + "%";
                if (research.Progress + 0.0000001 < d.Seconds) continue;
                research.Completed.Add(d.Id); research.Active = -1; research.Paid = false; research.Progress = 0; research.Status = d.Name + " freigeschaltet";
                Emit(f.Id, b.PlanetId, b.Id, "ResearchCompleted", research.Status);
            }
            var occupiedBerths = new HashSet<long>();
            foreach (var ship in State.Ships)
            {
                if (ship.Destroyed || ship.Consumed) continue;
                if (ship.Kind == ShipKind.OrbitalYard || ship.TransportPhase != TransportPhase.Space) continue;
                if (ship.Freight == null && ship.Kind == ShipKind.Freighter && Total(ship.Cargo) > 0) RecoverCargo(ship);
                if (ship.Freight == null && Total(ship.Cargo) == 0)
                {
                    var route = State.Routes.Find(r => r.ShipId == ship.Id && r.Enabled);
                    if (route != null && !TryFreight(ship, FindBuilding(route.SourceId), FindBuilding(route.TargetId), (int)route.Resource, route.Amount)) ship.Status = "Route wartet: Quellbestand, Exportreserve oder Zielplatz";
                    else if (route == null && ship.AutoSupply) PlanAutomaticFreight(ship);
                }
                if (ship.Freight != null) { TickFreight(ship, occupiedBerths); continue; }
                if (ship.Order == ShipOrder.MovePosition)
                {
                    ship.Status = "Freier Flug / Erkundung";
                    if (MoveShip(ship, new SimPoint(ship.GoalX, ship.GoalY))) { ship.Order = ShipOrder.Hold; ship.Status = "Position erreicht"; }
                }
                else if (ship.Order == ShipOrder.Fly || ship.Order == ShipOrder.Guard)
                {
                    MoveShip(ship, GetOrbitPosition(ship.TargetPlanetId));
                    ship.Status = ship.Order == ShipOrder.Fly ? "Flug zu " + FindPlanet(ship.TargetPlanetId).Name : "Bewacht " + FindPlanet(ship.TargetPlanetId).Name;
                    if (ship.Order == ShipOrder.Fly && Distance(ship.X, ship.Y, GetOrbitPosition(ship.TargetPlanetId)) <= Definitions.DockDistance) { ship.Order = ShipOrder.Guard; ship.Status = "Ziel erreicht"; }
                }
                else if (ship.Order == ShipOrder.Escort)
                {
                    var target = State.Ships.Find(s => s.Id == ship.EscortId && !s.Destroyed && !s.Consumed && s.OwnerId == ship.OwnerId);
                    if (target == null) { ship.Order = ShipOrder.Hold; ship.Status = "Eskortziel nicht verfügbar"; }
                    else { MoveShip(ship, new SimPoint(target.X, target.Y)); ship.Status = "Eskortiert Schiff " + target.Id; }
                }
            }
            UpdateSpaceKnowledge();
        }
        private static double Distance(double x, double y, SimPoint target) { return Math.Sqrt((x - target.X) * (x - target.X) + (y - target.Y) * (y - target.Y)); }
        private bool MoveShip(ShipState ship, SimPoint target)
        {
            double distance = Distance(ship.X, ship.Y, target), step = Definitions.Ships[(int)ship.Kind].Speed * Definitions.TickSeconds;
            if (distance <= step) { ship.X = target.X; ship.Y = target.Y; return true; }
            ship.X += (target.X - ship.X) / distance * step; ship.Y += (target.Y - ship.Y) / distance * step; return false;
        }
        private bool TryFreight(ShipState ship, BuildingState source, BuildingState target, int resource, int requested)
        {
            if (ship.Kind != ShipKind.Freighter || ship.Freight != null || Total(ship.Cargo) != 0 || !PortOwned(source, ship.OwnerId) || !PortOwned(target, ship.OwnerId) || source.PlanetId == target.PlanetId) return false;
            int available = source.Inventory[resource] - source.ReservedOutgoing[resource] - source.Supply.ExportReserve[resource];
            int amount = Math.Min(Math.Min(requested, Definitions.Ships[(int)ship.Kind].Capacity), Math.Min(available, FreeSpace(target)));
            if (amount <= 0) return false;
            source.ReservedOutgoing[resource] += amount; target.ReservedIncoming[resource] += amount;
            ship.Freight = new FreightJob { SourceId = source.Id, TargetId = target.Id, Resource = (ResourceKind)resource, Amount = amount };
            ship.Order = ShipOrder.Freight; return true;
        }
        private void PlanAutomaticFreight(ShipState ship)
        {
            for (int priority = 2; priority >= 0; priority--) foreach (var p in State.Planets) foreach (var target in p.Buildings)
            {
                if (!PortOwned(target, ship.OwnerId)) continue;
                for (int r = 0; r < 6; r++)
                {
                    int stock = target.Inventory[r] + target.ReservedIncoming[r];
                    if (stock < target.Supply.Minimum[r]) target.Supply.Replenishing[r] = true;
                    if (stock >= target.Supply.Target[r]) target.Supply.Replenishing[r] = false;
                    if (!target.Supply.Replenishing[r] || target.Supply.Priority[r] != priority) continue;
                    foreach (var sourcePlanet in State.Planets) foreach (var source in sourcePlanet.Buildings)
                        if (TryFreight(ship, source, target, r, target.Supply.Target[r] - stock)) return;
                    ship.Status = "Versorgungsbedarf: " + p.Name + " / " + Definitions.ResourceName((ResourceKind)r) + "; exportierbarer Bestand fehlt";
                }
            }
        }
        private void TickFreight(ShipState ship, HashSet<long> berths)
        {
            var job = ship.Freight; var source = FindBuilding(job.SourceId); var target = FindBuilding(job.TargetId);
            bool collecting = job.Phase <= FreightPhase.Loading;
            if (!PortOwned(target, ship.OwnerId) || collecting && !PortOwned(source, ship.OwnerId)) { CancelFreight(ship); ship.Status = "Hafen verloren; Fracht bleibt an Bord"; return; }
            var port = collecting ? source : target;
            bool arrived = MoveShip(ship, GetOrbitPosition(port.PlanetId));
            ship.Status = (collecting ? "Abholung: " : "Lieferung: ") + FindPlanet(port.PlanetId).Name + " / " + job.Amount + " " + Definitions.ResourceName(job.Resource);
            if (!arrived) return;
            if (!port.Operational) { ship.Status = "Hafentransfer pausiert: " + port.Status; return; }
            if (!berths.Add(port.Id)) { ship.Status = "Wartet auf freien Hafenanschluss"; return; }
            if (job.Phase == FreightPhase.Collect) { job.Phase = FreightPhase.Loading; job.Progress = 0; }
            if (job.Phase == FreightPhase.Deliver) { job.Phase = FreightPhase.Unloading; job.Progress = 0; }
            job.Progress += Definitions.TickSeconds;
            if (job.Progress + 0.0000001 < job.Amount * Definitions.SpaceTransferSeconds) return;
            int r = (int)job.Resource;
            if (collecting)
            {
                source.Inventory[r] -= job.Amount; source.ReservedOutgoing[r] -= job.Amount; ship.Cargo[r] += job.Amount;
                job.Phase = FreightPhase.Deliver; job.Progress = 0;
            }
            else
            {
                target.Inventory[r] += job.Amount; target.ReservedIncoming[r] -= job.Amount; ship.Cargo[r] -= job.Amount;
                ship.Freight = null; ship.Order = ShipOrder.Guard; ship.TargetPlanetId = target.PlanetId; ship.Status = "Lieferung abgeschlossen";
            }
        }
        private void CancelFreight(ShipState ship)
        {
            var job = ship.Freight;
            if (job != null)
            {
                var source = FindBuilding(job.SourceId); var target = FindBuilding(job.TargetId); int r = (int)job.Resource;
                if (source != null && job.Phase <= FreightPhase.Loading) source.ReservedOutgoing[r] -= job.Amount;
                if (target != null) target.ReservedIncoming[r] -= job.Amount;
            }
            ship.Freight = null; ship.Order = ShipOrder.Hold; ship.Status = Total(ship.Cargo) > 0 ? "Fracht gesichert; sucht eigenen Rücknahmehafen" : "Auftrag abgebrochen";
        }
        private void RecoverCargo(ShipState ship)
        {
            for (int r = 0; r < 6; r++) if (ship.Cargo[r] > 0)
            {
                BuildingState nearest = null; double best = double.MaxValue;
                foreach (var p in State.Planets) foreach (var port in p.Buildings)
                    if (PortOwned(port, ship.OwnerId) && FreeSpace(port) >= ship.Cargo[r])
                    { double distance = Distance(ship.X, ship.Y, GetOrbitPosition(p.Id)); if (distance < best) { best = distance; nearest = port; } }
                if (nearest == null) { ship.Status = "Fracht an Bord: kein eigener Hafen mit freiem Platz"; return; }
                nearest.ReservedIncoming[r] += ship.Cargo[r];
                ship.Freight = new FreightJob { SourceId = 0, TargetId = nearest.Id, Resource = (ResourceKind)r, Amount = ship.Cargo[r], Phase = FreightPhase.Deliver };
                ship.Order = ShipOrder.Freight; return;
            }
        }
        public void DestroyShip(long shipId)
        {
            var ship = State.Ships.Find(s => s.Id == shipId); if (ship == null || ship.Destroyed || ship.Consumed) return;
            CancelFreight(ship); for (int r = 0; r < 6; r++) { State.SpaceLostTotals[r] += ship.Cargo[r]; ship.Cargo[r] = 0; }
            ship.Destroyed = true; ship.Crew = 0; ship.AutoSupply = false; ship.Status = "Zerstört";
            ship.Health = 0; ship.Order = ShipOrder.Hold;
            foreach (var troop in ship.Troops) { troop.Destroyed = true; troop.Health = 0; State.SpaceLostTotals[3] += troop.Parts; troop.Parts = 0; }
            ship.ProductionKind = -1; ship.ProductionPaid = false; ship.ProductionProgress = 0;
            foreach (var route in State.Routes) if (route.ShipId == ship.Id) route.Enabled = false;
        }
        private void CancelPortWork(PlanetState p, BuildingState b)
        {
            foreach (var ship in State.Ships) if (ship.Freight != null && (ship.Freight.SourceId == b.Id || ship.Freight.TargetId == b.Id)) CancelFreight(ship);
            if (b.ShipBuildPaid && b.ShipBuildKind >= 0)
            {
                var d = Definitions.Ships[b.ShipBuildKind];
                if (!b.Destroyed) { for (int r = 0; r < 6; r++) { b.Inventory[r] += d.Cost[r]; p.ConsumedTotals[r] -= d.Cost[r]; } p.Population += d.Crew; }
            }
            b.ShipBuildKind = -1; b.ShipBuildPaid = false; b.ShipBuildProgress = 0;
        }
        public bool IsPlanetVisible(int faction, int planetId)
        { var p = FindPlanet(planetId); if (p.OwnerId == faction) return true; var point = GetOrbitPosition(planetId); return SpacePointVisible(faction, point); }
        public bool IsPlanetKnown(int faction, int planetId)
        { return IsPlanetVisible(faction, planetId) || State.SpaceSightings.Exists(s => s.FactionId == faction && s.PlanetId == planetId); }
        public bool IsShipVisible(int faction, ShipState ship)
        { return !ship.Destroyed && !ship.Consumed && ship.TransportPhase == TransportPhase.Space && (ship.OwnerId == faction || SpacePointVisible(faction, new SimPoint(ship.X, ship.Y))); }
        private bool SpacePointVisible(int faction, SimPoint point)
        {
            foreach (var p in State.Planets) if (p.OwnerId == faction && Distance(point.X, point.Y, GetOrbitPosition(p.Id)) <= 6) return true;
            foreach (var s in State.Ships) if (s.OwnerId == faction && !s.Destroyed && !s.Consumed && Distance(s.X, s.Y, point) <= Definitions.Ships[(int)s.Kind].Sight) return true;
            return false;
        }
        private void UpdateSpaceKnowledge()
        {
            foreach (var faction in State.Factions)
            {
                foreach (var p in State.Planets) if (IsPlanetVisible(faction.Id, p.Id))
                {
                    var sight = State.SpaceSightings.Find(v => v.FactionId == faction.Id && v.PlanetId == p.Id);
                    if (sight == null) { sight = new SpaceSighting { FactionId = faction.Id, PlanetId = p.Id }; State.SpaceSightings.Add(sight); }
                    sight.KnownOwnerId = p.OwnerId; sight.Tick = State.Tick; var point = GetOrbitPosition(p.Id); sight.X = point.X; sight.Y = point.Y;
                }
                foreach (var ship in State.Ships) if (ship.OwnerId != faction.Id && IsShipVisible(faction.Id, ship))
                {
                    var sight = State.SpaceSightings.Find(v => v.FactionId == faction.Id && v.ShipId == ship.Id);
                    if (sight == null) { sight = new SpaceSighting { FactionId = faction.Id, ShipId = ship.Id }; State.SpaceSightings.Add(sight); }
                    sight.KnownOwnerId = ship.OwnerId; sight.Kind = ship.Kind; sight.X = ship.X; sight.Y = ship.Y; sight.Tick = State.Tick;
                }
            }
        }
    }
}
