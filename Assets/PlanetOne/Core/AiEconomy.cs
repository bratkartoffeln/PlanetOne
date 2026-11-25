using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    /// <summary>Pure decision layer. Only detached, prefiltered observations and its own memory enter here.</summary>
    public static partial class AiPlanner
    {
        public static AiIntent Plan(AiObservation v, AiState ai)
        {
            Remember(v,ai);
            var urgent=Defend(v,ai);if(urgent!=null)return urgent;
            // Economy gets regular turns even while an operation issues tactical commands.
            bool economyTurn=(v.Tick/Definitions.AiPlanTicks)%2==0;
            if(!economyTurn) {var military=Military(v,ai);if(military!=null && military.Command!=null)return military;}
            foreach(var w in v.Worlds) if(w.OwnerId==v.FactionId)
            {var action=Economy(v,ai,w);if(action!=null)return action;}
            var space=Expand(v,ai);if(space!=null)return space;
            var operation=Military(v,ai);if(operation!=null)return operation;
            return new AiIntent("Bestände sichern","Keine ausführbare Verbesserung; echte Produktion läuft","Unbezahlbare Aufträge vermeiden");
        }
        private static bool Own(BuildingState b,AiObservation v) {return b.OwnerId==v.FactionId && !b.Deconstructing;}
        private static int Count(AiWorld w,AiObservation v,BuildingKind kind) {return w.Buildings.FindAll(b=>Own(b,v)&&b.Kind==kind).Count;}
        private static int Goods(AiWorld w,AiObservation v,int resource,bool storage=false)
        {int total=0;foreach(var b in w.Buildings)if(Own(b,v)&&b.Built&&(!storage||GameSimulation.IsStorage(b)))total+=b.Inventory[resource]-b.ReservedOutgoing[resource];return total;}
        private static AiIntent Order(string goal,string reason,SimCommand command,string alternative="Bestände halten und neu bewerten")
        {return new AiIntent(goal,reason,alternative,command);}

        private static List<GridCell> RoadPath(AiWorld w,GridCell start,GridCell goal,bool existing)
        {
            int length=w.Width*w.Height;var prev=new int[length];for(int i=0;i<length;i++)prev[i]=-1;
            int s=start.Y*w.Width+start.X,g=goal.Y*w.Width+goal.X;var queue=new Queue<int>();queue.Enqueue(s);prev[s]=s;
            while(queue.Count>0)
            {
                int c=queue.Dequeue();if(c==g)break;
                for(int d=0;d<6;d++)
                {
                    var n=HexGrid.Neighbor(c%w.Width,c/w.Width,d);if(n.X<0||n.Y<0||n.X>=w.Width||n.Y>=w.Height)continue;
                    int next=n.Y*w.Width+n.X;if(prev[next]>=0)continue;
                    if(next!=g && (existing?!w.Roads[next]:w.Terrain[next]!=0 || w.Buildings.Exists(b=>b.X==n.X&&b.Y==n.Y)))continue;
                    prev[next]=c;queue.Enqueue(next);
                }
            }
            if(prev[g]<0)return null;var path=new List<GridCell>();for(int c=g;;c=prev[c]){path.Add(new GridCell(c%w.Width,c/w.Width));if(c==s)break;}path.Reverse();return path;
        }
        private static AiIntent Connect(AiWorld w,BuildingState hq,GridCell site)
        {
            if(RoadPath(w,new GridCell(hq.X,hq.Y),site,true)!=null)return null;
            var path=RoadPath(w,new GridCell(hq.X,hq.Y),site,false);if(path==null)return null;
            for(int i=1;i<path.Count-1;i++) {var c=path[i];if(!w.Roads[c.Y*w.Width+c.X])return Order("Lieferweg herstellen","Fehlende Wabenkante im Weg zur Anlage",new SimCommand {Kind=CommandKind.PlaceRoad,PlanetId=w.Id,X=c.X,Y=c.Y},"Weitere Produktion behebt die Wegeunterbrechung nicht");}
            return null;
        }
        private static AiIntent Build(AiObservation v,AiState ai,AiWorld w,BuildingKind kind,string reason,bool essential=false)
        {
            var cost=Definitions.Get(kind).Cost;
            int reserveMetal=essential?0:Definitions.AiMetalReserve+(ai.Profile==AiProfile.Security?8:0), reserveParts=essential?0:Definitions.AiPartsReserve;
            if(Goods(w,v,2,true)<cost[2]+reserveMetal || Goods(w,v,3,true)<cost[3]+reserveParts)return null;
            var hq=w.Buildings.Find(b=>Own(b,v)&&b.Kind==BuildingKind.Headquarters);if(hq==null)return null;
            GridCell? chosen=null;double best=double.MaxValue;
            for(int y=0;y<w.Height;y++)for(int x=0;x<w.Width;x++)
            {
                int i=y*w.Width+x,terrain=kind==BuildingKind.Mine?2:kind==BuildingKind.CrystalMine?3:0;
                if(w.Terrain[i]!=terrain || terrain>=2 && w.Ore[i]<=0 || w.Roads[i] || w.Buildings.Exists(b=>b.X==x&&b.Y==y))continue;
                var path=RoadPath(w,new GridCell(hq.X,hq.Y),new GridCell(x,y),false);if(path==null)continue;
                double score=HexGrid.Steps(hq.X,hq.Y,x,y)*10+((x*17+y*31+v.Seed)&15)*0.01;
                if(RoadPath(w,new GridCell(hq.X,hq.Y),new GridCell(x,y),true)==null)score+=30;
                if(score<best){best=score;chosen=new GridCell(x,y);}
            }
            if(!chosen.HasValue)return null;
            var road=Connect(w,hq,chosen.Value);if(road!=null)return road;
            return Order("Bauen: "+Definitions.Get(kind).Name,reason+(essential?"; Grundversorgung darf Wiederaufbaureserve nutzen":"; Wiederaufbaureserve bleibt geschützt"),new SimCommand {Kind=CommandKind.PlaceBuilding,PlanetId=w.Id,Building=kind,X=chosen.Value.X,Y=chosen.Value.Y});
        }
        private static AiIntent Economy(AiObservation v,AiState ai,AiWorld w)
        {
            var hq=w.Buildings.Find(b=>Own(b,v)&&b.Kind==BuildingKind.Headquarters);if(hq==null)return null;
            foreach(var b in w.Buildings)if(Own(b,v)&&b.Id!=hq.Id)
            {var repair=Connect(w,hq,new GridCell(b.X,b.Y));if(repair!=null)return repair;}
            foreach(var b in w.Buildings)if(Own(b,v)&&b.Built&&b.Kind==BuildingKind.Workshop)
            {
                int parts=Goods(w,v,3,true)+b.Inventory[3]-b.ReservedOutgoing[3];bool enable=b.Enabled?parts<48:parts<24;
                if(enable!=b.Enabled)return Order("Verarbeitung am Bedarf ausrichten",enable?"Bauteilreserve fällt unter 24; Verarbeitung wieder aufnehmen":"Bauteilvorrat reicht; Metallverbrauch und Transportbedarf senken",new SimCommand {Kind=CommandKind.SetBuildingEnabled,PlanetId=w.Id,TargetId=b.Id,BoolValue=enable},"Weitere Werkstatt würde Material- und Transportengpass verschärfen");
            }
            // Four couriers are the actual local limit. Stop optional consumption instead of inventing vehicles.
            var optional=w.Buildings.Find(b=>Own(b,v)&&b.Built&&b.Kind==BuildingKind.ElectronicsFactory);
            if(optional!=null)
            {
                bool congested=w.Shipments>=Definitions.CouriersPerColony&&w.PendingDeliveries>=12;
                if(optional.Enabled&&congested)return Order("Transportengpass entlasten","Alle lokalen Kuriere belegt und mindestens zwölf offene Lieferungen",new SimCommand {Kind=CommandKind.SetBuildingEnabled,PlanetId=w.Id,TargetId=optional.Id,BoolValue=false},"Zusätzliche Produktion erhöht den Lieferstau");
                if(!optional.Enabled&&w.PendingDeliveries<4)return Order("Verarbeitung fortsetzen","Lieferstau abgebaut",new SimCommand {Kind=CommandKind.SetBuildingEnabled,PlanetId=w.Id,TargetId=optional.Id,BoolValue=true});
            }
            if(w.Buildings.FindAll(b=>Own(b,v)&&!b.Built).Count>=2)return null;
            var desired=new List<BuildingKind>();
            if(Count(w,v,BuildingKind.Farm)<Math.Max(1,(w.Population+39)/40))desired.Add(BuildingKind.Farm);
            if(w.EnergyDemand>w.Energy-2 && !w.Buildings.Exists(b=>Own(b,v)&&b.Kind==BuildingKind.SolarPlant&&!b.Built))desired.Add(BuildingKind.SolarPlant);
            if(Count(w,v,BuildingKind.Mine)==0)desired.Add(BuildingKind.Mine);
            if(Count(w,v,BuildingKind.Smelter)>=2 && Count(w,v,BuildingKind.Mine)<2 && Goods(w,v,1)<16)desired.Add(BuildingKind.Mine);
            if(Count(w,v,BuildingKind.Smelter)==0)desired.Add(BuildingKind.Smelter);
            if(Count(w,v,BuildingKind.Workshop)==0)desired.Add(BuildingKind.Workshop);
            int workers=0;foreach(var b in w.Buildings)if(Own(b,v))workers+=Definitions.Get(b.Kind).WorkerUse;
            if(w.Housing<Math.Max(28,(int)Math.Ceiling((workers+6)/Definitions.WorkforceFraction)) || w.Population>=w.Housing && w.Housing<52)desired.Add(BuildingKind.Housing);
            if(Count(w,v,BuildingKind.Smelter)<3 && Count(w,v,BuildingKind.Workshop)>0 && Goods(w,v,2)<100)desired.Add(BuildingKind.Smelter);
            if(ai.Profile==AiProfile.Technology&&Count(w,v,BuildingKind.Laboratory)==0)desired.Add(BuildingKind.Laboratory);
            if(Count(w,v,BuildingKind.Warehouse)==0)desired.Add(BuildingKind.Warehouse);
            if(Count(w,v,BuildingKind.Laboratory)==0)desired.Add(BuildingKind.Laboratory);
            if(Count(w,v,BuildingKind.Spaceport)==0)desired.Add(BuildingKind.Spaceport);
            foreach(var kind in desired)
            {
                bool essential=kind==BuildingKind.Farm||kind==BuildingKind.SolarPlant||kind==BuildingKind.Mine||kind==BuildingKind.Smelter||kind==BuildingKind.Workshop||kind==BuildingKind.Housing;
                string reason=kind==BuildingKind.Farm?"Nahrungsbedarf der eigenen Bevölkerung":kind==BuildingKind.SolarPlant?"Energiebedarf über verfügbarer Erzeugung":kind==BuildingKind.Housing?"Wohnraum oder zukünftige Arbeitskräfte fehlen":kind==BuildingKind.Mine?"Zu wenig erreichbare Erzgewinnung":kind==BuildingKind.Smelter?"Metallvorrat und Verarbeitungskapazität zu gering":kind==BuildingKind.Workshop?"Eigene Bauteilverarbeitung fehlt":"Fehlende Infrastruktur für den nächsten Wirtschaftsauftrag";
                var build=Build(v,ai,w,kind,reason,essential);if(build!=null)return build;
            }
            foreach(var b in w.Buildings) if(Own(b,v)&&b.Built && b.Kind==BuildingKind.Farm && b.Priority!=2)
                return Order("Grundversorgung schützen","Produktion vor optionalem Militärverbrauch priorisieren",new SimCommand {Kind=CommandKind.SetBuildingPriority,PlanetId=w.Id,TargetId=b.Id,IntValue=2});
            var lab=w.Buildings.Find(b=>Own(b,v)&&b.Kind==BuildingKind.Laboratory&&b.Built);
            if(lab!=null && v.Research.Active<0)
                foreach(var technology in Definitions.Technologies)
                {
                    if(v.Research.Completed.Contains(technology.Id))continue;bool ready=true;foreach(var prerequisite in technology.Prerequisites)if(!v.Research.Completed.Contains(prerequisite))ready=false;
                    for(int r=0;r<6;r++)if(Goods(w,v,r)<technology.Cost[r])ready=false;
                    if(ready)return Order("Forschung: "+technology.Name,"Voraussetzungen und reale Materialproben vorhanden",new SimCommand {Kind=CommandKind.StartResearch,PlanetId=w.Id,TargetId=lab.Id,IntValue=(int)technology.Id});
                }
            if(v.Research.Completed.Contains(Technology.CrystalMining) && Array.Exists(w.Terrain,t=>t==3) && Count(w,v,BuildingKind.CrystalMine)==0)
            {var action=Build(v,ai,w,BuildingKind.CrystalMine,"Eigenes Kristallvorkommen erschließen");if(action!=null)return action;}
            if(v.Research.Completed.Contains(Technology.Electronics)&&Count(w,v,BuildingKind.ElectronicsFactory)==0)
            {var action=Build(v,ai,w,BuildingKind.ElectronicsFactory,"Forschungsbedarf an Elektronik");if(action!=null)return action;}
            if(v.Enemies.Count>0)
            {
                foreach(var kind in new[]{BuildingKind.Barracks,BuildingKind.MilitaryDepot,BuildingKind.GunTower,BuildingKind.VehicleFactory})
                    if(Count(w,v,kind)==0){var build=Build(v,ai,w,kind,"Versorgte Heimatverteidigung und Operationsvorbereitung");if(build!=null)return build;}
                foreach(var b in w.Buildings)if(Own(b,v)&&b.Built&&b.RecruitKind<0 && w.Population>18)
                {
                    var troops=new List<UnitState>(w.Units.FindAll(u=>u.OwnerId==v.FactionId));foreach(var ship in v.Ships)if(ship.OwnerId==v.FactionId)troops.AddRange(ship.Troops);
                    UnitKind kind=UnitKind.Infantry;
                    if(b.Kind==BuildingKind.Barracks){if(troops.FindAll(u=>u.Kind==UnitKind.Infantry).Count>=3)continue;}
                    else if(b.Kind==BuildingKind.VehicleFactory){kind=troops.Exists(u=>u.Kind==UnitKind.Engineer)?UnitKind.Artillery:UnitKind.Engineer;if(troops.Exists(u=>u.Kind==kind))continue;}
                    else continue;
                    return Order("Reale Truppen beschaffen","Mobile Reserve, Infanterie, Pionier und Belagerung statt kostenloser Angriffswelle",new SimCommand {Kind=CommandKind.RecruitUnit,PlanetId=w.Id,TargetId=b.Id,IntValue=(int)kind});
                }
            }
            return null;
        }
    }
}
