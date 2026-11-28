using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        private sealed class CargoRoutes
        {
            public long Topology=long.MinValue;
            public bool[] Blocked;
            public readonly Dictionary<long,List<GridCell>> Paths=new Dictionary<long,List<GridCell>>();
        }
        private static readonly ConditionalWeakTable<PlanetState,CargoRoutes> cargoRoutes=new ConditionalWeakTable<PlanetState,CargoRoutes>();
        private static CargoRoutes CargoTopology(PlanetState p)
        {
            long hash=17;
            unchecked
            {
                foreach(int terrain in p.Terrain)hash=hash*31+(PlanetEnvironment.BlocksGround(terrain)?1:0);
                foreach(var b in p.Buildings)if(!b.Destroyed)hash=hash*31+b.X*103+b.Y*13+(int)b.Kind+(b.Built?1024:0)+(b.Deconstructing?2048:0)+BuildingOwner(p,b)*8192;
            }
            var cache=cargoRoutes.GetValue(p,_=>new CargoRoutes());
            if(cache.Topology==hash)return cache;
            cache.Topology=hash;cache.Paths.Clear();cache.Blocked=new bool[p.Terrain.Length];
            for(int i=0;i<p.Terrain.Length;i++)cache.Blocked[i]=PlanetEnvironment.BlocksGround(p.Terrain[i]);
            foreach(var b in p.Buildings)if(!b.Destroyed&&!(b.Kind==BuildingKind.Gate&&b.Built&&!b.Deconstructing&&BuildingOwner(p,b)==p.OwnerId))cache.Blocked[b.Y*p.Width+b.X]=true;
            return cache;
        }
        private static bool CargoCellOpen(PlanetState p,int x,int y)
        {return Inside(p,x,y)&&!PlanetEnvironment.BlocksGround(p.Terrain[y*p.Width+x])&&!p.Buildings.Exists(b=>!b.Destroyed&&b.X==x&&b.Y==y&&!(b.Kind==BuildingKind.Gate&&b.Built&&!b.Deconstructing&&BuildingOwner(p,b)==p.OwnerId));}
        private static List<GridCell> FreeCargoPath(PlanetState p,BuildingState source,BuildingState target)
        {
            if(source==null||target==null||!Inside(p,source.X,source.Y)||!Inside(p,target.X,target.Y))return null;
            if(PlanetEnvironment.Airborne(p))return PlanetEnvironment.AirPath(p.Width,p.Height,new GridCell(source.X,source.Y),new GridCell(target.X,target.Y));
            var cache=CargoTopology(p);int start=source.Y*p.Width+source.X,finish=target.Y*p.Width+target.X;
            long key=(long)start*p.Terrain.Length+finish;List<GridCell> found;
            if(cache.Paths.TryGetValue(key,out found))return found==null?null:new List<GridCell>(found);
            var previous=new int[p.Terrain.Length];for(int i=0;i<previous.Length;i++)previous[i]=-1;
            var queue=new Queue<int>();queue.Enqueue(start);previous[start]=start;
            while(queue.Count>0&&previous[finish]<0)
            {
                int at=queue.Dequeue();
                for(int d=0;d<6;d++)
                {
                    var n=HexGrid.Neighbor(at%p.Width,at/p.Width,d);if(!Inside(p,n.X,n.Y))continue;int next=n.Y*p.Width+n.X;
                    if(previous[next]>=0||next!=finish&&cache.Blocked[next])continue;
                    previous[next]=at;queue.Enqueue(next);
                }
            }
            if(previous[finish]<0){cache.Paths[key]=null;return null;}
            found=new List<GridCell>();for(int at=finish;;at=previous[at]){found.Add(new GridCell(at%p.Width,at/p.Width));if(at==start)break;}
            found.Reverse();cache.Paths[key]=found;return new List<GridCell>(found);
        }
    }
}
