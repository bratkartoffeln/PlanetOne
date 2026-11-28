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
        { BuildColonyModel(root,kind); }
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
            if(kind==ShipKind.Corvette || kind==ShipKind.EscortFrigate || kind==ShipKind.TorpedoShip || kind==ShipKind.Scout)
            {
                var turret=new GameObject("Turret").transform;turret.SetParent(root,false);turret.localPosition=new Vector3(0,.16f,.2f);
                float barrel=kind==ShipKind.TorpedoShip?.6f:.35f;
                for(int side=-1;side<=1;side+=2)
                {HullPart(turret,new Vector3(side*.18f,0,0),new Vector3(.15f,.13f,.3f),Steel);Rod(turret,"Geschützrohr",new Vector3(side*.18f,.02f,0),new Vector3(side*.18f,.02f,barrel),kind==ShipKind.TorpedoShip?.09f:.035f,Hull);}
                var muzzle=new GameObject("MuzzlePosition").transform;muzzle.SetParent(turret,false);muzzle.localPosition=new Vector3(.18f,.02f,barrel);
            }
            if(kind==ShipKind.OrbitalYard)
                for(int side=-1;side<=1;side+=2){Module(root,new Vector3(side*0.7f,0,0),new Vector3(0.18f,0.25f,1.8f),Hull);for(int j=-1;j<=1;j++)Rod(root,"Werftausleger",new Vector3(0,-0.1f,j*0.65f),new Vector3(side*0.7f,-0.1f,j*0.65f),0.08f,Industrial);}
        }

        private void BuildVehicleModel(Transform root,UnitKind kind,Color faction,bool courier=false)
        {
            if(AtmosphereSurface){BuildAirVehicle(root,kind,faction,courier);return;}
            if(!courier && (kind==UnitKind.Infantry || kind==UnitKind.AntiTank))
            {
                HullPart(root,new Vector3(0,0.29f,0),new Vector3(0.2f,0.23f,0.14f),faction);
                Part(root,"Helm",new Vector3(0,0.48f,0),Vector3.one*0.17f,Hull,PrimitiveType.Sphere);
                Part(root,"Visier",new Vector3(0,0.49f,0.075f),new Vector3(0.12f,0.045f,0.025f),Cyan);
                for(int side=-1;side<=1;side+=2)
                {Rod(root,"Bein",new Vector3(side*0.07f,0.05f,0.02f),new Vector3(side*0.06f,0.2f,0),0.075f,Steel);Rod(root,"Arm",new Vector3(side*0.13f,0.35f,0),new Vector3(side*0.1f,0.26f,0.1f),0.055f,Hull);}
                Rod(root,"Waffe",new Vector3(0.13f,0.3f,0),new Vector3(0.13f,0.3f,0.32f),kind==UnitKind.AntiTank?0.08f:0.035f,Steel);
                var muzzle=new GameObject("MuzzlePosition").transform;muzzle.SetParent(root,false);muzzle.localPosition=new Vector3(.13f,.3f,.32f);return;
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
            if(kind==UnitKind.Engineer)
            {HullPart(root,new Vector3(0,.4f,0),new Vector3(.32f,.19f,.35f),faction);Rod(root,"Kranarm",new Vector3(-0.1f,0.4f,-0.2f),new Vector3(-0.1f,0.7f,0.15f),0.07f,Industrial);Rod(root,"Greifer",new Vector3(-0.1f,0.7f,0.15f),new Vector3(-0.1f,0.4f,0.3f),0.04f,Steel);return;}
            var weaponPivot=new GameObject("Turret").transform;weaponPivot.SetParent(root,false);weaponPivot.localPosition=new Vector3(0,.4f,0);
            HullPart(weaponPivot,Vector3.zero,new Vector3(.32f,.19f,.35f),faction);
            Vector3 muzzlePosition=kind==UnitKind.AntiAir?new Vector3(.1f,.3f,.38f):new Vector3(0,kind==UnitKind.Artillery?.2f:.05f,kind==UnitKind.Artillery?.73f:.52f);
            if(kind==UnitKind.AntiAir)
                for(int side=-1;side<=1;side+=2)Rod(weaponPivot,"Flakrohr",new Vector3(side*.1f,.06f,0),new Vector3(side*.1f,.3f,.38f),.04f,Steel);
            else Rod(weaponPivot,"Kanone",new Vector3(0,.05f,.1f),muzzlePosition,.055f,Steel);
            var weaponMuzzle=new GameObject("MuzzlePosition").transform;weaponMuzzle.SetParent(weaponPivot,false);weaponMuzzle.localPosition=muzzlePosition;
        }
    }
}
