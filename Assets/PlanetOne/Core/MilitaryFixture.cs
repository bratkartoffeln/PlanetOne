using System;

namespace PlanetOne.Core
{
    /// <summary>Explicit finite test scenario. Never invoked by normal colony creation or simulation ticks.</summary>
    public static class MilitaryFixture
    {
        public static GameSimulation Create()
        {
            var sim = GameSimulation.CreateDefault(3003); var p = sim.FindPlanet(1);
            sim.State.MilitaryTestFixture = true; sim.State.Paused = true;
            sim.State.Relations[0].Relation = RelationKind.War;
            p.Name = "M3 Prüfkarte · endliche Testbestände";
            p.Population = 28; p.WorkersTotal = 21; p.HousingCapacity = 28;
            Array.Clear(p.Terrain, 0, p.Terrain.Length); Array.Clear(p.OreRemaining, 0, p.OreRemaining.Length);
            for (int x = 2; x < 16; x++) { p.Roads[2 * p.Width + x] = true; p.Roads[10 * p.Width + x] = true; }
            for (int y = 2; y <= 10; y++) p.Roads[y * p.Width + 9] = true;
            p.Buildings[0].Inventory[0] = 100;
            AddBuilding(sim, BuildingKind.SolarPlant, 7, 5);
            AddBuilding(sim, BuildingKind.SolarPlant, 7, 7);
            AddBuilding(sim, BuildingKind.Barracks, 5, 5);
            AddBuilding(sim, BuildingKind.VehicleFactory, 5, 7);
            var depot = AddBuilding(sim, BuildingKind.MilitaryDepot, 9, 7); depot.Inventory[0] = 8; depot.Inventory[3] = 24;
            AddBuilding(sim, BuildingKind.Farm, 6, 5);
            AddBuilding(sim, BuildingKind.Housing, 6, 7);
            AddBuilding(sim, BuildingKind.GunTower, 10, 3).Ammunition = 16;
            AddBuilding(sim, BuildingKind.AntiTankTower, 10, 9).Ammunition = 16;
            AddBuilding(sim, BuildingKind.ArtilleryTower, 7, 9).Ammunition = 8;
            AddBuilding(sim, BuildingKind.FlakTower, 6, 9).Ammunition = 8;
            for (int y = 4; y <= 8; y++)
            {
                AddBuilding(sim, y == 6 ? BuildingKind.Gate : BuildingKind.Wall, 11, y);
                if (y != 6) p.Roads[y * p.Width + 11] = false;
            }
            AddBuilding(sim, BuildingKind.Wall, 9, 4); AddBuilding(sim, BuildingKind.Gate, 9, 6);
            AddUnit(sim, UnitKind.Infantry, 1, 9, 3);
            AddUnit(sim, UnitKind.AntiTank, 1, 9, 9);
            AddUnit(sim, UnitKind.Tank, 1, 6, 6);
            AddUnit(sim, UnitKind.Artillery, 1, 4, 6);
            AddUnit(sim, UnitKind.Engineer, 1, 8, 7);
            AddUnit(sim, UnitKind.AntiAir, 1, 7, 8);
            AddUnit(sim, UnitKind.Tank, 2, 15, 2);
            AddUnit(sim, UnitKind.Infantry, 2, 14, 2);
            AddUnit(sim, UnitKind.Artillery, 2, 15, 4);
            AddUnit(sim, UnitKind.Tank, 2, 15, 10);
            AddUnit(sim, UnitKind.AntiTank, 2, 14, 10);
            sim.State.LastMessage = "TESTFIXTURE: zwei Angriffswege, endliche Armeen und Vorräte. Leertaste startet. Keine Fraktions-KI.";
            SaveCodec.Validate(sim.State); return sim;
        }
        private static BuildingState AddBuilding(GameSimulation sim, BuildingKind kind, int x, int y)
        {
            var p = sim.FindPlanet(1);
            var b = new BuildingState { Id = sim.State.NextEntityId++, PlanetId = p.Id, Kind = kind,
                X = x, Y = y, Built = true, Status = "Bereit", Health = Definitions.BuildingHealth(kind) };
            if (kind != BuildingKind.Gate) p.Roads[y * p.Width + x] = false;
            p.Buildings.Add(b); return b;
        }
        private static void AddUnit(GameSimulation sim, UnitKind kind, int owner, int x, int y)
        {
            var d = Definitions.Get(kind);
            sim.FindPlanet(1).Units.Add(new UnitState { Id = sim.State.NextEntityId++, OwnerId = owner,
                Kind = kind, X = x, Y = y, Health = d.Health, Ammunition = d.Ammo, Food = 2, GoalX = x, GoalY = y });
        }
        public static void OrderAttack(GameSimulation sim, bool north)
        {
            if (!sim.State.MilitaryTestFixture) throw new InvalidOperationException("Nur für die ausgewiesene Prüfkarte.");
            foreach (var u in sim.FindPlanet(1).Units)
            {
                if (u.OwnerId != 2 || u.Destroyed || ((u.Y < 7) != north)) continue;
                sim.Enqueue(new SimCommand { Id = sim.NextCommandId(), PlanetId = 1, IssuerId = 2, ActorId = u.Id,
                    Kind = CommandKind.MoveUnit, X = 9, Y = north ? 2 : 10 });
            }
        }
    }
}
