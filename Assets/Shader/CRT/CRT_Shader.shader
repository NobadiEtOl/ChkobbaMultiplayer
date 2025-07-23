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
        Tags { "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _ScanlineIntensity;
            float _DistortionStrength;
            float _AberrationOffset;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float2 BarrelDistortion(float2 uv, float strength)
            {
                float2 center = uv - 0.5;
                float dist = dot(center, center);
                return uv + center * dist * strength;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = BarrelDistortion(i.uv, _DistortionStrength);

                // Chromatic Aberration
                float3 col;
                col.r = tex2D(_MainTex, uv + float2(_AberrationOffset, 0)).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv - float2(_AberrationOffset, 0)).b;

                // Scanline Effect
                float scanline = sin(uv.y * 800.0) * 0.5 + 0.5;
                col *= lerp(1.0, scanline, _ScanlineIntensity);

                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}