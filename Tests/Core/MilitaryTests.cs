using System;
using System.IO;
using PlanetOne.Core;

internal static class MilitaryTests
{
    internal static void Register(Action<string, Action> run)
    {
        run("Workshop repairs local machinery using finite parts and damage limits mobility and output", FieldMaintenance);
        run("Tanks fire from a moving column without losing their destination", MovingFire);
        run("Gate traffic uses only the opposing pair aligned with its aperture", GateAperture);
        run("Three infantry share distinct stands and a fourth takes a neighbouring cell", InfantryStands);
        run("M3 recruits through construction and physical deliveries exactly once", Recruitment);
        run("M3 planet recruitment limits and one-at-a-time cancellation", RecruitmentLimit);
        run("M3 active recruitment cancellation refunds only paid costs", CancelActiveOrder);
        run("M3 hidden targets reject commands and stale sightings do not track", Visibility);
        run("M3 peace and ownership prevent unauthorized attacks", Rights);
        run("M3 range and cover change actual damage", Combat);
        run("M3 two routes support reserve movement and retreat without teleport", Navigation);
        run("M3 gates allow only explicit factions and destroyed walls reopen routes", Gates);
        run("M3 artillery needs knowledge and breaches a closed wall", Artillery);
        run("M3 tower ammunition and disconnected supply are finite", Supply);
        run("M3 engineers transport parts and repair with actual consumption", Repair);
        run("M3 flak hits transition targets but never space or ground units", Flak);
        run("M3 destruction clears reservations and preserves loaded cargo", Destruction);
        run("M3 save load preserves movement recruitment and combat continuation", Persistence);
        run("M3 version two migrates and corrupt military state is rejected", Migration);
        run("M3 finite defense fixture supports both approaches and military saves", Fixture);
        run("M3 cancelled paid recruitment recovers local goods and people without a unit", CancelRecruitment);
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static GameSimulation Empty()
    {
        var s = GameSimulation.CreateDefault(3003); var p = s.FindPlanet(1);
        p.Buildings.Clear(); p.Population = 0; p.WorkersTotal = 0; p.HousingCapacity = 0;
        Array.Clear(p.Terrain, 0, p.Terrain.Length); Array.Clear(p.Roads, 0, p.Roads.Length);
        s.State.Relations[0].Relation = RelationKind.War; s.State.MilitaryTestFixture = true;
        return s;
    }
    private static UnitState Unit(GameSimulation s, UnitKind kind, int owner, int x, int y)
    {
        var d = Definitions.Get(kind); var u = new UnitState { Id = s.State.NextEntityId++, Kind = kind,
            OwnerId = owner, X = x, Y = y, Health = d.Health, Ammunition = d.Ammo, Food = 2, GoalX = x, GoalY = y };
        s.FindPlanet(1).Units.Add(u); return u;
    }
    private static BuildingState Building(GameSimulation s, BuildingKind kind, int x, int y)
    {
        var b = new BuildingState { Id = s.State.NextEntityId++, PlanetId = 1, Kind = kind, X = x, Y = y,
            Built = true, Health = Definitions.BuildingHealth(kind) };
        s.FindPlanet(1).Buildings.Add(b); return b;
    }
    private static void Command(GameSimulation s, UnitState u, CommandKind kind, int x = 0, int y = 0, long target = 0)
    { s.Enqueue(new SimCommand { Id = s.NextCommandId(), IssuerId = u.OwnerId, PlanetId = 1, ActorId = u.Id, Kind = kind, X = x, Y = y, TargetId = target }); s.Step(); }
    private static string Result(GameSimulation s) { return s.State.CommandResults[s.State.CommandResults.Count - 1].Code; }
    private static void FieldMaintenance()
    {
        var s=Empty();var p=s.FindPlanet(1);var shop=Unit(s,UnitKind.MobileWorkshop,1,7,7);
        var tank=Unit(s,UnitKind.Tank,1,8,7);var soldier=Unit(s,UnitKind.Infantry,1,7,8);
        var far=Unit(s,UnitKind.Tank,1,14,8);soldier.Health=far.Health=tank.Health=10;shop.Parts=2;
        int consumed=p.ConsumedTotals[3];s.Step();
        Check(tank.Health==26 && shop.ActiveRepairTarget==tank.Id,"Nearby repair did not start");
        s.Step(90);Check(tank.Health==42 && shop.Parts==0 && shop.ActiveRepairTarget==0 && p.ConsumedTotals[3]==consumed+2,"Parts or repair marker incorrect");
        Check(soldier.Health==10 && far.Health==10,"Repaired ineligible target");
        var building=Building(s,BuildingKind.Workshop,7,6);building.Health=150;shop.Parts=1;
        Command(s,shop,CommandKind.RepairTarget,target:building.Id);s.Step(30);
        Check(building.Health==160 && shop.ActiveRepairTarget==0 && shop.Parts==0,"Building repair did not finish cleanly");
        double lastSpeed=10,lastOutput=2;
        foreach(double fraction in new[]{1,.75,.5,.25})
        {
            tank.Health=180*fraction;building.Health=160*fraction;
            Check(Definitions.GroundSpeed(tank)<lastSpeed && Definitions.ProductionRate(building)<lastOutput,"Damage tier missing");
            lastSpeed=Definitions.GroundSpeed(tank);lastOutput=Definitions.ProductionRate(building);
        }
    }
    private static void MovingFire()
    {
        var s=Empty();var tank=Unit(s,UnitKind.Tank,1,5,5);var enemy=Unit(s,UnitKind.Tank,2,6,7);enemy.Ammunition=0;
        Command(s,tank,CommandKind.MoveUnit,12,5);s.Step(3);
        Check(tank.X>5 && tank.Order==UnitOrder.Move && tank.GoalX==12 && tank.Ammunition<16 && enemy.Health<180,"Tank did not fire while advancing");
    }
    private static void GateAperture()
    {
        var s=Empty();var p=s.FindPlanet(1);var cell=new GridCell(5,6);
        Building(s,BuildingKind.Gate,5,6);Building(s,BuildingKind.Wall,5,5);Building(s,BuildingKind.Wall,5,7);
        int open=0;
        for(int i=0;i<6;i++)
        {
            var n=HexGrid.Neighbor(5,6,i);bool pass=GameSimulation.GateEdgeOpen(p,cell,n);
            Check(pass==GameSimulation.GateEdgeOpen(p,n,cell) && pass==GameSimulation.GateEdgeOpen(p,cell,HexGrid.Neighbor(5,6,(i+3)%6)),"Passage is asymmetric");
            if(pass)open++;
        }
        Check(open==2,"Gate has more than one passage axis");
    }
    private static void InfantryStands()
    {
        var s=Empty();var troops=new[]{Unit(s,UnitKind.Infantry,1,9,9),Unit(s,UnitKind.AntiTank,1,6,8),Unit(s,UnitKind.Infantry,1,12,10),Unit(s,UnitKind.Infantry,1,9,13)};
        for(int i=1;i<4;i++)Command(s,troops[i],CommandKind.MoveUnit,9,9);
        s.Step(180);
        for(int i=0;i<troops.Length;i++)
        {
            var u=troops[i];var at=HexGrid.CellAt(u.X,u.Y);
            Check(u.Order==UnitOrder.Hold && (at.X==9 && at.Y==9)==(i<3),"Unexpected infantry stand "+i+" at "+u.X+","+u.Y+" / "+u.Status);
            for(int j=0;j<i;j++)Check(HexGrid.Distance(u.X,u.Y,troops[j].X,troops[j].Y)>.43,"Two infantry overlap");
        }
        SaveCodec.Validate(s.State);
        Command(s,troops[1],CommandKind.MoveUnit,4,4);s.Step(160);
        Check(troops[1].Order==UnitOrder.Hold && HexGrid.Distance(troops[1].X,troops[1].Y,4,4)<.001,"Infantry could not leave its shared hex");
        for(int i=0;i<3;i++)Command(s,troops[i],CommandKind.MoveUnit,0,0);
        s.Step(360);
        for(int i=0;i<3;i++)Check(troops[i].Order==UnitOrder.Hold && HexGrid.CellAt(troops[i].X,troops[i].Y).Equals(new GridCell(0,0)),"Map-edge stand unreachable");
        SaveCodec.Validate(s.State);
    }
    private static void Recruitment()
    {
        var s = GameSimulation.CreateDefault(); var p = s.FindPlanet(1);
        s.Enqueue(new SimCommand { Id = s.NextCommandId(), PlanetId = 1, Kind = CommandKind.PlaceBuilding, Building = BuildingKind.Barracks, X = 7, Y = 5 });
        s.Step(1200); var b = p.Buildings.Find(v => v.Kind == BuildingKind.Barracks);
        Check(b != null && b.Built, "Barracks not built through couriers");
        int metal = p.ConsumedTotals[2], population = p.Population;
        var c = new SimCommand { Id = s.NextCommandId(), PlanetId = 1, Kind = CommandKind.RecruitUnit, TargetId = b.Id, IntValue = (int)UnitKind.Infantry };
        s.Enqueue(c); s.Enqueue(c); s.Step();
        Check(p.Units.Count == 0 && p.Deliveries.Count + p.Shipments.Count > 0, "Recruitment bypassed delivery/time");
        s.Step(1000);
        Check(p.Units.Count == 1 && p.Population <= population && p.ConsumedTotals[2] - metal == Definitions.Get(UnitKind.Infantry).Metal, "Recruitment duplicated or wrong cost");
        SaveCodec.Validate(s.State);
    }
    private static void Recruit(GameSimulation sim,BuildingState site,UnitKind kind,bool cancel=false)
    {
        sim.Enqueue(new SimCommand { Id=sim.NextCommandId(), PlanetId=site.PlanetId, TargetId=site.Id,
            Kind=cancel?CommandKind.CancelRecruitUnit:CommandKind.RecruitUnit, IntValue=(int)kind });
    }

    private static void RecruitmentLimit()
    {
        var sim=Empty();
        var first=Building(sim,BuildingKind.VehicleFactory,4,4);
        var second=Building(sim,BuildingKind.VehicleFactory,6,4);
        var barracks=Building(sim,BuildingKind.Barracks,8,4);
        var colony=sim.FindPlanet(2);
        colony.OwnerId=1;
        var remote=new BuildingState {Id=sim.State.NextEntityId++,PlanetId=colony.Id,Kind=BuildingKind.VehicleFactory,Built=true,Health=160};
        colony.Buildings.Add(remote);
        for(int i=0;i<19;i++)Recruit(sim,first,i%2==0?UnitKind.Tank:UnitKind.Artillery);
        Recruit(sim,second,UnitKind.Tank);
        Recruit(sim,second,UnitKind.Artillery);
        sim.Step();
        Check(Result(sim)=="RecruitmentQueueFull" && first.RecruitQueue.Count==18 && second.RecruitQueue==null,"Two factories exceeded their combined quota");
        for(int i=0;i<21;i++)Recruit(sim,barracks,i%2==0?UnitKind.Infantry:UnitKind.AntiTank);
        sim.Step();
        Check(Result(sim)=="RecruitmentQueueFull" && barracks.RecruitQueue.Count==19,"Troop limit is not independent of vehicles");
        Recruit(sim,remote,UnitKind.Artillery);
        Recruit(sim,first,UnitKind.Artillery,true);
        Recruit(sim,second,UnitKind.Artillery);
        sim.Step();
        Check(remote.RecruitKind==(int)UnitKind.Artillery && second.RecruitQueue.Count==1,"Released or remote slot unavailable");
        Check(first.RecruitQueue.Count==17 && first.RecruitQueue[15]==UnitKind.Tank && first.RecruitQueue[16]==UnitKind.Tank,"Did not remove just the last matching artillery");
    }

    private static void CancelActiveOrder()
    {
        var sim=GameSimulation.CreateDefault();var p=sim.FindPlanet(1);
        var site=Building(sim,BuildingKind.Barracks,7,5);
        var cost=Definitions.Get(UnitKind.Infantry).Cost;
        site.Inventory=(int[])cost.Clone();
        int population=p.Population;
        Recruit(sim,site,UnitKind.Infantry);Recruit(sim,site,UnitKind.AntiTank);
        sim.Step();
        Check(site.RecruitPaid && p.Population<population,"Fixture did not pay for active infantry");
        site.Enabled=false;
        Recruit(sim,site,UnitKind.Infantry,true);Recruit(sim,site,UnitKind.Infantry,true);
        sim.Step();
        Check(site.RecruitKind==(int)UnitKind.AntiTank && !site.RecruitPaid && site.RecruitProgress==0 && site.RecruitQueue==null,"Next job did not advance exactly once");
        Check(Result(sim)=="NoRecruitment" && p.Population==population && p.Units.Count==0,"Crew refund duplicated or phantom unit");
        for(int r=0;r<cost.Length;r++)Check(site.Inventory[r]==cost[r],"Wrong refund at production site");
        SaveCodec.Validate(sim.State);
    }
    private static void Visibility()
    {
        var s = Empty(); var p = s.FindPlanet(1); var a = Unit(s, UnitKind.Infantry, 1, 2, 5); var b = Unit(s, UnitKind.Infantry, 2, 15, 5);
        Command(s, a, CommandKind.AttackTarget, target: b.Id); Check(Result(s) == "TargetNotVisible", "Hidden target accepted");
        b.X = 7; s.Step(); var sight = p.Sightings.Find(v => v.FactionId == 1 && v.EntityId == b.Id);
        Check(sight != null, "Visible target not remembered"); long tick = sight.Tick; b.X = 15; s.Step(3);
        Check(sight.X == 7 && sight.Tick == tick, "Stale sight tracked hidden unit");
        b.X = 5; Command(s, a, CommandKind.AttackTarget, target: b.Id); b.X = 15; s.Step();
        Check(a.Order == UnitOrder.Hold && a.TargetId == 0, "Attack pursued hidden target");
    }
    private static void Rights()
    {
        var s = Empty(); var a = Unit(s, UnitKind.Infantry, 1, 2, 2); var b = Unit(s, UnitKind.Infantry, 2, 4, 2);
        s.State.Relations[0].Relation = RelationKind.Peace; Command(s, a, CommandKind.AttackTarget, target: b.Id);
        Check(Result(s) == "NotAtWar" && b.Health == Definitions.Get(b.Kind).Health, "Peace violated");
        s.Enqueue(new SimCommand { Id = s.NextCommandId(), PlanetId = 1, IssuerId = 2, ActorId = a.Id, Kind = CommandKind.MoveUnit, X = 8, Y = 8 }); s.Step();
        Check(Result(s) == "PermissionDenied", "Foreign unit controlled");
    }
    private static double OneShot(bool cover)
    {
        var s = Empty(); var p = s.FindPlanet(1); var a = Unit(s, UnitKind.Infantry, 1, 3, 4); var b = Unit(s, UnitKind.Infantry, 2, 6, 4); b.Ammunition = 0;
        if (cover) p.Terrain[5 * p.Width + 6] = 1;
        s.Step(); return Definitions.Get(b.Kind).Health - b.Health;
    }
    private static void Combat()
    {
        Check(OneShot(true) < OneShot(false), "Cover does not reduce damage");
        var s = Empty(); var a = Unit(s, UnitKind.Infantry, 1, 2, 2); var b = Unit(s, UnitKind.Infantry, 2, 8, 2); s.Step(10);
        Check(a.Health == Definitions.Get(a.Kind).Health && b.Health == Definitions.Get(b.Kind).Health, "Out of range damage");
        b.X = 4; s.Step(600); Check(a.Destroyed || b.Destroyed, "Combat never destroys units");
    }
    private static void Navigation()
    {
        var s = Empty(); var p = s.FindPlanet(1); for (int y = 3; y <= 8; y++) p.Terrain[y * p.Width + 6] = 1;
        var u = Unit(s, UnitKind.Tank, 1, 3, 5); Command(s, u, CommandKind.MoveUnit, 10, 5);
        for (int i = 0; i < 300; i++) { double x = u.X, y = u.Y; s.Step(); Check(HexGrid.Distance(u.X, u.Y, x, y) <= Definitions.Get(u.Kind).Speed * Definitions.TickSeconds + 0.000001, "Unit teleported"); }
        Check(Math.Abs(u.X - 10) < 0.01 && Math.Abs(u.Y - 5) < 0.01, "Reserve failed first route");
        p.Terrain[2 * p.Width + 6] = 1; Command(s, u, CommandKind.RetreatUnit, 3, 10); s.Step(400);
        Check(u.X == 3 && u.Y == 10, "Retreat failed alternate route");
    }
    private static void Gates()
    {
        var s = Empty(); var p = s.FindPlanet(1); var gate = Building(s, BuildingKind.Gate, 5, 5);
        Check(s.CanTraverse(p, 1, UnitKind.Tank, 5, 5) && !s.CanTraverse(p, 2, UnitKind.Tank, 5, 5), "Gate rights wrong");
        p.GatePermissions.Add(new GatePermission { GateId = gate.Id, FactionId = 2 });
        Check(!s.CanTraverse(p, 2, UnitKind.Tank, 5, 5), "War bypassed gate rights");
        s.State.Relations[0].Relation = RelationKind.Peace; Check(s.CanTraverse(p, 2, UnitKind.Tank, 5, 5), "Granted gate refused");
        gate.Destroyed = true; gate.Health = 0; gate.Deconstructing = true;
        Check(s.CanTraverse(p, 2, UnitKind.Tank, 5, 5), "Destroyed obstacle still blocks");
    }
    private static void Artillery()
    {
        var s = Empty(); var p = s.FindPlanet(1); var a = Unit(s, UnitKind.Artillery, 2, 3, 5); var wall = Building(s, BuildingKind.Wall, 8, 5);
        Command(s, a, CommandKind.AttackPosition, 8, 5); Check(Result(s) == "TargetNotKnown", "Blind artillery allowed");
        var scout = Unit(s, UnitKind.Infantry, 2, 7, 4); scout.Ammunition = 0; s.Step();
        Command(s, a, CommandKind.AttackPosition, 8, 5); scout.X = 1; scout.Y = 1; s.Step(400);
        Check(wall.Destroyed && !p.Buildings.Contains(wall), "Known-position artillery did not breach wall");
        Check(a.Ammunition < Definitions.Get(a.Kind).Ammo, "Artillery did not spend ammunition");
    }
    private static GameSimulation Supplied(out BuildingState depot)
    {
        var s = GameSimulation.CreateDefault(); var p = s.FindPlanet(1); s.State.MilitaryTestFixture = true;
        s.State.Relations[0].Relation = RelationKind.War;
        depot = Building(s, BuildingKind.MilitaryDepot, 7, 5); depot.Inventory[0] = 8; depot.Inventory[3] = 12;
        return s;
    }
    private static void Supply()
    {
        BuildingState depot; var s = Supplied(out depot); var p = s.FindPlanet(1);
        var tower = Building(s, BuildingKind.GunTower, 9, 5); tower.Ammunition = 3;
        var enemy = Unit(s, UnitKind.Tank, 2, 11, 5); enemy.Ammunition = 0;
        // Isolated firing window: existing local ammunition, no further raw inputs.
        p.Buildings[0].Inventory[3] = 0; depot.Inventory[3] = 0;
        s.Step(100); Check(tower.Ammunition == 0, "Tower did not spend reserve"); double health = enemy.Health; s.Step(200);
        Check(enemy.Health == health, "Tower fired without ammunition");
        LogisticsBarrier.Ring(p,tower.X,tower.Y);
        tower.Inventory[0] = 0; tower.Inventory[3] = 0; tower.Ammunition = 2; tower.EnergyStored = 2; s.Step();
        Check(tower.Operational, "Local reserve did not buffer interruption"); s.Step(50);
        Check(!tower.Operational && tower.EnergyStored < 0.05, "Disconnected tower ignored finite power reserve");
    }
    private static void Repair()
    {
        BuildingState depot; var s = Supplied(out depot); var p = s.FindPlanet(1);
        var engineer = Unit(s, UnitKind.Engineer, 1, 6, 5); var tank = Unit(s, UnitKind.Tank, 1, 4, 5); tank.Health = 80; tank.Ammunition = 0;
        SaveCodec.Validate(s.State);
        Command(s, engineer, CommandKind.ResupplyUnit, target: depot.Id); s.Step(80); Check(engineer.Parts > 0, "Engineer failed physical depot pickup");
        int before = engineer.Parts, consumed = p.ConsumedTotals[3]; Command(s, engineer, CommandKind.RepairTarget, target: tank.Id); s.Step(25);
        Check(tank.Health > 80 && engineer.Parts < before && p.ConsumedTotals[3] > consumed, "Repair free or ineffective");
        Command(s, engineer, CommandKind.ResupplyUnit, target: tank.Id); s.Step(25); Check(tank.Ammunition > 0, "Mobile supply failed");
        SaveCodec.Validate(s.State);
    }
    private static void Flak()
    {
        var s = Empty(); var a = Unit(s, UnitKind.AntiAir, 1, 3, 3); var b = Unit(s, UnitKind.Tank, 2, 4, 3); b.Ammunition = 0;
        s.Step(30); Check(b.Health == 180, "Flak hit ground"); b.Layer = TargetLayer.Space; s.Step(30); Check(b.Health == 180, "Flak hit space");
        b.Layer = TargetLayer.LandingTransition; s.Step(30); Check(b.Health < 180 && a.Ammunition < Definitions.Get(a.Kind).Ammo, "Flak missed transition");
    }
    private static void Destruction()
    {
        var s = GameSimulation.CreateDefault(); var p = s.FindPlanet(1); s.State.Relations[0].Relation = RelationKind.War;
        s.Enqueue(new SimCommand { Id = s.NextCommandId(), PlanetId = 1, Kind = CommandKind.PlaceBuilding, Building = BuildingKind.Warehouse, X = 14, Y = 5 }); s.Step(15);
        var target = p.Buildings.Find(b => b.Kind == BuildingKind.Warehouse); target.Health = 1;
        var enemy = Unit(s, UnitKind.Tank, 2, 14, 4); Command(s, enemy, CommandKind.AttackTarget, target: target.Id);
        Check(target.Destroyed, "Construction not damageable"); SaveCodec.Validate(s.State); s.Step(300); SaveCodec.Validate(s.State);
        Check(p.RecoveredTotals[2] == 0 && p.RecoveredTotals[3] == 0, "Destruction refunded construction");
    }
    private static void Persistence()
    {
        var a = Empty(); var u = Unit(a, UnitKind.Tank, 1, 3, 3); Unit(a, UnitKind.Infantry, 2, 13, 9);
        Command(a, u, CommandKind.MoveUnit, 10, 9); a.Step(27);
        var b = new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(a.State)));
        a.Step(180); b.Step(180); Check(SaveCodec.ToJson(a.State) == SaveCodec.ToJson(b.State), "Military continuation differs");
        BuildingState depot; a = Supplied(out depot); var barracks = Building(a, BuildingKind.Barracks, 9, 5);
        barracks.Inventory[0] = 2; barracks.Inventory[2] = 4; barracks.Inventory[3] = 1;
        a.Enqueue(new SimCommand { Id = a.NextCommandId(), PlanetId = 1, Kind = CommandKind.RecruitUnit, TargetId = barracks.Id }); a.Step(20);
        b = new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(a.State))); a.Step(180); b.Step(180);
        Check(SaveCodec.ToJson(a.State) == SaveCodec.ToJson(b.State), "Recruitment duplicated after load");
    }
    private static void Migration()
    {
        var s = GameSimulation.CreateDefault(); string json = SaveCodec.ToJson(s.State).Replace("\"Version\":" + SaveCodec.CurrentVersion, "\"Version\":2");
        var migrated = SaveCodec.FromJson(json); Check(migrated.Version == SaveCodec.CurrentVersion && migrated.Planets[0].Units.Count == 0 && migrated.Planets[0].Buildings[0].Health == 160, "V2 migration failed");
        var u = Unit(s, UnitKind.Tank, 1, 2, 2); u.Ammunition = 999;
        try { SaveCodec.Validate(s.State); throw new Exception("Invalid ammunition accepted"); } catch (InvalidDataException) { }
    }
    private static void Fixture()
    {
        var s = MilitaryFixture.Create(); s.State.Paused = false; var p = s.FindPlanet(1);
        var tank = p.Units.Find(u => u.OwnerId == 1 && u.Kind == UnitKind.Tank);
        Command(s, tank, CommandKind.MoveUnit, 8, 2); s.Step(240);
        Check(tank.X == 8 && tank.Y == 2, "Fixture path failed at " + tank.X + "," + tank.Y + ": " + tank.Status);
        MilitaryFixture.OrderAttack(s, true); MilitaryFixture.OrderAttack(s, false); s.Step(200); SaveCodec.Validate(s.State);
        Check(p.Units.Exists(u => u.Health < Definitions.Get(u.Kind).Health), "Fixture combat inactive");
        Command(s, tank, CommandKind.RetreatUnit, 5, 3); s.Step(120); SaveCodec.Validate(s.State);
        Check(tank.Destroyed || (tank.X == 5 && tank.Y == 3), "Fixture retreat failed");
    }
    private static void CancelRecruitment()
    {
        var s = GameSimulation.CreateDefault(); var p = s.FindPlanet(1); var b = Building(s, BuildingKind.Barracks, 7, 5);
        b.Inventory[0] = 2; b.Inventory[2] = 4; b.Inventory[3] = 1;
        s.Enqueue(new SimCommand { Id = s.NextCommandId(), PlanetId = 1, Kind = CommandKind.RecruitUnit, TargetId = b.Id }); s.Step();
        Check(b.RecruitPaid && p.Population == 15 && p.Units.Count == 0, "Recruitment did not reserve real people");
        s.Enqueue(new SimCommand { Id = s.NextCommandId(), PlanetId = 1, Kind = CommandKind.DemolishBuilding, TargetId = b.Id }); s.Step();
        Check(p.Population == 16 && p.Units.Count == 0 && b.Inventory[0] == 2 && b.Inventory[2] == 12 && b.Inventory[3] == 3,
            "Cancelled recruits or local recovery duplicated/lost");
        s.Step(500); SaveCodec.Validate(s.State); Check(p.Units.Count == 0, "Cancelled unit appeared later");
    }
}
