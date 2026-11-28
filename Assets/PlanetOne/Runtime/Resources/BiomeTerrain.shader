Shader "PlanetOne/BiomeTerrain" {
 Properties {_MainTex("Biome atlas",2D)="white"{} _RockTex("Rock atlas",2D)="white"{} _Biome("Biome",Float)=0}
 SubShader {Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard finalcolor:ApplySurfaceEdgeFog fullforwardshadows addshadow
 #pragma target 3.0
 sampler2D _MainTex,_RockTex;float _Biome,_PlanetOneSeconds;
 struct Input {float3 worldPos;float3 worldNormal;};
 float2 tile(float2 uv){return .012+(1-abs(frac(uv*.5)*2-1))*.976;}
 float3 sampleBiome(float2 uv,float2 cell){return tex2D(_MainTex,(cell+tile(uv))*.5).rgb;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(q),hash(q+float2(1,0)),f.x),lerp(hash(q+float2(0,1)),hash(q+1),f.x),f.y);}
 #include "SurfaceEdgeFog.cginc"
 void surf(Input i,inout SurfaceOutputStandard o) {
   float3 n=abs(normalize(i.worldNormal));float2 uv=i.worldPos.xz;
   float3 weights=pow(n,4);weights/=max(.001,weights.x+weights.y+weights.z);
   float3 rock=tex2D(_RockTex,(float2(0,1)+tile(i.worldPos.zy*.26))*.5).rgb*weights.x
              +tex2D(_RockTex,(float2(0,1)+tile(uv*.26))*.5).rgb*weights.y
              +tex2D(_RockTex,(float2(0,1)+tile(i.worldPos.xy*.26))*.5).rgb*weights.z;
   float2 cell=_Biome<1.5?float2(0,1):_Biome<3.5?float2(1,1):_Biome<4.5?float2(0,0):float2(1,0);
   float macro=noise(uv*.09+17),patch=noise(uv*.2+noise(uv*.065)*3);
   float3 fine=sampleBiome(uv*.34,cell),broad=sampleBiome(float2(uv.x+uv.y,uv.y-uv.x)*.073,cell);
   float3 col=lerp(fine,broad,.25+macro*.3)*lerp(.82,1.08,macro);
   float cliff=smoothstep(.12,.46,1-n.y);
   if(_Biome<.5)col=rock*lerp(.65,.88,macro);
   else if(_Biome<1.5)col=lerp(col,rock*.86,cliff*.65+smoothstep(.68,.83,patch)*.15);
   else if(_Biome<3.5){float3 earth=sampleBiome(uv*.23,float2(0,1))*.72;col=lerp(col,earth,smoothstep(.43,.7,patch)*.6);col=lerp(col,rock*.82,cliff*.8);}
   if(_Biome>2.5 && _Biome<3.5)col=lerp(sampleBiome(uv*.4,float2(0,1)),col,smoothstep(.02,.6,i.worldPos.y));
   if(_Biome>3.5 && _Biome<4.5)col=lerp(col,rock*.55,cliff*.45);
   if(_Biome>4.5){col=sampleBiome(uv*.045+float2(_PlanetOneSeconds*.0007,0),float2(1,0));o.Albedo=0;o.Emission=col*.68;o.Smoothness=0;return;}
   o.Albedo=col;o.Smoothness=_Biome>3.5 && _Biome<4.5?.26:.07;o.Metallic=0;
 }
 ENDCG
 } Fallback "Diffuse"
}
