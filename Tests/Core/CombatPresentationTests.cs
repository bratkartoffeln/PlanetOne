using System;
using PlanetOne.Core;

internal static class CombatPresentationTests
{
    public static void Register(Action<string,Action> run)
    {
        run("Combat presentation records only real ammunition expenditure and lethal damage", RealShots);
        run("Combat presentation never exposes unseen combatants or hidden artillery hits", HiddenCombat);
        run("Combat presentation is bounded and absent from save state", TransientStream);
    }
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    private static GameSimulation Empty()
    {
        var sim=GameSimulation.CreateDefault(918);var p=sim.FindPlanet(1);
        p.Buildings.Clear();p.Population=0;p.WorkersTotal=0;p.HousingCapacity=0;
        Array.Clear(p.Terrain,0,p.Terrain.Length);Array.Clear(p.Roads,0,p.Roads.Length);
        sim.State.Relations[0].Relation=RelationKind.War;sim.State.MilitaryTestFixture=true;return sim;
    }
    private static UnitState Unit(GameSimulation sim,UnitKind kind,int owner,int x,int y)
    {
        var d=Definitions.Get(kind);var unit=new UnitState {Id=sim.State.NextEntityId++,Kind=kind,OwnerId=owner,
            X=x,Y=y,GoalX=x,GoalY=y,Health=d.Health,Food=2,Ammunition=d.Ammo};
        sim.FindPlanet(1).Units.Add(unit);return unit;
    }
    private static void RealShots()
    {
        var sim=Empty();var source=Unit(sim,UnitKind.Tank,1,3,4);var target=Unit(sim,UnitKind.Tank,2,5,4);
        target.Ammunition=0;int ammunition=source.Ammunition;sim.Step();
        Check(sim.CombatPresentationEvents.Count==1 && source.Ammunition==ammunition-1,"No exact shot/ammunition correspondence");
        var shot=sim.CombatPresentationEvents[0];
        Check(shot.SourceId==source.Id && shot.TargetId==target.Id && shot.AmmunitionSpent==1 && shot.SourceVisible && shot.ImpactVisible,"Wrong shot observation");
        Check(target.Health<Definitions.Get(target.Kind).Health,"Shot without actual damage");
        sim.Step();Check(sim.CombatPresentationEvents.Count==1,"Reload cooldown invented another shot");
        source.Cooldown=0;source.Ammunition=0;sim.Step();Check(sim.CombatPresentationEvents.Count==1,"Empty gun produced a shot");
        source.Ammunition=1;target.Health=1;sim.Step();
        Check(target.Destroyed && sim.CombatPresentationEvents.Count==3 && sim.CombatPresentationEvents[2].Kind==CombatVisualKind.Explosion,"Real destruction not reported exactly once");
        sim.Step(5);Check(sim.CombatPresentationEvents.Count==3,"Destroyed target produced duplicate explosions");
    }
    private static void HiddenCombat()
    {
        var sim=Empty();sim.State.Factions.Add(new FactionState {Id=3,Name="Prüfgegner"});
        sim.State.Relations.Add(new FactionRelation {FirstId=2,SecondId=3,Relation=RelationKind.War});
        var enemy=Unit(sim,UnitKind.Tank,2,3,4);var other=Unit(sim,UnitKind.Tank,3,5,4);
        sim.Step();Check(enemy.Ammunition<Definitions.Get(enemy.Kind).Ammo && other.Health<Definitions.Get(other.Kind).Health,"Hidden fixture did not actually fight");
        Check(sim.CombatPresentationEvents.Count==0,"Unobserved battle leaked into player's event stream");

        var artillerySim=Empty();var artillery=Unit(artillerySim,UnitKind.Artillery,1,3,4);var hidden=Unit(artillerySim,UnitKind.Tank,2,10,4);
        hidden.Ammunition=0;hidden.Health=1;
        // Explicit test fixture: an already accepted known-position artillery order, now outside current sight.
        artillery.Order=UnitOrder.Bombard;artillery.GoalX=10;artillery.GoalY=4;
        artillerySim.Step();Check(hidden.Destroyed,"Known-position fire did not hit the fixture target");
        Check(artillerySim.CombatPresentationEvents.Count==1,"Hidden destruction leaked a confirmation");
        var shot=artillerySim.CombatPresentationEvents[0];
        Check(shot.SourceId==artillery.Id && shot.TargetId==0 && shot.TargetKnown && !shot.ImpactVisible && shot.TargetX==10,"Known location revealed hidden identity or impact");
    }
    private static void TransientStream()
    {
        var sim=Empty();var source=Unit(sim,UnitKind.Tank,1,3,4);var target=Unit(sim,UnitKind.Tank,2,5,4);target.Ammunition=0;
        for(int i=0;i<520;i++){source.Cooldown=0;source.Ammunition=1;target.Health=Definitions.Get(target.Kind).Health;sim.Step();}
        Check(sim.CombatPresentationEvents.Count==512 && sim.CombatPresentationSequence==520,"Presentation stream is unbounded or nonmonotonic");
        string save=SaveCodec.ToJson(sim.State);var events=sim.CombatPresentationEvents;
        Check(events[0].Sequence==9 && SaveCodec.ToJson(sim.State)==save,"Reading observations changed the simulation");
        var restored=new GameSimulation(SaveCodec.FromJson(save));
        Check(restored.CombatPresentationEvents.Count==0 && restored.CombatPresentationSequence==0 && SaveCodec.ToJson(restored.State)==save,"Transient observations entered savegame or replayed on load");
    }
}
