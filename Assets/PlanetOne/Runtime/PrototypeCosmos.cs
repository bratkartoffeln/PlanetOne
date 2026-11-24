using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private Texture2D celestialAtlas;
        private Transform nebulaView;
        private readonly Dictionary<int,Material> celestialMaterials=new Dictionary<int,Material>();
        private Material atmosphereMaterial,coronaMaterial;
        private int selectedPlanetId=1;
        private float lastPlanetClick;
        private int lastClickedPlanet;
        public int SelectedPlanetId {get{return selectedPlanetId;}}
        internal Transform CelestialView(int id){return planetViews[id];}
        public bool SelectPlanet(int id)
        {
            if(simulation.FindPlanet(id)==null || !simulation.IsPlanetKnown(PlayerFactionId,id))return false;
            selectedPlanetId=id;selectedShips.Clear();contextOpen=true;panelOpen=false;
            showingSpacePanel=false;showingDiplomacy=false;presentationSection=0;return true;
        }
        public bool OpenSelectedPlanet(){bool build=panelOpen && presentationSection==1;if(!SelectPlanet(selectedPlanetId))return false;SetView(selectedPlanetId);if(build)OpenPresentationSection(1);return true;}
        private Transform CreateCelestial(PlanetState p,Transform parent,Vector3 position,float diameter)
        {
            if(celestialAtlas==null)celestialAtlas=Resources.Load<Texture2D>("CelestialAtlas");
            Material material;
            if(!celestialMaterials.TryGetValue(p.Id,out material))
            {
                material=new Material(Resources.Load<Shader>("CelestialSurface"));material.SetTexture("_MainTex",celestialAtlas);
                celestialMaterials[p.Id]=material;
            }
            material.SetFloat("_Row",p.ParentId>0?0:p.Id==1?3:p.Id==2?2:1);
            var body=MakePrimitive(PrimitiveType.Sphere,"Himmelskörper",parent,position,Vector3.one*diameter,Color.white);
            body.GetComponent<Renderer>().sharedMaterial=material;
            if(p.ParentId<=0)
            {
                if(atmosphereMaterial==null){atmosphereMaterial=new Material(Resources.Load<Shader>("AtmosphereGlow"));atmosphereMaterial.color=new Color(0.08f,0.35f,0.7f,0.65f);}
                var shell=MakePrimitive(PrimitiveType.Sphere,"Atmosphäre",body,Vector3.zero,Vector3.one*1.055f,Color.white);shell.GetComponent<Renderer>().sharedMaterial=atmosphereMaterial;
            }
            return body;
        }
        private void CreateStarCorona(Transform star)
        {
            if(coronaMaterial==null){coronaMaterial=new Material(Resources.Load<Shader>("AtmosphereGlow"));coronaMaterial.color=new Color(1,0.36f,0.035f,0.8f);coronaMaterial.SetFloat("_Star",1);}
            var glow=MakePrimitive(PrimitiveType.Sphere,"Korona",star,Vector3.zero,Vector3.one*1.9f,Color.white);glow.GetComponent<Renderer>().sharedMaterial=coronaMaterial;
        }
        private float PlanetScreenRadius(int id)
        {return planetViews[id].localScale.x*0.5f*sceneCamera.pixelHeight/(sceneCamera.orthographicSize*2);}
        private void DrawPlanetNameplates(float scale)
        {
            foreach(var p in simulation.State.Planets)
            {
                if(!simulation.IsPlanetKnown(PlayerFactionId,p.Id))continue;
                var screen=sceneCamera.WorldToScreenPoint(planetViews[p.Id].position);
                if(!sceneCamera.pixelRect.Contains(screen))continue;
                float radius=PlanetScreenRadius(p.Id)/scale,x=screen.x/scale,y=(Screen.height-screen.y)/scale;
                if(p.Id==selectedPlanetId && selectedShips.Count==0)DrawTargetBrackets(new Rect(x-radius-7,y-radius-7,radius*2+14,radius*2+14),Cyan);
                string name=p.Name.ToUpperInvariant();float labelWidth=centerStyle.CalcSize(new GUIContent(" "+name+" ")).x;
                var label=new Rect(x-labelWidth/2,y+radius+9,labelWidth,22);Fill(label,new Color(0.015f,0.035f,0.05f,0.9f));
                GUI.Label(label,name,centerStyle);
            }
        }
        private void DrawTargetBrackets(Rect r,Color color)
        {
            float arm=Mathf.Min(14,r.width*0.22f);
            foreach(float x in new[]{r.x,r.xMax})foreach(float y in new[]{r.y,r.yMax})
            {Fill(new Rect(x==r.x?x:x-arm,y,arm,2),color);Fill(new Rect(x,y==r.y?y:y-arm,2,arm),color);}
        }
    }
}
