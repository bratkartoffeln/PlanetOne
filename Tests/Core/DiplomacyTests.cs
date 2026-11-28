using System;
using System.Collections.Generic;
using System.IO;
using PlanetOne.Core;

internal static class DiplomacyTests
{
    public static void Register(Action<string,Action> run)
    {
        run("M7 treaty rights remain separate and declarations never happen automatically",Rights);
        run("M7 unanimous coalition entry revalidates competing proposals",Unanimity);
        run("M7 ceasefire stops attacks and expiry does not restart war",Ceasefire);
        run("M7 defensive pact excludes partners offensive wars",Defense);
        run("M7 physical trade reserves once and credits only arrivals",Trade);
        run("M7 cancelled loaded trade returns real cargo without duplication",TradeCancel);
        run("M7 lost trade cargo is not credited or recreated",TradeLoss);
        run("M7 coalition sensors end immediately while old knowledge remains",Sight);
        run("M7 evacuation blocks internal attacks and preserves actual units",Evacuation);
        run("M7 joint promises are exclusive and reach their destination",JointDefense);
        run("M7 agreed conquest owner is independent of capturing infantry",JointCapture);
        run("M7 paid gate construction and components use local deliveries",GateDelivery);
        run("M7 peaceful coalition wins despite a hidden foreign scout",Victory);
        run("M7 final tick membership and power changes prevent victory",InterruptedVictory);
        run("M7 final tick control loss and exit prevent victory",ControlLoss);
        run("M7 independent simultaneous gates report shared success",SimultaneousVictory);
        run("M7 loss of capital preserves colonies and equipped settlement options",Elimination);
        run("M7 surrender settles actual assets once and disables commands",Surrender);
        run("M7 diplomacy saves deadlines cargo promises and exact continuation",Persistence);
        run("M7 malformed diplomatic commitments are rejected",InvalidSnapshots);
        run("M7 two computer factions negotiate without player involvement",AiNegotiation);
        run("M7 two loaded allied armies execute a complete joint invasion",JointInvasion);
        run("M7 capitulation transfers existing ids only after bilateral agreement",SurrenderTransfer);
        run("M7 civil transit and port repair require independent access rights",TransitRepair);
        run("M7 coalition entry in the last securing tick resets activation",LastTickJoin);
        run("M7 computer partners fulfil trade and escort promises with real assets",AiCooperation);
        run("M7 supply rights transfer bounded real partner depot stock",SharedSupply);
        run("M7 expired stationing protection ends without deleting troops",EvacuationExpiry);
        run("M7 surrender clears courier reservations and accounts loaded cargo once",SurrenderCouriers);
        run("M7 retained foreign warehouses cannot fund own construction",ForeignStocks);
    }
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    private static GameSimulation World(){var s=GameSimulation.CreateDiplomacyScenario(7);s.State.ComputerFactions.Clear();return s;}
    private static bool Command(GameSimulation s,int owner,CommandKind kind,long target=0,long actor=0,int value=0,bool flag=false,TreatyTerms terms=null,int planet=1,int x=0,int y=0)
    {var c=new SimCommand{Id=s.NextCommandId(),IssuerId=owner,Kind=kind,TargetId=target,ActorId=actor,IntValue=value,BoolValue=flag,Terms=terms,PlanetId=planet,X=x,Y=y};s.Enqueue(c);s.Step();var result=s.State.CommandResults.Find(r=>r.CommandId==c.Id);Check(result!=null,"Command result absent");return result.Success;}
    private static TreatyState Treaty(GameSimulation s,TreatyKind kind,params int[] members){return Treaty(s,new TreatyTerms{Kind=kind,Members=new List<int>(members)});}
    private static TreatyState Treaty(GameSimulation s,TreatyTerms terms)
    {Check(Command(s,terms.Members[0],CommandKind.ProposeTreaty,terms:terms),"Offer failed");var t=s.State.Treaties[s.State.Treaties.Count-1];for(int i=1;i<terms.Members.Count;i++)Check(Command(s,terms.Members[i],CommandKind.AcceptTreaty,t.Id),"Acceptance failed: "+t.Reason);return t;}
    private static BuildingState Building(GameSimulation s,int world,BuildingKind kind,int x=7,int y=5)
    {var existing=s.FindPlanet(world).Buildings.Find(item=>item.Kind==kind&&item.X==x&&item.Y==y);if(existing!=null)return existing;var b=new BuildingState{Id=s.State.NextEntityId++,PlanetId=world,Kind=kind,Built=true,X=x,Y=y,Operational=true,Health=Definitions.BuildingHealth(kind)};s.FindPlanet(world).Buildings.Add(b);return b;}
    private static ShipState Ship(GameSimulation s,int owner,ShipKind kind,int world)
    {var pos=s.GetOrbitPosition(world);var sh=new ShipState{Id=s.State.NextEntityId++,OwnerId=owner,Kind=kind,X=pos.X,Y=pos.Y,TargetPlanetId=world,Crew=Definitions.Ships[(int)kind].Crew,Health=Definitions.SpaceWeapons[(int)kind].Health,Ammunition=Definitions.SpaceWeapons[(int)kind].Ammo};s.State.Ships.Add(sh);return sh;}
    private static UnitState Unit(GameSimulation s,int owner,UnitKind kind,int world,double x,double y)
    {var d=Definitions.Get(kind);var u=new UnitState{Id=s.State.NextEntityId++,OwnerId=owner,Kind=kind,X=x,Y=y,GoalX=(int)x,GoalY=(int)y,Health=d.Health,Ammunition=d.Ammo,Food=4,Parts=kind==UnitKind.Engineer?8:0};s.FindPlanet(world).Units.Add(u);return u;}
    private static void Rights()
    {var s=World();var t=Treaty(s,TreatyKind.NonAggression,1,2);Check(!Command(s,1,CommandKind.DeclareWar,value:2),"Protection bypassed");Check(!s.HasAccess(1,2,2,AccessRight.Landing),"Peace grants landing");Check(Command(s,1,CommandKind.BreachTreaty,t.Id,flag:true),"Explicit breach failed");Check(Command(s,1,CommandKind.DeclareWar,value:2)&&s.AtWar(1,2),"War not declared");Check(!s.AtWar(1,3),"Unrelated war");Check(s.Trust(3,1)==-20,"Public breach not remembered");int events=s.State.DiplomacyEvents.Count;Check(!Command(s,1,CommandKind.BreachTreaty,t.Id,flag:true)&&events==s.State.DiplomacyEvents.Count,"Breach repeated");}
    private static void Unanimity()
    {var s=World();var c=Treaty(s,TreatyKind.Coalition,1,2);var terms=new TreatyTerms{Kind=TreatyKind.Coalition,CoalitionId=c.Id,Members=new List<int>{1,2,3}};Check(Command(s,1,CommandKind.ProposeTreaty,terms:terms),"Join offer failed");var t=s.State.Treaties[s.State.Treaties.Count-1];Command(s,3,CommandKind.AcceptTreaty,t.Id);Check(s.CoalitionOf(3)==null,"Unanimity bypassed");Command(s,2,CommandKind.AcceptTreaty,t.Id);Check(s.CoalitionOf(3)==t&&c.Phase==TreatyPhase.Expired,"Join failed");Check(!Command(s,1,CommandKind.AcceptTreaty,t.Id),"Repeat accepted");}
    private static void Ceasefire()
    {var s=World();Command(s,1,CommandKind.DeclareWar,value:2);var u=Unit(s,1,UnitKind.Infantry,2,3,3);u.Order=UnitOrder.Attack;var t=Treaty(s,new TreatyTerms{Kind=TreatyKind.Ceasefire,Members=new List<int>{1,2},DurationTicks=40,NoticeTicks=20});Check(!s.AtWar(1,2)&&u.Order==UnitOrder.Hold,"Ceasefire didn't stop orders");s.Step(42);Check(t.Phase==TreatyPhase.Expired&&!s.AtWar(1,2),"Expiry redeclared war");}
    private static void Defense()
    {var s=World();Treaty(s,TreatyKind.Defense,2,3);Command(s,2,CommandKind.DeclareWar,value:1);Check(!s.AtWar(1,3),"Offensive obligation propagated");s=World();Treaty(s,TreatyKind.Defense,2,3);Command(s,1,CommandKind.DeclareWar,value:2);Check(s.AtWar(1,3),"External defense not activated");}
    private static TreatyState TradeSetup(GameSimulation s,out ShipState ship,out BuildingState source,out BuildingState target)
    {source=Building(s,1,BuildingKind.Spaceport);target=Building(s,2,BuildingKind.Spaceport);Building(s,1,BuildingKind.SolarPlant,10,5);Building(s,2,BuildingKind.SolarPlant,10,5);source.Inventory[2]=24;target.Inventory[3]=12;ship=Ship(s,1,ShipKind.Freighter,1);var t=Treaty(s,new TreatyTerms{Kind=TreatyKind.Trade,Members=new List<int>{1,2},SourcePort=source.Id,TargetPort=target.Id,Goods=ResourceKind.Metal,Amount=12,Payment=ResourceKind.Parts,PaymentAmount=2});return t;}
    private static void Trade()
    {var s=World();ShipState sh;BuildingState a,b;var t=TradeSetup(s,out sh,out a,out b);int before=b.Inventory[2];Check(Command(s,1,CommandKind.DispatchTrade,t.Id,sh.Id),"Dispatch failed");Check(t.Delivered==0&&b.Inventory[2]==before&&a.ReservedOutgoing[2]>=12,"Teleport or absent reservation");var other=Ship(s,1,ShipKind.Freighter,1);Check(!Command(s,1,CommandKind.DispatchTrade,t.Id,other.Id),"Double reserved quantity");Check(!s.HasAccess(1,2,2,AccessRight.Landing),"Trade grants landing");s.Step(500);Check(t.Delivered==12&&b.Inventory[2]>=before+12,"No actual delivery");var payment=Ship(s,2,ShipKind.Freighter,2);Check(Command(s,2,CommandKind.DispatchTrade,t.Id,payment.Id),"Payment dispatch failed");s.Step(500);Check(t.PaymentDelivered==2&&s.Trust(1,2)==6,"Trade not completed");SaveCodec.ToJson(s.State);}
    private static void TradeCancel()
    {var s=World();ShipState sh;BuildingState a,b;var t=TradeSetup(s,out sh,out a,out b);Command(s,1,CommandKind.DispatchTrade,t.Id,sh.Id);for(int i=0;i<400&&sh.Cargo[2]==0;i++)s.Step();Check(sh.Cargo[2]==12,"Cargo not loaded");Command(s,1,CommandKind.BreachTreaty,t.Id,flag:true);Check(t.Delivered==0&&sh.Cargo[2]==12,"Cancellation lost or teleported cargo");s.Step(500);Check(sh.Cargo[2]==0&&t.Delivered==0,"Cargo not returned");SaveCodec.ToJson(s.State);}
    private static void TradeLoss()
    {var s=World();ShipState sh;BuildingState a,b;var t=TradeSetup(s,out sh,out a,out b);Command(s,1,CommandKind.DispatchTrade,t.Id,sh.Id);for(int i=0;i<400&&sh.Cargo[2]==0;i++)s.Step();s.DestroyShip(sh.Id);Check(t.Delivered==0&&s.State.SpaceLostTotals[2]==12&&b.ReservedIncoming[2]==0,"Lost cargo credited or reservation leaked");s.Step(50);SaveCodec.ToJson(s.State);}
    private static void Sight()
    {var s=World();var enemy=Unit(s,3,UnitKind.Infantry,2,8,7);Check(!s.CanSeeEntity(s.FindPlanet(2),1,enemy.Id),"Initial foreign sight leaked");var t=Treaty(s,TreatyKind.Coalition,1,2);Check(s.CanSeeEntity(s.FindPlanet(2),1,enemy.Id),"No shared sight");s.Step();Check(s.FindPlanet(2).Sightings.Exists(v=>v.FactionId==1&&v.EntityId==enemy.Id),"No remembered sight");Command(s,1,CommandKind.BreachTreaty,t.Id,flag:true);Check(!s.CanSeeEntity(s.FindPlanet(2),1,enemy.Id)&&s.FindPlanet(2).Sightings.Exists(v=>v.FactionId==1&&v.EntityId==enemy.Id),"Exit erased memory or retained sensors");}
    private static void Evacuation()
    {var s=World();var t=Treaty(s,new TreatyTerms{Kind=TreatyKind.Access,Members=new List<int>{1,2},PlanetId=2,Rights=AccessRight.Landing|AccessRight.Stationing|AccessRight.MilitaryTransit});var guest=Unit(s,1,UnitKind.Infantry,2,6,5);var host=Unit(s,2,UnitKind.Infantry,2,6,4);var carrier=Ship(s,1,ShipKind.LandingTransport,2);carrier.TransportPhase=TransportPhase.Landed;carrier.LandingX=5;carrier.LandingY=5;Command(s,1,CommandKind.BreachTreaty,t.Id,flag:true);Command(s,1,CommandKind.DeclareWar,value:2);double health=guest.Health,hostHealth=host.Health;Check(!Command(s,1,CommandKind.AttackTarget,host.Id,guest.Id,planet:2),"Inside attack permitted");Check(!Command(s,2,CommandKind.AttackTarget,guest.Id,host.Id,planet:2),"Host attacked evacuee");s.Step(100);Check(guest.Health==health&&host.Health==hostHealth,"Automatic fire bypassed protection");Check(Command(s,1,CommandKind.LoadTroop,guest.Id,carrier.Id,planet:2),"Evacuation loading denied");Check(!Command(s,1,CommandKind.UnloadTroop,guest.Id,carrier.Id,planet:2),"Evacuation permitted reinsertion");Check(Command(s,1,CommandKind.LaunchTransport,actor:carrier.Id,planet:2),"Launch denied");s.Step(180);Check(carrier.TransportPhase==TransportPhase.Space&&!guest.Destroyed,"Evacuation removed survivors");SaveCodec.ToJson(s.State);}
    private static void JointDefense()
    {var s=World();var a=Ship(s,1,ShipKind.EscortFrigate,1);var b=Ship(s,2,ShipKind.EscortFrigate,2);var terms=new TreatyTerms{Kind=TreatyKind.JointOperation,Members=new List<int>{1,2},PlanetId=2,FutureOwner=2};var t=Treaty(s,terms);var t2=Treaty(s,terms);Check(Command(s,1,CommandKind.PledgeShip,t.Id,a.Id),"First pledge failed");Check(!Command(s,1,CommandKind.PledgeShip,t2.Id,a.Id),"Ship pledged twice");Command(s,2,CommandKind.PledgeShip,t.Id,b.Id);s.Step(45);Check(Command(s,1,CommandKind.StartJointOperation,t.Id),"Joint start failed");s.Step(400);Check(t.Pledges.TrueForAll(p=>p.Arrived),"Contributions didn't arrive");s.DestroyShip(a.Id);s.Step();Check(t.OperationPhase==OperationPhase.Cancelled&&!s.ShipPledged(b.Id),"Lost pledge didn't release other forces");SaveCodec.ToJson(s.State);}
    private static void JointCapture()
    {var s=World();Command(s,1,CommandKind.DeclareWar,value:3);Command(s,2,CommandKind.DeclareWar,value:3);var t=Treaty(s,new TreatyTerms{Kind=TreatyKind.JointOperation,Members=new List<int>{1,2},PlanetId=4,FutureOwner=1,Goal=JointGoal.Invade});t.OperationPhase=OperationPhase.Executing;
        var p=s.FindPlanet(4);var hq=p.Buildings[0];var u=Unit(s,2,UnitKind.Infantry,4,hq.X-1,hq.Y);s.State.Beachheads.Add(new BeachheadState{Id=s.State.NextEntityId++,OwnerId=2,PlanetId=4,X=hq.X-1,Y=hq.Y+1,Food=8,Progress=Definitions.BeachheadSeconds});Check(Command(s,2,CommandKind.CaptureBuilding,hq.Id,u.Id,planet:4),"Capture rejected");s.Step(180);Check(p.OwnerId==1&&hq.OwnerId==1&&u.OwnerId==2,"Last capturing faction stole agreed ownership");}
    private static BuildingState Gate(GameSimulation s,int world)
    {var p=s.FindPlanet(world);var f=s.State.Factions.Find(v=>v.Id==p.OwnerId);foreach(Technology tech in Enum.GetValues(typeof(Technology)))if(!f.Research.Completed.Contains(tech))f.Research.Completed.Add(tech);Building(s,world,BuildingKind.SolarPlant,10,5);Building(s,world,BuildingKind.Farm,9,7);var g=Building(s,world,BuildingKind.JumpGate,7,7);Array.Copy(Definitions.GateComponents,g.Inventory,Definitions.ResourceCount);s.Step();return g;}
    private static void GateDelivery()
    {var s=World();var p=s.FindPlanet(1);foreach(Technology tech in Enum.GetValues(typeof(Technology)))s.State.Factions[0].Research.Completed.Add(tech);Building(s,1,BuildingKind.SolarPlant,10,5);Building(s,1,BuildingKind.Farm,9,7);p.Buildings[0].Inventory=new[]{60,0,100,70,8,8,0};var c=new SimCommand{Id=s.NextCommandId(),IssuerId=1,PlanetId=1,Kind=CommandKind.PlaceBuilding,Building=BuildingKind.JumpGate,X=7,Y=5};s.Enqueue(c);s.Step();var g=p.Buildings.Find(b=>b.Kind==BuildingKind.JumpGate);Check(g!=null&&!g.Built&&g.Inventory[2]==0,"Gate build teleport");s.Step(4000);Check(g.Built,"Paid gate not constructed");for(int r=0;r<Definitions.ResourceCount;r++)Check(g.Inventory[r]>=Definitions.GateComponents[r],"Gate component not delivered: "+r);Check(Command(s,1,CommandKind.ActivateGate,g.Id),"Delivered gate not activatable");}
    private static void Victory()
    {var s=World();Treaty(s,TreatyKind.Coalition,1,2,3);var scout=Ship(s,3,ShipKind.Scout,4);scout.X=scout.Y=100;var g=Gate(s,1);Check(Command(s,1,CommandKind.ActivateGate,g.Id),"Activation failed");s.Step(Definitions.GateSecureTicks);Check(s.State.Winners.Count==3&&s.State.GateProjects[0].Complete,"Peaceful coalition did not win");SaveCodec.ToJson(s.State);}
    private static void InterruptedVictory()
    {var s=World();var t=Treaty(s,TreatyKind.Coalition,1,2);var g=Gate(s,1);Command(s,1,CommandKind.ActivateGate,g.Id);s.State.GateProjects[0].SecureTicks=Definitions.GateSecureTicks-1;Command(s,2,CommandKind.BreachTreaty,t.Id,flag:true);Check(s.State.Winners.Count==0&&!s.State.GateProjects[0].Activating&&s.State.GateProjects[0].SecureTicks==0,"Final tick exit won");Command(s,1,CommandKind.ActivateGate,g.Id);s.State.GateProjects[0].SecureTicks=Definitions.GateSecureTicks-1;Command(s,1,CommandKind.SetBuildingEnabled,g.Id,flag:false);Check(s.State.Winners.Count==0&&!s.State.GateProjects[0].Activating,"Disabled gate won");}
    private static void ControlLoss()
    {var s=World();var g=Gate(s,1);Command(s,1,CommandKind.ActivateGate,g.Id);s.State.GateProjects[0].SecureTicks=Definitions.GateSecureTicks-1;s.FindPlanet(1).OwnerId=2;s.Step();Check(s.State.Winners.Count==0&&!s.State.GateProjects[0].Activating,"Lost control won");s=World();Treaty(s,TreatyKind.Coalition,1,2);g=Gate(s,1);Command(s,1,CommandKind.ActivateGate,g.Id);s.State.GateProjects[0].SecureTicks=Definitions.GateSecureTicks-1;Command(s,2,CommandKind.Surrender,flag:true);Check(s.State.Winners.Count==0&&!s.State.GateProjects[0].Activating,"Exited member won");}
    private static void SimultaneousVictory()
    {var s=World();var a=Gate(s,1);var b=Gate(s,2);Command(s,1,CommandKind.ActivateGate,a.Id);Command(s,2,CommandKind.ActivateGate,b.Id);foreach(var g in s.State.GateProjects)g.SecureTicks=Definitions.GateSecureTicks-1;s.Step();Check(s.State.Winners.Count==2,"Simultaneous success not shared");}
    private static void Elimination()
    {var s=World();s.FindPlanet(3).OwnerId=1;s.FindPlanet(1).OwnerId=2;s.Step();Check(s.State.Factions[0].Status==FactionStatus.Active,"Second colony lost faction");s=World();var colony=Ship(s,1,ShipKind.ColonyShip,3);Array.Copy(Definitions.Ships[1].ColonyCargo,colony.Cargo,Definitions.ResourceCount);s.Step();s.FindPlanet(1).OwnerId=2;s.Step();Check(s.State.Factions[0].Status==FactionStatus.Active,"Equipped colonizer ignored");colony.Cargo[0]=0;s.Step();Check(s.State.Factions[0].Status==FactionStatus.Eliminated,"Unequipped colony counted");s=World();Ship(s,1,ShipKind.Scout,1);s.FindPlanet(1).OwnerId=2;s.Step();Check(s.State.Factions[0].Status==FactionStatus.Eliminated,"Scout prevents elimination");}
    private static void Surrender()
    {var s=World();var building=s.FindPlanet(1).Buildings[0];int metal=building.Inventory[2];var ship=Ship(s,1,ShipKind.Freighter,1);ship.Cargo[2]=5;Treaty(s,TreatyKind.Coalition,1,2,3);Check(Command(s,1,CommandKind.Surrender,flag:true),"Surrender rejected");Check(s.FindPlanet(1).OwnerId==0&&GameSimulation.BuildingOwner(s.FindPlanet(1),building)==0&&!building.Enabled&&building.Inventory[2]==metal,"Neutral inventory changed");Check(ship.Destroyed&&s.State.SpaceLostTotals[2]==5&&s.CoalitionOf(1)==null,"Exit incomplete");Check(!Command(s,1,CommandKind.Surrender,flag:true),"Exited faction acts again");s.Step(20);Check(s.State.SpaceLostTotals[2]==5,"Loss counted twice");SaveCodec.ToJson(s.State);}
    private static void Persistence()
    {var s=World();ShipState sh;BuildingState a,b;var t=TradeSetup(s,out sh,out a,out b);Command(s,1,CommandKind.DispatchTrade,t.Id,sh.Id);s.Step(130);var alliance=Treaty(s,TreatyKind.Coalition,1,2);Command(s,1,CommandKind.TerminateTreaty,alliance.Id);var gate=Gate(s,1);Command(s,1,CommandKind.ActivateGate,gate.Id);var json=SaveCodec.ToJson(s.State);var copy=new GameSimulation(SaveCodec.FromJson(json));s.Step(250);copy.Step(250);Check(SaveCodec.ToJson(s.State)==SaveCodec.ToJson(copy.State),"Continuation diverged");}
    private static void InvalidSnapshots()
    {var s=World();var t=Treaty(s,TreatyKind.Coalition,1,2);t.Approvals.Add(2);bool rejected=false;try{SaveCodec.ToJson(s.State);}catch(InvalidDataException){rejected=true;}Check(rejected,"Duplicate approvals accepted");}
    private static void AiNegotiation()
    {var s=GameSimulation.CreateDiplomacyScenario(23);s.Step(2400);Check(s.State.Treaties.Exists(t=>t.Terms.Members.Contains(2)&&t.Terms.Members.Contains(3)&&t.Terms.Kind==TreatyKind.NonAggression&&GameSimulation.TreatyLive(t)),"Computer factions did not negotiate");Check(s.State.ComputerFactions.TrueForAll(a=>a.Decisions.Exists(d=>d.Action==CommandKind.AcceptTreaty.ToString()||d.Action==CommandKind.ProposeTreaty.ToString())),"No AI command evidence");SaveCodec.ToJson(s.State);}
    private static void JointInvasion()
    {
        var s=World();Command(s,1,CommandKind.DeclareWar,value:3);Command(s,2,CommandKind.DeclareWar,value:3);
        var t=Treaty(s,new TreatyTerms{Kind=TreatyKind.JointOperation,Members=new List<int>{1,2},PlanetId=4,FutureOwner=1,Goal=JointGoal.Invade});
        foreach(int f in new[]{1,2})
        {
            Building(s,f,BuildingKind.Spaceport);Building(s,f,BuildingKind.SolarPlant,10,5);
            var carrier=Ship(s,f,ShipKind.LandingTransport,f);carrier.TransportPhase=TransportPhase.Landed;carrier.LandingX=5;carrier.LandingY=5;carrier.Cargo[0]=12;carrier.Cargo[3]=12;
            for(int i=0;i<4;i++){var u=Unit(s,f,i==0?UnitKind.Engineer:UnitKind.Infantry,f,5,5);Check(Command(s,f,CommandKind.LoadTroop,u.Id,carrier.Id,planet:f),"Actual loading failed");}
            Check(Command(s,f,CommandKind.LaunchTransport,actor:carrier.Id,planet:f),"Launch failed");s.Step(170);
            var escort=Ship(s,f,ShipKind.EscortFrigate,f);Command(s,f,CommandKind.PledgeShip,t.Id,carrier.Id);Command(s,f,CommandKind.PledgeShip,t.Id,escort.Id);
        }
        s.Step(45);Check(Command(s,1,CommandKind.StartJointOperation,t.Id),"Prepared invasion could not start");
        s.Step(4800);Check(s.FindPlanet(4).OwnerId==1&&t.OperationPhase==OperationPhase.Completed,"Joint invasion failed: "+t.Reason+" / "+string.Join(";",s.State.Ships.ConvertAll(sh=>sh.Id+":"+sh.Status+":"+sh.TransportPhase)));
        Check(s.FindPlanet(4).Units.Exists(u=>u.OwnerId==2&&!u.Destroyed),"Partner troops disappeared or changed ownership");SaveCodec.ToJson(s.State);
    }
    private static void SurrenderTransfer()
    {
        var s=World();var ship=Ship(s,1,ShipKind.Freighter,1);ship.Cargo[2]=7;long building=s.FindPlanet(1).Buildings[0].Id;
        Check(!Command(s,1,CommandKind.Surrender,value:2,flag:true),"Unagreed transfer allowed");
        Treaty(s,TreatyKind.Surrender,1,2);Check(s.State.Factions[0].Status==FactionStatus.Surrendered&&ship.OwnerId==2&&!ship.Destroyed&&ship.Cargo[2]==7&&s.FindPlanet(1).OwnerId==2&&s.FindPlanet(1).Buildings[0].Id==building,"Existing assets not transferred exactly");SaveCodec.ToJson(s.State);
    }
    private static void TransitRepair()
    {
        var s=World();var ship=Ship(s,1,ShipKind.Freighter,2);var position=s.GetOrbitPosition(2);ship.X=position.X+4;ship.Y=position.Y;
        Command(s,1,CommandKind.FlyShip,actor:ship.Id,planet:2);s.Step(140);Check(ship.Status.Contains("Transitrecht"),"Peaceful protected orbit freely entered");
        Treaty(s,new TreatyTerms{Kind=TreatyKind.Access,Members=new List<int>{1,2},PlanetId=2,Rights=AccessRight.CivilTransit});s.Step(140);Check(!ship.Status.Contains("Transitrecht"),"Civil transit not applied");
        var yard=Ship(s,2,ShipKind.OrbitalYard,2);yard.Cargo[3]=8;ship.Health=60;
        Check(!Command(s,1,CommandKind.RepairShip,yard.Id,ship.Id,planet:2),"Transit grants repair");
        Treaty(s,new TreatyTerms{Kind=TreatyKind.Access,Members=new List<int>{1,2},PlanetId=2,Rights=AccessRight.Repair});Check(Command(s,1,CommandKind.RepairShip,yard.Id,ship.Id,planet:2),"Repair right ignored");s.Step(100);Check(ship.Health>60&&yard.Cargo[3]<8,"Repair did not consume actual host parts");
    }
    private static void LastTickJoin()
    {
        var s=World();var old=Treaty(s,TreatyKind.Coalition,1,2);var g=Gate(s,1);Command(s,1,CommandKind.ActivateGate,g.Id);
        Command(s,1,CommandKind.ProposeTreaty,terms:new TreatyTerms{Kind=TreatyKind.Coalition,CoalitionId=old.Id,Members=new List<int>{1,2,3}});var join=s.State.Treaties[s.State.Treaties.Count-1];Command(s,2,CommandKind.AcceptTreaty,join.Id);
        s.State.GateProjects[0].SecureTicks=Definitions.GateSecureTicks-1;Command(s,3,CommandKind.AcceptTreaty,join.Id);Check(s.State.Winners.Count==0&&!s.State.GateProjects[0].Activating,"Late join inherited instant victory");Check(Command(s,1,CommandKind.ActivateGate,g.Id)&&s.State.GateProjects[0].Participants.Count==3,"Fresh activation failed");
    }
    private static void AiCooperation()
    {
        var s=GameSimulation.CreateDiplomacyFixture();var source=s.FindPlanet(2).Buildings.Find(b=>b.Kind==BuildingKind.Spaceport);var target=s.FindPlanet(4).Buildings.Find(b=>b.Kind==BuildingKind.Spaceport);
        var ship=Ship(s,2,ShipKind.EscortFrigate,2);Ship(s,3,ShipKind.EscortFrigate,4);
        var trade=Treaty(s,new TreatyTerms{Kind=TreatyKind.Trade,Members=new List<int>{2,3},SourcePort=source.Id,TargetPort=target.Id});
        var coalition=Treaty(s,TreatyKind.Coalition,2,3);
        var freighter=s.State.Ships.Find(a=>a.OwnerId==2&&a.Kind==ShipKind.Freighter);
        var joint=Treaty(s,new TreatyTerms{Kind=TreatyKind.JointOperation,Members=new List<int>{2,3},PlanetId=2,FutureOwner=2,Goal=JointGoal.Escort,EscortTarget=freighter.Id});
        s.State.ComputerFactions.Add(new AiState{FactionId=2,Profile=AiProfile.Balanced,NextPlanTick=400});s.State.ComputerFactions.Add(new AiState{FactionId=3,Profile=AiProfile.Security,NextPlanTick=400});
        s.Step(3600);Check(trade.Delivered==8&&trade.PaymentDelivered==2,"AI did not physically fulfil bilateral trade");Check(joint.OperationPhase==OperationPhase.Executing&&joint.Pledges.Count==2&&ship.Order==ShipOrder.Escort,"AI joint promise not executed");
        Command(s,2,CommandKind.BreachTreaty,coalition.Id,flag:true);s.Step(800);Check(joint.Phase==TreatyPhase.Terminating||joint.Phase==TreatyPhase.Expired,"Known breach did not cause reasoned AI cancellation");SaveCodec.ToJson(s.State);
    }
    private static void SharedSupply()
    {
        var s=World();var depot=Building(s,2,BuildingKind.MilitaryDepot);depot.Inventory[3]=8;var u=Unit(s,1,UnitKind.Infantry,2,7,4);u.Ammunition=0;
        Check(!Command(s,1,CommandKind.ResupplyUnit,depot.Id,u.Id,planet:2),"Foreign supplies freely available");Treaty(s,new TreatyTerms{Kind=TreatyKind.Access,Members=new List<int>{1,2},PlanetId=2,Rights=AccessRight.Supply});
        Check(Command(s,1,CommandKind.ResupplyUnit,depot.Id,u.Id,planet:2),"Supply permission ignored");s.Step(30);Check(u.Ammunition>0&&s.FindPlanet(2).ConsumedTotals[3]>0,"Partner ammunition was free: ammo="+u.Ammunition+" status="+u.Status+" depot="+depot.Status);
    }
    private static void EvacuationExpiry()
    {
        var s=World();var t=Treaty(s,new TreatyTerms{Kind=TreatyKind.Access,Members=new List<int>{1,2},PlanetId=2,Rights=AccessRight.Stationing,DurationTicks=40,NoticeTicks=20});
        var guest=Unit(s,1,UnitKind.Infantry,2,6,5);var host=Unit(s,2,UnitKind.Infantry,2,6,4);s.Step(40);Check(t.Phase==TreatyPhase.Expired&&s.State.Evacuations.Count==1,"Stationing expiry did not announce evacuation");
        Command(s,1,CommandKind.DeclareWar,value:2);double hp=guest.Health;s.Step(100);Check(guest.Health==hp,"Early combat during deadline");s.State.Evacuations[0].UntilTick=s.State.Tick;s.Step(30);Check(guest.Health<hp||host.Health<Definitions.Get(host.Kind).Health,"Normal combat did not resume after deadline");Check(s.FindPlanet(2).Units.Contains(guest),"Deadline deleted troops");
    }
    private static void SurrenderCouriers()
    {
        var s=World();var p=s.FindPlanet(1);s.Enqueue(new SimCommand{Id=s.NextCommandId(),IssuerId=1,PlanetId=1,Kind=CommandKind.PlaceBuilding,Building=BuildingKind.Warehouse,X=7,Y=7});
        for(int i=0;i<80&&!p.Shipments.Exists(sh=>sh.Phase==ShipmentPhase.InTransit||sh.Phase==ShipmentPhase.Unloading);i++)s.Step();Check(p.Shipments.Exists(sh=>sh.Phase==ShipmentPhase.InTransit||sh.Phase==ShipmentPhase.Unloading),"No real loaded delivery fixture");int loaded=0;foreach(var shipment in p.Shipments)if(shipment.Phase!=ShipmentPhase.Loading)loaded+=shipment.Amount;
        Command(s,1,CommandKind.Surrender,flag:true);Check(p.Deliveries.Count==0&&p.Shipments.Count==0&&p.Buildings.TrueForAll(b=>Array.TrueForAll(b.ReservedOutgoing,n=>n==0)&&Array.TrueForAll(b.ReservedIncoming,n=>n==0)&&b.ReservedReturnSpace==0),"Courier reservations remained");
        int lost=0;foreach(int amount in p.LostTotals)lost+=amount;Check(lost==loaded,"Loaded courier cargo was refunded or duplicated");SaveCodec.ToJson(s.State);
    }
    private static void ForeignStocks()
    {
        var s=World();var p=s.FindPlanet(1);p.Buildings[0].Inventory[2]=0;p.Buildings[0].Inventory[3]=0;
        var foreign=Building(s,1,BuildingKind.Warehouse);foreign.OwnerId=2;foreign.Inventory[2]=80;foreign.Inventory[3]=40;
        Check(s.ValidatePlacement(1,BuildingKind.Farm,9,7,1)!=null,"Foreign retained stock funds construction");Check(foreign.ReservedOutgoing[2]==0,"Foreign goods reserved");
    }
}
