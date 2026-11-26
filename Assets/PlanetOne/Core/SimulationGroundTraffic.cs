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
            public int Owner;public double Radius;public SimPoint Position;
            public GroundTrafficState Traffic
            {
                get{return Unit!=null?Unit.Traffic:Shipment.Traffic;}
                set{if(Unit!=null)Unit.Traffic=value;else Shipment.Traffic=value;}
            }
        }
        private struct GroundSweep {public int Planet;public long Id;public double Radius;public SimPoint From,To;}
        private readonly List<GroundSweep> groundSweeps=new List<GroundSweep>();
        private long groundSweepTick=-1;
        public static double GroundUnitRadius(UnitKind kind)
        {return kind==UnitKind.Infantry?.36:kind==UnitKind.AntiTank?.38:.46;}
        public static double GroundCourierRadius(ShipmentState shipment){return .24;}
        public static SimPoint GroundUnitPosition(UnitState unit){return HexGrid.ToWorld(unit.X,unit.Y);}
        public static bool GroundCourierDocked(ShipmentState shipment)
        {return shipment.Phase==ShipmentPhase.Loading||shipment.Phase==ShipmentPhase.Unloading||shipment.Traffic!=null&&shipment.Traffic.Docked;}
        public static double ShipmentArrivalDistance(ShipmentState shipment)
        {return Math.Max(0,shipment.Path.Count-1-(shipment.Phase==ShipmentPhase.BuilderOutbound||shipment.Phase==ShipmentPhase.Building?.78:0));}
        public static SimPoint GroundCourierPosition(ShipmentState shipment)
        {
            if(shipment.Traffic!=null&&shipment.Traffic.Yielding)return YieldPosition(shipment.Traffic);
            return CourierPathPosition(shipment,shipment.DistanceTravelled);
        }
        private static SimPoint CourierPathPosition(ShipmentState shipment,double distance)
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
            double offset=RoadLaneOffset*Math.Max(0,taper);
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
        private bool RoadDepartureFree(PlanetState planet,ShipmentState shipment)
        {
            double end=shipment.Path.Count-1;
            foreach(var other in planet.Shipments)
            {
                if(other.Id==shipment.Id||other.Airborne||GroundCourierDocked(other))continue;
                bool following=shipment.Path.Count==other.Path.Count&&!IsConstructionVehicle(other)&&!IsConstructionVehicle(shipment);
                if(following)for(int i=0;i<shipment.Path.Count;i++)
                    if(shipment.Path[i].X!=other.Path[i].X||shipment.Path[i].Y!=other.Path[i].Y){following=false;break;}
                if(following)continue;
                double otherEnd=other.Path.Count-1;
                for(double d=0;d<end;d+=.2)
                {
                    var a=CourierPathPosition(shipment,d);var b=CourierPathPosition(shipment,Math.Min(end,d+.2));
                    for(double e=other.DistanceTravelled;e<otherEnd;e+=.2)
                    {
                        var c=CourierPathPosition(other,e);var f=CourierPathPosition(other,Math.Min(otherEnd,e+.2));
                        if(GroundSegmentDistance(a,b,c,f)<GroundCourierRadius(shipment)+GroundCourierRadius(other)+.02)return false;
                    }
                }
            }
            return true;
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
        private GroundAgent GroundBlocker(PlanetState planet,GroundAgent mover,SimPoint destination,bool knownOnly=false)
        {
            GroundAgent result=null;
            foreach(var other in GroundAgents(planet))
            {
                if(other.Id==mover.Id)continue;
                if(knownOnly&&other.Owner!=mover.Owner&&!IsCellVisible(planet,mover.Owner,other.Position.X,other.Position.Y/HexGrid.RowHeight))continue;
                double limit=mover.Radius+other.Radius-GroundTrafficTolerance;
                if(GroundPointSegmentDistance(other.Position,mover.Position,destination)>=limit)continue;
                if(result==null||other.Id<result.Id)result=other;
            }
            return result;
        }
        private bool GroundMoveFree(PlanetState planet,GroundAgent mover,SimPoint destination,out GroundAgent blocker)
        {
            PrepareGroundSweeps();blocker=GroundBlocker(planet,mover,destination);
            if(blocker!=null)return false;
            // Give a committed cooperative maneuver its complete remaining corridor. Without this,
            // another actor can enter halfway through and two individually safe maneuvers deadlock.
            foreach(var other in GroundAgents(planet))
                if(other.Id!=mover.Id&&other.Traffic!=null&&other.Traffic.Yielding&&other.Traffic.Progress<1)
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
                if(sweep.Planet==planet.Id&&sweep.Id!=mover.Id&&GroundSegmentDistance(mover.Position,destination,sweep.From,sweep.To)<mover.Radius+sweep.Radius-GroundTrafficTolerance)return false;
            return true;
        }
        private void RecordGroundMove(PlanetState planet,GroundAgent mover,SimPoint destination)
        {PrepareGroundSweeps();groundSweeps.Add(new GroundSweep{Planet=planet.Id,Id=mover.Id,Radius=mover.Radius,From=mover.Position,To=destination});if(mover.Traffic!=null)mover.Traffic.WaitTicks=0;}
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
                if(other.Id!=mover.Id&&other.Traffic!=null&&other.Traffic.Yielding)
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
            return Inside(planet,x,y)&&!PlanetEnvironment.BlocksGround(planet.Terrain[y*planet.Width+x])&&!planet.Buildings.Exists(b=>!b.Destroyed&&b.X==x&&b.Y==y&&!((b.Built||rejoining)&&(b.Id==agent.Shipment.SourceId||b.Id==agent.Shipment.TargetId)))&&!LandingBlocksGround(planet,x,y);
        }
        private bool CourierYieldReachable(PlanetState planet,GroundAgent agent,GridCell cell)
        {
            if(agent.Shipment==null)return true;
            var shipment=agent.Shipment;bool returning=shipment.Phase==ShipmentPhase.Returning||shipment.Phase==ShipmentPhase.Recovering||shipment.Phase==ShipmentPhase.BuilderReturning;
            var target=planet.Buildings.Find(b=>b.Id==(returning?shipment.SourceId:shipment.TargetId));
            return FindPath(planet,new BuildingState{X=cell.X,Y=cell.Y},target)!=null;
        }
        private bool YieldSegmentLegal(PlanetState planet,GroundAgent agent,SimPoint to)
        {
            int samples=Math.Max(1,(int)Math.Ceiling(Distance(agent.Position.X,agent.Position.Y,to.X,to.Y)/.15));
            var start=HexGrid.FromWorld(agent.Position.X,agent.Position.Y);
            for(int i=1;i<=samples;i++)
            {var point=GroundLerp(agent.Position,to,i/(double)samples);var cell=HexGrid.FromWorld(point.X,point.Y);if(cell.X==start.X&&cell.Y==start.Y)continue;if(!YieldCellLegal(planet,agent,cell.X,cell.Y))return false;}
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
                double speed=agent.Unit!=null?Definitions.Get(agent.Unit.Kind).Speed:Definitions.CourierSpeed;
                double progress=Math.Min(1,traffic.Progress+speed*Definitions.TickSeconds/Math.Max(.001,length));
                var next=GroundLerp(new SimPoint(traffic.StartX,traffic.StartY),finish,progress);GroundAgent blocker;
                if(!YieldSegmentLegal(planet,agent,next))
                {StartYield(planet,agent,agent,new HashSet<long>(),0);return true;}
                if(!GroundMoveFree(planet,agent,next,out blocker))
                {if(blocker!=null&&blocker.Owner==agent.Owner)StartYield(planet,agent,blocker,new HashSet<long>{agent.Id},0);return true;}
                RecordGroundMove(planet,agent,next);traffic.Progress=progress;
                if(agent.Unit!=null){SetGroundUnitPosition(agent.Unit,next);agent.Unit.Status="Macht eigener Einheit Platz";}
                else if(progress>=1&&planet.Buildings.Exists(b=>(b.Id==agent.Shipment.SourceId||b.Id==agent.Shipment.TargetId)&&b.Built&&b.X==traffic.GoalX&&b.Y==traffic.GoalY))traffic.Docked=true;
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
            if(requester!=null&&requester.Id!=agent.Id&&GroundIntendsTravel(planet,requester))
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
            {agent.Unit.Path.Clear();agent.Unit.PathDistance=0;agent.Unit.Status="Setzt ursprünglichen Auftrag fort";}
            else
            {
                var shipment=agent.Shipment;bool returning=shipment.Phase==ShipmentPhase.Returning||shipment.Phase==ShipmentPhase.Recovering||shipment.Phase==ShipmentPhase.BuilderReturning;
                var destination=planet.Buildings.Find(b=>b.Id==(returning?shipment.SourceId:shipment.TargetId));
                var path=FindPath(planet,new BuildingState{X=traffic.GoalX,Y=traffic.GoalY},destination);if(path==null)return true;
                shipment.Path=path;shipment.DistanceTravelled=0;
                shipment.RoadEntryCentered=!traffic.Docked;
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
