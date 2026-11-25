using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        public bool FactionActive(int id) { return State.Factions.Exists(f=>f.Id==id && f.Status==FactionStatus.Active); }
        public static bool TreatyLive(TreatyState t) { return t.Phase==TreatyPhase.Active || t.Phase==TreatyPhase.Terminating; }
        public TreatyState CoalitionOf(int faction) { return State.Treaties.Find(t=>TreatyLive(t)&&t.Terms.Kind==TreatyKind.Coalition&&t.Terms.Members.Contains(faction)); }
        public bool SharesSensors(int a,int b) { var t=CoalitionOf(a);return a>0 && (a==b || t!=null&&t.Terms.Members.Contains(b)); }
        public bool HasAccess(int guest,int host,int planet,AccessRight right)
        {
            if(guest==host&&host>0)return true;
            if(AtWar(guest,host))return false;
            if((right==AccessRight.Stationing||right==AccessRight.Supply||right==AccessRight.Landing||right==AccessRight.MilitaryTransit)&&State.Treaties.Exists(t=>TreatyLive(t)&&t.Terms.Kind==TreatyKind.JointOperation&&t.Terms.PlanetId==planet&&t.Terms.Members.Contains(guest)&&t.Terms.Members.Contains(host)&&t.OperationPhase==OperationPhase.Executing))return true;
            return State.Treaties.Exists(t=>TreatyLive(t)&&(t.Terms.Kind==TreatyKind.Access||t.Terms.Kind==TreatyKind.Coalition)&&t.Terms.Members.Contains(guest)&&t.Terms.Members.Contains(host)&&(t.Terms.PlanetId==0||t.Terms.PlanetId==planet)&&(t.Terms.Rights&right)==right);
        }
        public bool EvacuatingFaction(int faction,int planet) { return State.Evacuations.Exists(e=>e.Guest==faction&&e.PlanetId==planet&&e.UntilTick>State.Tick); }
        private bool EvacuatingUnit(PlanetState p,UnitState u) { return State.Evacuations.Exists(e=>e.PlanetId==p.Id&&e.Guest==u.OwnerId&&e.UntilTick>State.Tick&&e.Units.Contains(u.Id)); }
        private bool ProtectedGuest(PlanetState p,int attacker,long target) {return State.Evacuations.Exists(e=>e.PlanetId==p.Id&&e.Host==attacker&&e.UntilTick>State.Tick&&e.Units.Contains(target));}
        private bool ProtectedShip(int attacker,ShipState ship) {return State.Evacuations.Exists(e=>e.Host==attacker&&e.PlanetId==ship.TargetPlanetId&&e.UntilTick>State.Tick&&e.Ships.Contains(ship.Id)&&ship.TransportPhase!=TransportPhase.Space);}
        private void DiplomaticEvent(TreatyState t,int actor,int other,int delta,string key,string reason)
        {
            if(State.DiplomacyEvents.Exists(e=>e.TreatyId==(t==null?0:t.Id)&&e.Actor==actor&&e.Other==other&&e.Key==key))return;
            State.DiplomacyEvents.Add(new DiplomacyEvent {TreatyId=t==null?0:t.Id,Actor=actor,Other=other,TrustDelta=delta,Tick=State.Tick,Key=key,Reason=reason});
            Emit(other,t==null?0:t.Terms.PlanetId,t==null?0:t.Id,"Diplomacy",reason);
        }
        public int Trust(int observer,int actor) {int result=0;foreach(var e in State.DiplomacyEvents)if(e.Actor==actor&&(e.Other==observer||e.Other==0))result+=e.TrustDelta;return result;}
        private void SetTreaty(TreatyState t,TreatyPhase phase,string reason)
        {
            t.Phase=phase;t.ChangedTick=State.Tick;t.Reason=reason;
            foreach(int f in t.Terms.Members)Emit(f,t.Terms.PlanetId,t.Id,"TreatyChanged",Definitions.TreatyName(t.Terms.Kind)+": "+reason);
        }
        private void SetRelation(int a,int b,RelationKind relation)
        {
            var pair=State.Relations.Find(r=>r.FirstId==Math.Min(a,b)&&r.SecondId==Math.Max(a,b));pair.Relation=relation;
            if(relation!=RelationKind.Peace)return;
            foreach(var p in State.Planets) {
                foreach(var u in p.Units)if(u.OwnerId==a||u.OwnerId==b){u.Order=UnitOrder.Hold;u.TargetId=0;u.Path.Clear();u.PathDistance=0;}
                foreach(var building in p.Buildings)if(building.CapturingFaction==a||building.CapturingFaction==b){building.CapturingFaction=0;building.CaptureProgress=0;}
            }
            foreach(var s in State.Ships)if((s.OwnerId==a||s.OwnerId==b)&&(s.Order==ShipOrder.Attack||s.Order==ShipOrder.Bombard)){s.Order=ShipOrder.Hold;s.CombatTargetId=0;}
        }
        private bool ProtectsPeace(TreatyState t,int a,int b) {return TreatyLive(t)&&t.Terms.Members.Contains(a)&&t.Terms.Members.Contains(b)&&(t.Terms.Kind==TreatyKind.Ceasefire||t.Terms.Kind==TreatyKind.NonAggression||t.Terms.Kind==TreatyKind.Coalition);}
        private string TermsError(TreatyTerms terms,int proposer)
        {
            if(terms==null||!Enum.IsDefined(typeof(TreatyKind),terms.Kind)||terms.Members==null||terms.Members.Count<2||terms.Members.Count>State.Factions.Count||!terms.Members.Contains(proposer))return "Vertrag benötigt eindeutige beteiligte Fraktionen.";
            var seen=new HashSet<int>();foreach(int f in terms.Members)if(!FactionActive(f)||!seen.Add(f))return "Beteiligte Fraktion fehlt, ist ausgeschieden oder doppelt.";
            if(terms.Kind!=TreatyKind.Coalition&&terms.Kind!=TreatyKind.JointOperation&&terms.Members.Count!=2)return "Dieser Vertrag ist paarweise.";
            if(terms.DurationTicks<40||terms.DurationTicks>1200000||terms.NoticeTicks<20||terms.NoticeTicks>terms.DurationTicks||((int)terms.Rights&~63)!=0||terms.PlanetId!=0&&FindPlanet(terms.PlanetId)==null)return "Ungültige Laufzeit, Frist oder Rechte.";
            if((terms.Kind==TreatyKind.Access||terms.Kind==TreatyKind.Defense)&&terms.PlanetId!=0&&!terms.Members.Contains(FindPlanet(terms.PlanetId).OwnerId))return "Zugangs- oder Schutzgebiet muss einem Vertragspartner gehören.";
            if(terms.Kind!=TreatyKind.Ceasefire&&terms.Kind!=TreatyKind.Surrender)foreach(int a in terms.Members)foreach(int b in terms.Members)if(AtWar(a,b))return "Zuerst einen Waffenstillstand mit allen Beteiligten vereinbaren.";
            if(terms.Kind==TreatyKind.Coalition)
            {
                var old=terms.CoalitionId==0?null:State.Treaties.Find(t=>t.Id==terms.CoalitionId&&TreatyLive(t)&&t.Terms.Kind==TreatyKind.Coalition);
                if(terms.CoalitionId!=0&&(old==null||old.Terms.Members.Exists(f=>!terms.Members.Contains(f))))return "Bestehende Mitglieder müssen dem Beitritt vollständig zustimmen.";
                foreach(int f in terms.Members){var existing=CoalitionOf(f);if(existing!=null&&existing!=old)return "Fraktion ist bereits in einer anderen Koalition.";}
            }
            if(terms.Kind==TreatyKind.Trade)
            {
                if(!PortOwned(FindBuilding(terms.SourcePort),proposer)||!PortOwned(FindBuilding(terms.TargetPort),terms.Members.Find(f=>f!=proposer))||terms.SourcePort==terms.TargetPort||!Enum.IsDefined(typeof(ResourceKind),terms.Goods)||!Enum.IsDefined(typeof(ResourceKind),terms.Payment)||terms.Amount<1||terms.Amount>200||terms.PaymentAmount<0||terms.PaymentAmount>200)return "Eigene Lieferhäfen, gültige Güter und Mengen benötigt.";
            }
            if(terms.Kind==TreatyKind.JointOperation)
            {
                if(FindPlanet(terms.PlanetId)==null||!Enum.IsDefined(typeof(JointGoal),terms.Goal)||!terms.Members.Contains(terms.FutureOwner))return "Zielort und vereinbarter Eigentümer fehlen.";
                if(terms.Goal==JointGoal.Invade)foreach(int f in terms.Members)if(!AtWar(f,FindPlanet(terms.PlanetId).OwnerId))return "Jeder Teilnehmer muss dem äußeren Krieg ausdrücklich zugestimmt haben.";
                if(terms.Goal==JointGoal.Escort){var s=ActiveShip(terms.EscortTarget);if(s==null||!terms.Members.Contains(s.OwnerId))return "Gemeinsames Eskortziel fehlt.";}
            }
            return null;
        }
        private void ActivateTreaty(TreatyState t)
        {
            SetTreaty(t,TreatyPhase.Accepted,"Alle Beteiligten haben zugestimmt");
            if(t.Terms.Kind==TreatyKind.Coalition&&t.Terms.CoalitionId!=0){var old=State.Treaties.Find(v=>v.Id==t.Terms.CoalitionId);SetTreaty(old,TreatyPhase.Expired,"Durch einstimmige erweiterte Mitgliedschaft ersetzt");}
            t.StartTick=State.Tick;t.EndTick=State.Tick+t.Terms.DurationTicks;SetTreaty(t,TreatyPhase.Active,"Vereinbarung gilt bis Tick "+t.EndTick);
            if(t.Terms.Kind==TreatyKind.Ceasefire)SetRelation(t.Terms.Members[0],t.Terms.Members[1],RelationKind.Peace);
            if(t.Terms.Kind==TreatyKind.Surrender)ExitFaction(t.Proposer,FactionStatus.Surrendered,t.Terms.Members.Find(f=>f!=t.Proposer));
        }
        private void ApplyDiplomacy(SimCommand c)
        {
            string error=null;var t=State.Treaties.Find(v=>v.Id==c.TargetId);
            if(c.Kind==CommandKind.ProposeTreaty)
            {
                error=TermsError(c.Terms,c.IssuerId);
                if(error==null){var offer=new TreatyState {Id=State.NextEntityId++,Proposer=c.IssuerId,CreatedTick=State.Tick,ChangedTick=State.Tick,Terms=CopyAiValue(c.Terms)};offer.Approvals.Add(c.IssuerId);State.Treaties.Add(offer);SetTreaty(offer,TreatyPhase.Proposed,"Zustimmung aller Beteiligten erforderlich");}
            }
            else if(c.Kind==CommandKind.DeclareWar)
            {
                int enemy=c.IntValue;
                if(!FactionActive(enemy)||enemy==c.IssuerId||AtWar(c.IssuerId,enemy))error="Andere aktive friedliche Fraktion benötigt.";
                else if(State.Treaties.Exists(v=>ProtectsPeace(v,c.IssuerId,enemy)))error="Schutzvertrag zuerst ausdrücklich brechen oder fristgerecht beenden.";
                else
                {
                    SetRelation(c.IssuerId,enemy,RelationKind.War);
                    foreach(var pact in new List<TreatyState>(State.Treaties))if(TreatyLive(pact)&&pact.Terms.Kind==TreatyKind.Defense&&pact.Terms.Members.Contains(enemy)&&!pact.Terms.Members.Contains(c.IssuerId)&&(pact.Terms.PlanetId==0||FindPlanet(pact.Terms.PlanetId).OwnerId==enemy))
                    {int ally=pact.Terms.Members.Find(f=>f!=enemy);if(!State.Treaties.Exists(v=>ProtectsPeace(v,ally,c.IssuerId))){SetRelation(ally,c.IssuerId,RelationKind.War);DiplomaticEvent(pact,ally,enemy,4,"defense-"+State.Tick,"Verteidigungsbeistand gegen äußere Kriegserklärung aktiviert");}}
                    foreach(var existing in new List<TreatyState>(State.Treaties))if(TreatyLive(existing)&&existing.Terms.Members.Contains(c.IssuerId)&&existing.Terms.Members.Contains(enemy))EndTreaty(existing,TreatyPhase.Expired,"Kriegserklärung beendet friedliche Zusammenarbeit",c.IssuerId);
                    Emit(enemy,0,0,"WarDeclared","Fraktion "+c.IssuerId+" hat Krieg erklärt.");
                }
            }
            else if(c.Kind==CommandKind.ActivateGate)error=ActivateGate(c);
            else if(c.Kind==CommandKind.Surrender)
            {if(!c.BoolValue)error="Kapitulation ausdrücklich bestätigen.";else if(c.IntValue!=0)error="Restbesitzübergabe benötigt einen beiderseits angenommenen Kapitulationsvertrag.";else ExitFaction(c.IssuerId,FactionStatus.Surrendered,0);}
            else if(t==null||!t.Terms.Members.Contains(c.IssuerId))error="Eigene Vertragsbeteiligung benötigt.";
            else if(c.Kind==CommandKind.AcceptTreaty)
            {
                if(t.Phase!=TreatyPhase.Proposed||t.Approvals.Contains(c.IssuerId))error="Angebot ist nicht mehr offen oder bereits angenommen.";
                else if((error=TermsError(t.Terms,t.Proposer))==null){t.Approvals.Add(c.IssuerId);if(t.Approvals.Count==t.Terms.Members.Count)ActivateTreaty(t);}
            }
            else if(c.Kind==CommandKind.RejectTreaty||c.Kind==CommandKind.WithdrawTreaty)
            {
                if(t.Phase!=TreatyPhase.Proposed||c.Kind==CommandKind.WithdrawTreaty&&t.Proposer!=c.IssuerId)error="Nur ein offenes eigenes Angebot kann zurückgezogen werden.";
                else SetTreaty(t,c.Kind==CommandKind.RejectTreaty?TreatyPhase.Rejected:TreatyPhase.Withdrawn,c.Kind==CommandKind.RejectTreaty?(c.IntValue==1?"Abgelehnt: Vertrauen nach bekanntem Vertragsbruch zu niedrig":c.IntValue==2?"Abgelehnt: Gegenleistung aus freien eigenen Hafenbeständen nicht tragbar":c.IntValue==3?"Abgelehnt: zugesagte Kräfte derzeit nicht verfügbar":"Abgelehnt: Verpflichtung passt nicht zur aktuellen Lage"):"Angebot zurückgezogen");
            }
            else if(c.Kind==CommandKind.TerminateTreaty)
            {if(t.Phase!=TreatyPhase.Active)error="Nur aktive Verträge können gekündigt werden.";else{if(t.Terms.Kind!=TreatyKind.Ceasefire)t.EndTick=Math.Min(t.EndTick,State.Tick+t.Terms.NoticeTicks);t.LeavingFaction=c.IssuerId;SetTreaty(t,TreatyPhase.Terminating,"Wirksames Ende bei Tick "+t.EndTick+"; Waffenstillstand wahrt seine Mindestdauer. Mitgliederwechsel bricht Toraktivierung ab");}}
            else if(c.Kind==CommandKind.BreachTreaty)
            {if(!TreatyLive(t)||!c.BoolValue)error="Aktiven Vertragsbruch ausdrücklich bestätigen.";else{EndTreaty(t,TreatyPhase.Breached,"Bewusster Vertragsbruch durch Fraktion "+c.IssuerId,c.IssuerId);DiplomaticEvent(t,c.IssuerId,0,-20,"breach","Öffentlich bekannter Vertragsbruch");}}
            else if(c.Kind==CommandKind.DispatchTrade)error=DispatchTrade(t,c);
            else if(c.Kind==CommandKind.PledgeShip)error=PledgeJoint(t,c);
            else if(c.Kind==CommandKind.StartJointOperation)error=StartJoint(t);
            else if(c.Kind==CommandKind.CancelJointOperation)
            {if(t.Terms.Kind!=TreatyKind.JointOperation||!TreatyLive(t))error="Aktive gemeinsame Operation benötigt.";else EndTreaty(t,TreatyPhase.Expired,"Abbruch: Kräfte ziehen sich zum eigenen Hafen zurück",c.IssuerId);}
            else error="Unbekannter Diplomatiebefehl.";
            Report(c,error==null,error==null?"DiplomacyAccepted":"DiplomacyRejected",error??"Diplomatischer Auftrag ausgeführt.");
        }
        private void EndTreaty(TreatyState t,TreatyPhase phase,string reason,int leaving)
        {
            if(!TreatyLive(t)){SetTreaty(t,phase,reason);return;}
            var former=new List<int>(t.Terms.Members);SetTreaty(t,phase,reason);
            foreach(var ship in State.Ships)if(ship.Freight!=null&&ship.Freight.TreatyId==t.Id)CancelFreight(ship);
            if(t.Terms.Kind==TreatyKind.JointOperation)ReleaseJoint(t,reason);
            if(t.Terms.Kind==TreatyKind.Coalition&&leaving>0&&phase!=TreatyPhase.Breached&&former.Count>2&&t.StartTick+t.Terms.DurationTicks>State.Tick)
            {t.Terms.Members.Remove(leaving);t.Approvals.Remove(leaving);t.EndTick=t.StartTick+t.Terms.DurationTicks;SetTreaty(t,TreatyPhase.Active,"Fraktion "+leaving+" ausgetreten; übrige Koalition bleibt bis zum ursprünglichen Ende bestehen");}
            foreach(int guest in former)foreach(int host in former)if(guest!=host)foreach(var p in State.Planets)if(p.OwnerId==host&&!HasAccess(guest,host,p.Id,AccessRight.Stationing)&&((t.Terms.Rights&AccessRight.Stationing)!=0||t.Terms.Kind==TreatyKind.JointOperation))BeginEvacuation(guest,host,p);
        }
        private void BeginEvacuation(int guest,int host,PlanetState p)
        {
            var e=new EvacuationState{Guest=guest,Host=host,PlanetId=p.Id,UntilTick=State.Tick+Definitions.EvacuationTicks};
            foreach(var u in p.Units)if(u.OwnerId==guest&&!u.Destroyed){e.Units.Add(u.Id);u.Order=UnitOrder.Hold;u.Path.Clear();u.PathDistance=0;u.Status="Räumung: Abzug oder Verladung bis Tick "+e.UntilTick;}
            foreach(var s in State.Ships)if(s.OwnerId==guest&&!s.Destroyed&&s.TransportPhase!=TransportPhase.Space&&s.TargetPlanetId==p.Id){e.Ships.Add(s.Id);foreach(var u in s.Troops)e.Units.Add(u.Id);}
            if(e.Units.Count+e.Ships.Count>0){State.Evacuations.Add(e);Emit(guest,p.Id,0,"Evacuation","Stationierung endet; geschützter Abzug bis Tick "+e.UntilTick);}
        }
        private void TickDiplomacy()
        {
            foreach(var t in new List<TreatyState>(State.Treaties))
            {
                if(TreatyLive(t)&&State.Tick>=t.EndTick)EndTreaty(t,TreatyPhase.Expired,"Laufzeit oder Kündigungsfrist abgelaufen",t.LeavingFaction);
                if(t.Phase==TreatyPhase.Proposed&&State.Tick-t.CreatedTick>=t.Terms.DurationTicks)SetTreaty(t,TreatyPhase.Withdrawn,"Unbeantwortetes Angebot abgelaufen");
            }
            foreach(var e in State.Evacuations)
            {
                e.Units.RemoveAll(id=>!State.Planets.Exists(p=>p.Id==e.PlanetId&&p.Units.Exists(u=>u.Id==id&&!u.Destroyed))&&!State.Ships.Exists(s=>s.TargetPlanetId==e.PlanetId&&s.TransportPhase!=TransportPhase.Space&&!s.Destroyed&&s.Troops.Exists(u=>u.Id==id&&!u.Destroyed)));
                e.Ships.RemoveAll(id=>!State.Ships.Exists(s=>s.Id==id&&!s.Destroyed&&s.TargetPlanetId==e.PlanetId&&s.TransportPhase!=TransportPhase.Space));
            }
            State.Evacuations.RemoveAll(e=>State.Tick>=e.UntilTick||e.Units.Count+e.Ships.Count==0);
        }
        public static GameSimulation CreateDiplomacyScenario(int seed=12345)
        {
            var sim=CreateAiScenario(seed);sim.State.DiplomacyScenario=true;sim.State.Relations[0].Relation=RelationKind.Peace;
            sim.State.Factions.Add(new FactionState {Id=3,Name="Helios-Verbund"});
            sim.State.Relations.Add(new FactionRelation {FirstId=1,SecondId=3});sim.State.Relations.Add(new FactionRelation {FirstId=2,SecondId=3});
            sim.State.Planets.Add(CreatePlanet(sim.State,4,"Helios",-1,3,25,4));
            sim.State.ComputerFactions.Add(new AiState {FactionId=3,Profile=AiProfile.Technology});
            sim.State.LastMessage="M7: Drei unabhängige Fraktionen starten friedlich mit gleichen Mitteln. Verträge und Sprungtorprojekt sind über Diplomatie erreichbar.";
            AsteroidGeneration.Populate(sim.State);
            sim.UpdateSpaceKnowledge();return sim;
        }
    }
}
