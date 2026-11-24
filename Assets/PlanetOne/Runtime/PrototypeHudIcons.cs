using System.Collections.Generic;
using UnityEngine;
namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private enum HudIcon { Build,Fleet,Research,Diplomacy,Planet,Energy,Food,Metal,Parts,Crystal,Menu,Settings,Play,Pause,Back,Close,Units,Supply,Stop,Info }
        private readonly Dictionary<HudIcon,Texture2D> hudIcons=new Dictionary<HudIcon,Texture2D>();
        private Texture2D HudSymbol(HudIcon kind)
        {
            Texture2D texture;if(hudIcons.TryGetValue(kind,out texture))return texture;
            var lines=new List<Vector4>();
            System.Action<float,float,float,float> line=(a,b,c,d)=>lines.Add(new Vector4(a,b,c,d));
            System.Action<float,float,float> circle=(x,y,r)=>{for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;line(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r,x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r);}};
            switch(kind)
            {
                case HudIcon.Build:line(7,5,21,19);line(10,3,24,17);line(7,5,10,3);line(21,19,20,24);line(20,24,24,28);line(24,28,29,27);line(29,27,25,23);line(25,23,27,20);line(27,20,30,23);line(30,23,29,17);line(29,17,24,17);break;
                case HudIcon.Fleet:for(int i=-1;i<=1;i++){float x=16+i*9,y=i==0?4:1;line(x,y,x,y+22);line(x,y+22,x-5,y+7);line(x,y+22,x+5,y+7);line(x-5,y+7,x+5,y+7);}break;
                case HudIcon.Research:line(12,28,20,28);line(13,28,13,18);line(19,28,19,18);line(13,18,6,5);line(19,18,26,5);line(6,5,26,5);line(10,12,22,12);break;
                case HudIcon.Diplomacy:circle(11,19,7);circle(21,13,7);line(10,10,22,22);break;
                case HudIcon.Planet:circle(16,16,10);line(3,8,29,24);line(3,8,9,8);line(29,24,23,24);break;
                case HudIcon.Energy:line(19,29,8,15);line(8,15,17,15);line(17,15,12,3);line(12,3,26,20);line(26,20,17,20);line(17,20,19,29);break;
                case HudIcon.Food:circle(16,15,9);line(16,24,21,29);line(11,18,16,21);break;
                case HudIcon.Metal:line(5,6,27,6);line(27,6,23,18);line(23,18,9,18);line(9,18,5,6);line(9,18,13,25);line(13,25,23,18);break;
                case HudIcon.Parts:circle(16,16,5);circle(16,16,10);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;line(16+Mathf.Cos(a)*10,16+Mathf.Sin(a)*10,16+Mathf.Cos(a)*14,16+Mathf.Sin(a)*14);}break;
                case HudIcon.Crystal:line(16,29,7,19);line(7,19,12,4);line(12,4,23,11);line(23,11,24,22);line(24,22,16,29);line(16,29,15,13);line(15,13,12,4);line(15,13,24,22);break;
                case HudIcon.Menu:for(int i=0;i<3;i++)line(6,8+i*8,26,8+i*8);break;
                case HudIcon.Settings:circle(16,16,6);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;line(16+Mathf.Cos(a)*9,16+Mathf.Sin(a)*9,16+Mathf.Cos(a)*13,16+Mathf.Sin(a)*13);}break;
                case HudIcon.Play:line(10,5,25,16);line(25,16,10,27);line(10,27,10,5);break;
                case HudIcon.Pause:line(11,6,11,26);line(21,6,21,26);break;
                case HudIcon.Back:line(7,16,27,16);line(7,16,16,25);line(7,16,16,7);break;
                case HudIcon.Close:line(8,8,24,24);line(8,24,24,8);break;
                case HudIcon.Units:circle(16,23,5);line(6,5,9,15);line(9,15,23,15);line(23,15,26,5);line(6,5,26,5);break;
                case HudIcon.Supply:line(5,7,27,7);line(27,7,27,25);line(27,25,5,25);line(5,25,5,7);line(16,7,16,25);line(5,20,27,20);break;
                case HudIcon.Stop:line(7,7,25,7);line(25,7,25,25);line(25,25,7,25);line(7,25,7,7);break;
                default:circle(16,16,12);line(16,7,16,18);circle(16,24,0.8f);break;
            }
            texture=new Texture2D(48,48,TextureFormat.RGBA32,false){name="HUD "+kind,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<48;y++)for(int x=0;x<48;x++)
            {
                Vector2 p=new Vector2(x/1.5f,y/1.5f);float distance=100;
                foreach(var l in lines){Vector2 a=new Vector2(l.x,l.y),b=new Vector2(l.z,l.w),d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(0.001f,d.sqrMagnitude));distance=Mathf.Min(distance,Vector2.Distance(p,a+d*t));}
                texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(1.6f-distance)));
            }
            texture.Apply();hudIcons[kind]=texture;return texture;
        }
        private bool HudButton(Rect rect,string label,HudIcon icon,bool selected=false,string tooltip=null,bool vertical=false)
        {
            bool clicked=GUI.Button(rect,new GUIContent("",tooltip??label),selected?selectedButtonStyle:buttonStyle);
            Color old=GUI.color;GUI.color=selected?Cyan:Ink;
            if(vertical)
            {GUI.DrawTexture(new Rect(rect.center.x-16,rect.y+7,32,32),HudSymbol(icon));GUI.Label(new Rect(rect.x,rect.yMax-26,rect.width,21),label,centerStyle);}
            else
            {GUI.DrawTexture(new Rect(string.IsNullOrEmpty(label)?rect.center.x-11:rect.x+9,rect.center.y-11,22,22),HudSymbol(icon));if(!string.IsNullOrEmpty(label))GUI.Label(new Rect(rect.x+39,rect.y+7,rect.width-43,rect.height-10),label,bodyStyle);}
            GUI.color=old;if(selected)Fill(new Rect(rect.x+2,rect.y+6,2,rect.height-12),Cyan);return clicked;
        }
    }
}
