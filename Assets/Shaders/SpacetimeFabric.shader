Shader "Custom/SpacetimeFabric"
{
    Properties
    {
        _GridColor ("Base Fabric Color (Low)", Color) = (0, 0.3, 1, 0.5)
        _SubGridColor ("Secondary Grid Color", Color) = (0, 0.1, 0.5, 0.2)
        _CurvatureColor ("Deep Well Color (High)", Color) = (1, 0, 0.1, 1)
        _GridSpacing ("Base Grid Spacing (Units)", Float) = 10000.0
        _LineThickness ("Line Thickness (px)", Float) = 1.2
        
        [Header(Global Scaling)]
        _GlobalVerticalScale ("Global Vertical Scale", Range(0.01, 1.0)) = 0.08
        _GlobalSoftnessMultiplier ("Global Softness Multiplier", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct GravitySource {
                float3 position;
                float mass;
                float radius;
                float softness;
                float verticalScale;
            };

            StructuredBuffer<GravitySource> _GravitySources;
            int _SourceCount;

            struct Attributes {
                float4 positionOS : POSITION;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float totalPotential : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float4 _SubGridColor;
                float4 _CurvatureColor;
                float _GridSpacing;
                float _LineThickness;
                float _GlobalVerticalScale;
                float _GlobalSoftnessMultiplier;
            CBUFFER_END

            Varyings vert(Attributes input) {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                
                float summedHeight = 0;

                for (int i = 0; i < _SourceCount; i++) {
                    float3 sourcePos = _GravitySources[i].position;
                    float mass = _GravitySources[i].mass;
                    float radius = max(_GravitySources[i].radius, 0.1);
                    float softness = _GravitySources[i].softness * _GlobalSoftnessMultiplier;
                    float vScale = _GravitySources[i].verticalScale;
                    
                    float2 dir = worldPos.xz - sourcePos.xz;
                    float dist = length(dir);
                    
                    // Softened Potential with Radius Scaling
                    float dNorm = dist / radius;
                    float influence = (mass * vScale) / sqrt(dNorm * dNorm + softness * softness);
                    
                    summedHeight += influence;
                }

                worldPos.y -= summedHeight * _GlobalVerticalScale;

                output.positionHCS = TransformWorldToHClip(worldPos);
                output.worldPos = worldPos;
                output.totalPotential = summedHeight; 
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                float3 camPos = _WorldSpaceCameraPos;
                float distToCam = length(input.worldPos - camPos);
                
                // --- SENIOR ENGINEER LOGARITHMIC GRID ---
                // Primary Grid
                float2 uv1 = input.worldPos.xz / _GridSpacing;
                float2 grid1 = abs(frac(uv1 - 0.5) - 0.5);
                float2 dUv1 = fwidth(uv1);
                float line1 = max(smoothstep(dUv1 * _LineThickness, 0, grid1).x, smoothstep(dUv1 * _LineThickness, 0, grid1).y);

                // Secondary Sub-Grid (10x smaller)
                float subGridScale = _GridSpacing * 0.1;
                float2 uv2 = input.worldPos.xz / subGridScale;
                float2 grid2 = abs(frac(uv2 - 0.5) - 0.5);
                float2 dUv2 = fwidth(uv2);
                float line2 = max(smoothstep(dUv2 * _LineThickness, 0, grid2).x, smoothstep(dUv2 * _LineThickness, 0, grid2).y);
                
                float subGridFade = 1.0 - saturate(distToCam / (_GridSpacing * 2.0));
                
                // --- NEW COLORING: BLUE TO RED ---
                // We use a much smaller multiplier (0.0001) to ensure the Sun's 
                // massive background influence doesn't turn the whole system red.
                float curve = saturate(input.totalPotential * 0.00015);
                curve = pow(curve, 2.0); // Sharpen the transition even more
                
                half3 finalRGB = lerp(_GridColor.rgb, _CurvatureColor.rgb, curve);

                // Edge Fading
                float edgeFade = 1.0 - saturate(length(input.worldPos.xz - camPos.xz) / 1500000.0);
                
                // Combine primary and secondary grid
                float finalGrid = max(line1, line2 * subGridFade);
                
                float alpha = finalGrid * _GridColor.a * edgeFade;
                // Add a faint glow to the well itself
                alpha = max(alpha, curve * 0.6 * edgeFade);

                return half4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
}
