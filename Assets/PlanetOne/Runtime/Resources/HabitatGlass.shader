Shader "PlanetOne/HabitatGlass" {
 Properties{_Color("Glass",Color)=(0.65,0.79,0.82,0.2)}
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent"} ZWrite Off
 CGPROGRAM
 #pragma surface surf Standard alpha:fade
 struct Input{float3 viewDir;};fixed4 _Color;
 void surf(Input i,inout SurfaceOutputStandard o){float rim=pow(1-saturate(dot(normalize(i.viewDir),float3(0,0,1))),3);o.Albedo=_Color.rgb;o.Metallic=.08;o.Smoothness=.94;o.Emission=_Color.rgb*rim*.16;o.Alpha=.09+rim*.43;}
 ENDCG
 } }
