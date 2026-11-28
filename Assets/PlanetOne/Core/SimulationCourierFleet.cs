using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        private void OpenGarage(BuildingState b)
        {
            if(b.Couriers!=null || !b.Built || b.Destroyed || b.Deconstructing)return;
            int count=b.Kind==BuildingKind.Headquarters?Definitions.CouriersPerColony:b.Kind==BuildingKind.Warehouse?2:1;
            b.Couriers=new List<long>(count);
            for(int i=0;i<count;i++)b.Couriers.Add(State.NextEntityId++);
        }
        private static long FreeCourier(PlanetState p,BuildingState home,bool reserves)
        {
            if(home==null || home.Couriers==null || home.Deconstructing || home.Destroyed)return 0;
            int start=reserves?1:0, end=reserves?home.Couriers.Count:1;
            // Older trips have no vehicle identifier. Keep an equivalent number of
            // garage places busy until those existing trips complete.
            int legacy=p.Shipments.FindAll(s=>s.CourierId==0 && s.SourceId==home.Id).Count;
            for(int i=0;i<end && i<home.Couriers.Count;i++)
            {
                long id=home.Couriers[i];
                if(p.Shipments.Exists(s=>s.CourierId==id))continue;
                if(legacy>0){legacy--;continue;}
                if(i>=start)return id;
            }
            return 0;
        }
        private long ChooseCourier(PlanetState p,BuildingState source,BuildingState target,out BuildingState home)
        {
            home=source;
            long id=FreeCourier(p,home,false);
            if(id!=0)return id;
            home=target;id=FreeCourier(p,home,false);
            if(id!=0)return id;
            int best=int.MaxValue;
            foreach(var depot in p.Buildings)
            {
                if(!IsStorage(depot) || BuildingOwner(p,depot)!=BuildingOwner(p,source))continue;
                long candidate=FreeCourier(p,depot,true);
                if(candidate==0)continue;
                var route=FindPath(p,depot,source);
                if(route==null || route.Count>=best)continue;
                best=route.Count;home=depot;id=candidate;
            }
            return id;
        }
        public static long CourierDestination(ShipmentState trip)
        {
            if(trip.ApproachingSource || trip.Phase==ShipmentPhase.Recovering)return trip.SourceId;
            if(trip.Phase==ShipmentPhase.Returning || trip.Phase==ShipmentPhase.BuilderReturning && !trip.BuilderTurnPending)return trip.HomeId==0?trip.SourceId:trip.HomeId;
            return trip.TargetId;
        }
        private void EvacuateGarage(PlanetState p,BuildingState old)
        {
            if(old.Couriers==null || old.Couriers.Count==0)return;
            var depot=p.Buildings.Find(b=>b!=old && b.Built && !b.Destroyed && !b.Deconstructing &&
                IsStorage(b) && BuildingOwner(p,b)==BuildingOwner(p,old) && FindPath(p,old,b)!=null);
            if(depot==null)
            {
                if(old.Destroyed)old.Couriers.RemoveAll(id=>!p.Shipments.Exists(s=>s.CourierId==id));
                return;
            }
            OpenGarage(depot);
            foreach(long id in old.Couriers)
            {
                var trip=p.Shipments.Find(s=>s.CourierId==id);
                if(trip==null && old.Destroyed)continue;
                if(trip==null)
                {
                    trip=new ShipmentState{Id=State.NextEntityId++,CourierId=id,SourceId=old.Id,TargetId=depot.Id,
                        Phase=ShipmentPhase.Returning,Airborne=PlanetEnvironment.Airborne(p),Path=FindPath(p,old,depot),
                        Traffic=new GroundTrafficState{Docked=true}};
                    p.Shipments.Add(trip);
                }
                if(trip.Phase==ShipmentPhase.Returning)trip.RouteBlocked=true;
                trip.HomeId=depot.Id;depot.Couriers.Add(id);
            }
            old.Couriers.Clear();
        }
        // Preserve an empty vehicle's world position when recalling it between depots.
        private static void RecallCourier(PlanetState p,ShipmentState trip)
        {
            if(trip.CourierId==0){p.Shipments.Remove(trip);return;}
            trip.Amount=0;trip.RecoveryReserved=false;trip.Phase=ShipmentPhase.Returning;
            trip.ApproachingSource=false;trip.PhaseProgress=0;trip.RouteBlocked=true;
        }
    }
}
