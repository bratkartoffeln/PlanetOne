using System;

namespace PlanetOne.Core
{
    /// <summary>Explicit demonstration: prepares only through ordinary paid commands and simulation time.</summary>
    public static class SpaceFixture
    {
        public static GameSimulation Create(int seed=4004)
        {
            var s=GameSimulation.CreateDefault(seed); s.State.SpaceTestFixture=true;
            Build(s,1,BuildingKind.SolarPlant,10,5); Build(s,1,BuildingKind.Farm,9,7);
            Build(s,1,BuildingKind.Mine,4,4); Build(s,1,BuildingKind.Smelter,6,5);
            Build(s,1,BuildingKind.Workshop,11,5); Build(s,1,BuildingKind.Housing,7,7);
            Build(s,2,BuildingKind.Farm,9,7); Build(s,2,BuildingKind.SolarPlant,10,5);
            s.Step(5000);
            s.Enqueue(new SimCommand {Id=s.NextCommandId(),PlanetId=1,Kind=CommandKind.PlaceRoad,X=9,Y=5}); s.Step();
            Build(s,1,BuildingKind.SolarPlant,12,5); Build(s,1,BuildingKind.Laboratory,7,5);
            Build(s,1,BuildingKind.Warehouse,14,5); Build(s,1,BuildingKind.Spaceport,13,5);
            var lab=s.FindPlanet(1).Buildings.Find(b=>b.Kind==BuildingKind.Laboratory);
            foreach(var technology in new[]{Technology.Transport,Technology.Colonization,Technology.CrystalMining})
            {
                s.Enqueue(new SimCommand {Id=s.NextCommandId(),PlanetId=1,Kind=CommandKind.StartResearch,TargetId=lab.Id,IntValue=(int)technology});
                Wait(s,()=>s.HasTechnology(1,technology));
            }
            var port=s.FindPlanet(1).Buildings.Find(b=>b.Kind==BuildingKind.Spaceport);
            foreach(var kind in new[]{ShipKind.Freighter,ShipKind.ColonyShip})
            {
                s.Enqueue(new SimCommand {Id=s.NextCommandId(),PlanetId=1,Kind=CommandKind.BuildShip,TargetId=port.Id,IntValue=(int)kind});
                Wait(s,()=>s.State.Ships.Exists(v=>v.Kind==kind));
            }
            s.State.Paused=true; s.State.LastMessage="M4 Demonstration: Wirtschaft, Forschung und beide Schiffe regulär bezahlt. Khepri besiedeln und Kristalle erschließen.";
            SaveCodec.Validate(s.State); return s;
        }
        public static void Build(GameSimulation s,int planet,BuildingKind kind,int x,int y)
        {
            Wait(s,()=>s.ValidatePlacement(planet,kind,x,y)==null);
            s.Enqueue(new SimCommand {Id=s.NextCommandId(),PlanetId=planet,Kind=CommandKind.PlaceBuilding,Building=kind,X=x,Y=y});
            Wait(s,()=>s.FindPlanet(planet).Buildings.Exists(b=>b.X==x && b.Y==y && b.Kind==kind && b.Built));
        }
        private static void Wait(GameSimulation s,Func<bool> done)
        { for(int i=0;i<60000 && !done();i++) s.Step(); if(!done()) throw new InvalidOperationException("M4 Vorbereitungsablauf blockiert: "+s.State.LastMessage); }
    }
}
