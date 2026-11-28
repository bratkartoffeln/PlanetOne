using System;
using PlanetOne.Core;

internal static class SupplyNetworkTests
{
    internal static void Register(Action<string,Action> run)
    {
        run("Free logistics builds a roadless site through real paid deliveries",Build);
        run("Terrain barriers reject construction without reserving unreachable goods",Reject);
        run("Actual transport wear saves exactly and fades without changing goods",Persistence);
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static GameSimulation Fixture()
    {
        var s=GameSimulation.CreateDefault();var p=s.FindPlanet(1);
        Array.Clear(p.Roads,0,p.Roads.Length);Array.Clear(p.Terrain,0,p.Terrain.Length);return s;
    }
    private static BuildingState BuildSite(GameSimulation s)
    {
        s.Enqueue(new SimCommand{Id=s.NextCommandId(),PlanetId=1,Kind=CommandKind.PlaceBuilding,Building=BuildingKind.SolarPlant,X=14,Y=10});s.Step();
        var site=s.FindPlanet(1).Buildings.Find(b=>b.X==14&&b.Y==10);Check(site!=null,"Roadless paid site rejected");return site;
    }
    private static void Build()
    {
        var s=Fixture();var p=s.FindPlanet(1);p.Terrain[7*p.Width+10]=1;
        Check(s.ValidatePlacement(1,BuildingKind.SolarPlant,14,10)==null,"Reachable roadless site is not buildable");
        var site=BuildSite(s);bool physical=false;
        for(int i=0;i<4000&&!site.Built;i++){s.Step();physical|=p.Shipments.Exists(c=>c.Amount>0&&c.Phase==ShipmentPhase.InTransit);}
        Check(site.Built&&physical,"Real materials and builder did not reach the site");
        Check(Array.TrueForAll(p.Roads,r=>!r)&&p.ConsumedTotals[2]==8&&p.ConsumedTotals[3]==2,"Logistics painted roads or changed construction costs");
        Check(p.Trails.Count>0,"Real traffic left no usage trace");SaveCodec.Validate(s.State);
    }
    private static void Reject()
    {
        var s=Fixture();var p=s.FindPlanet(1);LogisticsBarrier.Column(p,12);
        Check(s.ValidatePlacement(1,BuildingKind.SolarPlant,14,10)!=null,"Barrier was ignored");
        s.Enqueue(new SimCommand{Id=s.NextCommandId(),PlanetId=1,Kind=CommandKind.PlaceBuilding,Building=BuildingKind.SolarPlant,X=14,Y=10});s.Step();
        Check(p.Buildings.Count==1&&p.Shipments.Count==0&&p.Deliveries.Count==0&&p.Trails.Count==0,"Rejected construction created traffic or traces");
        Check(p.Buildings[0].Inventory[2]==100&&p.Buildings[0].Inventory[3]==40&&Array.TrueForAll(p.Buildings[0].ReservedOutgoing,x=>x==0),"Unreachable construction consumed or reserved stock");
    }
    private static void Persistence()
    {
        var s=Fixture();var p=s.FindPlanet(1);BuildSite(s);
        for(int i=0;i<1000&&p.Trails.Count==0;i++)s.Step();
        Check(p.Trails.Count>0,"No moving courier was observed");
        var restored=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(s.State)));s.Step(600);restored.Step(600);
        Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(restored.State),"Traffic or wear diverged after save/load");
        var trail=p.Trails[0];Check(trail.Strength(trail.LastTick)>0&&trail.Strength(trail.LastTick+(long)Math.Ceiling(1800/Definitions.TickSeconds))==0,"Unused traces do not fade");
        string before=SaveCodec.ToJson(s.State);foreach(var trace in p.Trails)trace.Strength(s.State.Tick+100000);
        Check(before==SaveCodec.ToJson(s.State),"Reading visual wear mutated goods or simulation");
    }
}
