using System;
using System.IO;
using PlanetOne.Core;

internal static class InvasionTests
{
    internal static void Register(Action<string,Action> run)
    {
        run("M5 yard transfers actual port goods and produces one paid warship", Yard);
        run("M5 space weapons obey peace visibility ammunition and actual cargo loss", Combat);
        run("M5 escort protects transport and retreat does not erase damage", Escort);
        run("M5 ship repair consumes yard parts and time", Repair);
        run("M5 troops retain identity through partial unloading and carrier loss", Troops);
        run("M5 landing rejects peace occupied areas and active shields", Landing);
        run("M5 flak attacks transitions but not distant space", Flak);
        run("M5 capture requires infantry time supply and leaves defenders alive", Capture);
        run("M5 orbital fire has warning delay ammunition and shield counterplay", Bombard);
        run("M5 transport saves resume exactly and reject duplicate troop identities", Persistence);
        run("M5 version four migrates healthy ships without changing stock", Migration);
        run("M5 invasion fixture has finite identifiable transported troops", Fixture);
        run("M5 complete approach landing beachhead and occupation uses normal commands", FullInvasion);
        run("M5 evacuation requires nearby troops and retains their damage", Evacuation);
        run("M5 contested occupation resets and foreign buildings resist control", Contested);
        run("M5 destroyed headquarters remain repairable with finite pioneer parts", Ruins);
    }
    private static void Check(bool v,string message) { if(!v) throw new Exception(message); }
    private static bool Cmd(GameSimulation s,CommandKind k,long actor=0,long target=0,int planet=1,int value=0,int x=0,int y=0,int amount=0)
    { long id=s.NextCommandId();s.Enqueue(new SimCommand {Id=id,IssuerId=1,Kind=k,ActorId=actor,TargetId=target,PlanetId=planet,IntValue=value,X=x,Y=y,Amount=amount});s.Step();return s.State.CommandResults.Find(r=>r.CommandId==id).Success; }
    private static GameSimulation World()
    { var s=GameSimulation.CreateDefault();s.State.InvasionTestFixture=true;s.FindPlanet(2).OwnerId=2;s.State.Relations[0].Relation=RelationKind.War;s.State.Factions[0].Research.Completed.Add(Technology.Transport);foreach(var p in s.State.Planets){Array.Clear(p.Terrain,0,p.Terrain.Length);Array.Clear(p.OreRemaining,0,p.OreRemaining.Length);}return s; }
    private static void Yard()
    {
        var s=World();var p=s.FindPlanet(1);var port=InvasionFixture.AddBuilding(s,1,BuildingKind.Spaceport,9,5);
        port.Inventory=new[]{20,0,80,40,0,0};var yard=InvasionFixture.AddShip(s,1,1,ShipKind.OrbitalYard);
        Check(!Cmd(s,CommandKind.BuildShip,target:port.Id,value:(int)ShipKind.Corvette),"Port bypassed yard");
        Check(Cmd(s,CommandKind.BuildWarship,yard.Id,value:(int)ShipKind.Corvette),"Yard rejected");
        Check(s.State.Ships.Count==1 && !yard.ProductionPaid,"Instant free ship");
        s.Step(1800);Check(s.State.Ships.Count==2,"Paid ship not produced");
        Check(p.ConsumedTotals[2]>=20 && p.ConsumedTotals[3]>=8 && yard.ProductionKind==-1,"Cost or completion wrong");SaveCodec.Validate(s.State);
    }
    private static void Combat()
    {
        var s=World();var a=InvasionFixture.AddShip(s,1,1,ShipKind.Corvette);var b=InvasionFixture.AddShip(s,1,2,ShipKind.Freighter);b.Order=ShipOrder.Hold;b.Cargo[2]=20;
        s.State.Relations[0].Relation=RelationKind.Peace;Check(!Cmd(s,CommandKind.AttackShip,a.Id,b.Id),"Peace ignored");
        s.State.Relations[0].Relation=RelationKind.War;b.X=200;b.Y=200;Check(!Cmd(s,CommandKind.AttackShip,a.Id,b.Id),"Hidden target allowed");
        b.X=a.X;b.Y=a.Y;Check(Cmd(s,CommandKind.AttackShip,a.Id,b.Id),"Attack rejected");s.Step(250);
        Check(b.Health<100 && a.Ammunition<32,"No real damage/ammo");s.Step(200);Check(b.Destroyed && s.State.SpaceLostTotals[2]==20,"Cargo loss wrong");s.DestroyShip(b.Id);Check(s.State.SpaceLostTotals[2]==20,"Double loss");SaveCodec.Validate(s.State);
    }
    private static void Escort()
    {
        var s=World();var escort=InvasionFixture.AddShip(s,1,1,ShipKind.EscortFrigate);var cargo=InvasionFixture.AddShip(s,1,1,ShipKind.Freighter);var enemy=InvasionFixture.AddShip(s,1,2,ShipKind.Corvette);enemy.Order=ShipOrder.Hold;
        Cmd(s,CommandKind.EscortShip,escort.Id,cargo.Id);s.Step(40);Check(enemy.Health<140,"Escort did not respond");
        double health=escort.Health;Cmd(s,CommandKind.RetreatShip,escort.Id,planet:2);s.Step(5);Check(escort.Health<=health,"Retreat healed damage");
        enemy.X=200;enemy.Y=200;Check(!s.LocalOrbitThreat(1,1),"Distant ship blocks planet");
    }
    private static void Repair()
    {
        var s=World();var yard=InvasionFixture.AddShip(s,1,1,ShipKind.OrbitalYard);var ship=InvasionFixture.AddShip(s,1,1,ShipKind.Corvette);ship.Health=50;
        Cmd(s,CommandKind.RepairShip,ship.Id,yard.Id);s.Step(40);Check(ship.Health==50,"Free repair");yard.Cargo[3]=2;s.Step(50);Check(ship.Health==82 && yard.Cargo[3]==0,"Parts/time not consumed");
    }
    private static void Troops()
    {
        var s=World();var port=InvasionFixture.AddBuilding(s,1,BuildingKind.Spaceport,9,5);var carrier=InvasionFixture.AddShip(s,1,1,ShipKind.LandingTransport);
        s.Step();var ids=new long[4];for(int i=0;i<4;i++){var u=InvasionFixture.AddUnit(s,1,1,UnitKind.Infantry,9,4);u.Health=40;u.Ammunition=3;ids[i]=u.Id;Check(Cmd(s,CommandKind.LoadTroop,carrier.Id,u.Id),"Load failed");}
        Check(s.FindPlanet(1).Units.Count==0 && carrier.Troops.Count==4,"Troops exist twice");
        var pos=s.GetOrbitPosition(2);carrier.X=pos.X;carrier.Y=pos.Y;carrier.Order=ShipOrder.Hold;
        Check(Cmd(s,CommandKind.LandTransport,carrier.Id,planet:2,x:3,y:9),"Land rejected");s.Step(165);Check(carrier.TransportPhase==TransportPhase.Landed,"Did not land");
        for(int i=0;i<2;i++) Check(Cmd(s,CommandKind.UnloadTroop,carrier.Id,ids[i],planet:2),"Unload failed");
        s.DestroyShip(carrier.Id);Check(s.FindPlanet(2).Units.Count==2 && carrier.Troops.TrueForAll(u=>u.Destroyed),"Loss duplicated/deleted landed troops");
        Check(s.FindPlanet(2).Units[0].Health==40 && s.FindPlanet(2).Units[0].Ammunition==3,"Transfer reset state");SaveCodec.Validate(s.State);
        Check(port!=null,"Port missing");
    }
    private static void Landing()
    {
        var s=World();var ship=InvasionFixture.AddShip(s,2,1,ShipKind.LandingTransport);
        s.State.Relations[0].Relation=RelationKind.Peace;Check(!Cmd(s,CommandKind.LandTransport,ship.Id,planet:2,x:3,y:9),"Peace landing");
        s.State.Relations[0].Relation=RelationKind.War;Check(!Cmd(s,CommandKind.LandTransport,ship.Id,planet:2,x:8,y:6),"Building landing");
        var shield=InvasionFixture.AddBuilding(s,2,BuildingKind.ShieldGenerator,3,7);shield.Operational=true;
        Check(s.LandingError(ship,s.FindPlanet(2),3,9)!=null,"Shield bypassed");shield.Operational=false;Check(s.LandingError(ship,s.FindPlanet(2),3,9)==null,"Inactive shield blocks");
    }
    private static void Flak()
    {
        var s=World();var ship=InvasionFixture.AddShip(s,2,1,ShipKind.LandingTransport);var aa=InvasionFixture.AddUnit(s,2,2,UnitKind.AntiAir,4,7);
        s.Step(60);Check(ship.Health==180 && aa.Ammunition==16,"Ground flak hit space");
        Cmd(s,CommandKind.LandTransport,ship.Id,planet:2,x:3,y:9);s.Step(165);Check(ship.Health<180 && aa.Ammunition<16,"No transition damage");
    }
    private static void Capture()
    {
        var s=World();var p=s.FindPlanet(2);var hq=p.Buildings[0];var infantry=InvasionFixture.AddUnit(s,2,1,UnitKind.Infantry,hq.X-1,hq.Y);infantry.Ammunition=0;
        var survivor=InvasionFixture.AddUnit(s,2,2,UnitKind.Infantry,17,13);survivor.Ammunition=0;
        Check(Cmd(s,CommandKind.CaptureBuilding,infantry.Id,hq.Id,2),"Capture rejected");s.Step(180);Check(p.OwnerId==2,"Captured without supply");
        s.State.Beachheads.Add(new BeachheadState {Id=s.State.NextEntityId++,PlanetId=2,OwnerId=1,X=5,Y=6,Progress=Definitions.BeachheadSeconds,Food=8});
        var stock=(int[])hq.Inventory.Clone();s.Step(2);Check(p.OwnerId==1 && !survivor.Destroyed && survivor.OwnerId==2,"Capture deleted resistance");Check(hq.Inventory[2]==stock[2],"Stock changed on capture");SaveCodec.Validate(s.State);
    }
    private static void Bombard()
    {
        var s=World();var p=s.FindPlanet(2);var hq=p.Buildings[0];var ship=InvasionFixture.AddShip(s,2,1,ShipKind.TorpedoShip);var u=InvasionFixture.AddUnit(s,2,1,UnitKind.Infantry,hq.X-1,hq.Y);u.Ammunition=0;
        Check(Cmd(s,CommandKind.OrbitalBombard,ship.Id,planet:2,x:hq.X,y:hq.Y),"Bombard rejected");Check(hq.Health==160,"No warning window");s.Step(125);Check(hq.Health==90 && ship.Ammunition==14 && p.OwnerId==2,"Bombard wrong");
    }
    private static void Persistence()
    {
        var s=World();var ship=InvasionFixture.AddShip(s,2,1,ShipKind.LandingTransport);Cmd(s,CommandKind.LandTransport,ship.Id,planet:2,x:3,y:9);s.Step(30);
        var copy=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(s.State)));s.Step(150);copy.Step(150);Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(copy.State),"Descent continuation diverges");
        var u=InvasionFixture.AddUnit(s,1,1,UnitKind.Infantry,2,2);ship.Troops.Add(u);
        bool rejected=false;try{SaveCodec.Validate(s.State);}catch(InvalidDataException){rejected=true;}Check(rejected,"Duplicate ID accepted");
    }
    private static void Migration()
    {
        var s=World();var ship=InvasionFixture.AddShip(s,1,1,ShipKind.Freighter);ship.Cargo[2]=12;
        string json=SaveCodec.ToJson(s.State).Replace("\"Version\":"+SaveCodec.CurrentVersion,"\"Version\":4");var restored=SaveCodec.FromJson(json);
        Check(restored.Version==SaveCodec.CurrentVersion && restored.Ships[0].Health==100 && restored.Ships[0].Cargo[2]==12,"V4 migration changed cargo");
    }
    private static void Fixture()
    {var s=InvasionFixture.Create();Check(s.State.InvasionTestFixture && s.State.Ships.Find(v=>v.Kind==ShipKind.LandingTransport).Troops.Count==4,"Fixture transport missing");SaveCodec.Validate(s.State);}
    private static void FullInvasion()
    {
        var s=InvasionFixture.Create();s.State.Paused=false;var carrier=s.State.Ships.Find(v=>v.Kind==ShipKind.LandingTransport);
        foreach(var ship in s.State.Ships) if(ship.OwnerId==1 && ship.Kind!=ShipKind.OrbitalYard) Check(Cmd(s,CommandKind.FlyShip,ship.Id,planet:2),"Approach rejected: "+s.State.LastMessage);
        s.Step(500);Check(s.State.Ships.Exists(v=>v.OwnerId==2 && v.Destroyed),"Corvette survives");
        Check(!carrier.Destroyed && Cmd(s,CommandKind.LandTransport,carrier.Id,planet:2,x:4,y:9),"Landing failed");s.Step(170);
        var ids=carrier.Troops.ConvertAll(u=>u.Id);foreach(var id in ids) Check(Cmd(s,CommandKind.UnloadTroop,carrier.Id,id,2),"Unload failed");
        var p=s.FindPlanet(2);var e=p.Units.Find(u=>u.OwnerId==1 && u.Kind==UnitKind.Engineer);
        Check(Cmd(s,CommandKind.EstablishBeachhead,e.Id,planet:2,x:(int)e.X,y:(int)e.Y),"Beachhead failed");s.Step(140);
        var infantry=p.Units.Find(u=>u.OwnerId==1 && u.Kind==UnitKind.Infantry);Check(Cmd(s,CommandKind.MoveUnit,infantry.Id,planet:2,x:7,y:6),"Move rejected");s.Step(160);
        Check(Cmd(s,CommandKind.CaptureBuilding,infantry.Id,p.Buildings[0].Id,2),"Capture rejected at "+infantry.X+"/"+infantry.Y);s.Step(180);
        Check(p.OwnerId==1,"Occupation failed: "+p.Buildings[0].Status);SaveCodec.Validate(s.State);
    }
    private static void Evacuation()
    {
        var s=World();var carrier=InvasionFixture.AddShip(s,2,1,ShipKind.LandingTransport);
        Check(Cmd(s,CommandKind.LandTransport,carrier.Id,planet:2,x:3,y:9),"Evacuation carrier landing rejected");s.Step(165);
        Check(carrier.TransportPhase==TransportPhase.Landed,"Evacuation carrier never landed");
        var u=InvasionFixture.AddUnit(s,2,1,UnitKind.Infantry,1,1);u.Health=31;
        Check(!Cmd(s,CommandKind.LoadTroop,carrier.Id,u.Id,2),"Remote evacuation allowed");
        // The landed carrier occupies its center; boarding happens from a free adjacent hex.
        Check(Cmd(s,CommandKind.MoveUnit,u.Id,planet:2,x:3,y:8),"Evacuation move rejected");s.Step(150);
        Check(HexGrid.Distance(u.X,u.Y,carrier.LandingX,carrier.LandingY)<=1.5 && HexGrid.Distance(u.X,u.Y,carrier.LandingX,carrier.LandingY)>.5,"Evacuation approach must remain beside the physical carrier");
        Check(Cmd(s,CommandKind.LoadTroop,carrier.Id,u.Id,2),"Nearby evacuation failed");
        Cmd(s,CommandKind.LaunchTransport,carrier.Id,planet:2);s.Step(165);Check(carrier.TransportPhase==TransportPhase.Space && carrier.Troops[0].Health==31 && !s.FindPlanet(2).Units.Contains(u),"Evacuation lost identity/damage");SaveCodec.Validate(s.State);
    }
    private static void Contested()
    {
        var s=World();var p=s.FindPlanet(2);var b=InvasionFixture.AddBuilding(s,2,BuildingKind.Warehouse,5,5);b.Inventory[2]=17;
        var u=InvasionFixture.AddUnit(s,2,1,UnitKind.Infantry,4,5);u.Ammunition=0;
        Check(Cmd(s,CommandKind.CaptureBuilding,u.Id,b.Id,2),"Capture rejected");s.Step(50);Check(b.CaptureProgress>0,"No progress");
        var defender=InvasionFixture.AddUnit(s,2,2,UnitKind.Infantry,5,4);defender.Ammunition=0;s.Step();Check(b.CaptureProgress==0,"Contested progress not reset");
        defender.X=17;defender.Y=13;s.Step(165);Check(b.OwnerId==1 && b.Inventory[2]==17,"Capture changed actual stock");
        b.OwnerId=2;p.OwnerId=1;Check(!Cmd(s,CommandKind.SetBuildingEnabled,target:b.Id,planet:2),"Strategic owner controls foreign building");
    }
    private static void Ruins()
    {
        var s=World();var p=s.FindPlanet(2);var hq=p.Buildings[0];hq.Destroyed=true;hq.Health=0;hq.Deconstructing=true;hq.Enabled=false;Array.Clear(hq.Inventory,0,6);
        s.Step(5);Check(p.Buildings.Contains(hq),"Control ruin removed permanently");var e=InvasionFixture.AddUnit(s,2,1,UnitKind.Engineer,7,6);
        Check(!Cmd(s,CommandKind.RestoreHeadquarters,e.Id,hq.Id,2),"Free reconstruction");e.Parts=4;
        Check(Cmd(s,CommandKind.RestoreHeadquarters,e.Id,hq.Id,2) && e.Parts==0 && hq.Destroyed,"Reconstruction skipped time/cost");
        var copy=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(s.State)));s.Step(125);copy.Step(125);
        Check(hq.Health==64 && !hq.Destroyed && SaveCodec.ToJson(s.State)==SaveCodec.ToJson(copy.State),"Reconstruction failed or save diverged");SaveCodec.Validate(s.State);
    }
}
