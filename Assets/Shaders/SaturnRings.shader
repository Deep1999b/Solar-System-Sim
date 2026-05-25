Shader "Custom/SaturnRings"
{
    Properties
    {
        _MainTex ("Ring Texture (Alpha)", 2D) = "white" {}
        [HDR] _Color ("Ring Tint", Color) = (0.8, 0.7, 0.5, 0.6)
        _InnerRadius ("Inner Radius", Range(0, 1)) = 0.5
        _OuterRadius ("Outer Radius", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _InnerRadius;
            float _OuterRadius;

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Calculate distance from center of UV (0.5, 0.5)
                float dist = distance(input.uv, float2(0.5, 0.5)) * 2.0;
                
                // Mask the inner and outer edges to create a ring
                float alpha = smoothstep(_InnerRadius - 0.01, _InnerRadius, dist) * 
                             smoothstep(_OuterRadius + 0.01, _OuterRadius, dist);
                
                // Procedural stripes
                float stripes = sin(dist * 100.0) * 0.1 + 0.9;
                
                half4 tex = tex2D(_MainTex, input.uv);
                return _Color * stripes * alpha;
            }
            ENDHLSL
        }
    }
}
