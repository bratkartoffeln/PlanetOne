using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using PlanetOne.Core;

internal static class AiTests
{
    internal static void Register(Action<string,Action> run)
    {
        run("M6 own economy produces real goods and researches from equal starting stocks", Economy);
        run("M6 hidden enemy construction never enters observation or decisions", Knowledge);
        run("M6 broken road triggers reconnection rather than duplicate production", BrokenRoad);
        run("M6 missing transport capacity prevents an invasion start", Capacity);
        run("M6 AI snapshots preserve knowledge decisions and exact continuation", Persistence);
        run("M6 observed defenses change landing plans while stale contacts retain their last state", Adaptation);
        run("M6 damaged fleets retreat physically and release exclusive assignments", Retreat);
        run("M6 courier congestion reduces optional consumption without inventing vehicles", Congestion);
        run("M6 paid expansion and a real prepared invasion advance through ordinary commands", LongScenario);
        run("M6 peaceful colonization pays its full manifest without military conquest", Colonization);
        run("M6 multiple seeds preserve finite economies and explain planning choices", Seeds);
        run("M6 pause freezes AI and frame grouping never changes its commands", Timing);
        run("M6 operation snapshots reject duplicate and foreign assignments", InvalidReservations);
        run("M6 loss of war rights aborts the existing operation", Peace);
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Json<T>(T value)
    {using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(T)).WriteObject(stream,value);return Encoding.UTF8.GetString(stream.ToArray());}}
    private static void Economy()
    {
        var s=GameSimulation.CreateAiScenario(7);var p=s.FindPlanet(2);
        Check(p.Buildings.Count==1&&s.State.Ships.Count==0&&p.Units.Count==0&&p.Buildings[0].Inventory[2]==100,"Unequal start");
        s.Step(24000);SaveCodec.Validate(s.State);
        Console.WriteLine("  M6 20min: population="+p.Population+", metal="+p.ProducedTotals[2]+", parts="+p.ProducedTotals[3]+", buildings="+p.Buildings.Count+", tech="+s.State.Factions[1].Research.Completed.Count+", ships="+s.State.Ships.Count);
        Check(p.ProducedTotals[0]>60&&p.ProducedTotals[2]>30&&p.ProducedTotals[3]>10,"AI economy never replaces starting goods");
        Check(s.State.Factions[1].Research.Completed.Contains(Technology.Transport),"AI does not research");
        Check(p.Population>=16,"AI starves");
    }
    private static void Knowledge()
    {
        var a=GameSimulation.CreateAiScenario(17);var b=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(a.State)));
        InvasionFixture.AddBuilding(b,1,BuildingKind.GunTower,15,3);
        var av=a.ObserveForAi(2);var bv=b.ObserveForAi(2);
        Check(Json(av)==Json(bv),"Hidden construction leaks into observation");
        var ap=AiPlanner.Plan(av,a.State.ComputerFactions[0]);var bp=AiPlanner.Plan(bv,b.State.ComputerFactions[0]);
        Check(Json(ap.Command)==Json(bp.Command)&&ap.Goal==bp.Goal&&ap.Reason==bp.Reason&&ap.Alternative==bp.Alternative,"Secret changes decision");
        av.Worlds[0].Buildings[0].Inventory[2]=9999;Check(a.FindPlanet(2).Buildings[0].Inventory[2]==100,"Observation aliases simulation");
    }
    private static void BrokenRoad()
    {
        var s=GameSimulation.CreateAiScenario();var p=s.FindPlanet(2);Array.Clear(p.Roads,0,p.Roads.Length);
        InvasionFixture.AddBuilding(s,2,BuildingKind.Warehouse,11,6);
        var action=AiPlanner.Plan(s.ObserveForAi(2),s.State.ComputerFactions[0]);
        Check(action.Command!=null&&action.Command.Kind==CommandKind.PlaceRoad&&action.Reason.Contains("Wabenkante"),"Wrong bottleneck reaction");
        s.Step(200);Check(p.Roads[6*p.Width+9]||p.Roads[5*p.Width+9],"Road not repaired");
    }
    private static void Capacity()
    {
        var s=GameSimulation.CreateAiScenario();var p=s.FindPlanet(2);
        InvasionFixture.AddBuilding(s,2,BuildingKind.Spaceport,9,5);
        // Legitimate sensor at the target; no carrier or free transport slots.
        InvasionFixture.AddShip(s,1,2,ShipKind.Scout);s.Step(41);
        var ai=s.State.ComputerFactions[0];
        Check(ai.Operation.Phase!=OperationPhase.Executing&&s.State.Ships.TrueForAll(t=>t.TransportPhase==TransportPhase.Space),"Started without transport");
    }
    private static void Persistence()
    {
        var a=GameSimulation.CreateAiScenario(99);a.Step(800);var b=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(a.State)));
        a.Step(400);b.Step(400);Check(SaveCodec.ToJson(a.State)==SaveCodec.ToJson(b.State),"AI replay diverged");
        b.State.ComputerFactions[0].Operation.Ships.Add(999999);bool rejected=false;
        try{SaveCodec.Validate(b.State);}catch(InvalidDataException){rejected=true;}Check(rejected,"Unknown assigned ship accepted");
    }
    private static GameSimulation MilitaryWorld()
    {
        var s=InvasionFixture.Create();s.State.Paused=false;s.State.Factions[0].IsPlayer=false;s.State.Factions[1].IsPlayer=true;s.State.PlayerFactionId=2;
        s.State.ComputerFactions.Add(new AiState {FactionId=1});return s;
    }
    private static void Adaptation()
    {
        var s=MilitaryWorld();var ai=s.State.ComputerFactions[0];
        var scout=InvasionFixture.AddUnit(s,2,1,UnitKind.Infantry,3,9);scout.Ammunition=0;
        var tower=InvasionFixture.AddBuilding(s,2,BuildingKind.GunTower,3,7);
        var view=s.ObserveForAi(1);AiPlanner.Plan(view,ai);
        Check(ai.Contacts.Exists(c=>c.Id==tower.Id),"Visible tower not remembered");
        long tick=ai.Contacts.Find(c=>c.Id==tower.Id).Tick;int x=ai.Contacts.Find(c=>c.Id==tower.Id).X;
        s.FindPlanet(2).Units.Remove(scout);tower.X=15;tower.Y=12;s.State.Tick=40;
        AiPlanner.Plan(s.ObserveForAi(1),ai);
        Check(ai.Contacts.Find(c=>c.Id==tower.Id).Tick==tick&&ai.Contacts.Find(c=>c.Id==tower.Id).X==x,"Hidden movement refreshed contact");
        var target=view.Worlds.Find(w=>w.Id==2);var carrier=view.Ships.Find(t=>t.Kind==ShipKind.LandingTransport);var escort=view.Ships.Find(t=>t.Kind==ShipKind.EscortFrigate);
        ai.Operation=new AiOperation {HomePlanetId=1,TargetPlanetId=2,Phase=OperationPhase.Executing,LandingChosen=true,LandingX=3,LandingY=9};
        ai.Operation.Ships.Add(carrier.Id);ai.Operation.Ships.Add(escort.Id);carrier.X=target.X;carrier.Y=target.Y;view.Tick=40;target.Current=true;
        var plan=AiPlanner.Plan(view,ai);
        Check(ai.Operation.Phase==OperationPhase.Aborting||ai.Operation.LandingX!=3||ai.Operation.LandingY!=9,"Known tower did not alter plan");
        Check(plan.Command==null||plan.Command.Kind!=CommandKind.LandTransport||plan.Command.X!=3||plan.Command.Y!=9,"Repeated doomed landing");
    }
    private static void Retreat()
    {
        var s=MilitaryWorld();var ai=s.State.ComputerFactions[0];var carrier=s.State.Ships.Find(t=>t.Kind==ShipKind.LandingTransport);var escort=s.State.Ships.Find(t=>t.Kind==ShipKind.EscortFrigate);
        var target=s.GetOrbitPosition(2);carrier.X=target.X;carrier.Y=target.Y;carrier.TargetPlanetId=2;carrier.Health=40;escort.X=target.X;escort.Y=target.Y;escort.TargetPlanetId=2;
        ai.Operation=new AiOperation {HomePlanetId=1,TargetPlanetId=2,Phase=OperationPhase.Executing};ai.Operation.Ships.Add(carrier.Id);ai.Operation.Ships.Add(escort.Id);
        s.State.Ships.RemoveAll(t=>t.OwnerId==2);s.Step(41);
        Check(ai.Operation.Phase==OperationPhase.Aborting,"Damaged carrier did not abort");
        Check(carrier.X!=s.GetOrbitPosition(1).X,"Retreat teleported");
        for(int i=0;i<1000&&ai.Operation.Phase!=OperationPhase.Cancelled;i++)s.Step();
        Check(ai.Operation.Phase==OperationPhase.Cancelled&&ai.Operation.Ships.Count==0&&ai.Operation.Units.Count==0,"Assignments not released on real return");SaveCodec.Validate(s.State);
        var lost=MilitaryWorld();var lostAi=lost.State.ComputerFactions[0];var doomed=lost.State.Ships.Find(t=>t.Kind==ShipKind.LandingTransport);var remaining=lost.State.Ships.Find(t=>t.Kind==ShipKind.EscortFrigate);
        lost.DestroyShip(doomed.Id);
        lostAi.Operation=new AiOperation {HomePlanetId=1,TargetPlanetId=2,Phase=OperationPhase.Preparing};lostAi.Operation.Ships.Add(doomed.Id);lostAi.Operation.Ships.Add(remaining.Id);
        lost.Step(41);Check(lostAi.Operation.Phase!=OperationPhase.Preparing,"Lost reserved carrier permanently blocks replacement");SaveCodec.Validate(lost.State);
    }
    private static void Congestion()
    {
        var s=GameSimulation.CreateAiScenario();var p=s.FindPlanet(2);var factory=InvasionFixture.AddBuilding(s,2,BuildingKind.ElectronicsFactory,9,5);
        var v=s.ObserveForAi(2);var w=v.Worlds.Find(a=>a.Id==2);w.Shipments=4;w.PendingDeliveries=16;
        var plan=AiPlanner.Plan(v,s.State.ComputerFactions[0]);
        Check(plan.Command!=null&&plan.Command.Kind==CommandKind.SetBuildingEnabled&&!plan.Command.BoolValue&&plan.Command.TargetId==factory.Id,"Congestion increased production");
    }
    private static void LongScenario()
    {
        var s=GameSimulation.CreateAiScenario(7);s.Step(48000);SaveCodec.Validate(s.State);var ai=s.State.ComputerFactions[0];
        Console.WriteLine("  M6 40min: ships="+s.State.Ships.Count+", operation="+ai.Operation.Phase+", reason="+ai.Operation.Reason+", colonies="+s.State.Planets.FindAll(p=>p.OwnerId==2).Count);
        Check(s.State.Ships.Exists(t=>t.OwnerId==2&&t.Kind==ShipKind.OrbitalYard),"No paid yard");
        Check(s.State.Ships.Exists(t=>t.OwnerId==2&&t.Kind==ShipKind.Scout),"No paid exploration");
        var battle=MilitaryWorld();battle.State.Ships.RemoveAll(t=>t.OwnerId==2);battle.Step(2400);SaveCodec.Validate(battle.State);
        Console.WriteLine("  M6 prepared operation="+battle.State.ComputerFactions[0].Operation.Phase+" "+battle.State.ComputerFactions[0].Operation.Reason);
        Check(battle.FindPlanet(2).OwnerId==1&&battle.State.ComputerFactions[0].Operation.Phase==OperationPhase.Completed,"Prepared invasion never completes real occupation");
    }
    private static void Colonization()
    {
        var s=GameSimulation.CreateAiScenario(23);s.State.Relations[0].Relation=RelationKind.Peace;s.Step(48000);SaveCodec.Validate(s.State);
        Console.WriteLine("  M6 peaceful 40min: moon="+s.FindPlanet(3).OwnerId+", ships="+s.State.Ships.Count+", tech="+s.State.Factions[1].Research.Completed.Count);
        Check(s.FindPlanet(3).OwnerId==2&&s.State.Ships.Exists(t=>t.Kind==ShipKind.ColonyShip&&t.Consumed),"Paid colonization never completed");
    }
    private static void Seeds()
    {
        foreach(int seed in new[]{41,113})
        {
            var s=GameSimulation.CreateAiScenario(seed);s.Step(24000);SaveCodec.Validate(s.State);var p=s.FindPlanet(2);
            Check(p.Population>=16&&p.ProducedTotals[0]>60&&p.ProducedTotals[2]>30&&p.ProducedTotals[3]>10,"Seed economy failed: "+seed);
            Check(s.State.ComputerFactions[0].Decisions.TrueForAll(d=>d.Reason.Length>0&&d.Alternative.Length>0),"Missing decision evidence");
            Console.WriteLine("  M6 seed "+seed+": population="+p.Population+", metal="+p.ProducedTotals[2]+", parts="+p.ProducedTotals[3]);
        }
    }
    private static void Timing()
    {
        var a=GameSimulation.CreateAiScenario(4);var b=GameSimulation.CreateAiScenario(4);
        a.State.Paused=true;string before=SaveCodec.ToJson(a.State);a.Step(200);a.Advance(20);Check(before==SaveCodec.ToJson(a.State),"Paused AI moved");
        a.State.Paused=false;for(int i=0;i<800;i++)a.Step();for(int i=0;i<20;i++)b.Step(40);
        Check(SaveCodec.ToJson(a.State)==SaveCodec.ToJson(b.State),"Grouping changes AI decisions");
    }
    private static void InvalidReservations()
    {
        var s=MilitaryWorld();var op=s.State.ComputerFactions[0].Operation;var ship=s.State.Ships.Find(t=>t.OwnerId==1);
        op.Ships.Add(ship.Id);op.Ships.Add(ship.Id);bool duplicate=false;
        try{SaveCodec.Validate(s.State);}catch(InvalidDataException){duplicate=true;}Check(duplicate,"Duplicate reservation accepted");
        op.Ships.Clear();op.Ships.Add(s.State.Ships.Find(t=>t.OwnerId==2).Id);bool foreign=false;
        try{SaveCodec.Validate(s.State);}catch(InvalidDataException){foreign=true;}Check(foreign,"Enemy ship reserved");
    }
    private static void Peace()
    {
        var s=MilitaryWorld();var ai=s.State.ComputerFactions[0];var ship=s.State.Ships.Find(t=>t.Kind==ShipKind.LandingTransport);
        ai.Operation=new AiOperation {HomePlanetId=1,TargetPlanetId=2,Phase=OperationPhase.Executing};ai.Operation.Ships.Add(ship.Id);
        s.State.Relations[0].Relation=RelationKind.Peace;var view=s.ObserveForAi(1);view.Tick=40;
        AiPlanner.Plan(view,ai);
        Check(ai.Operation.Phase==OperationPhase.Aborting||ai.Operation.Phase==OperationPhase.Cancelled,"Peace retained attack operation");
    }
}
