Shader "PlanetOne/BackgroundStars" {
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha One
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 size:TEXCOORD1;fixed4 color:COLOR;};
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 v2f vert(appdata v) {v2f o;float4 view=mul(UNITY_MATRIX_MV,v.vertex);view.xy+=(v.uv*2-1)*v.size.x;o.pos=mul(UNITY_MATRIX_P,view);o.uv=v.uv*2-1;o.color=v.color;return o;}
 fixed4 frag(v2f i):SV_Target {float radius=dot(i.uv,i.uv);clip(1-radius);return fixed4(i.color.rgb,pow(saturate(1-radius),2));}
 ENDCG
 }}
}
