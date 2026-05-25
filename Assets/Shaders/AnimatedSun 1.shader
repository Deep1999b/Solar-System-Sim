Shader "Custom/AnimatedSun"
{
    Properties
    {
        _ColorBlack ("Color Black (Sunspots)", Color) = (0.1, 0.0, 0.0, 1.0)
        _ColorDark ("Color Dark (Cool Plasma)", Color) = (0.8, 0.2, 0.0, 1.0)
        _ColorBase ("Color Base (Hot Plasma)", Color) = (1.0, 0.6, 0.0, 1.0)
        _ColorBright ("Color Bright (Core/Flares)", Color) = (1.0, 0.9, 0.6, 1.0)
        _EmissionStrength ("Emission Multiplier", Float) = 3.0
        _CellScale ("Granule Scale", Float) = 15.0
        _Turbulence ("Turbulence", Float) = 1.5
        _AnimationSpeed ("Animation Speed", Float) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionOS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorBlack;
                half4 _ColorDark;
                half4 _ColorBase;
                half4 _ColorBright;
                float _EmissionStrength;
                float _CellScale;
                float _Turbulence;
                float _AnimationSpeed;
            CBUFFER_END

            // 3D Hash
            float hash(float3 p) {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            // 3D Value Noise
            float noise(float3 x) {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                                 lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                                 lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            // FBM (Fractional Brownian Motion)
            float fbm(float3 p) {
                float f = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 4; i++) {
                    f += amp * noise(p);
                    p *= 2.0;
                    amp *= 0.5;
                }
                return f;
            }

            // 3D Vector Hash for Voronoi
            float3 hash33(float3 p) {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453123);
            }

            // Smooth Voronoi for organic granules
            float smoothVoronoi(float3 x, float time) {
                float3 p = floor(x);
                float3 f = frac(x);
                float res = 0.0;
                float w = 0.0;
                
                for (int k = -1; k <= 1; k++) {
                    for (int j = -1; j <= 1; j++) {
                        for (int i = -1; i <= 1; i++) {
                            float3 b = float3(i, j, k);
                            float3 h = hash33(p + b);
                            float3 offset = 0.5 + 0.5 * sin(time + 6.2831 * h);
                            float3 r = float3(b) - f + offset;
                            float d = dot(r, r);
                            
                            // Smooth minimum blending
                            float h_blend = exp(-8.0 * d);
                            res += d * h_blend;
                            w += h_blend;
                        }
                    }
                }
                return res / w;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _AnimationSpeed;
                float3 pos = input.positionOS;
                
                // 1. Distortion field using FBM (makes the granules swirl and tear)
                float3 distortion = float3(
                    fbm(pos * 5.0 + time),
                    fbm(pos * 5.0 - time * 0.8),
                    fbm(pos * 5.0 + time * 1.2)
                ) * _Turbulence;

                // 2. Sample Granules (Voronoi) with distorted coordinates
                float3 samplePos = (pos + distortion * 0.2) * _CellScale;
                float v = smoothVoronoi(samplePos, time * 2.0);
                
                // Invert and shape the Voronoi to create bright centers and dark rivers
                float granules = saturate(1.0 - v * 1.5);
                granules = pow(granules, 2.0); // Sharpen the bright spots
                
                // 3. Macro structure (Sunspots / Magnetic activity)
                float macro = fbm(pos * 2.0 + distortion * 0.5 - time * 0.5);
                macro = smoothstep(0.3, 0.7, macro); // Contrast

                // Combine granules and macro structures
                float heat = granules * lerp(0.3, 1.2, macro);
                
                // 4. Color Mapping (Approximating Black Body Radiation)
                half3 finalColor = _ColorBlack.rgb;
                finalColor = lerp(finalColor, _ColorDark.rgb, smoothstep(0.0, 0.3, heat));
                finalColor = lerp(finalColor, _ColorBase.rgb, smoothstep(0.3, 0.7, heat));
                finalColor = lerp(finalColor, _ColorBright.rgb, smoothstep(0.7, 1.0, heat));
                
                // 5. Limb Darkening (Real stars are darker at the edges) & Corona Glow
                float3 worldPos = TransformObjectToWorld(input.positionOS);
                float3 viewDir = SafeNormalize(GetCameraPositionWS() - worldPos);
                float ndotv = saturate(dot(normalize(input.normalWS), viewDir));
                
                // Limb darkening (darker at the edges)
                float limbDarkening = pow(ndotv, 0.5); 
                finalColor *= lerp(0.5, 1.0, limbDarkening);
                
                // Corona / Plasma edge glow (spikes of heat at the very edge)
                float edgeGlow = pow(1.0 - ndotv, 4.0);
                finalColor += _ColorBright.rgb * edgeGlow * macro * 2.0;

                return half4(finalColor * _EmissionStrength, 1.0);
            }
            ENDHLSL
        }
    }
}
