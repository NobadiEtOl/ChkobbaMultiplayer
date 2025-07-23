Shader "Unlit/PatternLinesNoise"
{
    Properties
    {
        _MainColor ("Background Color", Color) = (0.8, 0.6, 0.4, 1) // Light background
        _LineColor ("Line Color", Color) = (0.12, 0.05, 0.03, 1) // Dark lines
        _ScaleX ("Pattern Scale X", Float) = 10
        _ScaleY ("Pattern Scale Y", Float) = 3
        _LineScale ("Line Scale", Float) = 10
        _NoiseStrength ("Noise Strength", Float) = 1
        _Speed ("Animation Speed", Float) = 1
        
        // Mode system like Ebru shader
        _ColorMode ("Color Mode (0=Original, 1=Enhanced)", Range(0,1)) = 0
        _BandWidth ("Band Width", Float) = 0.18
        _BandColor ("Band Edge Color", Color) = (0.7, 0.7, 0.7, 1) // Gray band color
        _BandIntensity ("Band Edge Intensity", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _MainColor;
            float4 _LineColor;
            float _ScaleX;
            float _ScaleY;
            float _LineScale;
            float _NoiseStrength;
            float _Speed;
            
            float _ColorMode;
            float _BandWidth;
            float4 _BandColor;
            float _BandIntensity;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(random(i + float2(0.0,0.0)), random(i + float2(1.0,0.0)), u.x),
                    lerp(random(i + float2(0.0,1.0)), random(i + float2(1.0,1.0)), u.x),
                    u.y
                );
            }

            float2 rotate2d(float2 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(
                    c * p.x - s * p.y,
                    s * p.x + c * p.y
                );
            }

            float lines(float2 pos, float b, float t)
            {
                float scale = _LineScale;
                pos *= scale;
                // Animate by offsetting x with time
                return smoothstep(0.0, 0.5 + b * 0.5, abs((sin((pos.x + t) * 3.1415) + b * 2.0)) * 0.5);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 st = i.uv;
                st.y *= _ScreenParams.y / _ScreenParams.x;

                float2 pos = st.yx * float2(_ScaleX, _ScaleY);

                float t = _Time.y * _Speed;

                float n = noise(pos) * _NoiseStrength;
                pos = rotate2d(pos, n);

                float pattern = lines(pos, 0.5, t);

                // Base color blend between background and line color
                float3 finalColor = lerp(_MainColor.rgb, _LineColor.rgb, pattern);

                // Enhanced mode with band edges (copied from Ebru shader logic)
                if (_ColorMode > 0.5)
                {
                    // Use the pattern value as band position (similar to Ebru's bandPos)
                    float bandPos = pattern * _LineScale;
                    
                    // Create band edge effect exactly like Ebru shader
                    float bandEdge = smoothstep(_BandWidth, _BandWidth * 0.7, frac(bandPos));
                    
                    // Apply band color as edge effect (like Ebru's halftone effect)
                    finalColor = lerp(finalColor, _BandColor.rgb, bandEdge * _BandIntensity);
                }

                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}