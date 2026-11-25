using System;

namespace PlanetOne.Core
{
    public static partial class AiPlanner
    {
        private static double SpaceDistance(ShipState s,AiWorld w)
        {double x=s.X-w.X,y=s.Y-w.Y;return Math.Sqrt(x*x+y*y);}
        private static bool HasShip(AiObservation v,ShipKind kind)
        {return v.Ships.Exists(s=>s.OwnerId==v.FactionId&&s.Kind==kind);}
        private static AiIntent Expand(AiObservation v,AiState ai)
        {
            if(!v.Research.Completed.Contains(Technology.Transport))return null;
            foreach(var w in v.Worlds) if(w.OwnerId==v.FactionId)
            {
                var port=w.Buildings.Find(b=>Own(b,v)&&b.Kind==BuildingKind.Spaceport&&b.Built);
                if(port==null)continue;
                if(v.Research.Completed.Contains(Technology.Colonization)&&!HasShip(v,ShipKind.ColonyShip)&&v.Worlds.Exists(p=>p.OwnerId==0))
                {
                    int[] colonialBudget={48,0,120,40,0,0};
                    for(int r=0;r<6;r++)if(port.Supply.Target[r]<colonialBudget[r])
                        return Order("Kolonisierung finanzieren","Kosten der Startladung physisch am Hafen sammeln; Reserven bleiben lokal",new SimCommand {Kind=CommandKind.SetSupplyPolicy,PlanetId=w.Id,TargetId=port.Id,IntValue=r,Minimum=colonialBudget[r]/2,Amount=colonialBudget[r],Reserve=4,X=1});
                }
                if(port.ShipBuildKind<0 && w.Population>=20)
                {
                    ShipKind? build=null;
                    if(!HasShip(v,ShipKind.OrbitalYard))build=ShipKind.OrbitalYard;
                    else if(!HasShip(v,ShipKind.Freighter)&&v.Worlds.Exists(p=>p.OwnerId==v.FactionId&&p.Id!=w.Id))build=ShipKind.Freighter;
                    else if(v.Research.Completed.Contains(Technology.Colonization)&&!HasShip(v,ShipKind.ColonyShip)&&v.Worlds.Exists(p=>p.OwnerId==0))build=ShipKind.ColonyShip;
                    if(build.HasValue)
                    {
                        var cost=Definitions.Ships[(int)build.Value].Cost;bool enough=true;
                        for(int r=0;r<6;r++)if(Goods(w,v,r)<cost[r]+(r==2?Definitions.AiMetalReserve:r==3?Definitions.AiPartsReserve:0))enough=false;
                        if(enough)return Order("Raumtransport beschaffen","Reale Baukosten und Besatzung verfügbar",new SimCommand {Kind=CommandKind.BuildShip,PlanetId=w.Id,TargetId=port.Id,IntValue=(int)build.Value},"Kolonisierung vor Angriff, sofern neutraler Standort bekannt");
                    }
                }
                var yard=v.Ships.Find(s=>s.OwnerId==v.FactionId&&s.Kind==ShipKind.OrbitalYard&&s.TargetPlanetId==w.Id);
                if(yard!=null&&yard.ProductionKind<0&&w.Population>18)
                {
                    ShipKind? kind=null;
                    if(!HasShip(v,ShipKind.Scout))kind=ShipKind.Scout;
                    else if(v.Enemies.Count>0&&!HasShip(v,ShipKind.EscortFrigate))kind=ShipKind.EscortFrigate;
                    else if(v.Enemies.Count>0&&!HasShip(v,ShipKind.LandingTransport))kind=ShipKind.LandingTransport;
                    if(kind.HasValue)return Order("Flottenkapazität aufbauen","Fehlende Aufklärung, Eskorte oder reale Transportplätze",new SimCommand {Kind=CommandKind.BuildWarship,ActorId=yard.Id,PlanetId=w.Id,IntValue=(int)kind.Value});
                }
                // Own ports with shortages use actual freight promises; the shared logistics engine reserves goods.
                if(v.Worlds.Exists(p=>p.OwnerId==v.FactionId&&p.Id!=w.Id)&&!v.Worlds.Exists(p=>p.OwnerId==0))
                {
                    int[] targets={32,0,36,16,12,4};
                    for(int r=0;r<6;r++)if(port.Supply.Target[r]!=targets[r])
                        return Order("Kolonie versorgen","Hafenpuffer für tatsächlichen interplanetaren Bedarf einrichten",new SimCommand {Kind=CommandKind.SetSupplyPolicy,PlanetId=w.Id,TargetId=port.Id,IntValue=r,Minimum=targets[r]/2,Amount=targets[r],Reserve=r==2?20:r==3?8:4,X=1});
                }
            }
            foreach(var s in v.Ships)if(s.OwnerId==v.FactionId)
            {
                if(s.Kind==ShipKind.LandingTransport&&!ai.Operation.Ships.Contains(s.Id)&&s.TransportPhase==TransportPhase.Landed)
                    return Order("Träger wieder bereitstellen","Nach abgeschlossener Operation leeren Träger regulär starten",new SimCommand {Kind=CommandKind.LaunchTransport,ActorId=s.Id,PlanetId=s.TargetPlanetId});
                if(s.Kind==ShipKind.Freighter&&!s.AutoSupply)return Order("Frachtkapazität einsetzen","Eigener Frachter bedient nur tatsächliche Hafenbedarfe",new SimCommand {Kind=CommandKind.SetAutoSupply,ActorId=s.Id,PlanetId=s.TargetPlanetId,BoolValue=true});
                if(s.Kind==ShipKind.ColonyShip)
                {
                    var target=v.Worlds.Find(w=>w.OwnerId==0);if(target==null)continue;
                    if(target.Current&&SpaceDistance(s,target)<=Definitions.DockDistance)return Order("Standort erschließen","Neutraler aufgeklärter Standort; tatsächliche Kolonieladung übertragen",new SimCommand {Kind=CommandKind.Colonize,ActorId=s.Id,PlanetId=target.Id});
                    if(s.Order!=ShipOrder.Fly||s.TargetPlanetId!=target.Id)return Order("Kolonisierung vorbereiten","Bekannter neutraler Standort als friedliche Alternative",new SimCommand {Kind=CommandKind.FlyShip,ActorId=s.Id,PlanetId=target.Id});
                }
                if(s.Kind==ShipKind.Scout && (v.Tick/Definitions.AiPlanTicks)%5==0 && (s.Order!=ShipOrder.MovePosition||Math.Abs(s.X-s.GoalX)+Math.Abs(s.Y-s.GoalY)<1))
                {
                    // Public coordinate sweep, never asks the simulation where unknown enemies are.
                    int i=(ai.ScoutWaypoint++ + (v.Seed&7))%8;double angle=i*Math.PI/4;
                    return Order("Aufklären","Öffentliche Suchsektoren abfliegen; unbekannte Einheiten werden nicht vorausgesetzt",new SimCommand {Kind=CommandKind.MoveShipPosition,ActorId=s.Id,PlanetId=s.TargetPlanetId,X=(int)(Math.Cos(angle)*18),Y=(int)(Math.Sin(angle)*18)});
                }
                if(s.Kind==ShipKind.EscortFrigate&&!ai.Operation.Ships.Contains(s.Id))
                {
                    var convoy=v.Ships.Find(t=>t.OwnerId==v.FactionId&&(t.Kind==ShipKind.ColonyShip||t.Kind==ShipKind.Freighter));
                    if(convoy!=null&&(s.Order!=ShipOrder.Escort||s.EscortId!=convoy.Id))return Order("Konvoi schützen","Vorhandene Eskorte sichert realen Transport",new SimCommand {Kind=CommandKind.EscortShip,ActorId=s.Id,TargetId=convoy.Id,PlanetId=convoy.TargetPlanetId});
                }
            }
            return null;
        }
    }
}
