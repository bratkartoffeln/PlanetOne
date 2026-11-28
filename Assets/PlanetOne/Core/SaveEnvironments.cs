namespace PlanetOne.Core
{
    public static partial class SaveCodec
    {
        private static void AddHydrogenSlot<T>(ref T[] values,T initial)
        {
            // Earlier migration stages can already have allocated current-size empty ledgers.
            Require(values!=null && (values.Length==6 || values.Length==Definitions.ResourceCount),"Alte Warenliste unvollständig.");
            System.Array.Resize(ref values,Definitions.ResourceCount);
            values[(int)ResourceKind.Hydrogen]=initial;
        }

        private static void MigrateAtmosphere(GameState state)
        {
            Require(state.Planets!=null && state.Ships!=null,"Alte Welt oder Schiffsliste fehlt.");
            AddHydrogenSlot(ref state.SpaceLostTotals,0);
            foreach(var ship in state.Ships)
            {
                Require(ship!=null,"Altes Schiff fehlt.");
                AddHydrogenSlot(ref ship.Cargo,0);
            }
            foreach(var p in state.Planets)
            {
                Require(p!=null && p.Buildings!=null,"Alte Kolonie fehlt.");
                p.AtmosphericHydrogen=PlanetEnvironment.InitialHydrogen(state.Seed,p);
                AddHydrogenSlot(ref p.ProducedTotals,0);
                AddHydrogenSlot(ref p.ConsumedTotals,0);
                AddHydrogenSlot(ref p.RecoveredTotals,0);
                AddHydrogenSlot(ref p.LostTotals,0);
                foreach(var b in p.Buildings)
                {
                    Require(b!=null && b.Supply!=null,"Alte Gebäudeversorgung fehlt.");
                    AddHydrogenSlot(ref b.Inventory,0);
                    AddHydrogenSlot(ref b.ReservedIncoming,0);
                    AddHydrogenSlot(ref b.ReservedOutgoing,0);
                    AddHydrogenSlot(ref b.Supply.Minimum,0);
                    AddHydrogenSlot(ref b.Supply.Target,PlanetEnvironment.Airborne(p) && b.Kind==BuildingKind.Spaceport?8:0);
                    AddHydrogenSlot(ref b.Supply.ExportReserve,0);
                    AddHydrogenSlot(ref b.Supply.Priority,1);
                    AddHydrogenSlot(ref b.Supply.Replenishing,false);
                }
            }
            state.Version=14;
        }

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
