using System;
namespace PlanetOne.Core
{
    /// <summary>Explicit visual test fixture; its starting colonies and stock are never injected into a normal game.</summary>
    public static class PlanetEnvironmentFixture
    {
        public static GameSimulation Create()
        {
            var game=GameSimulation.CreateDefault(251126);game.State.SpaceTestFixture=true;
            foreach(var p in game.State.Planets)
            {
                if(p.OwnerId==0) {
                    p.OwnerId=1;p.Population=16;p.WorkersTotal=12;p.HousingCapacity=16;
                    var hq=new BuildingState {Id=game.State.NextEntityId++,PlanetId=p.Id,Kind=BuildingKind.Headquarters,X=8,Y=6,Built=true,Health=Definitions.BuildingHealth(BuildingKind.Headquarters),Status="Prüfkarten-Startbestand"};
                    hq.Inventory[0]=60;hq.Inventory[2]=100;hq.Inventory[3]=40;p.Buildings.Add(hq);
                    if(!PlanetEnvironment.Airborne(p)) {
                        for(int x=2;x<=15;x++)if(p.Terrain[6*p.Width+x]==0 && x!=8)p.Roads[6*p.Width+x]=true;
                        for(int y=2;y<=11;y++)if(p.Terrain[y*p.Width+8]==0 && y!=6)p.Roads[y*p.Width+8]=true;
                    }
                }
                game.Enqueue(new SimCommand {Id=game.NextCommandId(),PlanetId=p.Id,Kind=CommandKind.PlaceBuilding,Building=BuildingKind.SolarPlant,X=10,Y=5});
                game.Enqueue(new SimCommand {Id=game.NextCommandId(),PlanetId=p.Id,Kind=CommandKind.PlaceBuilding,Building=BuildingKind.Farm,X=9,Y=7});
            }
            game.Step(800);game.State.Paused=true;
            game.State.LastMessage="PLANETENTYPEN · Prüfkarte mit gesetzten Kolonien und Startvorräten. Gebäude und Transporte werden regulär bezahlt. Keine normale Partie.";
            SaveCodec.Validate(game.State);return game;
        }
    }
}
