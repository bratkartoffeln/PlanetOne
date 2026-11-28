Shader "PlanetOne/ModelParticle" {
 Properties { _DstBlend ("Destination blend", Float) = 1 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Pass {
   Blend SrcAlpha [_DstBlend]
   Cull Off ZWrite Off
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct Vertex {float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0;};
   struct Varying {float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0;};
   Varying vert(Vertex v){Varying o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv*2-1;o.color=v.color;return o;}
   fixed4 frag(Varying i):SV_Target {float glow=saturate(1-dot(i.uv,i.uv));return fixed4(i.color.rgb,i.color.a*glow*glow);}
   ENDCG
  }
 }
}
