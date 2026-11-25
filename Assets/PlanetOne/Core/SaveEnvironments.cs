namespace PlanetOne.Core
{
    public static partial class SaveCodec
    {
        private static void MigrateEnvironments(GameState state)
        {
            Require(state.Planets!=null,"Weltliste fehlt.");
            foreach(var p in state.Planets)
            {
                Require(p!=null,"Welt fehlt.");
                // Legacy worlds have real ground infrastructure; never turn one into atmosphere or water.
                p.Kind=p.ParentId>=0?PlanetKind.Rocky:p.Id==1?PlanetKind.Temperate:p.Id==2?PlanetKind.Ice:PlanetKind.Desert;
            }
            state.Version=10;
        }
    }
}
