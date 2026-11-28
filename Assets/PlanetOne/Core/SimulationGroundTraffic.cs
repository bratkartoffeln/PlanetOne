using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlanetOne.Core
{
    [DataContract,Serializable]
    public sealed class GroundTrafficState
    {
        [DataMember] public bool Docked,Yielding;
        [DataMember] public long YieldForId,YieldSinceTick;
        [DataMember] public int WaitTicks;
        [DataMember] public double StartX,StartY,Progress;
        [DataMember] public double RequestX,RequestY;
        [DataMember] public int GoalX,GoalY;
    }

    public sealed partial class GameSimulation
    {
        public const double GroundTrafficTolerance=0.00001;
        private sealed class GroundAgent
        {
            public UnitState Unit;public ShipmentState Shipment;
            public long Id {get{return Unit!=null?Unit.Id:Shipment.Id;}}
            public bool Cargo {get{return Shipment!=null;}}
            public int Owner;public double Radius;public SimPoint Position;
            public GroundTrafficState Traffic
            {
                get{return Unit!=null?Unit.Traffic:Shipment.Traffic;}
                set{if(Unit!=null)Unit.Traffic=value;else Shipment.Traffic=value;}
            }
        }
        private struct GroundSweep {public bool Cargo;public int Planet;public long Id;public double Radius;public SimPoint From,To;}
        private readonly List<GroundSweep> groundSweeps=new List<GroundSweep>();
        private long groundSweepTick=-1;
        public static double GroundUnitRadius(UnitKind kind)
        {return kind==UnitKind.Infantry?.20:kind==UnitKind.AntiTank?.22:.25;}
        public static double GroundCourierRadius(ShipmentState shipment){return .24;}
        public static SimPoint GroundUnitPosition(UnitState unit){return HexGrid.ToWorld(unit.X,unit.Y);}
        public static bool GroundCourierDocked(ShipmentState shipment)
        {return shipment.Phase==ShipmentPhase.Loading&&!shipment.ApproachingSource||shipment.Phase==ShipmentPhase.Unloading||shipment.Traffic!=null&&shipment.Traffic.Docked;}
        public static double ShipmentArrivalDistance(ShipmentState shipment)
        {return Math.Max(0,shipment.Path.Count-1-(shipment.Phase==ShipmentPhase.BuilderOutbound||shipment.Phase==ShipmentPhase.Building?.78:0));}
        public static SimPoint GroundCourierPosition(ShipmentState shipment)
        {
            if(shipment.Traffic!=null&&shipment.Traffic.Yielding)return YieldPosition(shipment.Traffic);
            return CourierPathPosition(shipment,shipment.DistanceTravelled,0);
        }
        public static SimPoint CourierDisplayPosition(ShipmentState shipment)
        {
            if (shipment.Traffic!=null && shipment.Traffic.Yielding)
                return GroundCourierPosition(shipment);
            return CourierPathPosition(shipment,shipment.DistanceTravelled,shipment.PassingOffset);
        }
        private static SimPoint CourierPathPosition(ShipmentState shipment,double distance,double lane)
        {
            var center=CourierCenterPosition(shipment,distance);
            if(shipment.Path.Count<2||shipment.Airborne)return center;
            double last=shipment.Path.Count-1;
            var a=CourierCenterPosition(shipment,Math.Max(0,distance-.001));
            var b=CourierCenterPosition(shipment,Math.Min(last,distance+.001));
            double dx=b.X-a.X,dy=b.Y-a.Y,length=Math.Sqrt(dx*dx+dy*dy);
            // Separate depot entrances keep opposing lanes apart right into the building.
            // Only a physical route repair from a road-cell centre needs a short merge.
            double taper=shipment.RoadEntryCentered?Math.Min(1,distance/.5):1;
            double offset=lane*Math.Max(0,taper);
            return length<1e-9?center:new SimPoint(center.X+dy/length*offset,center.Y-dx/length*offset);
        }
        public const double RoadHalfWidth=.13;
        public const double RoadLaneOffset=.30;
        private static SimPoint CourierCenterPosition(ShipmentState shipment,double distance)
        {
            if(shipment.Path.Count==0)return new SimPoint(0,0);
            int last=shipment.Path.Count-1;double d=Math.Max(0,Math.Min(last,distance));
            Func<int,SimPoint> point=i=>HexGrid.ToWorld(shipment.Path[i].X,shipment.Path[i].Y);
            if(last==0)return point(0);
            if(d<=.5)return GroundLerp(point(0),point(1),d);
            if(d>=last-.5)return GroundLerp(point(last-1),point(last),d-(last-1));
            int index=Math.Max(1,Math.Min(last-1,(int)Math.Floor(d+.5)));double t=d-index+.5;
            var center=point(index);var a=GroundLerp(point(index-1),center,.5);var b=GroundLerp(center,point(index+1),.5);
            return GroundLerp(GroundLerp(a,center,t),GroundLerp(center,b,t),t);
        }
        private static SimPoint GroundLerp(SimPoint a,SimPoint b,double t){return new SimPoint(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);}
        private static bool ReverseEdge(ShipmentState a,int i,ShipmentState b,int j)
        {
            return i>=0 && j>=0 && i+1<a.Path.Count && j+1<b.Path.Count &&
                a.Path[i].X==b.Path[j+1].X && a.Path[i].Y==b.Path[j+1].Y &&
                a.Path[i+1].X==b.Path[j].X && a.Path[i+1].Y==b.Path[j].Y;
        }
        private static bool PassingTraffic(PlanetState planet,ShipmentState shipment)
        {
            // Include the edge just passed so both tails clear before merging.
            foreach (var other in planet.Shipments)
            {
                if (other == shipment || other.Airborne || other.Phase==ShipmentPhase.Building || GroundCourierDocked(other)) continue;

                var a=CourierCenterPosition(shipment,shipment.DistanceTravelled);
                var b=CourierCenterPosition(other,other.DistanceTravelled);
                double separation=Distance(a.X,a.Y,b.X,b.Y);
                if (separation>2.5) continue;
                // At a fork the same two routes can approach on different branches.
                // Do not steer toward one another just because a nearby edge is shared.
                var right=CourierPathPosition(shipment,shipment.DistanceTravelled,RoadLaneOffset);
                var otherRight=CourierPathPosition(other,other.DistanceTravelled,RoadLaneOffset);
                if (Distance(right.X,right.Y,otherRight.X,otherRight.Y)<separation) continue;
                int first=Math.Max(0,(int)Math.Floor(shipment.DistanceTravelled)-1);
                int second=Math.Max(0,(int)Math.Floor(other.DistanceTravelled)-1);
                for(int i=first;i<shipment.Path.Count-1 && i<=shipment.DistanceTravelled+2;i++)
                    for(int j=second;j<other.Path.Count-1 && j<=other.DistanceTravelled+2;j++)
                        if(ReverseEdge(shipment,i,other,j)) return true;
            }
            return false;
        }
        private static SimPoint YieldPosition(GroundTrafficState traffic)
        {return GroundLerp(new SimPoint(traffic.StartX,traffic.StartY),HexGrid.ToWorld(traffic.GoalX,traffic.GoalY),traffic.Progress);}
        private static void SetGroundUnitPosition(UnitState unit,SimPoint world)
        {
            double row=world.Y/HexGrid.RowHeight;int bottom=(int)Math.Floor(row);double t=row-bottom;
            unit.Y=row;unit.X=world.X-((bottom&1)*(1-t)+((bottom+1)&1)*t)*.5;
        }
        private GroundAgent UnitAgent(UnitState unit)
        {return new GroundAgent{Unit=unit,Owner=unit.OwnerId,Radius=GroundUnitRadius(unit.Kind),Position=GroundUnitPosition(unit)};}
        private GroundAgent CourierAgent(PlanetState planet,ShipmentState shipment)
        {var source=planet.Buildings.Find(b=>b.Id==shipment.SourceId);return new GroundAgent{Shipment=shipment,Owner=source==null?planet.OwnerId:BuildingOwner(planet,source),Radius=GroundCourierRadius(shipment),Position=GroundCourierPosition(shipment)};}
        private IEnumerable<GroundAgent> GroundAgents(PlanetState planet)
        {
            foreach(var unit in planet.Units)if(!unit.Destroyed&&unit.Layer==TargetLayer.Ground)yield return UnitAgent(unit);
            foreach(var shipment in planet.Shipments)if(!GroundCourierDocked(shipment))yield return CourierAgent(planet,shipment);
        }
        private void PrepareGroundSweeps(){if(groundSweepTick!=State.Tick){groundSweepTick=State.Tick;groundSweeps.Clear();}}
        public static double GroundPointSegmentDistance(SimPoint point,SimPoint a,SimPoint b)
        {
            double dx=b.X-a.X,dy=b.Y-a.Y,length=dx*dx+dy*dy;
            double t=length<1e-16?0:Math.Max(0,Math.Min(1,((point.X-a.X)*dx+(point.Y-a.Y)*dy)/length));
            return Distance(point.X,point.Y,a.X+dx*t,a.Y+dy*t);
        }
        private static double GroundSegmentDistance(SimPoint a,SimPoint b,SimPoint c,SimPoint d)
        {
            double abx=b.X-a.X,aby=b.Y-a.Y,cdx=d.X-c.X,cdy=d.Y-c.Y,den=abx*cdy-aby*cdx;
            if(Math.Abs(den)>1e-12)
            {double t=((c.X-a.X)*cdy-(c.Y-a.Y)*cdx)/den,u=((c.X-a.X)*aby-(c.Y-a.Y)*abx)/den;if(t>=0&&t<=1&&u>=0&&u<=1)return 0;}
            return Math.Min(Math.Min(GroundPointSegmentDistance(a,c,d),GroundPointSegmentDistance(b,c,d)),Math.Min(GroundPointSegmentDistance(c,a,b),GroundPointSegmentDistance(d,a,b)));
        }
        // Clip the movement segment against the open interior of a pointy-top hex.
        // Its boundary stays passable, including the edge between two parked tanks.
        private static bool EntersParkedHex(SimPoint from, SimPoint to, SimPoint parked)
        {
            var cell = HexGrid.FromWorld(parked.X, parked.Y);
            var center = HexGrid.ToWorld(cell.X, cell.Y);
            double enter = 0, leave = 1;
            for (int direction = 0; direction < 6; direction++)
            {
                var neighbor = HexGrid.Neighbor(cell.X, cell.Y, direction);
                var adjacent = HexGrid.ToWorld(neighbor.X, neighbor.Y);
                double nx = adjacent.X - center.X, ny = adjacent.Y - center.Y;
                double margin = .5 - GroundTrafficTolerance - ((from.X-center.X)*nx + (from.Y-center.Y)*ny);
                double speed = (to.X-from.X)*nx + (to.Y-from.Y)*ny;
                if (Math.Abs(speed) < 1e-12) { if (margin <= 0) return false; continue; }
                if (speed > 0) leave = Math.Min(leave, margin/speed);
                else enter = Math.Max(enter, margin/speed);
                if (enter >= leave) return false;
            }
            return true;
        }
        private static bool BlocksGroundSegment(GroundAgent mover,SimPoint destination,GroundAgent other)
        {
            if (mover.Cargo && other.Cargo) return false;
            double distance=GroundPointSegmentDistance(other.Position,mover.Position,destination);
            if (distance < mover.Radius+other.Radius-GroundTrafficTolerance) return true;
            // Even an off-centre parked unit is at most two hex radii from any point in its cell.
            if (distance > 2*HexGrid.Radius || mover.Unit==null || other.Unit==null) return false;
            if(FootSoldier(mover.Unit) && FootSoldier(other.Unit))return false;
            return other.Unit.Order==UnitOrder.Hold && !other.Unit.RecenterToCell &&
                (other.Traffic==null || !other.Traffic.Yielding) && EntersParkedHex(mover.Position,destination,other.Position);
        }
        private GroundAgent GroundBlocker(PlanetState planet,GroundAgent mover,SimPoint destination,bool knownOnly=false)
        {
            GroundAgent result=null;
            foreach(var other in GroundAgents(planet))
            {
                if(other.Id==mover.Id)continue;
                if(knownOnly&&other.Owner!=mover.Owner&&!IsCellVisible(planet,mover.Owner,other.Position.X,other.Position.Y/HexGrid.RowHeight))continue;
                if (!BlocksGroundSegment(mover,destination,other)) continue;
                if(result==null||other.Id<result.Id)result=other;
            }
            return result;
        }
        private bool GroundMoveFree(PlanetState planet,GroundAgent mover,SimPoint destination,out GroundAgent blocker)
        {
            PrepareGroundSweeps();blocker=GroundBlocker(planet,mover,destination);
            if(blocker!=null)return false;
            if(FootSoldier(mover.Unit))
            {
                var cell=HexGrid.FromWorld(destination.X,destination.Y);
                var start=HexGrid.FromWorld(mover.Position.X,mover.Position.Y);
                if(start.X!=cell.X || start.Y!=cell.Y)
                {
                    int count=0;
                    foreach(var other in planet.Units)
                    {
                        if(other==mover.Unit || other.Destroyed || other.Layer!=TargetLayer.Ground || !FootSoldier(other))continue;
                        var at=HexGrid.CellAt(other.X,other.Y);
                        if(at.X==cell.X && at.Y==cell.Y && ++count>=3)return false;
                    }
                }
            }
            // Give a committed cooperative maneuver its complete remaining corridor. Without this,
            // another actor can enter halfway through and two individually safe maneuvers deadlock.
            foreach(var other in GroundAgents(planet))
                if(!(mover.Cargo&&other.Cargo)&&other.Id!=mover.Id&&other.Traffic!=null&&other.Traffic.Yielding&&other.Traffic.Progress<1)
                {
                    var finish=HexGrid.ToWorld(other.Traffic.GoalX,other.Traffic.GoalY);
                    double limit=mover.Radius+other.Radius-GroundTrafficTolerance;
                    double corridorDistance=GroundSegmentDistance(mover.Position,destination,other.Position,finish);
                    if(corridorDistance<limit)
                    {
                        double initial=GroundPointSegmentDistance(mover.Position,other.Position,finish);
                        // A route repair may inherit intersecting intentions. Permit a physical
                        // escape out of that corridor, never a move deeper into the reserved path.
                        bool leavesCorridor=GroundPointSegmentDistance(destination,other.Position,finish)>initial+GroundTrafficTolerance;
                        bool opensAhead=Distance(destination.X,destination.Y,other.Position.X,other.Position.Y)>Distance(mover.Position.X,mover.Position.Y,other.Position.X,other.Position.Y)+GroundTrafficTolerance;
                        // Backing into a dock ahead of an approaching helper follows the same
                        // corridor, but opens space rather than entering the helper's body.
                        // Physical bodies and accepted tick sweeps still remain mandatory.
                        if(initial<limit&&(opensAhead||corridorDistance+GroundTrafficTolerance>=initial&&leavesCorridor))continue;
                        blocker=other;return false;
                    }
                }
            foreach(var sweep in groundSweeps)
                if(!(mover.Cargo&&sweep.Cargo)&&sweep.Planet==planet.Id&&sweep.Id!=mover.Id&&GroundSegmentDistance(mover.Position,destination,sweep.From,sweep.To)<mover.Radius+sweep.Radius-GroundTrafficTolerance)return false;
            return true;
        }
        private void RecordGroundMove(PlanetState planet,GroundAgent mover,SimPoint destination)
        {PrepareGroundSweeps();groundSweeps.Add(new GroundSweep{Cargo=mover.Cargo,Planet=planet.Id,Id=mover.Id,Radius=mover.Radius,From=mover.Position,To=destination});if(mover.Traffic!=null)mover.Traffic.WaitTicks=0;}
        public bool GroundCellAvailable(PlanetState planet,int faction,UnitKind kind,int x,int y,long except=0)
        {
            if(!CanTraverse(planet,faction,kind,x,y,except))return false;
            var at=HexGrid.ToWorld(x,y);var probe=new GroundAgent{Unit=new UnitState{Id=except,OwnerId=faction,Kind=kind},Owner=faction,Radius=GroundUnitRadius(kind),Position=at};
            GroundAgent blocked;return GroundMoveFree(planet,probe,at,out blocked)&&YieldDestinationFree(planet,probe,at);
        }
        private bool YieldDestinationFree(PlanetState planet,GroundAgent mover,SimPoint target)
        {
            // Several couriers may use the interior of their actual endpoint depot. Its entrance
            // remains protected by physical sweeps; an intended later arrival must not prevent
            // a vehicle already at that entrance from backing fully inside to clear the way.
            if(mover.Shipment!=null&&planet.Buildings.Exists(b=>b.Built&&(b.Id==mover.Shipment.SourceId||b.Id==mover.Shipment.TargetId)&&Distance(target.X,target.Y,HexGrid.ToWorld(b.X,b.Y).X,HexGrid.ToWorld(b.X,b.Y).Y)<GroundTrafficTolerance))return true;
            foreach(var other in GroundAgents(planet))
                if(!(mover.Cargo&&other.Cargo)&&other.Id!=mover.Id&&other.Traffic!=null&&other.Traffic.Yielding)
                {var goal=HexGrid.ToWorld(other.Traffic.GoalX,other.Traffic.GoalY);if(Distance(target.X,target.Y,goal.X,goal.Y)<mover.Radius+other.Radius-GroundTrafficTolerance)return false;}
            return true;
        }
        private bool GroundKnownCellFree(PlanetState planet,UnitState unit,int x,int y)
        {
            var probe=UnitAgent(unit);probe.Position=HexGrid.ToWorld(x,y);
            foreach(var other in GroundAgents(planet))
            {
                if(other.Id==unit.Id)continue;
                if(other.Owner!=unit.OwnerId && (other.Unit!=null?!CanSeeEntity(planet,unit.OwnerId,other.Id):!IsCellVisible(planet,unit.OwnerId,HexGrid.FromWorld(other.Position.X,other.Position.Y).X,HexGrid.FromWorld(other.Position.X,other.Position.Y).Y)))continue;
                var occupied=HexGrid.FromWorld(other.Position.X,other.Position.Y);
                if(occupied.X==x&&occupied.Y==y)return false;
            }
            return true;
        }
        private bool LandingBlocksGround(PlanetState planet,int x,int y)
        {
            return State.Ships.Exists(s=>!s.Destroyed&&!s.Consumed&&s.TargetPlanetId==planet.Id&&s.TransportPhase!=TransportPhase.Space&&
                HexGrid.Steps(x,y,s.LandingX,s.LandingY)<=(s.TransportPhase==TransportPhase.Landed?0:1));
        }
        private bool YieldCellLegal(PlanetState planet,GroundAgent agent,int x,int y)
        {
            if(agent.Unit!=null)return CanTraverse(planet,agent.Owner,agent.Unit.Kind,x,y,agent.Id);
            bool rejoining=agent.Traffic!=null&&agent.Traffic.Yielding&&agent.Traffic.YieldForId==agent.Id;
            return Inside(planet,x,y)&&!PlanetEnvironment.BlocksGround(planet.Terrain[y*planet.Width+x])&&!planet.Buildings.Exists(b=>!b.Destroyed&&b.X==x&&b.Y==y&&!((b.Built||rejoining)&&(b.Id==agent.Shipment.SourceId||b.Id==agent.Shipment.TargetId||b.Id==agent.Shipment.HomeId)))&&!LandingBlocksGround(planet,x,y);
        }
        private bool CourierYieldReachable(PlanetState planet,GroundAgent agent,GridCell cell)
        {
            if(agent.Shipment==null)return true;
            var shipment=agent.Shipment;
            var target=planet.Buildings.Find(b=>b.Id==CourierDestination(shipment));
            return FindPath(planet,new BuildingState{X=cell.X,Y=cell.Y},target)!=null;
        }
        private bool YieldSegmentLegal(PlanetState planet,GroundAgent agent,SimPoint to)
        {
            int samples=Math.Max(1,(int)Math.Ceiling(Distance(agent.Position.X,agent.Position.Y,to.X,to.Y)/.15));
            var start=HexGrid.FromWorld(agent.Position.X,agent.Position.Y);
            for(int i=1;i<=samples;i++)
            {var point=GroundLerp(agent.Position,to,i/(double)samples);var cell=HexGrid.FromWorld(point.X,point.Y);if(cell.X==start.X&&cell.Y==start.Y)continue;if(!GateEdgeOpen(planet,start,cell) || !YieldCellLegal(planet,agent,cell.X,cell.Y))return false;start=cell;}
            return true;
        }
        private bool StartYield(PlanetState planet,GroundAgent requester,GroundAgent blocker,HashSet<long> visited,int depth)
        {
            // Working machinery is a stationary worksite, never a cooperative passing vehicle.
            if(blocker.Shipment!=null&&blocker.Shipment.Phase==ShipmentPhase.Building)return false;
            if(blocker.Owner!=requester.Owner||depth>planet.Units.Count+planet.Shipments.Count||!visited.Add(blocker.Id))return false;
            // An already parked helper may still obstruct a turning route. Move it onward legally
            // instead of waiting forever for the requester to pass through its occupied position.
            if(blocker.Traffic!=null&&blocker.Traffic.Yielding&&blocker.Traffic.Progress<1)
            {
                var finish=HexGrid.ToWorld(blocker.Traffic.GoalX,blocker.Traffic.GoalY);GroundAgent pending;
                if(YieldSegmentLegal(planet,blocker,finish)&&GroundMoveFree(planet,blocker,finish,out pending))return true;
                // A restored or rerouted maneuver may already have a dependency. Replan from its
                // actual position, preserving the underlying command, instead of claiming progress.
            }
            var center=HexGrid.FromWorld(blocker.Position.X,blocker.Position.Y);var candidates=new List<GridCell>();
            if(blocker.Shipment!=null&&planet.Buildings.Exists(b=>(b.Id==blocker.Shipment.SourceId||b.Id==blocker.Shipment.TargetId)&&b.Built&&b.X==center.X&&b.Y==center.Y)&&Distance(blocker.Position.X,blocker.Position.Y,HexGrid.ToWorld(center.X,center.Y).X,HexGrid.ToWorld(center.X,center.Y).Y)>GroundTrafficTolerance)candidates.Add(center);
            for(int direction=0;direction<6;direction++){var next=HexGrid.Neighbor(center.X,center.Y,direction);if(YieldCellLegal(planet,blocker,next.X,next.Y)&&CourierYieldReachable(planet,blocker,next))candidates.Add(next);}
            if(blocker.Radius+requester.Radius>1)
                for(int y=center.Y-2;y<=center.Y+2;y++)for(int x=center.X-2;x<=center.X+2;x++)
                    if(HexGrid.Steps(center.X,center.Y,x,y)==2&&YieldCellLegal(planet,blocker,x,y)&&CourierYieldReachable(planet,blocker,new GridCell(x,y)))candidates.Add(new GridCell(x,y));
            candidates.Sort((a,b)=>
            {
                var aw=HexGrid.ToWorld(a.X,a.Y);var bw=HexGrid.ToWorld(b.X,b.Y);
                double dx=blocker.Position.X-requester.Position.X,dy=blocker.Position.Y-requester.Position.Y;
                double da=Math.Abs((aw.X-blocker.Position.X)*dy-(aw.Y-blocker.Position.Y)*dx)*3+Distance(aw.X,aw.Y,requester.Position.X,requester.Position.Y)*.2;
                double db=Math.Abs((bw.X-blocker.Position.X)*dy-(bw.Y-blocker.Position.Y)*dx)*3+Distance(bw.X,bw.Y,requester.Position.X,requester.Position.Y)*.2;
                int comparison=db.CompareTo(da);return comparison!=0?comparison:(a.Y*planet.Width+a.X).CompareTo(b.Y*planet.Width+b.X);
            });
            foreach(var cell in candidates)
            {
                var target=HexGrid.ToWorld(cell.X,cell.Y);if(!YieldSegmentLegal(planet,blocker,target))continue;
                GroundAgent obstruction;
                if(YieldDestinationFree(planet,blocker,target)&&GroundMoveFree(planet,blocker,target,out obstruction))
                {
                    var state=blocker.Traffic??new GroundTrafficState();blocker.Traffic=state;
                    state.Yielding=true;state.Docked=false;state.YieldForId=requester.Id;state.YieldSinceTick=State.Tick;
                    state.RequestX=requester.Position.X;state.RequestY=requester.Position.Y;
                    state.StartX=blocker.Position.X;state.StartY=blocker.Position.Y;state.GoalX=cell.X;state.GoalY=cell.Y;state.Progress=0;return true;
                }
            }
            foreach(var cell in candidates)
            {
                var target=HexGrid.ToWorld(cell.X,cell.Y);if(!YieldSegmentLegal(planet,blocker,target))continue;
                var obstruction=GroundBlocker(planet,blocker,target);
                if(obstruction!=null&&obstruction.Id!=requester.Id&&StartYield(planet,requester,obstruction,visited,depth+1))return true;
            }
            return false;
        }
        private void GroundWait(PlanetState planet,GroundAgent mover,GroundAgent blocker)
        {
            if(mover.Traffic==null)mover.Traffic=new GroundTrafficState();mover.Traffic.WaitTicks=Math.Min(int.MaxValue-1,mover.Traffic.WaitTicks)+1;
            if(blocker==null||blocker.Owner!=mover.Owner||mover.Traffic.Yielding)return;
            bool moving=GroundIntendsTravel(planet,blocker);
            int blockerWait=blocker.Traffic==null?0:blocker.Traffic.WaitTicks;
            if(moving&&blockerWait<8)
            {
                // A repair/resupply/attack order can be stationary for a long time. Count actual
                // blocked requests, while every successful movement resets this counter.
                if(blocker.Traffic==null)blocker.Traffic=new GroundTrafficState();blocker.Traffic.WaitTicks=blockerWait+1;return;
            }
            // Keep precedence stable throughout an encounter. Successful motion resets wait
            // counters; comparing those counters reverses precedence and creates a livelock.
            if(moving&&blocker.Id<mover.Id)
            {StartYield(planet,blocker,mover,new HashSet<long>{blocker.Id},0);return;}
            if(!StartYield(planet,mover,blocker,new HashSet<long>{mover.Id},0))
                // In a narrow approach the blocker may need the requester to back up before it
                // has turning clearance. Priority cannot manufacture that space: retreat legally.
                StartYield(planet,blocker,mover,new HashSet<long>{blocker.Id},0);
        }
        private bool TickGroundYield(PlanetState planet,GroundAgent agent)
        {
            var traffic=agent.Traffic;if(traffic==null||!traffic.Yielding)return false;
            if(traffic.Progress<1)
            {
                var finish=HexGrid.ToWorld(traffic.GoalX,traffic.GoalY);double length=Distance(traffic.StartX,traffic.StartY,finish.X,finish.Y);
                double speed=agent.Unit!=null?Definitions.GroundSpeed(agent.Unit):Definitions.CourierSpeed;
                double progress=Math.Min(1,traffic.Progress+speed*Definitions.TickSeconds/Math.Max(.001,length));
                var next=GroundLerp(new SimPoint(traffic.StartX,traffic.StartY),finish,progress);GroundAgent blocker;
                if(!YieldSegmentLegal(planet,agent,next))
                {StartYield(planet,agent,agent,new HashSet<long>(),0);return true;}
                if(!GroundMoveFree(planet,agent,next,out blocker))
                {if(blocker!=null&&blocker.Owner==agent.Owner)StartYield(planet,agent,blocker,new HashSet<long>{agent.Id},0);return true;}
                RecordGroundMove(planet,agent,next);traffic.Progress=progress;
                if(agent.Unit!=null){SetGroundUnitPosition(agent.Unit,next);agent.Unit.Status="Macht eigener Einheit Platz";}
                else if(progress>=1&&planet.Buildings.Exists(b=>(b.Id==agent.Shipment.SourceId||b.Id==agent.Shipment.TargetId||b.Id==agent.Shipment.HomeId)&&b.Built&&b.X==traffic.GoalX&&b.Y==traffic.GoalY))traffic.Docked=true;
                return true;
            }
            if(State.Tick-traffic.YieldSinceTick<12)return true;
            var requester=FindGroundAgent(planet,traffic.YieldForId);
            // A requester may be backing up specifically to give this parked helper turning
            // clearance. Reconsider that turn while the clearance opens; otherwise both
            // resume their original routes and repeat the identical blocked encounter.
            if(agent.Shipment!=null&&!traffic.Docked&&requester!=null&&requester.Traffic!=null&&requester.Traffic.Yielding&&
                requester.Traffic.YieldForId==agent.Id&&requester.Traffic.Progress<1&&
                StartYield(planet,requester,agent,new HashSet<long>{requester.Id},0))return true;
            if(requester!=null&&!(agent.Cargo&&requester.Cargo)&&requester.Id!=agent.Id&&GroundIntendsTravel(planet,requester))
            {
                double dx=traffic.StartX-traffic.RequestX,dy=traffic.StartY-traffic.RequestY,length=Math.Sqrt(dx*dx+dy*dy);
                double travel=length<GroundTrafficTolerance?double.MaxValue:((requester.Position.X-traffic.RequestX)*dx+(requester.Position.Y-traffic.RequestY)*dy)/length;
                bool passed=travel>=length+agent.Radius+requester.Radius||Distance(requester.Position.X,requester.Position.Y,traffic.RequestX,traffic.RequestY)>3;
                // Keep the passing bay until the requested actor has actually passed. Returning
                // after a fixed delay makes opposing couriers endlessly meet at the same junction.
                // A parked helper can still be requested to move farther by StartYield above.
                // An interior dock has already opened the physical corridor. Its helper
                // must stay inside while the outside requester resumes; breaking a logical
                // cycle here would send both vehicles straight back into the same encounter.
                if(!passed&&(traffic.Docked||!GroundYieldCycleLeader(planet,agent)))return true;
            }
            if(agent.Unit!=null)
            {
                agent.Unit.Path.Clear();agent.Unit.PathDistance=0;
                if(agent.Unit.Order==UnitOrder.Hold && agent.Unit.InfantrySlot>0)agent.Unit.Order=UnitOrder.Move;
                agent.Unit.Status="Setzt ursprünglichen Auftrag fort";
            }
            else
            {
                var shipment=agent.Shipment;
                var destination=planet.Buildings.Find(b=>b.Id==CourierDestination(shipment));
                var path=FindPath(planet,new BuildingState{X=traffic.GoalX,Y=traffic.GoalY},destination);if(path==null)return true;
                shipment.Path=path;shipment.DistanceTravelled=0;
                shipment.PassingOffset=0;shipment.RoadEntryCentered=!traffic.Docked;
                if(shipment.Phase==ShipmentPhase.Building)shipment.Phase=ShipmentPhase.BuilderOutbound;
            }
            traffic.Yielding=false;traffic.YieldForId=0;traffic.Progress=0;return true;
        }
        private GroundAgent FindGroundAgent(PlanetState planet,long id)
        {foreach(var agent in GroundAgents(planet))if(agent.Id==id)return agent;return null;}
        private bool GroundIntendsTravel(PlanetState planet,GroundAgent agent)
        {
            if(agent.Traffic!=null&&agent.Traffic.Yielding&&agent.Traffic.Progress<1)return true;
            if(agent.Unit==null)return agent.Shipment.Phase!=ShipmentPhase.Building;
            var unit=agent.Unit;var definition=Definitions.Get(unit.Kind);
            if(unit.Order==UnitOrder.Hold)return false;
            if(unit.Order==UnitOrder.Move||unit.Order==UnitOrder.Retreat)return true;
            if(unit.Order==UnitOrder.Bombard)return HexGrid.Distance(unit.X,unit.Y,unit.GoalX,unit.GoalY)>definition.Range;
            double x,y;int owner;TargetLayer layer;
            if(!Target(planet,unit.TargetId,out x,out y,out owner,out layer))return false;
            if(unit.Order==UnitOrder.Attack)
                return CanSeeEntity(planet,unit.OwnerId,unit.TargetId)&&(HexGrid.Distance(unit.X,unit.Y,x,y)>definition.Range||!definition.Indirect&&!HasLineOfSight(planet,unit.X,unit.Y,x,y));
            var target=planet.Units.Find(u=>u.Id==unit.TargetId&&!u.Destroyed);
            double range=target==null?1.1:Math.Max(1.1,GroundUnitRadius(unit.Kind)+GroundUnitRadius(target.Kind)+.15);
            return HexGrid.Distance(unit.X,unit.Y,x,y)>range;
        }
        private bool GroundYieldCycleLeader(PlanetState planet,GroundAgent start)
        {
            var visited=new HashSet<long>();long minimum=start.Id;var current=start;
            while(current!=null&&current.Traffic!=null&&current.Traffic.Yielding&&visited.Add(current.Id))
            {
                minimum=Math.Min(minimum,current.Id);long next=current.Traffic.YieldForId;
                if(next==start.Id)return start.Id==minimum;
                current=FindGroundAgent(planet,next);
            }
            return false;
        }
        private bool MoveGroundTraffic(PlanetState planet,UnitState unit,SimPoint destination)
        {
            var agent=UnitAgent(unit);GroundAgent blocker;
            if(!GroundMoveFree(planet,agent,destination,out blocker))
            {
                var cell=HexGrid.CellAt(unit.X,unit.Y);
                double offset=HexGrid.Distance(unit.X,unit.Y,cell.X,cell.Y);
                GroundAgent retreatBlocker;
                if(offset<.000001||GroundMoveFree(planet,agent,HexGrid.ToWorld(cell.X,cell.Y),out retreatBlocker))
                {
                    bool adjacent=unit.Order!=UnitOrder.Move&&unit.Order!=UnitOrder.Retreat;
                    var detour=GroundPath(planet,unit,unit.GoalX,unit.GoalY,adjacent,true,false);
                    if(detour!=null&&detour.Count>1)
                    {unit.Path=offset<.000001?StraightenGroundPath(planet,unit,detour):new List<GridCell>();unit.DirectPath=true;unit.PathDistance=0;unit.Status="Umfährt belegten Weg";return false;}
                }
                GroundWait(planet,agent,blocker);unit.Status="Wartet auf freien Weg";return false;
            }
            RecordGroundMove(planet,agent,destination);return true;
        }
    }
}
