Shader "PlanetOne/OceanSurface" {
 Properties {_Color("Water",Color)=(.018,.18,.29,1)}
 SubShader {Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard finalcolor:ApplySurfaceEdgeFog fullforwardshadows
 #pragma target 3.0
 fixed4 _Color;float _PlanetOneSeconds;
 struct Input {float3 worldPos;};
 #include "SurfaceEdgeFog.cginc"
 void surf(Input i,inout SurfaceOutputStandard o) {
   float w=sin(i.worldPos.x*5+i.worldPos.z*2+_PlanetOneSeconds*.4)*sin(i.worldPos.z*8-i.worldPos.x*3+_PlanetOneSeconds*.25);
   o.Albedo=_Color.rgb*(.85+w*.14);o.Smoothness=.78;o.Metallic=.25;
 }
 ENDCG
 } Fallback "Diffuse"
}
