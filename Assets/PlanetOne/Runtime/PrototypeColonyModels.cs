using UnityEngine;
using PlanetOne.Core;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        // Each definition has its own silhouette. Shared materials and small details keep the colony coherent.
        private void BuildColonyModel(Transform root,BuildingKind kind)
        {
            if(!IsFortification(kind))MakeHex("Fundament",root,new Vector3(0,.025f,0),new Vector3(.91f,.05f,.91f),Steel);
            switch(kind)
            {
                case BuildingKind.Headquarters:ColonyHeadquarters(root);break;
                case BuildingKind.Mine:ColonyMine(root,false);break;
                case BuildingKind.CrystalMine:ColonyMine(root,true);break;
                case BuildingKind.GasExtractor:
                    var separator=ColonyGroup(root,"Gasabscheider",Vector3.zero);
                    ColonyTank(separator,new Vector3(0,.08f,0),.17f,.54f,Cyan);
                    for(int side=-1;side<=1;side+=2)
                    {
                        var intake=new Vector3(side*.30f,.21f,0);
                        ColonyTank(separator,intake,.13f,.20f,Trim);
                        Ring(separator,intake+Vector3.up*.22f,.15f,Cyan,12,.025f);
                        Part(separator,"Ansaugleitung",new Vector3(side*.18f,.27f,0),new Vector3(.22f,.065f,.08f),Steel);
                    }
                    break;
                case BuildingKind.Smelter:ColonySmelter(root);break;
                case BuildingKind.SolarPlant:ColonySolar(root);break;
                case BuildingKind.Warehouse:ColonyWarehouse(root);break;
                case BuildingKind.Workshop:ColonyWorkshop(root);break;
                case BuildingKind.ElectronicsFactory:ColonyElectronics(root);break;
                case BuildingKind.Farm:ColonyFarm(root);break;
                case BuildingKind.Housing:ColonyHousing(root);break;
                case BuildingKind.Barracks:ColonyBarracks(root);break;
                case BuildingKind.VehicleFactory:ColonyVehicleFactory(root);break;
                case BuildingKind.MilitaryDepot:ColonyDepot(root);break;
                case BuildingKind.Spaceport:ColonySpaceport(root);break;
                case BuildingKind.Laboratory:ColonyLaboratory(root);break;
                case BuildingKind.ShieldGenerator:ColonyShield(root);break;
                case BuildingKind.GunTower:ColonyTurret(root,kind);break;
                case BuildingKind.AntiTankTower:ColonyTurret(root,kind);break;
                case BuildingKind.FlakTower:ColonyTurret(root,kind);break;
                case BuildingKind.ArtilleryTower:ColonyTurret(root,kind);break;
                case BuildingKind.OrbitalBattery:ColonyTurret(root,kind);break;
                case BuildingKind.Wall:ColonyWall(root);break;
                case BuildingKind.Gate:ColonyGate(root);break;
                case BuildingKind.JumpGate:ColonyJumpGate(root);break;
            }
        }
        private Transform ColonyGroup(Transform parent,string name,Vector3 position)
        {var group=new GameObject(name).transform;group.SetParent(parent,false);group.localPosition=position;return group;}
        private void ColonyHall(Transform root,Vector3 at,Vector3 size,Color accent)
        {
            ArmourPanel(root,"Tragrahmen",at,size,Steel);
            ArmourPanel(root,"Panzerverkleidung",at+Vector3.up*size.y*.10f,new Vector3(size.x*1.035f,size.y*.88f,size.z*1.035f),Hull);
            ArmourPanel(root,"Dachkassette",at+Vector3.up*size.y*.56f,new Vector3(size.x*.60f,.025f,size.z*.62f),Trim);
            for(int side=-1;side<=1;side+=2)
            {
                ArmourPanel(root,"Seitenwange",at+new Vector3(side*size.x*.47f,0,0),new Vector3(size.x*.15f,size.y*.96f,size.z*.67f),accent);
                var lintel=at+new Vector3(0,size.y*.13f,side*size.z*.52f);
                Part(root,"Fensterschacht",lintel,new Vector3(size.x*.65f,size.y*.31f,.018f),Canopy);
                Part(root,"Fenster",lintel+new Vector3(0,size.y*.035f,side*.011f),new Vector3(size.x*.48f,size.y*.084f,.006f),Cyan);
            }
        }
        private void ColonyTank(Transform root,Vector3 at,float radius,float height,Color stripe)
        {
            Part(root,"Metalltank",at+Vector3.up*height*.5f,new Vector3(radius*2,height*.5f,radius*2),Hull,PrimitiveType.Cylinder);
            Ring(root,at+Vector3.up*height*.2f,radius*1.02f,Steel,12,.014f);
            Ring(root,at+Vector3.up*height*.78f,radius*1.02f,stripe,12,.018f);
            Part(root,"Tankdeckel",at+Vector3.up*(height+.015f),new Vector3(radius*1.8f,.015f,radius*1.8f),Steel,PrimitiveType.Cylinder);
            Part(root,"Tankmanschette",at+Vector3.up*height*.68f,new Vector3(radius*2.04f,height*.065f,radius*2.04f),stripe,PrimitiveType.Cylinder);
            Part(root,"Druckverschluss",at+Vector3.up*(height+.035f),new Vector3(radius,.022f,radius),Hull,PrimitiveType.Cylinder);
            Rod(root,"Steigleitung",at+new Vector3(radius*.94f,.02f,0),at+new Vector3(radius*.94f,height*.85f,0),.027f,stripe);
        }
        private void ColonyCrate(Transform root,Vector3 at,Vector3 size,Color accent)
        {
            ArmourPanel(root,"Frachtkiste",at,size,accent);
            for(int i=-1;i<=1;i+=2)Part(root,"Kistenrahmen",at+new Vector3(i*size.x*.37f,.005f,0),new Vector3(.018f,size.y*1.04f,size.z*1.04f),Steel);
        }
        private void ColonyHeadquarters(Transform root)
        {
            Part(root,"Kommandofundament",new Vector3(0,.12f,0),new Vector3(.78f,.085f,.78f),Steel,PrimitiveType.Cylinder);
            MakeHex("Kommandopanzer",root,new Vector3(0,.20f,0),new Vector3(.64f,.13f,.64f),Hull);
            Part(root,"Leitzentrale",new Vector3(0,.29f,0),new Vector3(.48f,.14f,.48f),Hull,PrimitiveType.Cylinder);
            Ring(root,new Vector3(0,.38f,0),.246f,Trim,8,.037f);
            Ring(root,new Vector3(0,.27f,0),.247f,Cyan,8,.014f);
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f;var pod=ColonyGroup(root,"Kommandoflügel",new Vector3(Mathf.Sin(a)*.29f,0,Mathf.Cos(a)*.29f));pod.localRotation=Quaternion.Euler(0,i*90,0);
                ColonyHall(pod,new Vector3(0,.13f,0),new Vector3(.24f,.2f,.22f),Trim);
            }
            Part(root,"Kommandospitze",new Vector3(0,.56f,0),new Vector3(.17f,.16f,.17f),Hull,PrimitiveType.Cylinder);
            for(int i=0;i<4;i++)
            {
                var mast=ColonyGroup(root,"Mastverkleidung",new Vector3(0,.56f,0));mast.localRotation=Quaternion.Euler(0,90*i,0);
                Part(mast,"Fensterschacht",new Vector3(0,0,.084f),new Vector3(.058f,.20f,.018f),Steel);
                Part(mast,"Mastanzeige",new Vector3(0,0,.095f),new Vector3(.018f,.15f,.008f),Cyan);
            }
            Rod(root,"Antenne",new Vector3(0,.67f,0),new Vector3(0,.98f,0),.023f,Industrial);
            Part(root,"Betriebsanzeige",new Vector3(0,1,0),Vector3.one*.045f,Amber,PrimitiveType.Sphere);
        }
        private void ColonySolar(Transform root)
        {
            ColonyHall(root,new Vector3(0,.1f,.31f),new Vector3(.22f,.12f,.13f),Trim);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            {
                var panel=ColonyGroup(root,"Nachgeführtes Solarfeld",new Vector3(x*.225f,.29f,z*.2f));panel.localRotation=Quaternion.Euler(-25,0,0);
                Rod(root,"Solarsäule",new Vector3(x*.225f,.05f,z*.2f),new Vector3(x*.225f,.26f,z*.2f),.034f,Steel);
                Part(panel,"Solarrahmen",Vector3.zero,new Vector3(.405f,.035f,.335f),Hull);
                Part(panel,"Solarfläche",new Vector3(0,.024f,0),new Vector3(.37f,.012f,.3f),Color.white);
                for(int i=-1;i<=1;i++)Part(panel,"Solarunterteilung",new Vector3(i*.092f,.032f,0),new Vector3(.005f,.006f,.3f),Hull);
            }
        }
        private void ColonyFarm(Transform root)
        {
            ColonyTank(root,new Vector3(-.31f,.04f,-.25f),.08f,.18f,Trim);
            ColonyGreenhouse(root,new Vector3(-.18f,.09f,.14f),.25f);
            ColonyGreenhouse(root,new Vector3(.21f,.09f,-.14f),.23f);
            Rod(root,"Bewässerungsrohr",new Vector3(-.28f,.07f,-.25f),new Vector3(.22f,.07f,-.22f),.022f,Trim);
        }
        private void ColonyGreenhouse(Transform root,Vector3 at,float radius)
        {
            Part(root,"Gewächshausbasis",at,new Vector3(radius*2,.035f,radius*2),Steel,PrimitiveType.Cylinder);
            Dome(root,at+Vector3.up*.025f,radius);
            for(int i=0;i<4;i++)
            {float a=i*Mathf.PI*.5f;Part(root,"Gewächshauszugang",at+new Vector3(Mathf.Sin(a)*radius,.035f,Mathf.Cos(a)*radius),new Vector3(.07f,.1f,.07f),Industrial);}
        }
        private void ColonySmelter(Transform root)
        {
            ColonyHall(root,new Vector3(.02f,.14f,-.21f),new Vector3(.64f,.2f,.23f),Industrial);
            for(int i=-1;i<=1;i++)ColonyTank(root,new Vector3(i*.235f,.05f,.11f),.105f,i==0?.67f:.43f,Industrial);
            for(int i=-1;i<=1;i+=2)
            {
                Rod(root,"Rohrleitung",new Vector3(i*.24f,.16f,-.31f),new Vector3(i*.24f,.56f,-.31f),.024f,Industrial);
                Rod(root,"Schornstein",new Vector3(i*.31f,.1f,.31f),new Vector3(i*.31f,.72f,.31f),.048f,Steel);
                Part(root,"Ofenanzeige",new Vector3(i*.16f,.17f,-.334f),new Vector3(.11f,.075f,.013f),Amber);
            }
        }
        private void ColonyWorkshop(Transform root)
        {
            ColonyHall(root,new Vector3(-.11f,.2f,0),new Vector3(.45f,.3f,.67f),Industrial);
            LoadingDoor(root,new Vector3(-.11f,.16f,.348f),.28f,.22f,1);
            for(int i=0;i<3;i++)ColonyCrate(root,new Vector3(.29f,.11f,-.23f+i*.2f),new Vector3(.16f,.14f,.14f),Industrial);
            Rod(root,"Werkstattkran",new Vector3(.25f,.05f,-.26f),new Vector3(.25f,.59f,-.26f),.035f,Industrial);
            Rod(root,"Werkstattkran",new Vector3(.25f,.59f,-.26f),new Vector3(-.14f,.59f,-.26f),.035f,Industrial);
            Part(root,"Dachlüfter",new Vector3(-.11f,.4f,0),new Vector3(.25f,.04f,.21f),Steel);
        }
        private void ColonyElectronics(Transform root)
        {
            ColonyHall(root,new Vector3(-.12f,.18f,0),new Vector3(.34f,.25f,.73f),Trim);
            ColonyHall(root,new Vector3(.23f,.13f,.1f),new Vector3(.23f,.17f,.48f),Trim);
            for(int i=-1;i<=1;i++)
            {
                Part(root,"Elektronikmodul",new Vector3(-.12f,.34f,i*.2f),new Vector3(.23f,.07f,.14f),Trim);
                Part(root,"Prozessanzeige",new Vector3(.237f,.24f,i*.12f+.08f),new Vector3(.12f,.018f,.05f),Cyan);
            }
            Rod(root,"Datenantenne",new Vector3(.24f,.2f,-.24f),new Vector3(.24f,.61f,-.24f),.022f,Hull);
            Part(root,"Datensensor",new Vector3(.24f,.6f,-.24f),new Vector3(.2f,.06f,.13f),Trim);
        }
        private void ColonyWarehouse(Transform root)
        {
            ColonyHall(root,new Vector3(0,.23f,-.04f),new Vector3(.75f,.36f,.54f),Steel);
            // Paired loading bays remain visible on the apron and on the rear service side.
            for(int end=-1;end<=1;end+=2)for(int bay=-1;bay<=1;bay+=2)
            {
                var door=new Vector3(bay*.17f,.18f,-.04f+end*.28f);
                LoadingDoor(root,door+Vector3.forward*end*.013f,.23f,.21f,end);
                Part(root,"Torleuchte",door+new Vector3(0,.125f,end*.008f),new Vector3(.20f,.025f,.022f),Industrial);
            }
            for(int i=-1;i<=1;i+=2)ColonyCrate(root,new Vector3(i*.27f,.11f,.31f),new Vector3(.17f,.16f,.15f),Industrial);
            for(int i=-1;i<=1;i++)Part(root,"Dachträger",new Vector3(i*.23f,.43f,-.04f),new Vector3(.03f,.04f,.58f),Hull);
        }
        private void ColonyHousing(Transform root)
        {
            ColonyHall(root,new Vector3(-.2f,.23f,-.07f),new Vector3(.29f,.37f,.51f),Trim);
            ColonyHall(root,new Vector3(.18f,.17f,.08f),new Vector3(.29f,.25f,.54f),Trim);
            Part(root,"Verbindungsgang",new Vector3(0,.13f,.02f),new Vector3(.18f,.17f,.2f),Hull);
            for(int i=-1;i<=1;i++)Part(root,"Wohnfenster",new Vector3(-.2f,.16f+i*.095f,.193f),new Vector3(.21f,.033f,.013f),Cyan);
            Part(root,"Dachgarten",new Vector3(.18f,.31f,.08f),new Vector3(.22f,.035f,.36f),new Color(.2f,.38f,.16f));
        }
        private void ColonyBarracks(Transform root)
        {
            for(int side=-1;side<=1;side+=2)ColonyHall(root,new Vector3(side*.23f,.14f,0),new Vector3(.25f,.2f,.66f),new Color(.35f,.4f,.34f));
            Part(root,"Exerzierplatz",new Vector3(0,.055f,.06f),new Vector3(.14f,.012f,.58f),Steel);
            Rod(root,"Flaggenmast",new Vector3(0,.06f,-.3f),new Vector3(0,.63f,-.3f),.018f,Hull);
            var flag=ColonyGroup(root,"Standarte",new Vector3(0,.61f,-.3f));
            flag.gameObject.AddComponent<ColonyFlag>().Initialize(ArtMaterial("Flaggentuch",Cyan,false));
        }
        private void ColonyVehicleFactory(Transform root)
        {
            ColonyHall(root,new Vector3(0,.25f,-.05f),new Vector3(.76f,.4f,.57f),Industrial);
            LoadingDoor(root,new Vector3(0,.22f,.262f),.52f,.31f,1);
            Part(root,"Ausfahrrampe",new Vector3(0,.07f,.33f),new Vector3(.54f,.04f,.2f),Steel);
            for(int i=-1;i<=1;i+=2)Part(root,"Fahrbahnmarkierung",new Vector3(i*.18f,.097f,.33f),new Vector3(.025f,.008f,.18f),Industrial);
            Part(root,"Hallendach",new Vector3(0,.47f,-.05f),new Vector3(.60f,.055f,.43f),Trim);
            for(int side=-1;side<=1;side+=2)
            {
                ArmourPanel(root,"Portalpfeiler",new Vector3(side*.30f,.21f,.245f),new Vector3(.11f,.36f,.12f),Hull);
                Part(root,"Torwarnanzeige",new Vector3(side*.30f,.21f,.307f),new Vector3(.033f,.18f,.008f),Industrial);
            }
            Rod(root,"Kranträger",new Vector3(-.32f,.55f,-.18f),new Vector3(.32f,.55f,-.18f),.04f,Industrial);
        }
        private void ColonyDepot(Transform root)
        {
            ColonyHall(root,new Vector3(0,.17f,-.24f),new Vector3(.7f,.24f,.25f),new Color(.37f,.42f,.36f));
            for(int x=-1;x<=1;x++)for(int z=0;z<2;z++)ColonyCrate(root,new Vector3(x*.23f,.11f+z*.045f,.08f+z*.19f),new Vector3(.18f,.14f,.16f),z==0?Industrial:Trim);
            Part(root,"Versorgungszeichen",new Vector3(0,.31f,-.24f),new Vector3(.17f,.014f,.06f),Amber);
            Part(root,"Versorgungszeichen",new Vector3(0,.312f,-.24f),new Vector3(.06f,.014f,.17f),Amber);
        }
        private void ColonyMine(Transform root,bool crystal)
        {
            if(crystal)
            {
                Part(root,"Kristallschacht",new Vector3(0,.14f,.05f),new Vector3(.53f,.09f,.53f),Steel,PrimitiveType.Cylinder);
                for(int i=0;i<3;i++)
                {float a=i*Mathf.PI*2/3;var at=new Vector3(Mathf.Cos(a)*.24f,0,Mathf.Sin(a)*.24f+.05f);Rod(root,"Kristallbohrmast",at+Vector3.up*.05f,at+Vector3.up*.6f,.045f,Trim);}
                Part(root,"Kristallspindel",new Vector3(0,.40f,.05f),new Vector3(.14f,.25f,.14f),new Color(.56f,.37f,.73f),PrimitiveType.Cylinder);
                ColonyHall(root,new Vector3(.21f,.14f,-.25f),new Vector3(.3f,.18f,.22f),Trim);return;
            }
            ColonyHall(root,new Vector3(.18f,.13f,-.22f),new Vector3(.35f,.18f,.24f),Industrial);
            for(int side=-1;side<=1;side+=2)
            {
                Rod(root,"Förderturm",new Vector3(side*.23f,.04f,.22f),new Vector3(side*.14f,.73f,.22f),.052f,Industrial);
                Rod(root,"Förderverstrebung",new Vector3(side*.23f,.08f,.18f),new Vector3(-side*.14f,.65f,.18f),.021f,Steel);
            }
            Rod(root,"Förderbrücke",new Vector3(-.22f,.73f,.22f),new Vector3(.22f,.73f,.22f),.05f,Hull);
            Rod(root,"Bohrgestänge",new Vector3(0,.05f,.22f),new Vector3(0,.79f,.22f),.038f,Steel);
            Part(root,"Förderband",new Vector3(-.2f,.1f,-.08f),new Vector3(.22f,.1f,.6f),Steel);
            for(int i=0;i<4;i++)Part(root,"Erzgut",new Vector3(-.2f,.17f,-.29f+i*.13f),new Vector3(.13f,.075f,.09f),new Color(.45f,.3f,.16f));
        }
        private void ColonyLaboratory(Transform root)
        {
            Part(root,"Laborfundament",new Vector3(0,.13f,0),new Vector3(.64f,.08f,.64f),Hull,PrimitiveType.Cylinder);
            Part(root,"Forschungskuppel",new Vector3(0,.29f,0),new Vector3(.51f,.48f,.51f),Hull,PrimitiveType.Sphere);
            Ring(root,new Vector3(0,.27f,0),.26f,Trim,16,.02f);
            Ring(root,new Vector3(0,.36f,0),.23f,Hull,16,.014f);
            for(int side=-1;side<=1;side+=2)ColonyHall(root,new Vector3(side*.3f,.12f,-.08f),new Vector3(.16f,.14f,.25f),Trim);
            var dish=Part(root,"Radarschüssel",new Vector3(.22f,.55f,.19f),new Vector3(.28f,.045f,.28f),Steel,PrimitiveType.Sphere);dish.localRotation=Quaternion.Euler(35,0,0);
            Rod(root,"Messantenne",new Vector3(.22f,.39f,.19f),new Vector3(.22f,.66f,.27f),.018f,Hull);
        }
        private void ColonyShield(Transform root)
        {
            Part(root,"Generatorring",new Vector3(0,.15f,0),new Vector3(.54f,.09f,.54f),Steel,PrimitiveType.Cylinder);
            Part(root,"Feldkernanzeige",new Vector3(0,.34f,0),new Vector3(.26f,.34f,.26f),Cyan,PrimitiveType.Sphere);
            for(int i=0;i<4;i++)
            {float a=i*Mathf.PI*.5f+Mathf.PI*.25f;var at=new Vector3(Mathf.Cos(a)*.29f,0,Mathf.Sin(a)*.29f);Part(root,"Feldpylon",at+Vector3.up*.34f,new Vector3(.115f,.57f,.115f),Hull);Part(root,"Feldspulenanzeige",at+Vector3.up*.52f,new Vector3(.12f,.12f,.12f),Cyan);}
            Ring(root,new Vector3(0,.14f,0),.27f,Industrial,16,.025f);
        }
        private void ColonySpaceport(Transform root)
        {
            Part(root,"Landefläche",new Vector3(0,.07f,.10f),new Vector3(.83f,.035f,.59f),Steel);
            Ring(root,new Vector3(0,.095f,.12f),.22f,Industrial,16,.012f);
            for(int side=-1;side<=1;side+=2)Part(root,"Landemarkierung",new Vector3(side*.09f,.101f,.12f),new Vector3(.022f,.008f,.21f),Hull);
            Part(root,"Landemarkierung",new Vector3(0,.102f,.12f),new Vector3(.20f,.008f,.025f),Hull);
            ColonyHall(root,new Vector3(.04f,.23f,-.27f),new Vector3(.49f,.34f,.25f),Trim);
            LoadingDoor(root,new Vector3(.04f,.19f,-.13f),.32f,.25f,1);
            Rod(root,"Kontrollturm",new Vector3(-.32f,.03f,-.27f),new Vector3(-.32f,.68f,-.27f),.052f,Hull);
            Part(root,"Kontrollkanzel",new Vector3(-.32f,.66f,-.27f),new Vector3(.17f,.13f,.17f),Trim);
            Part(root,"Landebakenanzeige",new Vector3(-.32f,.76f,-.27f),Vector3.one*.05f,Amber,PrimitiveType.Sphere);
        }
        private void ColonyTurret(Transform root,BuildingKind kind)
        {
            bool heavy=kind==BuildingKind.ArtilleryTower||kind==BuildingKind.OrbitalBattery;
            Part(root,"Geschützfundament",new Vector3(0,.12f,0),new Vector3(heavy?.7f:.56f,.075f,heavy?.7f:.56f),Hull,PrimitiveType.Cylinder);
            for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Part(root,"Feste Geschützstütze",new Vector3(Mathf.Cos(a)*.29f,.10f,Mathf.Sin(a)*.29f),new Vector3(.14f,.12f,.14f),Steel);}
            var turret=ColonyGroup(root,"Turret",new Vector3(0,.25f,0));
            Vector3 muzzle;
            if(kind==BuildingKind.FlakTower)
            {
                ColonyHall(turret,new Vector3(0,.08f,-.08f),new Vector3(.28f,.14f,.28f),Trim);
                for(int side=-1;side<=1;side+=2)for(int level=0;level<2;level++)Rod(turret,"Flakrohr",new Vector3(side*.11f,.12f+level*.09f,.04f),new Vector3(side*.11f,.34f+level*.09f,.39f),.027f,Steel);
                Part(turret,"Flakradar",new Vector3(0,.33f,-.16f),new Vector3(.25f,.12f,.07f),Trim);muzzle=new Vector3(.11f,.43f,.39f);
            }
            else if(kind==BuildingKind.OrbitalBattery)
            {
                Part(turret,"Orbitalwiege",new Vector3(0,.10f,-.03f),new Vector3(.48f,.24f,.32f),Trim);
                for(int side=-1;side<=1;side+=2)
                {Rod(turret,"Orbitalrohr",new Vector3(side*.12f,.17f,.01f),new Vector3(side*.12f,.62f,.43f),.073f,Steel);Rod(turret,"Beschleunigerspule",new Vector3(side*.12f,.43f,.25f),new Vector3(side*.12f,.52f,.34f),.095f,Industrial);}
                muzzle=new Vector3(.12f,.62f,.43f);
            }
            else if(kind==BuildingKind.ArtilleryTower)
            {
                Part(turret,"Artilleriepanzerung",new Vector3(0,.11f,-.08f),new Vector3(.46f,.22f,.4f),Hull);
                Rod(turret,"Artillerierohr",new Vector3(0,.20f,.08f),new Vector3(0,.35f,.59f),.064f,Steel);
                Part(turret,"Rücklaufgehäuse",new Vector3(0,.23f,.09f),new Vector3(.18f,.12f,.32f),Industrial);muzzle=new Vector3(0,.35f,.59f);
            }
            else if(kind==BuildingKind.AntiTankTower)
            {
                Part(turret,"Keilpanzerung",new Vector3(0,.1f,-.05f),new Vector3(.39f,.19f,.34f),Hull);
                Rod(turret,"Panzerabwehrrohr",new Vector3(0,.14f,.1f),new Vector3(0,.14f,.59f),.052f,Steel);
                Part(turret,"Mündungsbremse",new Vector3(0,.14f,.59f),new Vector3(.11f,.09f,.1f),Steel);muzzle=new Vector3(0,.14f,.65f);
            }
            else
            {
                Part(turret,"Schnellfeuerwiege",new Vector3(0,.12f,0),new Vector3(.31f,.2f,.28f),Trim);
                for(int side=-1;side<=1;side+=2)Rod(turret,"Schnellfeuerrohr",new Vector3(side*.065f,.16f,.04f),new Vector3(side*.065f,.16f,.37f),.026f,Steel);
                ColonyCrate(turret,new Vector3(-.2f,.1f,-.03f),new Vector3(.12f,.13f,.2f),Industrial);muzzle=new Vector3(.065f,.16f,.37f);
            }
            ColonyGroup(turret,"MuzzlePosition",muzzle);
        }
        private void ColonyWall(Transform root)
        {
            WallArm(root,Vector3.left*.42f,Vector3.right*.42f);
        }
        private void ColonyGate(Transform root)
        {
            for(int side=-1;side<=1;side+=2)ColonyHall(root,new Vector3(side*.32f,.28f,0),new Vector3(.14f,.5f,.18f),Trim);
            Part(root,"Torsturz",new Vector3(0,.56f,0),new Vector3(.78f,.09f,.18f),Hull);
            var barrier=Part(root,"Torschranke",new Vector3(0,.285f,0),new Vector3(.52f,.46f,.06f),Steel);
            barrier.gameObject.AddComponent<RollingDoor>().Initialize();
            Part(root,"Zugangsanzeige",new Vector3(0,.56f,.1f),new Vector3(.26f,.03f,.013f),Cyan);
        }
        private void ColonyJumpGate(Transform root)
        {
            for(int side=-1;side<=1;side+=2)ColonyHall(root,new Vector3(side*.31f,.17f,.03f),new Vector3(.2f,.23f,.38f),Industrial);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;
                Rod(root,"Torpanzerung",new Vector3(Mathf.Cos(a)*.34f,.49f+Mathf.Sin(a)*.34f,0),new Vector3(Mathf.Cos(b)*.34f,.49f+Mathf.Sin(b)*.34f,0),.085f,Hull);
                Rod(root,"Torenergieanzeige",new Vector3(Mathf.Cos(a)*.273f,.49f+Mathf.Sin(a)*.273f,-.025f),new Vector3(Mathf.Cos(b)*.273f,.49f+Mathf.Sin(b)*.273f,-.025f),.022f,Cyan);
            }
            Part(root,"Torrampe",new Vector3(0,.075f,.25f),new Vector3(.39f,.045f,.4f),Steel);
        }
    }
}
