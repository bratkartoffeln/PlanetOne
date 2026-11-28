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
        private int diplomacySection, diplomacyContractFilter;
        private long expandedDiplomacyTreaty, diplomacyRiskTreaty;
        private bool diplomacyPartnersOpen, diplomacyDangerOpen, diplomacyOnlyPartner;
        private GameSimulation diplomacyUiSession;
        private readonly Vector2[] diplomacyScrolls=new Vector2[5];
        private static readonly string[] DiplomacySectionNames={"Überblick","Verträge","Angebot","Koalition","Sprungtor"};
        private static readonly string[] TreatyChoiceNames={"Waffenruhe","Nichtangriff","Handel","Zugangsrechte","Verteidigung","Koalition","Operation"};
        private static readonly string[] TradeGoodsNames={"Nahrung","Erz","Metall","Bauteile","Kristalle","Elektronik","Wasserstoff"};
        public int DiplomacySection {get{return diplomacySection;}}
        public bool OpenDiplomacySection(int section)
        {
            if(section<0 || section>=DiplomacySectionNames.Length)return false;
            if(showingDiplomacy)diplomacyScrolls[diplomacySection]=sidebarScroll;
            OpenPresentationSection(6);diplomacySection=section;sidebarScroll=diplomacyScrolls[section];
            confirmDiplomaticRisk=false;diplomacyDangerOpen=false;return true;
        }
        public string ScenarioOutcomeText
        {
            get
            {
                if(simulation.State.Winners.Count==0)return null;
                var winners=new List<string>();foreach(int id in simulation.State.Winners){var faction=simulation.State.Factions.Find(f=>f.Id==id);if(faction!=null)winners.Add(faction.Name);}
                return "Szenario abgeschlossen · Sieg: "+string.Join(", ",winners);
            }
        }
        private Color DiplomaticColor(int owner)
        {
            if(owner==PlayerFactionId)return Cyan;
            if(!simulation.AtWar(PlayerFactionId,owner))
                return simulation.SharesSensors(PlayerFactionId,owner)?new Color(.48f,.72f,.52f):Muted;
            int index=0;
            foreach(var faction in simulation.State.Factions)
                if(faction.Id<owner && simulation.AtWar(PlayerFactionId,faction.Id))index++;
            if(index==0)return Danger;
            if(index==1)return new Color(.85f,.44f,1f);
            if(index==2)return new Color(1f,.65f,.14f);
            return Color.HSVToRGB(Mathf.Repeat(.1f+.618034f*index,1),.72f,1);
        }
        public void StartDiplomacyScenario()
        {
            if(!simulation.State.AiScenario&&!simulation.State.InvasionTestFixture&&!simulation.State.SpaceTestFixture&&!simulation.State.MilitaryTestFixture)priorSession=simulation;
            simulation=GameSimulation.CreateDiplomacyScenario(System.Environment.TickCount);foodRates.Clear();controlGroups.Clear();shipGroups.Clear();showingSpacePanel=false;showingMilitary=false;showingDiplomacy=true;BuildSpace();SetView(0);Notify(simulation.State.LastMessage);
        }
        public void StartReferenceScenario()
        {
            priorSession=simulation;
            simulation=GameSimulation.CreateReferenceScenario(Environment.TickCount);foodRates.Clear();controlGroups.Clear();shipGroups.Clear();showingSpacePanel=false;showingMilitary=false;showingDiplomacy=true;BuildSpace();SetView(0);Notify(simulation.State.LastMessage);
        }
        public bool RequestDiplomacy(SimCommand command)
        {command.Id=simulation.NextCommandId();command.IssuerId=PlayerFactionId;return simulation.Enqueue(command);}
        public void StartDiplomacyFixture()
        {
            if(!simulation.State.DiplomacyScenario||!simulation.State.SpaceTestFixture)priorSession=simulation;
            simulation=GameSimulation.CreateDiplomacyFixture();foodRates.Clear();controlGroups.Clear();shipGroups.Clear();showingSpacePanel=false;showingMilitary=false;showingDiplomacy=true;BuildSpace();SetView(0);Notify(simulation.State.LastMessage);
        }
        private void DiplomacyAction(string label,CommandKind kind,long target=0,long actor=0,int number=0,bool confirmed=false)
        {if(GUILayout.Button(label,buttonStyle,GUILayout.MinHeight(UiRowHeight(38))))RequestDiplomacy(new SimCommand{Kind=kind,TargetId=target,ActorId=actor,IntValue=number,BoolValue=confirmed});}
        private void DrawDiplomacyPanel()
        {
            if(diplomacyUiSession!=simulation)
            {diplomacyUiSession=simulation;expandedDiplomacyTreaty=0;confirmDiplomaticRisk=false;diplomacyDangerOpen=false;Array.Clear(diplomacyScrolls,0,diplomacyScrolls.Length);}
            GUILayout.Label("DIPLOMATIE",headingStyle);
            int section=DrawWorkspaceTabs(diplomacySection,DiplomacySectionNames);
            if(section!=diplomacySection)OpenDiplomacySection(section);
            GUILayout.Space(12);
            if(diplomacySection<4)DrawDiplomacyPartner();
            switch(diplomacySection)
            {
                case 0:DrawDiplomacyOverview();break;
                case 1:DrawDiplomacyContracts();break;
                case 2:DrawDiplomacyOffer();break;
                case 3:DrawCoalitionWorkspace();break;
                case 4:DrawGateWorkspace();break;
            }
            if(simulation.State.SpaceTestFixture&&simulation.State.DiplomacyScenario)
            {GUILayout.Space(18);GUILayout.Label("PRÜFKARTE · vorbereitete Anlagen und endliche Bestände",smallStyle);}
            if(priorSession!=null&&GUILayout.Button("Zur vorherigen Partie zurück",buttonStyle,GUILayout.MinHeight(UiRowHeight(38))))RestorePreviousSession();
        }
        private string DiplomaticFactionName(int id)
        {var faction=simulation.State.Factions.Find(f=>f.Id==id);return faction==null?"Fraktion #"+id:faction.Name;}
        private string DiplomaticMembers(List<int> members)
        {var names=new List<string>();foreach(int member in members)names.Add(DiplomaticFactionName(member));return string.Join(", ",names);}
        private void DrawDiplomacyPartner()
        {
            var partner=simulation.State.Factions.Find(f=>f.Id==diplomacyPartner && f.Id!=PlayerFactionId);
            if(partner==null){partner=simulation.State.Factions.Find(f=>f.Id!=PlayerFactionId);if(partner!=null)diplomacyPartner=partner.Id;}
            if(partner==null){GUILayout.Label("Keine andere Fraktion in dieser Partie.",bodyStyle);return;}
            if(GUILayout.Button(partner.Name+(diplomacyPartnersOpen?"  ▴":"  ▾"),selectedButtonStyle,GUILayout.MinHeight(UiRowHeight(44))))diplomacyPartnersOpen=!diplomacyPartnersOpen;
            if(diplomacyPartnersOpen)
                foreach(var faction in simulation.State.Factions)if(faction.Id!=PlayerFactionId)
                    if(GUILayout.Button(faction.Name,faction.Id==diplomacyPartner?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight(38))))
                    {diplomacyPartner=faction.Id;confirmDiplomaticRisk=false;diplomacyPartnersOpen=false;partner=faction;}
            GUILayout.Label((partner.Status!=FactionStatus.Active?"Ausgeschieden":simulation.AtWar(PlayerFactionId,partner.Id)?"Im Krieg":"Friedliche Beziehungen")+" · Vertrauen "+simulation.Trust(PlayerFactionId,partner.Id),smallStyle);
            GUILayout.Space(12);
        }
        private void DrawDiplomacyOverview()
        {
            int live=0,waiting=0;
            foreach(var treaty in simulation.State.Treaties)if(treaty.Terms.Members.Contains(PlayerFactionId))
            {if(GameSimulation.TreatyLive(treaty))live++;if(treaty.Phase==TreatyPhase.Proposed&&!treaty.Approvals.Contains(PlayerFactionId))waiting++;}
            GUILayout.Label("DEINE BEZIEHUNGEN",headingStyle);
            GUILayout.Label(live+" laufende Vereinbarungen · "+waiting+" offene Zustimmungen",bodyStyle);
            if(GUILayout.Button(waiting>0?"Offene Angebote prüfen":"Verträge ansehen",buttonStyle,GUILayout.MinHeight(UiRowHeight(42))))
            {diplomacyContractFilter=waiting>0?1:0;diplomacyOnlyPartner=false;OpenDiplomacySection(1);}
            if(GUILayout.Button("Neues Angebot vorbereiten",buttonStyle,GUILayout.MinHeight(UiRowHeight(42))))OpenDiplomacySection(2);
            GUILayout.Space(14);GUILayout.Label("ÖFFENTLICHE LAGE",headingStyle);bool war=false;
            foreach(var relation in simulation.State.Relations)if(relation.Relation==RelationKind.War)
            {war=true;GUILayout.Label(DiplomaticFactionName(relation.FirstId)+" gegen "+DiplomaticFactionName(relation.SecondId),bodyStyle);}
            if(!war)GUILayout.Label("Zurzeit ist kein Krieg erklärt.",smallStyle);
            else GUILayout.Label("Ein Koalitionsbeitritt übernimmt keinen Angriffskrieg.",smallStyle);
            DrawEvacuationNotices();
            if(simulation.State.Winners.Count>0){GUILayout.Space(12);GUILayout.Label(ScenarioOutcomeText,headingStyle);}
            GUILayout.Space(18);
            if(GUILayout.Button((diplomacyDangerOpen?"▾ ":"▸ ")+"Krieg und Kapitulation",buttonStyle,GUILayout.MinHeight(UiRowHeight(40))))
            {diplomacyDangerOpen=!diplomacyDangerOpen;confirmDiplomaticRisk=false;}
            if(!diplomacyDangerOpen)return;
            GUILayout.Label("Diese Entscheidungen verändern die Beziehungen oder beenden deine Teilnahme an der Partie.",bodyStyle);
            confirmDiplomaticRisk=GUILayout.Toggle(confirmDiplomaticRisk,"Krieg / Kapitulation ausdrücklich freigeben",GUILayout.MinHeight(UiRowHeight(40)));
            if(!confirmDiplomaticRisk)return;
            DiplomacyAction("Krieg gegen "+DiplomaticFactionName(diplomacyPartner)+" erklären",CommandKind.DeclareWar,number:diplomacyPartner);
            GUILayout.Space(10);GUILayout.Label("Kapitulation ohne Übergabe: Restgebäude werden neutral, mobile Kräfte außer Dienst gestellt. Für diese Partie unwiderruflich.",smallStyle);
            DiplomacyAction("Ohne Übergabe kapitulieren",CommandKind.Surrender,confirmed:true);
            if(GUILayout.Button("Kapitulation mit Übergabe an Partner anbieten",buttonStyle,GUILayout.MinHeight(UiRowHeight(46))))
                RequestDiplomacy(new SimCommand{Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms{Kind=TreatyKind.Surrender,Members=new List<int>{PlayerFactionId,diplomacyPartner}}});
        }
        private void DrawDiplomacyContracts()
        {
            GUILayout.Label("VERTRÄGE UND ZUSAGEN",headingStyle);
            diplomacyContractFilter=DrawWorkspaceTabs(diplomacyContractFilter,new[]{"Aktuell","Angebote","Archiv"});
            diplomacyOnlyPartner=GUILayout.Toggle(diplomacyOnlyPartner,"Nur den gewählten Partner anzeigen",GUILayout.MinHeight(UiRowHeight(36)));
            GUILayout.Space(10);bool any=false;
            foreach(var treaty in simulation.State.Treaties)
            {
                if(!treaty.Terms.Members.Contains(PlayerFactionId) || diplomacyOnlyPartner&&!treaty.Terms.Members.Contains(diplomacyPartner))continue;
                bool pending=treaty.Phase==TreatyPhase.Proposed || treaty.Phase==TreatyPhase.Accepted;
                if(diplomacyContractFilter==0 && !pending&&!GameSimulation.TreatyLive(treaty) || diplomacyContractFilter==1&&!pending || diplomacyContractFilter==2&&(pending||GameSimulation.TreatyLive(treaty)))continue;
                any=true;DrawTreatyCard(treaty);
            }
            if(!any)GUILayout.Label("Keine Vereinbarungen in dieser Auswahl.",bodyStyle);
            DrawEvacuationNotices();
        }
        private void DrawTreatyCard(TreatyState treaty)
        {
            bool expanded=expandedDiplomacyTreaty==treaty.Id;
            if(GUILayout.Button((expanded?"▾ ":"▸ ")+Definitions.TreatyName(treaty.Terms.Kind)+" · "+Definitions.TreatyStatus(treaty.Phase)+"  #"+treaty.Id,expanded?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight(48))))
            {expandedDiplomacyTreaty=expanded?0:treaty.Id;expanded=!expanded;confirmDiplomaticRisk=false;}
            if(!expanded){GUILayout.Space(6);return;}
            GUILayout.Label(DiplomaticMembers(treaty.Terms.Members),bodyStyle);GUILayout.Label(treaty.Reason,smallStyle);
            var world=simulation.FindPlanet(treaty.Terms.PlanetId);
            GUILayout.Label("Ort: "+(world==null?"Systemweit":world.Name)+" · Dauer "+FormatTime(treaty.Terms.DurationTicks*Definitions.TickSeconds)+" · Kündigungsfrist "+FormatTime(treaty.Terms.NoticeTicks*Definitions.TickSeconds),smallStyle);
            if(treaty.Terms.Rights!=AccessRight.None)GUILayout.Label("Zugangsrechte: "+RightsText(treaty.Terms.Rights),smallStyle);
            if(treaty.Terms.Kind==TreatyKind.Trade)
            {
                GUILayout.Label(treaty.Terms.Amount+" "+Definitions.ResourceName(treaty.Terms.Goods)+" gegen "+treaty.Terms.PaymentAmount+" "+Definitions.ResourceName(treaty.Terms.Payment),bodyStyle);
                GUILayout.Label("Geliefert: "+treaty.Delivered+" / "+treaty.PaymentDelivered+" · Hafen #"+treaty.Terms.SourcePort+" → #"+treaty.Terms.TargetPort,smallStyle);
            }
            if(treaty.Terms.Kind==TreatyKind.JointOperation)
                GUILayout.Label("Ziel: "+new[]{"Verteidigung","Eskorte","Eroberung"}[(int)treaty.Terms.Goal]+" · künftiger Eigentümer: "+DiplomaticFactionName(treaty.Terms.FutureOwner)+" · Eskortziel #"+treaty.Terms.EscortTarget,smallStyle);
            GUILayout.Space(8);
            if(treaty.Phase==TreatyPhase.Proposed)
            {
                if(!treaty.Approvals.Contains(PlayerFactionId))DiplomacyAction("Angebot annehmen",CommandKind.AcceptTreaty,treaty.Id);
                DiplomacyAction(treaty.Proposer==PlayerFactionId?"Angebot zurückziehen":"Angebot ablehnen",treaty.Proposer==PlayerFactionId?CommandKind.WithdrawTreaty:CommandKind.RejectTreaty,treaty.Id);
            }
            if(GameSimulation.TreatyLive(treaty))
            {
                bool assignable=false;
                foreach(var ship in simulation.State.Ships)if(ship.OwnerId==PlayerFactionId&&!ship.Destroyed&&!ship.Consumed)
                {
                    if(treaty.Terms.Kind==TreatyKind.Trade&&ship.Kind==ShipKind.Freighter)
                    {assignable=true;DiplomacyAction("Mit Frachter #"+ship.Id+" liefern",CommandKind.DispatchTrade,treaty.Id,ship.Id);}
                    if(treaty.Terms.Kind==TreatyKind.JointOperation&&!simulation.ShipPledged(ship.Id))
                    {assignable=true;DiplomacyAction(Definitions.Ships[(int)ship.Kind].Name+" #"+ship.Id+" zusagen",CommandKind.PledgeShip,treaty.Id,ship.Id);}
                }
                if(!assignable&&(treaty.Terms.Kind==TreatyKind.Trade||treaty.Terms.Kind==TreatyKind.JointOperation))GUILayout.Label("Aktuell kein passendes eigenes Schiff verfügbar.",smallStyle);
                if(treaty.Terms.Kind==TreatyKind.JointOperation)
                {
                    foreach(var pledge in treaty.Pledges)GUILayout.Label("Schiff #"+pledge.ShipId+" · bereit ab "+FormatTime(pledge.ReadyTick*Definitions.TickSeconds)+" · "+(pledge.Arrived?"angekommen":"unterwegs / Vorbereitung"),smallStyle);
                    DiplomacyAction("Gemeinsame Operation beginnen",CommandKind.StartJointOperation,treaty.Id);
                    DiplomacyAction("Operation abbrechen / zurückziehen",CommandKind.CancelJointOperation,treaty.Id);
                }
                if(treaty.Phase==TreatyPhase.Active)DiplomacyAction("Fristgerecht kündigen",CommandKind.TerminateTreaty,treaty.Id);
                bool risk=confirmDiplomaticRisk&&diplomacyRiskTreaty==treaty.Id;
                bool confirmed=GUILayout.Toggle(risk,"Vertragsbruch ausdrücklich freigeben",GUILayout.MinHeight(UiRowHeight(38)));
                if(confirmed!=risk){confirmDiplomaticRisk=confirmed;diplomacyRiskTreaty=treaty.Id;}
                if(confirmed)DiplomacyAction("Diesen Vertrag brechen",CommandKind.BreachTreaty,treaty.Id,confirmed:true);
            }
            GUILayout.Space(16);
        }
        private void DrawDiplomacyOffer()
        {
            GUILayout.Label("ANGEBOT VORBEREITEN",headingStyle);
            GUILayout.Label("1 · Art der Vereinbarung",bodyStyle);
            treatyType=GUILayout.SelectionGrid(treatyType,TreatyChoiceNames,2,buttonStyle,GUILayout.MinHeight(UiRowHeight(42)*4));
            GUILayout.Space(12);GUILayout.Label("2 · Geltungsort / Operationsziel",bodyStyle);
            foreach(var world in simulation.State.Planets)if(simulation.IsPlanetKnown(PlayerFactionId,world.Id))
                if(GUILayout.Button(world.Name,world.Id==diplomacyWorld?selectedButtonStyle:buttonStyle,GUILayout.MinHeight(UiRowHeight(36))))diplomacyWorld=world.Id;
            if(treatyType==(int)TreatyKind.Access||treatyType==(int)TreatyKind.Coalition)
            {
                GUILayout.Space(12);GUILayout.Label("3 · Zugangsrechte",bodyStyle);
                foreach(AccessRight right in new[]{AccessRight.CivilTransit,AccessRight.MilitaryTransit,AccessRight.Landing,AccessRight.Stationing,AccessRight.Repair,AccessRight.Supply})
                {bool value=GUILayout.Toggle((offeredRights&right)!=0,AccessLabel(right),GUILayout.MinHeight(UiRowHeight(34)));if(value)offeredRights|=right;else offeredRights&=~right;}
                GUILayout.Label("Rechte gelten am ausgewählten Ort. Koalitionssicht gilt systemweit; Waren bleiben beim jeweiligen Eigentümer.",smallStyle);
            }
            if(treatyType==(int)TreatyKind.Trade)
            {
                GUILayout.Space(12);GUILayout.Label("3 · Eigene Lieferung",bodyStyle);
                offeredGood=GUILayout.SelectionGrid(offeredGood,TradeGoodsNames,2,buttonStyle,GUILayout.MinHeight(UiRowHeight(40)*3));
                GUILayout.Label("Menge",smallStyle);offeredAmount=GUILayout.TextField(offeredAmount,4,GUILayout.MinHeight(UiRowHeight(34)));
                GUILayout.Label("4 · Gegenleistung des Partners",bodyStyle);
                paymentGood=GUILayout.SelectionGrid(paymentGood,TradeGoodsNames,2,buttonStyle,GUILayout.MinHeight(UiRowHeight(40)*3));
                GUILayout.Label("Menge",smallStyle);paymentAmount=GUILayout.TextField(paymentAmount,4,GUILayout.MinHeight(UiRowHeight(34)));
                GUILayout.Label("Verwendet den ersten eigenen und aktuell aufgeklärten Partnerhafen. Jede Seite liefert mit einem eigenen Frachter. Bei Kündigung kehrt geladene Ware zum Lieferanten zurück.",smallStyle);
            }
            if(treatyType==(int)TreatyKind.JointOperation)
            {
                GUILayout.Space(12);GUILayout.Label("3 · Gemeinsames Ziel",bodyStyle);
                jointGoal=GUILayout.SelectionGrid(jointGoal,new[]{"Verteidigen","Eskortieren","Erobern"},1,buttonStyle,GUILayout.MinHeight(UiRowHeight(40)*3));
                GUILayout.Label("Bei Eroberung erhält der Anbieter den Planeten. Schiffe zusagen und Operation beginnen folgen unter Koalition. Jeder Teilnehmer muss dem Krieg zustimmen.",smallStyle);
            }
            GUILayout.Space(14);GUILayout.Label("An "+DiplomaticFactionName(diplomacyPartner)+" · Laufzeit 10 Minuten · Kündigung 60 Sekunden",smallStyle);
            if(GUILayout.Button("Verbindliches Angebot senden",selectedButtonStyle,GUILayout.MinHeight(UiRowHeight(46))))SubmitDiplomacyOffer();
        }
        private void SubmitDiplomacyOffer()
        {
            var terms=new TreatyTerms{Kind=(TreatyKind)treatyType,Members=new List<int>{PlayerFactionId,diplomacyPartner},PlanetId=diplomacyWorld,Rights=offeredRights,Goal=(JointGoal)jointGoal,FutureOwner=PlayerFactionId};
            var coalition=simulation.CoalitionOf(PlayerFactionId);if(terms.Kind==TreatyKind.Coalition&&coalition!=null){terms.CoalitionId=coalition.Id;terms.Members=new List<int>(coalition.Terms.Members);if(!terms.Members.Contains(diplomacyPartner))terms.Members.Add(diplomacyPartner);}
            foreach(var world in simulation.State.Planets)foreach(var building in world.Buildings)if(building.Kind==BuildingKind.Spaceport&&building.Built&&!building.Destroyed)
            {if(GameSimulation.BuildingOwner(world,building)==PlayerFactionId&&terms.SourcePort==0)terms.SourcePort=building.Id;if(GameSimulation.BuildingOwner(world,building)==diplomacyPartner&&terms.TargetPort==0&&simulation.CanSeeEntity(world,PlayerFactionId,building.Id))terms.TargetPort=building.Id;}
            foreach(var ship in simulation.State.Ships)if(ship.OwnerId==PlayerFactionId&&!ship.Destroyed&&!ship.Consumed&&ship.Kind==ShipKind.Freighter){terms.EscortTarget=ship.Id;break;}
            int amount,payment;if(int.TryParse(offeredAmount,out amount))terms.Amount=amount;if(int.TryParse(paymentAmount,out payment))terms.PaymentAmount=payment;terms.Goods=(ResourceKind)offeredGood;terms.Payment=(ResourceKind)paymentGood;
            RequestDiplomacy(new SimCommand{Kind=CommandKind.ProposeTreaty,Terms=terms});
        }
        private void DrawCoalitionWorkspace()
        {
            GUILayout.Label("KOALITION UND OPERATIONEN",headingStyle);var coalition=simulation.CoalitionOf(PlayerFactionId);
            GUILayout.Label(coalition==null?"Du gehörst derzeit keiner Koalition an.":"Mitglieder: "+DiplomaticMembers(coalition.Terms.Members),bodyStyle);
            GUILayout.Label("Partner behalten eigene Vorräte und Armeen. Gemeinsame Sicht und vereinbarte Rechte verbinden die Zusammenarbeit.",smallStyle);
            if(GUILayout.Button(coalition==null?"Koalition vorschlagen":"Gewählten Partner zur Koalition vorschlagen",buttonStyle,GUILayout.MinHeight(UiRowHeight(42))))
            {treatyType=(int)TreatyKind.Coalition;OpenDiplomacySection(2);}
            if(GUILayout.Button("Gemeinsame Operation planen",buttonStyle,GUILayout.MinHeight(UiRowHeight(42))))
            {treatyType=(int)TreatyKind.JointOperation;OpenDiplomacySection(2);}
            GUILayout.Space(14);bool any=false;
            foreach(var treaty in simulation.State.Treaties)
                if(treaty.Terms.Members.Contains(PlayerFactionId)&&(treaty.Terms.Kind==TreatyKind.Coalition||treaty.Terms.Kind==TreatyKind.JointOperation)&&(GameSimulation.TreatyLive(treaty)||treaty.Phase==TreatyPhase.Proposed||treaty.Phase==TreatyPhase.Accepted))
                {any=true;DrawTreatyCard(treaty);}
            if(!any)GUILayout.Label("Laufende Koalitionsangebote und Operationen erscheinen hier.",smallStyle);
        }
        private void DrawEvacuationNotices()
        {
            foreach(var evacuation in simulation.State.Evacuations)if(evacuation.Guest==PlayerFactionId||evacuation.Host==PlayerFactionId)
            {
                var world=simulation.FindPlanet(evacuation.PlanetId);
                GUILayout.Space(10);GUILayout.Label("Räumung: "+(world==null?"Welt #"+evacuation.PlanetId:world.Name),headingStyle);
                GUILayout.Label(Math.Max(0,(evacuation.UntilTick-simulation.State.Tick)*Definitions.TickSeconds).ToString("F0")+" Sekunden · "+evacuation.Units.Count+" Truppen. Abzug zum eigenen Träger und Verladung; während der Frist beidseitiger Schutz.",smallStyle);
            }
        }
        private void DrawGateWorkspace()
        {
            GUILayout.Label("SPRUNGTORPROJEKT",headingStyle);
            GUILayout.Label("1 · Elektronik und Sensoren erforschen",bodyStyle);
            GUILayout.Label("Der bauliche Betreiber benötigt beide Technologien.",smallStyle);
            GUILayout.Label("2 · Tor bauen und Komponenten anliefern",bodyStyle);
            for(int i=0;i<Definitions.GateComponents.Length;i++)if(Definitions.GateComponents[i]>0)GUILayout.Label(Definitions.GateComponents[i]+" "+Definitions.ResourceName((ResourceKind)i),smallStyle);
            GUILayout.Label("3 · Aktivieren und "+FormatTime(Definitions.GateSecureTicks*Definitions.TickSeconds)+" sichern",bodyStyle);
            GUILayout.Label("Energie, Personal, Nahrung und Kontrolle müssen bestehen. Mitgliederwechsel startet die Sicherungsphase neu.",smallStyle);
            GUILayout.Space(14);bool any=false;
            foreach(var world in simulation.State.Planets)foreach(var building in world.Buildings)
                if(building.Kind==BuildingKind.JumpGate&&GameSimulation.BuildingOwner(world,building)==PlayerFactionId)
                {any=true;DiplomacyAction(world.Name+" · Tor #"+building.Id+" aktivieren",CommandKind.ActivateGate,building.Id);}
            if(!any)GUILayout.Label("Noch kein eigenes Sprungtor vorhanden. Der Baukatalog zeigt die Voraussetzungen.",smallStyle);
            foreach(var project in simulation.State.GateProjects)
            {
                GUILayout.Space(14);GUILayout.Label("Tor #"+project.BuildingId+" · "+DiplomaticFactionName(project.OwnerId),headingStyle);
                GUILayout.Label(project.Reason,bodyStyle);
                GUILayout.Label("Sicherung: "+FormatTime(project.SecureTicks*Definitions.TickSeconds)+" / "+FormatTime(Definitions.GateSecureTicks*Definitions.TickSeconds),smallStyle);
                GUILayout.Label("Teilnehmer: "+DiplomaticMembers(project.Participants),smallStyle);
            }
            if(simulation.State.Winners.Count>0){GUILayout.Space(14);GUILayout.Label(ScenarioOutcomeText,headingStyle);}
        }
        private static string AccessLabel(AccessRight r){switch(r){case AccessRight.CivilTransit:return "Ziviler Transit";case AccessRight.MilitaryTransit:return "Militärischer Transit / Tore";case AccessRight.Landing:return "Militärische Landung";case AccessRight.Stationing:return "Stationierung";case AccessRight.Repair:return "Hafenreparatur";default:return "Feldversorgung";}}
        private static string RightsText(AccessRight rights){var labels=new List<string>();foreach(AccessRight r in Enum.GetValues(typeof(AccessRight)))if(r!=AccessRight.None&&(rights&r)!=0)labels.Add(AccessLabel(r));return labels.Count==0?"Keine Zugangsrechte":string.Join(", ",labels);}
    }
}
