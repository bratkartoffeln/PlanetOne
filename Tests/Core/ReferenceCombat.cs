using System;
using System.IO;
using PlanetOne.Core;
using PlanetOne.Platform;

internal static partial class ReferenceCombat
{
    public static void Run()
    {
        var s=new GameSimulation(SnapshotStore.Load(Path.Combine(Environment.CurrentDirectory,"TestResults","Reference","military-ready.json")));
        Defend(s);
    }
    public static void Defend(GameSimulation s,Action prepareCivilEconomy=null)
    {
        ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.DeclareWar,IntValue=3});
        var electronics=s.FindPlanet(1).Buildings.Find(b=>b.Kind==BuildingKind.ElectronicsFactory);
        ReferenceTests.Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.SetBuildingEnabled,TargetId=electronics.Id,BoolValue=false});
        // The command centre is an endpoint, not a road through the middle of the city.
        foreach(int x in new[]{5,6,7})
            if(!s.FindPlanet(1).Roads[4*s.FindPlanet(1).Width+x])
                ReferenceTests.Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.PlaceRoad,X=x,Y=4});
        ReferenceTests.Build(s,1,BuildingKind.SolarPlant,3,5);
        ReferenceTests.Build(s,1,BuildingKind.MilitaryDepot,2,5);
        ReferenceTests.Build(s,1,BuildingKind.GunTower,4,7);
        ReferenceTests.Build(s,1,BuildingKind.ArtilleryTower,2,7);
        ReferenceTests.Build(s,1,BuildingKind.Housing,15,7);
        var port=s.FindPlanet(1).Buildings.Find(b=>b.Kind==BuildingKind.Spaceport);
        ReferenceTests.Policy(s,port,0,40,20);ReferenceTests.Policy(s,port,2,100,50);ReferenceTests.Policy(s,port,3,40,20);
        var yard=ReferenceTests.Ship(s,port,ShipKind.OrbitalYard);
        long before=s.State.NextEntityId;
        ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.BuildWarship,ActorId=yard.Id,PlanetId=1,IntValue=(int)ShipKind.EscortFrigate});
        ReferenceTests.Wait(s,()=>s.State.Ships.Exists(v=>v.OwnerId==1&&v.Id>=before&&v.Kind==ShipKind.EscortFrigate),"Paid home escort");
        var escort=s.State.Ships.Find(v=>v.OwnerId==1&&v.Id>=before&&v.Kind==ShipKind.EscortFrigate);
        // Keep investing through ordinary commands while the paid defence is on duty.
        // Waiting for the opponent's whole campaign before starting the gate loses
        // the economic race to the independent third faction.
        if(prepareCivilEconomy!=null)prepareCivilEconomy();
        bool fought=false,landed=false;long transport=0,recoveryAfter=0;int target=0;
        for(int i=0;i<120000&&!landed;i++)
        {
            s.Step();
            // Replace a lost civilian hull while the defences still protect the port,
            // rather than leave the shipyard idle until every treaty is negotiated.
            if(recoveryAfter==0&&s.State.Ships.Exists(v=>v.OwnerId==1&&v.Kind==ShipKind.Freighter&&v.Destroyed))
            {
                recoveryAfter=s.State.NextEntityId;
                ReferenceTests.Command(s,new SimCommand{PlanetId=1,Kind=CommandKind.SetBuildingPriority,TargetId=port.Id,IntValue=2});
                ReferenceTests.Command(s,new SimCommand{PlanetId=1,Kind=CommandKind.BuildShip,TargetId=port.Id,IntValue=(int)ShipKind.Freighter});
            }
            fought|=escort.Health<Definitions.SpaceWeapons[(int)escort.Kind].Health||escort.Ammunition<Definitions.SpaceWeapons[(int)escort.Kind].Ammo;
            foreach(var ship in s.State.Ships)if(ship.OwnerId==3&&ship.Kind==ShipKind.LandingTransport&&ship.TransportPhase!=TransportPhase.Space)
            {transport=ship.Id;target=ship.TargetPlanetId;if(ship.TransportPhase==TransportPhase.Landed)landed=true;}
            if(i%10000==0)Console.WriteLine("  Defense tick="+s.State.Tick+" ai="+s.State.ComputerFactions.Find(a=>a.FactionId==3).Operation.Reason);
        }
        SnapshotStore.Save(Path.Combine(Environment.CurrentDirectory,"TestResults","Reference","defense.json"),s.State);
        ReferenceTests.Check(s.State.Winners.Count==0,"Scenario ended before the military phase completed");
        ReferenceTests.Check(landed,"Paid AI invasion did not land; last state saved");
        ReferenceTests.Check(fought,"Prepared escort did not engage in real space combat");
        var carrier=s.State.Ships.Find(v=>v.Id==transport);
        ReferenceTests.Check(carrier.Troops.Count>0||s.FindPlanet(target).Units.Exists(u=>u.OwnerId==3),"Invasion had no real troops");
        ReferenceTests.ContinueExactly(s,"reference-invasion",400);
        ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms {Kind=TreatyKind.Ceasefire,Members=new System.Collections.Generic.List<int>{1,3},DurationTicks=1200000}});
        var peace=s.State.Treaties[s.State.Treaties.Count-1];ReferenceTests.Wait(s,()=>peace.Phase==TreatyPhase.Active,"Negotiated end to the defended invasion",4000);
        ReferenceTests.Command(s,new SimCommand {Kind=CommandKind.ProposeTreaty,Terms=new TreatyTerms {Kind=TreatyKind.Access,Members=new System.Collections.Generic.List<int>{1,3},Rights=AccessRight.CivilTransit,DurationTicks=1200000}});
        var access=s.State.Treaties[s.State.Treaties.Count-1];ReferenceTests.Wait(s,()=>access.Phase==TreatyPhase.Active,"Restore peaceful freight transit",4000);
        ReferenceTests.Check(s.State.Ships.Exists(v=>v.OwnerId==1&&v.Kind==ShipKind.Freighter&&v.Destroyed),"Reference did not actually lose its convoy");
        ShipState replacement=null;
        if(recoveryAfter!=0)
        {
            ReferenceTests.Wait(s,()=>s.State.Ships.Exists(v=>v.OwnerId==1&&v.Kind==ShipKind.Freighter&&v.Id>=recoveryAfter),"Paid replacement hull");
            replacement=s.State.Ships.Find(v=>v.OwnerId==1&&v.Kind==ShipKind.Freighter&&v.Id>=recoveryAfter&&!v.Destroyed);
        }
        if(replacement==null)
        {
            ReferenceTests.Command(s,new SimCommand{PlanetId=1,Kind=CommandKind.SetBuildingPriority,TargetId=port.Id,IntValue=2});
            replacement=ReferenceTests.Ship(s,port,ShipKind.Freighter);
        }
        ReferenceTests.Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.SetAutoSupply,ActorId=replacement.Id,BoolValue=true});
        ReferenceTests.Command(s,new SimCommand {PlanetId=1,Kind=CommandKind.SetBuildingEnabled,TargetId=electronics.Id,BoolValue=true});
        ReferenceTests.Wait(s,()=>replacement.Freight!=null&&replacement.Freight.Resource==ResourceKind.Crystals,"Paid replacement resumes physical crystal freight",12000);
        ReferenceTests.Command(s,new SimCommand{PlanetId=1,Kind=CommandKind.SetBuildingPriority,TargetId=port.Id,IntValue=0});
        SnapshotStore.Save(Path.Combine(Environment.CurrentDirectory,"TestResults","Reference","convoy-recovery.json"),s.State);
        Console.WriteLine("  Paid invasion transport="+transport+" target="+target+" troops="+s.FindPlanet(target).Units.FindAll(u=>u.OwnerId==3).Count);
    }
}
