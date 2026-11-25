using System;
using System.Collections.Generic;
using System.IO;
using PlanetOne.Core;

internal static class HexGridTests
{
    internal static void Register(Action<string, Action> run)
    {
        run("Hex centers and both sides of all six edges pick the correct cell", Picking);
        run("Hex neighbors are symmetric and every direction has equal physical length", Neighbors);
        run("Hex ground movement uses all six exits at equal speed and resumes saves", Movement);
        run("Hex roads connect across sloping edges and never across nonadjacent corners", Roads);
        run("Hex cover and line of sight follow sloping neighboring cells", Sight);
        run("Hex landing reserves seven cells and prevents overlapping carriers", Landing);
        run("Hex save validation accepts sloping paths and rejects square diagonals", Paths);
        run("Actual version five invasion save migrates without changing goods or entities", Migration);
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static bool Same(GridCell a, GridCell b) { return a.X == b.X && a.Y == b.Y; }
    private static void Picking()
    {
        for (int y = -1; y < 14; y++) for (int x = -1; x < 18; x++)
        {
            var a = HexGrid.ToWorld(x, y); Check(Same(HexGrid.FromWorld(a.X, a.Y), new GridCell(x, y)), "Center mismatch");
            for (int d = 0; d < 6; d++)
            {
                var n = HexGrid.Neighbor(x, y, d); var b = HexGrid.ToWorld(n.X, n.Y);
                Check(Same(HexGrid.FromWorld(a.X + (b.X-a.X)*0.499, a.Y+(b.Y-a.Y)*0.499), new GridCell(x,y)), "Inner edge wrong");
                Check(Same(HexGrid.FromWorld(a.X + (b.X-a.X)*0.501, a.Y+(b.Y-a.Y)*0.501), n), "Outer edge wrong");
            }
        }
    }
    private static void Neighbors()
    {
        for (int y = 4; y <= 5; y++)
        {
            var found = new HashSet<string>();
            for (int d = 0; d < 6; d++)
            {
                var n = HexGrid.Neighbor(4,y,d); Check(found.Add(n.X+":"+n.Y), "Repeated neighbor");
                Check(HexGrid.Steps(4,y,n.X,n.Y)==1 && Same(HexGrid.Neighbor(n.X,n.Y,(d+3)%6),new GridCell(4,y)), "Asymmetric neighbor");
                Check(Math.Abs(HexGrid.Distance(4,y,n.X,n.Y)-1)<1e-9, "Direction changes speed/range");
                for (int i=1;i<10;i++) Check(Math.Abs(HexGrid.Distance(4,y,4+(n.X-4)*i/10.0,y+(n.Y-y)*i/10.0)-i/10.0)<1e-9,"Nonlinear segment");
            }
        }
    }
    private static GameSimulation Empty()
    {
        var sim = GameSimulation.CreateDefault(); var p = sim.FindPlanet(1);
        Array.Clear(p.Terrain,0,p.Terrain.Length); Array.Clear(p.OreRemaining,0,p.OreRemaining.Length); Array.Clear(p.Roads,0,p.Roads.Length);
        return sim;
    }
    private static void Movement()
    {
        for(int row=4;row<=5;row++) for(int d=0;d<6;d++)
        {
            var sim=Empty(); var u=InvasionFixture.AddUnit(sim,1,1,UnitKind.Tank,4,row); var n=HexGrid.Neighbor(4,row,d);
            sim.Enqueue(new SimCommand {Id=sim.NextCommandId(),IssuerId=1,PlanetId=1,ActorId=u.Id,Kind=CommandKind.MoveUnit,X=n.X,Y=n.Y});
            sim.Step(5); Check(u.Path.Count==2,"Neighbor requires detour");
            Check(Math.Abs(HexGrid.Distance(4,row,u.X,u.Y)-5*Definitions.TickSeconds*Definitions.Get(u.Kind).Speed)<1e-8,"Wrong speed");
            var copy=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(sim.State))); sim.Step(30);copy.Step(30);
            Check(u.X==n.X && u.Y==n.Y,"Neighbor never reached"); Check(SaveCodec.ToJson(sim.State)==SaveCodec.ToJson(copy.State),"Movement continuation differs");
        }
    }
    private static void Roads()
    {
        var sim=Empty();var p=sim.FindPlanet(1); // HQ at 8/6, west-up sloping neighbor 7/5.
        Check(sim.ValidatePlacement(1,BuildingKind.Warehouse,7,5)==null,"Sloping building edge not connected");
        Check(sim.ValidatePlacement(1,BuildingKind.Warehouse,9,5)!=null,"Square diagonal connected");
        p.Roads[5*p.Width+7]=true;
        Check(sim.ValidatePlacement(1,BuildingKind.Warehouse,7,4)==null,"Hex road fails to connect");
        sim.Enqueue(new SimCommand {Id=sim.NextCommandId(),PlanetId=1,Kind=CommandKind.PlaceBuilding,Building=BuildingKind.Warehouse,X=7,Y=4});sim.Step(800);
        Check(p.Buildings.Exists(b=>b.X==7 && b.Y==4 && b.Built),"Hex courier never builds destination");SaveCodec.Validate(sim.State);
    }
    private static void Sight()
    {
        var sim=Empty();var p=sim.FindPlanet(1);var u=InvasionFixture.AddUnit(sim,1,1,UnitKind.Infantry,4,4);
        p.Terrain[5*p.Width+3]=1; Check(sim.HasCover(p,u),"Sloping adjacent rock gives no cover");
        Check(!sim.HasLineOfSight(p,4,4,3,6) && !sim.HasLineOfSight(p,3,6,4,4),"Hex rock failed to block ray");
        p.Terrain[5*p.Width+3]=0; p.Terrain[5*p.Width+5]=1; Check(!sim.HasCover(p,u),"Nonadjacent square diagonal gives cover");
        Check(sim.HasLineOfSight(p,4,4,3,6),"Unrelated rock blocks ray");
    }
    private static void Landing()
    {
        var sim=Empty();var p=sim.FindPlanet(1);var a=InvasionFixture.AddShip(sim,1,1,ShipKind.LandingTransport);
        var b=InvasionFixture.AddShip(sim,1,1,ShipKind.LandingTransport);
        Check(sim.LandingError(a,p,4,4)==null,"Empty footprint blocked");
        foreach(var c in HexGrid.LandingCells(4,4))
        {p.Terrain[c.Y*p.Width+c.X]=1;Check(sim.LandingError(a,p,4,4)!=null,"Footprint ignores rock");p.Terrain[c.Y*p.Width+c.X]=0;}
        Check(sim.LandingError(a,p,0,0)!=null,"Off-map footprint accepted");
        a.LandingX=4;a.LandingY=4;a.TransportPhase=TransportPhase.Descending;
        Check(sim.LandingError(b,p,6,4)!=null,"Overlapping reservations accepted");
        Check(sim.LandingError(b,p,7,4)==null,"Disjoint footprints rejected");
    }
    private static void Paths()
    {
        var sim=Empty();var u=InvasionFixture.AddUnit(sim,1,1,UnitKind.Infantry,4,4);
        u.Path.Add(new GridCell(4,4));u.Path.Add(new GridCell(3,5));SaveCodec.Validate(sim.State);
        u.Path[1]=new GridCell(5,5); bool rejected=false;
        try{SaveCodec.Validate(sim.State);}catch(InvalidDataException){rejected=true;}
        Check(rejected,"Invalid hex edge persisted");
    }
    private static void Migration()
    {
        string json=File.ReadAllText("Tests/Core/Fixtures/invasion-v5.json");
        Check(json.Contains("\"Version\":5"),"Not a real old schema fixture");
        var state=SaveCodec.FromJson(json); Check(state.Version==SaveCodec.CurrentVersion,"Migration not applied");
        string migrated=SaveCodec.ToJson(state).Replace("\"AiScenario\":false,", "").Replace("\"ComputerFactions\":[],", "");
        foreach(string added in new[]{"\"DiplomacyScenario\":false,","\"DiplomacyEvents\":[],","\"Evacuations\":[],","\"GateProjects\":[],","\"Treaties\":[],","\"Winners\":[],",",\"Status\":0",",\"Terms\":null",",\"TreatyId\":0","\"PaymentLeg\":false,"})migrated=migrated.Replace(added,"");
        migrated=migrated.Replace(",\"Winners\":[]", "");
        migrated=migrated.Replace("\"AsteroidBelts\":[],", "").Replace("\"Asteroids\":[],", "").Replace("\"SpaceMinedOre\":0,", "").Replace("\"Mining\":null,", "");
        migrated=migrated.Replace("\"DestinationId\":0,", "");
        // V9 deliberately slows legacy orbits while preserving their position at migration.
        string orbitalFields="\"Orbit(Eccentricity|Periapsis|PeriodSeconds|Phase)\":[^,}]+,";
        migrated=System.Text.RegularExpressions.Regex.Replace(migrated,orbitalFields,"");
        migrated=System.Text.RegularExpressions.Regex.Replace(migrated,"\"Kind\":[0-9]+,(?=\"LostTotals\")","");
        string original=System.Text.RegularExpressions.Regex.Replace(json,orbitalFields,"");
        Check(migrated==original.Replace("\"Version\":5","\"Version\":"+SaveCodec.CurrentVersion),"Migration changed non-orbital state");
        var a=new GameSimulation(state);var b=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(state)));
        a.State.Paused=b.State.Paused=false;a.Step(200);b.Step(200);
        Check(SaveCodec.ToJson(a.State)==SaveCodec.ToJson(b.State),"Migrated continuation differs");
    }
}
