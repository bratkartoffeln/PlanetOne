using System;
using System.IO;
using System.Runtime.Serialization;

namespace PlanetOne.Core
{
    public sealed partial class GameSimulation
    {
        private static T CopyAiValue<T>(T value)
        {
            var serializer=new DataContractSerializer(typeof(T));
            using(var stream=new MemoryStream()) {serializer.WriteObject(stream,value);stream.Position=0;return (T)serializer.ReadObject(stream);}
        }

        public AiObservation ObserveForAi(int faction)
        {
            var f=State.Factions.Find(v=>v.Id==faction);
            if(f==null) throw new ArgumentException("Unbekannte KI-Fraktion");
            var view=new AiObservation {FactionId=faction,Seed=State.Seed,Tick=State.Tick,Research=CopyAiValue(f.Research)};
            foreach(var other in State.Factions) if(AtWar(faction,other.Id)) view.Enemies.Add(other.Id);
            foreach(var p in State.Planets)
            {
                var last=State.SpaceSightings.Find(v=>v.FactionId==faction && v.PlanetId==p.Id);
                bool own=p.OwnerId==faction, current=IsPlanetVisible(faction,p.Id);
                if(!own && last==null && !current) continue;
                var pos=current?GetOrbitPosition(p.Id):new SimPoint(last.X,last.Y);
                var w=new AiWorld {Id=p.Id,OwnerId=current?p.OwnerId:last.KnownOwnerId,Current=current,X=pos.X,Y=pos.Y,
                    Width=p.Width,Height=p.Height,Terrain=new int[p.Terrain.Length],Ore=new int[p.Terrain.Length],
                    Roads=new bool[p.Terrain.Length],Visible=new bool[p.Terrain.Length]};
                // Geography is public once a world is charted. Depletion and infrastructure are not.
                Array.Copy(p.Terrain,w.Terrain,p.Terrain.Length);
                if(own)
                {
                    w.Population=p.Population;w.Housing=p.HousingCapacity;w.Workers=p.WorkersTotal;w.WorkersUsed=p.WorkersUsed;
                    w.Energy=p.EnergyProduced;w.Shipments=p.Shipments.Count;w.PendingDeliveries=p.Deliveries.Count;Array.Copy(p.Roads,w.Roads,p.Roads.Length);Array.Copy(p.OreRemaining,w.Ore,p.OreRemaining.Length);
                }
                for(int y=0;y<p.Height;y++) for(int x=0;x<p.Width;x++) w.Visible[y*p.Width+x]=IsCellVisible(p,faction,x,y);
                foreach(var b in p.Buildings)
                {
                    int owner=BuildingOwner(p,b);if(b.Destroyed) continue;
                    if(owner==faction)
                    {
                        var copy=CopyAiValue(b);copy.OwnerId=owner;w.Buildings.Add(copy);
                        if(copy.Enabled && !copy.Deconstructing) w.EnergyDemand+=Definitions.Get(copy.Kind).EnergyUse;
                    }
                    else if(w.Visible[b.Y*p.Width+b.X]) w.Buildings.Add(new BuildingState {Id=b.Id,PlanetId=p.Id,OwnerId=owner,Kind=b.Kind,X=b.X,Y=b.Y,Built=b.Built,Health=b.Health,CapturingFaction=b.CapturingFaction==faction?faction:0});
                }
                foreach(var u in p.Units)
                    if(!u.Destroyed && (u.OwnerId==faction || IsCellVisible(p,faction,u.X,u.Y)))
                        w.Units.Add(u.OwnerId==faction?CopyAiValue(u):new UnitState {Id=u.Id,OwnerId=u.OwnerId,Kind=u.Kind,X=u.X,Y=u.Y,Health=u.Health,Layer=u.Layer});
                foreach(var h in State.Beachheads) if(!h.Destroyed && h.PlanetId==p.Id && h.OwnerId==faction) w.Beachheads.Add(CopyAiValue(h));
                view.Worlds.Add(w);
            }
            foreach(var s in State.Ships)
                if(!s.Destroyed && !s.Consumed && (s.OwnerId==faction || IsShipVisible(faction,s)))
                    view.Ships.Add(s.OwnerId==faction?CopyAiValue(s):new ShipState {Id=s.Id,OwnerId=s.OwnerId,Kind=s.Kind,X=s.X,Y=s.Y,Health=s.Health});
            return view;
        }

        private void TickAi()
        {
            foreach(var ai in State.ComputerFactions)
            {
                if(State.Tick<ai.NextPlanTick) continue;
                ai.NextPlanTick=State.Tick+Definitions.AiPlanTicks;
                var previous=ai.Decisions.Count==0?null:ai.Decisions[ai.Decisions.Count-1];
                if(previous!=null&&previous.Action==CommandKind.LandTransport.ToString())
                {
                    var result=State.CommandResults.Find(r=>r.CommandId==previous.CommandId);
                    if(result!=null&&!result.Success&&!ai.Operation.RejectedLandings.Exists(c=>c.X==ai.Operation.LandingX&&c.Y==ai.Operation.LandingY))
                        ai.Operation.RejectedLandings.Add(new GridCell(ai.Operation.LandingX,ai.Operation.LandingY));
                }
                var view=ObserveForAi(ai.FactionId);var intent=AiPlanner.Plan(view,ai);
                long command=0;
                if(intent.Command!=null)
                {intent.Command.Id=command=NextCommandId();intent.Command.IssuerId=ai.FactionId;Enqueue(intent.Command);}
                ai.Decisions.Add(new AiDecision {Tick=State.Tick,CommandId=command,Goal=intent.Goal,Reason=intent.Reason,Alternative=intent.Alternative,Action=intent.Command==null?"Warten":intent.Command.Kind.ToString()});
                if(ai.Decisions.Count>Definitions.AiDecisionLimit) ai.Decisions.RemoveAt(0);
            }
        }

        public static GameSimulation CreateAiScenario(int seed=12345)
        {
            var sim=CreateDefault(seed);sim.FindPlanet(2).OwnerId=2;
            sim.State.ComputerFactions.Add(new AiState {FactionId=2,Profile=AiProfile.Expansion});
            sim.State.AiScenario=true;sim.State.Relations[0].Relation=RelationKind.War;
            sim.State.SpaceSightings.Clear();sim.UpdateSpaceKnowledge();
            sim.State.LastMessage="M6: Nereid wird von einer eigenständigen KI aufgebaut. Gleiche Startbestände, festgelegter Szenariokrieg; keine geschenkten Flotten.";
            return sim;
        }
    }
}
