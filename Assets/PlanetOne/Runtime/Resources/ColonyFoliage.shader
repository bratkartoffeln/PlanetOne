Shader "PlanetOne/ColonyFoliage" {
 Properties { _Color("Leaf tint",Color)=(.2,.35,.12,1) }
 SubShader { Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
 #pragma target 3.0
 #include "UnityCG.cginc"
 fixed4 _Color;float _PlanetOneSeconds;
 struct Input {float3 worldPos;};
 void vert(inout appdata_full v){float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;float wind=sin(_PlanetOneSeconds*.7+world.x*.45+world.z*.33);v.vertex.x+=wind*.014;v.vertex.z+=cos(_PlanetOneSeconds*.52+world.x*.2)*.009;}
 void surf(Input i,inout SurfaceOutputStandard o){float grain=frac(sin(dot(floor(i.worldPos*45),float3(127.1,311.7,74.7)))*43758.5453);float light=.85+grain*.3;o.Albedo=_Color.rgb*light;o.Smoothness=.08;o.Metallic=0;}
 ENDCG
 } Fallback "Diffuse"
}
