using System;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        public float TextScale {get{return presentation.TextScale;}}
        public int BodyTextPixels {get{return Mathf.RoundToInt(FontUnitSize(18)*UiScale);}}
        public bool ShowSettingsPage(int page){if(page<0||page>2)return false;SetSettingsVisible(true);settingsPage=page;settingsScroll=Vector2.zero;return true;}
        private int FontUnitSize(int pixels) {return Mathf.Max(10,Mathf.RoundToInt(pixels*TextScale/UiScale));}
        private float UiRowHeight(float baseline=36) {return Mathf.Max(baseline,FontUnitSize(18)+24);}
        private float TextLineHeight {get{return FontUnitSize(18)+5;}}
        private float sidebarContentWidth;
        private float SidebarContentWidth {get{return sidebarContentWidth>0?sidebarContentWidth:SideWidth-64;}}
        private string WrapObjectTitle(string title,float width)
        {
            if(headingStyle.CalcSize(new GUIContent(title)).x<=width)return title;
            return title.Replace("Kommandozentrum","Kommando-\nzentrum").Replace("Panzerabwehr","Panzer-\nabwehr")
                .Replace("Versorgungsdepot","Versorgungs-\ndepot").Replace("Orbitalwerft","Orbital-\nwerft");
        }
        private string CompactHudCaption(string text,float width,float height,GUIStyle style)
        {
            if(style.CalcSize(new GUIContent(text)).x<=width)return text;
            if(style.CalcHeight(new GUIContent(text),width)<=height)return text;
            string shorter=text=="Details und Lager"?"Details":text=="Gebäude bauen"?"Bauen":text=="Kolonieübersicht"?"Kolonie":
                text=="Bodentruppen"?"Truppen":text=="Weitere …"?"Mehr …":text;
            if(style.CalcHeight(new GUIContent(shorter),width)<=height)return shorter;
            while(shorter.Length>1 && style.CalcSize(new GUIContent(shorter+"…")).x>width)shorter=shorter.Substring(0,shorter.Length-1).TrimEnd();
            return shorter+"…";
        }
        private float NavigationWidth {get{return Mathf.Clamp(Screen.width/UiScale*.2f,248,330);}}
        public bool SetTextScale(float scale,bool persist=true)
        {
            if(float.IsNaN(scale)||float.IsInfinity(scale)||scale<.9f||scale>1.4f)return false;
            presentation.TextScale=scale;CloseCommandMenu();sidebarScroll=Vector2.zero;UpdateCamera();
            if(persist)SavePresentationSettings();return true;
        }
        private void ApplyTypography()
        {
            titleStyle.fontSize=FontUnitSize(30);headingStyle.fontSize=FontUnitSize(20);
            bodyStyle.fontSize=FontUnitSize(18);mutedStyle.fontSize=FontUnitSize(18);smallStyle.fontSize=FontUnitSize(15);
            buttonStyle.fontSize=FontUnitSize(17);selectedButtonStyle.fontSize=buttonStyle.fontSize;
            if(buildStyle!=null){buildStyle.fontSize=FontUnitSize(16);selectedBuildStyle.fontSize=buildStyle.fontSize;buildStyle.fixedWidth=0;selectedBuildStyle.fixedWidth=0;}
            if(centerStyle!=null)centerStyle.fontSize=FontUnitSize(17);
            if(treeStyle!=null)treeStyle.fontSize=FontUnitSize(17);
            GUI.skin.toggle.fontSize=FontUnitSize(18);GUI.skin.toggle.wordWrap=true;
            GUI.skin.textField.fontSize=FontUnitSize(18);GUI.skin.textField.padding=new RectOffset(10,10,6,6);
            GUI.skin.horizontalSlider.fixedHeight=24;GUI.skin.horizontalSliderThumb.fixedHeight=24;
            GUI.skin.verticalScrollbar.fixedWidth=20;GUI.skin.verticalScrollbarThumb.fixedWidth=18;
        }
        private void LabelFitted(Rect rect,string text,GUIStyle style)
        {
            bool wrap=style.wordWrap;style.wordWrap=false;GUI.Label(rect,text,style);style.wordWrap=wrap;
        }
    }
}
