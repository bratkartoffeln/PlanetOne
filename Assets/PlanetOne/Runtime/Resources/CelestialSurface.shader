Shader "PlanetOne/CelestialSurface" {
 Properties {_MainTex("Atlas",2D)="white"{} _Row("Map row",Float)=3 _SunDirection("Sun",Vector)=(0,0,-1,0)}
 SubShader {Tags {"RenderType"="Opaque"} Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 sampler2D _MainTex;float _Row;float4 _SunDirection;
 struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 local:TEXCOORD1;float3 world:TEXCOORD2;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.local=v.normal;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 float3 samplePlanet(float2 uv) {
 // The atlas artwork is not periodic. Two overlapping longitude charts hide each
 // chart's cut inside the continuous interior of the other, without changing assets.
 float2 dx=ddx(uv),dy=ddy(uv);
 // atan2 wraps at the meridian. Its derivative must represent the short angular
 // distance; otherwise that single pixel quad selects a blurred, wrong atlas mip.
 dx.x-=round(dx.x);dy.x-=round(dy.x);
 dx.y*=.248;dy.y*=.248;
 float v=(_Row+.004+uv.y*.992)*.25;
 float edgeDistance=min(uv.x,1-uv.x);
 float chartWeight=smoothstep(.035,.13,edgeDistance);
 float3 mainChart=tex2Dgrad(_MainTex,float2(uv.x,v),dx,dy).rgb;
 float3 overlapChart=tex2Dgrad(_MainTex,float2(frac(uv.x+.5),v),dx,dy).rgb;
 return lerp(overlapChart,mainChart,chartWeight);
 }
 fixed4 frag(v2f i):SV_Target {
 float3 n=normalize(i.normal),local=normalize(i.local);
 float2 uv=float2(frac(atan2(local.x,local.z)/6.2831853+0.5),asin(clamp(local.y,-1,1))/3.14159265+0.5);
 float3 land=samplePlanet(uv);
 float light=dot(n,normalize(_SunDirection.xyz));
 float day=smoothstep(-0.18,0.7,light);
 float rim=pow(1-saturate(dot(n,normalize(_WorldSpaceCameraPos-i.world))),3);
 float3 color=land*(0.055+day*1.1);
 if(_Row>0.5)color+=rim*day*float3(0.07,0.23,0.39);
 return fixed4(color,1);
 }
 ENDCG
 }}
}
