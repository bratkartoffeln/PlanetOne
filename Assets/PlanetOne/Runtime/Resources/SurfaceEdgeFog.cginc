#ifndef PLANETONE_SURFACE_EDGE_FOG
#define PLANETONE_SURFACE_EDGE_FOG
float4 _SurfaceLimits;
fixed4 _SurfaceFogColor;

void ApplySurfaceEdgeFog(Input i, SurfaceOutputStandard o, inout fixed4 color)
{
    float2 mapXZ=i.worldPos.xz;
    float2 center=(_SurfaceLimits.zw+_SurfaceLimits.xy)*.5;
    float2 extents=(_SurfaceLimits.zw-_SurfaceLimits.xy)*.5;
    float2 q=abs(mapXZ-center)-extents+1.5;
    float distanceToEdge=length(max(q,0))+min(max(q.x,q.y),0)-1.5;
    float variation=sin(mapXZ.x*.61+mapXZ.y*.28)*.4+sin(mapXZ.y*.89-mapXZ.x*.17)*.25;
    float fog=smoothstep(-1.3,7.5,distanceToEdge+variation);
    #ifdef UNITY_PASS_FORWARDADD
        color.rgb*=1-fog;
    #else
        fixed3 haze=_SurfaceFogColor.rgb+fixed3(.05,.074,.093)*(1-fog);
        color.rgb=lerp(color.rgb,haze,fog);
    #endif
}
#endif
