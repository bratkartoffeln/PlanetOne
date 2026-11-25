Shader "PlanetOne/SpaceBackdrop" {
 Properties {_MainTex("Nebula",2D)="black"{}}
 SubShader { Tags {"RenderType"="Opaque"} Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
 fixed4 frag(v2f i):SV_Target{return fixed4(tex2D(_MainTex,i.uv).rgb*0.85,1);}
 ENDCG
 }}}
