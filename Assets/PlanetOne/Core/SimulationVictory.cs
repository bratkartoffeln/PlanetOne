using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        private List<int> GateParticipants(int owner) {var t=CoalitionOf(owner);var result=t==null?new List<int>{owner}:new List<int>(t.Terms.Members);result.Sort();return result;}
        private string GateError(BuildingState b,int owner)
        {
            if(b==null||b.Kind!=BuildingKind.JumpGate||b.Destroyed||!b.Built||b.Deconstructing||BuildingOwner(FindPlanet(b.PlanetId),b)!=owner||FindPlanet(b.PlanetId).OwnerId!=owner||!FactionActive(owner))return "Fertiges eigenes Sprungtor auf kontrollierter Kolonie benötigt.";
            foreach(var tech in Definitions.GateResearch)if(!HasTechnology(owner,tech))return "Betreiber benötigt Elektronik und fortgeschrittene Sensoren.";
            if(!b.Enabled||!b.Operational||b.PowerBlocked||FindPlanet(b.PlanetId).FoodShortageSeconds>Definitions.FoodShortageGraceSeconds)return "Betrieb, Energie, Arbeitskräfte und Nahrung sichern.";
            if(EnemyNear(FindPlanet(b.PlanetId),owner,b.X,b.Y,Definitions.CaptureRadius))return "Torstandort militärisch nicht gesichert.";
            return null;
        }
        private string ActivateGate(SimCommand c)
        {
            if(State.Winners.Count>0)return "Das Szenario ist bereits entschieden. Eine neue Partie startet ein neues Torprojekt.";
            var b=FindBuilding(c.TargetId);string error=GateError(b,c.IssuerId);if(error!=null)return error;
            var gate=State.GateProjects.Find(g=>g.BuildingId==b.Id);
            if(gate!=null&&(gate.Activating||gate.Complete))return "Tor ist bereits aktiv oder abgeschlossen.";
            for(int r=0;r<6;r++)if(b.Inventory[r]-b.ReservedOutgoing[r]<Definitions.GateComponents[r])return "Alle Projektkomponenten müssen physisch am Tor eingetroffen sein.";
            if(gate==null){gate=new GateProject{BuildingId=b.Id};State.GateProjects.Add(gate);}
            gate.OwnerId=c.IssuerId;gate.Participants=GateParticipants(c.IssuerId);gate.Activating=true;gate.SecureTicks=0;gate.Reason="Öffentliche Sicherungsphase läuft";
            foreach(var f in State.Factions)Emit(f.Id,b.PlanetId,b.Id,"GateActivation","Sprungtor auf "+FindPlanet(b.PlanetId).Name+": Betreiber "+c.IssuerId+", Teilnehmer "+string.Join(",",gate.Participants)+", Sicherung "+Definitions.GateSecureTicks*Definitions.TickSeconds+" Sekunden");
            return null;
        }
        private void TickGateProjects()
        {
            if(State.Winners.Count>0)return;
            foreach(var gate in State.GateProjects)
            {
                if(!gate.Activating||gate.Complete)continue;
                var b=FindBuilding(gate.BuildingId);string error=GateError(b,gate.OwnerId);var participants=GateParticipants(gate.OwnerId);
                if(error==null&&(participants.Count!=gate.Participants.Count||participants.Exists(f=>!gate.Participants.Contains(f)||!FactionActive(f))))error="Teilnehmerliste geändert; Sicherung neu beginnen";
                if(error==null)for(int r=0;r<6;r++)if(b.Inventory[r]-b.ReservedOutgoing[r]<Definitions.GateComponents[r]){error="Projektkomponenten fehlen";break;}
                if(error!=null){gate.Activating=false;gate.SecureTicks=0;gate.Reason=error;Emit(gate.OwnerId,b==null?0:b.PlanetId,gate.BuildingId,"GateInterrupted",error);continue;}
                gate.SecureTicks++;
                if(gate.SecureTicks<Definitions.GateSecureTicks)continue;
                for(int r=0;r<6;r++){b.Inventory[r]-=Definitions.GateComponents[r];FindPlanet(b.PlanetId).ConsumedTotals[r]+=Definitions.GateComponents[r];}
                gate.Activating=false;gate.Complete=true;gate.Reason="Gemeinsamer Szenarioerfolg";
                foreach(int f in gate.Participants)if(!State.Winners.Contains(f))State.Winners.Add(f);
                foreach(var f in State.Factions)Emit(f.Id,b.PlanetId,b.Id,"Victory","Sprungtor abgeschlossen; Sieger: "+string.Join(",",gate.Participants));
            }
        }
        private bool CanRebuild(int faction)
        {
            if(State.Planets.Exists(p=>p.OwnerId==faction))return true;
            foreach(var ship in State.Ships)if(ship.OwnerId==faction&&ship.Kind==ShipKind.ColonyShip&&!ship.Destroyed&&!ship.Consumed&&ship.Crew==Definitions.Ships[(int)ShipKind.ColonyShip].Crew)
            {
                bool loaded=true;for(int r=0;r<6;r++)if(ship.Cargo[r]<Definitions.Ships[(int)ShipKind.ColonyShip].ColonyCargo[r])loaded=false;
                if(loaded&&State.Planets.Exists(p=>p.OwnerId==0&&p.Buildings.Count==0&&p.Terrain[6*p.Width+8]==0))return true;
            }
            return false;
        }
        private void TickFactionExit()
        {
            // Legacy demonstrations are isolated fixtures, not elimination-enabled matches.
            if(!State.DiplomacyScenario)return;
            foreach(var f in State.Factions)if(f.Status==FactionStatus.Active&&!CanRebuild(f.Id))ExitFaction(f.Id,FactionStatus.Eliminated,0);
        }
        private void ExitFaction(int faction,FactionStatus status,int recipient)
        {
            var f=State.Factions.Find(v=>v.Id==faction);if(f==null||f.Status!=FactionStatus.Active)return;
            f.Status=status;
            foreach(var t in new List<TreatyState>(State.Treaties))if(t.Terms.Members.Contains(faction)&&(TreatyLive(t)||t.Phase==TreatyPhase.Proposed))EndTreaty(t,TreatyPhase.Expired,"Fraktion ausgeschieden; Verpflichtung endet",faction);
            State.PendingCommands.RemoveAll(c=>c.IssuerId==faction);State.ComputerFactions.RemoveAll(a=>a.FactionId==faction);
            foreach(var s in State.Ships)if(s.OwnerId==faction&&!s.Destroyed&&!s.Consumed)
            {
                CancelFreight(s);if(recipient==0)DestroyShip(s.Id);else{s.OwnerId=recipient;s.Order=ShipOrder.Hold;s.AutoSupply=false;foreach(var u in s.Troops)u.OwnerId=recipient;}
            }
            foreach(var route in State.Routes){var ship=State.Ships.Find(s=>s.Id==route.ShipId);if(ship.OwnerId==faction||recipient!=0&&ship.OwnerId==recipient)route.Enabled=false;}
            foreach(var p in State.Planets)
            {
                if(recipient==0)SettleExitedCouriers(p,faction);
                // Fix inherited ownership before strategic ownership changes.
                foreach(var b in p.Buildings)if(b.OwnerId==0)b.OwnerId=p.OwnerId;
                foreach(var b in p.Buildings)if(BuildingOwner(p,b)==faction)
                {
                    b.ShipBuildPaid=false;CancelPortWork(p,b);b.RecruitQueue=null;b.RecruitKind=-1;b.RecruitPaid=false;b.RecruitProgress=0;b.CapturingFaction=0;b.CaptureProgress=0;b.RestoreWorkerId=0;b.RestoreProgress=0;
                    b.OwnerId=recipient==0?-1:recipient;b.Enabled=recipient!=0;b.Operational=false;
                    b.CycleActive=false;b.CycleProgress=0;b.ProductionReservedSpace=0;
                }
                if(p.OwnerId==faction){p.OwnerId=recipient;p.GatePermissions.Clear();}
                foreach(var u in p.Units)if(u.OwnerId==faction&&!u.Destroyed)
                {u.Order=UnitOrder.Hold;u.Path.Clear();u.PathDistance=0;if(recipient!=0)u.OwnerId=recipient;else{u.Destroyed=true;u.Health=0;p.LostTotals[3]+=u.Parts;u.Parts=0;}}
                ReconcileOccupationLogistics(p);
                State.Evacuations.RemoveAll(e=>e.Guest==faction);
            }
            foreach(var h in State.Beachheads)if(h.OwnerId==faction&&!h.Destroyed){if(recipient!=0)h.OwnerId=recipient;else{h.Destroyed=true;h.Health=0;FindPlanet(h.PlanetId).LostTotals[0]+=h.Food;FindPlanet(h.PlanetId).LostTotals[3]+=h.Parts;h.Food=h.Parts=0;}}
            f.Research.Active=-1;f.Research.Paid=false;f.Research.Progress=0;f.Research.LaboratoryId=0;
            foreach(var pair in State.Relations)if(pair.FirstId==faction||pair.SecondId==faction)pair.Relation=RelationKind.Peace;
            Emit(faction,0,0,"FactionExit",status==FactionStatus.Surrendered?"Bestätigte Kapitulation abgewickelt":"Keine Kolonie oder ausgerüstete legale Neugründung mehr verfügbar");
        }
        private void SettleExitedCouriers(PlanetState p,int faction)
        {
            for(int i=p.Deliveries.Count-1;i>=0;i--)
            {
                var order=p.Deliveries[i];var source=FindBuilding(order.SourceId);var target=FindBuilding(order.TargetId);
                if(BuildingOwner(p,source)!=faction)continue;
                source.ReservedOutgoing[(int)order.Resource]-=order.Amount;target.ReservedIncoming[(int)order.Resource]-=order.Amount;p.Deliveries.RemoveAt(i);
            }
            for(int i=p.Shipments.Count-1;i>=0;i--)
            {
                var shipment=p.Shipments[i];var source=FindBuilding(shipment.SourceId);var target=FindBuilding(shipment.TargetId);int r=(int)shipment.Resource;
                if(BuildingOwner(p,source)!=faction)continue;
                if(shipment.Phase==ShipmentPhase.Loading)source.ReservedOutgoing[r]-=shipment.Amount;
                if(shipment.Phase!=ShipmentPhase.Returning&&shipment.Phase!=ShipmentPhase.Recovering)target.ReservedIncoming[r]-=shipment.Amount;
                if(shipment.Phase==ShipmentPhase.Recovering&&shipment.RecoveryReserved)source.ReservedIncoming[r]-=shipment.Amount;
                if(shipment.Phase==ShipmentPhase.InTransit||shipment.Phase==ShipmentPhase.Unloading)source.ReservedReturnSpace-=shipment.Amount;
                if(shipment.Phase!=ShipmentPhase.Loading)p.LostTotals[r]+=shipment.Amount;
                p.Shipments.RemoveAt(i);
            }
        }
    }
}
