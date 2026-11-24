using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    /// <summary>Authoritative fixed-step simulation. It never receives a camera or active view.</summary>
    public sealed partial class GameSimulation
    {
        public GameState State { get; private set; }

        public GameSimulation(GameState state)
        {
            Definitions.ValidateTechnologies(Definitions.Technologies);
            SaveCodec.Validate(state);
            State = state;
        }

        public static GameSimulation CreateDefault(int seed = 12345)
        {
            var state = new GameState { Seed = seed };
            state.Factions.Add(new FactionState { Id = 1, Name = "Koloniekonsortium", IsPlayer = true });
            state.Factions.Add(new FactionState { Id = 2, Name = "Freie Stationen", IsPlayer = false });
            state.Relations.Add(new FactionRelation { FirstId = 1, SecondId = 2, Relation = RelationKind.Peace });
            state.Planets.Add(CreatePlanet(state, 1, "Aster", -1, 1, 10, 240, 0.2));
            state.Planets.Add(CreatePlanet(state, 2, "Nereid", -1, 1, 18, 420, 2.5));
            state.Planets.Add(CreatePlanet(state, 3, "Khepri", 2, 0, 3, 60, 0));
            state.Planets[2].Terrain[9 * 18 + 13] = 3;
            var simulation = new GameSimulation(state);
            simulation.UpdateSpaceKnowledge();
            return simulation;
        }

        private static PlanetState CreatePlanet(GameState state, int id, string name, int parent,
            int owner, double radius, double period, double phase)
        {
            var p = new PlanetState { Id = id, Name = name, ParentId = parent, OwnerId = owner,
                OrbitRadius = radius, OrbitPeriodSeconds = period, OrbitPhase = phase };
            p.Terrain = new int[p.Width * p.Height];
            p.OreRemaining = new int[p.Terrain.Length];
            p.Roads = new bool[p.Terrain.Length];
            for (int y = 0; y < p.Height; y++)
                for (int x = 0; x < p.Width; x++)
                    if ((x < 2 && y < 3) || (x > 15 && y > 10) || (x == 12 && y == 2))
                        p.Terrain[y * p.Width + x] = 1;
            foreach (var cell in new[] { new GridCell(4, 4), new GridCell(13, 9), new GridCell(3, 10) })
            {
                int index = cell.Y * p.Width + cell.X;
                p.Terrain[index] = 2;
                p.OreRemaining[index] = 1800 + (int)((uint)(state.Seed + id * 191 + index) % 401);
            }
            if (owner != 0)
            {
                for (int x = 2; x <= 15; x++) p.Roads[6 * p.Width + x] = true;
                for (int y = 2; y <= 11; y++) p.Roads[y * p.Width + 8] = true;
                p.Roads[6 * p.Width + 8] = false;
                p.Roads[5 * p.Width + 4] = true;
                var headquarters = new BuildingState { Id = state.NextEntityId++, PlanetId = id,
                    Kind = BuildingKind.Headquarters, X = 8, Y = 6, Built = true, Status = "Bereit" };
                headquarters.Inventory[(int)ResourceKind.Food] = 60;
                headquarters.Inventory[(int)ResourceKind.Metal] = 100;
                headquarters.Inventory[(int)ResourceKind.Parts] = 40;
                p.Buildings.Add(headquarters);
                p.EnergyProduced = 4;
                p.WorkersTotal = 12;
                p.FoodDemandPerMinute = p.Population * Definitions.FoodPerPersonPerSecond * 60;
                p.EnergyCapacity = Definitions.Get(BuildingKind.Headquarters).EnergyStorageCapacity;
            }
            else { p.WorkersTotal = 0; p.Population = 0; p.HousingCapacity = 0; }
            return p;
        }

        public long NextCommandId() { return State.NextCommand++; }

        public bool Enqueue(SimCommand command)
        {
            if (command == null || command.Id <= 0) return Reject("Ungültiger Befehl.");
            if (State.ProcessedCommandIds.Contains(command.Id) ||
                State.PendingCommands.Exists(c => c.Id == command.Id))
            { Report(command, false, "DuplicateCommand", "Befehl bereits erteilt."); return false; }
            State.NextCommand = Math.Max(State.NextCommand, command.Id + 1);
            if (!State.Factions.Exists(f => f.Id == command.IssuerId))
            { Report(command, false, "UnknownFaction", "Unbekannte Fraktion."); State.ProcessedCommandIds.Add(command.Id); return false; }
            if (command.Kind == CommandKind.SetPaused || command.Kind == CommandKind.SetTimeScale)
            {
                State.ProcessedCommandIds.Add(command.Id);
                if (command.IssuerId != State.PlayerFactionId)
                { Report(command, false, "PermissionDenied", "Nur der Spieler steuert die Simulationszeit."); return false; }
                if (command.Kind == CommandKind.SetPaused)
                { State.Paused = command.BoolValue; Report(command, true, "PauseChanged", State.Paused ? "Simulation pausiert." : "Simulation läuft."); }
                else
                {
                    if (command.IntValue != 1 && command.IntValue != 2 && command.IntValue != 4)
                    { Report(command, false, "InvalidValue", "Spieltempo muss 1, 2 oder 4 sein."); return false; }
                    State.TimeScale = command.IntValue; Report(command, true, "SpeedChanged", "Spieltempo: " + command.IntValue + "×.");
                }
                return true;
            }
            // Copy the intent. The presentation cannot change an already queued command afterwards.
            State.PendingCommands.Add(new SimCommand { Id = command.Id, IssuerId = command.IssuerId,
                Kind = command.Kind, PlanetId = command.PlanetId, X = command.X, Y = command.Y,
                Building = command.Building, TargetId = command.TargetId, ActorId = command.ActorId, IntValue = command.IntValue, BoolValue = command.BoolValue, Amount = command.Amount, Minimum = command.Minimum, Reserve = command.Reserve });
            State.LastMessage = State.Paused ? "Befehl für die Fortsetzung vorgemerkt." : "Befehl vorgemerkt.";
            return true;
        }

        public void Advance(double realSeconds)
        {
            if (double.IsNaN(realSeconds) || double.IsInfinity(realSeconds) || realSeconds < 0)
                throw new ArgumentOutOfRangeException("realSeconds");
            if (State.Paused) return;
            State.Accumulator += realSeconds * State.TimeScale;
            while (State.Accumulator + 0.000000001 >= Definitions.TickSeconds)
            {
                State.Accumulator = Math.Max(0, State.Accumulator - Definitions.TickSeconds);
                TickOnce();
            }
        }

        public void Step(int ticks = 1)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException("ticks");
            if (State.Paused) return;
            for (int i = 0; i < ticks; i++) TickOnce();
        }

        private void TickOnce()
        {
            foreach (var command in State.PendingCommands)
            {
                if (State.ProcessedCommandIds.Contains(command.Id)) continue;
                State.ProcessedCommandIds.Add(command.Id);
                Apply(command);
            }
            State.PendingCommands.Clear();
            foreach (var planet in State.Planets)
            {
                TickEconomy(planet);
                PlanDeliveries(planet);
                DispatchDeliveries(planet);
                TickShipments(planet);
                FinishDeconstruction(planet);
                TickMilitary(planet);
            }
            TickSpace();
            TickInvasion();
            State.Tick++;
        }

        public PlanetState FindPlanet(int planetId) { return State.Planets.Find(p => p.Id == planetId); }

        public SimPoint GetOrbitPosition(int planetId)
        {
            PlanetState planet = FindPlanet(planetId);
            if (planet == null) throw new ArgumentException("Unbekannter Himmelskörper.");
            double angle = planet.OrbitPhase + State.Tick * Definitions.TickSeconds * Math.PI * 2 / planet.OrbitPeriodSeconds;
            var point = new SimPoint(Math.Cos(angle) * planet.OrbitRadius, Math.Sin(angle) * planet.OrbitRadius);
            if (planet.ParentId >= 0)
            {
                var parent = GetOrbitPosition(planet.ParentId);
                point.X += parent.X; point.Y += parent.Y;
            }
            return point;
        }

        public SimPoint GetShipmentPosition(ShipmentState shipment)
        {
            if (shipment.Path.Count == 0) return new SimPoint();
            double distance = Math.Min(shipment.DistanceTravelled, shipment.Path.Count - 1);
            int index = Math.Min((int)distance, shipment.Path.Count - 1);
            GridCell a = shipment.Path[index];
            GridCell b = shipment.Path[Math.Min(index + 1, shipment.Path.Count - 1)];
            double fraction = distance - index;
            return new SimPoint(a.X + (b.X - a.X) * fraction, a.Y + (b.Y - a.Y) * fraction);
        }

        public string ValidatePlacement(int planetId, BuildingKind kind, int x, int y, int factionId = 1)
        {
            PlanetState p = FindPlanet(planetId);
            if (!State.Factions.Exists(f => f.Id == factionId)) return "Unbekannte Fraktion.";
            string error = ValidateCell(p, x, y, factionId);
            if (error != null) return error;
            if (!Enum.IsDefined(typeof(BuildingKind), kind) || kind == BuildingKind.Headquarters)
                return "Dieser Gebäudetyp kann hier nicht gebaut werden.";
            string technologyError = BuildingTechnologyError(kind, factionId);
            if (technologyError != null) return technologyError;
            int index = y * p.Width + x;
            if (kind == BuildingKind.CrystalMine && p.Terrain[index] != 3) return "Kristallminen benötigen ein Kristallvorkommen.";
            if (kind != BuildingKind.CrystalMine && p.Terrain[index] == 3) return "Dieses Feld ist für Kristallabbau reserviert.";
            if (p.Roads[index] && kind != BuildingKind.Gate) return "Auf dieser Fläche liegt bereits eine Straße.";
            if (kind == BuildingKind.Mine && p.Terrain[index] != 2) return "Erzminen benötigen ein Erzvorkommen.";
            if (kind != BuildingKind.Mine && p.Terrain[index] == 2) return "Dieses Feld ist für den Erzabbau reserviert.";
            var headquarters = p.Buildings.Find(b => b.Kind == BuildingKind.Headquarters && b.Built);
            var proposedSite = new BuildingState { X = x, Y = y };
            if (headquarters == null || FindPath(p, headquarters, proposedSite) == null)
                return "Zuerst eine Straße vom Kommandozentrum bis an den Bauplatz legen.";
            var definition = Definitions.Get(kind);
            for (int r = 0; r < 6; r++)
            {
                int available = 0;
                foreach (var b in p.Buildings)
                    if (b.Built && !b.Deconstructing && IsStorage(b)) available += b.Inventory[r] - b.ReservedOutgoing[r];
                if (available < definition.Cost[r]) return "Nicht genug " + Definitions.ResourceName((ResourceKind)r) + " in lokalen Lagern.";
            }
            return null;
        }

        private static string ValidateCell(PlanetState p, int x, int y, int factionId)
        {
            if (p == null) return "Unbekannter Planet.";
            if (p.OwnerId == 0 || p.OwnerId != factionId) return "Keine eigene Kolonie an diesem Ort.";
            if (x < 0 || y < 0 || x >= p.Width || y >= p.Height) return "Außerhalb der Baufläche.";
            if (p.Terrain[y * p.Width + x] == 1) return "Fels ist nicht bebaubar.";
            if (p.Buildings.Exists(b => b.X == x && b.Y == y)) return "Die Baufläche ist bereits belegt.";
            return null;
        }

        private bool Reject(string message) { State.LastMessage = message; return false; }

        private void Apply(SimCommand command)
        {
            PlanetState p = FindPlanet(command.PlanetId);
            if (!State.Factions.Exists(f => f.Id == command.IssuerId))
            { Report(command, false, "UnknownFaction", "Unbekannte Fraktion."); return; }
            if (p == null) { Report(command, false, "UnknownPlanet", "Unbekannter Planet."); return; }
            if (command.Kind >= CommandKind.AttackShip) { ApplyInvasion(command); return; }
            if (command.Kind >= CommandKind.BuildShip) { ApplySpace(command); return; }
            if (command.Kind >= CommandKind.RecruitUnit) { ApplyMilitary(p, command); return; }
            if (p.OwnerId != command.IssuerId || p.OwnerId == 0)
            { Report(command, false, "PermissionDenied", "Keine eigene Kolonie an diesem Ort."); return; }
            string error;
            if (command.Kind == CommandKind.RemoveRoad)
            {
                if (command.X < 0 || command.Y < 0 || command.X >= p.Width || command.Y >= p.Height ||
                    !p.Roads[command.Y * p.Width + command.X])
                { Report(command, false, "InvalidTarget", "Hier liegt keine Straße."); return; }
                p.Roads[command.Y * p.Width + command.X] = false;
                Report(command, true, "RoadRemoved", "Straße entfernt. Betroffene Lieferungen suchen einen Weg oder warten."); return;
            }
            if (command.Kind == CommandKind.PlaceRoad)
            {
                error = ValidateCell(p, command.X, command.Y, command.IssuerId);
                if (error != null) { Report(command, false, "InvalidPlacement", error); return; }
                int index = command.Y * p.Width + command.X;
                if (p.Roads[index]) { Report(command, false, "Occupied", "Hier liegt bereits eine Straße."); return; }
                if (p.Terrain[index] >= 2) { Report(command, false, "InvalidTerrain", "Erzvorkommen für den Bergbau freihalten."); return; }
                p.Roads[index] = true;
                Report(command, true, "RoadPlaced", "Straße angelegt. Straßenbau ist vorläufig kostenfrei.");
                return;
            }
            if (command.Kind == CommandKind.SetBuildingEnabled || command.Kind == CommandKind.SetBuildingPriority ||
                command.Kind == CommandKind.CancelConstruction || command.Kind == CommandKind.DemolishBuilding)
            {
                var target = p.Buildings.Find(b => b.Id == command.TargetId);
                if (target == null || target.Deconstructing || BuildingOwner(p,target)!=command.IssuerId)
                { Report(command, false, "InvalidTarget", "Das Gebäude fehlt oder wird bereits zurückgebaut."); return; }
                if (target.Kind == BuildingKind.Headquarters)
                { Report(command, false, "ProtectedHeadquarters", "Das Kommandozentrum sichert die Grundversorgung."); return; }
                if (command.Kind == CommandKind.SetBuildingEnabled)
                {
                    if (!target.Built) { Report(command, false, "InvalidTarget", "Die Baustelle ist noch nicht fertig."); return; }
                    target.Enabled = command.BoolValue;
                    Report(command, true, "BuildingEnabled", target.Enabled ? "Betrieb eingeschaltet." : "Betrieb angehalten."); return;
                }
                if (command.Kind == CommandKind.SetBuildingPriority)
                {
                    if (command.IntValue < 0 || command.IntValue > 2)
                    { Report(command, false, "InvalidValue", "Priorität muss 0, 1 oder 2 sein."); return; }
                    target.Priority = command.IntValue; Report(command, true, "PriorityChanged", "Betriebspriorität geändert."); return;
                }
                if ((command.Kind == CommandKind.CancelConstruction && target.Built) ||
                    (command.Kind == CommandKind.DemolishBuilding && !target.Built))
                { Report(command, false, "InvalidTarget", target.Built ? "Fertiges Gebäude mit Abriss zurückbauen." : "Baustelle mit Bauabbruch zurückbauen."); return; }
                BeginDeconstruction(p, target);
                Report(command, true, "DeconstructionStarted", "Rückbau begonnen. Material wird vor Ort geborgen und abgeholt."); return;
            }
            if (command.Kind != CommandKind.PlaceBuilding) { Report(command, false, "InvalidCommand", "Unbekannter Befehl."); return; }
            error = ValidatePlacement(command.PlanetId, command.Building, command.X, command.Y, command.IssuerId);
            if (error != null) { Report(command, false, PlacementCode(error), error); return; }
            var building = new BuildingState { Id = State.NextEntityId++, PlanetId = p.Id,
                Kind = command.Building, X = command.X, Y = command.Y,
                Health = Definitions.BuildingHealth(command.Building),
                Priority = command.Building == BuildingKind.Farm ? 2 : 1 };
            p.Buildings.Add(building);
            var definition = Definitions.Get(command.Building);
            for (int r = 0; r < 6; r++)
            {
                int remaining = definition.Cost[r];
                foreach (var source in p.Buildings)
                {
                    if (!source.Built || source.Deconstructing || !IsStorage(source) || remaining == 0) continue;
                    int amount = Math.Min(remaining, source.Inventory[r] - source.ReservedOutgoing[r]);
                    if (amount <= 0) continue;
                    ReserveDelivery(p, source, building, r, amount);
                    remaining -= amount;
                }
            }
            Report(command, true, "BuildingPlanned", definition.Name + " geplant. Baumaterial wird angeliefert.");
        }

        private void Report(SimCommand command, bool success, string code, string message)
        {
            State.LastMessage = message;
            State.CommandResults.Add(new CommandResult { CommandId = command.Id, Tick = State.Tick,
                Success = success, Code = code, Message = message });
            if (State.CommandResults.Count > Definitions.EventHistoryLimit) State.CommandResults.RemoveAt(0);
            Emit(command.IssuerId, command.PlanetId, command.TargetId, success ? code : "CommandRejected", message);
        }

        private void Emit(int factionId, int planetId, long entityId, string code, string message)
        {
            State.Events.Add(new SimulationEvent { Id = State.NextEventId++, Tick = State.Tick,
                FactionId = factionId, PlanetId = planetId, EntityId = entityId, Code = code, Message = message });
            if (State.Events.Count > Definitions.EventHistoryLimit) State.Events.RemoveAt(0);
        }

        private static string PlacementCode(string message)
        {
            if (message.StartsWith("Nicht genug", StringComparison.Ordinal)) return "InsufficientStock";
            if (message.StartsWith("Zuerst", StringComparison.Ordinal)) return "RoadRequired";
            if (message.Contains("belegt") || message.Contains("bereits")) return "Occupied";
            return "InvalidTerrain";
        }

        private static bool IsStorage(BuildingState b)
        { return b.Kind == BuildingKind.Headquarters || b.Kind == BuildingKind.Warehouse; }

        private static int Total(int[] goods)
        { int total = 0; foreach (int amount in goods) total += amount; return total; }

        private static int FreeSpace(BuildingState building)
        {
            return Math.Max(0, StorageCapacity(building) - Total(building.Inventory) -
                Total(building.ReservedIncoming) - building.ProductionReservedSpace - building.ReservedReturnSpace);
        }

        public static int StorageCapacity(BuildingState building)
        { return Math.Max(Definitions.Get(building.Kind).Capacity, building.CapacityOverride) + building.LegacyCapacityAllowance; }

        private static bool PathStillValid(PlanetState p, List<GridCell> path)
        {
            for (int i = 1; i < path.Count - 1; i++)
                if (!p.Roads[path[i].Y * p.Width + path[i].X]) return false;
            return true;
        }

        private static List<GridCell> FindPath(PlanetState p, BuildingState source, BuildingState target)
        {
            if (source == null || target == null) return null;
            int start = source.Y * p.Width + source.X;
            int finish = target.Y * p.Width + target.X;
            int[] previous = new int[p.Terrain.Length];
            for (int i = 0; i < previous.Length; i++) previous[i] = -1;
            var queue = new Queue<int>(); queue.Enqueue(start); previous[start] = start;
            int[] dx = { 1, 0, -1, 0 }; int[] dy = { 0, 1, 0, -1 };
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (current == finish) break;
                for (int direction = 0; direction < 4; direction++)
                {
                    int x = current % p.Width + dx[direction], y = current / p.Width + dy[direction];
                    if (x < 0 || y < 0 || x >= p.Width || y >= p.Height) continue;
                    int next = y * p.Width + x;
                    if (previous[next] >= 0 || (next != finish && !p.Roads[next])) continue;
                    previous[next] = current; queue.Enqueue(next);
                }
            }
            if (previous[finish] < 0) return null;
            var path = new List<GridCell>();
            for (int current = finish; ; current = previous[current])
            {
                path.Add(new GridCell(current % p.Width, current / p.Width));
                if (current == start) break;
            }
            path.Reverse(); return path;
        }
    }
}
