Shader "PlanetOne/BiomeTerrain" {
 Properties {_MainTex("Biome atlas",2D)="white"{} _RockTex("Rock atlas",2D)="white"{} _Biome("Biome",Float)=0}
 SubShader {Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 sampler2D _MainTex,_RockTex;float _Biome;
 struct Input {float3 worldPos;float3 worldNormal;};
 float2 tile(float2 uv){return .012+(1-abs(frac(uv*.5)*2-1))*.976;}
 float3 sampleBiome(float2 uv,float2 cell){return tex2D(_MainTex,(cell+tile(uv))*.5).rgb;}
 void surf(Input i,inout SurfaceOutputStandard o) {
   float3 n=abs(normalize(i.worldNormal));float2 uv=i.worldPos.xz;
   float3 rock=tex2D(_RockTex,(float2(0,1)+tile(uv*.3))*.5).rgb;
   float2 cell=_Biome<1.5?float2(0,1):_Biome<3.5?float2(1,1):_Biome<4.5?float2(0,0):float2(1,0);
   float3 fine=sampleBiome(uv*.42,cell),broad=sampleBiome(float2(uv.x+uv.y,uv.y-uv.x)*.085,cell);
   float3 col=lerp(fine,broad,.32);
   float cliff=smoothstep(.15,.5,1-n.y);
   if(_Biome<.5)col=rock*.7;
   else if(_Biome<3.5)col=lerp(col,rock*.8,cliff*.85);
   if(_Biome>2.5 && _Biome<3.5)col=lerp(sampleBiome(uv*.4,float2(0,1)),col,smoothstep(.02,.6,i.worldPos.y));
   if(_Biome>3.5 && _Biome<4.5)col=lerp(col,rock*.55,cliff*.45);
   if(_Biome>4.5){col=sampleBiome(uv*.045,float2(1,0));o.Albedo=0;o.Emission=col*.68;o.Smoothness=0;return;}
   o.Albedo=col;o.Smoothness=_Biome>3.5 && _Biome<4.5?.32:.13;o.Metallic=0;
 }
 ENDCG
 } Fallback "Diffuse"
}
