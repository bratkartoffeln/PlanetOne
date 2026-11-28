using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed class DiplomaticObservation
    {
        public AiObservation World;
        public List<TreatyState> Treaties=new List<TreatyState>();
        public List<int> Partners=new List<int>();
        public Dictionary<int,int> Trust=new Dictionary<int,int>();
        public List<GateProject> Gates=new List<GateProject>();
    }
    public static partial class AiPlanner
    {
        public static AiIntent PlanDiplomacy(DiplomaticObservation v,AiState ai)
        {
            int own=v.World.FactionId;
            foreach(var t in v.Treaties)if(t.Phase==TreatyPhase.Proposed&&!t.Approvals.Contains(own))
            {
                bool accept=v.Trust[t.Proposer]>=-10;
                int rejection=accept?0:1;
                string reason=accept?"Verlässlicher Partner und erfüllbare Verpflichtung":"Öffentlich bekannter Vertragsbruch mindert Vertrauen";
                if(t.Terms.Kind==TreatyKind.Trade)
                {
                    var port=FindOwnPort(v.World,t.Terms.TargetPort);int resource=(int)t.Terms.Payment;
                    accept=accept&&port!=null&&port.Inventory[resource]-port.ReservedOutgoing[resource]>=t.Terms.PaymentAmount;
                    if(!accept){reason="Gegenleistung aus eigenen freien Hafenbeständen nicht tragbar";rejection=2;}
                }
                if(t.Terms.Kind==TreatyKind.JointOperation)
                {
                    accept=accept&&v.World.Ships.Exists(s=>s.OwnerId==own&&Definitions.SpaceWeapons[(int)s.Kind].Damage>0);
                    if(t.Terms.Goal==JointGoal.Invade)accept=accept&&v.World.Ships.Exists(s=>s.OwnerId==own&&!ai.Operation.Ships.Contains(s.Id)&&s.Kind==ShipKind.LandingTransport&&s.Troops.Count>=3&&s.Troops.Exists(u=>u.Kind==UnitKind.Engineer)&&s.Cargo[0]>=8&&s.Cargo[3]>=8);
                    if(!accept){reason="Zugesagte Kräfte derzeit nicht tatsächlich verfügbar";rejection=3;}
                }
                return Order("Verpflichtung bewerten",reason,new SimCommand{Kind=accept?CommandKind.AcceptTreaty:CommandKind.RejectTreaty,TargetId=t.Id,IntValue=rejection});
            }
            foreach(var t in v.Treaties)if(GameSimulation.TreatyLive(t))
            {
                if(t.Phase==TreatyPhase.Active&&t.Terms.Members.Exists(f=>f!=own&&v.Trust[f]<-10))return Order("Vertrauen schützen","Bekannter Vertragsbruch: geordnete Kündigung statt Überraschungsangriff",new SimCommand{Kind=CommandKind.TerminateTreaty,TargetId=t.Id});
                if(t.Terms.Kind==TreatyKind.Trade)
                {
                    bool payment=t.Proposer!=own;int remaining=payment?t.Terms.PaymentAmount-t.PaymentDelivered:t.Terms.Amount-t.Delivered;
                    foreach(var s in v.World.Ships)if(s.Freight!=null&&s.Freight.TreatyId==t.Id&&s.Freight.PaymentLeg==payment)remaining-=s.Freight.Amount;
                    var ship=v.World.Ships.Find(s=>s.OwnerId==own&&s.Kind==ShipKind.Freighter&&s.Freight==null&&Array.TrueForAll(s.Cargo,n=>n==0));
                    if(remaining>0&&ship!=null)return Order("Handel erfüllen","Realen freien Frachter für vereinbarte Lieferung einsetzen",new SimCommand{Kind=CommandKind.DispatchTrade,TargetId=t.Id,ActorId=ship.Id});
                    var finishing=v.World.Ships.Find(s=>s.Id==v.World.TradeFreighterId&&s.AutoSupply);
                    if(remaining>0&&finishing!=null)return Order("Frachter für Vertrag freistellen","Laufende Fracht abschließen; anschließend zugesagte Gegenleistung liefern",new SimCommand {Kind=CommandKind.SetAutoSupply,ActorId=finishing.Id,PlanetId=finishing.TargetPlanetId,BoolValue=false});
                    if(remaining>0&&!v.World.Ships.Exists(s=>s.OwnerId==own&&s.Kind==ShipKind.Freighter)&&v.World.Research.Completed.Contains(Technology.Transport))
                        foreach(var world in v.World.Worlds)if(world.OwnerId==own)
                        {
                            var port=world.Buildings.Find(b=>Own(b,v.World)&&b.Kind==BuildingKind.Spaceport&&b.Built&&b.ShipBuildKind<0);
                            if(port!=null&&world.Population>=Definitions.Ships[(int)ShipKind.Freighter].Crew+4)
                                return Order("Vertragsfracht beschaffen","Auch eine einzelne Kolonie benötigt einen bezahlten Frachter für zugesagte Lieferungen",new SimCommand{Kind=CommandKind.BuildShip,PlanetId=world.Id,TargetId=port.Id,IntValue=(int)ShipKind.Freighter});
                        }
                }
                if(t.Terms.Kind==TreatyKind.JointOperation&&t.OperationPhase<OperationPhase.Executing)
                {
                    int needed=t.Terms.Goal==JointGoal.Invade?2:1;
                    if(t.Pledges.FindAll(p=>p.FactionId==own).Count<needed)
                    {
                        var ship=v.World.Ships.Find(s=>s.OwnerId==own&&!t.Pledges.Exists(p=>p.ShipId==s.Id)&&!ai.Operation.Ships.Contains(s.Id)&&(t.Terms.Goal==JointGoal.Invade&&!t.Pledges.Exists(p=>p.FactionId==own)?s.Kind==ShipKind.LandingTransport&&s.Troops.Count>=3:Definitions.SpaceWeapons[(int)s.Kind].Damage>0));
                        if(ship!=null)return Order("Hilfe zusagen","Verfügbare Träger und Eskorte exklusiv bereitstellen",new SimCommand{Kind=CommandKind.PledgeShip,TargetId=t.Id,ActorId=ship.Id});
                    }
                    if(t.Terms.Members.TrueForAll(f=>t.Pledges.FindAll(p=>p.FactionId==f).Count>=needed)&&t.Pledges.TrueForAll(p=>p.ReadyTick<=v.World.Tick))return Order("Gemeinsame Hilfe beginnen","Alle Beiträge wurden bereitgestellt",new SimCommand{Kind=CommandKind.StartJointOperation,TargetId=t.Id});
                }
            }
            if(v.World.Research.Completed.Contains(Technology.Sensors))foreach(var world in v.World.Worlds)if(world.OwnerId==own)
            {
                var gate=world.Buildings.Find(b=>b.Kind==BuildingKind.JumpGate&&b.OwnerId==own);
                if(gate==null&&world.Energy-world.EnergyDemand>=8&&world.Workers-world.WorkersUsed>=4)
                {var build=Build(v.World,ai,world,BuildingKind.JumpGate,"Eigene Forschung und freie Betriebskapazität erlauben das gemeinsame Szenarioziel");if(build!=null)return build;}
                if(gate!=null&&gate.Built&&gate.Operational&&!v.Gates.Exists(g=>g.BuildingId==gate.Id&&(g.Activating||g.Complete)))
                {bool ready=true;for(int r=0;r<Definitions.ResourceCount;r++)if(gate.Inventory[r]-gate.ReservedOutgoing[r]<Definitions.GateComponents[r])ready=false;if(ready)return Order("Sprungtor sichern","Alle Komponenten wurden physisch angeliefert; Aktivierung öffentlich ankündigen",new SimCommand{Kind=CommandKind.ActivateGate,TargetId=gate.Id});}
            }
            foreach(int partner in v.Partners)
            {
                if(v.World.Enemies.Contains(partner)||v.Trust[partner]<0||v.Treaties.Exists(t=>t.Phase==TreatyPhase.Proposed&&t.Terms.Members.Contains(partner)))continue;
                bool nonAggression=v.Treaties.Exists(t=>GameSimulation.TreatyLive(t)&&t.Terms.Kind==TreatyKind.NonAggression&&t.Terms.Members.Contains(partner));
                if(!nonAggression&&!v.Treaties.Exists(t=>t.Terms.Kind==TreatyKind.NonAggression&&t.Terms.Members.Contains(partner)))return Offer(own,partner,TreatyKind.NonAggression,"Sichere Grenzen ermöglichen eigenen Wirtschaftsaufbau");
                bool coalition=v.Treaties.Exists(t=>GameSimulation.TreatyLive(t)&&t.Terms.Kind==TreatyKind.Coalition);
                if(nonAggression&&!coalition&&!v.Treaties.Exists(t=>t.Terms.Kind==TreatyKind.Coalition&&t.Terms.Members.Contains(partner)))return Offer(own,partner,TreatyKind.Coalition,"Friedliche Zusammenarbeit ermöglicht gemeinsame Aufklärung und Torziel");
                var local=v.World.Worlds.Find(w=>w.OwnerId==own&&w.Buildings.Exists(b=>b.Kind==BuildingKind.Spaceport&&b.Built));
                var remote=v.World.Worlds.Find(w=>w.OwnerId==partner&&w.Buildings.Exists(b=>b.Kind==BuildingKind.Spaceport&&b.Built));
                if(local!=null&&remote!=null&&!v.Treaties.Exists(t=>t.Terms.Kind==TreatyKind.Trade&&t.Terms.Members.Contains(partner)))
                {
                    var source=local.Buildings.Find(b=>b.Kind==BuildingKind.Spaceport&&b.Built);var target=remote.Buildings.Find(b=>b.Kind==BuildingKind.Spaceport&&b.Built);
                    if(source.Inventory[2]-source.ReservedOutgoing[2]>=12&&v.World.Ships.Exists(s=>s.Kind==ShipKind.Freighter&&s.OwnerId==own))
                        return Order("Lieferbeziehung aufbauen","Eigener Metallüberschuss gegen zwei Bauteile; Partner prüft seine Gegenleistung",new SimCommand{Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms{Kind=TreatyKind.Trade,Members=new List<int>{own,partner},SourcePort=source.Id,TargetPort=target.Id}});
                }
                if(coalition&&remote!=null&&v.World.Ships.Exists(s=>s.OwnerId==own&&Definitions.SpaceWeapons[(int)s.Kind].Damage>0)&&!v.Treaties.Exists(t=>t.Terms.Kind==TreatyKind.JointOperation&&t.Terms.Members.Contains(partner)))
                    return Order("Partner schützen","Konkrete bewaffnete Hilfe am bekannten Partnerhafen anbieten",new SimCommand{Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms{Kind=TreatyKind.JointOperation,Members=new List<int>{own,partner},PlanetId=remote.Id,FutureOwner=partner,Goal=JointGoal.Defend}});
            }
            return null;
        }
        private static BuildingState FindOwnPort(AiObservation v,long id){foreach(var w in v.Worlds)foreach(var b in w.Buildings)if(b.Id==id&&b.OwnerId==v.FactionId)return b;return null;}
        private static AiIntent Offer(int own,int other,TreatyKind kind,string reason)
        {return Order("Kooperation anbieten",reason,new SimCommand{Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms{Kind=kind,Members=new List<int>{own,other}}});}
    }
    public sealed partial class GameSimulation
    {
        public DiplomaticObservation ObserveDiplomacy(int faction,AiObservation world=null)
        {
            var v=new DiplomaticObservation{World=world??ObserveForAi(faction)};
            foreach(var f in State.Factions)if(FactionActive(f.Id)){if(f.Id!=faction)v.Partners.Add(f.Id);v.Trust[f.Id]=Trust(faction,f.Id);}
            foreach(var t in State.Treaties)if(t.Terms.Members.Contains(faction))v.Treaties.Add(CopyAiValue(t));
            foreach(var g in State.GateProjects)if(g.OwnerId==faction||g.Activating||g.Complete)v.Gates.Add(CopyAiValue(g));return v;
        }
    }
}
