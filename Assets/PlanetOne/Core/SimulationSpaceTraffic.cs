using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public static partial class Definitions
    {
        public static double ShipRadius(ShipKind kind)
        {
            return kind==ShipKind.OrbitalYard?.46:
                kind==ShipKind.ColonyShip || kind==ShipKind.LandingTransport || kind==ShipKind.EscortFrigate?.34:
                kind==ShipKind.TorpedoShip?.32:kind==ShipKind.Scout || kind==ShipKind.SurveyShip?.28:.30;
        }
        public const double SpaceTrafficGap=.035;
    }

    public sealed partial class GameSimulation
    {
        private readonly Dictionary<long,SimPoint> spaceTickStarts=new Dictionary<long,SimPoint>();
        private readonly HashSet<long> spaceMoved=new HashSet<long>();
        private long spaceTrafficTick=-1;
        private static bool OccupiesSpace(ShipState s)
        {return !s.Destroyed && !s.Consumed && s.TransportPhase==TransportPhase.Space;}
        private static double ShipSeparation(ShipState a,ShipState b)
        {return Definitions.ShipRadius(a.Kind)+Definitions.ShipRadius(b.Kind)+Definitions.SpaceTrafficGap;}
        private void ResetSpaceDestination(ShipState ship)
        {ship.TrafficGoalSet=false;ship.TrafficWaitTicks=0;ship.TrafficYieldUntil=0;ship.TrafficYieldFor=0;}

        private void BeginSpaceTrafficTick()
        {
            spaceTrafficTick=State.Tick;spaceTickStarts.Clear();spaceMoved.Clear();
            foreach(var s in State.Ships)if(OccupiesSpace(s))spaceTickStarts[s.Id]=new SimPoint(s.X,s.Y);
            // Anchored stations retain a stable offset; orbital movement does not reassign their parking slot.
            foreach(var s in State.Ships)if(OccupiesSpace(s) && s.Kind==ShipKind.OrbitalYard)
                UpdateYardTrafficAnchor(s);
            foreach(var s in State.Ships)
            {
                if(!OccupiesSpace(s) || Definitions.Ships[(int)s.Kind].Speed<=0)continue;
                if(s.TrafficYieldUntil>State.Tick)
                {
                    if(!State.Ships.Exists(v=>v.Id==s.TrafficYieldFor && OccupiesSpace(v)))s.TrafficYieldUntil=0;
                    else {AdvanceSpaceTraffic(s,new SimPoint(s.TrafficYieldX,s.TrafficYieldY),false);continue;}
                }
                // Legacy saves may already contain overlapping ships. Separate by ordinary bounded movement,
                // never by relocating their saved positions or deleting their original mission.
                ShipState overlap=null;
                foreach(var other in State.Ships)if(other.Id!=s.Id && OccupiesSpace(other) && Distance(s.X,s.Y,other.X,other.Y)<ShipSeparation(s,other)-.000001){overlap=other;break;}
                if(overlap!=null)
                {
                    double dx=s.X-overlap.X,dy=s.Y-overlap.Y,length=Math.Sqrt(dx*dx+dy*dy);
                    if(length<.00001){double angle=(s.Id%37)*2.399963229728653;dx=Math.Cos(angle);dy=Math.Sin(angle);length=1;}
                    AdvanceSpaceTraffic(s,new SimPoint(s.X+dx/length,s.Y+dy/length),false);
                }
            }
        }

        private void UpdateYardTrafficAnchor(ShipState yard)
        {
            var orbit=GetOrbitPosition(yard.TargetPlanetId);
            if(!yard.TrafficGoalSet){yard.TrafficGoalSet=true;yard.TrafficOffsetX=yard.X-orbit.X;yard.TrafficOffsetY=yard.Y-orbit.Y;}
            yard.TrafficAnchorX=orbit.X;yard.TrafficAnchorY=orbit.Y;
            if(spaceMoved.Contains(yard.Id))return;
            var target=new SimPoint(orbit.X+yard.TrafficOffsetX,orbit.Y+yard.TrafficOffsetY);
            double distance=Distance(yard.X,yard.Y,target),step=Math.Min(distance,.15);
            if(distance<.000001)return;
            var next=new SimPoint(yard.X+(target.X-yard.X)/distance*step,yard.Y+(target.Y-yard.Y)/distance*step);ShipState blocker;
            if(SpaceSegmentFree(yard,next,out blocker)){yard.X=next.X;yard.Y=next.Y;spaceMoved.Add(yard.Id);}
            else if(blocker!=null && blocker.OwnerId==yard.OwnerId)RequestSpaceYield(yard,blocker,target,new HashSet<long>(),0);
        }

        private bool SpacePointFree(ShipState ship,SimPoint point,bool reservations,bool allPhysical)
        {
            foreach(var other in State.Ships)
            {
                if(other.Id==ship.Id || !OccupiesSpace(other))continue;
                bool known=other.OwnerId==ship.OwnerId || IsShipVisible(ship.OwnerId,other);
                if(!known && !allPhysical)continue;
                double separation=ShipSeparation(ship,other);
                if(Distance(point.X,point.Y,other.X,other.Y)<separation)return false;
                if(reservations && other.OwnerId==ship.OwnerId && other.TrafficGoalSet && other.Order!=ShipOrder.Hold && other.Order!=ShipOrder.Bombard && other.TrafficYieldUntil<=State.Tick &&
                    Distance(point.X,point.Y,other.TrafficAnchorX+other.TrafficOffsetX,other.TrafficAnchorY+other.TrafficOffsetY)<separation)return false;
                if(reservations && other.OwnerId==ship.OwnerId && other.TrafficYieldUntil>State.Tick &&
                    Distance(point.X,point.Y,other.TrafficYieldX,other.TrafficYieldY)<separation)return false;
            }
            return true;
        }

        private bool FindSpacePosition(ShipState ship,SimPoint anchor,double extent,bool reservations,bool allPhysical,out SimPoint result)
        {
            if(SpacePointFree(ship,anchor,reservations,allPhysical)){result=anchor;return true;}
            // Stable rings of candidates; choose the nearest approach on the first available ring.
            for(int ring=1;ring<=Math.Ceiling(extent/.22);ring++)
            {
                double radius=ring*.22,best=double.MaxValue;bool found=false;SimPoint chosen=anchor;
                int samples=Math.Max(12,(int)Math.Ceiling(2*Math.PI*radius/.32));
                for(int i=0;i<samples;i++)
                {
                    double a=i*Math.PI*2/samples;var candidate=new SimPoint(anchor.X+Math.Cos(a)*radius,anchor.Y+Math.Sin(a)*radius);
                    if(!SpacePointFree(ship,candidate,reservations,allPhysical))continue;
                    double distance=Distance(ship.X,ship.Y,candidate);
                    if(distance<best){best=distance;chosen=candidate;found=true;}
                }
                if(found){result=chosen;return true;}
            }
            result=anchor;return false;
        }

        private bool SpaceArrivalPoint(ShipState ship,SimPoint anchor,out SimPoint target)
        {
            var reserved=new SimPoint(anchor.X+ship.TrafficOffsetX,anchor.Y+ship.TrafficOffsetY);
            bool occupiedAfterReservation=ship.TrafficGoalSet && ship.TrafficWaitTicks>=12 && !SpacePointFree(ship,reserved,false,false);
            if(!ship.TrafficGoalSet || Distance(ship.TrafficAnchorX,ship.TrafficAnchorY,anchor)>2 || occupiedAfterReservation)
            {
                double extent=State.Planets.Exists(p=>Distance(anchor.X,anchor.Y,GetOrbitPosition(p.Id))<.01)?2.4:4.4;
                if(!FindSpacePosition(ship,anchor,extent,true,false,out target))return false;
                ship.TrafficOffsetX=target.X-anchor.X;ship.TrafficOffsetY=target.Y-anchor.Y;ship.TrafficGoalSet=true;
                ship.TrafficWaitTicks=0;
            }
            ship.TrafficAnchorX=anchor.X;ship.TrafficAnchorY=anchor.Y;
            target=new SimPoint(anchor.X+ship.TrafficOffsetX,anchor.Y+ship.TrafficOffsetY);return true;
        }

        public bool AtPlanetDock(ShipState ship,int planetId)
        {
            return AtSpaceDock(ship,GetOrbitPosition(planetId));
        }
        public static bool AtSpaceDock(ShipState ship,SimPoint anchor)
        {
            if(ship==null || !OccupiesSpace(ship))return false;
            if(Distance(ship.X,ship.Y,anchor)<=Definitions.DockDistance)return true;
            return ship.TrafficGoalSet && ship.TrafficOffsetX*ship.TrafficOffsetX+ship.TrafficOffsetY*ship.TrafficOffsetY<=2.65*2.65 && Distance(ship.TrafficAnchorX,ship.TrafficAnchorY,anchor)<=.1 &&
                Distance(ship.X,ship.Y,new SimPoint(anchor.X+ship.TrafficOffsetX,anchor.Y+ship.TrafficOffsetY))<=Definitions.DockDistance;
        }

        internal bool TrySpaceSpawn(ShipKind kind,int owner,SimPoint anchor,out SimPoint position)
        {
            var probe=new ShipState {Id=State.NextEntityId,Kind=kind,OwnerId=owner,X=anchor.X,Y=anchor.Y};
            return FindSpacePosition(probe,anchor,2.4,false,true,out position);
        }

        private bool SpaceTransitAllowed(ShipState ship,SimPoint next)
        {
            foreach(var p in State.Planets)if(p.OwnerId>0 && p.OwnerId!=ship.OwnerId && !AtWar(ship.OwnerId,p.OwnerId) && IsPlanetVisible(ship.OwnerId,p.Id))
            {
                var orbit=GetOrbitPosition(p.Id);double before=Distance(ship.X,ship.Y,orbit),after=Distance(next.X,next.Y,orbit);
                bool trade=ship.Freight!=null && ship.Freight.TreatyId>0 && TradePortValid(ship.Freight,FindBuilding(ship.Freight.TargetId),false) && FindBuilding(ship.Freight.TargetId).PlanetId==p.Id;
                bool operation=State.Treaties.Exists(t=>TreatyLive(t) && t.Terms.Kind==TreatyKind.JointOperation && t.Terms.PlanetId==p.Id && t.Pledges.Exists(a=>a.ShipId==ship.Id));
                var right=ship.Kind<=ShipKind.SurveyShip?AccessRight.CivilTransit:AccessRight.MilitaryTransit;
                // A moon's diplomatic exclusion zone must not encompass its parent port.
                // Weapon/landing range is a separate rule from peaceful transit protection.
                double protectedRadius=p.ParentId>=0?Math.Min(Definitions.OrbitalRadius,p.OrbitRadius*.4):Definitions.OrbitalRadius;
                if(after<protectedRadius && after<before-.000001 && !trade && !operation && !HasAccess(ship.OwnerId,p.OwnerId,p.Id,right) && !(ship.Order==ShipOrder.Repair && HasAccess(ship.OwnerId,p.OwnerId,p.Id,AccessRight.Repair)))return false;
            }
            return true;
        }

        private bool SpaceSegmentFree(ShipState ship,SimPoint next,out ShipState blocking)
        {
            blocking=null;SimPoint ownStart;
            if(!spaceTickStarts.TryGetValue(ship.Id,out ownStart))ownStart=new SimPoint(ship.X,ship.Y);
            foreach(var other in State.Ships)
            {
                if(other.Id==ship.Id || !OccupiesSpace(other))continue;
                SimPoint otherStart;if(!spaceTickStarts.TryGetValue(other.Id,out otherStart))otherStart=new SimPoint(other.X,other.Y);
                double rx=ownStart.X-otherStart.X,ry=ownStart.Y-otherStart.Y;
                double vx=(next.X-ownStart.X)-(other.X-otherStart.X),vy=(next.Y-ownStart.Y)-(other.Y-otherStart.Y);
                double speed=vx*vx+vy*vy,t=speed>1e-16?Math.Max(0,Math.Min(1,-(rx*vx+ry*vy)/speed)):0;
                double minX=rx+t*vx,minY=ry+t*vy,separation=ShipSeparation(ship,other),start2=rx*rx+ry*ry,end2=(rx+vx)*(rx+vx)+(ry+vy)*(ry+vy);
                if(start2<separation*separation-.000001)
                {
                    if(end2>start2+.00000001 && minX*minX+minY*minY>=start2-.00000001)continue;
                }
                else if(minX*minX+minY*minY>=separation*separation-.00000001)continue;
                blocking=other;return false;
            }
            return true;
        }

        private bool AdvanceSpaceTraffic(ShipState ship,SimPoint target,bool cooperate)
        {
            if(spaceMoved.Contains(ship.Id))return Distance(ship.X,ship.Y,target)<.015;
            double distance=Distance(ship.X,ship.Y,target),limit=Definitions.Ships[(int)ship.Kind].Speed*Definitions.TickSeconds;
            if(distance<.000001)return true;if(limit<=0)return false;
            double step=Math.Min(distance,limit),dx=(target.X-ship.X)/distance,dy=(target.Y-ship.Y)/distance;
            var direct=new SimPoint(ship.X+dx*step,ship.Y+dy*step);ShipState blocker=null;
            bool directAllowed=SpaceTransitAllowed(ship,direct);
            if(directAllowed&&SpaceSegmentFree(ship,direct,out blocker))
            {ship.X=direct.X;ship.Y=direct.Y;spaceMoved.Add(ship.Id);ship.TrafficWaitTicks=0;return distance<=limit;}
            ship.TrafficWaitTicks++;
            // Steer right consistently in head-on encounters. Planning uses only observable neighbours;
            // the final swept segment always checks every physical ship, including hidden opponents.
            double[] angles={.35,-.35,.7,-.7,1.05,-1.05,1.4,-1.4,1.75,-1.75,2.1,-2.1};
            foreach(double angle in angles)
            {
                double cos=Math.Cos(angle),sin=Math.Sin(angle);var candidate=new SimPoint(ship.X+(dx*cos-dy*sin)*step,ship.Y+(dx*sin+dy*cos)*step);
                if(!SpaceTransitAllowed(ship,candidate))continue;
                var ahead=new SimPoint(ship.X+(dx*cos-dy*sin)*Math.Min(distance,1.2),ship.Y+(dx*sin+dy*cos)*Math.Min(distance,1.2));
                ShipState aheadBlocker;if(!SpaceSegmentFree(ship,ahead,out aheadBlocker))continue;
                ShipState ignored;if(!SpaceSegmentFree(ship,candidate,out ignored))continue;
                ship.X=candidate.X;ship.Y=candidate.Y;spaceMoved.Add(ship.Id);ship.Status=directAllowed?"Weicht im Flugkorridor aus":"Geschützter Orbit: Transitrecht fehlt; umfliegt Grenze";return false;
            }
            if(cooperate && blocker!=null && blocker.OwnerId==ship.OwnerId && blocker.Kind!=ShipKind.OrbitalYard)
                RequestSpaceYield(ship,blocker,target,new HashSet<long>(),0);
            ship.Status=!directAllowed?"Geschützter Orbit: Transitrecht fehlt; sucht erlaubten Flugkorridor":ship.TrafficYieldUntil>State.Tick?"Macht Durchfahrt frei; Ausweichweg noch belegt":"Flugkorridor belegt; wartet auf Durchfahrt";return false;
        }

        private void RequestSpaceYield(ShipState requester,ShipState blocker,SimPoint destination,HashSet<long> visited,int depth)
        {
            if(depth>5 || !visited.Add(blocker.Id) || blocker.OwnerId!=requester.OwnerId || !OccupiesSpace(blocker) || blocker.Kind==ShipKind.OrbitalYard || blocker.TrafficYieldUntil>State.Tick)return;
            bool stationary=blocker.Order==ShipOrder.Hold || blocker.Order==ShipOrder.Guard;
            if(!stationary && requester.Id>blocker.Id)return;
            double dx=destination.X-requester.X,dy=destination.Y-requester.Y,length=Math.Sqrt(dx*dx+dy*dy);
            if(length<.001){dx=1;dy=0;length=1;}dx/=length;dy/=length;
            for(int ring=0;ring<3;ring++)for(int i=0;i<12;i++)
            {
                double angle=(i%2==0?1:-1)*(Math.PI*.5+(i/2)*Math.PI/6),r=1+ring*.55;
                var candidate=new SimPoint(blocker.X+(dx*Math.Cos(angle)-dy*Math.Sin(angle))*r,blocker.Y+(dx*Math.Sin(angle)+dy*Math.Cos(angle))*r);
                if(!SpacePointFree(blocker,candidate,true,false) || !SpaceTransitAllowed(blocker,candidate))continue;
                blocker.TrafficYieldX=candidate.X;blocker.TrafficYieldY=candidate.Y;blocker.TrafficYieldFor=requester.Id;blocker.TrafficYieldUntil=State.Tick+60;
                blocker.Status="Macht einem eigenen Schiff Platz";
                // A neighbouring stationary ship may need to open the path to the free pocket first.
                double d=Distance(blocker.X,blocker.Y,candidate),step=Math.Min(d,Definitions.Ships[(int)blocker.Kind].Speed*Definitions.TickSeconds);
                var next=new SimPoint(blocker.X+(candidate.X-blocker.X)/d*step,blocker.Y+(candidate.Y-blocker.Y)/d*step);ShipState nextBlocker;
                if(!SpaceSegmentFree(blocker,next,out nextBlocker) && nextBlocker!=null && nextBlocker.Id!=requester.Id)
                    RequestSpaceYield(blocker,nextBlocker,candidate,visited,depth+1);
                return;
            }
        }

        private bool MoveShipWithTraffic(ShipState ship,SimPoint anchor)
        {
            if(spaceTrafficTick!=State.Tick)BeginSpaceTrafficTick();
            if(ship.TrafficYieldUntil>State.Tick)return false;
            SimPoint target;
            if(!SpaceArrivalPoint(ship,anchor,out target)){ship.Status="Ankunftsbereich belegt; wartet auf freien Platz";return false;}
            return AdvanceSpaceTraffic(ship,target,true);
        }
    }
}
