using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        public static int FortificationMask(PlanetState p,int x,int y,int owner,bool includeTowers=true)
        {
            int mask=0;
            for(int d=0;d<6;d++)
            {
                var cell=HexGrid.Neighbor(x,y,d);
                foreach(var b in p.Buildings)
                    if(!b.Destroyed && b.X==cell.X && b.Y==cell.Y && (b.Kind==BuildingKind.Wall || b.Kind==BuildingKind.Gate || includeTowers && Definitions.IsTower(b.Kind)) && BuildingOwner(p,b)==owner)
                    { mask |= 1<<d; break; }
            }
            return mask;
        }

        // Choose the opposing pair closest to the normal of the adjoining wall.
        // Prefer an unoccupied pair, including at bends and junctions.
        public static int GatePassage(int mask)
        {
            double best=double.MaxValue; int axis=1;
            for(int k=0;k<3;k++)
            {
                int d=(k+1)%3;
                var n=HexGrid.Neighbor(0,0,d); var along=HexGrid.ToWorld(n.X,n.Y);
                double cost=0;
                for(int j=0;j<6;j++) if((mask & (1<<j))!=0)
                {
                    var c=HexGrid.Neighbor(0,0,j);var arm=HexGrid.ToWorld(c.X,c.Y);
                    cost+=Math.Pow(along.X*arm.X+along.Y*arm.Y,2);
                    if(j%3==d)cost+=10;
                }
                if(cost<best-.000001){best=cost;axis=d;}
            }
            return axis;
        }
        public static bool GateEdgeOpen(PlanetState p,GridCell a,GridCell b)
        {
            if(a.X==b.X && a.Y==b.Y)return true;
            foreach(var gate in p.Buildings)
            {
                if(gate.Kind!=BuildingKind.Gate || gate.Destroyed || !gate.Built || gate.Deconstructing)continue;
                bool from=a.X==gate.X && a.Y==gate.Y;
                if(!from && (b.X!=gate.X || b.Y!=gate.Y))continue;
                var other=from?b:a;
                int axis=GatePassage(FortificationMask(p,gate.X,gate.Y,BuildingOwner(p,gate)));
                var first=HexGrid.Neighbor(gate.X,gate.Y,axis);var second=HexGrid.Neighbor(gate.X,gate.Y,axis+3);
                if(!(first.X==other.X && first.Y==other.Y || second.X==other.X && second.Y==other.Y))return false;
            }
            return true;
        }
        private sealed class CargoRoutes
        {
            public long Topology=long.MinValue;
            public bool[] Blocked;
            public int[] Passage;
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
            cache.Topology=hash;cache.Paths.Clear();cache.Blocked=new bool[p.Terrain.Length];cache.Passage=new int[p.Terrain.Length];
            foreach(var g in p.Buildings)if(g.Kind==BuildingKind.Gate && g.Built && !g.Destroyed && !g.Deconstructing)
                cache.Passage[g.Y*p.Width+g.X]=1+GatePassage(FortificationMask(p,g.X,g.Y,BuildingOwner(p,g)));
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
                    if(cache.Passage[at]!=0 && cache.Passage[at]!=1+d%3 || cache.Passage[next]!=0 && cache.Passage[next]!=1+d%3)continue;
                    previous[next]=at;queue.Enqueue(next);
                }
            }
            if(previous[finish]<0){cache.Paths[key]=null;return null;}
            found=new List<GridCell>();for(int at=finish;;at=previous[at]){found.Add(new GridCell(at%p.Width,at/p.Width));if(at==start)break;}
            found.Reverse();cache.Paths[key]=found;return new List<GridCell>(found);
        }
    }
}
