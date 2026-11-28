using System;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        // Explicit finite integration fixture. Never called by CreateDiplomacyScenario.
        public static GameSimulation CreateDiplomacyFixture()
        {
            var sim=CreateDiplomacyScenario(2026);sim.State.ComputerFactions.Clear();sim.State.SpaceTestFixture=true;
            foreach(var p in sim.State.Planets)if(p.OwnerId>0)
            {
                var f=sim.State.Factions.Find(a=>a.Id==p.OwnerId);foreach(Technology t in Enum.GetValues(typeof(Technology)))f.Research.Completed.Add(t);
                foreach(var spec in new[]{new[]{(int)BuildingKind.SolarPlant,10,5},new[]{(int)BuildingKind.Farm,9,7},new[]{(int)BuildingKind.Spaceport,7,5},new[]{(int)BuildingKind.JumpGate,7,7}})
                {
                    var b=new BuildingState {Id=sim.State.NextEntityId++,PlanetId=p.Id,Kind=(BuildingKind)spec[0],X=spec[1],Y=spec[2],Built=true};
                    if(b.Kind==BuildingKind.JumpGate)Array.Copy(Definitions.GateComponents,b.Inventory,Definitions.ResourceCount);
                    if(b.Kind==BuildingKind.Spaceport){b.Inventory[2]=24;b.Inventory[3]=12;}
                    p.Buildings.Add(b);
                }
                var pos=sim.GetOrbitPosition(p.Id);sim.State.Ships.Add(new ShipState {Id=sim.State.NextEntityId++,OwnerId=p.OwnerId,Kind=ShipKind.Freighter,X=pos.X,Y=pos.Y,TargetPlanetId=p.Id,Crew=2,Health=100});
            }
            sim.Step();sim.State.LastMessage="TESTFIXTURE M7: endliche vorgebaute Häfen, Toranlagen, Forschung, Frachter und Komponenten. Kein regulärer Wirtschaftsstart.";return sim;
        }
    }
}
