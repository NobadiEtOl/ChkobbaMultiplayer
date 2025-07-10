Shader "Custom/EbruMarble_TwoColorBands_Velvet"
{
    Properties
    {
        _TimeSpeed ("Time Speed", Float) = 1
        _GelgitFrequency ("Gelgit Frequency", Float) = 8
        _GelgitAmplitude ("Gelgit Amplitude", Float) = 0.08
        _CombFrequency ("Comb Frequency", Float) = 20
        _CombAmplitude ("Comb Amplitude", Float) = 0.12
        _BandCount ("Band Count", Float) = 8
        _BandWidth ("Band Width", Float) = 0.18
        _Zoom ("Zoom", Float) = 0.6
        _Rotation ("Rotation (Degrees)", Range(0,360)) = 0
        _ColorMode ("Color Mode (0=DarkGreenBlack, 1=BrownBlack, 2=WhiteBlack, 3=Custom)", Range(0,3)) = 0
        _BlackBandRatio ("Black Band Ratio (0-1)", Range(0.05,0.5)) = 0.2

        // Velvet controls
        _VelvetColor ("Velvet Color", Color) = (1,0.9,0.8,1)
        _VelvetIntensity ("Velvet Intensity", Range(0,1)) = 0.25
        _VelvetSoftness ("Velvet Softness", Range(0.5,8)) = 2.5
        _VelvetDirection ("Velvet Light Direction", Vector) = (0,1,0,0)

        // Custom band colors
        _BandColorA ("Custom Band Color A", Color) = (0.125, 0, 0.094, 1) // #200018
        _BandColorB ("Custom Band Color B", Color) = (0,0,0,1)
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
            float _BandCount;
            float _BandWidth;
            float _Zoom;
            float _Rotation;
            float _ColorMode;
            float _BlackBandRatio;

            float4 _VelvetColor;
            float _VelvetIntensity;
            float _VelvetSoftness;
            float4 _VelvetDirection;

            float4 _BandColorA;
            float4 _BandColorB;

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

            float2 rotate2D(float2 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = (i.uv * 2.0 - 1.0) * _Zoom;
                float t = _Time.x * _TimeSpeed;

                float angle = radians(_Rotation);
                uv = rotate2D(uv, angle);

                float gelgit = sin(uv.x * _GelgitFrequency + t);
                uv.y += gelgit * _GelgitAmplitude;

                float combAngle = t * 0.5;
                float2 dir = float2(cos(combAngle), sin(combAngle));
                float combAxis = dot(uv, dir);
                float comb = sin(combAxis * _CombFrequency + t);
                float2 combedUV = uv + dir * (comb * _CombAmplitude);

                float r = length(combedUV);
                float bandPos = r * _BandCount - t * 0.5;

                // --- Two-color bands with adjustable black band ratio ---
                float band = frac(bandPos / 1.0); // Each band is 1.0 wide
                float3 colorA;
                float3 colorB = float3(0,0,0); // Black

                if(_ColorMode < 0.5)
                {
                    colorA = float3(0.0, 0.15, 0.1); // Dark green
                }
                else if(_ColorMode < 1.5)
                {
                    colorA = float3(0.12, 0.05, 0.03); // Darker brown
                }
                else if(_ColorMode < 2.5)
                {
                    colorA = float3(0.1, 0.1, 0.1); // White
                }
                else
                {
                    colorA = _BandColorA.rgb; // Custom color
                    colorB = _BandColorB.rgb;
                }

                float3 col;
                if (band < (1.0 - _BlackBandRatio))
                    col = colorA;
                else
                    col = colorB;

                // Soften band edges
                float bandEdge = smoothstep(_BandWidth, _BandWidth * 0.7, frac(bandPos));
                col = lerp(col, float3(1,1,1), bandEdge * 0.2);

                // --- Velvet effect ---
                // Fake normal: radial from center
                float2 center = float2(0,0);
                float2 normal = normalize(combedUV - center);
                float2 lightDir = normalize(_VelvetDirection.xy);

                // Velvet highlight: strongest at grazing angles
                float velvet = pow(1.0 - abs(dot(normal, lightDir)), _VelvetSoftness) * _VelvetIntensity;
                col = lerp(col, _VelvetColor.rgb, velvet);

                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}