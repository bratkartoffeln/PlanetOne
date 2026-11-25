using System;
using System.Collections.Generic;
using PlanetOne.Core;

internal static class TransitionTests
{
    public static void Register(Action<string,Action> run)
    {
        run("M8 disk restore preserves loaded troops through descent and real occupation",Invasion);
        run("M8 disk restore expires treaty rights at the exact deadline without restarting war",Termination);
        run("M8 construction reserves only stores with an actual route to the site",DisconnectedStore);
        run("M8 finished scenarios reject misleading new gate activations",FinishedGate);
        run("M8 opt-in component timings do not change simulation results",Profiling);
    }
    private static void Invasion()
    {
        // This test isolates integration transitions; it is explicitly not the paid F05 proof.
        var s=InvasionFixture.Create();s.State.Paused=false;
        var carrier=s.State.Ships.Find(v=>v.Kind==ShipKind.LandingTransport);
        var troopIds=carrier.Troops.ConvertAll(u=>u.Id);
        foreach(var ship in s.State.Ships)if(ship.OwnerId==1&&ship.Kind!=ShipKind.OrbitalYard)
            ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.FlyShip,ActorId=ship.Id,PlanetId=2});
        s.Step(500);
        ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.LandTransport,ActorId=carrier.Id,PlanetId=2,X=4,Y=9});s.Step(30);
        ReferenceTests.Check(carrier.TransportPhase==TransportPhase.Descending,"Missing partial descent");
        ReferenceTests.ContinueExactly(s,"troop-descent",170);
        foreach(long id in troopIds)ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.UnloadTroop,ActorId=carrier.Id,TargetId=id,PlanetId=2});
        var p=s.FindPlanet(2);var engineer=p.Units.Find(u=>u.OwnerId==1&&u.Kind==UnitKind.Engineer);
        ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.EstablishBeachhead,ActorId=engineer.Id,PlanetId=2,X=(int)engineer.X,Y=(int)engineer.Y});s.Step(140);
        var infantry=p.Units.Find(u=>u.OwnerId==1&&u.Kind==UnitKind.Infantry);
        ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.MoveUnit,ActorId=infantry.Id,PlanetId=2,X=7,Y=6});s.Step(160);
        var hq=p.Buildings[0];ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.CaptureBuilding,ActorId=infantry.Id,TargetId=hq.Id,PlanetId=2});s.Step(40);
        ReferenceTests.Check(hq.CaptureProgress>0&&p.OwnerId==2,"No partial occupation");
        ReferenceTests.ContinueExactly(s,"occupation",180);
        ReferenceTests.Check(p.OwnerId==1&&s.AtWar(1,2),"Ownership or war changed incorrectly");
        foreach(long id in troopIds)ReferenceTests.Check(p.Units.FindAll(u=>u.Id==id).Count==1&&!carrier.Troops.Exists(u=>u.Id==id),"Troop duplicated or lost during save/load");
    }
    private static void Termination()
    {
        var s=GameSimulation.CreateReferenceScenario();
        ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms {Kind=TreatyKind.Access,Members=new List<int>{1,2},PlanetId=2,Rights=AccessRight.CivilTransit|AccessRight.Landing,DurationTicks=4000,NoticeTicks=200}});
        var treaty=s.State.Treaties[s.State.Treaties.Count-1];
        ReferenceTests.Wait(s,()=>treaty.Phase==TreatyPhase.Active,"AI accepts access agreement",800);
        ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.TerminateTreaty,TargetId=treaty.Id});
        ReferenceTests.Check(s.HasAccess(1,2,2,AccessRight.Landing),"Notice period lost existing rights");
        ReferenceTests.ContinueExactly(s,"treaty-notice",(int)(treaty.EndTick-s.State.Tick)-1);
        ReferenceTests.Check(GameSimulation.TreatyLive(treaty),"Treaty expired early");s.Step(2);
        ReferenceTests.Check(!s.HasAccess(1,2,2,AccessRight.Landing)&&!s.AtWar(1,2),"Expiry retained rights or silently declared war");
    }
    private static void DisconnectedStore()
    {
        var s=GameSimulation.CreateDefault();s.State.SpaceTestFixture=true;
        var p=s.FindPlanet(1);p.Buildings[0].Inventory[3]=0;
        var store=InvasionFixture.AddBuilding(s,1,BuildingKind.Warehouse,14,5);store.Inventory[3]=5;
        string reason=s.ValidatePlacement(1,BuildingKind.SolarPlant,5,5);
        ReferenceTests.Check(reason!=null&&reason.Contains("erreichbar"),"Disconnected stock counted as buildable material");
        long id=s.NextCommandId();s.Enqueue(new SimCommand {Id=id,Kind=CommandKind.PlaceBuilding,PlanetId=1,Building=BuildingKind.SolarPlant,X=5,Y=5});s.Step();
        ReferenceTests.Check(!s.State.CommandResults.Find(r=>r.CommandId==id).Success&&store.ReservedOutgoing[3]==0&&!p.Buildings.Exists(b=>b.X==5&&b.Y==5),"Unreachable parts were reserved anyway");
        ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.PlaceRoad,PlanetId=1,X=7,Y=5});
        ReferenceTests.Build(s,1,BuildingKind.SolarPlant,5,5);
        ReferenceTests.Check(store.Inventory[3]==3&&store.ReservedOutgoing[3]==0,"Reconnected actual stock was not delivered exactly once");
    }
    private static void FinishedGate()
    {
        var s=GameSimulation.CreateDiplomacyFixture();s.State.Paused=false;s.State.Winners.Add(1);
        var gate=s.FindPlanet(1).Buildings.Find(b=>b.Kind==BuildingKind.JumpGate);long id=s.NextCommandId();
        s.Enqueue(new SimCommand {Id=id,Kind=CommandKind.ActivateGate,TargetId=gate.Id});s.Step();
        ReferenceTests.Check(!s.State.CommandResults.Find(r=>r.CommandId==id).Success&&!s.State.GateProjects.Exists(g=>g.Activating),"Gate claims to activate after outcome is already final");
    }
    private static void Profiling()
    {
        var ordinary=GameSimulation.CreateReferenceScenario();var measured=GameSimulation.CreateReferenceScenario();measured.Metrics=new SimulationMetrics();
        ordinary.Step(400);measured.Step(400);
        ReferenceTests.Check(SaveCodec.ToJson(ordinary.State)==SaveCodec.ToJson(measured.State)&&measured.Metrics.TickMilliseconds.Count==400,"Timing instrumentation changed state or skipped ticks");
    }
}
