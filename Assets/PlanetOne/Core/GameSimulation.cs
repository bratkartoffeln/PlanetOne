using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    /// <summary>Authoritative fixed-step simulation. It never receives a camera or active view.</summary>
    public sealed partial class GameSimulation
    {
        public GameState State { get; private set; }
        public SimulationMetrics Metrics {get;set;}

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
            state.Planets.Add(CreatePlanet(state, 1, "Aster", -1, 1, 10, 0.2));
            state.Planets.Add(CreatePlanet(state, 2, "Nereid", -1, 1, 18, 2.5));
            state.Planets.Add(CreatePlanet(state, 3, "Khepri", 2, 0, 3, 0));
            state.Planets[2].Terrain[9 * state.Planets[2].Width + 13] = 3;
            state.Planets.Add(CreatePlanet(state, 5, "Sahra", -1, 0, 7.2, 3.8));
            state.Planets.Add(CreatePlanet(state, 6, "Pelagia", -1, 0, 12.5, 5.2));
            state.Planets.Add(CreatePlanet(state, 7, "Zephyr", -1, 0, 52, 1.6));
            AsteroidGeneration.Populate(state);
            var simulation = new GameSimulation(state);
            simulation.UpdateSpaceKnowledge();
            return simulation;
        }

        private static PlanetState CreatePlanet(GameState state, int id, string name, int parent,
            int owner, double radius, double phase)
        {
            var p = new PlanetState { Id = id, Name = name, ParentId = parent, OwnerId = owner, Width = 40, Height = 32,
                OrbitRadius = radius, OrbitPeriodSeconds = OrbitalMotion.Period(radius, parent >= 0), OrbitPhase = phase,
                OrbitEccentricity = parent >= 0 ? 0.04 : id == 1 ? 0.06 : id == 2 ? 0.09 : 0.12,
                OrbitPeriapsis = parent >= 0 ? 0.3 : (id - 1) * 0.55,
                Kind = PlanetEnvironment.InitialKind(id,parent) };
            p.Terrain = new int[p.Width * p.Height];
            p.AtmosphericHydrogen=PlanetEnvironment.InitialHydrogen(state.Seed,p);
            p.OreRemaining = new int[p.Terrain.Length];
            p.Roads = new bool[p.Terrain.Length];
            for (int y = 0; y < p.Height; y++)
                for (int x = 0; x < p.Width; x++)
                    if ((x < 2 && y < 3) || (x > p.Width-3 && y > p.Height-4) || (x == 12 && y == 2))
                        p.Terrain[y * p.Width + x] = 1;
            foreach (var cell in new[] { new GridCell(4, 4), new GridCell(13, 9), new GridCell(3, 10) })
            {
                int index = cell.Y * p.Width + cell.X;
                p.Terrain[index] = 2;
                p.OreRemaining[index] = 1800 + (int)((uint)(state.Seed + id * 191 + index) % 401);
            }
            if(p.Kind==PlanetKind.Ocean)
                for(int y=0;y<p.Height;y++)for(int x=0;x<p.Width;x++)
                    if(!PlanetEnvironment.OceanLand(x,y)){p.Terrain[y*p.Width+x]=4;p.OreRemaining[y*p.Width+x]=0;}
            if(p.Kind==PlanetKind.Temperate)
            {
                // Smooth seeded woodland patches, confined to the existing outer forest zone.
                // The starting colony, roads and deposits retain their original clear terrain.
                double WoodlandValue(int gx,int gy)
                {
                    unchecked
                    {
                        uint hash=(uint)state.Seed^((uint)id*0x9e3779b9u)^((uint)gx*0x85ebca6bu)^((uint)gy*0xc2b2ae35u);
                        hash^=hash>>16;hash*=0x7feb352du;hash^=hash>>15;hash*=0x846ca68bu;hash^=hash>>16;
                        return (hash&0xffffffu)/16777215d;
                    }
                }
                double WoodlandNoise(double wx,double wy)
                {
                    int gx=(int)Math.Floor(wx),gy=(int)Math.Floor(wy);
                    double u=wx-gx,v=wy-gy;u=u*u*(3-2*u);v=v*v*(3-2*v);
                    double a=WoodlandValue(gx,gy)*(1-u)+WoodlandValue(gx+1,gy)*u;
                    double b=WoodlandValue(gx,gy+1)*(1-u)+WoodlandValue(gx+1,gy+1)*u;
                    return a*(1-v)+b*v;
                }
                for(int y=14;y<p.Height;y++)for(int x=18;x<p.Width;x++)
                    if(p.Terrain[y*p.Width+x]==0 && WoodlandNoise(x*.16,y*.16)*.7+WoodlandNoise(x*.37+11,y*.37-8)*.3>.51)
                        p.Terrain[y*p.Width+x]=1;
            }
            if(PlanetEnvironment.Airborne(p)) {Array.Clear(p.Terrain,0,p.Terrain.Length);Array.Clear(p.OreRemaining,0,p.OreRemaining.Length);}
            if (owner != 0)
            {
                if(!PlanetEnvironment.Airborne(p)) {
                for (int x = 2; x <= 15; x++) p.Roads[6 * p.Width + x] = true;
                for (int y = 2; y <= 11; y++) p.Roads[y * p.Width + 8] = true;
                p.Roads[6 * p.Width + 8] = false;
                p.Roads[5 * p.Width + 4] = true;
                }
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
                Building = command.Building, TargetId = command.TargetId, ActorId = command.ActorId, DestinationId = command.DestinationId, IntValue = command.IntValue, BoolValue = command.BoolValue, Amount = command.Amount, Minimum = command.Minimum, Reserve = command.Reserve, Terms = command.Terms==null?null:CopyAiValue(command.Terms) });
            if(command.IssuerId==State.PlayerFactionId) State.LastMessage = State.Paused ? "Befehl für die Fortsetzung vorgemerkt." : "Befehl vorgemerkt.";
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
            long measuredStart=Metrics==null?0:System.Diagnostics.Stopwatch.GetTimestamp(),mark=measuredStart;
            foreach (var command in new List<SimCommand>(State.PendingCommands))
            {
                if (State.ProcessedCommandIds.Contains(command.Id)) continue;
                State.ProcessedCommandIds.Add(command.Id);
                Apply(command);
            }
            State.PendingCommands.Clear();
            TickDiplomacy();
            TickFactionExit();
            if(Metrics!=null)mark=Metrics.Record(SimulationStage.Commands,mark);
            foreach (var planet in State.Planets)
            {
                TickEconomy(planet);
                if(Metrics!=null)mark=Metrics.Record(SimulationStage.Economy,mark);
                PlanDeliveries(planet);
                DispatchDeliveries(planet);
                TickShipments(planet);
                FinishDeconstruction(planet);
                if(Metrics!=null)mark=Metrics.Record(SimulationStage.Logistics,mark);
                TickMilitary(planet);
                if(Metrics!=null)mark=Metrics.Record(SimulationStage.Units,mark);
            }
            TickSpace();
            if(Metrics!=null)mark=Metrics.Record(SimulationStage.Space,mark);
            TickInvasion();
            TickJointOperations();
            if(Metrics!=null)mark=Metrics.Record(SimulationStage.Invasion,mark);
            TickAi();
            if(Metrics!=null)mark=Metrics.Record(SimulationStage.Ai,mark);
            TickFactionExit();
            TickGateProjects();
            State.Tick++;
            if(Metrics!=null){Metrics.Record(SimulationStage.Victory,mark);Metrics.End(measuredStart);}
        }

        public PlanetState FindPlanet(int planetId) { return State.Planets.Find(p => p.Id == planetId); }

        public SimPoint GetOrbitPosition(int planetId)
        {
            PlanetState planet = FindPlanet(planetId);
            if (planet == null) throw new ArgumentException("Unbekannter Himmelskörper.");
            var point = OrbitalMotion.Position(planet, State.Tick * Definitions.TickSeconds);
            if (planet.ParentId >= 0)
            {
                var parent = GetOrbitPosition(planet.ParentId);
                point.X += parent.X; point.Y += parent.Y;
            }
            return point;
        }

        public SimPoint GetShipmentPosition(ShipmentState shipment)
        {
            // Physical world coordinates, including curved roads and a temporary give-way maneuver.
            return GroundCourierPosition(shipment);
        }

        public string ValidatePlacement(int planetId, BuildingKind kind, int x, int y, int factionId = 1)
        {
            PlanetState p = FindPlanet(planetId);
            if (!State.Factions.Exists(f => f.Id == factionId)) return "Unbekannte Fraktion.";
            string error = ValidateCell(p, x, y, factionId);
            if (error != null) return error;
            if (!Enum.IsDefined(typeof(BuildingKind), kind) || kind == BuildingKind.Headquarters)
                return "Dieser Gebäudetyp kann hier nicht gebaut werden.";
            if(!PlanetEnvironment.AllowsBuilding(p.Kind,kind))
                return kind==BuildingKind.GasExtractor ? "Gasförderer benötigen einen Gasriesen." : "Gasstationen besitzen keinen festen Boden für Minen, Mauern oder Straßentore. Metall und Erz per Raumfracht importieren.";
            string technologyError = BuildingTechnologyError(kind, factionId);
            if (technologyError != null) return technologyError;
            int index = y * p.Width + x;
            if (kind == BuildingKind.CrystalMine && p.Terrain[index] != 3) return "Kristallminen benötigen ein Kristallvorkommen.";
            if (kind != BuildingKind.CrystalMine && p.Terrain[index] == 3) return "Dieses Feld ist für Kristallabbau reserviert.";
            if (kind == BuildingKind.Mine && p.Terrain[index] != 2) return "Erzminen benötigen ein Erzvorkommen.";
            if (kind != BuildingKind.Mine && p.Terrain[index] == 2) return "Dieses Feld ist für den Erzabbau reserviert.";
            var headquarters = p.Buildings.Find(b => b.Kind == BuildingKind.Headquarters && b.Built);
            var proposedSite = new BuildingState { X = x, Y = y };
            if (headquarters == null || FindPath(p, headquarters, proposedSite) == null)
                return "Kein befahrbarer Weg vom Kommandozentrum zum Bauplatz. Gelände und Gebäude versperren die Zufahrt.";
            var definition = Definitions.Get(kind);
            var shortages = new List<string>();
            for (int r = 0; r < Definitions.ResourceCount; r++)
            {
                if(definition.Cost[r]==0)continue;
                int available = FreeBuildingStock(p,factionId,r,proposedSite);
                if (available >= definition.Cost[r]) continue;
                string missing = (definition.Cost[r]-available) + " " + Definitions.ResourceName((ResourceKind)r);
                if (FreeBuildingStock(p,factionId,r) >= definition.Cost[r]) missing += " (Lager nicht erreichbar)";
                shortages.Add(missing);
            }
            return shortages.Count == 0 ? null : "Am Bauplatz fehlen: " + string.Join(", ",shortages);
        }

        private static string ValidateCell(PlanetState p, int x, int y, int factionId)
        {
            if (p == null) return "Unbekannter Planet.";
            if (p.OwnerId == 0 || p.OwnerId != factionId) return "Keine eigene Kolonie an diesem Ort.";
            if (x < 0 || y < 0 || x >= p.Width || y >= p.Height) return "Außerhalb der Baufläche.";
            if (p.Terrain[y * p.Width + x] == 1) return "Fels ist nicht bebaubar.";
            if (p.Terrain[y * p.Width + x] == 4) return "Offenes Wasser ist nicht bebaubar. Einen Inselbauplatz wählen.";
            if (p.Buildings.Exists(b => b.X == x && b.Y == y)) return "Die Baufläche ist bereits belegt.";
            return null;
        }

        private bool Reject(string message) { State.LastMessage = message; return false; }

        private void Apply(SimCommand command)
        {
            PlanetState p = FindPlanet(command.PlanetId);
            if (!State.Factions.Exists(f => f.Id == command.IssuerId))
            { Report(command, false, "UnknownFaction", "Unbekannte Fraktion."); return; }
            if (!FactionActive(command.IssuerId)) { Report(command,false,"FactionInactive","Ausgeschiedene Fraktion kann keine Aufträge erteilen."); return; }
            if(command.Kind==CommandKind.ConnectSupply){ApplySupplyConnection(command);return;}
            if(command.Kind==CommandKind.CancelRecruitUnit)
            {
                if(p==null)Report(command,false,"UnknownPlanet","Unbekannter Planet.");
                else ApplyMilitary(p,command);
                return;
            }
            if (command.Kind == CommandKind.MineAsteroid) { ApplyMining(command); return; }
            if (command.Kind >= CommandKind.ProposeTreaty) { ApplyDiplomacy(command); return; }
            if (p == null) { Report(command, false, "UnknownPlanet", "Unbekannter Planet."); return; }
            if (command.Kind >= CommandKind.AttackShip) { ApplyInvasion(command); return; }
            if (command.Kind >= CommandKind.BuildShip) { ApplySpace(command); return; }
            if (command.Kind >= CommandKind.RecruitUnit) { ApplyMilitary(p, command); return; }
            if (p.OwnerId != command.IssuerId || p.OwnerId == 0)
            { Report(command, false, "PermissionDenied", "Keine eigene Kolonie an diesem Ort."); return; }
            string error;
            if(PlanetEnvironment.Airborne(p) && (command.Kind==CommandKind.PlaceRoad || command.Kind==CommandKind.RemoveRoad))
            { Report(command,false,"AirFreightOnly","Gasstationen nutzen Frachtdrohnen zwischen Plattformen; Straßen sind hier nicht möglich."); return; }
            if (command.Kind == CommandKind.RemoveRoad)
            {
                if (command.X < 0 || command.Y < 0 || command.X >= p.Width || command.Y >= p.Height ||
                    !p.Roads[command.Y * p.Width + command.X])
                { Report(command, false, "InvalidTarget", "Hier verläuft keine Versorgungsverbindung."); return; }
                p.Roads[command.Y * p.Width + command.X] = false;
                Report(command, true, "RoadRemoved", "Verbindungsabschnitt gesperrt. Lieferungen suchen einen Umweg oder warten."); return;
            }
            if (command.Kind == CommandKind.PlaceRoad)
            {
                error = ValidateCell(p, command.X, command.Y, command.IssuerId);
                if (error != null) { Report(command, false, "InvalidPlacement", error); return; }
                int index = command.Y * p.Width + command.X;
                if (p.Roads[index]) { Report(command, false, "Occupied", "Hier verläuft bereits eine Versorgungsverbindung."); return; }
                if (p.Terrain[index] >= 2) { Report(command, false, "InvalidTerrain", "Erzvorkommen für den Bergbau freihalten."); return; }
                p.Roads[index] = true;
                Report(command, true, "RoadPlaced", "Versorgungsabschnitt angelegt. Verbindungen sind vorläufig kostenfrei.");
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
            if(building.Kind==BuildingKind.Spaceport && PlanetEnvironment.Airborne(p))
                building.Supply.Target[(int)ResourceKind.Hydrogen]=8;
            var definition = Definitions.Get(command.Building);
            for (int r = 0; r < Definitions.ResourceCount; r++)
            {
                int remaining = definition.Cost[r];
                foreach (var source in p.Buildings)
                {
                    if (BuildingOwner(p,source)!=command.IssuerId || !source.Built || source.Deconstructing || !IsStorage(source) || remaining == 0 || FindPath(p,source,building)==null) continue;
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
            if(command.IssuerId==State.PlayerFactionId) State.LastMessage = message;
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

        internal static bool IsStorage(BuildingState b)
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
            if(PlanetEnvironment.Airborne(p))return true;
            for (int i = 1; i < path.Count - 1; i++)
                if (!CargoCellOpen(p,path[i].X,path[i].Y)) return false;
            return true;
        }

        private static List<GridCell> FindPath(PlanetState p, BuildingState source, BuildingState target)
        { return FreeCargoPath(p,source,target); }
    }
}
