using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        private string DispatchTrade(TreatyState t,SimCommand c)
        {
            if(!TreatyLive(t)||t.Terms.Kind!=TreatyKind.Trade)return "Aktiver Handelsvertrag benötigt.";
            var ship=ActiveShip(c.ActorId);bool payment=c.IssuerId!=t.Proposer;
            var source=FindBuilding(payment?t.Terms.TargetPort:t.Terms.SourcePort);var target=FindBuilding(payment?t.Terms.SourcePort:t.Terms.TargetPort);
            int r=(int)(payment?t.Terms.Payment:t.Terms.Goods),remaining=(payment?t.Terms.PaymentAmount-t.PaymentDelivered:t.Terms.Amount-t.Delivered);
            foreach(var s in State.Ships)if(s.Freight!=null&&s.Freight.TreatyId==t.Id&&s.Freight.PaymentLeg==payment)remaining-=s.Freight.Amount;
            if(ship==null||ship.OwnerId!=c.IssuerId||ship.Kind!=ShipKind.Freighter||ship.Freight!=null||Total(ship.Cargo)>0||ShipPledged(ship.Id)||!PortOwned(source,c.IssuerId)||!PortOwned(target,payment?t.Proposer:t.Terms.Members.Find(f=>f!=t.Proposer)))return "Freier eigener Frachter und vereinbarte unveränderte Häfen benötigt.";
            int amount=Math.Min(remaining,Math.Min(Definitions.Ships[0].Capacity,Math.Min(source.Inventory[r]-source.ReservedOutgoing[r],FreeSpace(target))));
            if(amount<=0){t.Reason="Lieferung wartet: freier Bestand oder Zielplatz fehlt, oder Menge bereits unterwegs";return t.Reason;}
            source.ReservedOutgoing[r]+=amount;target.ReservedIncoming[r]+=amount;ship.AutoSupply=false;foreach(var route in State.Routes)if(route.ShipId==ship.Id)route.Enabled=false;
            ship.Freight=new FreightJob {TreatyId=t.Id,PaymentLeg=payment,SourceId=source.Id,TargetId=target.Id,Resource=(ResourceKind)r,Amount=amount};ship.Order=ShipOrder.Freight;t.Reason="Reservierte Ware wird physisch geliefert; bis Ankunft Eigentum des Lieferanten";return null;
        }
        private bool TradePortValid(FreightJob job,BuildingState port,bool source)
        {
            var t=State.Treaties.Find(v=>v.Id==job.TreatyId&&TreatyLive(v)&&v.Terms.Kind==TreatyKind.Trade);if(t==null)return false;
            int other=t.Terms.Members.Find(f=>f!=t.Proposer);int owner=source?(job.PaymentLeg?other:t.Proposer):(job.PaymentLeg?t.Proposer:other);
            return PortOwned(port,owner);
        }
        private void CreditTrade(FreightJob job)
        {
            var t=State.Treaties.Find(v=>v.Id==job.TreatyId);if(job.PaymentLeg)t.PaymentDelivered+=job.Amount;else t.Delivered+=job.Amount;
            t.Reason="Geliefert "+t.Delivered+" / "+t.Terms.Amount+"; Gegenleistung "+t.PaymentDelivered+" / "+t.Terms.PaymentAmount;
            if(t.Delivered==t.Terms.Amount&&t.PaymentDelivered==t.Terms.PaymentAmount)
            {foreach(int f in t.Terms.Members)DiplomaticEvent(t,f,t.Terms.Members.Find(a=>a!=f),6,"delivered","Handel vollständig erfüllt");}
        }
        public bool ShipPledged(long id) {return State.Treaties.Exists(t=>TreatyLive(t)&&t.Terms.Kind==TreatyKind.JointOperation&&t.OperationPhase!=OperationPhase.Completed&&t.OperationPhase!=OperationPhase.Cancelled&&t.Pledges.Exists(p=>p.ShipId==id));}
        private string PledgeJoint(TreatyState t,SimCommand c)
        {
            var ship=ActiveShip(c.ActorId);
            if(!TreatyLive(t)||t.Terms.Kind!=TreatyKind.JointOperation||t.OperationPhase>=OperationPhase.Executing||ship==null||ship.OwnerId!=c.IssuerId||ship.Kind==ShipKind.OrbitalYard||ship.Freight!=null||ShipPledged(ship.Id)||State.ComputerFactions.Exists(a=>a.Operation.Ships.Contains(ship.Id)))return "Freies eigenes Schiff ohne konkurrierende Zusage benötigt.";
            if(t.Terms.Goal!=JointGoal.Invade&&Definitions.SpaceWeapons[(int)ship.Kind].Damage==0)return "Schutzauftrag benötigt bewaffnete Eskorte.";
            t.Pledges.Add(new JointPledge {FactionId=c.IssuerId,ShipId=ship.Id,ReadyTick=State.Tick+40});t.OperationPhase=OperationPhase.Preparing;t.Reason="Schiff #"+ship.Id+" exklusiv zugesagt; frühestens in zwei Sekunden bereit";return null;
        }
        private string StartJoint(TreatyState t)
        {
            if(!TreatyLive(t)||t.Terms.Kind!=TreatyKind.JointOperation||t.OperationPhase!=OperationPhase.Preparing)return "Vorbereitete gemeinsame Operation benötigt.";
            string error=TermsError(t.Terms,t.Proposer);if(error!=null)return error;
            foreach(int f in t.Terms.Members)if(!t.Pledges.Exists(p=>p.FactionId==f&&p.ReadyTick<=State.Tick&&ActiveShip(p.ShipId)!=null))return "Jeder Teilnehmer benötigt einen tatsächlich verfügbaren Beitrag.";
            if(t.Terms.Goal==JointGoal.Invade)
            {
                foreach(int f in t.Terms.Members)
                {
                    var carrier=t.Pledges.Find(p=>p.FactionId==f&&ActiveShip(p.ShipId)!=null&&ActiveShip(p.ShipId).Kind==ShipKind.LandingTransport);
                    if(carrier==null)return "Jeder Invasionsteilnehmer benötigt einen beladenen Träger.";
                    var s=ActiveShip(carrier.ShipId);if(s.Troops.Count<3||!s.Troops.Exists(u=>u.Kind==UnitKind.Engineer)||s.Cargo[0]<8||s.Cargo[3]<8||!t.Pledges.Exists(p=>p.FactionId==f&&ActiveShip(p.ShipId)!=null&&Definitions.SpaceWeapons[(int)ActiveShip(p.ShipId).Kind].Damage>0))return "Drei Truppen mit Pionier, acht Nahrung/Bauteile und bewaffnete Eskorte je Teilnehmer benötigt.";
                }
            }
            t.OperationPhase=OperationPhase.Executing;t.Reason="Zugesagte Kräfte unterwegs; Kontrolle bleibt bei ihren Eigentümern";return null;
        }
        private void ReleaseJoint(TreatyState t,string reason)
        {
            foreach(var pledge in t.Pledges)
            {
                var s=ActiveShip(pledge.ShipId);if(s==null)continue;var home=State.Planets.Find(p=>p.OwnerId==s.OwnerId&&p.Buildings.Exists(b=>PortOwned(b,s.OwnerId)));
                if(home!=null&&s.TransportPhase==TransportPhase.Space){s.TargetPlanetId=home.Id;s.Order=ShipOrder.Retreat;}
                s.Status=reason;
            }
            t.Pledges.Clear();t.OperationPhase=OperationPhase.Cancelled;t.Reason=reason;
        }
        private void JointCommand(int owner,SimCommand command) {command.Id=NextCommandId();command.IssuerId=owner;Enqueue(command);}
        private void TickJointOperations()
        {
            foreach(var t in State.Treaties)
            {
                if(!TreatyLive(t)||t.Terms.Kind!=TreatyKind.JointOperation||t.OperationPhase!=OperationPhase.Executing)continue;
                var world=FindPlanet(t.Terms.PlanetId);
                if(t.Pledges.Exists(p=>ActiveShip(p.ShipId)==null)){ReleaseJoint(t,"Zusage geändert: Schiff verloren; Rückzug und neue Vorbereitung erforderlich");continue;}
                if(t.Terms.Goal==JointGoal.Invade&&world.OwnerId==t.Terms.FutureOwner){t.OperationPhase=OperationPhase.Completed;t.Reason="Gemeinsam gesichert; vereinbarter Eigentümer übernimmt";t.Pledges.Clear();foreach(int guest in t.Terms.Members)if(guest!=world.OwnerId&&!HasAccess(guest,world.OwnerId,world.Id,AccessRight.Stationing))BeginEvacuation(guest,world.OwnerId,world);continue;}
                if(TermsError(t.Terms,t.Proposer)!=null){ReleaseJoint(t,"Rechte oder Voraussetzungen verändert; Operation abgebrochen");continue;}
                if(State.Tick%40!=0)continue;
                foreach(var pledge in t.Pledges)
                {
                    var ship=ActiveShip(pledge.ShipId);var pos=GetOrbitPosition(world.Id);
                    if(t.Terms.Goal==JointGoal.Escort)
                    {if(ship.Order!=ShipOrder.Escort||ship.EscortId!=t.Terms.EscortTarget)JointCommand(ship.OwnerId,new SimCommand{Kind=CommandKind.EscortShip,PlanetId=world.Id,ActorId=ship.Id,TargetId=t.Terms.EscortTarget});pledge.Arrived=Distance(ship.X,ship.Y,pos)<=Definitions.OrbitalRadius;continue;}
                    if(ship.TransportPhase==TransportPhase.Space&&Distance(ship.X,ship.Y,pos)>Definitions.OrbitalRadius)
                    {if(ship.Order!=ShipOrder.Fly||ship.TargetPlanetId!=world.Id)JointCommand(ship.OwnerId,new SimCommand{Kind=CommandKind.FlyShip,PlanetId=world.Id,ActorId=ship.Id});continue;}
                    pledge.Arrived=true;
                    if(t.Terms.Goal!=JointGoal.Invade){if(ship.Order!=ShipOrder.Guard)JointCommand(ship.OwnerId,new SimCommand{Kind=CommandKind.GuardPlanet,PlanetId=world.Id,ActorId=ship.Id});continue;}
                    if(ship.Kind!=ShipKind.LandingTransport)continue;
                    if(ship.TransportPhase==TransportPhase.Space)
                    {
                        bool found=false;for(int y=1;y<world.Height-1&&!found;y++)for(int x=1;x<world.Width-1&&!found;x++)if(LandingError(ship,world,x,y)==null){JointCommand(ship.OwnerId,new SimCommand{Kind=CommandKind.LandTransport,PlanetId=world.Id,ActorId=ship.Id,X=x,Y=y});found=true;}
                        if(!found)t.Reason="Landung wartet auf geeignete freie Landezone";
                    }
                    else if(ship.TransportPhase==TransportPhase.Landed&&ship.Troops.Count>0)JointCommand(ship.OwnerId,new SimCommand{Kind=CommandKind.UnloadTroop,PlanetId=world.Id,ActorId=ship.Id,TargetId=ship.Troops[0].Id});
                    else if(ship.TransportPhase==TransportPhase.Landed)JointGroundOrders(t,world,ship);
                }
            }
        }
        private void JointGroundOrders(TreatyState t,PlanetState p,ShipState carrier)
        {
            var h=State.Beachheads.Find(b=>!b.Destroyed&&b.PlanetId==p.Id&&b.OwnerId==carrier.OwnerId);
            var engineer=p.Units.Find(u=>!u.Destroyed&&u.OwnerId==carrier.OwnerId&&u.Kind==UnitKind.Engineer&&HexGrid.Distance(u.X,u.Y,carrier.LandingX,carrier.LandingY)<=3);
            if(h==null&&engineer!=null&&engineer.Parts>=4)
            {var cell=HexGrid.CellAt(engineer.X,engineer.Y);JointCommand(carrier.OwnerId,new SimCommand{Kind=CommandKind.EstablishBeachhead,PlanetId=p.Id,ActorId=engineer.Id,X=cell.X,Y=cell.Y});return;}
            foreach(var u in p.Units)if(!u.Destroyed&&u.OwnerId==carrier.OwnerId&&u.Kind==UnitKind.Infantry)
            {
                var target=p.Buildings.Find(b=>!b.Destroyed&&b.Built&&AtWar(u.OwnerId,BuildingOwner(p,b))&&CanSeeEntity(p,u.OwnerId,b.Id)&&b.Kind==BuildingKind.Headquarters);
                if(target==null)
                {if(u.Order!=UnitOrder.Move)foreach(var cell in HexGrid.LandingCells(p.Width/2,p.Height/2))if(CanTraverse(p,u.OwnerId,u.Kind,cell.X,cell.Y)){JointCommand(u.OwnerId,new SimCommand{Kind=CommandKind.MoveUnit,PlanetId=p.Id,ActorId=u.Id,X=cell.X,Y=cell.Y});break;}continue;}
                if(HexGrid.Distance(u.X,u.Y,target.X,target.Y)<=1.5){if(target.CapturingFaction==0||!t.Terms.Members.Contains(target.CapturingFaction))JointCommand(u.OwnerId,new SimCommand{Kind=CommandKind.CaptureBuilding,PlanetId=p.Id,ActorId=u.Id,TargetId=target.Id});}
                else if(u.Order!=UnitOrder.Move)foreach(var cell in HexGrid.LandingCells(target.X,target.Y))if(CanTraverse(p,u.OwnerId,u.Kind,cell.X,cell.Y)){JointCommand(u.OwnerId,new SimCommand{Kind=CommandKind.MoveUnit,PlanetId=p.Id,ActorId=u.Id,X=cell.X,Y=cell.Y});break;}
            }
        }
        private int CaptureOwner(int actor,int planet)
        {var t=State.Treaties.Find(v=>TreatyLive(v)&&v.Terms.Kind==TreatyKind.JointOperation&&v.Terms.Goal==JointGoal.Invade&&v.OperationPhase==OperationPhase.Executing&&v.Terms.PlanetId==planet&&v.Terms.Members.Contains(actor));return t==null?actor:t.Terms.FutureOwner;}
    }
}
