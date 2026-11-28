Shader "PlanetOne/PlacementPreview" {
 Properties { _Color("Tint",Color)=(.333,.937,.569,.42) }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off
  Cull Back
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _Color;
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
   struct v2f {float4 vertex:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;};
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.view=UnityWorldSpaceViewDir(mul(unity_ObjectToWorld,v.vertex).xyz);return o;}
   fixed4 frag(v2f i):SV_Target {float rim=pow(1-abs(dot(normalize(i.normal),normalize(i.view))),2);return fixed4(_Color.rgb*(.76+rim*.38),saturate(_Color.a+rim*.2));}
   ENDCG
  }
 }
}
