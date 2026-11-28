using System;
using System.Runtime.Serialization;

namespace PlanetOne.Core
{
    [DataContract,Serializable]
    public sealed class GroundTrailState
    {
        [DataMember] public int From,To;
        [DataMember] public double Wear;
        [DataMember] public long LastTick;
        public double Strength(long tick)
        {return Math.Max(0,Wear-Math.Max(0,tick-LastTick)*Definitions.TickSeconds/1800.0);}
    }
    public sealed partial class GameSimulation
    {
        private void RecordGroundTrail(PlanetState planet,ShipmentState shipment,double next,double distance)
        {
            double start=shipment.DistanceTravelled;
            if(shipment.Airborne || distance<=0 || next<=start || shipment.Path.Count<2)return;
            for(int index=(int)start;index<next && index<shipment.Path.Count-1;index++)
            {
                double portion=Math.Min(next,index+1)-Math.Max(start,index);
                WearTrailEdge(planet,shipment,index,distance*portion/(next-start));
            }
        }

        private void WearTrailEdge(PlanetState planet,ShipmentState shipment,int index,double distance)
        {
            var a=shipment.Path[index];var b=shipment.Path[index+1];int from=a.Y*planet.Width+a.X,to=b.Y*planet.Width+b.X;
            if(from>to){int swap=from;from=to;to=swap;}
            var trail=planet.Trails.Find(t=>t.From==from&&t.To==to);
            if(trail==null){trail=new GroundTrailState{From=from,To=to};planet.Trails.Add(trail);}
            trail.Wear=Math.Min(1,trail.Strength(State.Tick)+distance*.12);trail.LastTick=State.Tick;
        }
    }
}
