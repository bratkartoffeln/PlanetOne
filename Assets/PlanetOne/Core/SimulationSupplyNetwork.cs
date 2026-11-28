using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        public List<GridCell> SupplyPath(int planetId,long sourceId,long targetId)
        {
            var p=FindPlanet(planetId);if(p==null)return null;
            return FindPath(p,p.Buildings.Find(b=>b.Id==sourceId&&!b.Destroyed),p.Buildings.Find(b=>b.Id==targetId&&!b.Destroyed));
        }
        public List<GridCell> PlanSupplyConnection(int planetId,int faction,long sourceId,int x,int y,out string error)
        {
            error=null;var p=FindPlanet(planetId);
            if(p==null||p.OwnerId!=faction||!FactionActive(faction)){error="Eigene Kolonie benötigt.";return null;}
            if(PlanetEnvironment.Airborne(p)){error="Frachtdrohnen verbinden Plattformen automatisch.";return null;}
            var source=p.Buildings.Find(b=>b.Id==sourceId&&!b.Destroyed&&!b.Deconstructing&&BuildingOwner(p,b)==faction);
            if(source==null||!Inside(p,x,y)){error="Eigenes Startgebäude und gültiges Ziel wählen.";return null;}
            var target=p.Buildings.Find(b=>b.X==x&&b.Y==y&&!b.Destroyed);
            if(target!=null&&(target.Deconstructing||BuildingOwner(p,target)!=faction)||source.X==x&&source.Y==y)
            {error="Ein anderes eigenes Gebäude oder einen freien Bauplatz wählen.";return null;}
            if(target==null&&PlanetEnvironment.BlocksGround(p.Terrain[y*p.Width+x])){error="Der Zielbauplatz ist nicht frei.";return null;}
            int start=source.Y*p.Width+source.X,finish=y*p.Width+x;
            var previous=new int[p.Roads.Length];for(int i=0;i<previous.Length;i++)previous[i]=-1;
            var occupied=new HashSet<int>();foreach(var b in p.Buildings)if(!b.Destroyed)occupied.Add(b.Y*p.Width+b.X);
            var queue=new Queue<int>();queue.Enqueue(start);previous[start]=start;
            while(queue.Count>0&&previous[finish]<0)
            {
                int at=queue.Dequeue();
                for(int d=0;d<6;d++)
                {
                    var cell=HexGrid.Neighbor(at%p.Width,at/p.Width,d);if(!Inside(p,cell.X,cell.Y))continue;
                    int next=cell.Y*p.Width+cell.X;
                    if(previous[next]>=0||next!=finish&&(p.Terrain[next]!=0||occupied.Contains(next)))continue;
                    previous[next]=at;queue.Enqueue(next);
                }
            }
            if(previous[finish]<0){error="Keine freie Verbindung um Gelände und Gebäude gefunden.";return null;}
            var path=new List<GridCell>();for(int at=finish;;at=previous[at]){path.Add(new GridCell(at%p.Width,at/p.Width));if(at==start)break;}
            path.Reverse();return path;
        }
        private void ApplySupplyConnection(SimCommand command)
        {
            string error;var path=PlanSupplyConnection(command.PlanetId,command.IssuerId,command.ActorId,command.X,command.Y,out error);
            if(path==null){Report(command,false,"InvalidConnection",error);return;}
            var p=FindPlanet(command.PlanetId);
            // Validate the complete path before changing any cell. Existing buildings
            // are endpoints; they never become roads through the middle of a colony.
            foreach(var cell in path)
                if((cell.X!=command.X||cell.Y!=command.Y)&&!p.Buildings.Exists(b=>!b.Destroyed&&b.X==cell.X&&b.Y==cell.Y))p.Roads[cell.Y*p.Width+cell.X]=true;
            Report(command,true,"SupplyConnected","Versorgungsverbindung angelegt. Waren werden durch Fahrzeuge geliefert.");
        }
    }
}
