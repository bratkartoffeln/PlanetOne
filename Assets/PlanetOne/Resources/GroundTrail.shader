Shader "PlanetOne/GroundTrail"
{
    Properties { _Color ("Fahrspur", Color) = (.2,.15,.08,.3) }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct input { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
            struct output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
            output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            fixed4 frag(output i):SV_Target
            {
                float noise=frac(sin(dot(floor(i.world.xz*31),float2(12.9898,78.233)))*43758.5453);
                float edge=smoothstep(0,.55,1-abs(i.uv.y*2-1));
                fixed4 color=_Color;color.rgb*=.8+noise*.35;color.a*=edge*(.65+noise*.35);return color;
            }
            ENDCG
        }
    }
}
