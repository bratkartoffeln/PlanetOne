Shader "PlanetOne/SpaceBackdrop"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            v2f vert(appdata_base v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*10;
                float n=noise(p)+noise(p*2.1)*0.35+noise(p*4.3)*0.12;
                float band=exp(-pow((i.uv.y-i.uv.x*0.45-0.25)*8,2));
                float cloud=pow(saturate(n-0.25),3)*band;
                return fixed4(float3(0.006,0.012,0.025)+cloud*lerp(float3(0.025,0.08,0.16),float3(0.10,0.035,0.14),noise(p*0.6)),1);
            }
            ENDCG
        }
    }
}
