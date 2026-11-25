using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public static partial class SaveCodec
    {
        private static void MigrateDiplomacy(GameState s)
        {
            s.Treaties=new List<TreatyState>();s.Evacuations=new List<EvacuationState>();s.DiplomacyEvents=new List<DiplomacyEvent>();s.GateProjects=new List<GateProject>();s.Winners=new List<int>();s.DiplomacyScenario=false;s.Version=8;
        }
        private static void ValidateDiplomacy(GameState s)
        {
            Require(s.Treaties!=null&&s.Evacuations!=null&&s.DiplomacyEvents!=null&&s.GateProjects!=null&&s.Winners!=null&&s.Factions!=null&&s.Planets!=null&&s.Ships!=null,"Diplomatiezustand fehlt.");
            Require(s.Treaties.Count<=4096&&s.DiplomacyEvents.Count<=16384,"Diplomatiezustand zu groß.");
            foreach(var f in s.Factions)Require(f!=null&&Enum.IsDefined(typeof(FactionStatus),f.Status),"Ungültiger Fraktionsstatus.");
            var ids=new HashSet<long>();var memberships=new HashSet<int>();var ships=new HashSet<long>();
            foreach(var t in s.Treaties)
            {
                Require(t!=null&&t.Id>0&&t.Id<s.NextEntityId&&ids.Add(t.Id)&&Enum.IsDefined(typeof(TreatyPhase),t.Phase)&&Enum.IsDefined(typeof(OperationPhase),t.OperationPhase)&&t.CreatedTick>=0&&t.CreatedTick<=t.ChangedTick&&t.ChangedTick<=s.Tick&&t.StartTick>=0&&t.StartTick<=s.Tick&&t.EndTick>=0&&t.Approvals!=null&&t.Pledges!=null&&!string.IsNullOrEmpty(t.Reason),"Ungültiger Vertrag.");
                ValidateTerms(s,t.Terms);Require(s.Factions.Exists(f=>f.Id==t.Proposer)&&(t.Terms.Members.Contains(t.Proposer)||t.Terms.Kind==TreatyKind.Coalition&&t.Phase!=TreatyPhase.Proposed),"Anbieter ist kein Teilnehmer.");var approvals=new HashSet<int>();foreach(int f in t.Approvals)Require(t.Terms.Members.Contains(f)&&approvals.Add(f),"Ungültige Zustimmung.");
                if(GameSimulation.TreatyLive(t))
                {
                    Require(t.Approvals.Count==t.Terms.Members.Count&&t.EndTick>=t.StartTick&&t.EndTick<=t.StartTick+t.Terms.DurationTicks,"Aktiver Vertrag ohne Zustimmung oder Laufzeit.");
                    foreach(int f in t.Terms.Members)Require(s.Factions.Exists(a=>a.Id==f&&a.Status==FactionStatus.Active),"Ausgeschiedener Vertragspartner.");
                    if(t.Terms.Kind==TreatyKind.Coalition)foreach(int f in t.Terms.Members){Require(memberships.Add(f),"Mehrfache Koalitionsmitgliedschaft.");foreach(int other in t.Terms.Members)Require(!s.Relations.Exists(r=>r.Relation==RelationKind.War&&r.FirstId==Math.Min(f,other)&&r.SecondId==Math.Max(f,other)),"Koalitionsmitglieder führen Krieg.");}
                }
                Require(t.Delivered>=0&&t.Delivered<=t.Terms.Amount&&t.PaymentDelivered>=0&&t.PaymentDelivered<=t.Terms.PaymentAmount,"Ungültige Liefergutschrift.");
                foreach(var p in t.Pledges)Require(p!=null&&t.Terms.Kind==TreatyKind.JointOperation&&GameSimulation.TreatyLive(t)&&t.Terms.Members.Contains(p.FactionId)&&p.ReadyTick>=0&&ships.Add(p.ShipId)&&s.Ships.Exists(a=>a.Id==p.ShipId&&a.OwnerId==p.FactionId)&&!s.ComputerFactions.Exists(a=>a.Operation.Ships.Contains(p.ShipId)),"Doppelte oder fremde Operationszusage.");
            }
            foreach(var ship in s.Ships)if(ship.Freight!=null&&ship.Freight.TreatyId>0)
            {
                var job=ship.Freight;var t=s.Treaties.Find(a=>a.Id==job.TreatyId&&GameSimulation.TreatyLive(a)&&a.Terms.Kind==TreatyKind.Trade);Require(t!=null,"Handelsfracht ohne Vertrag.");
                int other=t.Terms.Members.Find(a=>a!=t.Proposer);
                Require(ship.OwnerId==(job.PaymentLeg?other:t.Proposer)&&job.SourceId==(job.PaymentLeg?t.Terms.TargetPort:t.Terms.SourcePort)&&job.TargetId==(job.PaymentLeg?t.Terms.SourcePort:t.Terms.TargetPort)&&job.Resource==(job.PaymentLeg?t.Terms.Payment:t.Terms.Goods),"Handelsmanifest widerspricht Vertrag.");
            }
            foreach(var t in s.Treaties)if(t.Terms.Kind==TreatyKind.Trade)
            {int outgoing=t.Delivered,payment=t.PaymentDelivered;foreach(var ship in s.Ships)if(ship.Freight!=null&&ship.Freight.TreatyId==t.Id){if(ship.Freight.PaymentLeg)payment+=ship.Freight.Amount;else outgoing+=ship.Freight.Amount;}Require(outgoing<=t.Terms.Amount&&payment<=t.Terms.PaymentAmount,"Handelsmenge mehrfach reserviert.");}
            foreach(var e in s.Evacuations)
            {
                Require(e!=null&&e.Guest!=e.Host&&s.Factions.Exists(f=>f.Id==e.Guest)&&s.Factions.Exists(f=>f.Id==e.Host)&&s.Planets.Exists(p=>p.Id==e.PlanetId)&&e.UntilTick>=0&&e.Units!=null&&e.Ships!=null,"Ungültige Räumungsfrist.");
                var unitIds=new HashSet<long>();foreach(long id in e.Units)Require(id>0&&unitIds.Add(id)&&(s.Planets.Exists(p=>p.Id==e.PlanetId&&p.Units.Exists(u=>u.Id==id&&u.OwnerId==e.Guest))||s.Ships.Exists(ship=>ship.OwnerId==e.Guest&&ship.Troops.Exists(u=>u.Id==id))),"Fremdes Räumungskontingent.");
            }
            var events=new HashSet<string>();foreach(var e in s.DiplomacyEvents)Require(e!=null&&e.Tick>=0&&e.Tick<=s.Tick&&e.TrustDelta>=-100&&e.TrustDelta<=100&&!string.IsNullOrEmpty(e.Key)&&events.Add(e.TreatyId+":"+e.Actor+":"+e.Other+":"+e.Key),"Doppeltes oder ungültiges Beziehungsereignis.");
            var gates=new HashSet<long>();foreach(var g in s.GateProjects)
            {
                Require(g!=null&&gates.Add(g.BuildingId)&&s.Planets.Exists(p=>p.Buildings.Exists(b=>b.Id==g.BuildingId&&b.Kind==BuildingKind.JumpGate))&&s.Factions.Exists(f=>f.Id==g.OwnerId)&&g.Participants!=null&&g.Participants.Count>0&&g.SecureTicks>=0&&g.SecureTicks<=Definitions.GateSecureTicks&&!(g.Complete&&g.Activating),"Ungültiges Torprojekt.");
                var members=new HashSet<int>();foreach(int f in g.Participants)Require(s.Factions.Exists(a=>a.Id==f)&&members.Add(f),"Ungültige Tor-Teilnehmer.");
            }
            var winners=new HashSet<int>();foreach(int f in s.Winners)Require(winners.Add(f)&&s.GateProjects.Exists(g=>g.Complete&&g.Participants.Contains(f)),"Sieg ohne abgeschlossenes Projekt.");
        }
        private static void ValidateTerms(GameState s,TreatyTerms t)
        {
            Require(t!=null&&Enum.IsDefined(typeof(TreatyKind),t.Kind)&&Enum.IsDefined(typeof(JointGoal),t.Goal)&&Enum.IsDefined(typeof(ResourceKind),t.Goods)&&Enum.IsDefined(typeof(ResourceKind),t.Payment)&&t.Members!=null&&t.Members.Count>=2&&t.Members.Count<=s.Factions.Count&&t.DurationTicks>=40&&t.DurationTicks<=1200000&&t.NoticeTicks>=20&&t.NoticeTicks<=t.DurationTicks&&((int)t.Rights&~63)==0&&t.Amount>=0&&t.Amount<=200&&t.PaymentAmount>=0&&t.PaymentAmount<=200,"Ungültige Vertragsbedingungen.");
            var members=new HashSet<int>();foreach(int f in t.Members)Require(s.Factions.Exists(a=>a.Id==f)&&members.Add(f),"Ungültige Vertragsmitglieder.");
        }
    }
}
