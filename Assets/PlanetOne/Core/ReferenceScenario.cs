namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        /// <summary>F05: finite, ordinary starting economy; no test grants or prepared fleets.</summary>
        public static GameSimulation CreateReferenceScenario(int seed=8026)
        {
            var state=new GameState {Seed=seed,AiScenario=true,DiplomacyScenario=true};
            state.Factions.Add(new FactionState {Id=1,Name="Koloniekonsortium",IsPlayer=true});
            state.Factions.Add(new FactionState {Id=2,Name="Freie Stationen"});
            state.Factions.Add(new FactionState {Id=3,Name="Helios-Verbund"});
            state.Relations.Add(new FactionRelation {FirstId=1,SecondId=2});
            state.Relations.Add(new FactionRelation {FirstId=1,SecondId=3});
            state.Relations.Add(new FactionRelation {FirstId=2,SecondId=3});
            state.Planets.Add(CreatePlanet(state,1,"Aster",-1,1,10,.2));
            state.Planets.Add(CreatePlanet(state,2,"Nereid",-1,2,18,2.5));
            state.Planets.Add(CreatePlanet(state,4,"Helios",-1,3,7.2,4));
            state.Planets.Add(CreatePlanet(state,3,"Khepri",2,0,3,0));
            state.Planets.Add(CreatePlanet(state,5,"Selene",4,0,2,2));
            foreach(var world in state.Planets)if(world.ParentId>=0)world.Terrain[9*world.Width+13]=3;
            state.ComputerFactions.Add(new AiState {FactionId=2,Profile=AiProfile.Expansion});
            state.ComputerFactions.Add(new AiState {FactionId=3,Profile=AiProfile.Technology});
            AsteroidGeneration.Populate(state);
            state.LastMessage="Drei Kolonien, zwei Kristallmonde: Wirtschaft aufbauen, Partner gewinnen und ein Sprungtor sichern. Jede Fraktion beginnt mit denselben endlichen Vorräten.";
            var simulation=new GameSimulation(state);simulation.UpdateSpaceKnowledge();return simulation;
        }
    }
}
