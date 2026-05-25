Shader "Custom/AsteroidOrbit"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (0.5, 0.5, 0.5, 1)
        _InnerRadius ("Inner Radius", Float) = 314
        _OuterRadius ("Outer Radius", Float) = 493
        _SpeedScale ("Speed Scale", Float) = 1.0
        _TimeOffset ("Time Offset", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _InnerRadius;
                float _OuterRadius;
                float _SpeedScale;
                float _TimeOffset;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _OrbitParams) // x: radius, y: phase, z: orbitSpeed, w: scale
                UNITY_DEFINE_INSTANCED_PROP(float4, _RotationParams) // x,y,z: rotSpeed, w: tilt
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4 orbit = UNITY_ACCESS_INSTANCED_PROP(Props, _OrbitParams);
                float4 rot = UNITY_ACCESS_INSTANCED_PROP(Props, _RotationParams);

                float radius = orbit.x;
                float phase = orbit.y;
                float orbitSpeed = orbit.z;
                float scale = orbit.w;

                float time = _Time.y * _SpeedScale + _TimeOffset;
                float angle = phase + time * orbitSpeed;

                // REDUCED Vertical wobble for realistic ratio scale
                float verticalWobble = sin(angle * 0.5) * (radius * 0.001); 
                float3 worldOffset = float3(cos(angle) * radius, verticalWobble, sin(angle) * radius);
                
                float3 axis = normalize(float3(rot.x, rot.y, rot.z));
                float rotAngle = time * length(rot.xyz);
                
                float3 v = input.positionOS.xyz * scale;
                float3 rotatedPos = v * cos(rotAngle) + cross(axis, v) * sin(rotAngle) + axis * dot(axis, v) * (1.0 - cos(rotAngle));

                float3 finalWorldPos = rotatedPos + worldOffset;
                output.positionWS = finalWorldPos;
                output.positionHCS = TransformWorldToHClip(finalWorldPos);
                
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                float3 lightDir = normalize(-input.positionWS);
                float diff = max(0.2, dot(normalize(input.normalWS), lightDir));
                
                return tex * _Color * diff;
            }
            ENDHLSL
        }
    }
}
