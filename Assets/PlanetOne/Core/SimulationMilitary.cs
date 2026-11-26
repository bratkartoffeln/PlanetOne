using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        public bool AtWar(int a, int b)
        { return a != b && State.Relations.Exists(r => r.Relation == RelationKind.War &&
            ((r.FirstId == a && r.SecondId == b) || (r.FirstId == b && r.SecondId == a))); }

        private static double Distance(double ax, double ay, double bx, double by)
        { double x = ax - bx, y = ay - by; return Math.Sqrt(x * x + y * y); }
        private static bool Inside(PlanetState p, int x, int y)
        { return x >= 0 && y >= 0 && x < p.Width && y < p.Height; }

        public bool HasLineOfSight(PlanetState p, double ax, double ay, double bx, double by)
        {
            int steps = (int)Math.Ceiling(HexGrid.Distance(ax, ay, bx, by) * 4);
            var start = HexGrid.CellAt(ax, ay); int startX = start.X, startY = start.Y;
            var a = HexGrid.ToWorld(ax, ay); var bWorld = HexGrid.ToWorld(bx, by);
            var end = HexGrid.CellAt(bx, by); int endX = end.X, endY = end.Y;
            for (int i = 1; i < steps; i++)
            {
                var sample = HexGrid.FromWorld(a.X + (bWorld.X - a.X) * i / steps, a.Y + (bWorld.Y - a.Y) * i / steps);
                int x = sample.X, y = sample.Y;
                if ((x == startX && y == startY) || (x == endX && y == endY)) continue;
                if (!Inside(p, x, y) || p.Terrain[y * p.Width + x] == 1) return false;
                if (p.Buildings.Exists(b => !b.Destroyed && b.X == x && b.Y == y &&
                    (b.Kind == BuildingKind.Wall || b.Kind == BuildingKind.Gate))) return false;
            }
            return true;
        }

        public bool IsCellVisible(PlanetState p, int faction, double x, double y)
        {
            foreach (var u in p.Units)
                if (!u.Destroyed && SharesSensors(faction,u.OwnerId) && u.Layer == TargetLayer.Ground &&
                    HexGrid.Distance(u.X, u.Y, x, y) <= Definitions.Get(u.Kind).Sight && HasLineOfSight(p, u.X, u.Y, x, y)) return true;
            foreach (var b in p.Buildings)
                    if (SharesSensors(faction,BuildingOwner(p,b)) && b.Built && !b.Destroyed && !b.Deconstructing && HexGrid.Distance(b.X, b.Y, x, y) <= Definitions.BuildingSight &&
                        HasLineOfSight(p, b.X, b.Y, x, y)) return true;
            return false;
        }

        public bool CanSeeEntity(PlanetState p, int faction, long id)
        {
            var u = p.Units.Find(v => v.Id == id && !v.Destroyed);
            if (u != null) return u.OwnerId == faction || IsCellVisible(p, faction, u.X, u.Y);
            var b = p.Buildings.Find(v => v.Id == id && !v.Destroyed);
            return b != null && (faction == BuildingOwner(p,b) || IsCellVisible(p, faction, b.X, b.Y));
        }

        public bool CanTraverse(PlanetState p, int faction, UnitKind kind, int x, int y, long unitId=0)
        {
            if (!Inside(p, x, y) || PlanetEnvironment.BlocksGround(p.Terrain[y * p.Width + x]) || LandingBlocksGround(p,x,y)) return false;
            var building = p.Buildings.Find(b => b.X == x && b.Y == y && !b.Destroyed);
            if (building == null) return true;
            if (building.Kind == BuildingKind.Gate && building.Built && !building.Deconstructing)
                return faction == BuildingOwner(p,building) || State.Evacuations.Exists(e=>e.Guest==faction&&e.PlanetId==p.Id&&e.UntilTick>State.Tick&&e.Units.Contains(unitId)) || (!AtWar(faction, BuildingOwner(p,building)) && (HasAccess(faction,BuildingOwner(p,building),p.Id,AccessRight.MilitaryTransit) || p.GatePermissions.Exists(g => g.GateId == building.Id && g.FactionId == faction)));
            // Friendly infantry may occupy completed civilian buildings for cover.
            return kind == UnitKind.Infantry && faction == BuildingOwner(p,building) && building.Built && !building.Deconstructing &&
                building.Kind < BuildingKind.Barracks;
        }

        private List<GridCell> GroundPath(PlanetState p, UnitState u, int x, int y, bool adjacent,bool avoidTraffic=true,bool allowFallback=true)
        {
            var startCell = HexGrid.CellAt(u.X, u.Y); int sx = startCell.X, sy = startCell.Y;
            if (!Inside(p, sx, sy) || !Inside(p, x, y)) return null;
            int start = sy * p.Width + sx;
            var previous = new int[p.Terrain.Length]; for (int i = 0; i < previous.Length; i++) previous[i] = -1;
            var queue = new Queue<int>(); queue.Enqueue(start); previous[start] = start;
            int finish = -1;
            int arrivalRadius=adjacent?1:0;
            bool firingPosition=adjacent&&(u.Order==UnitOrder.Attack||u.Order==UnitOrder.Bombard);
            double targetX=x,targetY=y;int targetOwner;TargetLayer targetLayer;
            if(firingPosition&&u.Order==UnitOrder.Attack&&
                (!Target(p,u.TargetId,out targetX,out targetY,out targetOwner,out targetLayer)||!CanSeeEntity(p,u.OwnerId,u.TargetId)))return null;
            var weapon=Definitions.Get(u.Kind);
            if(avoidTraffic&&!adjacent)foreach(var other in p.Units)
                if(other.Id!=u.Id&&!other.Destroyed&&other.Layer==TargetLayer.Ground&&other.Order==UnitOrder.Hold&&(other.OwnerId==u.OwnerId||CanSeeEntity(p,u.OwnerId,other.Id))&&HexGrid.Distance(other.X,other.Y,x,y)<GroundUnitRadius(u.Kind)+GroundUnitRadius(other.Kind))
                    arrivalRadius=Math.Max(arrivalRadius,(int)Math.Ceiling(GroundUnitRadius(u.Kind)+GroundUnitRadius(other.Kind)));

            while (queue.Count > 0)
            {
                int cell = queue.Dequeue(), cx = cell % p.Width, cy = cell / p.Width;
                bool arrived=firingPosition?
                    HexGrid.Distance(cx,cy,targetX,targetY)<=weapon.Range&&(weapon.Indirect||HasLineOfSight(p,cx,cy,targetX,targetY)):
                    HexGrid.Steps(cx,cy,x,y)<=arrivalRadius;
                if (arrived && (!avoidTraffic||GroundKnownCellFree(p,u,cx,cy)))
                { finish = cell; break; }
                for (int d = 0; d < 6; d++)
                {
                    var neighbor = HexGrid.Neighbor(cx, cy, d); int nx = neighbor.X, ny = neighbor.Y;
                    if (!CanTraverse(p, u.OwnerId, u.Kind, nx, ny,u.Id)) continue;
                    if(avoidTraffic&&!GroundKnownCellFree(p,u,nx,ny))continue;
                    if(avoidTraffic)
                    {
                        var probe=UnitAgent(u);probe.Position=HexGrid.ToWorld(cx,cy);
                        if(GroundBlocker(p,probe,HexGrid.ToWorld(nx,ny),true)!=null)continue;
                    }
                    int next = ny * p.Width + nx;
                    if (previous[next] >= 0) continue;
                    previous[next] = cell; queue.Enqueue(next);
                }
            }
            if (finish < 0) return avoidTraffic&&allowFallback?GroundPath(p,u,x,y,adjacent,false):null;
            var result = new List<GridCell>();
            for (int cell = finish; ; cell = previous[cell])
            { result.Add(new GridCell(cell % p.Width, cell / p.Width)); if (cell == start) break; }
            result.Reverse(); return result;
        }

        private void ApplyMilitary(PlanetState p, SimCommand c)
        {
            if (c.Kind == CommandKind.RecruitUnit)
            {
                var b = p.Buildings.Find(v => v.Id == c.TargetId);
                if (b == null || BuildingOwner(p,b) != c.IssuerId || !b.Built || b.Destroyed || b.Deconstructing)
                { Report(c, false, "PermissionDenied", "Keine eigene betriebsfähige Produktionsstätte."); return; }
                if (!Enum.IsDefined(typeof(UnitKind), c.IntValue) || Definitions.Producer((UnitKind)c.IntValue) != b.Kind)
                { Report(c, false, "InvalidUnit", "Dieser Einheitentyp benötigt eine andere Produktionsstätte."); return; }
                if (b.RecruitKind >= 0) { Report(c, false, "QueueFull", "Die Produktionsstätte hat bereits einen Auftrag."); return; }
                b.RecruitKind = c.IntValue; b.RecruitProgress = 0; b.RecruitPaid = false;
                Report(c, true, "RecruitmentQueued", "Rekrutierung bestellt; Material wird physisch angeliefert."); return;
            }
            var u = p.Units.Find(v => v.Id == c.ActorId && !v.Destroyed);
            if (u == null || u.OwnerId != c.IssuerId || u.Layer != TargetLayer.Ground)
            { Report(c, false, "PermissionDenied", "Keine eigene aktive Bodeneinheit."); return; }
            if(EvacuatingUnit(p,u))
            {
                if(c.Kind!=CommandKind.MoveUnit && c.Kind!=CommandKind.RetreatUnit && c.Kind!=CommandKind.HoldPosition){Report(c,false,"EvacuationOnly","Räumung erlaubt nur Abzug und Verladung.");return;}
                if(c.Kind!=CommandKind.HoldPosition && !State.Ships.Exists(s=>!s.Destroyed&&s.OwnerId==u.OwnerId&&s.TargetPlanetId==p.Id&&s.TransportPhase==TransportPhase.Landed&&HexGrid.Steps(c.X,c.Y,s.LandingX,s.LandingY)<=1)){Report(c,false,"EvacuationRoute","Abzug muss zum gelandeten eigenen Evakuierungsträger führen.");return;}
            }
            UnitOrder order;
            switch (c.Kind)
            {
                case CommandKind.MoveUnit: order = UnitOrder.Move; break;
                case CommandKind.RetreatUnit: order = UnitOrder.Retreat; break;
                case CommandKind.HoldPosition: order = UnitOrder.Hold; break;
                case CommandKind.AttackTarget: order = UnitOrder.Attack; break;
                case CommandKind.AttackPosition: order = UnitOrder.Bombard; break;
                case CommandKind.ResupplyUnit: order = UnitOrder.Resupply; break;
                case CommandKind.RepairTarget: order = UnitOrder.Repair; break;
                default: Report(c, false, "InvalidCommand", "Unbekannter Militärbefehl."); return;
            }
            int x = c.X, y = c.Y;
            if (order == UnitOrder.Attack)
            {
                int owner; TargetLayer layer; double tx, ty;
                if (!Target(p, c.TargetId, out tx, out ty, out owner, out layer) || !CanSeeEntity(p, u.OwnerId, c.TargetId))
                { Report(c, false, "TargetNotVisible", "Ziel ist nicht aktuell aufgeklärt."); return; }
                if (ProtectedGuest(p,u.OwnerId,c.TargetId)) {Report(c,false,"ProtectedEvacuation","Abziehendes Kontingent ist geschützt.");return;}
                if (!AtWar(u.OwnerId, owner)) { Report(c, false, "NotAtWar", "Kein gültiger Kriegszustand."); return; }
                if (!ValidWeaponTarget(Definitions.Get(u.Kind).AntiAir, layer))
                { Report(c, false, "InvalidTargetLayer", "Waffe kann diese Zielebene nicht bekämpfen."); return; }
                var targetCell = HexGrid.CellAt(tx, ty); x = targetCell.X; y = targetCell.Y;
            }
            if (order == UnitOrder.Bombard)
            {
                if (!Inside(p, x, y) || !Definitions.Get(u.Kind).Indirect ||
                    (!IsCellVisible(p, u.OwnerId, x, y) && !p.Sightings.Exists(s => s.FactionId == u.OwnerId && s.X == x && s.Y == y)))
                { Report(c, false, "TargetNotKnown", "Artillerie benötigt eine aufgeklärte oder zuletzt bekannte Zielposition."); return; }
            }
            if (order == UnitOrder.Resupply || order == UnitOrder.Repair)
            {
                int owner; TargetLayer layer; double tx, ty;
                if (!Target(p, c.TargetId, out tx, out ty, out owner, out layer) || (owner != u.OwnerId && !HasAccess(u.OwnerId,owner,p.Id,order==UnitOrder.Resupply?AccessRight.Supply:AccessRight.Repair)))
                { Report(c, false, "PermissionDenied", "Versorgung und Reparatur benötigen ein eigenes Ziel."); return; }
                var depot = p.Buildings.Find(b => b.Id == c.TargetId && b.Kind == BuildingKind.MilitaryDepot && b.Built && !b.Deconstructing);
                if (order == UnitOrder.Resupply && depot == null && u.Kind != UnitKind.Engineer)
                { Report(c, false, "DepotRequired", "Zum Nachfüllen ein Militärdepot anfahren."); return; }
                if (order == UnitOrder.Repair && u.Kind != UnitKind.Engineer)
                { Report(c, false, "EngineerRequired", "Reparatur benötigt ein Pionierfahrzeug."); return; }
                var targetCell = HexGrid.CellAt(tx, ty); x = targetCell.X; y = targetCell.Y;
            }
            if (order != UnitOrder.Hold && !Inside(p, x, y))
            { Report(c, false, "InvalidPosition", "Ziel liegt außerhalb der Oberfläche."); return; }
            if(u.Traffic!=null&&u.Traffic.Yielding){u.Traffic.Yielding=false;u.Traffic.YieldForId=0;u.Traffic.Progress=0;}
            u.Path.Clear(); u.PathDistance = 0; u.Order = order; u.TargetId = c.TargetId;
            var currentCell=HexGrid.CellAt(u.X,u.Y);u.RecenterToCell=HexGrid.Distance(u.X,u.Y,currentCell.X,currentCell.Y)>.000001;
            u.GoalX = x; u.GoalY = y; u.Status = "Befehl übernommen";
            Report(c, true, "UnitOrderAccepted", "Einheitenbefehl übernommen.");
        }

        public static bool ValidWeaponTarget(bool antiAir, TargetLayer layer)
        { return antiAir ? layer == TargetLayer.LandingTransition : layer == TargetLayer.Ground; }

        private bool Target(PlanetState p, long id, out double x, out double y, out int owner, out TargetLayer layer)
        {
            var u = p.Units.Find(v => v.Id == id && !v.Destroyed);
            if (u != null) { x = u.X; y = u.Y; owner = u.OwnerId; layer = u.Layer; return true; }
            var b = p.Buildings.Find(v => v.Id == id && !v.Destroyed);
            if (b != null) { x = b.X; y = b.Y; owner = BuildingOwner(p,b); layer = TargetLayer.Ground; return true; }
            x = y = 0; owner = 0; layer = TargetLayer.Ground; return false;
        }

        private void TickMilitary(PlanetState p)
        {
            foreach (var b in p.Buildings)
            {
                if (b.Destroyed || b.Deconstructing || BuildingOwner(p,b)==0) continue;
                if (b.RecruitKind >= 0 && BuildingOwner(p,b)==p.OwnerId) TickRecruitment(p, b);
                if (Definitions.IsTower(b.Kind)) TickTower(p, b);
            }
            foreach (var u in p.Units)
            {
                if (u.Destroyed) continue;
                u.Cooldown = Math.Max(0, u.Cooldown - Definitions.TickSeconds);
                u.RationTime += Definitions.TickSeconds;
                if (u.RationTime >= Definitions.RationSeconds)
                { u.RationTime = 0; if (u.Food > 0) u.Food--; }
                if (u.Layer != TargetLayer.Ground) continue;
                TickUnit(p, u);
            }
            foreach (var faction in State.Factions)
            {
                foreach (var u in p.Units)
                    if (!u.Destroyed && u.OwnerId != faction.Id && CanSeeEntity(p, faction.Id, u.Id))
                        Remember(p, faction.Id, u.Id, HexGrid.CellAt(u.X, u.Y).X, HexGrid.CellAt(u.X, u.Y).Y);
                if (p.OwnerId != faction.Id)
                    foreach (var b in p.Buildings)
                        if (!b.Destroyed && CanSeeEntity(p, faction.Id, b.Id)) Remember(p, faction.Id, b.Id, b.X, b.Y);
            }
        }

        private void Remember(PlanetState p, int faction, long id, int x, int y)
        {
            var s = p.Sightings.Find(v => v.FactionId == faction && v.EntityId == id);
            if (s == null) { s = new SightingState { FactionId = faction, EntityId = id }; p.Sightings.Add(s); }
            s.X = x; s.Y = y; s.Tick = State.Tick;
        }

        private void TickRecruitment(PlanetState p, BuildingState b)
        {
            var kind = (UnitKind)b.RecruitKind; var d = Definitions.Get(kind);
            if (!b.Operational) return;
            if (!b.RecruitPaid)
            {
                // Leave the emergency workforce in the colony. Recruitment consumes population once.
                if (p.Population - d.Crew < 4) { b.Status = "Zu wenig Bevölkerung für Rekrutierung"; return; }
                var cost = d.Cost;
                for (int r = 0; r < 6; r++) if (b.Inventory[r] - b.ReservedOutgoing[r] < cost[r])
                { b.Status = "Wartet auf Rekrutierungsmaterial"; return; }
                for (int r = 0; r < 6; r++) { b.Inventory[r] -= cost[r]; p.ConsumedTotals[r] += cost[r]; }
                p.Population -= d.Crew; p.WorkersTotal = Math.Min(p.WorkersTotal, p.Population);
                p.WorkersUsed = Math.Min(p.WorkersUsed, p.WorkersTotal); b.RecruitPaid = true;
            }
            b.RecruitProgress = Math.Min(d.Seconds, b.RecruitProgress + Definitions.TickSeconds * p.Productivity);
            b.Status = "Rekrutierung " + (int)(100 * b.RecruitProgress / d.Seconds) + " %";
            if (b.RecruitProgress + 0.000001 < d.Seconds) return;

            for (int i = 0; i < 6; i++)
            {
                var neighbor = HexGrid.Neighbor(b.X, b.Y, i); int x = neighbor.X, y = neighbor.Y;
                if (!GroundCellAvailable(p,p.OwnerId,kind,x,y)) continue;
                p.Units.Add(new UnitState { Id = State.NextEntityId++, OwnerId = p.OwnerId, Kind = kind,
                    X = x, Y = y, Health = d.Health, Ammunition = d.Ammo, Food = d.Food, GoalX = x, GoalY = y });
                b.RecruitKind = -1; b.RecruitPaid = false; b.RecruitProgress = 0;
                Emit(p.OwnerId, p.Id, b.Id, "UnitProduced", d.Name + " einsatzbereit."); return;
            }
            b.Status = "Ausgang durch Einheiten oder Gebäude blockiert";
        }

        private void TickUnit(PlanetState p, UnitState u)
        {
            if(TickGroundYield(p,UnitAgent(u)))return;
            if(u.RecenterToCell&&!CenterGroundUnit(p,u))return;
            var d = Definitions.Get(u.Kind);
            if(EvacuatingUnit(p,u)){if(u.Order==UnitOrder.Move||u.Order==UnitOrder.Retreat)MoveGround(p,u,false);return;}
            if (u.Order == UnitOrder.Attack)
            {
                double x, y; int owner; TargetLayer layer;
                if (!Target(p, u.TargetId, out x, out y, out owner, out layer) || !AtWar(u.OwnerId, owner) ||
                    !CanSeeEntity(p, u.OwnerId, u.TargetId) || ProtectedGuest(p,u.OwnerId,u.TargetId))
                { u.Order = UnitOrder.Hold; u.TargetId = 0; u.Path.Clear(); u.PathDistance = 0; u.Status = "Ziel verloren; hält letzte Position"; return; }
                var goal = HexGrid.CellAt(x, y); u.GoalX = goal.X; u.GoalY = goal.Y;
                if (HexGrid.Distance(u.X, u.Y, x, y) <= d.Range && (d.Indirect || HasLineOfSight(p, u.X, u.Y, x, y)))
                { u.Path.Clear(); u.PathDistance = 0; FireUnit(p, u, u.TargetId, x, y, layer); return; }
                MoveGround(p, u, true); return;
            }
            if (u.Order == UnitOrder.Bombard)
            {
                if (HexGrid.Distance(u.X, u.Y, u.GoalX, u.GoalY) > d.Range) { MoveGround(p, u, true); return; }
                if (u.Cooldown > 0 || u.Ammunition <= 0) { u.Status = u.Ammunition <= 0 ? "Munition fehlt" : "Lädt nach"; return; }
                u.Ammunition--; u.Cooldown = d.Interval; u.Status = "Beschießt bekannte Position";
                RecordCombatShot(UnitShotKind(u.Kind), p.Id, u.Id, u.X, u.Y, p.Id, 0, u.GoalX, u.GoalY, u.OwnerId == State.PlayerFactionId);
                // Deliberate area fire does not track hidden targets or reveal hit information.
                foreach (var other in p.Units)
                    if (!other.Destroyed && other.Layer == TargetLayer.Ground && !ProtectedGuest(p,u.OwnerId,other.Id) && AtWar(u.OwnerId, other.OwnerId) &&
                        HexGrid.Distance(other.X, other.Y, u.GoalX, u.GoalY) <= Definitions.AreaFireRadius) HurtUnit(p, other, d.Damage, u.Kind);
                var b = p.Buildings.Find(v => !v.Destroyed && v.X == u.GoalX && v.Y == u.GoalY);
                if (b != null && AtWar(u.OwnerId, BuildingOwner(p,b))) HurtBuilding(p, b, d.Damage);
                return;
            }
            if (u.Order == UnitOrder.Resupply || u.Order == UnitOrder.Repair)
            {
                double x, y; int owner; TargetLayer layer;
                if (!Target(p, u.TargetId, out x, out y, out owner, out layer) || (owner != u.OwnerId && !HasAccess(u.OwnerId,owner,p.Id,u.Order==UnitOrder.Resupply?AccessRight.Supply:AccessRight.Repair)))
                { u.Order = UnitOrder.Hold; return; }
                var goal = HexGrid.CellAt(x, y); u.GoalX = goal.X; u.GoalY = goal.Y;
                var workTarget=p.Units.Find(v=>v.Id==u.TargetId&&!v.Destroyed);
                double workRange=workTarget==null?1.1:Math.Max(1.1,GroundUnitRadius(u.Kind)+GroundUnitRadius(workTarget.Kind)+.15);
                if (HexGrid.Distance(u.X, u.Y, x, y) > workRange) { MoveGround(p, u, true); return; }
                if (u.Order == UnitOrder.Resupply) Supply(p, u); else Repair(p, u);
                return;
            }
            if (u.Order == UnitOrder.Move || u.Order == UnitOrder.Retreat)
            { MoveGround(p, u, false); return; }
            long target = FindWeaponTarget(p, u.OwnerId, u.X, u.Y, d.Range, d.Indirect, d.AntiAir);
            if (target != 0)
            { double x, y; int owner; TargetLayer layer; Target(p, target, out x, out y, out owner, out layer); FireUnit(p, u, target, x, y, layer); }
            else u.Status = u.Ammunition == 0 ? "Hält Stellung; Munition fehlt" : "Hält Stellung";
        }

        private void MoveGround(PlanetState p, UnitState u, bool adjacent)
        {
            if(!adjacent&&(u.Order==UnitOrder.Move||u.Order==UnitOrder.Retreat))
                foreach(var other in p.Units)
                    if(other.Id!=u.Id&&!other.Destroyed&&other.Layer==TargetLayer.Ground&&other.Order==UnitOrder.Hold&&
                        (other.OwnerId==u.OwnerId||CanSeeEntity(p,u.OwnerId,other.Id))&&
                        HexGrid.Distance(other.X,other.Y,u.GoalX,u.GoalY)<GroundUnitRadius(u.Kind)+GroundUnitRadius(other.Kind)&&
                        HexGrid.Distance(u.X,u.Y,u.GoalX,u.GoalY)<=Math.Ceiling(GroundUnitRadius(u.Kind)+GroundUnitRadius(other.Kind)))
                    {u.Path.Clear();u.PathDistance=0;u.Order=UnitOrder.Hold;u.Status="Freien Platz am Ziel erreicht";return;}
            // Reroute only at cell centers; an invalid next segment never teleports a unit.
            if (u.Path.Count == 0 || u.PathDistance >= u.Path.Count - 1 - 0.000001)
            {
                var center = HexGrid.CellAt(u.X, u.Y); double cx = center.X, cy = center.Y;
                double offset = HexGrid.Distance(u.X, u.Y, cx, cy);
                if (offset > 0.000001)
                {
                    CenterGroundUnit(p,u);
                    return;
                }
                u.Path = StraightenGroundPath(p,u,GroundPath(p, u, u.GoalX, u.GoalY, adjacent)) ?? new List<GridCell>(); u.PathDistance = 0;u.DirectPath=true;
                if (u.Path.Count == 0) { u.Status = "Weg blockiert; Durchbruch oder andere Route nötig"; return; }
                if (u.Path.Count == 1)
                { if (u.Order == UnitOrder.Move || u.Order == UnitOrder.Retreat) u.Order = UnitOrder.Hold; u.Status = "Ziel erreicht"; return; }
            }
            int index = Math.Min((int)u.PathDistance, u.Path.Count - 2); var next = u.Path[index + 1];
            if (!CanTraverse(p, u.OwnerId, u.Kind, next.X, next.Y,u.Id))
            { if (u.DirectPath || u.PathDistance == index) { u.RecenterToCell=u.DirectPath;u.Path.Clear(); u.PathDistance = 0; } u.Status = "Weg blockiert"; return; }
            double speed = Definitions.Get(u.Kind).Speed;
            if(u.DirectPath)
            {
                var previousCell=u.Path[index];var from=HexGrid.ToWorld(previousCell.X,previousCell.Y);var to=HexGrid.ToWorld(next.X,next.Y);
                if(!GroundSegmentClear(p,u,GroundUnitPosition(u),to,false)){u.Path.Clear();u.PathDistance=0;u.RecenterToCell=true;u.Status="Weg geändert; zentriert sich";return;}
                double distance=Distance(from.X,from.Y,to.X,to.Y);
                double progress=Math.Min(index+1,u.PathDistance+speed*Definitions.TickSeconds/Math.Max(.000001,distance));
                var position=GroundLerp(from,to,progress-index);
                if(MoveGroundTraffic(p,u,position))
                {u.PathDistance=progress;SetGroundUnitPosition(u,position);if(progress==index+1){u.X=next.X;u.Y=next.Y;}u.Status="Unterwegs";}
                return;
            }
            // Stop at each cell to revalidate obstacles before crossing another segment.
            double nextDistance=Math.Min(index+1,u.PathDistance+speed*Definitions.TickSeconds);
            double fraction=nextDistance-index;var previous=u.Path[index];
            double nextX=previous.X+(next.X-previous.X)*fraction,nextY=previous.Y+(next.Y-previous.Y)*fraction;
            if(!MoveGroundTraffic(p,u,HexGrid.ToWorld(nextX,nextY)))
            {if(u.Traffic!=null&&!u.Traffic.Yielding&&u.Traffic.WaitTicks%12==0&&Math.Abs(u.PathDistance-index)<.000001){u.Path.Clear();u.PathDistance=0;}return;}
            u.PathDistance=nextDistance;u.X=nextX;u.Y=nextY;
            u.Status = u.Order == UnitOrder.Retreat ? "Zieht sich zurück" : "Unterwegs";
        }

        private long FindWeaponTarget(PlanetState p, int owner, double x, double y, double range, bool indirect, bool antiAir)
        {
            long result = 0; double nearest = range + 0.000001;
            foreach (var u in p.Units)
            {
                double distance = HexGrid.Distance(x, y, u.X, u.Y);
                if (u.Destroyed || ProtectedGuest(p,owner,u.Id) || !AtWar(owner, u.OwnerId) || !ValidWeaponTarget(antiAir, u.Layer) || distance >= nearest ||
                    !CanSeeEntity(p, owner, u.Id) || (!indirect && !HasLineOfSight(p, x, y, u.X, u.Y))) continue;
                nearest = distance; result = u.Id;
            }
            return result;
        }

        private void FireUnit(PlanetState p, UnitState u, long target, double x, double y, TargetLayer layer)
        {
            var d = Definitions.Get(u.Kind);
            if (!ValidWeaponTarget(d.AntiAir, layer)) return;
            if (u.Ammunition <= 0) { u.Status = "Munition fehlt; Depot oder Versorgung anfordern"; return; }
            if (u.Cooldown > 0) return;
            u.Ammunition--; u.Cooldown = d.Interval * (u.Food > 0 ? 1 : Definitions.UnfedReloadFactor); u.Status = "Feuert";
            RecordCombatShot(UnitShotKind(u.Kind), p.Id, u.Id, u.X, u.Y, p.Id, target, x, y);
            var enemy = p.Units.Find(v => v.Id == target);
            if (enemy != null) HurtUnit(p, enemy, d.Damage, u.Kind);
            else { var b = p.Buildings.Find(v => v.Id == target); if (b != null) HurtBuilding(p, b, d.Damage); }
        }

        public bool HasCover(PlanetState p, UnitState u)
        {
            if (u.Kind != UnitKind.Infantry && u.Kind != UnitKind.AntiTank) return false;
            var cell = HexGrid.CellAt(u.X, u.Y); int x = cell.X, y = cell.Y;
            if (p.Buildings.Exists(b => !b.Destroyed && HexGrid.Steps(b.X, b.Y, x, y) <= 1)) return true;

            for (int i = 0; i < 6; i++)
            { var n = HexGrid.Neighbor(x, y, i); if (Inside(p, n.X, n.Y) && p.Terrain[n.Y * p.Width + n.X] == 1) return true; }
            return false;
        }

        private void HurtUnit(PlanetState p, UnitState u, double damage, UnitKind attacker)
        {
            if (u.Destroyed) return;
            if (u.Kind == UnitKind.Tank) damage *= attacker == UnitKind.AntiTank ? Definitions.TankAntiArmorFactor :
                attacker == UnitKind.Infantry ? Definitions.TankInfantryFactor : Definitions.TankOtherDamageFactor;
            if (HasCover(p, u)) damage *= Definitions.CoverDamageFactor;
            u.Health = Math.Max(0, u.Health - damage);
            if (u.Health > 0) return;
            RecordCombatExplosion(p.Id, u.Id, u.X, u.Y);
            u.Destroyed = true; u.Path.Clear(); u.PathDistance = 0; u.Status = "Vernichtet";
            p.LostTotals[(int)ResourceKind.Parts] += u.Parts; u.Parts = 0;
            Emit(u.OwnerId, p.Id, u.Id, "UnitDestroyed", Definitions.Get(u.Kind).Name + " verloren.");
        }

        private void TickTower(PlanetState p, BuildingState b)
        {
            b.FireCooldown = Math.Max(0, b.FireCooldown - Definitions.TickSeconds);
            if (!b.Built || !b.Operational) return;
            if (b.Ammunition <= Definitions.TowerAmmoCapacity - Definitions.AmmoPerPart &&
                b.Inventory[0] - b.ReservedOutgoing[0] > 0 && b.Inventory[3] - b.ReservedOutgoing[3] > 0)
            { b.Inventory[0]--; b.Inventory[3]--; p.ConsumedTotals[0]++; p.ConsumedTotals[3]++; b.Ammunition += Definitions.AmmoPerPart; }
            var weapon = Definitions.GetTower(b.Kind);
            if (b.Ammunition == 0) { b.Status = "Munition fehlt; Nahrung und Bauteile anliefern"; return; }
            if (b.FireCooldown > 0) return;
            long target = FindWeaponTarget(p, BuildingOwner(p,b), b.X, b.Y, weapon.Range, weapon.Indirect, weapon.AntiAir);
            if (target == 0) return;
            b.Ammunition--; b.FireCooldown = weapon.Interval;
            var enemy = p.Units.Find(u => u.Id == target);
            RecordCombatShot(UnitShotKind(weapon.AttackKind), p.Id, b.Id, b.X, b.Y, p.Id, enemy.Id, enemy.X, enemy.Y);
            HurtUnit(p, enemy, weapon.Damage, weapon.AttackKind);
        }

        private void Supply(PlanetState p, UnitState u)
        {
            if (u.Cooldown > 0) return;
            var depot = p.Buildings.Find(b => b.Id == u.TargetId && b.Kind == BuildingKind.MilitaryDepot);
            if (depot != null)
            {
                if (!depot.Operational || depot.Destroyed) { u.Status = "Depot ohne Betriebsmittel"; return; }
                if (u.Food < 4 && depot.Inventory[0] - depot.ReservedOutgoing[0] > 0)
                { depot.Inventory[0]--; p.ConsumedTotals[0]++; u.Food++; }
                if (u.Ammunition < Definitions.Get(u.Kind).Ammo && depot.Inventory[3] - depot.ReservedOutgoing[3] > 0)
                { depot.Inventory[3]--; p.ConsumedTotals[3]++; u.Ammunition = Math.Min(Definitions.Get(u.Kind).Ammo, u.Ammunition + Definitions.AmmoPerPart); }
                if (u.Kind == UnitKind.Engineer && u.Parts < 8 && depot.Inventory[3] - depot.ReservedOutgoing[3] > 0)
                { depot.Inventory[3]--; u.Parts++; }
                u.Status = "Am Depot; Vorräte werden nachgefüllt"; u.Cooldown = Definitions.SupplyTransferSeconds; return;
            }
            var target = p.Units.Find(v => v.Id == u.TargetId && !v.Destroyed);
            if (target == null || u.Kind != UnitKind.Engineer) return;
            if (u.Parts > 0 && target.Ammunition < Definitions.Get(target.Kind).Ammo)
            { u.Parts--; p.ConsumedTotals[3]++; target.Ammunition = Math.Min(Definitions.Get(target.Kind).Ammo, target.Ammunition + Definitions.AmmoPerPart); }
            if (u.Food > 1 && target.Food < 4) { u.Food--; target.Food++; }
            u.Status = u.Parts == 0 ? "Bauteile erschöpft; Depot anfahren" : "Versorgt Verband"; u.Cooldown = Definitions.SupplyTransferSeconds;
        }

        private void Repair(PlanetState p, UnitState u)
        {
            if (u.Cooldown > 0) return;
            if (u.Parts == 0) { u.Status = "Reparatur benötigt mitgeführte Bauteile"; return; }
            var target = p.Units.Find(v => v.Id == u.TargetId && !v.Destroyed);
            var building = p.Buildings.Find(b => b.Id == u.TargetId && !b.Destroyed);
            if (target != null && target.Health < Definitions.Get(target.Kind).Health)
                target.Health = Math.Min(Definitions.Get(target.Kind).Health, target.Health + Definitions.RepairPerPart);
            else if (building != null && building.Health < Definitions.BuildingHealth(building.Kind))
                building.Health = Math.Min(Definitions.BuildingHealth(building.Kind), building.Health + Definitions.RepairPerPart);
            else { u.Order = UnitOrder.Hold; return; }
            u.Parts--; p.ConsumedTotals[3]++; u.Cooldown = Definitions.SupplyTransferSeconds; u.Status = "Repariert";
        }

        private void HurtBuilding(PlanetState p, BuildingState b, double damage)
        {
            if (b.Destroyed) return;
            b.Health = Math.Max(0, b.Health - damage);
            if (b.Health > 0) return;
            RecordCombatExplosion(p.Id, b.Id, b.X, b.Y);
            // Cancel delivery relationships through the proven return-cargo flow first.
            // Military destruction has zero building-cost recovery; real courier cargo survives.
            var stock = (int[])b.Inventory.Clone();
            int recruits = b.RecruitKind >= 0 && b.RecruitPaid ? Definitions.Get((UnitKind)b.RecruitKind).Crew : 0;
            int minedOre = b.CycleActive && (b.Kind == BuildingKind.Mine || b.Kind == BuildingKind.CrystalMine) ? Definitions.Get(b.Kind).Outputs[b.Kind == BuildingKind.CrystalMine ? 4 : 1] : 0;
            b.Destroyed = true; BeginDeconstruction(p, b);
            p.Population -= recruits;
            p.OreRemaining[b.Y * p.Width + b.X] -= minedOre;
            for (int r = 0; r < 6; r++)
            {
                p.RecoveredTotals[r] -= b.Inventory[r] - stock[r];
                p.LostTotals[r] += stock[r]; b.Inventory[r] = 0;
            }
            b.Destroyed = true; b.Operational = false; b.Ammunition = 0;
            // Outgoing uncollected stock is lost; loaded shipments keep their real cargo.
            for (int i = p.Deliveries.Count - 1; i >= 0; i--)
                if (p.Deliveries[i].SourceId == b.Id)
                { var o = p.Deliveries[i]; p.Buildings.Find(v => v.Id == o.TargetId).ReservedIncoming[(int)o.Resource] -= o.Amount; b.ReservedOutgoing[(int)o.Resource] -= o.Amount; p.Deliveries.RemoveAt(i); }
            for (int i = p.Shipments.Count - 1; i >= 0; i--)
                if (p.Shipments[i].SourceId == b.Id && p.Shipments[i].Phase == ShipmentPhase.Loading)
                { var s = p.Shipments[i]; p.Buildings.Find(v => v.Id == s.TargetId).ReservedIncoming[(int)s.Resource] -= s.Amount; b.ReservedOutgoing[(int)s.Resource] -= s.Amount; p.Shipments.RemoveAt(i); }
            Emit(p.OwnerId, p.Id, b.Id, "BuildingDestroyed", Definitions.Get(b.Kind).Name + " zerstört.");
        }
    }
}
