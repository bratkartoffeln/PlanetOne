using UnityEngine;
using PlanetOne.Core;
namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private static readonly Color Hull=new Color(.82f,.80f,.74f),Steel=new Color(.22f,.26f,.29f),Trim=new Color(.23f,.39f,.50f),Industrial=new Color(.91f,.54f,.14f);
        private static readonly Color Canopy=new Color(.055f,.17f,.23f);
        private Material domeGlass;
        private Transform Part(Transform root,string name,Vector3 position,Vector3 size,Color color,PrimitiveType type=PrimitiveType.Cube)
        {return MakePrimitive(type,name,root,position,size,color);}
        private void Rod(Transform root,string name,Vector3 a,Vector3 b,float radius,Color color)
        {var part=Part(root,name,(a+b)*0.5f,new Vector3(radius,(b-a).magnitude*0.5f,radius),color,PrimitiveType.Cylinder);part.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        private void Ring(Transform root,Vector3 center,float radius,Color color,int sides=16,float thickness=0.025f)
        {for(int i=0;i<sides;i++){float a=i*2*Mathf.PI/sides,b=(i+1)*2*Mathf.PI/sides;Rod(root,"Rahmen",center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius,center+new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*radius,thickness,color);}}
        private void Module(Transform root,Vector3 at,Vector3 size,Color color)
        {
            ArmourPanel(root,"Modulschale",at,size,color);
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
            shell.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Part(root,"Kulturboden",at+Vector3.up*radius*.09f,new Vector3(radius*1.78f,radius*.08f,radius*1.78f),new Color(.23f,.31f,.07f),PrimitiveType.Cylinder);
            Ring(root,at,radius,Hull,20,0.018f);
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI/4;
                var foot=at+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
                var apex=at+Vector3.up*radius*.85f;
                var shoulder=at+new Vector3(Mathf.Cos(angle+.19f)*.78f,.53f,Mathf.Sin(angle+.19f)*.78f)*radius;
                var next=at+new Vector3(Mathf.Cos(angle+Mathf.PI/4+.19f)*.78f,.53f,Mathf.Sin(angle+Mathf.PI/4+.19f)*.78f)*radius;
                var ribColor=new Color(.75f,.70f,.52f);
                Rod(root,"Kuppelstrebe",foot,shoulder,.012f,ribColor);
                Rod(root,"Kuppelstrebe",shoulder,next,.010f,ribColor);
                Rod(root,"Kuppelstrebe",shoulder,apex,.012f,ribColor);
            }
            for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++)
            {
                float crop=((i+1)*3+j+1)%4*.24f;
                var leaf=Color.Lerp(new Color(.14f,.38f,.045f),new Color(.53f,.64f,.09f),crop);
                Part(root,"Pflanzreihe",at+new Vector3(i*.40f,.30f+crop*.10f,j*.40f)*radius,
                    new Vector3(.46f,.56f+crop*.14f,.52f)*radius,leaf,PrimitiveType.Sphere);
            }
        }
        private void BuildDetailedModel(Transform root,BuildingKind kind)
        { BuildColonyModel(root,kind); }
        private void BuildShipModel(Transform root,ShipKind kind)
        {
            bool heavy=kind==ShipKind.EscortFrigate || kind==ShipKind.LandingTransport || kind==ShipKind.ColonyShip;
            bool cargo=kind==ShipKind.Freighter||kind==ShipKind.LandingTransport;
            bool scout=kind==ShipKind.Scout||kind==ShipKind.SurveyShip;
            float beam=cargo?.38f:heavy?.65f:.45f;
            HullPart(root,Vector3.zero,new Vector3(beam,.22f,heavy?1.5f:1.1f),cargo?Steel:Hull);
            HullPart(root,new Vector3(0,.115f,.11f),new Vector3(beam*.84f,.11f,scout?.65f:.48f),Hull);
            HullPart(root,new Vector3(0,.18f,scout?.19f:-.12f),new Vector3(.23f,.12f,scout?.48f:.34f),Canopy);
            Part(root,"Fenster",new Vector3(0,0.22f,0.07f),new Vector3(0.18f,0.045f,0.025f),Cyan);
            for(int side=-1;side<=1;side+=2)
            {
                if(scout)
                {
                    ScoutWing(root,side);
                    var stripe=Part(root,"Flügelmarkierung",new Vector3(side*.365f,.031f,-.274f),new Vector3(.075f,.01f,.022f),Industrial);
                    stripe.localRotation=Quaternion.Euler(0,side*18,0);
                }
                else
                    HullPart(root,new Vector3(side*(cargo?.21f:.31f),-.02f,-.16f),
                        new Vector3(cargo?.13f:.21f,.14f,kind==ShipKind.EscortFrigate?.92f:.64f),cargo?Steel:Hull);
                Rod(root,"Triebwerk",new Vector3(side*0.26f,0,-0.24f),new Vector3(side*0.26f,0,-0.57f),0.12f,Steel);
                Rod(root,"Düsenkragen",new Vector3(side*.26f,0,-.53f),new Vector3(side*.26f,0,-.61f),.17f,Industrial);
                Part(root,"Antrieb",new Vector3(side*.26f,0,-.65f),new Vector3(.13f,.13f,.21f),new Color(.15f,.75f,1),PrimitiveType.Sphere);
                for(int j=0;j<3;j++)ArmourPanel(root,"Deckschulter",new Vector3(side*beam*.31f,.095f,-.31f+j*.19f),new Vector3(beam*.24f,.055f,.145f),j==0?Trim:Hull);
            }
            if(cargo)for(int i=-1;i<=1;i++)for(int side=-1;side<=1;side+=2)
            {
                var at=new Vector3(side*.135f,.16f,i*.245f);
                float height=kind==ShipKind.Freighter?.22f:.16f;
                ArmourPanel(root,"Frachtmodul",at,new Vector3(.25f,height,.22f),Industrial);
                ArmourPanel(root,"Containerdeckel",at+Vector3.up*height*.5f,new Vector3(.21f,.024f,.18f),Hull);
                Part(root,"Laschgurt",at+Vector3.up*(height*.5f+.016f),new Vector3(.19f,.018f,.036f),Steel);
            }
            if(kind==ShipKind.ColonyShip){Module(root,new Vector3(0,0.2f,0.3f),new Vector3(0.42f,0.28f,0.5f),Hull);Dome(root,new Vector3(0,0.34f,0.3f),0.17f);}
            if(kind==ShipKind.SurveyShip)
            {
                Rod(root,"Sensormast",new Vector3(0,.15f,-.22f),new Vector3(0,.36f,-.22f),.055f,Steel);
                var array=Part(root,"Messschüssel",new Vector3(0,.37f,-.22f),new Vector3(.42f,.09f,.42f),Hull,PrimitiveType.Sphere);
                array.localRotation=Quaternion.Euler(22,0,0);
            }
            if(kind==ShipKind.LandingTransport)ArmourPanel(root,"Landeschleuse",new Vector3(0,.045f,.52f),new Vector3(.44f,.23f,.37f),Industrial);
            if(kind==ShipKind.TorpedoShip)for(int side=-1;side<=1;side+=2)
                ArmourPanel(root,"Seitengondel",new Vector3(side*.29f,.09f,.05f),new Vector3(.18f,.19f,.71f),Trim);
            if(kind==ShipKind.Corvette || kind==ShipKind.EscortFrigate || kind==ShipKind.TorpedoShip || kind==ShipKind.Scout)
            {
                var turret=new GameObject("Turret").transform;turret.SetParent(root,false);turret.localPosition=new Vector3(0,.16f,.2f);
                float barrel=kind==ShipKind.TorpedoShip?.6f:.35f;
                for(int side=-1;side<=1;side+=2)
                {HullPart(turret,new Vector3(side*.18f,0,0),new Vector3(scout?.075f:.15f,scout?.055f:.13f,.3f),Steel);Rod(turret,"Geschützrohr",new Vector3(side*.18f,.02f,0),new Vector3(side*.18f,.02f,barrel),kind==ShipKind.TorpedoShip?.09f:.035f,Hull);}
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
                var rig=root.gameObject.AddComponent<InfantryStride>();
                rig.Torso=ColonyGroup(root,"Oberkörper",Vector3.up*.20f);
                ArmourPanel(rig.Torso,"Brustpanzer",Vector3.up*.09f,new Vector3(.19f,.21f,.15f),Hull);
                ArmourPanel(rig.Torso,"Tornister",new Vector3(0,.09f,-.10f),new Vector3(.15f,.17f,.08f),Steel);
                Part(rig.Torso,"Helm",Vector3.up*.275f,Vector3.one*.16f,Hull,PrimitiveType.Sphere);
                ArmourPanel(rig.Torso,"Visier",new Vector3(0,.275f,.064f),new Vector3(.14f,.058f,.045f),Canopy);
                for(int i=0;i<2;i++)
                {
                    float side=i*2-1;
                    var hip=rig.Hips[i]=ColonyGroup(root,"Hüfte",new Vector3(side*.057f,.216f,0));
                    Rod(hip,"Oberschenkel",Vector3.zero,Vector3.down*.10f,.069f,Steel);
                    var knee=rig.Knees[i]=ColonyGroup(hip,"Knie",Vector3.down*.10f);
                    Rod(knee,"Unterschenkel",Vector3.zero,Vector3.down*.10f,.063f,Hull);
                    rig.Feet[i]=ArmourPanel(knee,"Stiefel",new Vector3(0,-.093f,.021f),new Vector3(.076f,.045f,.115f),Steel);
                    var arm=rig.Arms[i]=ColonyGroup(rig.Torso,"Schulter",new Vector3(side*.107f,.152f,0));
                    ArmourPanel(arm,"Schulterstück",Vector3.zero,new Vector3(.073f,.067f,.105f),faction);
                    Rod(arm,"Oberarm",Vector3.zero,new Vector3(side*.014f,-.079f,0),.046f,Hull);
                    Rod(arm,"Unterarm",new Vector3(side*.014f,-.079f,0),new Vector3(side*.002f,-.098f,.075f),.043f,Steel);
                }
                var hand=rig.Arms[1];var grip=new Vector3(.005f,-.098f,.07f);
                Rod(hand,"Waffe",grip,grip+Vector3.forward*.23f,kind==UnitKind.AntiTank?.08f:.035f,Steel);
                rig.BarrelTip=ColonyGroup(hand,"Laufende",grip+Vector3.forward*.23f);
                rig.Muzzle=ColonyGroup(root,"MuzzlePosition",root.InverseTransformPoint(rig.BarrelTip.position));
                return;
            }
            HullPart(root,new Vector3(0,0.23f,0),new Vector3(0.46f,0.22f,0.65f),Hull);
            for(int side=-1;side<=1;side+=2)
            {
                HullPart(root,new Vector3(side*0.25f,0.13f,0),new Vector3(0.14f,0.18f,0.71f),Steel);
                for(int wheel=-1;wheel<=1;wheel++)
                {var w=Part(root,"Laufrad",new Vector3(side*0.32f,0.13f,wheel*0.22f),new Vector3(0.15f,0.025f,0.15f),Trim,PrimitiveType.Cylinder);w.localRotation=Quaternion.Euler(0,0,90);}
                ArmourPanel(root,"Kettenschürze",new Vector3(side*.245f,.22f,-.02f),new Vector3(.16f,.12f,.68f),Hull);
                ArmourPanel(root,"Kettenband",new Vector3(side*.26f,.065f,-.015f),new Vector3(.145f,.068f,.66f),Steel);
                Part(root,"Fraktionsmarkierung",new Vector3(side*.246f,.285f,.12f),new Vector3(.093f,.011f,.16f),faction);
                ArmourPanel(root,"Wartungsluke",new Vector3(side*.13f,.315f,-.22f),new Vector3(.13f,.028f,.16f),Steel);
                Part(root,"Scheinwerfer",new Vector3(side*0.16f,0.24f,0.31f),Vector3.one*0.055f,Cyan,PrimitiveType.Sphere);
            }
            if(courier){Module(root,new Vector3(0,.39f,.18f),new Vector3(.32f,.20f,.20f),Hull);ArmourPanel(root,"Ladebehälter",new Vector3(0,.4f,-.15f),new Vector3(.35f,.22f,.33f),Industrial);return;}
            if(kind==UnitKind.MobileWorkshop){WorkshopBody(root,faction);return;}
            if(kind==UnitKind.Engineer)
            {HullPart(root,new Vector3(0,.4f,0),new Vector3(.32f,.19f,.35f),faction);Rod(root,"Kranarm",new Vector3(-0.1f,0.4f,-0.2f),new Vector3(-0.1f,0.7f,0.15f),0.07f,Industrial);Rod(root,"Greifer",new Vector3(-0.1f,0.7f,0.15f),new Vector3(-0.1f,0.4f,0.3f),0.04f,Steel);return;}
            var weaponPivot=new GameObject("Turret").transform;weaponPivot.SetParent(root,false);weaponPivot.localPosition=new Vector3(0,.4f,0);
            ArmourPanel(weaponPivot,"Turmwanne",Vector3.zero,new Vector3(.37f,.17f,.38f),Steel);
            ArmourPanel(weaponPivot,"Turmpanzerung",Vector3.up*.064f,new Vector3(.34f,.13f,.33f),Hull);
            Part(weaponPivot,"Kennstreifen",new Vector3(-.09f,.133f,0),new Vector3(.045f,.012f,.19f),faction);
            ArmourPanel(weaponPivot,"Wartungsluke",new Vector3(.065f,.135f,-.043f),new Vector3(.109f,.022f,.135f),Steel);
            if(kind==UnitKind.AntiAir)
                ArmourPanel(weaponPivot,"Suchradar",new Vector3(0,.18f,-.11f),new Vector3(.24f,.12f,.06f),Trim);
            if(kind==UnitKind.Artillery)
                ArmourPanel(weaponPivot,"Rohrwiege",new Vector3(0,.12f,.23f),new Vector3(.13f,.105f,.32f),Industrial);
            Vector3 muzzlePosition=kind==UnitKind.AntiAir?new Vector3(.1f,.3f,.38f):new Vector3(0,kind==UnitKind.Artillery?.2f:.05f,kind==UnitKind.Artillery?.73f:.52f);
            if(kind==UnitKind.AntiAir)
                for(int side=-1;side<=1;side+=2)Rod(weaponPivot,"Flakrohr",new Vector3(side*.1f,.06f,0),new Vector3(side*.1f,.3f,.38f),.04f,Steel);
            else Rod(weaponPivot,"Kanone",new Vector3(0,.05f,.1f),muzzlePosition,.055f,Steel);
            var weaponMuzzle=new GameObject("MuzzlePosition").transform;weaponMuzzle.SetParent(weaponPivot,false);weaponMuzzle.localPosition=muzzlePosition;
        }
    }
}
