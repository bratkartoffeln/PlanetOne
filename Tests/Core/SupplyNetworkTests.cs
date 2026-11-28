using System;
using PlanetOne.Core;

internal static class SupplyNetworkTests
{
    internal static void Register(Action<string,Action> run)
    {
        run("Supply connection plans around obstacles and constructs through real deliveries",Build);
        run("Rejected supply connections are atomic and respect ownership and terrain",Reject);
        run("Queued supply connections survive save load and do not duplicate goods",Persistence);
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static GameSimulation Fixture()
    {
        var s=GameSimulation.CreateDefault();var p=s.FindPlanet(1);
        Array.Clear(p.Roads,0,p.Roads.Length);Array.Clear(p.Terrain,0,p.Terrain.Length);
        return s;
    }
    private static SimCommand Connect(GameSimulation s,int x,int y,int issuer=1)
    {return new SimCommand{Id=s.NextCommandId(),IssuerId=issuer,PlanetId=1,Kind=CommandKind.ConnectSupply,ActorId=s.FindPlanet(1).Buildings[0].Id,X=x,Y=y};}
    private static void Build()
    {
        var s=Fixture();var p=s.FindPlanet(1);p.Terrain[7*p.Width+10]=1;
        string error;var path=s.PlanSupplyConnection(1,1,p.Buildings[0].Id,14,10,out error);
        Check(path!=null&&!path.Exists(c=>c.X==10&&c.Y==7),"Planner crossed rock");
        var command=Connect(s,14,10);s.Enqueue(command);s.Step();
        Check(s.State.CommandResults.Find(r=>r.CommandId==command.Id).Success,"Connection rejected");
        Check(s.ValidatePlacement(1,BuildingKind.SolarPlant,14,10)==null,"Connected site is not buildable");
        s.Enqueue(new SimCommand{Id=s.NextCommandId(),PlanetId=1,Kind=CommandKind.PlaceBuilding,Building=BuildingKind.SolarPlant,X=14,Y=10});s.Step();
        var site=p.Buildings.Find(b=>b.X==14&&b.Y==10);Check(site!=null,"Missing paid site");
        bool physical=false;for(int i=0;i<4000&&!site.Built;i++){s.Step();physical|=p.Shipments.Count>0;}
        Check(site.Built&&physical,"Connection did not carry real materials and a builder");SaveCodec.Validate(s.State);
    }
    private static void Reject()
    {
        var s=Fixture();var p=s.FindPlanet(1);for(int x=0;x<p.Width;x++)p.Terrain[7*p.Width+x]=1;
        foreach(var c in new[]{Connect(s,14,10),Connect(s,10,6,2),Connect(s,-1,0)})
        {
            var before=(bool[])p.Roads.Clone();s.Enqueue(c);s.Step();
            Check(!s.State.CommandResults.Find(r=>r.CommandId==c.Id).Success,"Invalid route accepted");
            for(int i=0;i<before.Length;i++)Check(before[i]==p.Roads[i],"Failed connection partially changed the network");
        }
        string error;var gas=PlanetEnvironmentFixture.Create();
        Check(gas.PlanSupplyConnection(7,1,gas.FindPlanet(7).Buildings[0].Id,10,10,out error)==null,"Gas world accepted ground corridors");
    }
    private static void Persistence()
    {
        var s=Fixture();var p=s.FindPlanet(1);int metal=p.Buildings[0].Inventory[2],parts=p.Buildings[0].Inventory[3];
        var c=Connect(s,14,10);s.Enqueue(c);var restored=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(s.State)));
        s.Step();restored.Step();Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(restored.State),"Connection diverged after save/load");
        Check(p.Buildings[0].Inventory[2]==metal&&p.Buildings[0].Inventory[3]==parts,"Planning created or consumed cargo");
        Check(!s.Enqueue(c),"Duplicate command was accepted");SaveCodec.Validate(s.State);
    }
}
