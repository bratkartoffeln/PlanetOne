Shader "PlanetOne/AsteroidDust" {
 Properties { _Color("Dust",Color)=(0.48,0.43,0.35,0.38) _Visibility("Visibility",Range(0,1))=1 }
 SubShader { Tags {"Queue"="Transparent-10" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;float _Visibility;
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 fixed4 frag(v2f i):SV_Target {
 float radial=smoothstep(0,.22,i.uv.y)*(1-smoothstep(.75,1,i.uv.y));
 float2 p=i.world.xz*.19;
 float clouds=.52*noise(p)+.30*noise(p*2.13)+.18*noise(p*5.71);
 float wisps=.65+.35*noise(p*3.1+clouds*2);
 return fixed4(_Color.rgb*(.7+clouds*.65),_Color.a*_Visibility*radial*(.35+clouds*.9)*wisps);
 }
 ENDCG
 }}
}
