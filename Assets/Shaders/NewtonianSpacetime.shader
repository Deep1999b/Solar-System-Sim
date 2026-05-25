Shader "Custom/NewtonianSpacetime"
{
    Properties
    {
        _BaseColor ("Base Fabric (Blue)", Color) = (0, 0.05, 0.4, 0.5)
        _DeepColor ("Well Depth (Red)", Color) = (1, 0, 0, 1)
        _GridSpacing ("Grid Spacing", Float) = 10000.0
        _LineThickness ("Line Thickness", Float) = 1.5
        
        [Header(Physics Overrides)]
        _G ("Gravitational Constant (G)", Float) = 1.0
        _CurvatureScale ("Curvature Scale", Float) = 10.0
        _Softening ("Global Softness (ε)", Float) = 1.0
        _MaxDepth ("Max Well Depth", Float) = 50000.0
        _ColorSensitivity ("Color Sensitivity", Range(0.1, 5.0)) = 1.0
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
                float radius; // Per-body softening
            };

            StructuredBuffer<GravitySource> _GravitySources;
            int _SourceCount;

            struct Attributes {
                float4 positionOS : POSITION;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float displacementFactor : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _DeepColor;
                float _GridSpacing;
                float _LineThickness;
                float _G;
                float _CurvatureScale;
                float _Softening;
                float _MaxDepth;
                float _ColorSensitivity;
            CBUFFER_END

            Varyings vert(Attributes input) {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                
                float totalPotential = 0;

                for (int i = 0; i < _SourceCount; i++) {
                    float3 sourcePos = _GravitySources[i].position;
                    float M = _GravitySources[i].mass;
                    float epsilon = _GravitySources[i].radius * _Softening;
                    
                    float3 dir = worldPos - sourcePos;
                    // Newtonian Potential: Φ = -GM / sqrt(r² + ε²)
                    float distSq = dot(dir.xz, dir.xz);
                    float phi = -(_G * M) / sqrt(distSq + (epsilon * epsilon));
                    
                    totalPotential += phi;
                }

                // Apply displacement
                float displacement = totalPotential * _CurvatureScale;
                
                // Track displacement relative to max depth for coloring (0 to 1 range)
                output.displacementFactor = saturate(abs(displacement) / _MaxDepth);

                displacement = max(displacement, -_MaxDepth); 
                worldPos.y += displacement;

                output.positionHCS = TransformWorldToHClip(worldPos);
                output.worldPos = worldPos;
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                float3 camPos = _WorldSpaceCameraPos;
                float distToCam = length(input.worldPos - camPos);

                // --- GRID LOGIC ---
                float2 uv = input.worldPos.xz / _GridSpacing;
                float2 grid = abs(frac(uv - 0.5) - 0.5);
                float2 dUv = fwidth(uv);
                float lineFactor = max(smoothstep(dUv * _LineThickness, 0, grid).x, 
                                       smoothstep(dUv * _LineThickness, 0, grid).y);

                // --- NEWTONIAN COLORING (BLUE TO RED) ---
                // Coloring is now tied to the visual displacement itself
                float colorMap = saturate(input.displacementFactor * _ColorSensitivity);
                colorMap = pow(colorMap, 0.7); 
                
                half3 finalRGB = lerp(_BaseColor.rgb, _DeepColor.rgb, colorMap);
                
                float alpha = lerp(_BaseColor.a, _DeepColor.a, colorMap) * lineFactor;
                
                // Edge Fading
                float edgeFade = 1.0 - saturate(length(input.worldPos.xz - camPos.xz) / 1500000.0);
                alpha *= edgeFade;
                alpha = max(alpha, colorMap * 0.5 * edgeFade);

                return half4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
}
