using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        private bool GroundSegmentClear(PlanetState p,UnitState unit,SimPoint from,SimPoint to,bool traffic)
        {
            int steps=Math.Max(1,(int)Math.Ceiling(Distance(from.X,from.Y,to.X,to.Y)*8));
            var previous=HexGrid.FromWorld(from.X,from.Y);
            for(int i=1;i<=steps;i++)
            {
                var point=GroundLerp(from,to,i/(double)steps);var cell=HexGrid.FromWorld(point.X,point.Y);
                if(!GateEdgeOpen(p,previous,cell) || !CanTraverse(p,unit.OwnerId,unit.Kind,cell.X,cell.Y,unit.Id))return false;
                previous=cell;
            }
            if(traffic){var probe=UnitAgent(unit);probe.Position=from;if(GroundBlocker(p,probe,to,true)!=null)return false;}
            return true;
        }
        private List<GridCell> StraightenGroundPath(PlanetState p,UnitState unit,List<GridCell> path)
        {
            if(path==null||path.Count<3)return path;
            var result=new List<GridCell>{path[0]};int at=0;
            while(at<path.Count-1)
            {
                int next=path.Count-1;var from=HexGrid.ToWorld(path[at].X,path[at].Y);
                while(next>at+1&&!GroundSegmentClear(p,unit,from,HexGrid.ToWorld(path[next].X,path[next].Y),true))next--;
                result.Add(path[next]);at=next;
            }
            return result;
        }
        private bool CenterGroundUnit(PlanetState p,UnitState unit)
        {
            var cell=HexGrid.CellAt(unit.X,unit.Y);var to=HexGrid.ToWorld(cell.X,cell.Y);var from=GroundUnitPosition(unit);
            if(FootSoldier(unit) && p.Units.Exists(other=>other!=unit && !other.Destroyed && FootSoldier(other) && other.Layer==TargetLayer.Ground &&
                HexGrid.CellAt(other.X,other.Y).Equals(cell)))
            {
                cell=InfantryExit(p,unit,cell,from);to=HexGrid.ToWorld(cell.X,cell.Y);
            }
            double length=Distance(from.X,from.Y,to.X,to.Y);
            if(length<.000001){unit.X=cell.X;unit.Y=cell.Y;unit.RecenterToCell=false;return true;}
            var next=GroundLerp(from,to,Math.Min(1,Definitions.GroundSpeed(unit)*Definitions.TickSeconds/length));
            var agent=UnitAgent(unit);GroundAgent blocker;
            if(GroundMoveFree(p,agent,next,out blocker))
            {
                RecordGroundMove(p,agent,next);
                SetGroundUnitPosition(unit,next);unit.Status="Zentriert sich vor dem neuen Auftrag";
                if(Distance(next.X,next.Y,to.X,to.Y)<.000001){unit.X=cell.X;unit.Y=cell.Y;unit.RecenterToCell=false;}
            }
            else {GroundWait(p,agent,blocker);unit.Status="Wartet auf freie Zellmitte";}
            return false;
        }
    }
}
