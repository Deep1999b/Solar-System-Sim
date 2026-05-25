Shader "Custom/GravityWell"
{
    Properties
    {
        _GridColor ("Grid Line Color", Color) = (0, 1, 1, 0.4)
        _WellColor ("Planet Well Color", Color) = (0, 0.5, 1, 1)
        _SolarColor ("Solar Well Color", Color) = (1, 0.6, 0, 1)
        _VisualGain ("Visual Depth Gain", Float) = 1000.0
        _GridSpacing ("Grid Spacing", Float) = 5000.0
        _LineThickness ("Line Thickness (Pixels)", Float) = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 worldPos     : TEXCOORD0;
                float planetWell    : TEXCOORD1;
                float solarWell     : TEXCOORD3;
                float sourceGlow    : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float4 _WellColor;
                float4 _SolarColor;
                float _VisualGain;
                float _GridSpacing;
                float _LineThickness;
            CBUFFER_END

            float4 _GravitySources[32]; 
            float4 _GravityParams[32];
            int _SourceCount;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                
                float totalDisp = 0;
                float pWell = 0;
                float sWell = 0;
                float glow = 0;

                for (int i = 0; i < _SourceCount; i++)
                {
                    float3 sourcePos = _GravitySources[i].xyz;
                    float mass = _GravitySources[i].w; 
                    float softening = _GravityParams[i].x;
                    float isSun = _GravityParams[i].y;
                    
                    float dist = distance(worldPos.xz, sourcePos.xz);
                    
                    // Physical displacement
                    float disp = mass / (dist + softening);
                    totalDisp += disp;
                    
                    if (isSun > 0.5) sWell += disp;
                    else pWell += disp;

                    // CENTER-POINT GLOW
                    // This creates a bright tactical marker at the center of every source
                    // so small moons are visible even on a coarse grid.
                    glow += exp(-dist * 0.005); 
                }

                output.planetWell = pWell;
                output.solarWell = sWell;
                output.sourceGlow = glow;
                
                worldPos.y -= totalDisp * _VisualGain;
                
                output.positionHCS = TransformWorldToHClip(worldPos);
                output.worldPos = worldPos;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.worldPos.xz / _GridSpacing;
                float2 grid = abs(frac(uv - 0.5) - 0.5);
                float2 dUv = fwidth(uv);
                float2 lineGrid = smoothstep(dUv * _LineThickness, 0, grid);
                float gridLine = max(lineGrid.x, lineGrid.y);

                float pGlow = saturate(input.planetWell * 0.05);
                float sGlow = saturate(input.solarWell * 0.02);
                float markerGlow = saturate(input.sourceGlow);
                
                half3 finalRGB = _GridColor.rgb;
                finalRGB = lerp(finalRGB, _WellColor.rgb, pGlow);
                finalRGB = lerp(finalRGB, _SolarColor.rgb, sGlow);
                
                // Marker/Source lighting
                finalRGB += _WellColor.rgb * markerGlow * 2.0;
                finalRGB += _SolarColor.rgb * sGlow * 1.5;

                float edgeFade = 1.0 - saturate(length(input.worldPos.xz) / 1000000.0);
                float alpha = gridLine * _GridColor.a * edgeFade;
                
                // Ensure small signatures are visible
                alpha = max(alpha, gridLine * (pGlow + sGlow + markerGlow) * 0.8 * edgeFade);
                // Add center point glow regardless of grid lines
                alpha = max(alpha, markerGlow * 0.5 * edgeFade);

                return half4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
}
