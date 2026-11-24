Shader "PlanetOne/MinimalSurface"
{
 Properties { _Color("Tint",Color)=(1,1,1,1) _MainTex("Atlas",2D)="white"{} _SurfaceKind("Material",Float)=0 _HexTerrain("Hex",Float)=0 _Variant("Variant",Float)=0 }
 SubShader { Tags { "RenderType"="Opaque" } Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color; sampler2D _MainTex; float _SurfaceKind,_HexTerrain,_PlanetOneGrid,_Variant;
 struct appdata { float4 vertex:POSITION;float3 normal:NORMAL; };
 struct v2f { float4 vertex:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float3 local:TEXCOORD2; };
 v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.local=v.vertex.xyz;return o; }
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float noise(float3 p){float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+float3(1,1,1)),f.x),f.y),f.z);}
 float3 atlas(float2 uv,float2 cell){uv=1-abs(frac(uv*0.5)*2-1);return tex2D(_MainTex,(cell+0.006+uv*0.988)*0.5).rgb;}
 fixed4 frag(v2f i):SV_Target {
 float3 n=normalize(i.normal),col=_Color.rgb;float light=0.48+0.52*saturate(dot(n,normalize(float3(-0.3,0.8,-0.5))));
 float3 coord=_SurfaceKind<1.5?i.world:i.local;float2 uv=abs(n.y)>0.5?coord.xz:abs(n.x)>0.5?coord.zy:coord.xy;float grain=noise(i.local*18);
 if(_SurfaceKind>0.5 && _SurfaceKind<4.5){float2 cell=_SurfaceKind<1.5?float2(0,1):_SurfaceKind<2.5?float2(1,1):_SurfaceKind<3.5?float2(0,0):float2(1,0);float3 tex=atlas(uv*(_SurfaceKind<1.5?0.35:1.2),cell);col=tex*lerp(float3(1,1,1),_Color.rgb,0.45)*1.35;}
 if(_SurfaceKind>4.5 && _SurfaceKind<6.5)col*=0.65+grain*0.55;
 if(_SurfaceKind>6.5 && _SurfaceKind<7.5){float terrain=noise(i.local*7+_Variant)+noise(i.local*21+_Variant)*0.25;col=lerp(float3(0.035,0.16,0.25),lerp(float3(0.24,0.34,0.17),float3(0.63,0.58,0.37),terrain),smoothstep(0.48,0.60,terrain));float clouds=smoothstep(0.72,0.88,noise(i.local*11+3));col=lerp(col,float3(0.85,0.91,0.94),clouds*0.8);col=lerp(col,float3(0.80,0.88,0.92),smoothstep(0.41,0.5,abs(i.local.y)));float rim=pow(1-saturate(dot(n,normalize(_WorldSpaceCameraPos-i.world))),3);col+=rim*float3(0.04,0.19,0.28);}
 if(_SurfaceKind>7.5 && _SurfaceKind<8.5){col=_Color.rgb*(0.85+noise(i.local*24)*0.6);light=1;}
 if(_HexTerrain>0.5 && _PlanetOneGrid>0.5){float2 p=abs(i.local.xz);float edge=max(p.x,dot(p,float2(0.5,0.8660254)));col*=lerp(1,0.64,smoothstep(0.48,0.498,edge));}
 if(_SurfaceKind>8.5 && _SurfaceKind<9.5)col=lerp(float3(0.21,0.22,0.24),float3(0.62,0.59,0.53),noise(i.local*14))* (0.8+grain*0.2);
 if(_SurfaceKind>9.5){float cloud=pow(noise(i.local*8)+noise(i.local*19)*0.2,3);col=lerp(float3(0.018,0.028,0.045),float3(0.065,0.075,0.12),cloud);light=1;}
 return fixed4(col*light,1); }
 ENDCG
 } }
}
