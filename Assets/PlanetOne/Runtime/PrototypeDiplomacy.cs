using System;
using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private bool showingDiplomacy;
        private int diplomacyPartner=2, diplomacyWorld=1, treatyType, jointGoal, offeredGood=2, paymentGood=3;
        private string offeredAmount="8",paymentAmount="2";
        private AccessRight offeredRights;
        private bool confirmDiplomaticRisk;
        private Color DiplomaticColor(int owner) {return owner==PlayerFactionId?Cyan:simulation.AtWar(PlayerFactionId,owner)?Danger:simulation.SharesSensors(PlayerFactionId,owner)?new Color(0.48f,0.72f,0.52f):Muted;}
        public void StartDiplomacyScenario()
        {
            if(!simulation.State.AiScenario&&!simulation.State.InvasionTestFixture&&!simulation.State.SpaceTestFixture&&!simulation.State.MilitaryTestFixture)priorSession=simulation;
            simulation=GameSimulation.CreateDiplomacyScenario();foodRates.Clear();controlGroups.Clear();shipGroups.Clear();showingSpacePanel=false;showingMilitary=false;showingDiplomacy=true;BuildSpace();SetView(0);Notify(simulation.State.LastMessage);
        }
        public bool RequestDiplomacy(SimCommand command)
        {command.Id=simulation.NextCommandId();command.IssuerId=PlayerFactionId;return simulation.Enqueue(command);}
        public void StartDiplomacyFixture()
        {
            if(!simulation.State.DiplomacyScenario||!simulation.State.SpaceTestFixture)priorSession=simulation;
            simulation=GameSimulation.CreateDiplomacyFixture();foodRates.Clear();controlGroups.Clear();shipGroups.Clear();showingSpacePanel=false;showingMilitary=false;showingDiplomacy=true;BuildSpace();SetView(0);Notify(simulation.State.LastMessage);
        }
        private void DiplomacyAction(string label,CommandKind kind,long target=0,long actor=0,int number=0,bool confirmed=false)
        {if(GUILayout.Button(label,buttonStyle,GUILayout.Height(30)))RequestDiplomacy(new SimCommand{Kind=kind,TargetId=target,ActorId=actor,IntValue=number,BoolValue=confirmed});}
        private void DrawDiplomacyPanel()
        {
            GUILayout.Label("DIPLOMATIE UND SPRUNGTOR",headingStyle);
            if(simulation.State.SpaceTestFixture&&simulation.State.DiplomacyScenario)GUILayout.Label("TESTFIXTURE · Endliche vorgebaute Anlagen und Bestände; kein regulärer Start.",smallStyle);
            if(priorSession!=null&&GUILayout.Button("Zur vorherigen Partie zurück",buttonStyle)){simulation=priorSession;priorSession=null;showingDiplomacy=false;BuildSpace();SetView(0);return;}
            foreach(var f in simulation.State.Factions)if(f.Id!=PlayerFactionId)
                if(GUILayout.Button(f.Name+" · "+(f.Status!=FactionStatus.Active?"Ausgeschieden":simulation.AtWar(PlayerFactionId,f.Id)?"Krieg":"Frieden"),f.Id==diplomacyPartner?selectedButtonStyle:buttonStyle,GUILayout.Height(30)))diplomacyPartner=f.Id;
            GUILayout.Label("Vertrauen: "+simulation.Trust(PlayerFactionId,diplomacyPartner)+" · Laufzeit 10 Min., Kündigung 60 Sek.",smallStyle);
            foreach(var relation in simulation.State.Relations)if(relation.Relation==RelationKind.War)GUILayout.Label("Öffentlicher Krieg: "+simulation.State.Factions.Find(f=>f.Id==relation.FirstId).Name+" gegen "+simulation.State.Factions.Find(f=>f.Id==relation.SecondId).Name+". Koalitionsbeitritt übernimmt keinen Angriffskrieg.",smallStyle);
            treatyType=GUILayout.SelectionGrid(treatyType,new[]{"Waffenruhe","Nichtangriff","Handel","Zugang","Verteidigung","Koalition","Operation"},2,buttonStyle);
            GUILayout.Label("Geltungsort / Operationsziel",smallStyle);
            foreach(var p in simulation.State.Planets)if(simulation.IsPlanetKnown(PlayerFactionId,p.Id))
                if(GUILayout.Button(p.Name,p.Id==diplomacyWorld?selectedButtonStyle:buttonStyle,GUILayout.Height(26)))diplomacyWorld=p.Id;
            if(treatyType==(int)TreatyKind.Access||treatyType==(int)TreatyKind.Coalition)
            {
                foreach(AccessRight right in new[]{AccessRight.CivilTransit,AccessRight.MilitaryTransit,AccessRight.Landing,AccessRight.Stationing,AccessRight.Repair,AccessRight.Supply})
                {bool value=GUILayout.Toggle((offeredRights&right)!=0,AccessLabel(right));if(value)offeredRights|=right;else offeredRights&=~right;}
                GUILayout.Label("Rechte gelten am ausgewählten Ort; Koalitionssicht gilt systemweit. Keine freie Warenentnahme.",smallStyle);
            }
            if(treatyType==(int)TreatyKind.Trade)
            {
                GUILayout.Label("Eigene Lieferung / Gegenleistung",smallStyle);
                offeredGood=GUILayout.SelectionGrid(offeredGood,new[]{"Nahrung","Erz","Metall","Bauteile","Kristalle","Elektronik"},3,buttonStyle);offeredAmount=GUILayout.TextField(offeredAmount,4);
                paymentGood=GUILayout.SelectionGrid(paymentGood,new[]{"Nahrung","Erz","Metall","Bauteile","Kristalle","Elektronik"},3,buttonStyle);paymentAmount=GUILayout.TextField(paymentAmount,4);
                GUILayout.Label("Verwendet den ersten eigenen und aktuell aufgeklärten Partnerhafen. Jeder liefert mit eigenem Frachter. Bei Kündigung bleibt geladene Ware beim Lieferanten und kehrt zurück.",smallStyle);
            }
            if(treatyType==(int)TreatyKind.JointOperation){jointGoal=GUILayout.SelectionGrid(jointGoal,new[]{"Verteidigen","Eskortieren","Erobern"},1,buttonStyle);GUILayout.Label("Bei Eroberung erhält der Anbieter den Planeten. Zusagen und Beginn erfolgen separat; jeder Teilnehmer muss dem Krieg zustimmen.",smallStyle);}
            if(GUILayout.Button("Verbindliches Angebot senden",buttonStyle,GUILayout.Height(34)))
            {
                var terms=new TreatyTerms{Kind=(TreatyKind)treatyType,Members=new List<int>{PlayerFactionId,diplomacyPartner},PlanetId=diplomacyWorld,Rights=offeredRights,Goal=(JointGoal)jointGoal,FutureOwner=PlayerFactionId};
                var coalition=simulation.CoalitionOf(PlayerFactionId);if(terms.Kind==TreatyKind.Coalition&&coalition!=null){terms.CoalitionId=coalition.Id;terms.Members=new List<int>(coalition.Terms.Members);if(!terms.Members.Contains(diplomacyPartner))terms.Members.Add(diplomacyPartner);}
                foreach(var p in simulation.State.Planets)foreach(var b in p.Buildings)if(b.Kind==BuildingKind.Spaceport&&b.Built&&!b.Destroyed)
                {if(GameSimulation.BuildingOwner(p,b)==PlayerFactionId&&terms.SourcePort==0)terms.SourcePort=b.Id;if(GameSimulation.BuildingOwner(p,b)==diplomacyPartner&&terms.TargetPort==0&&simulation.CanSeeEntity(p,PlayerFactionId,b.Id))terms.TargetPort=b.Id;}
                foreach(var s in simulation.State.Ships)if(s.OwnerId==PlayerFactionId&&!s.Destroyed&&!s.Consumed&&s.Kind==ShipKind.Freighter){terms.EscortTarget=s.Id;break;}
                int amount,payment;if(int.TryParse(offeredAmount,out amount))terms.Amount=amount;if(int.TryParse(paymentAmount,out payment))terms.PaymentAmount=payment;terms.Goods=(ResourceKind)offeredGood;terms.Payment=(ResourceKind)paymentGood;
                RequestDiplomacy(new SimCommand{Kind=CommandKind.ProposeTreaty,Terms=terms});
            }
            confirmDiplomaticRisk=GUILayout.Toggle(confirmDiplomaticRisk,"Krieg / Vertragsbruch / Kapitulation ausdrücklich bestätigen");
            if(confirmDiplomaticRisk)DiplomacyAction("Krieg erklären",CommandKind.DeclareWar,number:diplomacyPartner);
            GUILayout.Space(10);GUILayout.Label("VERTRÄGE UND ZUSAGEN",headingStyle);
            foreach(var t in simulation.State.Treaties)if(t.Terms.Members.Contains(PlayerFactionId))
            {
                GUILayout.Label("#"+t.Id+" "+Definitions.TreatyName(t.Terms.Kind)+" · "+Definitions.TreatyStatus(t.Phase),headingStyle);
                GUILayout.Label("Beteiligte: "+string.Join(",",t.Terms.Members)+" · "+t.Reason,smallStyle);
                GUILayout.Label("Ort "+t.Terms.PlanetId+" · Rechte: "+RightsText(t.Terms.Rights)+" · Dauer "+t.Terms.DurationTicks*Definitions.TickSeconds+"s / Kündigung "+t.Terms.NoticeTicks*Definitions.TickSeconds+"s",smallStyle);
                if(t.Terms.Kind==TreatyKind.Trade)GUILayout.Label(t.Terms.Amount+" "+Definitions.ResourceName(t.Terms.Goods)+" von Hafen #"+t.Terms.SourcePort+" nach #"+t.Terms.TargetPort+" gegen "+t.Terms.PaymentAmount+" "+Definitions.ResourceName(t.Terms.Payment)+"; geliefert "+t.Delivered+" / "+t.PaymentDelivered,smallStyle);
                if(t.Terms.Kind==TreatyKind.JointOperation)GUILayout.Label("Ziel: "+new[]{"Verteidigung","Eskorte","Eroberung"}[(int)t.Terms.Goal]+" · Eigentümer nach Eroberung: "+t.Terms.FutureOwner+" · Eskortziel #"+t.Terms.EscortTarget,smallStyle);
                if(t.Phase==TreatyPhase.Proposed)
                {if(!t.Approvals.Contains(PlayerFactionId))DiplomacyAction("Annehmen",CommandKind.AcceptTreaty,t.Id);DiplomacyAction(t.Proposer==PlayerFactionId?"Zurückziehen":"Ablehnen",t.Proposer==PlayerFactionId?CommandKind.WithdrawTreaty:CommandKind.RejectTreaty,t.Id);}
                if(GameSimulation.TreatyLive(t))
                {
                    if(t.Phase==TreatyPhase.Active)DiplomacyAction("Fristgerecht kündigen",CommandKind.TerminateTreaty,t.Id);
                    if(confirmDiplomaticRisk)DiplomacyAction("Vertrag ausdrücklich brechen",CommandKind.BreachTreaty,t.Id,confirmed:true);
                    foreach(var s in simulation.State.Ships)if(s.OwnerId==PlayerFactionId&&!s.Destroyed&&!s.Consumed)
                    {if(t.Terms.Kind==TreatyKind.Trade&&s.Kind==ShipKind.Freighter)DiplomacyAction("Liefern mit Frachter #"+s.Id,CommandKind.DispatchTrade,t.Id,s.Id);if(t.Terms.Kind==TreatyKind.JointOperation&&!simulation.ShipPledged(s.Id))DiplomacyAction("Schiff #"+s.Id+" zusagen",CommandKind.PledgeShip,t.Id,s.Id);}
                    if(t.Terms.Kind==TreatyKind.JointOperation){foreach(var p in t.Pledges)GUILayout.Label("#"+p.ShipId+" · bereit ab "+p.ReadyTick+" · "+(p.Arrived?"angekommen":"unterwegs / Vorbereitung"),smallStyle);DiplomacyAction("Gemeinsame Operation beginnen",CommandKind.StartJointOperation,t.Id);DiplomacyAction("Operation abbrechen / Rückzug",CommandKind.CancelJointOperation,t.Id);}
                }
            }
            foreach(var e in simulation.State.Evacuations)if(e.Guest==PlayerFactionId||e.Host==PlayerFactionId)GUILayout.Label("Räumung auf "+simulation.FindPlanet(e.PlanetId).Name+": "+Math.Max(0,(e.UntilTick-simulation.State.Tick)*Definitions.TickSeconds).ToString("F0")+"s · "+e.Units.Count+" Truppen. Nur Abzug zum eigenen Träger und Verladung; beidseitiger Schutz.",smallStyle);
            GUILayout.Label("SPRUNGTORPROJEKT",headingStyle);
            GUILayout.Label("Betreiber: Elektronik + Sensoren erforschen. Sprungtor bauen und 12 Nahrung, 40 Metall, 24 Bauteile, 8 Kristalle und 8 Elektronik anliefern. Danach 60 Sekunden mit Energie, Personal, Nahrung und Kontrolle sichern. Mitgliederwechsel setzt Sicherung zurück.",smallStyle);
            foreach(var p in simulation.State.Planets)foreach(var b in p.Buildings)if(b.Kind==BuildingKind.JumpGate&&GameSimulation.BuildingOwner(p,b)==PlayerFactionId)DiplomacyAction("Sprungtor #"+b.Id+" aktivieren",CommandKind.ActivateGate,b.Id);
            foreach(var g in simulation.State.GateProjects)GUILayout.Label("Tor #"+g.BuildingId+" · "+g.Reason+" · "+g.SecureTicks+" / "+Definitions.GateSecureTicks+" · Teilnehmer "+string.Join(",",g.Participants),smallStyle);
            if(simulation.State.Winners.Count>0)GUILayout.Label("SZENARIOERFOLG: "+string.Join(",",simulation.State.Winners),headingStyle);
            if(confirmDiplomaticRisk){GUILayout.Label("Kapitulation ohne Übergabe: Restgebäude werden neutral, mobile Kräfte außer Dienst gestellt. Unwiderruflich für diese Partie.",smallStyle);DiplomacyAction("Kapitulation bestätigen",CommandKind.Surrender,confirmed:true);}
            if(confirmDiplomaticRisk&&GUILayout.Button("Kapitulation mit Übergabe an gewählten Partner anbieten",buttonStyle))RequestDiplomacy(new SimCommand{Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms{Kind=TreatyKind.Surrender,Members=new List<int>{PlayerFactionId,diplomacyPartner}}});
        }
        private static string AccessLabel(AccessRight r){switch(r){case AccessRight.CivilTransit:return "Ziviler Transit";case AccessRight.MilitaryTransit:return "Militärischer Transit / Tore";case AccessRight.Landing:return "Militärische Landung";case AccessRight.Stationing:return "Stationierung";case AccessRight.Repair:return "Hafenreparatur";default:return "Feldversorgung";}}
        private static string RightsText(AccessRight rights){var labels=new List<string>();foreach(AccessRight r in Enum.GetValues(typeof(AccessRight)))if(r!=AccessRight.None&&(rights&r)!=0)labels.Add(AccessLabel(r));return labels.Count==0?"Keine Zugangsrechte":string.Join(", ",labels);}
    }
}
