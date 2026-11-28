using System;

namespace PlanetOne.Core
{
    // Explicitly finite debug scenario. Never used by CreateDefault or normal production.
    public static class InvasionFixture
    {
        public static GameSimulation Create()
        {
            var s=GameSimulation.CreateDefault(); s.State.InvasionTestFixture=true;
            s.State.Relations[0].Relation=RelationKind.War; s.FindPlanet(2).OwnerId=2;
            foreach(var f in s.State.Factions) foreach(Technology t in Enum.GetValues(typeof(Technology))) f.Research.Completed.Add(t);
            foreach(var p in s.State.Planets)
            {
                if(p.OwnerId==0) continue;
                Array.Clear(p.Terrain,0,p.Terrain.Length); Array.Clear(p.OreRemaining,0,p.OreRemaining.Length);
                AddBuilding(s,p.Id,BuildingKind.Spaceport,9,5);
                AddBuilding(s,p.Id,BuildingKind.SolarPlant,7,5);
                AddBuilding(s,p.Id,BuildingKind.Farm,7,7);
                var port=p.Buildings.Find(b=>b.Kind==BuildingKind.Spaceport);
                port.Inventory=new[]{40,0,60,30,0,0}; port.Supply.Target=new[]{24,0,32,24,0,0};
            }
            AddBuilding(s,2,BuildingKind.FlakTower,13,7);
            AddBuilding(s,2,BuildingKind.GunTower,11,7);
            AddBuilding(s,2,BuildingKind.Wall,10,6);
            AddBuilding(s,2,BuildingKind.Wall,10,7);
            AddBuilding(s,2,BuildingKind.MilitaryDepot,12,5);
            AddUnit(s,2,2,UnitKind.Infantry,14,10);
            AddUnit(s,2,2,UnitKind.Tank,14,11);
            AddShip(s,1,1,ShipKind.OrbitalYard);
            AddShip(s,1,1,ShipKind.EscortFrigate);
            AddShip(s,1,1,ShipKind.TorpedoShip);
            var carrier=AddShip(s,1,1,ShipKind.LandingTransport);
            carrier.Cargo[0]=12;carrier.Cargo[3]=12;
            s.Step();
            foreach(var kind in new[]{UnitKind.Infantry,UnitKind.Infantry,UnitKind.Engineer,UnitKind.Artillery})
            {
                var u=AddUnit(s,1,1,kind,9,4);
                if(kind==UnitKind.Engineer) u.Parts=8;
                s.Enqueue(new SimCommand {Id=s.NextCommandId(),IssuerId=1,PlanetId=1,Kind=CommandKind.LoadTroop,ActorId=carrier.Id,TargetId=u.Id});s.Step();
            }
            AddShip(s,2,2,ShipKind.Corvette);
            var target=s.GetOrbitPosition(2);
            s.State.SpaceSightings.RemoveAll(v=>v.FactionId==1 && v.PlanetId==2);
            s.State.SpaceSightings.Add(new SpaceSighting {FactionId=1,PlanetId=2,KnownOwnerId=2,X=target.X,Y=target.Y,Tick=s.State.Tick});
            s.State.Paused=true;
            s.State.LastMessage="M5 PRÜFKARTE: endliche Testflotten, Truppen und Vorräte. Anflug → Abstieg bei 4/9 → Entladen → Landekopf → Verwaltung sichern. Keine Fraktions-KI.";
            return s;
        }
        public static BuildingState AddBuilding(GameSimulation s,int planet,BuildingKind kind,int x,int y)
        {
            var p=s.FindPlanet(planet); p.Terrain[y*p.Width+x]=0;p.Roads[y*p.Width+x]=false;
            var b=new BuildingState {Id=s.State.NextEntityId++,PlanetId=planet,Kind=kind,X=x,Y=y,Built=true,Health=Definitions.BuildingHealth(kind)};
            p.Buildings.Add(b);return b;
        }
        public static UnitState AddUnit(GameSimulation s,int planet,int owner,UnitKind kind,int x,int y)
        {
            var d=Definitions.Get(kind);var u=new UnitState {Id=s.State.NextEntityId++,OwnerId=owner,Kind=kind,X=x,Y=y,GoalX=x,GoalY=y,Health=d.Health,Ammunition=d.Ammo,Food=d.Food};
            s.FindPlanet(planet).Units.Add(u);return u;
        }
        public static ShipState AddShip(GameSimulation s,int planet,int owner,ShipKind kind)
        {
            var anchor=s.GetOrbitPosition(planet);SimPoint pos;
            if(!s.TrySpaceSpawn(kind,owner,anchor,out pos))throw new InvalidOperationException("Prüfkarte: Orbitalbereich besitzt keinen freien Schiffstandort.");
            var d=Definitions.Ships[(int)kind];var w=Definitions.SpaceWeapons[(int)kind];
            var ship=new ShipState {Id=s.State.NextEntityId++,OwnerId=owner,Kind=kind,X=pos.X,Y=pos.Y,TargetPlanetId=planet,Order=ShipOrder.Guard,Crew=d.Crew,Health=w.Health,Ammunition=w.Ammo,
                TrafficGoalSet=true,TrafficAnchorX=anchor.X,TrafficAnchorY=anchor.Y,TrafficOffsetX=pos.X-anchor.X,TrafficOffsetY=pos.Y-anchor.Y};
            s.State.Ships.Add(ship);return ship;
        }
    }
}
