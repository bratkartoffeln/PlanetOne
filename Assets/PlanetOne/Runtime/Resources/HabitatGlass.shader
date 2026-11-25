Shader "PlanetOne/HabitatGlass" {
 Properties{_Color("Glass",Color)=(0.32,0.66,0.72,0.2)}
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent"} ZWrite Off
 CGPROGRAM
 #pragma surface surf Standard alpha:fade
 struct Input{float3 viewDir;};fixed4 _Color;
 void surf(Input i,inout SurfaceOutputStandard o){float rim=pow(1-saturate(dot(normalize(i.viewDir),float3(0,0,1))),3);o.Albedo=_Color.rgb;o.Metallic=0.2;o.Smoothness=0.9;o.Alpha=0.13+rim*0.45;}
 ENDCG
 } }
