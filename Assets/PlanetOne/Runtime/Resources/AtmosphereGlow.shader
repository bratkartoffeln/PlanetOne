Shader "PlanetOne/AtmosphereGlow" {
 Properties {_Color("Glow",Color)=(0.1,0.4,0.8,0.5) _Star("Star",Float)=0}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha One ZWrite Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;float _Star;
 struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 fixed4 frag(v2f i):SV_Target {
 // Orthographic rays are parallel, even when the star is far from the screen centre.
 float3 viewDirection=unity_OrthoParams.w>0.5?UNITY_MATRIX_V[2].xyz:normalize(_WorldSpaceCameraPos-i.world);
 float f=saturate(dot(normalize(i.normal),normalize(viewDirection)));
 float a=_Star>0.5?pow(f,3):pow(1-f,2)*pow(f,0.4);return fixed4(_Color.rgb,a*_Color.a);}
 ENDCG
 }}
}
