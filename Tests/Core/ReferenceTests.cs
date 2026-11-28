using System;
using System.IO;
using System.Collections.Generic;
using PlanetOne.Core;
using PlanetOne.Platform;

internal static class ReferenceTests
{
    public static void Register(Action<string,Action> run)
    {
        run("M8 F05 finite equal starts and two independent economic opponents",Start);
        run("M8 F05 paid economy transports research and gate finish",Economy);
        run("M8 F05 real partner delivery and shared coalition victory",CooperativeVictory);
    }
    private static bool prepared;
    private static readonly string Output=Path.Combine(Environment.CurrentDirectory,"TestResults","Reference");
    internal static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Start()
    {
        var s=GameSimulation.CreateReferenceScenario();
        Check(s.State.Planets.Count==5 && s.State.ComputerFactions.Count==2 && s.State.Planets.FindAll(p=>p.ParentId>=0&&p.OwnerId==0).Count==2,"Incorrect F05 world composition");
        foreach(var p in s.State.Planets)if(p.OwnerId!=0)
            Check(p.Buildings.Count==1 && p.Buildings[0].Inventory[0]==60 && p.Buildings[0].Inventory[2]==100 && p.Buildings[0].Inventory[3]==40,"Unequal or hidden starting funds");
        Check(s.State.Ships.Count==0 && !s.State.SpaceTestFixture && !s.State.InvasionTestFixture,"Reference uses debug grants");
        Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(GameSimulation.CreateReferenceScenario().State),"Seed is not repeatable");
        Directory.CreateDirectory(Output);SnapshotStore.Save(Path.Combine(Output,"start.json"),s.State);
    }
    internal static void Wait(GameSimulation s,Func<bool> condition,string stage,int limit=60000)
    {
        Console.WriteLine("  Waiting: "+stage+" tick="+s.State.Tick);
        for(int i=0;i<limit&&!condition();i++)
        {
            s.Step();
            if(s.State.Tick%40==0)ManagePlayerIndustry(s);
        }
        if(!condition())
        {
            Directory.CreateDirectory(Output);SnapshotStore.Save(Path.Combine(Output,"blocked.json"),s.State);
            throw new Exception(stage+" timed out at tick "+s.State.Tick+": "+s.State.LastMessage);
        }
    }
    private static void ManagePlayerIndustry(GameSimulation s)
    {
        // The scripted player uses ordinary operating controls just like the AI: a full
        // parts reserve must not endlessly consume the metal needed by a colony ship.
        foreach(var planet in s.State.Planets)
        {
            if(planet.OwnerId!=1)continue;
            foreach(var workshop in planet.Buildings)
            {
                if(workshop.Kind!=BuildingKind.Workshop || !workshop.Built || workshop.Deconstructing)continue;
                int parts=workshop.Inventory[3]-workshop.ReservedOutgoing[3];
                foreach(var storage in planet.Buildings)
                    if(storage.Built && !storage.Deconstructing && GameSimulation.IsStorage(storage))
                        parts+=storage.Inventory[3]-storage.ReservedOutgoing[3];
                // Keep a working reserve, not a second idle shipload of parts while
                // colony ships and defensive construction are waiting for the same metal.
                bool enabled=workshop.Enabled?parts<24:parts<12;
                if(enabled!=workshop.Enabled)
                    s.Enqueue(new SimCommand {Id=s.NextCommandId(),IssuerId=1,PlanetId=planet.Id,
                        Kind=CommandKind.SetBuildingEnabled,TargetId=workshop.Id,BoolValue=enabled});
            }
            // Full ore stores do not need permanently staffed extraction.
            // Use the same operating controls a player has, with a genuine local buffer.
            foreach(var producer in planet.Buildings)
            {
                if(!producer.Built||producer.Deconstructing||producer.Kind!=BuildingKind.Mine)continue;
                int resource=1,stock=0;
                foreach(var building in planet.Buildings)stock+=building.Inventory[resource];
                foreach(var shipment in planet.Shipments)if((int)shipment.Resource==resource&&shipment.Phase!=ShipmentPhase.Loading)stock+=shipment.Amount;
                int maximum=32;
                bool enabled=producer.Enabled?stock<maximum:stock<maximum/2;
                if(enabled!=producer.Enabled)s.Enqueue(new SimCommand{Id=s.NextCommandId(),IssuerId=1,PlanetId=planet.Id,Kind=CommandKind.SetBuildingEnabled,TargetId=producer.Id,BoolValue=enabled});
            }
        }
    }
    internal static void Command(GameSimulation s,SimCommand c)
    {
        c.Id=s.NextCommandId();Check(s.Enqueue(c),"Command enqueue "+c.Kind);s.Step();
        var result=s.State.CommandResults.Find(r=>r.CommandId==c.Id);
        Check(result!=null&&result.Success,c.Kind+": "+(result==null?"No result":result.Message));
    }
    internal static BuildingState Build(GameSimulation s,int planet,BuildingKind kind,int x,int y)
    {
        Wait(s,()=>s.ValidatePlacement(planet,kind,x,y)==null,"Placement "+kind);
        Command(s,new SimCommand {PlanetId=planet,Kind=CommandKind.PlaceBuilding,Building=kind,X=x,Y=y});
        Wait(s,()=>s.FindPlanet(planet).Buildings.Exists(b=>b.Kind==kind&&b.X==x&&b.Y==y&&b.Built),"Build "+kind);
        return s.FindPlanet(planet).Buildings.Find(b=>b.Kind==kind&&b.X==x&&b.Y==y);
    }
    private static void Research(GameSimulation s,BuildingState lab,Technology technology)
    {
        if(!lab.Enabled)Command(s,new SimCommand{PlanetId=lab.PlanetId,Kind=CommandKind.SetBuildingEnabled,TargetId=lab.Id,BoolValue=true});
        Command(s,new SimCommand {PlanetId=lab.PlanetId,Kind=CommandKind.StartResearch,TargetId=lab.Id,IntValue=(int)technology});
        Wait(s,()=>s.HasTechnology(1,technology),"Research "+technology);
        // Idle laboratories otherwise keep workers away from the finite metal economy.
        Command(s,new SimCommand{PlanetId=lab.PlanetId,Kind=CommandKind.SetBuildingEnabled,TargetId=lab.Id,BoolValue=false});
    }
    internal static ShipState Ship(GameSimulation s,BuildingState port,ShipKind kind)
    {
        long before=s.State.NextEntityId;
        Command(s,new SimCommand {PlanetId=port.PlanetId,Kind=CommandKind.BuildShip,TargetId=port.Id,IntValue=(int)kind});
        Wait(s,()=>s.State.Ships.Exists(v=>v.Id>=before&&v.OwnerId==1&&v.Kind==kind),"Ship "+kind);
        return s.State.Ships.Find(v=>v.Id>=before&&v.OwnerId==1&&v.Kind==kind);
    }
    internal static void Policy(GameSimulation s,BuildingState port,int resource,int target,int minimum=0)
    {Command(s,new SimCommand {PlanetId=port.PlanetId,Kind=CommandKind.SetSupplyPolicy,TargetId=port.Id,IntValue=resource,Minimum=minimum,Amount=target,Reserve=0,X=1});}
    internal static void ContinueExactly(GameSimulation s,string stage,int ticks=100)
    {
        string path=Path.Combine(Output,stage+".json");SnapshotStore.Save(path,s.State);
        var restored=new GameSimulation(SnapshotStore.Load(path));
        Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(restored.State),stage+" changed on disk");
        s.Step(ticks);restored.Step(ticks);
        Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(restored.State),stage+" diverged after continuation");
        Console.WriteLine("  Snapshot "+stage+" tick="+s.State.Tick+" matched "+ticks+" subsequent ticks");
    }
    private static void Economy()
    {
        Directory.CreateDirectory(Output);prepared=false;var s=GameSimulation.CreateReferenceScenario();
        // Shorter connections leave the four real couriers available for productive work.
        foreach(int x in new[]{5,6,7})Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.PlaceRoad,X=x,Y=4});
        Build(s,1,BuildingKind.SolarPlant,10,5);Build(s,1,BuildingKind.Farm,9,7);
        Build(s,1,BuildingKind.Mine,4,4);Build(s,1,BuildingKind.Smelter,6,5);
        Build(s,1,BuildingKind.Workshop,11,5);Build(s,1,BuildingKind.Housing,7,7);
        Build(s,1,BuildingKind.SolarPlant,12,7);Build(s,1,BuildingKind.Smelter,5,5);
        // Claim the contested moon before optional industrial expansion. The nearer port
        // and real operating priorities reduce courier trips without adding capacity.
        var lab=Build(s,1,BuildingKind.Laboratory,7,5);
        var port=Build(s,1,BuildingKind.Spaceport,9,5);
        Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.SetBuildingPriority,TargetId=lab.Id,IntValue=2});
        foreach(var tech in new[]{Technology.Transport,Technology.Colonization})Research(s,lab,tech);
        Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.SetBuildingPriority,TargetId=port.Id,IntValue=2});
        var colony=Ship(s,port,ShipKind.ColonyShip);
        Command(s,new SimCommand {Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms {Kind=TreatyKind.Access,Members=new List<int>{1,2},Rights=AccessRight.CivilTransit,DurationTicks=1200000}});
        var access=s.State.Treaties[s.State.Treaties.Count-1];Wait(s,()=>access.Phase==TreatyPhase.Active,"Civil transit agreement");
        Command(s,new SimCommand {Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms {Kind=TreatyKind.Access,Members=new List<int>{1,3},Rights=AccessRight.CivilTransit,DurationTicks=1200000}});
        var secondAccess=s.State.Treaties[s.State.Treaties.Count-1];Wait(s,()=>secondAccess.Phase==TreatyPhase.Active,"Transit through inner system");
        SnapshotStore.Save(Path.Combine(Output,"colony-ready.json"),s.State);
        var point=s.GetOrbitPosition(3);
        Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.MoveShipPosition,ActorId=colony.Id,X=(int)Math.Round(point.X),Y=(int)Math.Round(point.Y)});
        Wait(s,()=>s.IsPlanetKnown(1,3),"Scout moon");
        Command(s,new SimCommand {PlanetId=3,Kind=CommandKind.FlyShip,ActorId=colony.Id});
        Wait(s,()=>{var p=s.GetOrbitPosition(3);return Math.Abs(colony.X-p.X)+Math.Abs(colony.Y-p.Y)<.2;},"Colony arrival");
        Check(s.FindPlanet(3).OwnerId==0,"Moon already settled at tick "+s.State.Tick);
        Command(s,new SimCommand {PlanetId=3,Kind=CommandKind.Colonize,ActorId=colony.Id});
        Build(s,3,BuildingKind.Farm,9,7);Build(s,3,BuildingKind.SolarPlant,10,5);
        for(int y=7;y<=8;y++)Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.PlaceRoad,X=13,Y=y});
        Build(s,1,BuildingKind.Mine,13,9);Build(s,1,BuildingKind.Housing,5,7);
        Build(s,1,BuildingKind.SolarPlant,11,7);Build(s,1,BuildingKind.SolarPlant,12,5);
        Build(s,1,BuildingKind.Smelter,6,7);Build(s,1,BuildingKind.Warehouse,14,5);
        Research(s,lab,Technology.CrystalMining);
        var freight=Ship(s,port,ShipKind.Freighter);
        var source=Build(s,3,BuildingKind.Spaceport,11,5);
        for(int y=7;y<=8;y++)Command(s,new SimCommand {PlanetId=3,Kind=CommandKind.PlaceRoad,X=13,Y=y});
        Build(s,3,BuildingKind.CrystalMine,13,9);Policy(s,source,4,32);Policy(s,port,4,24,12);
        Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.SetAutoSupply,ActorId=freight.Id,BoolValue=true});
        Wait(s,()=>freight.Freight!=null&&freight.Freight.Phase==FreightPhase.Loading,"Automatic loading");ContinueExactly(s,"freight-loading");
        Wait(s,()=>freight.Freight!=null&&freight.Freight.Phase==FreightPhase.Deliver,"Automatic transit");ContinueExactly(s,"freight-transit");
        Wait(s,()=>port.Inventory[4]>=12,"Automatic crystal delivery");
        Research(s,lab,Technology.Electronics);Build(s,1,BuildingKind.ElectronicsFactory,15,5);
        Research(s,lab,Technology.Sensors);
        SnapshotStore.Save(Path.Combine(Output,"military-ready.json"),s.State);prepared=true;
        BuildingState gate=null;
        ReferenceCombat.Defend(s,()=>gate=PrepareGate(s,false));
        Wait(s,()=>gate.Operational&&Enough(gate.Inventory,Definitions.GateComponents),"Gate material delivery");
        Check(s.State.ComputerFactions.Count==2 && s.FindPlanet(2).ProducedTotals[2]>0 && s.FindPlanet(4).ProducedTotals[2]>0,"Opponents did not sustain independent economies");
        Check(s.State.Treaties.Exists(t=>t.Terms.Members.Contains(2)&&t.Terms.Members.Contains(3)&&t.StartTick>0),"No AI to AI agreement in the recorded history");
        SnapshotStore.Save(Path.Combine(Output,"gate-ready.json"),s.State);
        Command(s,new SimCommand {Kind=CommandKind.ActivateGate,TargetId=gate.Id});ContinueExactly(s,"gate-activation",300);
        Wait(s,()=>s.State.Winners.Contains(1),"Gate victory",2000);
        Check(s.State.Winners.Count==1,"Unexpected coalition victory");
        SnapshotStore.Save(Path.Combine(Output,"victory.json"),s.State);
        VerifyIndustrialLedger(s);
        Console.WriteLine("  F05 seed="+s.State.Seed+" victory tick="+s.State.Tick+" player metal="+s.FindPlanet(1).ProducedTotals[2]+" parts="+s.FindPlanet(1).ProducedTotals[3]);
    }
    private static BuildingState PrepareGate(GameSimulation s,bool waitForMaterials=true)
    {
        var port=s.FindPlanet(1).Buildings.Find(b=>b.Kind==BuildingKind.Spaceport);
        Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.SetBuildingPriority,TargetId=port.Id,IntValue=0});
        foreach(int r in new[]{0,2,3})Policy(s,port,r,0);
        var electronics=s.FindPlanet(1).Buildings.Find(b=>b.Kind==BuildingKind.ElectronicsFactory);
        Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.SetBuildingEnabled,TargetId=electronics.Id,BoolValue=false});
        Build(s,1,BuildingKind.SolarPlant,10,7);Build(s,1,BuildingKind.SolarPlant,9,4);
        Build(s,1,s.FindPlanet(1).HousingCapacity<52?BuildingKind.Housing:BuildingKind.Farm,3,7);
        var gate=Build(s,1,BuildingKind.JumpGate,14,7);
        Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.SetBuildingEnabled,TargetId=electronics.Id,BoolValue=true});
        if(waitForMaterials)Wait(s,()=>gate.Operational&&Enough(gate.Inventory,Definitions.GateComponents),"Gate material delivery");
        return gate;
    }
    private static bool Enough(int[] actual,int[] needed){for(int r=0;r<6;r++)if(actual[r]<needed[r])return false;return true;}
    private static void CooperativeVictory()
    {
        Check(prepared,"Current run did not produce a valid cooperative baseline");
        var s=new GameSimulation(SnapshotStore.Load(Path.Combine(Output,"military-ready.json")));
        Command(s,new SimCommand {Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms {Kind=TreatyKind.Coalition,Members=new List<int>{1,3},DurationTicks=1200000}});
        var coalition=s.State.Treaties[s.State.Treaties.Count-1];Wait(s,()=>coalition.Phase==TreatyPhase.Active,"AI coalition approval",1000);
        var source=s.FindPlanet(1).Buildings.Find(b=>b.Kind==BuildingKind.Spaceport);
        var target=s.FindPlanet(4).Buildings.Find(b=>b.Kind==BuildingKind.Spaceport);
        var freight=s.State.Ships.Find(v=>v.OwnerId==1&&v.Kind==ShipKind.Freighter&&!v.Destroyed);
        Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.SetAutoSupply,ActorId=freight.Id,BoolValue=false});
        Wait(s,()=>freight.Freight==null&&Array.TrueForAll(freight.Cargo,n=>n==0),"Finish existing cargo");
        Policy(s,source,2,16);Wait(s,()=>source.Inventory[2]-source.ReservedOutgoing[2]>=8 && target.Inventory[3]-target.ReservedOutgoing[3]>=2,"Both real trade contributions");
        Command(s,new SimCommand {Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms {Kind=TreatyKind.Trade,Members=new List<int>{1,3},SourcePort=source.Id,TargetPort=target.Id,Goods=ResourceKind.Metal,Amount=8,Payment=ResourceKind.Parts,PaymentAmount=2,DurationTicks=1200000}});
        var trade=s.State.Treaties[s.State.Treaties.Count-1];Wait(s,()=>trade.Phase==TreatyPhase.Active,"AI trade approval",1000);
        Command(s,new SimCommand {Kind=CommandKind.DispatchTrade,TargetId=trade.Id,ActorId=freight.Id});
        Wait(s,()=>trade.Delivered==8&&trade.PaymentDelivered==2,"Both partners physically deliver",24000);
        Check(s.Trust(1,3)>0,"Partner commitment did not complete");
        Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.SetAutoSupply,ActorId=freight.Id,BoolValue=true});
        SnapshotStore.Save(Path.Combine(Output,"cooperative-delivery.json"),s.State);
        var gate=PrepareGate(s);
        Command(s,new SimCommand {Kind=CommandKind.ActivateGate,TargetId=gate.Id});ContinueExactly(s,"cooperative-activation",300);
        Wait(s,()=>s.State.Winners.Contains(1)&&s.State.Winners.Contains(3),"Shared gate victory",2000);
        Check(s.State.Winners.Count==2&&!s.State.Winners.Contains(2),"Non-member received victory");
        SnapshotStore.Save(Path.Combine(Output,"cooperative-victory.json"),s.State);
        VerifyIndustrialLedger(s);
        Console.WriteLine("  F05 coalition 1+3 victory tick="+s.State.Tick+" traded="+trade.Delivered+"/"+trade.PaymentDelivered);
    }
    internal static void VerifyIndustrialLedger(GameSimulation s)
    {
        SaveCodec.Validate(s.State);
        for(int r=1;r<6;r++)
        {
            long total=s.State.SpaceLostTotals[r]-(r==1?s.State.SpaceMinedOre:0);
            foreach(var p in s.State.Planets)
            {
                total+=p.ConsumedTotals[r]+p.LostTotals[r]-p.ProducedTotals[r]-p.RecoveredTotals[r];
                foreach(var b in p.Buildings)total+=b.Inventory[r];
                foreach(var shipment in p.Shipments)if((int)shipment.Resource==r&&shipment.Phase!=ShipmentPhase.Loading)total+=shipment.Amount;
                if(r==3)foreach(var u in p.Units)total+=u.Parts;
            }
            foreach(var ship in s.State.Ships){total+=ship.Cargo[r];if(r==3)foreach(var u in ship.Troops)total+=u.Parts;}
            if(r==3)foreach(var h in s.State.Beachheads)total+=h.Parts;
            long initial=r==2?300:r==3?120:0;
            Check(total==initial,"Global "+(ResourceKind)r+" ledger differs: "+total+" expected "+initial);
        }
        Console.WriteLine("  All five industrial resource ledgers match finite initial stocks plus actual production, consumption, recovery and loss");
    }
}
