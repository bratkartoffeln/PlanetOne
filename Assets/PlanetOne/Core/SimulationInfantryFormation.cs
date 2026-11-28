using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        private static bool FootSoldier(UnitState u)
        { return u!=null && Definitions.Producer(u.Kind)==BuildingKind.Barracks; }
        private static SimPoint InfantryStand(int x,int y,int slot)
        {
            var center=HexGrid.ToWorld(x,y);double angle=(slot-1)*Math.PI*2/3;
            return new SimPoint(center.X+.34*Math.Cos(angle),center.Y+.34*Math.Sin(angle));
        }
        private GridCell? InfantryDestination(PlanetState p,UnitState u,int x,int y)
        {
            if(!FootSoldier(u) || !Inside(p,x,y) || !CanTraverse(p,u.OwnerId,u.Kind,x,y,u.Id))return null;
            // A unit on a service or combat assignment keeps its task and the ordinary
            // traffic controller negotiates passage. Only move/hold orders share stands.
            foreach(var worker in p.Units)
            {
                if(worker.OwnerId!=u.OwnerId || worker.Destroyed || worker.Order==UnitOrder.Move || worker.Order==UnitOrder.Hold)continue;
                var at=HexGrid.CellAt(worker.X,worker.Y);
                if(at.X==x && at.Y==y)return null;
            }
            var cells=new Queue<GridCell>();var seen=new HashSet<int>();
            cells.Enqueue(new GridCell(x,y));seen.Add(y*p.Width+x);
            while(cells.Count>0)
            {
                var cell=cells.Dequeue();
                if(CanTraverse(p,u.OwnerId,u.Kind,cell.X,cell.Y,u.Id) && AssignInfantryStand(p,u,cell))return cell;
                for(int i=0;i<6;i++)
                {
                    var n=HexGrid.Neighbor(cell.X,cell.Y,i);
                    if(Inside(p,n.X,n.Y) && GateEdgeOpen(p,cell,n) && CanTraverse(p,u.OwnerId,u.Kind,n.X,n.Y,u.Id) &&
                        seen.Add(n.Y*p.Width+n.X))cells.Enqueue(n);
                }
            }
            return null;
        }
        private bool AssignInfantryStand(PlanetState p,UnitState u,GridCell cell)
        {
            var peers=new List<UnitState>(2);
            foreach(var other in p.Units)
            {
                if(other==u || other.Destroyed || other.Layer!=TargetLayer.Ground)continue;
                var at=HexGrid.CellAt(other.X,other.Y);
                bool moving=other.Order==UnitOrder.Move || other.Order==UnitOrder.Hold && other.InfantrySlot!=0;
                if(!(moving?other.GoalX==cell.X && other.GoalY==cell.Y:at.X==cell.X && at.Y==cell.Y))continue;
                if(other.OwnerId!=u.OwnerId){if(CanSeeEntity(p,u.OwnerId,other.Id))return false;continue;}
                if(!FootSoldier(other) || peers.Count==2 || !moving && other.Order!=UnitOrder.Hold)return false;
                peers.Add(other);
            }
            if(peers.Count==0)return true;
            int used=0;
            foreach(var peer in peers)if(peer.InfantrySlot!=0)used|=1<<peer.InfantrySlot;
            peers.Add(u);
            foreach(var peer in peers)
            {
                if(peer.InfantrySlot!=0)continue;
                var here=GroundUnitPosition(peer);double best=double.MaxValue;int choice=0;
                for(int i=1;i<=3;i++)if((used&(1<<i))==0)
                {
                    var stand=InfantryStand(cell.X,cell.Y,i);
                    double length=Distance(here.X,here.Y,stand.X,stand.Y);
                    if(length<best){best=length;choice=i;}
                }
                peer.InfantrySlot=choice;used|=1<<choice;
                if(peer!=u && peer.Order==UnitOrder.Hold)
                {peer.GoalX=cell.X;peer.GoalY=cell.Y;peer.Order=UnitOrder.Move;peer.RecenterToCell=false;}
            }
            return true;
        }
        private GridCell InfantryExit(PlanetState p,UnitState u,GridCell cell,SimPoint from)
        {
            var center=HexGrid.ToWorld(cell.X,cell.Y);
            if(GroundSegmentClear(p,u,from,center,true))return cell;
            var best=cell;double shortest=double.MaxValue;
            for(int d=0;d<6;d++)
            {
                var n=HexGrid.Neighbor(cell.X,cell.Y,d);var to=HexGrid.ToWorld(n.X,n.Y);
                double cost=Distance(from.X,from.Y,to.X,to.Y)+HexGrid.Distance(n.X,n.Y,u.GoalX,u.GoalY);
                if(cost<shortest && GroundSegmentClear(p,u,from,to,true)){shortest=cost;best=n;}
            }
            return best;
        }
        private bool ApproachInfantrySlot(PlanetState p,UnitState u)
        {
            var here=GroundUnitPosition(u);var center=HexGrid.ToWorld(u.GoalX,u.GoalY);
            if(Distance(here.X,here.Y,center.X,center.Y)>1.65)return false;
            var goal=InfantryStand(u.GoalX,u.GoalY,u.InfantrySlot);var next=goal;
            if(!GroundSegmentClear(p,u,here,goal,true))
            {
                // Approach a free side of the formation. Search both sides instead of
                // displacing comrades who already occupy their assigned stands.
                double best=double.MaxValue;
                for(int i=0;i<12;i++)
                {
                    double angle=i*Math.PI/6;
                    var via=new SimPoint(center.X+.95*Math.Cos(angle),center.Y+.95*Math.Sin(angle));
                    double length=Distance(here.X,here.Y,via.X,via.Y)+Distance(via.X,via.Y,goal.X,goal.Y);
                    if(length<best && GroundSegmentClear(p,u,here,via,true) && GroundSegmentClear(p,u,via,goal,true))
                    {best=length;next=via;}
                }
                if(best==double.MaxValue)return false;
            }
            u.Path.Clear();u.PathDistance=0;
            double distance=Distance(here.X,here.Y,next.X,next.Y);
            var step=GroundLerp(here,next,Math.Min(1,Definitions.GroundSpeed(u)*Definitions.TickSeconds/Math.Max(.000001,distance)));
            var agent=UnitAgent(u);GroundAgent blocker;
            if(!GroundMoveFree(p,agent,step,out blocker))return true;
            RecordGroundMove(p,agent,step);SetGroundUnitPosition(u,step);
            if(Distance(step.X,step.Y,goal.X,goal.Y)<.000001){u.Order=UnitOrder.Hold;u.Status="Standplatz erreicht";}
            return true;
        }
    }
}
