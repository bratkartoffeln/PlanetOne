using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public static partial class SaveCodec
    {
        private static void MigrateMilitary(GameState state)
        {
            Require(state.Planets != null, "Alte Weltliste fehlt.");
            foreach (var p in state.Planets)
            {
                Require(p != null && p.Buildings != null, "Alter Koloniezustand fehlt.");
                p.Units = new List<UnitState>(); p.Sightings = new List<SightingState>();
                p.GatePermissions = new List<GatePermission>(); p.LostTotals = new int[Definitions.ResourceCount];
                foreach (var b in p.Buildings)
                {
                    b.Health = Definitions.BuildingHealth(b.Kind); b.Destroyed = false;
                    b.Operational = false; b.Ammunition = 0; b.FireCooldown = 0;
                    b.RecruitKind = -1; b.RecruitPaid = false; b.RecruitProgress = 0;
                }
            }
            state.MilitaryTestFixture = false; state.Version = 3;
        }

        private static void ValidateMilitary(GameState state, PlanetState p, HashSet<int> factions, HashSet<long> entities)
        {
            Goods(p.LostTotals);
            Require(p.Units != null && p.Sightings != null && p.GatePermissions != null && p.Units.Count <= 10000, "Militärzustand fehlt oder ist zu groß.");
            foreach (var b in p.Buildings)
            {
                Require(Finite(b.Health) && b.Health >= 0 && b.Health <= Definitions.BuildingHealth(b.Kind) &&
                    b.Destroyed == (b.Health == 0) && (!b.Destroyed || b.Deconstructing), "Ungültiger Gebäudeschaden.");
                Require(b.Ammunition >= 0 && b.Ammunition <= Definitions.TowerAmmoCapacity &&
                    Finite(b.FireCooldown) && b.FireCooldown >= 0 && Finite(b.RecruitProgress) && b.RecruitProgress >= 0,
                    "Ungültige Militärproduktion.");
                Require(b.RecruitKind == -1 || (Enum.IsDefined(typeof(UnitKind), b.RecruitKind) &&
                    Definitions.Producer((UnitKind)b.RecruitKind) == b.Kind && b.Built && !b.Deconstructing &&
                    b.RecruitProgress <= Definitions.Get((UnitKind)b.RecruitKind).Seconds), "Ungültiger Rekrutierungsauftrag.");
                Require(b.RecruitQueue==null || b.RecruitKind>=0 && b.RecruitQueue.TrueForAll(kind=>
                    Enum.IsDefined(typeof(UnitKind),kind) && Definitions.Producer(kind)==b.Kind),"Ungültige Ausbildungswarteschlange.");
                Require(b.RecruitKind != -1 || (!b.RecruitPaid && b.RecruitProgress == 0), "Verwaiste Rekrutierung.");
            }
            foreach (var u in p.Units)
            {
                Require(u != null && u.Id > 0 && entities.Add(u.Id) && factions.Contains(u.OwnerId) &&
                    Enum.IsDefined(typeof(UnitKind), u.Kind) && Enum.IsDefined(typeof(UnitOrder), u.Order) &&
                    Enum.IsDefined(typeof(TargetLayer), u.Layer), "Ungültige Einheit.");
                var d = Definitions.Get(u.Kind);
                Require(Finite(u.X) && Finite(u.Y) && u.X >= 0 && u.Y >= 0 && u.X <= p.Width - 1 && u.Y <= p.Height - 1 &&
                    Finite(u.Health) && u.Health >= 0 && u.Health <= d.Health && u.Destroyed == (u.Health == 0), "Ungültiger Einheitenort oder Schaden.");
                Require(u.Ammunition >= 0 && u.Ammunition <= d.Ammo && u.Food >= 0 && u.Food <= 4 && u.Parts >= 0 && u.Parts <= 8 &&
                    (u.Kind == UnitKind.Engineer || u.Parts == 0) && Finite(u.Cooldown) && u.Cooldown >= 0 &&
                    Finite(u.RationTime) && u.RationTime >= 0 && u.RationTime < Definitions.RationSeconds + 0.000001,
                    "Ungültige Einheitenvorräte.");
                Require(u.Layer == TargetLayer.Ground || state.MilitaryTestFixture, "Übergangsziel benötigt eine ausgewiesene P12-Testfixture.");
                Require(u.Path != null && Finite(u.PathDistance) && u.PathDistance >= 0 &&
                    u.PathDistance <= Math.Max(0, u.Path.Count - 1), "Ungültiger Bodenweg.");
                Require(u.GoalX >= 0 && u.GoalY >= 0 && u.GoalX < p.Width && u.GoalY < p.Height, "Ungültiges Bodenziel.");
                for (int i = 0; i < u.Path.Count; i++)
                {
                    var c = u.Path[i];
                    Require(c.X >= 0 && c.Y >= 0 && c.X < p.Width && c.Y < p.Height, "Bodenweg außerhalb der Karte.");
                    if (i > 0) Require(u.DirectPath?c.X!=u.Path[i-1].X||c.Y!=u.Path[i-1].Y:HexGrid.Adjacent(c, u.Path[i - 1]), "Ungültige Bodenwegpunkte.");
                }
                if (u.Path.Count > 0 && (u.Traffic==null||!u.Traffic.Yielding))
                {
                    int i = Math.Min((int)u.PathDistance, u.Path.Count - 1);
                    var a = u.Path[i]; var b = u.Path[Math.Min(i + 1, u.Path.Count - 1)]; double t = u.PathDistance - i;
                    if(u.DirectPath)
                    {
                        var from=HexGrid.ToWorld(a.X,a.Y);var to=HexGrid.ToWorld(b.X,b.Y);var at=HexGrid.ToWorld(u.X,u.Y);
                        Require(Math.Abs(at.X-(from.X+(to.X-from.X)*t))<.000001&&Math.Abs(at.Y-(from.Y+(to.Y-from.Y)*t))<.000001,"Direkter Bodenweg und Position widersprechen sich.");
                    }
                    else Require(Math.Abs(u.X - (a.X + (b.X - a.X) * t)) < 0.000001 &&
                        Math.Abs(u.Y - (a.Y + (b.Y - a.Y) * t)) < 0.000001, "Bodenweg und Einheitenposition widersprechen sich.");
                }
                ValidateGroundTraffic(state,p,u.Traffic);
                Require(u.Traffic==null||!u.Traffic.Docked,"Bodentruppe besitzt einen ungültigen Fahrzeugdockzustand.");
                if(u.Traffic!=null&&u.Traffic.Yielding)
                {
                    var goal=HexGrid.ToWorld(u.Traffic.GoalX,u.Traffic.GoalY);var at=GameSimulation.GroundUnitPosition(u);
                    Require(Math.Abs(at.X-(u.Traffic.StartX+(goal.X-u.Traffic.StartX)*u.Traffic.Progress))<.000001&&Math.Abs(at.Y-(u.Traffic.StartY+(goal.Y-u.Traffic.StartY)*u.Traffic.Progress))<.000001,"Ausweichweg und Einheitenposition widersprechen sich.");
                }
            }
            if(p.Id>0)
            {
                var cargoIndices=new HashSet<int>();var positions=new List<SimPoint>();var radii=new List<double>();
                foreach(var unit in p.Units)if(!unit.Destroyed&&unit.Layer==TargetLayer.Ground){positions.Add(GameSimulation.GroundUnitPosition(unit));radii.Add(GameSimulation.GroundUnitRadius(unit.Kind));}
                foreach(var shipment in p.Shipments)
                {
                    ValidateGroundTraffic(state,p,shipment.Traffic);
                    if(GameSimulation.GroundCourierDocked(shipment))
                    {
                        var at=GameSimulation.GroundCourierPosition(shipment);
                        Require(p.Buildings.Exists(b=>(b.Id==shipment.SourceId||b.Id==shipment.TargetId)&&GroundAtBuilding(at,b)),"Innenfahrzeug steht außerhalb seines tatsächlichen Gebäudes.");
                    }
                    else {cargoIndices.Add(positions.Count);positions.Add(GameSimulation.GroundCourierPosition(shipment));radii.Add(GameSimulation.GroundCourierRadius(shipment));}
                }
                for(int i=0;i<positions.Count;i++)for(int j=i+1;j<positions.Count;j++)
                {if(cargoIndices.Contains(i)&&cargoIndices.Contains(j)) continue;double dx=positions[i].X-positions[j].X,dy=positions[i].Y-positions[j].Y;double radius=radii[i]+radii[j]-GameSimulation.GroundTrafficTolerance;Require(dx*dx+dy*dy>=radius*radius,"Bodeneinheiten oder Fahrzeuge überlagern sich.");}
            }
            var sightings = new HashSet<string>();
            foreach (var s in p.Sightings)
                Require(s != null && factions.Contains(s.FactionId) && s.EntityId > 0 && s.EntityId < state.NextEntityId &&
                    s.Tick >= 0 && s.Tick <= state.Tick && s.X >= 0 && s.Y >= 0 && s.X < p.Width && s.Y < p.Height &&
                    sightings.Add(s.FactionId + ":" + s.EntityId), "Ungültige letzte Sichtung.");
            var gates = new HashSet<string>();
            foreach (var g in p.GatePermissions)
                Require(g != null && factions.Contains(g.FactionId) &&
                    p.Buildings.Exists(b => b.Id == g.GateId && b.Kind == BuildingKind.Gate) &&
                    gates.Add(g.FactionId + ":" + g.GateId), "Ungültiges Torrecht.");
        }
        private static void ValidateGroundTraffic(GameState state,PlanetState planet,GroundTrafficState traffic)
        {
            if(traffic==null)return;
            Require(traffic.WaitTicks>=0&&Finite(traffic.Progress)&&traffic.Progress>=0&&traffic.Progress<=1&&Finite(traffic.StartX)&&Finite(traffic.StartY)&&Finite(traffic.RequestX)&&Finite(traffic.RequestY),"Ungültiger Bodenverkehrszustand.");
            Require(!traffic.Yielding || (!traffic.Docked||traffic.Progress==1)&&traffic.YieldForId>0&&traffic.YieldForId<state.NextEntityId&&traffic.YieldSinceTick>=0&&traffic.YieldSinceTick<=state.Tick&&traffic.GoalX>=0&&traffic.GoalY>=0&&traffic.GoalX<planet.Width&&traffic.GoalY<planet.Height,"Ungültiges Ausweichmanöver.");
            Require(!traffic.Yielding || traffic.StartX>=0&&traffic.StartX<=planet.Width-.5&&traffic.StartY>=0&&traffic.StartY<=(planet.Height-1)*HexGrid.RowHeight,"Ausweichfahrt beginnt außerhalb der Karte.");
        }
        private static bool GroundAtBuilding(SimPoint position,BuildingState building)
        {var center=HexGrid.ToWorld(building.X,building.Y);double dx=center.X-position.X,dy=center.Y-position.Y;return dx*dx+dy*dy<=(GameSimulation.RoadLaneOffset+.000001)*(GameSimulation.RoadLaneOffset+.000001);}
    }
}
