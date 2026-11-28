using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;
namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private Texture2D surfaceAtlas;
        private Texture2D modelFinishAtlas;
        private Light catalogueFill;
        private void RenderCatalogue(Camera camera)
        {
            if(catalogueFill==null)
            {
                catalogueFill=new GameObject("Katalog-Aufhelllicht").AddComponent<Light>();
                catalogueFill.transform.SetParent(transform,false);
                catalogueFill.type=LightType.Directional;catalogueFill.cullingMask=1<<30;
                catalogueFill.color=new Color(.82f,.9f,1);catalogueFill.intensity=.34f;
            }
            catalogueFill.transform.rotation=camera.transform.rotation*Quaternion.Euler(12,30,0);
            catalogueFill.enabled=true;
            try{camera.Render();}finally{catalogueFill.enabled=false;}
        }
        private readonly Dictionary<string,Material> artMaterials=new Dictionary<string,Material>();
        private readonly Dictionary<BuildingKind,Texture2D> buildingIcons=new Dictionary<BuildingKind,Texture2D>();
        public void ExportBuildingCatalogue(string directory)
        {
            foreach(BuildingKind kind in System.Enum.GetValues(typeof(BuildingKind)))
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,"building-"+kind+".png"),BuildingIcon(kind).EncodeToPNG());
        }
        public void ExportModelCatalogue(string directory)
        {
            ExportBuildingCatalogue(directory);
            foreach(UnitKind kind in System.Enum.GetValues(typeof(UnitKind)))
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,"unit-"+kind+".png"),UnitPortrait(kind).EncodeToPNG());
            foreach(ShipKind kind in System.Enum.GetValues(typeof(ShipKind)))
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,"ship-"+kind+".png"),ShipPortrait(kind).EncodeToPNG());
        }
        // Unity uses Y as the vertical axis of the XZ hex surface.
        private static Vector3 CatalogueCameraOffset(float yaw)
        { return Quaternion.AngleAxis(yaw,Vector3.up)*new Vector3(0,1.65f,-2.8f); }
        // Fit the actual silhouette in camera space, including turrets and foundations.
        // This runs only when generating a cached catalogue portrait.
        private static void FrameCatalogueModel(Camera camera, Transform root)
        {
            var min=new Vector2(float.PositiveInfinity,float.PositiveInfinity);
            var max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<Renderer>();
                if(renderer==null || !renderer.enabled || filter.sharedMesh==null)continue;
                var toCamera=camera.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                foreach(var vertex in filter.sharedMesh.vertices)
                {
                    Vector2 projected=toCamera.MultiplyPoint3x4(vertex);
                    min=Vector2.Min(min,projected);
                    max=Vector2.Max(max,projected);
                }
            }
            if(min.x>max.x)return;
            var center=(min+max)*.5f;
            camera.transform.position+=camera.transform.right*center.x+camera.transform.up*center.y;
            var span=max-min;
            camera.orthographicSize=Mathf.Max(.01f,span.y,span.x/camera.aspect)*.56f;
        }

        private Texture2D BuildingIcon(BuildingKind kind)
        {
            Texture2D icon;if(buildingIcons.TryGetValue(kind,out icon))return icon;
            var root=new GameObject("Baukatalog-Vorschau").transform;root.position=new Vector3(2000,2000,2000);
            var cameraObject=new GameObject("Baukatalog-Kamera");var camera=cameraObject.AddComponent<Camera>();
            var target=RenderTexture.GetTemporary(192,128,24);var previous=RenderTexture.active;
            try
            {
                PopulateBuildingModel(root,kind);
                var baseHex=root.Find("Fundament");
                if(baseHex!=null)baseHex.localRotation=Quaternion.Euler(0,30,0);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.035f,0.06f,0.08f);
                camera.orthographic=true;camera.orthographicSize=0.76f;camera.aspect=1.5f;camera.nearClipPlane=0.01f;camera.farClipPlane=20;
                camera.transform.position=root.position+CatalogueCameraOffset(60);camera.transform.LookAt(root.position+Vector3.up*0.3f);
                FrameCatalogueModel(camera,root);
                camera.targetTexture=target;RenderCatalogue(camera);RenderTexture.active=target;
                icon=new Texture2D(192,128,TextureFormat.RGB24,false);icon.ReadPixels(new Rect(0,0,192,128),0,0);icon.Apply();buildingIcons[kind]=icon;return icon;
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
            if(label=="Pflanzreihe" || label=="Blattwerk" || label=="Gras" || label=="Dachgarten")mode=6;
            if(label.StartsWith("Planet "))mode=7;
            if(label.Contains("Stern") || label=="Sonne")mode=8;
            if(label.StartsWith("Mond "))mode=9;
            if(label=="Sternennebel")mode=10;
            if(label=="Straße" || label=="Fahrbahn")mode=11;
            if(label.Contains("anzeige") || label.Contains("markierung") || label=="Antrieb" || label.Contains("fenster") || label=="Fenster")mode=0;
            int tile=0;
            if(mode==4)tile=3;
            else if(mode==2)
            {
                if(label=="Dachkassette" || label=="Wartungsluke" || label=="Dachlüfter" || label=="Hallendach" || label=="Motorgitter")tile=1;
                else if(color==Industrial)tile=2;
            }
            string key=mode+":"+tile+":"+color.ToString()+":"+(label=="Gelände"?"grid":mode==7?label:"");Material material;
            if(artMaterials.TryGetValue(key,out material))return material;
            material=new Material(mode==10?Resources.Load<Shader>("SpaceBackdrop"):mode==6?Resources.Load<Shader>("ColonyFoliage"):worldShader){name="PlanetOne "+label};
            if(mode==10){material.SetTexture("_MainTex",Resources.Load<Texture2D>("StarlessNebula"));artMaterials[key]=material;return material;}
            material.color=color;
            if(mode==6){artMaterials[key]=material;return material;}
            if(mode==2 || mode==4)
            {
                if(modelFinishAtlas==null)modelFinishAtlas=Resources.Load<Texture2D>("ModelFinishAtlas");
                material.SetTexture("_MainTex",modelFinishAtlas);material.SetFloat("_FinishTile",tile);
            }
            else material.SetTexture("_MainTex",surfaceAtlas);
            material.SetFloat("_SurfaceKind",mode);material.SetFloat("_HexTerrain",label=="Gelände"?1:0);
            int variant=0;foreach(char c in label)variant=(variant*31+c)%997;material.SetFloat("_Variant",variant/99f);
            artMaterials[key]=material;Shader.SetGlobalFloat("_PlanetOneGrid",presentation.Grid?1:0);return material;
        }
    }
}
