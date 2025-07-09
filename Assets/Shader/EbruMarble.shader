Shader "Custom/EbruMarble_NightingaleCombedGelgit"
{
    Properties
    {
        _TimeSpeed ("Time Speed", Float) = 1
        _GelgitFrequency ("Gelgit Frequency", Float) = 8
        _GelgitAmplitude ("Gelgit Amplitude", Float) = 0.08
        _CombFrequency ("Comb Frequency", Float) = 20
        _CombAmplitude ("Comb Amplitude", Float) = 0.12
        _Zoom ("Zoom", Float) = 0.6 // <--- Add this line
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _TimeSpeed;
            float _GelgitFrequency;
            float _GelgitAmplitude;
            float _CombFrequency;
            float _CombAmplitude;
            float _Zoom; // <--- Add this line

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 palette(float t, float3 a, float3 b, float3 c, float3 d)
            {
                return a + b * cos(6.28318 * (c * t + d));
            }

            float swirl(float2 p, float2 center, float t, float freq, float speed)
            {
                float2 rel = p - center;
                float a = atan2(rel.y, rel.x);
                float r = length(rel);
                return sin(a * freq + r * freq - t * speed);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Apply zoom to UVs
                float2 uv = (i.uv * 2.0 - 1.0) * _Zoom;
                float t = _Time.x * _TimeSpeed;

                // --- Gel-Git (Back-and-Forth) effect ---
                float gelgit = sin(uv.x * _GelgitFrequency + t);
                uv.y += gelgit * _GelgitAmplitude;

                // --- Tarakli (Combed) effect ---
                float angle = t * 0.5;
                float2 dir = float2(cos(angle), sin(angle));
                float combAxis = dot(uv, dir);
                float comb = sin(combAxis * _CombFrequency + t);
                float2 combedUV = uv + dir * (comb * _CombAmplitude);

                // --- Bülbül Yuvası (Nightingale’s Nest) ---
                float swirlSum = 0.0;
                int swirlCount = 3;
                for (int j = 0; j < swirlCount; j++)
                {
                    float swirlAngle = t * (0.3 + 0.2 * j) + j * 2.1;
                    float2 center = float2(cos(swirlAngle), sin(swirlAngle)) * (0.3 + 0.2 * j);
                    swirlSum += swirl(combedUV, center, t, 8.0 + 2.0 * j, 0.5 + 0.2 * j);
                }
                swirlSum /= swirlCount;

                float r = length(combedUV);
                float bandPos = r * 8.0 + swirlSum - t * 0.5;
                float3 col = palette(
                    bandPos,
                    float3(0.5, 0.5, 0.2),
                    float3(0.5, 0.5, 0.5),
                    float3(1.0, 1.0, 1.0),
                    float3(0.0, 0.33, 0.67)
                );
                float bandEdge = smoothstep(0.18, 0.18 * 0.7, frac(bandPos));
                col *= lerp(1.2, 0.8, bandEdge);

                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}