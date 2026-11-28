using System;

namespace PlanetOne.Core
{
    public sealed class BuildingDefinition
    {
        public readonly BuildingKind Kind;
        public readonly string Name;
        public readonly int[] Cost;
        public readonly double BuildSeconds;
        public readonly double CycleSeconds;
        public readonly int EnergyUse;
        public readonly int EnergyProduced;
        public readonly int WorkerUse;
        public readonly int Capacity;
        public readonly int[] Inputs;
        public readonly int[] Outputs;
        public readonly int HousingCapacity;
        public readonly double EnergyStorageCapacity;

        internal BuildingDefinition(BuildingKind kind, string name, int metal, int parts,
            double buildSeconds, int energyUse, int energyProduced, int workerUse, int capacity,
            double cycleSeconds = 0, int[] inputs = null, int[] outputs = null,
            int housingCapacity = 0, double energyStorageCapacity = 0)
        {
            Kind = kind; Name = name; Cost = new int[6];
            Cost[(int)ResourceKind.Metal] = metal; Cost[(int)ResourceKind.Parts] = parts;
            BuildSeconds = buildSeconds * Definitions.ConstructionDurationMultiplier; EnergyUse = energyUse; EnergyProduced = energyProduced;
            WorkerUse = workerUse; Capacity = capacity; CycleSeconds = cycleSeconds;
            Inputs = inputs ?? new int[6]; Outputs = outputs ?? new int[6];
            HousingCapacity = housingCapacity; EnergyStorageCapacity = energyStorageCapacity;
        }
    }

    public static partial class Definitions
    {
        public const double TickSeconds = 0.05;
        public const int CourierCapacity = 8;
        public const int CouriersPerColony = 4;
        public const double CourierSpeed = 3;
        public const double TransferSeconds = 0.5;
        public const double FoodPerPersonPerSecond = 1.0 / 120;
        public const double PopulationGrowthSeconds = 45;
        public const double WorkforceFraction = 0.75;
        public const double MinimumProductivity = 0.35;
        public const double FoodShortageGraceSeconds = 15;
        public const double FoodShortageRampSeconds = 60;
        public const double ConstructionRefundFraction = 0.5;
        public const double DemolitionRefundFraction = 0.5;
        public const int EventHistoryLimit = 128;
        public const int ConstructionSlotsPerColony = 2;
        public const double ConstructionDurationMultiplier = 2;
        public const double EnergyRestartReserveSeconds = 2;

        private static int[] Goods(ResourceKind resource, int amount)
        {
            var result = new int[6]; result[(int)resource] = amount; return result;
        }

        // Initial prototype balance. Rules live in GameSimulation, numerical values here.
        private static readonly BuildingDefinition[] Buildings =
        {
            new BuildingDefinition(BuildingKind.Headquarters, "Kommandozentrum", 0, 0, 0, 0, 4, 0, 300,
                housingCapacity: 16, energyStorageCapacity: 20),
            new BuildingDefinition(BuildingKind.Mine, "Erzmine", 8, 2, 3, 2, 0, 2, 40, 2,
                null, Goods(ResourceKind.Ore, 2)),
            new BuildingDefinition(BuildingKind.Smelter, "Schmelze", 12, 4, 4, 3, 0, 3, 40, 3,
                Goods(ResourceKind.Ore, 2), Goods(ResourceKind.Metal, 1)),
            new BuildingDefinition(BuildingKind.SolarPlant, "Solaranlage", 8, 2, 3, 0, 8, 0, 30,
                energyStorageCapacity: 10),
            new BuildingDefinition(BuildingKind.Warehouse, "Lager", 12, 2, 4, 0, 0, 0, 120),
            new BuildingDefinition(BuildingKind.Workshop, "Werkstatt", 14, 4, 4, 3, 0, 3, 40, 5,
                Goods(ResourceKind.Metal, 2), Goods(ResourceKind.Parts, 1)),
            new BuildingDefinition(BuildingKind.Farm, "Hydroponik", 8, 2, 3, 2, 0, 2, 40, 4,
                null, Goods(ResourceKind.Food, 2)),
            new BuildingDefinition(BuildingKind.Housing, "Wohnhabitat", 18, 4, 5, 1, 0, 0, 40,
                housingCapacity: 12),
            new BuildingDefinition(BuildingKind.Barracks, "Kaserne", 16, 4, 6, 2, 0, 2, 60),
            new BuildingDefinition(BuildingKind.VehicleFactory, "Fahrzeugwerk", 22, 6, 8, 3, 0, 3, 80),
            new BuildingDefinition(BuildingKind.MilitaryDepot, "Militärdepot", 12, 4, 5, 1, 0, 1, 80),
            new BuildingDefinition(BuildingKind.GunTower, "Schnellfeuerturm", 10, 3, 5, 1, 0, 1, 24, energyStorageCapacity: 10),
            new BuildingDefinition(BuildingKind.AntiTankTower, "Panzerabwehrstellung", 14, 4, 6, 1, 0, 1, 24, energyStorageCapacity: 10),
            new BuildingDefinition(BuildingKind.FlakTower, "Flugabwehrstellung", 12, 4, 6, 1, 0, 1, 24, energyStorageCapacity: 10),
            new BuildingDefinition(BuildingKind.ArtilleryTower, "Artilleriestellung", 18, 5, 8, 2, 0, 2, 24, energyStorageCapacity: 10),
            new BuildingDefinition(BuildingKind.Wall, "Mauer", 3, 0, 3, 0, 0, 0, 12),
            new BuildingDefinition(BuildingKind.Gate, "Tor", 5, 1, 4, 0, 0, 0, 12),
            new BuildingDefinition(BuildingKind.Spaceport, "Raumhafen", 18, 5, 8, 2, 0, 2, 240),
            new BuildingDefinition(BuildingKind.Laboratory, "Forschungslabor", 12, 4, 6, 2, 0, 2, 40),
            new BuildingDefinition(BuildingKind.CrystalMine, "Kristallmine", 12, 4, 6, 2, 0, 2, 40, 5, null, Goods(ResourceKind.Crystals, 1)),
            new BuildingDefinition(BuildingKind.ElectronicsFactory, "Elektronikwerk", 16, 6, 8, 3, 0, 3, 50, 6, new[] {0,0,2,0,1,0}, Goods(ResourceKind.Electronics, 1)),
            new BuildingDefinition(BuildingKind.ShieldGenerator, "Lokaler Schild", 24, 10, 12, 6, 0, 2, 40),
            new BuildingDefinition(BuildingKind.OrbitalBattery, "Planetare Abwehrbatterie", 28, 12, 12, 4, 0, 2, 40),
            new BuildingDefinition(BuildingKind.JumpGate, "Interstellares Sprungtor", 40, 16, 30, 8, 0, 4, 160)
        };

        public static BuildingDefinition Get(BuildingKind kind)
        {
            if ((int)kind < 0 || (int)kind >= Buildings.Length) throw new ArgumentOutOfRangeException("kind");
            return Buildings[(int)kind];
        }

        public static string ResourceName(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.Food: return "Nahrung";
                case ResourceKind.Ore: return "Erz";
                case ResourceKind.Metal: return "Metall";
                case ResourceKind.Parts: return "Bauteile";
                case ResourceKind.Crystals: return "Kristalle";
                case ResourceKind.Electronics: return "Elektronik";
                default: return "Unbekannt";
            }
        }
    }
}
