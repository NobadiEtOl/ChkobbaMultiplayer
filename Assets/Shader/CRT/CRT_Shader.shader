Shader "Custom/CRTScreenURP"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.2
        _DistortionStrength ("Curvature Strength", Range(0, 1)) = 0.1
        _AberrationOffset ("Chromatic Aberration", Range(0, 0.01)) = 0.002
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Overlay"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        
        Pass
        {
            Name "CRTPass"
            Tags { "LightMode" = "UniversalForward" }
            
            ZWrite Off
            ZTest Always
            Blend One Zero
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            CBUFFER_START(UnityPerMaterial)
                half _ScanlineIntensity;
                half _DistortionStrength;
                half _AberrationOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }

            half2 BarrelDistortion(half2 uv, half strength)
            {
                half2 center = uv - 0.5h;
                half dist = dot(center, center);
                return uv + center * dist * strength;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half2 uv = BarrelDistortion(input.uv, _DistortionStrength);

                // Chromatic Aberration (optimized for mobile)
                half3 col;
                col.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(_AberrationOffset, 0.0h)).r;
                col.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                col.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - half2(_AberrationOffset, 0.0h)).b;

                // Scanline Effect (mobile-optimized with half precision)
                half scanline = sin(uv.y * 800.0h) * 0.5h + 0.5h;
                col *= lerp(1.0h, scanline, _ScanlineIntensity);

                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}