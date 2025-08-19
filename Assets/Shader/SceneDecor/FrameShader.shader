Shader "Custom/SimpleTwoColorLines"
{
    Properties
    {
        _Color1 ("Color 1", Color) = (0.8, 0.6, 0.4, 1)
        _Color2 ("Color 2", Color) = (0.2, 0.1, 0.05, 1)
        _Color3 ("Color 3", Color) = (0.5, 0.3, 0.1, 1)
        _LineScale ("Line Scale", Float) = 50.0
        _AnimationSpeed ("Animation Speed", Float) = 0.5
        _Normalizer ("Normalizer", Float) = 1000.0
        _Sharpness ("Sharpness", Range(1, 20)) = 5.0
        _WaveAmount ("Wave Amount", Range(0, 2)) = 0.5
        _WaveFrequency ("Wave Frequency", Float) = 10.0
        _Turbulence ("Turbulence", Range(0, 1)) = 0.3
        _RandomSeed ("Random Seed", Float) = 1.0
        _Rotation ("Rotation (Degrees)", Range(0, 360)) = 90.0
        _LineThickness ("Line Thickness", Range(0.1, 5.0)) = 1.0
        _LineWidth ("Line Width", Range(0.1, 3.0)) = 1.0
        _Color3Intensity ("Color 3 Intensity", Range(0, 1)) = 0.3
        _Color3Frequency ("Color 3 Frequency", Float) = 2.0
        _Color3Sharpness ("Color 3 Sharpness", Range(1, 20)) = 5.0
        _Color3LineCount ("Color 3 Line Count", Range(0.1, 5.0)) = 1.0
        _Color3AnimationSpeed ("Color 3 Animation Speed", Float) = 0.5
    }
    SubShader
    {
        Tags {"RenderType"="Transparent" "Queue"="AlphaTest"}
        LOD 100

        Pass
        {
            ZWrite On
            ZTest LEqual
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float _LineScale;
            float _AnimationSpeed;
            float _Normalizer;
            float _Sharpness;
            float _WaveAmount;
            float _WaveFrequency;
            float _Turbulence;
            float _RandomSeed;
            float _Rotation;
            float _LineThickness;
            float _LineWidth;
            float _Color3Intensity;
            float _Color3Frequency;
            float _Color3Sharpness;
            float _Color3LineCount;
            float _Color3AnimationSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            // Simple hash function for random values
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Noise function
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // 2D rotation function
            float2 rotate2d(float2 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Scale UV for line density with normalizer
                uv *= _LineScale * _Normalizer;
                
                // Apply rotation
                float angle = radians(_Rotation);
                uv = rotate2d(uv, angle);
                
                // Add simple animation
                float time = _Time.y * _AnimationSpeed;
                
                // Create wavy, turbulent distortion
                float2 distortedUV = uv;
                
                // Add wave distortion
                float wave = sin(uv.x * _WaveFrequency + time) * _WaveAmount;
                distortedUV.y += wave;
                
                // Add turbulence using noise
                float2 noiseUV = uv * 5.0 + time * 0.5;
                float turbulence = noise(noiseUV) * _Turbulence;
                distortedUV.y += turbulence;
                
                // Add random seed variation
                distortedUV += _RandomSeed * 0.1;
                
                // Create wavy, turbulent lines with thickness control
                float lineValue = sin(distortedUV.y * 3.14159 * _LineThickness);
                lineValue = lineValue * 0.5 + 0.5; // Map to 0-1
                
                // Apply line width to make individual lines thicker/thinner
                lineValue = smoothstep(0.5 - _LineWidth * 0.1, 0.5 + _LineWidth * 0.1, lineValue);
                
                // Apply sharpness to make lines more defined
                lineValue = pow(lineValue, _Sharpness);
                
                // Create accent pattern for third color (inspired by MasaShader)
                float color3Time = _Time.y * _Color3AnimationSpeed;
                float accentPattern = sin(distortedUV.x * _Color3Frequency * _Color3LineCount + color3Time * 0.7 + sin(distortedUV.y * _Color3Frequency * 1.25 * _Color3LineCount + color3Time * 1.3));
                accentPattern = accentPattern * 0.5 + 0.5; // Map to 0-1
                
                // Apply sharpness to third color pattern
                accentPattern = pow(accentPattern, _Color3Sharpness);
                
                // Blend between three colors
                float4 baseColor = lerp(_Color1, _Color2, lineValue);
                float4 finalColor = lerp(baseColor, _Color3, accentPattern * _Color3Intensity);
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}