using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public static partial class AiPlanner
    {
        private static void Remember(AiObservation v,AiState ai)
        {
            foreach(var w in v.Worlds)
            {
                ai.Contacts.RemoveAll(c=>c.PlanetId==w.Id && w.Visible[c.Y*w.Width+c.X] && !w.Buildings.Exists(b=>b.Id==c.Id&&b.OwnerId==c.OwnerId));
                foreach(var b in w.Buildings)if(b.OwnerId!=v.FactionId)
                {
                    var c=ai.Contacts.Find(a=>a.Id==b.Id);
                    if(c==null){c=new AiContact {Id=b.Id};ai.Contacts.Add(c);}
                    c.PlanetId=w.Id;c.OwnerId=b.OwnerId;c.Kind=b.Kind;c.X=b.X;c.Y=b.Y;c.Tick=v.Tick;
                }
            }
        }
        private static void Phase(AiObservation v,AiState ai,OperationPhase phase,string reason)
        {if(ai.Operation.Phase!=phase){ai.Operation.ChangedTick=v.Tick;ai.Operation.Phase=phase;}ai.Operation.Reason=reason;}
        private static AiIntent Defend(AiObservation v,AiState ai)
        {
            foreach(var w in v.Worlds)if(w.OwnerId==v.FactionId)
            {
                var enemy=w.Units.Find(u=>v.Enemies.Contains(u.OwnerId));
                var depot=w.Buildings.Find(b=>Own(b,v)&&b.Kind==BuildingKind.MilitaryDepot&&b.Operational);
                foreach(var u in w.Units)if(u.OwnerId==v.FactionId&&!ai.Operation.Units.Contains(u.Id))
                {
                    if((u.Ammunition==0||u.Food==0||u.Health<Definitions.Get(u.Kind).Health*0.4)&&depot!=null)
                    {
                        if(u.Order!=UnitOrder.Resupply||u.TargetId!=depot.Id)return Order("Reserve erhalten","Vorräte oder Gesundheit reichen nicht für weiteren Angriff",new SimCommand {Kind=CommandKind.ResupplyUnit,PlanetId=w.Id,ActorId=u.Id,TargetId=depot.Id});
                    }
                    else if(enemy!=null && u.Kind!=UnitKind.Engineer && (u.Order!=UnitOrder.Attack||u.TargetId!=enemy.Id))
                        return Order("Heimat verteidigen","Aktuell aufgeklärte feindliche Bodeneinheit bedroht eigene Anlagen",new SimCommand {Kind=CommandKind.AttackTarget,PlanetId=w.Id,ActorId=u.Id,TargetId=enemy.Id});
                    if(u.Kind==UnitKind.Engineer&&u.Parts>0)
                    {
                        var hurt=w.Units.Find(a=>a.OwnerId==v.FactionId&&a.Health<Definitions.Get(a.Kind).Health*0.75);
                        if(hurt!=null&&(u.Order!=UnitOrder.Repair||u.TargetId!=hurt.Id))return Order("Reserve reparieren","Pionier verwendet mitgeführte Bauteile",new SimCommand {Kind=CommandKind.RepairTarget,PlanetId=w.Id,ActorId=u.Id,TargetId=hurt.Id});
                    }
                }
            }
            foreach(var s in v.Ships)if(s.OwnerId==v.FactionId&&!ai.Operation.Ships.Contains(s.Id)&&s.Kind>=ShipKind.Scout&&s.TransportPhase==TransportPhase.Space)
            {
                var yard=v.Ships.Find(a=>a.OwnerId==v.FactionId&&a.Kind==ShipKind.OrbitalYard);
                if(yard!=null&&(s.Health<Definitions.SpaceWeapons[(int)s.Kind].Health*0.5||s.Ammunition==0&&Definitions.SpaceWeapons[(int)s.Kind].Ammo>0)&&s.Order!=ShipOrder.Repair)
                    return Order("Flotte erhalten","Beschädigtes oder leergeschossenes Schiff zur bezahlten Werftreparatur",new SimCommand {Kind=CommandKind.RepairShip,ActorId=s.Id,TargetId=yard.Id,PlanetId=yard.TargetPlanetId});
            }
            return null;
        }
        private static bool Free(AiWorld w,int x,int y)
        {return x>=0&&y>=0&&x<w.Width&&y<w.Height&&w.Terrain[y*w.Width+x]==0&&!w.Buildings.Exists(b=>b.X==x&&b.Y==y);}
        private static GridCell Near(AiWorld w,int x,int y)
        {for(int d=0;d<6;d++){var n=HexGrid.Neighbor(x,y,d);if(Free(w,n.X,n.Y))return n;}return new GridCell(x,y);}
        private static AiIntent Move(AiWorld w,UnitState u,GridCell goal,bool retreat=false)
        {
            if(HexGrid.Distance(u.X,u.Y,goal.X,goal.Y)<0.15 || u.GoalX==goal.X&&u.GoalY==goal.Y&&(u.Order==UnitOrder.Move||u.Order==UnitOrder.Retreat))return null;
            return Order(retreat?"Rückzug":"Verband sammeln",retreat?"Überlebende fahren tatsächlich zum Evakuierungspunkt":"Zielweg durch reguläre Bodennavigation",new SimCommand {Kind=retreat?CommandKind.RetreatUnit:CommandKind.MoveUnit,PlanetId=w.Id,ActorId=u.Id,X=goal.X,Y=goal.Y});
        }
        private static bool ChooseLanding(AiObservation v,AiState ai,AiWorld target)
        {
            double best=double.MaxValue;GridCell? chosen=null;
            for(int y=1;y<target.Height-1;y++)for(int x=1;x<target.Width-1;x++)
            {
                if(ai.Operation.RejectedLandings.Exists(c=>c.X==x&&c.Y==y))continue;
                bool free=true;foreach(var c in HexGrid.LandingCells(x,y))if(!Free(target,c.X,c.Y))free=false;
                if(!free)continue;
                double risk=0;
                foreach(var c in ai.Contacts)if(c.PlanetId==target.Id&&v.Enemies.Contains(c.OwnerId))
                {
                    double distance=HexGrid.Distance(x,y,c.X,c.Y);
                    if(Definitions.IsTower(c.Kind)&&distance<=Definitions.GetTower(c.Kind).Range+2)risk+=100;
                    if(c.Kind==BuildingKind.ShieldGenerator&&distance<=Definitions.ShieldRadius+1)risk+=1000;
                }
                double score=risk+HexGrid.Distance(x,y,8,6);
                if(ai.Operation.LandingChosen&&x==ai.Operation.LandingX&&y==ai.Operation.LandingY)score-=0.1;
                if(score<best){best=score;chosen=new GridCell(x,y);}
            }
            if(!chosen.HasValue||best>=100)return false;
            ai.Operation.LandingX=chosen.Value.X;ai.Operation.LandingY=chosen.Value.Y;ai.Operation.LandingChosen=true;return true;
        }
        private static AiIntent Military(AiObservation v,AiState ai)
        {
            var op=ai.Operation;
            if(op.Phase==OperationPhase.Aborting)return Abort(v,ai);
            var home=v.Worlds.Find(w=>w.OwnerId==v.FactionId && w.Buildings.Exists(b=>Own(b,v)&&b.Kind==BuildingKind.Spaceport&&b.Built));
            if(home==null)return null;
            if(op.Phase==OperationPhase.Completed||op.Phase==OperationPhase.Cancelled||op.Phase==OperationPhase.Failed)
            {if(v.Tick<op.RetryAfter)return null;var rejected=op.RejectedLandings;ai.Operation=op=new AiOperation {RejectedLandings=rejected};}
            var target=v.Worlds.Find(w=>w.Id==op.TargetPlanetId);
            if(target==null || !v.Enemies.Contains(target.OwnerId))
            {
                if(target!=null&&target.OwnerId==v.FactionId){Phase(v,ai,OperationPhase.Completed,"Ziel durch tatsächliche Besetzung erreicht");op.Ships.Clear();op.Units.Clear();op.FoodReserved=op.PartsReserved=0;op.RetryAfter=v.Tick+Definitions.AiRetryTicks;return null;}
                if(op.Ships.Count>0){Phase(v,ai,OperationPhase.Aborting,"Ziel oder Kriegsgrundlage nicht mehr gültig");return Abort(v,ai);}
                target=v.Worlds.Find(w=>v.Enemies.Contains(w.OwnerId));
                if(target==null)return null;
                op.TargetPlanetId=target.Id;op.HomePlanetId=home.Id;
            }
            var carrier=v.Ships.Find(s=>s.OwnerId==v.FactionId&&s.Kind==ShipKind.LandingTransport && (op.Ships.Count==0||op.Ships.Contains(s.Id)));
            var escort=v.Ships.Find(s=>s.OwnerId==v.FactionId&&s.Kind==ShipKind.EscortFrigate);
            if(op.Phase==OperationPhase.Executing)
            {
                if(carrier==null||escort==null||carrier.Health<Definitions.SpaceWeapons[(int)carrier.Kind].Health*0.45||escort.Health<Definitions.SpaceWeapons[(int)escort.Kind].Health*0.3)
                {Phase(v,ai,OperationPhase.Aborting,"Träger/Eskorte verloren oder Verband zu stark beschädigt");return Abort(v,ai);}
                return Execute(v,ai,target,carrier,escort);
            }
            if(op.Phase!=OperationPhase.Ready)Phase(v,ai,OperationPhase.Preparing,"Kräfte, Transportplätze und Vorräte prüfen");
            if(op.Ships.Count>0&&(carrier==null||escort==null))
            {Phase(v,ai,OperationPhase.Aborting,"Bereits reservierter Träger oder Eskorte während der Vorbereitung verloren");return Abort(v,ai);}
            if(carrier==null||escort==null)return new AiIntent("Invasion vorbereiten","Echter Träger oder Eskorte fehlt; Start gesperrt","Werft beschafft fehlende Transportkapazität");
            if(carrier.TransportPhase!=TransportPhase.Space)return null;
            if(!op.Ships.Contains(carrier.Id)){op.Ships.Add(carrier.Id);op.Ships.Add(escort.Id);}
            if(!AtKnownDock(carrier,home))
            {if(carrier.Order!=ShipOrder.Fly||carrier.TargetPlanetId!=home.Id)return Order("Träger sammeln","Beladung nur am tatsächlichen eigenen Hafen",new SimCommand {Kind=CommandKind.FlyShip,ActorId=carrier.Id,PlanetId=home.Id});return null;}
            var port=home.Buildings.Find(b=>Own(b,v)&&b.Kind==BuildingKind.Spaceport&&b.Operational);if(port==null)return null;
            int[] supply={12,12};int[] resources={0,3};
            for(int i=0;i<2;i++)
            {
                int r=resources[i];
                if(carrier.Cargo[r]>=supply[i])continue;
                if(port.Supply.Target[r]<supply[i]+4)return Order("Landung versorgen","Physische Hafenzufuhr für Nahrung und Bauteile",new SimCommand {Kind=CommandKind.SetSupplyPolicy,PlanetId=home.Id,TargetId=port.Id,IntValue=r,Minimum=8,Amount=16,Reserve=4,X=2});
                int available=Math.Min(supply[i]-carrier.Cargo[r],port.Inventory[r]-port.ReservedOutgoing[r]);
                if(available>0)return Order("Träger beladen","Tatsächliche Hafenware in begrenzten Frachtraum übertragen",new SimCommand {Kind=CommandKind.LoadShipSupplies,PlanetId=home.Id,ActorId=carrier.Id,TargetId=port.Id,IntValue=r,Amount=available});
            }
            op.FoodReserved=carrier.Cargo[0];op.PartsReserved=carrier.Cargo[3];
            foreach(var kind in new[]{UnitKind.Engineer,UnitKind.Infantry,UnitKind.Infantry,UnitKind.Artillery})
            {
                int need=kind==UnitKind.Infantry?2:1;
                if(carrier.Troops.FindAll(troop=>troop.Kind==kind).Count>=need)continue;
                var u=home.Units.Find(a=>a.OwnerId==v.FactionId&&a.Kind==kind&&!a.Destroyed);
                if(u==null)continue;
                if(!op.Units.Contains(u.Id))op.Units.Add(u.Id);
                if(u.Kind==UnitKind.Engineer&&u.Parts<4||u.Food==0||u.Ammunition==0)
                {
                    var depot=home.Buildings.Find(b=>Own(b,v)&&b.Kind==BuildingKind.MilitaryDepot&&b.Operational);
                    if(depot!=null&&(u.Order!=UnitOrder.Resupply||u.TargetId!=depot.Id))return Order("Truppen versorgen","Pioniermaterial und Rationen müssen real abgeholt werden",new SimCommand {Kind=CommandKind.ResupplyUnit,PlanetId=home.Id,ActorId=u.Id,TargetId=depot.Id});
                    continue;
                }
                if(HexGrid.Distance(u.X,u.Y,port.X,port.Y)<=1.5)return Order("Truppen verladen","Bestehende Einheit mit ihrer Identität in realen Transportplatz",new SimCommand {Kind=CommandKind.LoadTroop,ActorId=carrier.Id,TargetId=u.Id,PlanetId=home.Id});
                var move=Move(home,u,Near(home,port.X,port.Y));if(move!=null)return move;
            }
            if(carrier.Troops.Count<3||!carrier.Troops.Exists(u=>u.Kind==UnitKind.Engineer)||carrier.Cargo[0]<8||carrier.Cargo[3]<8)
                return new AiIntent("Invasion vorbereiten","Infanterie, Pionier oder endliche Startversorgung fehlt","Auf bezahlte Verstärkung warten");
            foreach(var u in carrier.Troops)if(!op.Units.Contains(u.Id))op.Units.Add(u.Id);
            if(!ChooseLanding(v,ai,target)){Phase(v,ai,OperationPhase.Suspended,"Keine bekannte vertretbare Landezone; Stellung nicht blind wiederholen");return new AiIntent("Angriff verschieben",op.Reason,"Aufklärung, andere Fläche oder Belagerung");}
            if(escort.Order!=ShipOrder.Escort||escort.EscortId!=carrier.Id)return Order("Eskorte zuweisen","Reservierte Eskorte schützt den real beladenen Träger",new SimCommand {Kind=CommandKind.EscortShip,ActorId=escort.Id,TargetId=carrier.Id,PlanetId=home.Id});
            if(op.Phase!=OperationPhase.Ready){Phase(v,ai,OperationPhase.Ready,"Kräfte, Plätze, Landealternative und Versorgung vorhanden");return new AiIntent("Angriff bereit",op.Reason,"Vor Abflug erneut prüfen");}
            Phase(v,ai,OperationPhase.Executing,"Versorgten Verband zur aufgeklärten Zielwelt entsenden");
            return Order("Invasion ausführen",op.Reason,new SimCommand {Kind=CommandKind.FlyShip,ActorId=carrier.Id,PlanetId=target.Id});
        }

        private static AiIntent Execute(AiObservation v,AiState ai,AiWorld target,ShipState carrier,ShipState escort)
        {
            var op=ai.Operation;
            if(carrier.TransportPhase==TransportPhase.Space)
            {
                if(!target.Current||SpaceDistance(carrier,target)>Definitions.OrbitalRadius)return null;
                if(!ChooseLanding(v,ai,target)){Phase(v,ai,OperationPhase.Aborting,"Aufgeklärte Verteidigung sperrt sichere Landeflächen");return Abort(v,ai);}
                return Order("Andere Landezone prüfen","Bekannte Türme und Schilde meiden; unbekannte Hindernisse bleiben Befehlsprüfung",new SimCommand {Kind=CommandKind.LandTransport,ActorId=carrier.Id,PlanetId=target.Id,X=op.LandingX,Y=op.LandingY});
            }
            if(carrier.TransportPhase!=TransportPhase.Landed)return null;
            if(carrier.Troops.Count>0)return Order("Truppen entladen","Identitäten nur einmal auf der Oberfläche aktivieren",new SimCommand {Kind=CommandKind.UnloadTroop,ActorId=carrier.Id,PlanetId=target.Id,TargetId=carrier.Troops[0].Id});
            var engineer=target.Units.Find(u=>op.Units.Contains(u.Id)&&u.Kind==UnitKind.Engineer);
            if(engineer!=null&&!target.Beachheads.Exists(h=>h.OwnerId==v.FactionId))
            {var cell=HexGrid.CellAt(engineer.X,engineer.Y);return Order("Landekopf aufbauen","Pionier bezahlt Brückenkopf aus mitgeführten Vorräten",new SimCommand {Kind=CommandKind.EstablishBeachhead,ActorId=engineer.Id,PlanetId=target.Id,X=cell.X,Y=cell.Y});}
            var hq=target.Buildings.Find(b=>v.Enemies.Contains(b.OwnerId)&&b.Kind==BuildingKind.Headquarters);
            foreach(var u in target.Units)if(op.Units.Contains(u.Id))
            {
                if(u.Health<Definitions.Get(u.Kind).Health*0.35 || u.Food==0)
                {Phase(v,ai,OperationPhase.Aborting,"Bodenverband beschädigt oder unversorgt; Evakuierung statt neuer kostenloser Welle");return Abort(v,ai);}
                if(u.Kind==UnitKind.Engineer)continue;
                var threat=target.Buildings.Find(b=>v.Enemies.Contains(b.OwnerId)&&Definitions.IsTower(b.Kind));
                if(threat!=null)
                {
                    if(u.Kind==UnitKind.Artillery)
                    {
                        if(u.Order!=UnitOrder.Attack||u.TargetId!=threat.Id)return Order("Stellung aufbrechen","Aufgeklärte Türme mit vorhandener Artillerie bekämpfen",new SimCommand {Kind=CommandKind.AttackTarget,ActorId=u.Id,TargetId=threat.Id,PlanetId=target.Id});
                        // Keep the existing siege order until the observed threat is gone.
                        // Falling through to the rally move abandons its firing position.
                        continue;
                    }
                    if(u.Kind!=UnitKind.Artillery && !target.Units.Exists(a=>op.Units.Contains(a.Id)&&a.Kind==UnitKind.Artillery))
                    {Phase(v,ai,OperationPhase.Aborting,"Aufgeklärte Turmstellung benötigt fehlende Belagerung; Angriff abbrechen");return Abort(v,ai);}
                    if(u.Kind!=UnitKind.Artillery)continue;
                }
                if(hq!=null&&u.Kind==UnitKind.Infantry&&HexGrid.Distance(u.X,u.Y,hq.X,hq.Y)<=1.5)
                {if(hq.CapturingFaction!=v.FactionId)return Order("Verwaltung sichern","Infanterie und tatsächlicher Landekopf übernehmen das Ziel",new SimCommand {Kind=CommandKind.CaptureBuilding,ActorId=u.Id,TargetId=hq.Id,PlanetId=target.Id});continue;}
                var move=Move(target,u,Near(target,hq==null?8:hq.X,hq==null?6:hq.Y));if(move!=null)return move;
            }
            return null;
        }
        private static AiIntent Abort(AiObservation v,AiState ai)
        {
            var op=ai.Operation;var home=v.Worlds.Find(w=>w.Id==op.HomePlanetId&&w.OwnerId==v.FactionId&&w.Buildings.Exists(b=>Own(b,v)&&b.Kind==BuildingKind.Spaceport&&b.Built));
            if(home==null)home=v.Worlds.Find(w=>w.OwnerId==v.FactionId&&w.Buildings.Exists(b=>Own(b,v)&&b.Kind==BuildingKind.Spaceport&&b.Built));
            if(home==null)return new AiIntent("Rückzug blockiert","Keine bekannte eigene Rückzugsbasis","Überlebende bleiben physisch im Spiel");
            op.HomePlanetId=home.Id;
            var carrier=v.Ships.Find(s=>op.Ships.Contains(s.Id)&&s.Kind==ShipKind.LandingTransport);
            if(carrier!=null&&carrier.TransportPhase==TransportPhase.Landed)
            {
                var w=v.Worlds.Find(a=>a.Id==carrier.TargetPlanetId);
                if(w!=null)foreach(var u in w.Units)if(op.Units.Contains(u.Id))
                {
                    if(HexGrid.Distance(u.X,u.Y,carrier.LandingX,carrier.LandingY)<=1.5 && carrier.Troops.Count<Definitions.TroopCapacity)
                        return Order("Evakuieren","Überlebende tatsächlich wieder verladen",new SimCommand {Kind=CommandKind.LoadTroop,ActorId=carrier.Id,TargetId=u.Id,PlanetId=w.Id});
                    var move=Move(w,u,new GridCell(carrier.LandingX,carrier.LandingY),true);if(move!=null)return move;
                    // Allow real travel before launching. A timeout leaves stranded units present.
                    if(v.Tick-op.ChangedTick<600)return null;
                }
                return Order("Träger zurückziehen","Evakuierungsfenster beendet; eingeschlossene Einheiten bleiben bestehen",new SimCommand {Kind=CommandKind.LaunchTransport,ActorId=carrier.Id,PlanetId=carrier.TargetPlanetId});
            }
            bool arrived=true;
            foreach(var s in v.Ships)if(op.Ships.Contains(s.Id))
            {
                if(s.TransportPhase!=TransportPhase.Space){arrived=false;continue;}
                if(!AtKnownDock(s,home)){arrived=false;if(s.Order!=ShipOrder.Retreat||s.TargetPlanetId!=home.Id)return Order("Rückzug ausführen",op.Reason,new SimCommand {Kind=CommandKind.RetreatShip,ActorId=s.Id,PlanetId=home.Id});}
            }
            if(arrived){Phase(v,ai,OperationPhase.Cancelled,"Überlebende zurück; exklusive Zuweisungen freigegeben");op.Ships.Clear();op.Units.Clear();op.FoodReserved=op.PartsReserved=0;op.RetryAfter=v.Tick+Definitions.AiRetryTicks;}
            return new AiIntent("Operation abbrechen",op.Reason,"Keine identische sofortige Angriffswiederholung");
        }
    }
}
