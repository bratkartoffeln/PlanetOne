using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        private void PlanDeliveries(PlanetState p)
        {
            var headquarters = p.Buildings.Find(b => b.Kind == BuildingKind.Headquarters && b.Built && !b.Deconstructing);
            if (headquarters != null)
                PlanInput(p, headquarters, (int)ResourceKind.Food, Math.Max(24, p.Population * 2));
            foreach (var target in p.Buildings)
            {
                if (!target.Built || !target.Enabled || target.Deconstructing) continue;
                var definition = Definitions.Get(target.Kind);
                for (int r = 0; r < 6; r++)
                    if (definition.Inputs[r] > 0) PlanInput(p, target, r, definition.Inputs[r] * 3);
            }
            foreach (var source in p.Buildings)
            {
                if (!source.Deconstructing && (!source.Built || IsStorage(source))) continue;
                var definition = Definitions.Get(source.Kind);
                for (int r = 0; r < 6; r++)
                {
                    if (!source.Deconstructing && definition.Outputs[r] == 0) continue;
                    int amount = Math.Min(Definitions.CourierCapacity, source.Inventory[r] - source.ReservedOutgoing[r]);
                    if (amount < (source.Deconstructing ? 1 : 4)) continue;
                    foreach (var target in p.Buildings)
                    {
                        if (!target.Built || target.Deconstructing || !IsStorage(target) || target.Id == source.Id ||
                            FreeSpace(target) <= 0 || FindPath(p, source, target) == null) continue;
                        int targetNeed = StorageTarget(p, r) - target.Inventory[r] - target.ReservedIncoming[r];
                        if (source.Deconstructing) targetNeed = FreeSpace(target);
                        int shipped = Math.Min(amount, Math.Min(FreeSpace(target), targetNeed));
                        if (shipped <= 0) continue;
                        ReserveDelivery(p, source, target, r, shipped);
                        break;
                    }
                }
            }
        }

        private static int StorageTarget(PlanetState planet, int resource)
        {
            switch ((ResourceKind)resource)
            {
                case ResourceKind.Food: return Math.Max(24, planet.Population * 2);
                case ResourceKind.Ore: return 32;
                case ResourceKind.Metal: return 80;
                case ResourceKind.Parts: return 40;
                default: return 16;
            }
        }

        private void PlanInput(PlanetState p, BuildingState target, int resource, int desired)
        {
            int needed = Math.Min(desired - target.Inventory[resource] - target.ReservedIncoming[resource], FreeSpace(target));
            if (needed <= 0) return;
            foreach (var source in p.Buildings)
            {
                if ((!source.Built && !source.Deconstructing) || source.Id == target.Id ||
                    (!source.Deconstructing && !IsStorage(source) && Definitions.Get(source.Kind).Outputs[resource] == 0)) continue;
                // Local factories may use every uncommitted item; export reserves do not apply here.
                int available = source.Inventory[resource] - source.ReservedOutgoing[resource];
                int amount = Math.Min(needed, Math.Min(available, Definitions.CourierCapacity));
                if (amount <= 0 || FindPath(p, source, target) == null) continue;
                ReserveDelivery(p, source, target, resource, amount);
                needed -= amount;
                if (needed <= 0) break;
            }
        }

        private void ReserveDelivery(PlanetState planet, BuildingState source, BuildingState target, int resource, int amount)
        {
            source.ReservedOutgoing[resource] += amount;
            target.ReservedIncoming[resource] += amount;
            planet.Deliveries.Add(new DeliveryOrder { Id = State.NextEntityId++, SourceId = source.Id,
                TargetId = target.Id, Resource = (ResourceKind)resource, Amount = amount });
        }

        private void DispatchDeliveries(PlanetState p)
        {
            p.Deliveries.Sort((a, b) =>
            {
                int comparison = DeliveryPriority(p, b).CompareTo(DeliveryPriority(p, a));
                return comparison != 0 ? comparison : a.Id.CompareTo(b.Id);
            });
            for (int i = 0; i < p.Deliveries.Count && p.Shipments.Count < Definitions.CouriersPerColony; i++)
            {
                var order = p.Deliveries[i];
                var source = p.Buildings.Find(b => b.Id == order.SourceId);
                var target = p.Buildings.Find(b => b.Id == order.TargetId);
                var path = FindPath(p, source, target);
                if (path == null) continue;
                int amount = Math.Min(Definitions.CourierCapacity, order.Amount);
                p.Shipments.Add(new ShipmentState { Id = State.NextEntityId++, SourceId = source.Id,
                    TargetId = target.Id, Resource = order.Resource, Amount = amount, Path = path,
                    Phase = ShipmentPhase.Loading });
                order.Amount -= amount;
                if (order.Amount == 0) { p.Deliveries.RemoveAt(i); i--; }
            }
        }

        private static int DeliveryPriority(PlanetState p, DeliveryOrder order)
        {
            var target = p.Buildings.Find(b => b.Id == order.TargetId);
            if (target.Kind == BuildingKind.Headquarters && order.Resource == ResourceKind.Food) return 100;
            return target.Priority * 10 + (target.Built ? 0 : 2);
        }

        private void TickShipments(PlanetState p)
        {
            for (int i = p.Shipments.Count - 1; i >= 0; i--)
            {
                var shipment = p.Shipments[i];
                var source = p.Buildings.Find(b => b.Id == shipment.SourceId);
                var target = p.Buildings.Find(b => b.Id == shipment.TargetId);
                int resource = (int)shipment.Resource;
                if (shipment.Phase == ShipmentPhase.Recovering && !shipment.RecoveryReserved && FreeSpace(source) >= shipment.Amount)
                { source.ReservedIncoming[resource] += shipment.Amount; shipment.RecoveryReserved = true; }
                var destination = shipment.Phase == ShipmentPhase.Returning || shipment.Phase == ShipmentPhase.Recovering ? source : target;
                if (!EnsureShipmentRoute(p, shipment, destination)) continue;
                switch (shipment.Phase)
                {
                    case ShipmentPhase.Loading:
                        shipment.PhaseProgress += Definitions.TickSeconds;
                        if (shipment.PhaseProgress + 0.000000001 < Definitions.TransferSeconds) break;
                        source.Inventory[resource] -= shipment.Amount;
                        source.ReservedOutgoing[resource] -= shipment.Amount;
                        source.ReservedReturnSpace += shipment.Amount;
                        shipment.Phase = ShipmentPhase.InTransit; shipment.PhaseProgress = 0;
                        break;
                    case ShipmentPhase.InTransit:
                        MoveShipment(shipment);
                        if (AtRouteEnd(shipment)) { shipment.Phase = ShipmentPhase.Unloading; shipment.PhaseProgress = 0; }
                        break;
                    case ShipmentPhase.Unloading:
                        shipment.PhaseProgress += Definitions.TickSeconds;
                        if (shipment.PhaseProgress + 0.000000001 < Definitions.TransferSeconds) break;
                        target.Inventory[resource] += shipment.Amount;
                        target.ReservedIncoming[resource] -= shipment.Amount;
                        source.ReservedReturnSpace -= shipment.Amount;
                        shipment.Amount = 0; shipment.Phase = ShipmentPhase.Returning;
                        shipment.Path.Reverse(); shipment.DistanceTravelled = 0; shipment.PhaseProgress = 0;
                        break;
                    case ShipmentPhase.Returning:
                        MoveShipment(shipment);
                        if (AtRouteEnd(shipment)) p.Shipments.RemoveAt(i);
                        break;
                    case ShipmentPhase.Recovering:
                        MoveShipment(shipment);
                        if (!AtRouteEnd(shipment) || !shipment.RecoveryReserved) break;
                        shipment.PhaseProgress += Definitions.TickSeconds;
                        if (shipment.PhaseProgress + 0.000000001 < Definitions.TransferSeconds) break;
                        source.Inventory[resource] += shipment.Amount;
                        source.ReservedIncoming[resource] -= shipment.Amount;
                        p.Shipments.RemoveAt(i);
                        break;
                }
            }
        }

        private static void MoveShipment(ShipmentState shipment)
        { shipment.DistanceTravelled = Math.Min(shipment.Path.Count - 1, shipment.DistanceTravelled + Definitions.CourierSpeed * Definitions.TickSeconds); }

        private static bool AtRouteEnd(ShipmentState shipment)
        { return shipment.DistanceTravelled + 0.000000001 >= shipment.Path.Count - 1; }

        private bool EnsureShipmentRoute(PlanetState planet, ShipmentState shipment, BuildingState destination)
        {
            bool routeValid = true;
            var last = shipment.Path[shipment.Path.Count - 1];
            if (last.X != destination.X || last.Y != destination.Y) routeValid = false;
            int currentIndex = Math.Min((int)shipment.DistanceTravelled, shipment.Path.Count - 1);
            for (int i = currentIndex + 1; i < shipment.Path.Count - 1; i++)
                if (!planet.Roads[shipment.Path[i].Y * planet.Width + shipment.Path[i].X]) routeValid = false;
            if (!routeValid)
            {
                // Preserve the exact point on the currently traversed segment when replacing its remainder.
                double fraction = shipment.DistanceTravelled - currentIndex;
                var a = shipment.Path[currentIndex];
                var b = shipment.Path[Math.Min(currentIndex + 1, shipment.Path.Count - 1)];
                List<GridCell> replacement;
                double distance;
                if (fraction < 0.000000001)
                {
                    replacement = FindPath(planet, new BuildingState { X = a.X, Y = a.Y }, destination);
                    distance = 0;
                }
                else
                {
                    replacement = FindPath(planet, new BuildingState { X = b.X, Y = b.Y }, destination);
                    distance = fraction;
                    if (replacement != null) replacement.Insert(0, a);
                    else
                    {
                        replacement = FindPath(planet, new BuildingState { X = a.X, Y = a.Y }, destination);
                        distance = 1 - fraction;
                        if (replacement != null) replacement.Insert(0, b);
                    }
                }
                if (replacement == null)
                {
                    if (!shipment.RouteBlocked)
                        Emit(planet.OwnerId, planet.Id, shipment.Id, "DeliveryBlocked", "Lieferweg unterbrochen. Ladung bleibt im Transporter.");
                    shipment.RouteBlocked = true;
                    return false;
                }
                shipment.Path = replacement; shipment.DistanceTravelled = distance;
            }
            if (shipment.RouteBlocked)
                Emit(planet.OwnerId, planet.Id, shipment.Id, "DeliveryResumed", "Lieferweg wieder verfügbar.");
            shipment.RouteBlocked = false;
            return true;
        }
    }
}
