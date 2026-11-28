Shader "PlanetOne/MinimalSurface" {
 Properties { _Color("Tint",Color)=(1,1,1,1) _MainTex("Atlas",2D)="white"{} _SurfaceKind("Material",Float)=0 _FinishTile("Model finish",Float)=0 _HexTerrain("Hex",Float)=0 _Variant("Variant",Float)=0 }
 SubShader {Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
 #pragma target 3.0
 #include "UnityCG.cginc"
 fixed4 _Color; sampler2D _MainTex;float _SurfaceKind,_FinishTile,_HexTerrain,_PlanetOneGrid,_Variant;
 struct Input {float3 worldPos;float3 worldNormal;float3 localPosition;float3 modelNormal;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.localPosition=v.vertex.xyz;o.modelNormal=v.normal;}
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float noise(float3 p){float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+float3(1,1,1)),f.x),f.y),f.z);}
 float3 atlas(float2 uv,float2 cell){uv=1-abs(frac(uv*0.5)*2-1);return tex2D(_MainTex,(cell+0.006+uv*0.988)*0.5).rgb;}

 void surf(Input i,inout SurfaceOutputStandard o){
 float3 n=normalize(i.worldNormal),col=_Color.rgb;float light=0.48+0.52*saturate(dot(n,normalize(float3(-0.3,0.8,-0.5))));
 float3 coord=_SurfaceKind<1.5?i.worldPos:i.localPosition;float2 uv=abs(n.y)>0.5?coord.xz:abs(n.x)>0.5?coord.zy:coord.xy;float grain=noise(i.localPosition*18);
 if(_SurfaceKind>1.5 && _SurfaceKind<4.5){
     float3 axis=abs(i.modelNormal);
     float2 panelUV=axis.y>max(axis.z,axis.x)?coord.xz:axis.x>axis.z?coord.zy:coord.xy;
     if(_SurfaceKind>2.5 && _SurfaceKind<3.5)col=_Color.rgb*(.64+.30*atlas(panelUV,float2(0,0)).r+.10*grain);
     else{
         // Each face uses its model-space axes so the painted seams rotate with the actual part.
         float2 tile=float2(fmod(_FinishTile,2),1-step(1.5,_FinishTile));
         float3 paint=atlas(panelUV+.5,tile);
         col=_FinishTile<.5?paint*_Color.rgb*.91:paint;
     }
 }
 if(_SurfaceKind<0.5)light=1;
 if(_SurfaceKind>0.5 && _SurfaceKind<1.5){float broad=noise(i.worldPos*0.11);float3 fine=atlas(i.worldPos.xz*0.43,float2(0,1));float3 coarse=atlas(float2(i.worldPos.x+i.worldPos.z,i.worldPos.z-i.worldPos.x)*0.11,float2(0,1));col=lerp(fine,coarse,0.65)*lerp(float3(0.44,0.38,0.30),float3(0.80,0.67,0.49),broad);float cliff=smoothstep(0.1,0.55,1-abs(n.y));col*=lerp(float3(1,1,1),float3(0.64,0.66,0.68),cliff);}
 if(_SurfaceKind>4.5 && _SurfaceKind<6.5)col*=0.65+grain*0.55;
 if(_SurfaceKind>6.5 && _SurfaceKind<7.5){float terrain=noise(i.localPosition*7+_Variant)+noise(i.localPosition*21+_Variant)*0.25;col=lerp(float3(0.035,0.16,0.25),lerp(float3(0.24,0.34,0.17),float3(0.63,0.58,0.37),terrain),smoothstep(0.48,0.60,terrain));float clouds=smoothstep(0.72,0.88,noise(i.localPosition*11+3));col=lerp(col,float3(0.85,0.91,0.94),clouds*0.8);col=lerp(col,float3(0.80,0.88,0.92),smoothstep(0.41,0.5,abs(i.localPosition.y)));float rim=pow(1-saturate(dot(n,normalize(_WorldSpaceCameraPos-i.worldPos))),3);col+=rim*float3(0.04,0.19,0.28);}
 if(_SurfaceKind>7.5 && _SurfaceKind<8.5){col=_Color.rgb*(0.85+noise(i.localPosition*24)*0.6);light=1;}
 if(_HexTerrain>1.5 && _PlanetOneGrid>0.5){float2 p=abs(i.localPosition.xz);float edge=max(p.x,dot(p,float2(0.5,0.8660254)));col*=lerp(1,0.64,smoothstep(0.48,0.498,edge));}
 if(_SurfaceKind>8.5 && _SurfaceKind<9.5)col=lerp(float3(0.21,0.22,0.24),float3(0.62,0.59,0.53),noise(i.localPosition*14))* (0.8+grain*0.2);
 if(_SurfaceKind>9.5 && _SurfaceKind<10.5){float cloud=pow(noise(i.localPosition*8)+noise(i.localPosition*19)*0.2,3);col=lerp(float3(0.018,0.028,0.045),float3(0.065,0.075,0.12),cloud);light=1;}
 if(_SurfaceKind>10.5)col=_Color.rgb*(0.7+noise(i.worldPos*25)*0.2);

 o.Albedo=col;o.Metallic=(_SurfaceKind>1.5 && _SurfaceKind<2.5)?0.35:0.05;o.Smoothness=_SurfaceKind<1.5?0.12:0.35;
 if(_SurfaceKind<0.5){o.Emission=col*0.55;}
 if(_SurfaceKind>7.5 && _SurfaceKind<8.5 || _SurfaceKind>9.5 && _SurfaceKind<10.5){o.Albedo=0;o.Emission=col;}
 }
 ENDCG
 } Fallback "Diffuse"
}
