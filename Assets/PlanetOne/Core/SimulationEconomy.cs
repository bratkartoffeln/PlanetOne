using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        private void TickEconomy(PlanetState p)
        {
            var headquarters = p.Buildings.Find(b => b.Kind == BuildingKind.Headquarters && b.Built && !b.Deconstructing);
            foreach (var b in p.Buildings)
                if (b.LegacyCapacityAllowance > 0)
                    b.LegacyCapacityAllowance = Math.Max(0, Total(b.Inventory) + Total(b.ReservedIncoming) +
                        b.ProductionReservedSpace + b.ReservedReturnSpace - Math.Max(Definitions.Get(b.Kind).Capacity, b.CapacityOverride));
            UpdatePopulation(p, headquarters);
            p.EnergyProduced = 0; p.EnergyUsed = 0; p.WorkersUsed = 0;
            p.EnergyStored = 0; p.EnergyCapacity = 0; p.HousingCapacity = 0;
            var connected = new HashSet<long>();
            foreach (var b in p.Buildings)
            {
                if (BuildingOwner(p,b)!=p.OwnerId || headquarters == null || FindPath(p, headquarters, b) == null) continue;
                connected.Add(b.Id);
                if (!b.Built || !b.Enabled || b.Deconstructing) continue;
                var definition = Definitions.Get(b.Kind);
                p.EnergyProduced += definition.EnergyProduced;
                p.EnergyStored += b.EnergyStored;
                p.EnergyCapacity += definition.EnergyStorageCapacity;
            }
            double availablePower = p.EnergyProduced + p.EnergyStored / Definitions.TickSeconds;
            var ordered = new List<BuildingState>(p.Buildings);
            int constructionSlotsUsed = 0;
            ordered.Sort((a, b) => a.Priority != b.Priority ? b.Priority.CompareTo(a.Priority) : a.Id.CompareTo(b.Id));
            foreach (var b in ordered)
            {
                b.Operational = false;
                if (b.Deconstructing)
                { SetStatus(p, b, "RecoveringMaterials", connected.Contains(b.Id) ? "Rückbau: Material wird abgeholt" : "Rückbau: Straßenanschluss fehlt"); continue; }
                if (!connected.Contains(b.Id))
                {
                    var isolated = Definitions.Get(b.Kind);
                    if (b.Built && b.Enabled && Definitions.IsTower(b.Kind) && b.EnergyStored >= isolated.EnergyUse * Definitions.TickSeconds)
                    { b.EnergyStored = Math.Max(0, b.EnergyStored - isolated.EnergyUse * Definitions.TickSeconds); b.Operational = true;
                        SetStatus(p, b, "LocalReserve", "Versorgungsweg unterbrochen; örtlicher Energiepuffer"); }
                    else SetStatus(p, b, "NoRoad", "Kein Straßenanschluss");
                    continue;
                }
                var definition = Definitions.Get(b.Kind);
                if (!b.Built)
                {
                    if (constructionSlotsUsed >= Definitions.ConstructionSlotsPerColony)
                        SetStatus(p, b, "NoConstructionSlot", "Wartet auf freie Baukapazität");
                    else if (TickConstruction(p, b, definition)) constructionSlotsUsed++;
                    continue;
                }
                if (!b.Enabled) { SetStatus(p, b, "Disabled", "Betrieb angehalten"); continue; }
                double storedPowerNeeded = Math.Max(0, p.EnergyUsed + definition.EnergyUse - p.EnergyProduced);
                if (p.EnergyUsed + definition.EnergyUse > availablePower + 0.0000001 ||
                    (b.PowerBlocked && storedPowerNeeded > 0 && p.EnergyStored < storedPowerNeeded * Definitions.EnergyRestartReserveSeconds))
                { b.PowerBlocked = true; SetStatus(p, b, "NoPower", "Zu wenig Energie"); continue; }
                if (p.WorkersUsed + definition.WorkerUse > p.WorkersTotal)
                { SetStatus(p, b, "NoWorkers", "Zu wenig Arbeitskräfte"); continue; }
                p.EnergyUsed += definition.EnergyUse;
                b.PowerBlocked = false;
                p.WorkersUsed += definition.WorkerUse;
                b.Operational = true;
                p.HousingCapacity += definition.HousingCapacity;
                if (definition.CycleSeconds <= 0) { SetStatus(p, b, "Ready", "Bereit"); continue; }
                TickProduction(p, b, definition);
            }
            double energyChange = (p.EnergyProduced - p.EnergyUsed) * Definitions.TickSeconds;
            foreach (var b in p.Buildings)
            {
                if (!b.Built || !b.Enabled || b.Deconstructing || !connected.Contains(b.Id)) continue;
                double capacity = Definitions.Get(b.Kind).EnergyStorageCapacity;
                if (energyChange > 0)
                { double amount = Math.Min(energyChange, capacity - b.EnergyStored); b.EnergyStored += amount; energyChange -= amount; }
                else if (energyChange < 0)
                { double amount = Math.Min(-energyChange, b.EnergyStored); b.EnergyStored -= amount; energyChange += amount; }
                b.EnergyStored = Math.Max(0, Math.Min(capacity, b.EnergyStored));
            }
            p.EnergyStored = 0;
            foreach (var b in p.Buildings)
                if (b.Built && b.Enabled && !b.Deconstructing && connected.Contains(b.Id)) p.EnergyStored += b.EnergyStored;
        }

        private void UpdatePopulation(PlanetState p, BuildingState headquarters)
        {
            if (p.Population == 0 || headquarters == null)
            { p.FoodDemandPerMinute = 0; p.WorkersTotal = 0; return; }
            p.FoodDemandPerMinute = p.Population * Definitions.FoodPerPersonPerSecond * 60;
            p.FoodDebt += p.Population * Definitions.FoodPerPersonPerSecond * Definitions.TickSeconds;
            bool wasStarving = p.FoodShortageSeconds > 0;
            int food = (int)ResourceKind.Food;
            while (p.FoodDebt + 0.000000001 >= 1 && headquarters.Inventory[food] - headquarters.ReservedOutgoing[food] > 0)
            {
                headquarters.Inventory[food]--; p.FoodConsumedTotal++; p.ConsumedTotals[food]++;
                p.FoodDebt = Math.Max(0, p.FoodDebt - 1);
            }
            if (p.FoodDebt + 0.000000001 >= 1)
            {
                p.FoodDebt = 1; // Hunger never accumulates an impossible retrospective food debt.
                p.FoodShortageSeconds += Definitions.TickSeconds;
                p.PopulationGrowthProgress = 0;
                if (!wasStarving) Emit(p.OwnerId, p.Id, headquarters.Id, "FoodShortage", "Nahrungsmangel auf " + p.Name + ". Wachstum stoppt; Arbeitsleistung sinkt nach der Reservezeit.");
            }
            else
            {
                p.FoodShortageSeconds = 0;
                if (wasStarving) Emit(p.OwnerId, p.Id, headquarters.Id, "FoodRestored", "Nahrungsversorgung auf " + p.Name + " wiederhergestellt.");
                if (p.Population < p.HousingCapacity)
                {
                    p.PopulationGrowthProgress += Definitions.TickSeconds;
                    if (p.PopulationGrowthProgress + 0.000000001 >= Definitions.PopulationGrowthSeconds)
                    {
                        p.Population++; p.PopulationGrowthProgress = 0;
                        Emit(p.OwnerId, p.Id, headquarters.Id, "PopulationGrown", "Bevölkerung auf " + p.Name + " wächst auf " + p.Population + ".");
                    }
                }
                else p.PopulationGrowthProgress = 0;
            }
            double shortage = Math.Max(0, p.FoodShortageSeconds - Definitions.FoodShortageGraceSeconds);
            p.Productivity = Math.Max(Definitions.MinimumProductivity,
                1 - shortage / Definitions.FoodShortageRampSeconds * (1 - Definitions.MinimumProductivity));
            // A small emergency workforce can always restore basic food and mining operations.
            p.WorkersTotal = Math.Max(Math.Min(4, p.Population),
                (int)Math.Floor(p.Population * Definitions.WorkforceFraction * p.Productivity));
        }

        private bool TickConstruction(PlanetState p, BuildingState b, BuildingDefinition definition)
        {
            if (!b.ConstructionStarted)
            {
                for (int r = 0; r < 6; r++)
                    if (b.Inventory[r] < definition.Cost[r])
                    { SetStatus(p, b, "AwaitingConstructionMaterial", "Wartet auf Baumaterial"); return false; }
                for (int r = 0; r < 6; r++)
                { b.Inventory[r] -= definition.Cost[r]; p.ConsumedTotals[r] += definition.Cost[r]; }
                b.ConstructionStarted = true;
            }
            b.BuildProgress = Math.Min(definition.BuildSeconds, b.BuildProgress + Definitions.TickSeconds * p.Productivity);
            if (b.BuildProgress + 0.000000001 >= definition.BuildSeconds)
            {
                b.Built = true;
                SetStatus(p, b, "BuildingCompleted", "Bereit");
                if(BuildingOwner(p,b)==State.PlayerFactionId) State.LastMessage = definition.Name + " auf " + p.Name + " fertiggestellt.";
            }
            else SetStatus(p, b, "UnderConstruction", "Wird errichtet");
            return true;
        }

        private void TickProduction(PlanetState p, BuildingState b, BuildingDefinition definition)
        {
            if (!b.CycleActive)
            {
                int missingInput = 0, storedOutput = 0;
                for (int r = 0; r < 6; r++)
                {
                    missingInput += Math.Max(0, definition.Inputs[r] - (b.Inventory[r] - b.ReservedOutgoing[r]));
                    if (definition.Outputs[r] > 0) storedOutput += b.Inventory[r];
                }
                if (missingInput > FreeSpace(b) && storedOutput > 0)
                { SetStatus(p, b, "OutputsBlockingStorage", "Lager voll: Ausgänge abtransportieren"); return; }
                for (int r = 0; r < 6; r++)
                    if (b.Inventory[r] - b.ReservedOutgoing[r] < definition.Inputs[r])
                    { SetStatus(p, b, "AwaitingInput", "Wartet auf Rohstoffe"); return; }
                if (Total(b.Inventory) - Total(definition.Inputs) + Total(b.ReservedIncoming) + b.ReservedReturnSpace + Total(definition.Outputs) > StorageCapacity(b))
                { SetStatus(p, b, "StorageFull", "Lager voll"); return; }
                if (b.Kind == BuildingKind.Mine || b.Kind == BuildingKind.CrystalMine)
                {
                    int index = b.Y * p.Width + b.X;
                    if (p.OreRemaining[index] < definition.Outputs[b.Kind == BuildingKind.CrystalMine ? 4 : 1])
                    { SetStatus(p, b, "DepositExhausted", "Vorkommen erschöpft"); return; }
                    p.OreRemaining[index] -= definition.Outputs[b.Kind == BuildingKind.CrystalMine ? 4 : 1];
                }
                for (int r = 0; r < 6; r++)
                { b.Inventory[r] -= definition.Inputs[r]; p.ConsumedTotals[r] += definition.Inputs[r]; }
                b.CycleActive = true; b.CycleProgress = 0;
                b.ProductionReservedSpace = Total(definition.Outputs);
            }
            SetStatus(p, b, "Producing", "Produziert");
            b.CycleProgress += Definitions.TickSeconds * p.Productivity;
            if (b.CycleProgress + 0.000000001 >= definition.CycleSeconds)
            {
                for (int r = 0; r < 6; r++)
                { b.Inventory[r] += definition.Outputs[r]; p.ProducedTotals[r] += definition.Outputs[r]; }
                p.FoodProducedTotal += definition.Outputs[(int)ResourceKind.Food];
                b.CycleActive = false; b.CycleProgress = 0; b.ProductionReservedSpace = 0;
            }
        }

        private void SetStatus(PlanetState p, BuildingState building, string code, string message)
        {
            if (building.Status == message) return;
            building.Status = message;
            Emit(p.OwnerId, p.Id, building.Id, code, Definitions.Get(building.Kind).Name + ": " + message);
        }

        public static int[] GetRecoveryPreview(BuildingState building)
        {
            var result = (int[])building.Inventory.Clone();
            if (building.Deconstructing) return result;
            var definition = Definitions.Get(building.Kind);
            if (building.Built || building.ConstructionStarted)
            {
                double fraction = building.Built ? Definitions.DemolitionRefundFraction : Definitions.ConstructionRefundFraction;
                for (int r = 0; r < 6; r++) result[r] += (int)Math.Floor(definition.Cost[r] * fraction);
            }
            if (building.CycleActive)
                for (int r = 0; r < 6; r++) result[r] += definition.Inputs[r];
            if (building.ShipBuildKind >= 0 && building.ShipBuildPaid && !building.Destroyed)
            { var cost = Definitions.Ships[building.ShipBuildKind].Cost; for (int r = 0; r < 6; r++) result[r] += cost[r]; }
            if (building.RecruitKind >= 0 && building.RecruitPaid)
            { var cost = Definitions.Get((UnitKind)building.RecruitKind).Cost; for (int r = 0; r < 6; r++) result[r] += cost[r]; }
            return result;
        }

        private void BeginDeconstruction(PlanetState p, BuildingState building)
        {
            CancelPortWork(p, building);
            var recovery = GetRecoveryPreview(building);
            if (building.RecruitKind >= 0 && building.RecruitPaid) p.Population += Definitions.Get((UnitKind)building.RecruitKind).Crew;
            building.RecruitKind = -1; building.RecruitPaid = false; building.RecruitProgress = 0;
            var definition = Definitions.Get(building.Kind);
            building.Deconstructing = true; building.Enabled = false;
            for (int r = 0; r < 6; r++) p.RecoveredTotals[r] += recovery[r] - building.Inventory[r];
            building.Inventory = recovery;
            if (building.CycleActive && (building.Kind == BuildingKind.Mine || building.Kind == BuildingKind.CrystalMine))
                p.OreRemaining[building.Y * p.Width + building.X] += definition.Outputs[building.Kind == BuildingKind.CrystalMine ? 4 : 1];
            building.CycleActive = false; building.CycleProgress = 0; building.ProductionReservedSpace = 0;
            building.CapacityOverride = Math.Max(definition.Capacity, Total(recovery) + Total(building.ReservedIncoming) +
                building.ReservedReturnSpace + Total(definition.Cost));
            // Cancel uncollected inbound orders. Loaded goods remain on their actual couriers.
            for (int i = p.Deliveries.Count - 1; i >= 0; i--)
            {
                var order = p.Deliveries[i];
                if (order.TargetId != building.Id) continue;
                var source = p.Buildings.Find(b => b.Id == order.SourceId);
                source.ReservedOutgoing[(int)order.Resource] -= order.Amount;
                building.ReservedIncoming[(int)order.Resource] -= order.Amount;
                p.Deliveries.RemoveAt(i);
            }
            for (int i = p.Shipments.Count - 1; i >= 0; i--)
            {
                var shipment = p.Shipments[i];
                if (shipment.TargetId != building.Id || shipment.Phase == ShipmentPhase.Returning || shipment.Phase == ShipmentPhase.Recovering) continue;
                building.ReservedIncoming[(int)shipment.Resource] -= shipment.Amount;
                if (shipment.Phase == ShipmentPhase.Loading)
                {
                    p.Buildings.Find(b => b.Id == shipment.SourceId).ReservedOutgoing[(int)shipment.Resource] -= shipment.Amount;
                    p.Shipments.RemoveAt(i);
                }
                else
                {
                    var source = p.Buildings.Find(b => b.Id == shipment.SourceId);
                    source.ReservedReturnSpace -= shipment.Amount;
                    source.ReservedIncoming[(int)shipment.Resource] += shipment.Amount;
                    shipment.RecoveryReserved = true;
                    shipment.Phase = ShipmentPhase.Recovering;
                    shipment.Path.Reverse();
                    shipment.DistanceTravelled = shipment.Path.Count - 1 - shipment.DistanceTravelled;
                    shipment.PhaseProgress = 0;
                    shipment.RouteBlocked = true; // Recheck the way to its original source next tick.
                }
            }
        }

        private void FinishDeconstruction(PlanetState p)
        {
            for (int i = p.Buildings.Count - 1; i >= 0; i--)
            {
                var b = p.Buildings[i];
                if ((b.Destroyed && b.Kind==BuildingKind.Headquarters) || !b.Deconstructing || Total(b.Inventory) != 0 || Total(b.ReservedOutgoing) != 0 || Total(b.ReservedIncoming) != 0 || b.ReservedReturnSpace != 0 ||
                    p.Deliveries.Exists(d => d.SourceId == b.Id || d.TargetId == b.Id) ||
                    p.Shipments.Exists(s => s.SourceId == b.Id || s.TargetId == b.Id)) continue;
                p.Buildings.RemoveAt(i);
                p.GatePermissions.RemoveAll(g => g.GateId == b.Id);
                Emit(p.OwnerId, p.Id, b.Id, "BuildingRemoved", "Rückbau abgeschlossen; Baufläche ist wieder frei.");
            }
        }
    }
}
