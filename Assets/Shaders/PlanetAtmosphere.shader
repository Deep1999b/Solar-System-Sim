Shader "Custom/PlanetAtmosphere"
{
    Properties
    {
        [HDR] _GlowColor("Glow Color", Color) = (0, 0.5, 1, 1)
        _MainTex("Atmosphere Texture (Alpha)", 2D) = "white" {}
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 2.0
        _AtmosphereScale("Atmosphere Scale", Range(1.0, 1.5)) = 1.1
        _ScrollSpeed("Scroll Speed", Vector) = (0.01, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        ZWrite Off
        Blend One One // Additive blending

        Pass
        {
            Name "AtmospherePass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _GlowColor;
            float _FresnelPower;
            float _AtmosphereScale;
            float2 _ScrollSpeed;

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 scaledPos = input.positionOS.xyz * _AtmosphereScale;
                output.positionCS = TransformObjectToHClip(scaledPos);
                
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(TransformObjectToWorld(input.positionOS.xyz));
                output.uv = input.uv + (_ScrollSpeed * _Time.y); // Optional scrolling for clouds/movement
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                // Fresnel
                float fresnel = 1.0 - saturate(dot(normal, viewDir));
                fresnel = pow(fresnel, _FresnelPower);
                
                // Texture Sample
                half4 texColor = tex2D(_MainTex, input.uv);
                
                // Combine: Texture * Color * Fresnel
                return texColor * _GlowColor * fresnel;
            }
            ENDHLSL
        }
    }
}
