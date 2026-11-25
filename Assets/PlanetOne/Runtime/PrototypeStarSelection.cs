using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        public bool StarSelected {get{return openPlanetId==0 && selectedPlanetId==-1 && selectedShips.Count==0 && selectedAsteroidId==0;}}
        public bool SelectStarAtScreen(Vector2 point)
        {
            if(openPlanetId!=0 || !IsWorldPoint(point))return false;
            var centre=sceneCamera.WorldToScreenPoint(StarView.position);
            float radius=StarDiameter*sceneCamera.pixelHeight/(4*sceneCamera.orthographicSize);
            if(centre.z<=0 || Vector2.Distance(centre,point)>radius+4)return false;
            selectedPlanetId=-1;selectedShips.Clear();selectedAsteroidId=0;miningPanel=false;contextOpen=true;panelOpen=false;
            showingSpacePanel=false;showingDiplomacy=false;presentationSection=0;lastClickedPlanet=0;sidebarScroll=Vector2.zero;return true;
        }
        public void OpenStarDetails()
        {if(!StarSelected)return;contextOpen=true;panelOpen=true;showingSpacePanel=false;showingDiplomacy=false;presentationSection=0;sidebarScroll=Vector2.zero;}
        private Texture2D StarPortrait()
        {
            return ObjectPortrait("central-star",root=>{
                var body=MakePrimitive(PrimitiveType.Sphere,"Stern",root,Vector3.zero,Vector3.one*1.5f,new Color(1,.5f,.055f));CreateStarCorona(body);
            },1.6f);
        }
        private void DrawStarDetails()
        {
            GUILayout.Label("ZENTRALSTERN",headingStyle);Rect portrait=GUILayoutUtility.GetRect(160,100);GUI.DrawTexture(portrait,StarPortrait(),ScaleMode.ScaleToFit);
            GUILayout.Label("Gelber Hauptreihenstern · "+StellarReference.SpectralType,bodyStyle);GUILayout.Space(10);
            GUILayout.Label("Masse     "+StellarReference.MassSolar.ToString("0.0")+" Sonnenmassen",smallStyle);
            GUILayout.Label("Radius     "+StellarReference.RadiusKm.ToString("N0")+" km",smallStyle);
            GUILayout.Label("Temperatur     ca. "+StellarReference.EffectiveTemperatureKelvin.ToString("N0")+" K",smallStyle);
            GUILayout.Label("Leuchtkraft     "+StellarReference.LuminositySolar.ToString("0.0")+" Sonnenleuchtkräfte",smallStyle);
            GUILayout.Space(12);GUILayout.Label("Zentrum des Sternsystems. Sein Licht versorgt die Welten; Abstand und Atmosphäre bestimmen deren Klima.",mutedStyle);
            GUILayout.Label("Nicht besiedelbar",smallStyle);
            if(GUILayout.Button("Ansicht auf Stern zentrieren",buttonStyle))SetSpaceFocus(Vector3.zero);
        }
    }
}
