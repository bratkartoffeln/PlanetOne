using System;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        public SimPoint GetAsteroidPosition(AsteroidState asteroid) {return AsteroidGeneration.Position(asteroid,State.Tick*Definitions.TickSeconds);}
        public bool IsAsteroidVisible(int faction,AsteroidState asteroid) {return SpacePointVisible(faction,GetAsteroidPosition(asteroid));}
        private void ApplyMining(SimCommand command)
        {
            var ship=State.Ships.Find(s=>s.Id==command.ActorId && s.OwnerId==command.IssuerId && !s.Destroyed && !s.Consumed);
            var asteroid=State.Asteroids.Find(a=>a.Id==command.TargetId);
            var planet=FindPlanet(command.PlanetId);
            var port=planet==null?null:planet.Buildings.Find(b=>PortOwned(b,command.IssuerId) && (command.DestinationId==0 || b.Id==command.DestinationId));
            string error=null;
            if(ship==null || ship.Kind!=ShipKind.Freighter || ship.TransportPhase!=TransportPhase.Space || ship.Freight!=null || ship.Mining!=null || Total(ship.Cargo)>0 || ShipPledged(ship.Id))error="Freier eigener Frachter benötigt.";
            else if(!HasTechnology(command.IssuerId,Technology.Transport))error="Raumtransport mit Bergbaudrohnen muss erforscht sein.";
            else if(asteroid==null || !IsAsteroidVisible(command.IssuerId,asteroid))error="Asteroid muss zunächst mit einem Schiff erkundet werden.";
            else if(port==null)error="Eigener Rücknahmehafen benötigt.";
            else if(command.Amount<1 || command.Amount>Definitions.Ships[0].Capacity)error="Ungültige Abbaumenge.";
            else
            {
                int amount=Math.Min(command.Amount,Math.Min(asteroid.RemainingOre-asteroid.ReservedOre,FreeSpace(port)));
                if(amount==0)error="Vorkommen erschöpft, reserviert oder Hafenlager voll.";
                else
                {
                    asteroid.ReservedOre+=amount;port.ReservedIncoming[(int)ResourceKind.Ore]+=amount;
                    ship.Mining=new MiningJob {AsteroidId=asteroid.Id,PortId=port.Id,Amount=amount};
                    ResetSpaceDestination(ship);ship.Order=ShipOrder.Mine;ship.AutoSupply=false;
                    foreach(var route in State.Routes)if(route.ShipId==ship.Id)route.Enabled=false;
                    ship.Status="Flug zum Asteroiden";
                }
            }
            Report(command,error==null,error==null?"MiningAccepted":"InvalidMining",error??"Abbau und Rücktransport beauftragt.");
        }
        private void TickMining(ShipState ship)
        {
            var job=ship.Mining;var asteroid=State.Asteroids.Find(a=>a.Id==job.AsteroidId);var port=FindBuilding(job.PortId);
            if(!PortOwned(port,ship.OwnerId)) {CancelFreight(ship);ship.Status="Abbau beendet: Rücknahmehafen verloren";return;}
            ship.Status="Anflug: Asteroid "+asteroid.Id;
            if(!MoveShip(ship,GetAsteroidPosition(asteroid)))return;
            ship.Status="Bergbaudrohnen: "+job.Extracted+" / "+job.Amount+" Erz";
            job.Progress+=Definitions.TickSeconds;
            if(job.Progress+1e-8<AsteroidGeneration.SecondsPerOre)return;
            job.Progress-=AsteroidGeneration.SecondsPerOre;job.Extracted++;
            asteroid.RemainingOre--;asteroid.ReservedOre--;ship.Cargo[(int)ResourceKind.Ore]++;State.SpaceMinedOre++;
            if(job.Extracted<job.Amount)return;
            ship.Freight=new FreightJob {TargetId=port.Id,Resource=ResourceKind.Ore,Amount=job.Amount,Phase=FreightPhase.Deliver};
            ResetSpaceDestination(ship);ship.Mining=null;ship.Order=ShipOrder.Freight;ship.Status="Erz wird zum Hafen gebracht";
        }
        private void CancelMining(ShipState ship)
        {
            if(ship.Mining==null)return;
            var job=ship.Mining;var asteroid=State.Asteroids.Find(a=>a.Id==job.AsteroidId);var port=FindBuilding(job.PortId);
            asteroid.ReservedOre-=job.Amount-job.Extracted;
            if(port!=null)port.ReservedIncoming[(int)ResourceKind.Ore]-=job.Amount;
            ship.Mining=null;
        }
    }
}
