Shader "PlanetOne/SpaceBackdrop" {
 Properties {_MainTex("Nebula",2D)="black"{}}
 SubShader { Tags {"RenderType"="Opaque"} Cull Front ZWrite Off Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct v2f {float4 vertex:SV_POSITION;float3 direction:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o;}
 fixed4 frag(v2f i):SV_Target {
   float3 d=normalize(i.direction),w=pow(abs(d),6);w/=w.x+w.y+w.z;
   // Seam-free directional mapping of diffuse gas only; every visible star is a separate 3D position.
   float3 x=tex2D(_MainTex,d.zy*0.46+0.5).rgb;
   float3 y=tex2D(_MainTex,d.xz*0.46+0.5).rgb;
   float3 z=tex2D(_MainTex,d.xy*0.46+0.5).rgb;
   return fixed4((x*w.x+y*w.y+z*w.z)*0.85,1);
 }
 ENDCG
 }}}
