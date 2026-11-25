using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using PlanetOne.Core;

internal static class OrbitTests
{
    public static void Register(Action<string,Action> run)
    {
        run("Kepler ellipse has its central body at a focus and closes after one period", Ellipse);
        run("Kepler motion sweeps equal areas in equal times and speeds up at periapsis", EqualAreas);
        run("Orbital periods obey the common central mass relation and are substantially slower", Periods);
        run("Kepler state preserves pause and exact save-load continuation", Continuation);
        run("Actual v8 migration preserves every non-orbital value and current world positions", Migration);
        run("Invalid eccentricity and inconsistent orbital periods are rejected", InvalidOrbits);
    }
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    private static double Distance(SimPoint a,SimPoint b){double x=a.X-b.X,y=a.Y-b.Y;return Math.Sqrt(x*x+y*y);}
    private static void Ellipse()
    {
        var p=GameSimulation.CreateDefault().FindPlanet(1);p.OrbitEccentricity=0.35;p.OrbitPhase=0;p.OrbitPeriapsis=0;
        var near=OrbitalMotion.Position(p,0);var far=OrbitalMotion.Position(p,p.OrbitPeriodSeconds/2);
        Check(Math.Abs(near.X-p.OrbitRadius*(1-p.OrbitEccentricity))<1e-10 && Math.Abs(far.X+p.OrbitRadius*(1+p.OrbitEccentricity))<1e-10,"Incorrect ellipse focus");
        Check(Distance(near,OrbitalMotion.Position(p,p.OrbitPeriodSeconds))<1e-10,"Orbit does not close");
        for(int i=0;i<100;i++)
        {var v=OrbitalMotion.Position(p,i*p.OrbitPeriodSeconds/100);double a=p.OrbitRadius,b=a*Math.Sqrt(1-p.OrbitEccentricity*p.OrbitEccentricity),x=v.X+a*p.OrbitEccentricity;Check(Math.Abs(x*x/(a*a)+v.Y*v.Y/(b*b)-1)<1e-10,"Position is off ellipse");}
    }
    private static double SweptArea(PlanetState p,double start,double duration)
    {
        double area=0;var previous=OrbitalMotion.Position(p,start);
        for(int i=1;i<=1024;i++){var next=OrbitalMotion.Position(p,start+duration*i/1024);area+=(previous.X*next.Y-next.X*previous.Y)/2;previous=next;}
        return area;
    }
    private static void EqualAreas()
    {
        var p=GameSimulation.CreateDefault().FindPlanet(1);p.OrbitEccentricity=0.45;p.OrbitPhase=0;
        double areaNear=SweptArea(p,0,100),areaFar=SweptArea(p,p.OrbitPeriodSeconds/2,100);
        Check(Math.Abs(areaNear/areaFar-1)<1e-7,"Area speed is not constant");
        double near=Distance(OrbitalMotion.Position(p,0),OrbitalMotion.Position(p,1));
        double far=Distance(OrbitalMotion.Position(p,p.OrbitPeriodSeconds/2),OrbitalMotion.Position(p,p.OrbitPeriodSeconds/2+1));
        Check(near>far*2,"Periapsis is not faster");
    }
    private static void Periods()
    {
        var game=GameSimulation.CreateDiplomacyScenario();double common=0;
        foreach(var p in game.State.Planets)
        {
            if(p.ParentId>=0){Check(p.OrbitPeriodSeconds>600,"Moon remains too fast");continue;}
            double ratio=p.OrbitPeriodSeconds*p.OrbitPeriodSeconds/Math.Pow(p.OrbitRadius,3);
            if(common==0)common=ratio;Check(Math.Abs(ratio/common-1)<1e-12,"Third law violated");
            Check(p.OrbitPeriodSeconds>=3600,"Planet remains too fast");
        }
        Check(Math.Abs(Math.Pow(OrbitalMotion.Period(6,true)/OrbitalMotion.Period(3,true),2)-8)<1e-12,"Moon period scaling differs");
    }
    private static void Continuation()
    {
        var a=GameSimulation.CreateDefault();a.State.Tick=987654;a.State.Paused=true;
        var before=a.GetOrbitPosition(1);a.Advance(80);Check(Distance(before,a.GetOrbitPosition(1))==0,"Paused orbit moved");
        var b=new GameSimulation(SaveCodec.FromJson(SaveCodec.ToJson(a.State)));a.State.Paused=b.State.Paused=false;
        a.Step(200);b.Step(200);Check(SaveCodec.ToJson(a.State)==SaveCodec.ToJson(b.State),"Continuation diverged");
        foreach(var p in a.State.Planets)Check(Distance(a.GetOrbitPosition(p.Id),b.GetOrbitPosition(p.Id))==0,"Position changed on load");
    }
    private static string RawJson(GameState state)
    {using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(GameState)).WriteObject(stream,state);return Encoding.UTF8.GetString(stream.ToArray());}}
    private static SimPoint OldPosition(GameState s,PlanetState p)
    {double angle=p.OrbitPhase+s.Tick*Definitions.TickSeconds*OrbitalMotion.Tau/p.OrbitPeriodSeconds;var point=new SimPoint(Math.Cos(angle)*p.OrbitRadius,Math.Sin(angle)*p.OrbitRadius);if(p.ParentId>=0){var parent=OldPosition(s,s.Planets.Find(v=>v.Id==p.ParentId));point.X+=parent.X;point.Y+=parent.Y;}return point;}
    private static void Migration()
    {
        string json=File.ReadAllText("Tests/Core/Fixtures/prototype-v8.json");GameState old;
        using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))old=(GameState)new DataContractJsonSerializer(typeof(GameState)).ReadObject(stream);
        Check(old.Version==8,"Fixture is not v8");var migrated=SaveCodec.FromJson(json);var game=new GameSimulation(migrated);
        foreach(var p in old.Planets)Check(Distance(OldPosition(old,p),game.GetOrbitPosition(p.Id))<1e-8,"Migration teleported "+p.Name);
        var comparison=SaveCodec.FromJson(SaveCodec.ToJson(migrated));comparison.Version=old.Version;
        for(int i=0;i<old.Planets.Count;i++)
        {var p=comparison.Planets[i];p.Kind=old.Planets[i].Kind;p.OrbitPhase=old.Planets[i].OrbitPhase;p.OrbitPeriodSeconds=old.Planets[i].OrbitPeriodSeconds;p.OrbitEccentricity=old.Planets[i].OrbitEccentricity;p.OrbitPeriapsis=old.Planets[i].OrbitPeriapsis;}
        Check(RawJson(old)==RawJson(comparison),"Migration changed goods, ships, orders or another non-orbital value");
    }
    private static void InvalidOrbits()
    {
        foreach(int kind in new[]{0,1,2})
        {var game=GameSimulation.CreateDefault();var p=game.FindPlanet(1);if(kind==0)p.OrbitEccentricity=1;else if(kind==1)p.OrbitPeriodSeconds*=2;else p.OrbitPeriapsis=double.NaN;bool rejected=false;try{SaveCodec.ToJson(game.State);}catch(InvalidDataException){rejected=true;}Check(rejected,"Malformed orbit accepted");}
    }
}
