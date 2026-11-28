using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        private void PlanDeliveries(PlanetState p)
        {
            if(p.OwnerId==0)return;
            var headquarters = p.Buildings.Find(b => b.Kind == BuildingKind.Headquarters && b.Built && !b.Deconstructing);
            if (headquarters != null)
                PlanInput(p, headquarters, (int)ResourceKind.Food, Math.Max(24, p.Population * 2));
            foreach (var target in p.Buildings)
            {
                if (BuildingOwner(p,target)!=p.OwnerId || !target.Built || !target.Enabled || target.Deconstructing) continue;
                var definition = Definitions.Get(target.Kind);
                PlanSpaceInputs(p, target);
                if(target.Kind==BuildingKind.JumpGate && !State.GateProjects.Exists(g=>g.BuildingId==target.Id&&g.Complete)) for(int r=0;r<Definitions.ResourceCount;r++) if(Definitions.GateComponents[r]>0)PlanInput(p,target,r,Definitions.GateComponents[r]);
                if (target.RecruitKind >= 0)
                {
                    var cost = Definitions.Get((UnitKind)target.RecruitKind).Cost;
                    if (!target.RecruitPaid) for (int r = 0; r < Definitions.ResourceCount; r++) if (cost[r] > 0) PlanInput(p, target, r, cost[r]);
                }
                if (target.Kind == BuildingKind.MilitaryDepot || target.Kind == BuildingKind.OrbitalBattery || Definitions.IsTower(target.Kind))
                {
                    PlanInput(p, target, (int)ResourceKind.Food, 8);
                    PlanInput(p, target, (int)ResourceKind.Parts, target.Kind == BuildingKind.MilitaryDepot ? 24 : 8);
                }
                for (int r = 0; r < Definitions.ResourceCount; r++)
                    if (definition.Inputs[r] > 0) PlanInput(p, target, r, definition.Inputs[r] * 3);
            }
            foreach (var source in p.Buildings)
            {
                if (source.Kind==BuildingKind.JumpGate && !source.Deconstructing) continue;
                if (BuildingOwner(p,source)!=p.OwnerId || !source.Deconstructing && (!source.Built || IsStorage(source))) continue;
                var definition = Definitions.Get(source.Kind);
                for (int r = 0; r < Definitions.ResourceCount; r++)
                {
                    if (!source.Deconstructing && definition.Outputs[r] == 0) continue;
                    int amount = Math.Min(Definitions.CourierCapacity, source.Inventory[r] - source.ReservedOutgoing[r]);
                    bool lastGasBatch=source.Kind==BuildingKind.GasExtractor && p.AtmosphericHydrogen==0 && !source.CycleActive;
                    if (amount < (source.Deconstructing || lastGasBatch ? 1 : 4)) continue;
                    foreach (var target in p.Buildings)
                    {
                        if (BuildingOwner(p,target)!=p.OwnerId || !target.Built || target.Deconstructing || !IsStorage(target) || target.Id == source.Id ||
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

        public static int FreeSupplyStock(PlanetState planet, int owner, BuildingState source, int resource)
        {
            if (source.Destroyed || BuildingOwner(planet,source) != owner || (!source.Built && !source.Deconstructing)) return 0;
            if (!source.Deconstructing && !IsStorage(source) && source.Kind != BuildingKind.Spaceport &&
                Definitions.Get(source.Kind).Outputs[resource] == 0) return 0;
            return Math.Max(0, source.Inventory[resource] - source.ReservedOutgoing[resource]);
        }

        public static int FreeBuildingStock(PlanetState planet, int owner, int resource, BuildingState site = null)
        {
            int available = 0;
            foreach (var source in planet.Buildings)
                if (IsStorage(source) && source.Built && !source.Deconstructing &&
                    (site == null || FindPath(planet,source,site) != null))
                    available += FreeSupplyStock(planet,owner,source,resource);
            return available;
        }

        private void PlanInput(PlanetState p, BuildingState target, int resource, int desired)
        {
            int needed = Math.Min(desired - target.Inventory[resource] - target.ReservedIncoming[resource], FreeSpace(target));
            if (needed <= 0) return;
            foreach (var source in p.Buildings)
            {
                if (source.Id == target.Id) continue;
                int available = FreeSupplyStock(p, BuildingOwner(p,target), source, resource);
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
            foreach(var building in p.Buildings)OpenGarage(building);
            p.Deliveries.Sort((a, b) =>
            {
                int comparison = DeliveryPriority(p, b).CompareTo(DeliveryPriority(p, a));
                return comparison != 0 ? comparison : a.Id.CompareTo(b.Id);
            });
            for (int i = 0; i < p.Deliveries.Count; i++)
            {
                var order = p.Deliveries[i];
                var source = p.Buildings.Find(b => b.Id == order.SourceId);
                var target = p.Buildings.Find(b => b.Id == order.TargetId);
                var path = FindPath(p, source, target);
                if (path == null) continue;
                int amount = Math.Min(Definitions.CourierCapacity, order.Amount);
                BuildingState home;
                long courier=ChooseCourier(p,source,target,out home);
                if(courier==0)continue;
                bool collect=home.Id!=source.Id;
                if(collect){path=FindPath(p,home,source);if(path==null)continue;}
                p.Shipments.Add(new ShipmentState { Id = State.NextEntityId++, CourierId=courier,HomeId=home.Id,ApproachingSource=collect,SourceId = source.Id,
                    TargetId = target.Id, Resource = order.Resource, Amount = amount, Path = path,
                    Phase = ShipmentPhase.Loading, Airborne=PlanetEnvironment.Airborne(p), Traffic=new GroundTrafficState{Docked=true} });
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
            // Older admitted jobs enter a newly cleared crossing before fresh short trips.
            // Reverse iteration still permits removal; descending storage order gives oldest first.
            p.Shipments.Sort((a,b)=>b.Id.CompareTo(a.Id));
            for (int i = p.Shipments.Count - 1; i >= 0; i--)
            {
                var shipment = p.Shipments[i];
                var source = p.Buildings.Find(b => b.Id == shipment.SourceId);
                var target = p.Buildings.Find(b => b.Id == shipment.TargetId);
                if(shipment.HomeId==0 && shipment.Phase==ShipmentPhase.Returning && source.Deconstructing)
                {
                    // An already empty vehicle in transit docks at its surviving receiver.
                    var depot=!target.Deconstructing && !target.Destroyed ? target :
                        p.Buildings.Find(b=>b.Built && !b.Deconstructing && !b.Destroyed && IsStorage(b) && BuildingOwner(p,b)==p.OwnerId);
                    if(depot!=null)
                    {
                        shipment.TargetId=source.Id;shipment.SourceId=depot.Id;target=source;source=depot;
                        ReverseCourierPath(shipment);
                    }
                }
                if(IsConstructionVehicle(shipment)&&(target.Built||target.Deconstructing||target.Destroyed||source.Deconstructing||
                    source.Destroyed||BuildingOwner(p,source)!=BuildingOwner(p,target)))ReturnConstructionVehicle(shipment);
                if(TickGroundYield(p,CourierAgent(p,shipment)))continue;
                if (IsConstructionVehicle(shipment))
                {
                    if(shipment.BuilderTurnPending && (target.Deconstructing || target.Destroyed))
                    {
                        shipment.BuilderTurnPending=false;
                        ReverseCourierPath(shipment);
                    }
                    var garage=shipment.HomeId==0?source:p.Buildings.Find(b=>b.Id==shipment.HomeId);
                    if(shipment.BuilderTurnPending&&FindPath(p,target,garage)==null)
                    {shipment.RouteBlocked=true;continue;}
                    var depot = shipment.Phase == ShipmentPhase.BuilderReturning&&!shipment.BuilderTurnPending ? garage : target;
                    if (!EnsureShipmentRoute(p, shipment, depot)) continue;
                    if (shipment.Phase == ShipmentPhase.Building) continue;
                    MoveShipment(p,shipment);
                    if (!AtRouteEnd(shipment)) continue;
                    if(shipment.BuilderTurnPending)
                    {shipment.Path.Reverse();shipment.DistanceTravelled=0;shipment.RoadEntryCentered=false;shipment.BuilderTurnPending=false;shipment.Traffic.Docked=true;shipment.RouteBlocked=true;}
                    else if (shipment.Phase == ShipmentPhase.BuilderReturning) p.Shipments.RemoveAt(i);
                    else {shipment.Phase = ShipmentPhase.Building;shipment.PassingOffset=0;}
                    continue;
                }
                int resource = (int)shipment.Resource;
                if (shipment.Phase == ShipmentPhase.Recovering && !shipment.RecoveryReserved && FreeSpace(source) >= shipment.Amount)
                { source.ReservedIncoming[resource] += shipment.Amount; shipment.RecoveryReserved = true; }
                var destination=p.Buildings.Find(b=>b.Id==CourierDestination(shipment));
                if(shipment.ApproachingSource)
                {
                    if(!EnsureShipmentRoute(p,shipment,source))continue;
                    MoveShipment(p,shipment);
                    if(!AtRouteEnd(shipment))continue;
                    var outbound=FindPath(p,source,target);if(outbound==null){shipment.RouteBlocked=true;continue;}
                    shipment.Path=outbound;shipment.DistanceTravelled=0;shipment.PassingOffset=0;
                    shipment.ApproachingSource=false;shipment.Traffic.Docked=true;shipment.PhaseProgress=0;
                    continue;
                }
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
                        MoveShipment(p,shipment);
                        if (AtRouteEnd(shipment)) { shipment.Phase = ShipmentPhase.Unloading; shipment.PhaseProgress = 0; shipment.Traffic.Docked=true; }
                        break;
                    case ShipmentPhase.Unloading:
                        shipment.PhaseProgress += Definitions.TickSeconds;
                        if (shipment.PhaseProgress + 0.000000001 < Definitions.TransferSeconds) break;
                        target.Inventory[resource] += shipment.Amount;
                        target.ReservedIncoming[resource] -= shipment.Amount;
                        source.ReservedReturnSpace -= shipment.Amount;
                        shipment.Amount = 0;
                        if(shipment.HomeId==target.Id || shipment.HomeId==0 && (source.Deconstructing || source.Destroyed)){p.Shipments.RemoveAt(i);break;}
                        shipment.Phase = ShipmentPhase.Returning;
                        shipment.Path.Reverse(); shipment.DistanceTravelled = 0; shipment.PhaseProgress = 0;
                        shipment.PassingOffset=0;shipment.RoadEntryCentered=false;
                        // A give-way maneuver may have joined the outbound route halfway along.
                        // The reversed suffix need not end at the original source depot.
                        shipment.RouteBlocked = true;
                        break;
                    case ShipmentPhase.Returning:
                        MoveShipment(p,shipment);
                        if (AtRouteEnd(shipment)) p.Shipments.RemoveAt(i);
                        break;
                    case ShipmentPhase.Recovering:
                        MoveShipment(p,shipment);
                        if(AtRouteEnd(shipment)){if(shipment.Traffic==null)shipment.Traffic=new GroundTrafficState();shipment.Traffic.Docked=true;}
                        if (!AtRouteEnd(shipment) || !shipment.RecoveryReserved) break;
                        shipment.PhaseProgress += Definitions.TickSeconds;
                        if (shipment.PhaseProgress + 0.000000001 < Definitions.TransferSeconds) break;
                        source.Inventory[resource] += shipment.Amount;
                        source.ReservedIncoming[resource] -= shipment.Amount;
                        if(shipment.HomeId!=0 && shipment.HomeId!=source.Id)RecallCourier(p,shipment);
                        else p.Shipments.RemoveAt(i);
                        break;
                }
            }
        }

        private void MoveShipment(PlanetState planet,ShipmentState shipment)
        {
            if(shipment.Traffic==null)shipment.Traffic=new GroundTrafficState();
            BuildingState exit=null;
            if(shipment.Traffic.Docked && shipment.DistanceTravelled==0 && shipment.Path.Count>1)
            {
                var first=shipment.Path[0];
                exit=planet.Buildings.Find(b=>b.X==first.X && b.Y==first.Y && !b.Destroyed);
                if(exit!=null && State.Tick<exit.NextCourierExitTick)return;
            }
            double next=Math.Min(ShipmentArrivalDistance(shipment),shipment.DistanceTravelled+Definitions.CourierSpeed*Definitions.TickSeconds);
            double desired=!shipment.Airborne && PassingTraffic(planet,shipment)?RoadLaneOffset:0;
            double lateralStep=Definitions.CourierSpeed*Definitions.TickSeconds*.5;
            double lane=shipment.PassingOffset+Math.Max(-lateralStep,Math.Min(lateralStep,desired-shipment.PassingOffset));
            var mover=CourierAgent(planet,shipment);var target=CourierPathPosition(shipment,next,0);GroundAgent blocker;
            double budget=Definitions.CourierSpeed*Definitions.TickSeconds;
            if(Distance(mover.Position.X,mover.Position.Y,target.X,target.Y)>budget)
            {
                double low=shipment.DistanceTravelled,high=next;
                for(int i=0;i<18;i++){double middle=(low+high)*.5;var point=CourierPathPosition(shipment,middle,0);if(Distance(mover.Position.X,mover.Position.Y,point.X,point.Y)>budget)high=middle;else low=middle;}
                next=low;target=CourierPathPosition(shipment,next,0);
            }
            if(!GroundMoveFree(planet,mover,target,out blocker))
            {
                if(!shipment.Airborne&&blocker!=null&&blocker.Shipment!=null)
                    shipment.Traffic.WaitTicks++;
                else GroundWait(planet,mover,blocker);
                return;
            }
            RecordGroundTrail(planet,shipment,next,Distance(mover.Position.X,mover.Position.Y,target.X,target.Y));
            // Reserve only a short launch interval. Traffic already on the route cannot hold this exit.
            if(exit!=null && next>0)exit.NextCourierExitTick=State.Tick+Definitions.CourierExitIntervalTicks;
            RecordGroundMove(planet,mover,target);shipment.Traffic.Docked=false;shipment.DistanceTravelled=next;
            shipment.PassingOffset=lane;
        }

        private static bool AtRouteEnd(ShipmentState shipment)
        { return shipment.DistanceTravelled + 0.000000001 >= ShipmentArrivalDistance(shipment); }

        public static bool IsConstructionVehicle(ShipmentState shipment)
        { return shipment.Phase == ShipmentPhase.BuilderOutbound || shipment.Phase == ShipmentPhase.Building || shipment.Phase == ShipmentPhase.BuilderReturning; }

        private static void ReverseCourierPath(ShipmentState shipment)
        {
            shipment.Path.Reverse();
            shipment.DistanceTravelled=shipment.Path.Count-1-shipment.DistanceTravelled;
            shipment.RouteBlocked=true;
        }

        private static void ReturnConstructionVehicle(ShipmentState shipment)
        {
            if (shipment.Phase == ShipmentPhase.BuilderReturning) return;
            // Finish the approach into the site before turning onto the return lane.
            // Reversing the lane offset at the work position would teleport across the road.
            shipment.BuilderTurnPending=true;
            shipment.Phase = ShipmentPhase.BuilderReturning;
            shipment.PhaseProgress = 0;
            shipment.RouteBlocked = true;
        }

        private bool EnsureShipmentRoute(PlanetState planet, ShipmentState shipment, BuildingState destination)
        {
            bool routeValid = true;
            var last = shipment.Path[shipment.Path.Count - 1];
            if (last.X != destination.X || last.Y != destination.Y) routeValid = false;
            int currentIndex = Math.Min((int)shipment.DistanceTravelled, shipment.Path.Count - 1);
            for (int i = currentIndex + 1; i < shipment.Path.Count; i++)
                if (!PlanetEnvironment.Airborne(planet) && (!GateEdgeOpen(planet,shipment.Path[i-1],shipment.Path[i]) || i<shipment.Path.Count-1 && !CargoCellOpen(planet,shipment.Path[i].X,shipment.Path[i].Y))) routeValid = false;
            if (!routeValid)
            {
                // Curved movement also depends on the next/previous segment. Preserve the exact world
                // position and physically rejoin a reachable cell before using the replacement curve.
                var originalPosition=GroundCourierPosition(shipment);
                bool insideDepot=GroundCourierDocked(shipment);
                double fraction = shipment.DistanceTravelled - currentIndex;
                var a = shipment.Path[currentIndex];
                var b = shipment.Path[Math.Min(currentIndex + 1, shipment.Path.Count - 1)];
                List<GridCell> replacement;
                GridCell anchor=a;
                if (fraction < 0.000000001 || IsConstructionVehicle(shipment)&&shipment.Phase!=ShipmentPhase.BuilderReturning&&b.X==destination.X&&b.Y==destination.Y)
                {
                    replacement = FindPath(planet, new BuildingState { X = a.X, Y = a.Y }, destination);
                }
                else
                {
                    replacement = CargoCellOpen(planet,b.X,b.Y)?FindPath(planet, new BuildingState { X = b.X, Y = b.Y }, destination):null;
                    if (replacement != null) anchor=b;
                    else
                    {
                        replacement = FindPath(planet, new BuildingState { X = a.X, Y = a.Y }, destination);
                    }
                }
                if (replacement == null)
                {
                    if (!shipment.RouteBlocked)
                        Emit(planet.OwnerId, planet.Id, shipment.Id, "DeliveryBlocked", IsConstructionVehicle(shipment) ?
                            "Baufahrzeug wartet auf eine befahrbare Verbindung." : "Lieferweg unterbrochen. Ladung bleibt im Transporter.");
                    shipment.RouteBlocked = true;
                    return false;
                }
                shipment.Path = replacement; shipment.DistanceTravelled = 0;
                shipment.PassingOffset=0;shipment.RoadEntryCentered=!insideDepot;
                var anchorPosition=HexGrid.ToWorld(anchor.X,anchor.Y);
                if(!insideDepot&&Distance(originalPosition.X,originalPosition.Y,anchorPosition.X,anchorPosition.Y)>GroundTrafficTolerance)
                {
                    if(shipment.Traffic==null)shipment.Traffic=new GroundTrafficState();
                    shipment.Traffic.Yielding=true;shipment.Traffic.Docked=false;shipment.Traffic.YieldForId=shipment.Id;
                    shipment.Traffic.YieldSinceTick=State.Tick;shipment.Traffic.StartX=originalPosition.X;shipment.Traffic.StartY=originalPosition.Y;
                    shipment.Traffic.GoalX=anchor.X;shipment.Traffic.GoalY=anchor.Y;shipment.Traffic.Progress=0;
                    if(shipment.Phase==ShipmentPhase.Building)shipment.Phase=ShipmentPhase.BuilderOutbound;
                }
            }
            if (shipment.RouteBlocked)
                Emit(planet.OwnerId, planet.Id, shipment.Id, "DeliveryResumed", "Lieferweg wieder verfügbar.");
            shipment.RouteBlocked = false;
            return shipment.Traffic==null||!shipment.Traffic.Yielding;
        }
    }
}
