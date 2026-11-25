using UnityEngine;
using PlanetOne.Core;
namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private static readonly Color Hull=new Color(0.73f,0.76f,0.75f),Steel=new Color(0.25f,0.29f,0.31f),Trim=new Color(0.18f,0.39f,0.51f),Industrial=new Color(0.83f,0.52f,0.18f);
        private Material domeGlass;
        private Transform Part(Transform root,string name,Vector3 position,Vector3 size,Color color,PrimitiveType type=PrimitiveType.Cube)
        {return MakePrimitive(type,name,root,position,size,color);}
        private void Rod(Transform root,string name,Vector3 a,Vector3 b,float radius,Color color)
        {var part=Part(root,name,(a+b)*0.5f,new Vector3(radius,(b-a).magnitude*0.5f,radius),color,PrimitiveType.Cylinder);part.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        private void Ring(Transform root,Vector3 center,float radius,Color color,int sides=16,float thickness=0.025f)
        {for(int i=0;i<sides;i++){float a=i*2*Mathf.PI/sides,b=(i+1)*2*Mathf.PI/sides;Rod(root,"Rahmen",center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius,center+new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*radius,thickness,color);}}
        private void Module(Transform root,Vector3 at,Vector3 size,Color color)
        {
            Part(root,"Dach",at,size,color);
            Part(root,"Panzerabschluss",at+Vector3.up*size.y*0.48f,new Vector3(size.x*0.91f,0.035f,size.z*0.93f),Hull);
            for(int side=-1;side<=1;side+=2)
            {
                Part(root,"Fenster",at+new Vector3(0,0.03f,side*(size.z*0.5f+0.004f)),new Vector3(size.x*0.72f,0.045f,0.009f),Cyan);
                for(int i=-1;i<=1;i+=2)Part(root,"Stützrahmen",at+new Vector3(i*size.x*0.41f,-size.y*0.1f,side*(size.z*0.5f+0.012f)),new Vector3(0.035f,size.y*0.8f,0.035f),Steel);
            }
        }
        private void Dome(Transform root,Vector3 at,float radius)
        {
            // Glass shell is true curved geometry, with plants and support ribs inside it.
            var shell=Part(root,"Gewächshausglas",at,new Vector3(radius*2,radius*1.7f,radius*2),Color.white,PrimitiveType.Sphere);
            if(domeGlass==null){domeGlass=new Material(Resources.Load<Shader>("HabitatGlass"));}
            shell.GetComponent<Renderer>().sharedMaterial=domeGlass;
            Ring(root,at,radius,Hull,20,0.018f);
            for(int axis=0;axis<4;axis++)for(int step=0;step<8;step++)
            {
                float a=step*Mathf.PI/8,b=(step+1)*Mathf.PI/8,heading=axis*Mathf.PI/4;
                Vector3 dir=new Vector3(Mathf.Cos(heading),0,Mathf.Sin(heading));
                Rod(root,"Kuppelstrebe",at+dir*Mathf.Cos(a)*radius+Vector3.up*Mathf.Sin(a)*radius*0.85f,at+dir*Mathf.Cos(b)*radius+Vector3.up*Mathf.Sin(b)*radius*0.85f,0.012f,Hull);
            }
            for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++)
                Part(root,"Pflanzreihe",at+new Vector3(i*radius*0.42f,0.03f,j*radius*0.42f),new Vector3(radius*0.24f,0.08f,radius*0.24f),new Color(0.22f,0.45f,0.09f),PrimitiveType.Sphere);
        }
        private void BuildDetailedModel(Transform root,BuildingKind kind)
        {
            MakeHex("Fundament",root,new Vector3(0,0.03f,0),new Vector3(0.89f,0.06f,0.89f),Steel);
            switch(kind)
            {
                case BuildingKind.Headquarters:
                    Part(root,"Zentrale",new Vector3(0,0.22f,0),new Vector3(0.64f,0.18f,0.64f),Hull,PrimitiveType.Cylinder);
                    Ring(root,new Vector3(0,0.39f,0),0.30f,Trim,8,0.055f);
                    Part(root,"Kommandoturm",new Vector3(0,0.46f,0),new Vector3(0.27f,0.17f,0.27f),Hull,PrimitiveType.Cylinder);
                    Ring(root,new Vector3(0,0.58f,0),0.13f,Industrial,12,0.02f);
                    Rod(root,"Antenne",new Vector3(0,0.59f,0),new Vector3(0,0.93f,0),0.025f,Steel);
                    Part(root,"Betriebsanzeige",new Vector3(0,0.94f,0),Vector3.one*0.04f,Amber,PrimitiveType.Sphere);
                    for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var pod=new GameObject("Kommandomodul").transform;pod.SetParent(root,false);pod.localPosition=new Vector3(Mathf.Sin(a)*0.31f,0,Mathf.Cos(a)*0.31f);pod.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);Module(pod,new Vector3(0,0.14f,0),new Vector3(0.22f,0.2f,0.2f),Hull);}break;
                case BuildingKind.SolarPlant:
                    for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                    {var pos=new Vector3(x*0.22f,0.27f,z*0.21f);Rod(root,"Solarstütze",pos-Vector3.up*0.2f,pos,0.035f,Steel);Part(root,"Solarrahmen",pos,new Vector3(0.4f,0.035f,0.32f),Hull).localRotation=Quaternion.Euler(-22,0,0);Part(root,"Solarfläche",pos+Vector3.up*0.02f,new Vector3(0.37f,0.015f,0.29f),Color.white).localRotation=Quaternion.Euler(-22,0,0);}break;
                case BuildingKind.Farm:
                    Dome(root,new Vector3(-0.18f,0.12f,0.12f),0.25f);Dome(root,new Vector3(0.2f,0.1f,-0.16f),0.23f);break;
                case BuildingKind.Smelter:case BuildingKind.Workshop:case BuildingKind.ElectronicsFactory:
                    Module(root,new Vector3(0,0.16f,0),new Vector3(0.64f,0.23f,0.54f),Hull);
                    for(int i=0;i<3;i++)
                    {float x=-0.23f+i*0.23f,h=i==1?0.72f:0.52f;Part(root,"Reaktortank",new Vector3(x,h*0.5f,0.12f),new Vector3(0.17f,h*0.5f,0.17f),Hull,PrimitiveType.Cylinder);Ring(root,new Vector3(x,h*0.78f,0.12f),0.09f,Industrial,12,0.022f);Rod(root,"Rohrleitung",new Vector3(x,0.15f,-0.27f),new Vector3(x,h*0.7f,-0.27f),0.025f,Industrial);Rod(root,"Rohrleitung",new Vector3(x,h*0.7f,-0.27f),new Vector3(x,h*0.7f,0.12f),0.025f,Industrial);}break;
                case BuildingKind.Spaceport:
                    Part(root,"Landefläche",new Vector3(0,0.08f,-0.12f),new Vector3(0.85f,0.035f,0.60f),Steel);
                    Ring(root,new Vector3(0,0.105f,-0.18f),0.22f,Industrial,12,0.015f);
                    Module(root,new Vector3(0.1f,0.23f,0.26f),new Vector3(0.48f,0.35f,0.27f),Hull);
                    Part(root,"Hangartor",new Vector3(0.1f,0.20f,0.115f),new Vector3(0.35f,0.25f,0.015f),Steel);
                    Rod(root,"Landebake",new Vector3(-0.3f,0,0.3f),new Vector3(-0.3f,0.68f,0.3f),0.025f,Hull);break;
                case BuildingKind.Mine:case BuildingKind.CrystalMine:
                    Module(root,new Vector3(0.15f,0.14f,-0.2f),new Vector3(0.4f,0.2f,0.26f),Hull);
                    for(int side=-1;side<=1;side+=2)Rod(root,"Fördermast",new Vector3(side*0.25f,0.04f,0.18f),new Vector3(side*0.16f,0.68f,0.18f),0.06f,Industrial);
                    Rod(root,"Förderbrücke",new Vector3(-0.23f,0.65f,0.18f),new Vector3(0.23f,0.65f,0.18f),0.05f,Hull);
                    Rod(root,"Bohrgestänge",new Vector3(0,0.05f,0.18f),new Vector3(0,0.72f,0.18f),0.05f,Steel);break;
                case BuildingKind.Housing:case BuildingKind.Barracks:
                    for(int i=-1;i<=1;i++)Module(root,new Vector3(i*0.23f,0.18f,i*0.06f),new Vector3(0.2f,0.28f,0.48f),Hull);break;
                case BuildingKind.Laboratory:case BuildingKind.ShieldGenerator:
                    Part(root,"Laborbasis",new Vector3(0,0.17f,0),new Vector3(0.6f,0.12f,0.6f),Hull,PrimitiveType.Cylinder);Dome(root,new Vector3(0,0.3f,0),0.26f);
                    for(int i=0;i<4;i++){float a=i*Mathf.PI*0.5f;Rod(root,"Feldspule",new Vector3(Mathf.Cos(a)*0.32f,0,Mathf.Sin(a)*0.32f),new Vector3(Mathf.Cos(a)*0.32f,0.57f,Mathf.Sin(a)*0.32f),0.03f,Trim);}break;
                case BuildingKind.JumpGate:
                    for(int i=0;i<12;i++){float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;Rod(root,"Torrahmen",new Vector3(Mathf.Cos(a)*0.35f,0.48f+Mathf.Sin(a)*0.35f,0),new Vector3(Mathf.Cos(b)*0.35f,0.48f+Mathf.Sin(b)*0.35f,0),0.09f,Hull);Rod(root,"Betriebsanzeige",new Vector3(Mathf.Cos(a)*0.29f,0.48f+Mathf.Sin(a)*0.29f,-0.02f),new Vector3(Mathf.Cos(b)*0.29f,0.48f+Mathf.Sin(b)*0.29f,-0.02f),0.025f,Cyan);}break;
                case BuildingKind.GunTower:case BuildingKind.AntiTankTower:case BuildingKind.FlakTower:case BuildingKind.ArtilleryTower:case BuildingKind.OrbitalBattery:
                    Part(root,"Geschützsockel",new Vector3(0,0.17f,0),new Vector3(0.56f,0.13f,0.56f),Hull,PrimitiveType.Cylinder);Module(root,new Vector3(0,0.37f,0),new Vector3(0.37f,0.18f,0.35f),Hull);
                    for(int i=-1;i<=1;i+=2)Rod(root,"Geschützrohr",new Vector3(i*0.09f,0.42f,-0.1f),new Vector3(i*0.09f,kind==BuildingKind.FlakTower?0.72f:0.46f,-0.53f),0.04f,Steel);break;
                case BuildingKind.Wall:Part(root,"Mauer",new Vector3(0,0.22f,0),new Vector3(0.85f,0.44f,0.25f),Hull);break;
                case BuildingKind.Gate:
                    for(int side=-1;side<=1;side+=2)Module(root,new Vector3(side*0.33f,0.25f,0),new Vector3(0.18f,0.5f,0.25f),Hull);Part(root,"Tor",new Vector3(0,0.53f,0),new Vector3(0.8f,0.1f,0.25f),Trim);break;
                default:
                    Module(root,new Vector3(0,0.22f,0),new Vector3(0.7f,0.36f,0.63f),Hull);
                    for(int i=-1;i<=1;i++)Module(root,new Vector3(i*0.23f,0.48f,0.08f),new Vector3(0.19f,0.13f,0.3f),kind==BuildingKind.MilitaryDepot?Industrial:Trim);break;
            }
        }
        private void BuildShipModel(Transform root,ShipKind kind)
        {
            bool heavy=kind==ShipKind.EscortFrigate || kind==ShipKind.LandingTransport || kind==ShipKind.ColonyShip;
            HullPart(root,Vector3.zero,new Vector3(heavy?0.65f:0.45f,0.20f,heavy?1.5f:1.1f),Hull);
            HullPart(root,new Vector3(0,0.15f,-0.12f),new Vector3(0.25f,0.18f,0.5f),Trim);
            Part(root,"Fenster",new Vector3(0,0.22f,0.07f),new Vector3(0.18f,0.045f,0.025f),Cyan);
            for(int side=-1;side<=1;side+=2)
            {
                HullPart(root,new Vector3(side*0.33f,-0.02f,-0.16f),new Vector3(0.28f,0.10f,0.65f),Hull);
                Rod(root,"Triebwerk",new Vector3(side*0.26f,0,-0.24f),new Vector3(side*0.26f,0,-0.57f),0.12f,Steel);
                Part(root,"Antrieb",new Vector3(side*0.26f,0,-0.65f),new Vector3(0.09f,0.09f,0.22f),Cyan,PrimitiveType.Sphere);
                for(int j=0;j<4;j++)Part(root,"Rumpfspant",new Vector3(side*0.23f,0.08f,-0.3f+j*0.18f),new Vector3(0.045f,0.06f,0.035f),Industrial);
            }
            if(kind==ShipKind.Freighter || kind==ShipKind.LandingTransport)
                for(int i=-1;i<=1;i++)Part(root,"Frachtmodul",new Vector3(0,0.16f,i*0.25f),new Vector3(0.38f,0.18f,0.2f),Industrial);
            if(kind==ShipKind.ColonyShip){Module(root,new Vector3(0,0.2f,0.3f),new Vector3(0.42f,0.28f,0.5f),Hull);Dome(root,new Vector3(0,0.34f,0.3f),0.17f);}
            if(kind==ShipKind.SurveyShip || kind==ShipKind.Scout)
            {Rod(root,"Sensormast",new Vector3(0,0.15f,0),new Vector3(0,0.45f,0),0.025f,Hull);Part(root,"Sensor",new Vector3(0,0.46f,0),new Vector3(0.3f,0.06f,0.2f),Trim);}
            if(kind==ShipKind.Corvette || kind==ShipKind.EscortFrigate || kind==ShipKind.TorpedoShip)
                for(int side=-1;side<=1;side+=2){HullPart(root,new Vector3(side*0.18f,0.16f,0.2f),new Vector3(0.15f,0.13f,0.3f),Steel);Rod(root,"Geschützrohr",new Vector3(side*0.18f,0.18f,0.2f),new Vector3(side*0.18f,0.18f,kind==ShipKind.TorpedoShip?0.8f:0.55f),kind==ShipKind.TorpedoShip?0.09f:0.035f,Hull);}
            if(kind==ShipKind.OrbitalYard)
                for(int side=-1;side<=1;side+=2){Module(root,new Vector3(side*0.7f,0,0),new Vector3(0.18f,0.25f,1.8f),Hull);for(int j=-1;j<=1;j++)Rod(root,"Werftausleger",new Vector3(0,-0.1f,j*0.65f),new Vector3(side*0.7f,-0.1f,j*0.65f),0.08f,Industrial);}
        }

        private void BuildVehicleModel(Transform root,UnitKind kind,Color faction,bool courier=false)
        {
            if(!courier && (kind==UnitKind.Infantry || kind==UnitKind.AntiTank))
            {
                HullPart(root,new Vector3(0,0.29f,0),new Vector3(0.2f,0.23f,0.14f),faction);
                Part(root,"Helm",new Vector3(0,0.48f,0),Vector3.one*0.17f,Hull,PrimitiveType.Sphere);
                Part(root,"Visier",new Vector3(0,0.49f,0.075f),new Vector3(0.12f,0.045f,0.025f),Cyan);
                for(int side=-1;side<=1;side+=2)
                {Rod(root,"Bein",new Vector3(side*0.07f,0.05f,0.02f),new Vector3(side*0.06f,0.2f,0),0.075f,Steel);Rod(root,"Arm",new Vector3(side*0.13f,0.35f,0),new Vector3(side*0.1f,0.26f,0.1f),0.055f,Hull);}
                Rod(root,"Waffe",new Vector3(0.13f,0.3f,0),new Vector3(0.13f,0.3f,0.32f),kind==UnitKind.AntiTank?0.08f:0.035f,Steel);return;
            }
            HullPart(root,new Vector3(0,0.23f,0),new Vector3(0.46f,0.22f,0.65f),Hull);
            for(int side=-1;side<=1;side+=2)
            {
                HullPart(root,new Vector3(side*0.25f,0.13f,0),new Vector3(0.14f,0.18f,0.71f),Steel);
                for(int wheel=-1;wheel<=1;wheel++)
                {var w=Part(root,"Laufrad",new Vector3(side*0.32f,0.13f,wheel*0.22f),new Vector3(0.15f,0.025f,0.15f),Trim,PrimitiveType.Cylinder);w.localRotation=Quaternion.Euler(0,0,90);}
                Part(root,"Fraktionsmarkierung",new Vector3(side*0.245f,0.27f,0),new Vector3(0.015f,0.06f,0.3f),faction);
                Part(root,"Scheinwerfer",new Vector3(side*0.16f,0.24f,0.31f),Vector3.one*0.055f,Cyan,PrimitiveType.Sphere);
            }
            if(courier){Module(root,new Vector3(0,0.39f,0.18f),new Vector3(0.3f,0.20f,0.20f),Hull);Part(root,"Ladebehälter",new Vector3(0,0.4f,-0.15f),new Vector3(0.32f,0.20f,0.31f),Industrial);return;}
            HullPart(root,new Vector3(0,0.40f,0),new Vector3(0.32f,0.19f,0.35f),faction);
            if(kind==UnitKind.Engineer)
            {Rod(root,"Kranarm",new Vector3(-0.1f,0.4f,-0.2f),new Vector3(-0.1f,0.7f,0.15f),0.07f,Industrial);Rod(root,"Greifer",new Vector3(-0.1f,0.7f,0.15f),new Vector3(-0.1f,0.4f,0.3f),0.04f,Steel);}
            else if(kind==UnitKind.AntiAir)
                for(int side=-1;side<=1;side+=2)Rod(root,"Flakrohr",new Vector3(side*0.1f,0.46f,0),new Vector3(side*0.1f,0.7f,0.38f),0.04f,Steel);
            else Rod(root,"Kanone",new Vector3(0,0.45f,0.1f),new Vector3(0,kind==UnitKind.Artillery?0.6f:0.45f,kind==UnitKind.Artillery?0.73f:0.52f),0.055f,Steel);
        }
    }
}
