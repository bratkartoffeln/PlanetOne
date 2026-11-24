using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        public static int BuildingOwner(PlanetState p, BuildingState b) { return b.OwnerId == 0 ? p.OwnerId : b.OwnerId; }
        private ShipState ActiveShip(long id) { return State.Ships.Find(s=>s.Id==id && !s.Destroyed && !s.Consumed); }
        private bool EnemyNear(PlanetState p, int owner, double x, double y, double radius)
        { return p.Units.Exists(u=>!u.Destroyed && u.Layer==TargetLayer.Ground && AtWar(owner,u.OwnerId) && Distance(u.X,u.Y,x,y)<=radius); }
        public bool Shielded(PlanetState p, int attacker, double x, double y)
        { return p.Buildings.Exists(b=>b.Kind==BuildingKind.ShieldGenerator && b.Operational && !b.Destroyed && AtWar(attacker,BuildingOwner(p,b)) && Distance(x,y,b.X,b.Y)<=Definitions.ShieldRadius); }
        public bool LocalOrbitThreat(int faction, int planet)
        { var pos=GetOrbitPosition(planet); return State.Ships.Exists(s=>IsShipVisible(faction,s) && AtWar(faction,s.OwnerId) && Definitions.SpaceWeapons[(int)s.Kind].Damage>0 && Distance(s.X,s.Y,pos)<=Definitions.OrbitalRadius); }
        public string LandingError(ShipState s, PlanetState p, int x, int y)
        {
            if(s.Kind!=ShipKind.LandingTransport || s.TransportPhase!=TransportPhase.Space) return "Landungstransporter im Raum benötigt.";
            if(!IsPlanetVisible(s.OwnerId,p.Id) || Distance(s.X,s.Y,GetOrbitPosition(p.Id))>Definitions.OrbitalRadius) return "Zielwelt zuerst aufklären und nahen Orbit erreichen.";
            if(p.OwnerId!=0 && p.OwnerId!=s.OwnerId && !AtWar(s.OwnerId,p.OwnerId)) return "Keine militärische Landeberechtigung im Frieden.";
            if(Shielded(p,s.OwnerId,x,y)) return "Aktiver Schild: anderen Landeort wählen.";
            for(int dx=0;dx<2;dx++) for(int dy=0;dy<2;dy++)
                if(!Inside(p,x+dx,y+dy) || p.Terrain[(y+dy)*p.Width+x+dx]!=0 || p.Buildings.Exists(b=>!b.Destroyed && b.X==x+dx && b.Y==y+dy) || p.Units.Exists(u=>!u.Destroyed && Distance(u.X,u.Y,x+dx,y+dy)<0.7)) return "Freie begehbare 2×2-Landefläche benötigt.";
            if(State.Ships.Exists(v=>v.Id!=s.Id && !v.Destroyed && v.TargetPlanetId==p.Id && v.TransportPhase!=TransportPhase.Space && Math.Abs(v.LandingX-x)<2 && Math.Abs(v.LandingY-y)<2)) return "Landefläche ist bereits reserviert.";
            return null;
        }
        private bool AtLoadingPoint(ShipState s, PlanetState p, UnitState u)
        {
            if(s.Kind!=ShipKind.LandingTransport || s.Troops.Count>=Definitions.TroopCapacity || u==null || u.Destroyed || u.OwnerId!=s.OwnerId || u.Layer!=TargetLayer.Ground) return false;
            if(s.TransportPhase==TransportPhase.Landed) return s.TargetPlanetId==p.Id && Distance(u.X,u.Y,s.LandingX,s.LandingY)<=1.5;
            var port=p.Buildings.Find(b=>PortOwned(b,s.OwnerId) && b.Operational);
            return s.TransportPhase==TransportPhase.Space && port!=null && Distance(s.X,s.Y,GetOrbitPosition(p.Id))<=Definitions.DockDistance && Distance(u.X,u.Y,port.X,port.Y)<=1.5;
        }
        private void ApplyInvasion(SimCommand c)
        {
            var p=FindPlanet(c.PlanetId); var s=ActiveShip(c.ActorId); string error=null;
            if(c.Kind==CommandKind.CaptureBuilding || c.Kind==CommandKind.RestoreHeadquarters || c.Kind==CommandKind.EstablishBeachhead)
            { ApplyGroundInvasion(p,c); return; }
            if(s==null || s.OwnerId!=c.IssuerId) error="Eigenes aktives Schiff benötigt.";
            else if(c.Kind==CommandKind.BuildWarship)
            {
                if(s.Kind!=ShipKind.OrbitalYard || s.ProductionKind>=0 || c.IntValue<(int)ShipKind.Scout || c.IntValue>=(int)Definitions.Ships.Length) error="Freie Orbitalwerft und gültiger Militärschiffstyp benötigt.";
                else if(!HasTechnology(c.IssuerId,Definitions.Ships[c.IntValue].Required)) error="Schiffstechnologie fehlt.";
                else { s.ProductionKind=c.IntValue; s.ProductionProgress=0; s.ProductionPaid=false; }
            }
            else if(c.Kind==CommandKind.LoadTroop)
            {
                var u=p.Units.Find(v=>v.Id==c.TargetId);
                if(!AtLoadingPoint(s,p,u)) error="Truppe muss am eigenen Hafen oder neben dem gelandeten Träger stehen; vier Plätze maximal.";
                else { p.Units.Remove(u); u.Order=UnitOrder.Hold; u.Path.Clear(); u.PathDistance=0; u.X=u.Y=u.GoalX=u.GoalY=0; s.Troops.Add(u); s.Status="Verladen: "+s.Troops.Count+" / 4"; }
            }
            else if(c.Kind==CommandKind.UnloadTroop)
            {
                var u=s.Troops.Find(v=>v.Id==c.TargetId && !v.Destroyed);
                if(u==null || s.TransportPhase!=TransportPhase.Landed || s.TargetPlanetId!=p.Id) error="Geladene Einheit und gelandeter Träger benötigt.";
                else
                {
                    GridCell? cell=null;
                    for(int dx=0;dx<2;dx++) for(int dy=0;dy<2;dy++)
                    { int x=s.LandingX+dx,y=s.LandingY+dy; if(CanTraverse(p,u.OwnerId,u.Kind,x,y) && !p.Units.Exists(v=>!v.Destroyed && Distance(v.X,v.Y,x,y)<0.7)) cell=new GridCell(x,y); }
                    if(!cell.HasValue) error="Landefläche belegt; Truppen zuerst wegbewegen.";
                    else { s.Troops.Remove(u); u.X=u.GoalX=cell.Value.X; u.Y=u.GoalY=cell.Value.Y; u.Status="Gelandet"; p.Units.Add(u); }
                }
            }
            else if(c.Kind==CommandKind.LandTransport)
            {
                error=LandingError(s,p,c.X,c.Y);
                if(error==null) { s.TransportPhase=TransportPhase.Descending; s.TargetPlanetId=p.Id; s.LandingX=c.X; s.LandingY=c.Y; s.ActionProgress=0; s.Order=ShipOrder.Hold; Emit(s.OwnerId,p.Id,s.Id,"Descent","Abstieg auf "+p.Name+"; Träger angreifbar."); if(p.OwnerId!=0 && p.OwnerId!=s.OwnerId) Emit(p.OwnerId,p.Id,s.Id,"EnemyDescent","Feindlicher Abstieg auf "+p.Name+"."); }
            }
            else if(c.Kind==CommandKind.LaunchTransport)
            {
                if(s.TransportPhase!=TransportPhase.Landed) error="Träger ist nicht gelandet.";
                else { s.TransportPhase=TransportPhase.Ascending; s.ActionProgress=0; s.Status="Evakuierung: Aufstieg"; }
            }
            else if(c.Kind==CommandKind.LoadShipSupplies)
            {
                var port=FindBuilding(c.TargetId);
                if(c.IntValue<0 || c.IntValue>=6 || c.Amount<=0 || c.Amount>24 || s.Kind!=ShipKind.LandingTransport || !PortOwned(port,c.IssuerId) || !port.Operational || s.TransportPhase!=TransportPhase.Space || Distance(s.X,s.Y,GetOrbitPosition(port.PlanetId))>Definitions.DockDistance) error="Träger am betriebsfähigen eigenen Hafen und gültige Menge benötigt.";
                else if(Total(s.Cargo)+c.Amount>Definitions.Ships[(int)s.Kind].Capacity || port.Inventory[c.IntValue]-port.ReservedOutgoing[c.IntValue]<c.Amount) error="Frachtraum oder freie Hafenware fehlt.";
                else { port.Inventory[c.IntValue]-=c.Amount; s.Cargo[c.IntValue]+=c.Amount; }
            }
            else if(s.TransportPhase!=TransportPhase.Space || s.Kind==ShipKind.OrbitalYard) error="Bewegliches Schiff im Raum benötigt.";
            else if(c.Kind==CommandKind.AttackShip)
            {
                var target=ActiveShip(c.TargetId);
                if(target==null || !IsShipVisible(c.IssuerId,target) || !AtWar(c.IssuerId,target.OwnerId) || Definitions.SpaceWeapons[(int)s.Kind].Damage==0) error="Aufgeklärtes feindliches Raumziel und bewaffnetes Schiff benötigt.";
                else { CancelFreight(s); s.CombatTargetId=target.Id; s.Order=ShipOrder.Attack; }
            }
            else if(c.Kind==CommandKind.RetreatShip)
            { if(!IsPlanetKnown(c.IssuerId,p.Id)) error="Rückzugswelt unbekannt."; else { CancelFreight(s); s.AutoSupply=false; s.Order=ShipOrder.Retreat; s.TargetPlanetId=p.Id; } }
            else if(c.Kind==CommandKind.RepairShip)
            {
                var yard=ActiveShip(c.TargetId);
                if(yard==null || yard.Kind!=ShipKind.OrbitalYard || yard.OwnerId!=c.IssuerId) error="Eigene Orbitalwerft benötigt.";
                else { CancelFreight(s); s.CombatTargetId=yard.Id; s.Order=ShipOrder.Repair; }
            }
            else if(c.Kind==CommandKind.OrbitalBombard)
            {
                if(s.Kind!=ShipKind.TorpedoShip || s.Ammunition<2 || !Inside(p,c.X,c.Y) || !IsCellVisible(p,c.IssuerId,c.X,c.Y) || !AtWar(c.IssuerId,p.OwnerId) || Distance(s.X,s.Y,GetOrbitPosition(p.Id))>Definitions.OrbitalRadius) error="Torpedoschiff im nahen Orbit, zwei Munition und aktuelle Bodenaufklärung benötigt.";
                else { s.Order=ShipOrder.Bombard; s.TargetPlanetId=p.Id; s.LandingX=c.X; s.LandingY=c.Y; s.ActionProgress=0; Emit(p.OwnerId,p.Id,s.Id,"OrbitalWarning","Orbitalfeuer angekündigt: "+c.X+" / "+c.Y); }
            }
            else error="Unbekannter Invasionsbefehl.";
            Report(c,error==null,error==null?"InvasionOrderAccepted":"InvalidInvasionOrder",error??"Auftrag angenommen.");
        }
        private void ApplyGroundInvasion(PlanetState p, SimCommand c)
        {
            var u=p.Units.Find(v=>v.Id==c.ActorId && !v.Destroyed && v.OwnerId==c.IssuerId && v.Layer==TargetLayer.Ground);
            string error=null; var b=p.Buildings.Find(v=>v.Id==c.TargetId);
            if(u==null) error="Eigene aktive Bodeneinheit benötigt.";
            else if(c.Kind==CommandKind.EstablishBeachhead)
            {
                if(u.Kind!=UnitKind.Engineer || u.Parts<Definitions.BeachheadParts || u.Food<1 || !Inside(p,c.X,c.Y) || !CanTraverse(p,u.OwnerId,u.Kind,c.X,c.Y) || Distance(u.X,u.Y,c.X,c.Y)>1.5 || EnemyNear(p,u.OwnerId,c.X,c.Y,2) || State.Beachheads.Exists(h=>!h.Destroyed && h.PlanetId==p.Id && h.X==c.X && h.Y==c.Y)) error="Pionier mit vier Bauteilen und Nahrung an freier gesicherter Fläche benötigt.";
                else { u.Parts-=Definitions.BeachheadParts; u.Food--; p.ConsumedTotals[3]+=Definitions.BeachheadParts; State.Beachheads.Add(new BeachheadState {Id=State.NextEntityId++,OwnerId=u.OwnerId,PlanetId=p.Id,X=c.X,Y=c.Y,Food=1}); }
            }
            else if(c.Kind==CommandKind.RestoreHeadquarters)
            {
                if(u.Kind!=UnitKind.Engineer || u.Parts<4 || b==null || b.Kind!=BuildingKind.Headquarters || !b.Destroyed || b.RestoreWorkerId!=0 || Distance(u.X,u.Y,b.X,b.Y)>1.5 || EnemyNear(p,u.OwnerId,b.X,b.Y,Definitions.CaptureRadius)) error="Pionier mit vier Bauteilen an gesicherter freier Verwaltungsruine benötigt.";
                else { u.Parts-=4; p.ConsumedTotals[3]+=4; b.RestoreWorkerId=u.Id; b.RestoreProgress=0; }
            }
            else if(b==null || b.Destroyed || !b.Built || u.Kind!=UnitKind.Infantry || !AtWar(u.OwnerId,BuildingOwner(p,b)) || Distance(u.X,u.Y,b.X,b.Y)>1.5) error="Infanterie muss unmittelbar am feindlichen intakten Gebäude stehen.";
            else { b.CapturingFaction=u.OwnerId; b.CaptureProgress=0; u.Order=UnitOrder.Hold; u.Path.Clear(); u.PathDistance=0; }
            Report(c,error==null,error==null?"GroundInvasionAccepted":"InvalidGroundInvasion",error??"Bodenauftrag angenommen.");
        }
        private void TickInvasion()
        {
            foreach(var s in new List<ShipState>(State.Ships))
            {
                if(s.Destroyed || s.Consumed) continue;
                s.Cooldown=Math.Max(0,s.Cooldown-Definitions.TickSeconds);
                if(s.Kind==ShipKind.OrbitalYard) { TickYard(s); continue; }
                if(s.TransportPhase!=TransportPhase.Space) { TickTransport(s); continue; }
                if(s.Order==ShipOrder.Retreat) { if(MoveShip(s,GetOrbitPosition(s.TargetPlanetId))) s.Order=ShipOrder.Guard; s.Status="Geordneter Rückzug"; continue; }
                if(s.Order==ShipOrder.Repair) { TickShipRepair(s); continue; }
                if(s.Order==ShipOrder.Bombard) { TickBombard(s); continue; }
                var weapon=Definitions.SpaceWeapons[(int)s.Kind]; if(weapon.Damage==0) continue;
                ShipState enemy=null;
                if(s.Order==ShipOrder.Attack)
                {
                    enemy=ActiveShip(s.CombatTargetId);
                    if(enemy==null || !AtWar(s.OwnerId,enemy.OwnerId) || !IsShipVisible(s.OwnerId,enemy)) { s.Order=ShipOrder.Hold; s.Status="Ziel aus Sicht; hält Position"; continue; }
                    if(Distance(s.X,s.Y,new SimPoint(enemy.X,enemy.Y))>weapon.Range) MoveShip(s,new SimPoint(enemy.X,enemy.Y));
                }
                else
                    foreach(var target in State.Ships) if(IsShipVisible(s.OwnerId,target) && AtWar(s.OwnerId,target.OwnerId) && Distance(s.X,s.Y,new SimPoint(target.X,target.Y))<=weapon.Range) { enemy=target; break; }
                if(enemy==null || s.Cooldown>0 || s.Ammunition==0 || Distance(s.X,s.Y,new SimPoint(enemy.X,enemy.Y))>weapon.Range) continue;
                double damage=weapon.Damage;
                if(s.Kind==ShipKind.TorpedoShip) damage*=enemy.Kind==ShipKind.Corvette || enemy.Kind==ShipKind.Scout ? 0.4 : 1.5;
                s.Ammunition--; s.Cooldown=weapon.Interval; HurtShip(enemy,damage); s.Status="Raumgefecht gegen #"+enemy.Id;
                Emit(enemy.OwnerId,enemy.TargetPlanetId,enemy.Id,"ShipUnderAttack","Schiff #"+enemy.Id+" unter Beschuss.");
            }
            foreach(var p in State.Planets) { TickOrbitalDefense(p); TickOccupation(p); }
            foreach(var h in State.Beachheads) TickBeachhead(h);
        }
        private void HurtShip(ShipState s, double damage) { s.Health=Math.Max(0,s.Health-damage); if(s.Health==0) DestroyShip(s.Id); }
        private void TickYard(ShipState yard)
        {
            var p=FindPlanet(yard.TargetPlanetId); var pos=GetOrbitPosition(p.Id); yard.X=pos.X; yard.Y=pos.Y;
            var port=p.Buildings.Find(b=>PortOwned(b,yard.OwnerId) && b.Operational);
            if(port==null) { yard.Status="Werft ohne eigenen Hafenanschluss"; return; }
            yard.ActionProgress+=Definitions.TickSeconds;
            if(yard.ActionProgress>=Definitions.SpaceTransferSeconds)
            {
                yard.ActionProgress=0;
                for(int r=0;r<6;r++)
                { int desired=yard.ProductionKind>=0 && !yard.ProductionPaid?Definitions.Ships[yard.ProductionKind].Cost[r]:0; if(r==3) desired=Math.Max(desired,8);
                  if(yard.Cargo[r]<desired && Total(yard.Cargo)<Definitions.Ships[(int)yard.Kind].Capacity && port.Inventory[r]-port.ReservedOutgoing[r]>0) { port.Inventory[r]--; yard.Cargo[r]++; break; } }
            }
            if(yard.ProductionKind<0) { yard.Status="Orbitalwerft bereit; Hafenmaterial wird übertragen"; return; }
            var d=Definitions.Ships[yard.ProductionKind];
            if(!yard.ProductionPaid)
            {
                for(int r=0;r<6;r++) if(yard.Cargo[r]<d.Cost[r]) { yard.Status="Werft wartet auf Hafentransfer"; return; }
                if(p.Population-d.Crew<4) { yard.Status="Werft wartet auf Besatzung"; return; }
                for(int r=0;r<6;r++) { yard.Cargo[r]-=d.Cost[r]; p.ConsumedTotals[r]+=d.Cost[r]; }
                p.Population-=d.Crew; p.WorkersTotal=Math.Min(p.WorkersTotal,p.Population); p.WorkersUsed=Math.Min(p.WorkersUsed,p.WorkersTotal); yard.ProductionPaid=true;
            }
            yard.ProductionProgress+=Definitions.TickSeconds;
            yard.Status="Werftbau: "+d.Name+" "+(int)(100*yard.ProductionProgress/d.Seconds)+"%";
            if(yard.ProductionProgress<d.Seconds) return;
            var weapon=Definitions.SpaceWeapons[yard.ProductionKind];
            State.Ships.Add(new ShipState {Id=State.NextEntityId++,OwnerId=yard.OwnerId,Kind=(ShipKind)yard.ProductionKind,Health=weapon.Health,Ammunition=weapon.Ammo,Crew=d.Crew,X=yard.X,Y=yard.Y,TargetPlanetId=p.Id,Order=ShipOrder.Guard});
            yard.ProductionKind=-1; yard.ProductionProgress=0; yard.ProductionPaid=false;
        }
        private void TickShipRepair(ShipState s)
        {
            var yard=ActiveShip(s.CombatTargetId);
            if(yard==null || yard.OwnerId!=s.OwnerId || yard.Kind!=ShipKind.OrbitalYard) { s.Order=ShipOrder.Hold; return; }
            if(!MoveShip(s,new SimPoint(yard.X,yard.Y))) return;
            var d=Definitions.SpaceWeapons[(int)s.Kind];
            if(s.Health>=d.Health && s.Ammunition>=d.Ammo) { s.Order=ShipOrder.Guard; s.TargetPlanetId=yard.TargetPlanetId; return; }
            s.Status="Reparatur/Nachmunitionierung: Werftbauteile benötigt";
            if(s.Cooldown>0 || yard.Cargo[3]<=0) return;
            yard.Cargo[3]--; FindPlanet(yard.TargetPlanetId).ConsumedTotals[3]++; s.Cooldown=1;
            if(s.Health<d.Health) s.Health=Math.Min(d.Health,s.Health+Definitions.RepairPerPart);
            else s.Ammunition=Math.Min(d.Ammo,s.Ammunition+Definitions.AmmoPerPart);
        }
        private void TickTransport(ShipState s)
        {
            var p=FindPlanet(s.TargetPlanetId); var pos=GetOrbitPosition(p.Id); s.X=pos.X; s.Y=pos.Y;
                if(s.TransportPhase==TransportPhase.Landed)
                {
                    s.Status="Brückenkopf: gelandet; Entladen oder Evakuieren";
                    foreach(var u in p.Units) if(!u.Destroyed && AtWar(s.OwnerId,u.OwnerId) && u.Kind!=UnitKind.AntiAir && u.Cooldown<=0 && u.Ammunition>0 && Distance(u.X,u.Y,s.LandingX,s.LandingY)<=Definitions.Get(u.Kind).Range && HasLineOfSight(p,u.X,u.Y,s.LandingX,s.LandingY))
                    { u.Ammunition--; u.Cooldown=Definitions.Get(u.Kind).Interval; HurtShip(s,Definitions.Get(u.Kind).Damage); }
                    return;
                }
            s.ActionProgress+=Definitions.TickSeconds;
            s.Status=(s.TransportPhase==TransportPhase.Descending?"Abstieg ":"Aufstieg ")+(int)(100*s.ActionProgress/Definitions.TransitionSeconds)+"%";
            foreach(var b in p.Buildings)
            {
                if(b.Kind!=BuildingKind.FlakTower || !b.Operational || b.Destroyed || !AtWar(s.OwnerId,BuildingOwner(p,b)) || b.FireCooldown>0 || b.Ammunition<=0 || Distance(s.LandingX,s.LandingY,b.X,b.Y)>Definitions.GetTower(b.Kind).Range) continue;
                b.Ammunition--; b.FireCooldown=Definitions.GetTower(b.Kind).Interval; HurtShip(s,Definitions.GetTower(b.Kind).Damage);
            }
            foreach(var u in p.Units)
            {
                if(u.Destroyed || u.Kind!=UnitKind.AntiAir || !AtWar(s.OwnerId,u.OwnerId) || u.Cooldown>0 || u.Ammunition<=0 || Distance(s.LandingX,s.LandingY,u.X,u.Y)>Definitions.Get(u.Kind).Range) continue;
                u.Ammunition--; u.Cooldown=Definitions.Get(u.Kind).Interval; HurtShip(s,Definitions.Get(u.Kind).Damage);
            }
            if(s.Destroyed || s.ActionProgress<Definitions.TransitionSeconds) return;
            if(s.TransportPhase==TransportPhase.Descending)
            {
                s.TransportPhase=TransportPhase.Space;
                string landingError=LandingError(s,p,s.LandingX,s.LandingY);
                if(landingError!=null) { s.TransportPhase=TransportPhase.Ascending; s.ActionProgress=0; s.Status="Landung abgebrochen: "+landingError; return; }
                s.TransportPhase=TransportPhase.Landed;
            }
            else { s.TransportPhase=TransportPhase.Space; s.Order=ShipOrder.Guard; }
            s.ActionProgress=0;
        }
        private void TickOrbitalDefense(PlanetState p)
        {
            foreach(var b in p.Buildings)
            {
                if(b.Kind!=BuildingKind.OrbitalBattery || !b.Operational || b.Destroyed) continue;
                b.FireCooldown=Math.Max(0,b.FireCooldown-Definitions.TickSeconds);
                if(b.Ammunition==0 && b.Inventory[3]-b.ReservedOutgoing[3]>0) { b.Inventory[3]--; p.ConsumedTotals[3]++; b.Ammunition=Definitions.AmmoPerPart; }
                if(b.FireCooldown>0 || b.Ammunition==0) continue;
                foreach(var s in State.Ships) if(IsShipVisible(BuildingOwner(p,b),s) && AtWar(BuildingOwner(p,b),s.OwnerId) && Distance(s.X,s.Y,GetOrbitPosition(p.Id))<=Definitions.OrbitalRadius)
                { b.Ammunition--; b.FireCooldown=2; HurtShip(s,24); break; }
            }
        }
        private void TickBombard(ShipState s)
        {
            var p=FindPlanet(s.TargetPlanetId);
            if(!AtWar(s.OwnerId,p.OwnerId) || !IsCellVisible(p,s.OwnerId,s.LandingX,s.LandingY) || Distance(s.X,s.Y,GetOrbitPosition(p.Id))>Definitions.OrbitalRadius || s.Ammunition<2) { s.Order=ShipOrder.Hold; s.Status="Feuerauftrag abgebrochen: Ziel oder Aufklärung verloren"; return; }
            s.ActionProgress+=Definitions.TickSeconds; s.Status="Orbitalfeuer vorbereitet: "+(int)(100*s.ActionProgress/Definitions.BombardSeconds)+"%";
            if(s.ActionProgress<Definitions.BombardSeconds) return;
            s.Ammunition-=2; s.Order=ShipOrder.Hold; s.ActionProgress=0;
            if(Shielded(p,s.OwnerId,s.LandingX,s.LandingY)) { s.Status="Orbitalfeuer vom Schild abgefangen"; return; }
            foreach(var b in p.Buildings) if(!b.Destroyed && AtWar(s.OwnerId,BuildingOwner(p,b)) && Distance(b.X,b.Y,s.LandingX,s.LandingY)<=1.2) HurtBuilding(p,b,70);
            foreach(var u in p.Units) if(!u.Destroyed && AtWar(s.OwnerId,u.OwnerId) && Distance(u.X,u.Y,s.LandingX,s.LandingY)<=1.2) HurtUnit(p,u,70,UnitKind.Artillery);
        }
        private void TickOccupation(PlanetState p)
        {
            foreach(var b in p.Buildings)
            {
                if(b.Destroyed && b.RestoreWorkerId!=0)
                {
                    var restorer=p.Units.Find(u=>u.Id==b.RestoreWorkerId && !u.Destroyed && u.Kind==UnitKind.Engineer);
                    if(restorer==null) {b.RestoreWorkerId=0;b.RestoreProgress=0;continue;}
                    if(Distance(restorer.X,restorer.Y,b.X,b.Y)>1.5 || EnemyNear(p,restorer.OwnerId,b.X,b.Y,Definitions.CaptureRadius)) continue;
                    b.RestoreProgress+=Definitions.TickSeconds;b.Status="Verwaltung wird repariert";
                    if(b.RestoreProgress<Definitions.BeachheadSeconds) continue;
                    b.Destroyed=false;b.Deconstructing=false;b.Built=true;b.Health=64;b.Enabled=true;b.RestoreWorkerId=0;b.RestoreProgress=0;
                }
                int faction=b.CapturingFaction; if(faction==0 || b.Destroyed) continue;
                var infantry=p.Units.Find(u=>!u.Destroyed && u.OwnerId==faction && u.Kind==UnitKind.Infantry && u.Layer==TargetLayer.Ground && Distance(u.X,u.Y,b.X,b.Y)<=1.5);
                if(infantry==null || EnemyNear(p,faction,b.X,b.Y,Definitions.CaptureRadius) || !AtWar(faction,BuildingOwner(p,b))) { b.CaptureProgress=0; continue; }
                b.CaptureProgress+=Definitions.TickSeconds;
                if(b.CaptureProgress<Definitions.CaptureSeconds) continue;
                if(b.Kind==BuildingKind.Headquarters && !HasInvasionSupply(p,infantry)) { b.CaptureProgress=Definitions.CaptureSeconds; b.Status="Verwaltung gesichert; erreichbarer Versorgungspunkt fehlt"; continue; }
                CancelPortWork(p,b);
                if(b.RecruitPaid && b.RecruitKind>=0)
                {
                    var recruit=Definitions.Get((UnitKind)b.RecruitKind);p.Population+=recruit.Crew;
                    for(int r=0;r<6;r++){b.Inventory[r]+=recruit.Cost[r];p.ConsumedTotals[r]-=recruit.Cost[r];}
                }
                b.RecruitKind=-1;b.RecruitPaid=false;b.RecruitProgress=0;
                b.CapacityOverride=Math.Max(StorageCapacity(b),Total(b.Inventory)+Total(b.ReservedIncoming)+b.ReservedReturnSpace+b.ProductionReservedSpace);
                if(b.Kind==BuildingKind.Headquarters)
                {
                    int old=p.OwnerId;
                    foreach(var other in p.Buildings) if(other.OwnerId==0) other.OwnerId=old;
                    p.OwnerId=faction; p.GatePermissions.Clear();
                    Emit(faction,p.Id,b.Id,"PlanetCaptured",p.Name+" gesichert; verbleibende Verteidiger und Anlagen bestehen fort.");
                }
                b.OwnerId=faction; b.CapturingFaction=0; b.CaptureProgress=0; b.Operational=false;
                ReconcileOccupationLogistics(p);
            }
        }
        private bool HasInvasionSupply(PlanetState p, UnitState infantry)
        {
            foreach(var h in State.Beachheads) if(!h.Destroyed && h.Progress>=Definitions.BeachheadSeconds && h.PlanetId==p.Id && h.OwnerId==infantry.OwnerId && h.Food>0 && !EnemyNear(p,h.OwnerId,h.X,h.Y,2) && GroundPath(p,infantry,h.X,h.Y,false)!=null) return true;
            foreach(var b in p.Buildings) if(b.Kind==BuildingKind.Spaceport && b.Built && !b.Destroyed && BuildingOwner(p,b)==infantry.OwnerId && b.Inventory[0]-b.ReservedOutgoing[0]>0 && !EnemyNear(p,infantry.OwnerId,b.X,b.Y,2) && GroundPath(p,infantry,b.X,b.Y,true)!=null) return true;
            return false;
        }
        private void ReconcileOccupationLogistics(PlanetState p)
        {
            for(int i=p.Deliveries.Count-1;i>=0;i--)
            {
                var d=p.Deliveries[i];var a=FindBuilding(d.SourceId);var b=FindBuilding(d.TargetId);
                if(BuildingOwner(p,a)==BuildingOwner(p,b)) continue;
                a.ReservedOutgoing[(int)d.Resource]-=d.Amount;b.ReservedIncoming[(int)d.Resource]-=d.Amount;p.Deliveries.RemoveAt(i);
            }
            for(int i=p.Shipments.Count-1;i>=0;i--)
            {
                var d=p.Shipments[i];var a=FindBuilding(d.SourceId);var b=FindBuilding(d.TargetId);
                if(BuildingOwner(p,a)==BuildingOwner(p,b) || d.Phase==ShipmentPhase.Recovering || d.Phase==ShipmentPhase.Returning) continue;
                b.ReservedIncoming[(int)d.Resource]-=d.Amount;
                if(d.Phase==ShipmentPhase.Loading) {a.ReservedOutgoing[(int)d.Resource]-=d.Amount;p.Shipments.RemoveAt(i);continue;}
                a.ReservedReturnSpace-=d.Amount;a.ReservedIncoming[(int)d.Resource]+=d.Amount;d.RecoveryReserved=true;d.Phase=ShipmentPhase.Recovering;
                d.Path.Reverse();d.DistanceTravelled=d.Path.Count-1-d.DistanceTravelled;d.PhaseProgress=0;d.RouteBlocked=true;
            }
        }
        private void TickBeachhead(BeachheadState h)
        {
            if(h.Destroyed) return; var p=FindPlanet(h.PlanetId);
            foreach(var enemy in p.Units)
            {
                var weapon=Definitions.Get(enemy.Kind);
                if(enemy.Destroyed || !AtWar(h.OwnerId,enemy.OwnerId) || weapon.AntiAir || enemy.Cooldown>0 || enemy.Ammunition<=0 || Distance(enemy.X,enemy.Y,h.X,h.Y)>weapon.Range || !HasLineOfSight(p,enemy.X,enemy.Y,h.X,h.Y)) continue;
                enemy.Ammunition--;enemy.Cooldown=weapon.Interval;h.Health=Math.Max(0,h.Health-weapon.Damage);
                if(h.Health==0) {h.Destroyed=true;p.LostTotals[0]+=h.Food;p.LostTotals[3]+=h.Parts;h.Food=h.Parts=0;return;}
            }
            if(EnemyNear(p,h.OwnerId,h.X,h.Y,2)) return;
            var engineer=p.Units.Find(u=>!u.Destroyed && u.OwnerId==h.OwnerId && u.Kind==UnitKind.Engineer && Distance(u.X,u.Y,h.X,h.Y)<=1.5);
            if(h.Progress<Definitions.BeachheadSeconds) { if(engineer!=null) h.Progress=Math.Min(Definitions.BeachheadSeconds,h.Progress+Definitions.TickSeconds); return; }
            foreach(var s in State.Ships) if(!s.Destroyed && s.TransportPhase==TransportPhase.Landed && s.TargetPlanetId==p.Id && s.OwnerId==h.OwnerId && Distance(s.LandingX,s.LandingY,h.X,h.Y)<=2)
            { h.Food+=s.Cargo[0]; h.Parts+=s.Cargo[3]; s.Cargo[0]=s.Cargo[3]=0; }
            foreach(var u in p.Units) if(!u.Destroyed && u.OwnerId==h.OwnerId && Distance(u.X,u.Y,h.X,h.Y)<=1.5 && u.Cooldown<=0)
            {
                if(u.Food<4 && h.Food>0) { h.Food--;u.Food++;u.Cooldown=1; }
                if(u.Ammunition<Definitions.Get(u.Kind).Ammo && h.Parts>0) { h.Parts--;p.ConsumedTotals[3]++;u.Ammunition=Math.Min(Definitions.Get(u.Kind).Ammo,u.Ammunition+Definitions.AmmoPerPart);u.Cooldown=1; }
                if(u.Kind==UnitKind.Engineer && u.Parts<8 && h.Parts>0) {h.Parts--;u.Parts++;u.Cooldown=1;}
            }
        }
    }
}
