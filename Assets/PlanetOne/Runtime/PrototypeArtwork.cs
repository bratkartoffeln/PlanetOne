using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;
namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private Texture2D surfaceAtlas;
        private readonly Dictionary<string,Material> artMaterials=new Dictionary<string,Material>();
        private readonly Dictionary<BuildingKind,Texture2D> buildingIcons=new Dictionary<BuildingKind,Texture2D>();
        public void ExportBuildingCatalogue(string directory)
        {
            foreach(BuildingKind kind in System.Enum.GetValues(typeof(BuildingKind)))
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,"building-"+kind+".png"),BuildingIcon(kind).EncodeToPNG());
        }
        private Texture2D BuildingIcon(BuildingKind kind)
        {
            Texture2D icon;if(buildingIcons.TryGetValue(kind,out icon))return icon;
            var root=new GameObject("Baukatalog-Vorschau").transform;root.position=new Vector3(2000,2000,2000);
            var cameraObject=new GameObject("Baukatalog-Kamera");var camera=cameraObject.AddComponent<Camera>();
            var target=RenderTexture.GetTemporary(96,64,24);var previous=RenderTexture.active;
            try
            {
                PopulateBuildingModel(root,kind);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.035f,0.06f,0.08f);
                camera.orthographic=true;camera.orthographicSize=0.76f;camera.aspect=1.5f;camera.nearClipPlane=0.01f;camera.farClipPlane=20;
                camera.transform.position=root.position+new Vector3(1.8f,1.8f,-2.4f);camera.transform.LookAt(root.position+Vector3.up*0.4f);
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                icon=new Texture2D(96,64,TextureFormat.RGB24,false);icon.ReadPixels(new Rect(0,0,96,64),0,0);icon.Apply();buildingIcons[kind]=icon;return icon;
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);root.gameObject.SetActive(false);Destroy(root.gameObject);Destroy(cameraObject);}
        }
        public bool HasSurfaceAtlas { get { return surfaceAtlas!=null; } }
        private Material ArtMaterial(string label,Color color,bool sphere)
        {
            if(surfaceAtlas==null)surfaceAtlas=Resources.Load<Texture2D>("SurfaceAtlas");
            int mode=2;
            if(label=="Gelände" || label=="Fels" || label.Contains("Erz"))mode=1;
            if(label.Contains("Fundament") || label=="Straße" || label=="Wohnmodul" || label=="Dach")mode=3;
            if(label=="Solarfläche")mode=4;
            if(label.Contains("Kristall"))mode=5;
            if(label=="Pflanzreihe")mode=6;
            if(label.StartsWith("Planet "))mode=7;
            if(label.Contains("Stern") || label=="Sonne")mode=8;
            if(label.StartsWith("Mond "))mode=9;
            if(label=="Sternennebel")mode=10;
            if(label.Contains("anzeige") || label.Contains("markierung") || label=="Antrieb" || label=="Fenster")mode=0;
            string key=mode+":"+color.ToString()+":"+(label=="Gelände"?"grid":mode==7?label:"");Material material;
            if(artMaterials.TryGetValue(key,out material))return material;
            material=new Material(worldShader){color=color,name="PlanetOne "+label};
            material.SetTexture("_MainTex",surfaceAtlas);material.SetFloat("_SurfaceKind",mode);material.SetFloat("_HexTerrain",label=="Gelände"?1:0);
            int variant=0;foreach(char c in label)variant=(variant*31+c)%997;material.SetFloat("_Variant",variant/99f);
            artMaterials[key]=material;Shader.SetGlobalFloat("_PlanetOneGrid",presentation.Grid?1:0);return material;
        }
        private void DecorateBuilding(Transform root,BuildingKind kind)
        {
            if(kind==BuildingKind.Wall || kind==BuildingKind.Gate || kind==BuildingKind.JumpGate)return;
            if(kind==BuildingKind.Farm)
            {
                MakePrimitive(PrimitiveType.Sphere,"Gewächshaus",root,new Vector3(0,0.25f,0),new Vector3(0.78f,0.38f,0.77f),new Color(0.38f,0.65f,0.47f));
                for(int i=0;i<3;i++)MakePrimitive(PrimitiveType.Cube,"Dachstrebe",root,new Vector3(-0.25f+i*0.25f,0.37f,0),new Vector3(0.025f,0.04f,0.67f),Ink);return;
            }
            if(kind==BuildingKind.SolarPlant){MakePrimitive(PrimitiveType.Cylinder,"Solarsockel",root,new Vector3(0,0.2f,0),new Vector3(0.12f,0.15f,0.12f),Muted);return;}
            for(int i=0;i<3;i++)MakePrimitive(PrimitiveType.Cube,"Fenster",root,new Vector3(-0.2f+i*0.2f,0.41f,-0.326f),new Vector3(0.10f,0.07f,0.025f),Cyan);
            MakePrimitive(PrimitiveType.Cube,"Eingang",root,new Vector3(0,0.2f,-0.39f),new Vector3(0.18f,0.26f,0.05f),Muted);
            if(kind==BuildingKind.Workshop || kind==BuildingKind.Smelter)
                for(int i=0;i<2;i++)MakePrimitive(PrimitiveType.Cylinder,"Druckbehälter",root,new Vector3(-0.26f+i*0.22f,0.50f,0.25f),new Vector3(0.16f,0.30f,0.16f),Ink*0.7f);
            if(kind==BuildingKind.Warehouse || kind==BuildingKind.Spaceport)
                for(int i=0;i<3;i++)MakePrimitive(PrimitiveType.Cube,"Frachtmodul",root,new Vector3(-0.25f+i*0.25f,0.78f,0),new Vector3(0.18f,0.20f,0.40f),Amber);
            if(kind==BuildingKind.Headquarters)MakePrimitive(PrimitiveType.Cylinder,"Kommunikationskuppel",root,new Vector3(0,0.80f,0),new Vector3(0.45f,0.08f,0.45f),Ink);
            if(kind==BuildingKind.Laboratory || kind==BuildingKind.ShieldGenerator)
            {
                MakePrimitive(PrimitiveType.Sphere,"Feldkuppel",root,new Vector3(0,0.75f,0),new Vector3(0.55f,0.45f,0.55f),kind==BuildingKind.Laboratory?Cyan:new Color(0.65f,0.42f,0.85f));
                for(int i=-1;i<=1;i+=2)MakePrimitive(PrimitiveType.Cylinder,"Feldspule",root,new Vector3(i*0.29f,0.62f,0),new Vector3(0.07f,0.3f,0.07f),Ink);
            }
            if(kind==BuildingKind.CrystalMine)
            {
                MakePrimitive(PrimitiveType.Cylinder,"Bohrturm",root,new Vector3(0,0.9f,0),new Vector3(0.22f,0.4f,0.22f),Muted);
                MakePrimitive(PrimitiveType.Cube,"Kristallkern",root,new Vector3(0.22f,0.85f,0),new Vector3(0.18f,0.4f,0.18f),new Color(0.65f,0.4f,0.9f)).localRotation=Quaternion.Euler(0,30,20);
            }
            if(kind==BuildingKind.Barracks)
                for(int i=-1;i<=1;i+=2)MakePrimitive(PrimitiveType.Cube,"Unterkunft",root,new Vector3(i*0.22f,0.75f,0),new Vector3(0.22f,0.17f,0.6f),BuildingColor(kind));
            if(kind==BuildingKind.VehicleFactory)
            {
                MakePrimitive(PrimitiveType.Cube,"Montagehalle",root,new Vector3(0,0.85f,0),new Vector3(0.6f,0.35f,0.6f),Muted);
                MakePrimitive(PrimitiveType.Cube,"Hallentor",root,new Vector3(0,0.68f,-0.32f),new Vector3(0.45f,0.35f,0.04f),Ink);
            }
            if(kind==BuildingKind.MilitaryDepot || kind==BuildingKind.ElectronicsFactory)
                for(int i=0;i<3;i++)MakePrimitive(PrimitiveType.Cube,"Versorgungsmodul",root,new Vector3(-0.24f+i*0.24f,0.78f,0.15f),new Vector3(0.15f,0.25f,0.3f),kind==BuildingKind.MilitaryDepot?Amber:Cyan);
            if(kind==BuildingKind.OrbitalBattery)
                for(int i=-1;i<=1;i+=2)MakePrimitive(PrimitiveType.Cube,"Orbitalrohr",root,new Vector3(i*0.15f,0.92f,0),new Vector3(0.10f,0.75f,0.13f),Ink).localRotation=Quaternion.Euler(-25,0,0);
            if(kind==BuildingKind.Spaceport)
            {
                MakePrimitive(PrimitiveType.Cylinder,"Landebake",root,new Vector3(0.3f,1,0.25f),new Vector3(0.08f,0.5f,0.08f),Ink);
                MakePrimitive(PrimitiveType.Cube,"Fenster",root,new Vector3(0.3f,1.5f,0.25f),new Vector3(0.12f,0.06f,0.12f),Cyan);
            }
        }
        private void DecorateShip(Transform root)
        {
            MakePrimitive(PrimitiveType.Cube,"Panzerflügel",root,new Vector3(0,0,-0.1f),new Vector3(1.8f,0.35f,0.5f),Muted);
            MakePrimitive(PrimitiveType.Cube,"Brücke",root,new Vector3(0,0.65f,0.05f),new Vector3(0.65f,0.5f,0.4f),Ink);
            for(int i=-1;i<=1;i+=2)
            {
                MakePrimitive(PrimitiveType.Cylinder,"Triebwerk",root,new Vector3(i*0.6f,0,-0.38f),new Vector3(0.45f,0.35f,0.45f),Muted).localRotation=Quaternion.Euler(90,0,0);
                MakePrimitive(PrimitiveType.Sphere,"Antrieb",root,new Vector3(i*0.6f,0,-0.7f),new Vector3(0.24f,0.24f,0.45f),Cyan);
            }
        }
    }
}
