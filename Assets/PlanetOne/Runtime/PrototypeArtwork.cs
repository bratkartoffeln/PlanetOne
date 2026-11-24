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
                camera.transform.position=root.position+new Vector3(0,2.4f,-2);camera.transform.LookAt(root.position+Vector3.up*0.3f);
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
            if(label=="Straße")mode=11;
            if(label.Contains("anzeige") || label.Contains("markierung") || label=="Antrieb" || label=="Fenster")mode=0;
            string key=mode+":"+color.ToString()+":"+(label=="Gelände"?"grid":mode==7?label:"");Material material;
            if(artMaterials.TryGetValue(key,out material))return material;
            material=new Material(mode==10?Resources.Load<Shader>("SpaceBackdrop"):worldShader){name="PlanetOne "+label};
            if(mode==10){artMaterials[key]=material;return material;}
            material.color=color;
            material.SetTexture("_MainTex",surfaceAtlas);material.SetFloat("_SurfaceKind",mode);material.SetFloat("_HexTerrain",label=="Gelände"?1:0);
            int variant=0;foreach(char c in label)variant=(variant*31+c)%997;material.SetFloat("_Variant",variant/99f);
            artMaterials[key]=material;Shader.SetGlobalFloat("_PlanetOneGrid",presentation.Grid?1:0);return material;
        }
    }
}
