Shader "Unlit/WoodCutWithSmoke"
{
    Properties
    {
        _GrainColor ("Grain Color", Color) = (0.36, 0.22, 0.09, 1)
        _BgColor ("Background Color", Color) = (0.65, 0.50, 0.30, 1)
        _SmokeColor ("Smoke Color", Color) = (0.36, 0.22, 0.09, 0.2)
        _GrainScale ("Grain Scale", Float) = 12.0
        _GrainSpeed ("Grain Animation Speed", Float) = 0.2
        _SmokeStrength ("Smoke Strength", Float) = 0.5
        _SmokeSpread ("Smoke Spread", Float) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            float4 _GrainColor;
            float4 _BgColor;
            float4 _SmokeColor;
            float _GrainScale;
            float _GrainSpeed;
            float _SmokeStrength;
            float _SmokeSpread;

            // Simple 2D noise function (value noise)
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Center UVs
                float2 uv = i.uv * 2.0 - 1.0;
                float r = length(uv);
                float angle = atan2(uv.y, uv.x);

                // Animate grains
                float grainTime = _Time.y * _GrainSpeed;
                float grainNoise = noise(float2(r * _GrainScale + grainTime, angle * 2.0 + grainTime));
                float grain = sin(r * _GrainScale + grainNoise * 2.0 + grainTime);

                // Grain mask
                float grainMask = smoothstep(0.2, 0.7, grain * 0.5 + 0.5);

                // Wood color
                float4 col = lerp(_BgColor, _GrainColor, grainMask);

                // Smoke: outward from grains, animated
                float smokeNoise = noise(float2(r * _GrainScale * 0.5 + _Time.y * 0.1, angle * 2.0 - _Time.y * 0.2));
                float smoke = smoothstep(0.45, 0.55, grain * 0.5 + 0.5 + smokeNoise * 0.2);
                float smokeFade = exp(-r * _SmokeSpread); // fade outwards
                float4 smokeCol = _SmokeColor * smoke * smokeFade * _SmokeStrength;

                // Combine
                col.rgb += smokeCol.rgb;
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}

