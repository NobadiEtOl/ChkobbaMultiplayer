Shader "Unlit/StarShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _StarColor ("Star Color", Color) = (1, 1, 1, 1)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0.1, 1)
        _StarPoints ("Star Points", Range(3, 12)) = 5
        _StarBrightness ("Star Brightness", Range(0.1, 5)) = 2
        _PointSharpness ("Point Sharpness", Range(0.1, 5)) = 2
        _InnerRadius ("Inner Radius", Range(0.1, 0.9)) = 0.4
        _StarRotation ("Star Rotation", Range(0, 360)) = 0
        _LineThickness ("Line Thickness", Range(0.005, 0.1)) = 0.02
        _ExpansionSpeed ("Expansion Speed", Range(0.1, 5)) = 1
        _StarInterval ("Star Interval", Range(0.1, 2)) = 0.5
        _StartSize ("Start Size", Range(0.01, 0.1)) = 0.02
        _MaxSize ("Max Size", Range(0.5, 3)) = 1.5
        _FadeStart ("Fade Start", Range(0.1, 1)) = 0.8
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.05)) = 0.01
        _WaveFrequency ("Wave Frequency", Range(1, 20)) = 8
        _WaveSpeed ("Wave Speed", Range(0.1, 5)) = 2
        _RandomSeed ("Random Seed", Range(0, 100)) = 42
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _StarColor;
            fixed4 _BackgroundColor;
            float _StarPoints;
            float _StarBrightness;
            float _PointSharpness;
            float _InnerRadius;
            float _StarRotation;
            float _LineThickness;
            float _ExpansionSpeed;
            float _StarInterval;
            float _StartSize;
            float _MaxSize;
            float _FadeStart;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _RandomSeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Hash function for pseudo-random numbers
            float hash(float n)
            {
                return frac(sin(n + _RandomSeed) * 43758.5453);
            }

            // Rotate a 2D point around origin
            float2 rotate2D(float2 pos, float angle)
            {
                float rad = angle * 0.0174533; // Convert degrees to radians
                float cosA = cos(rad);
                float sinA = sin(rad);
                return float2(
                    pos.x * cosA - pos.y * sinA,
                    pos.x * sinA + pos.y * cosA
                );
            }

            // Smooth wavy star outline function
            float starShape(float2 uv, float2 center, float size, float points, float layerIndex)
            {
                float2 pos = uv - center;
                
                // Apply rotation
                pos = rotate2D(pos, _StarRotation);
                
                float dist = length(pos);
                float angle = atan2(pos.y, pos.x);
                
                // Normalize angle to 0-2π
                angle = angle + 3.14159;
                
                // Calculate which segment of the star we're in
                float segmentAngle = 6.28318 / points; // 2π / points
                float localAngle = fmod(angle, segmentAngle);
                float halfSegment = segmentAngle * 0.5;
                
                // Create sharp points by using abs and power functions
                float pointFactor = abs(localAngle - halfSegment) / halfSegment;
                pointFactor = pow(pointFactor, _PointSharpness);
                
                // Calculate base radius: full size at points, reduced at valleys
                float currentRadius = size * lerp(1.0, _InnerRadius, pointFactor);
                
                // Add smooth wavy distortion to the radius
                float waveTime = _Time.y * _WaveSpeed;
                
                // Create smoother random phase offset for each layer
                float randomPhase = hash(layerIndex) * 6.28318; // 0 to 2π
                
                // Primary wave with smooth random phase
                float primaryWave = sin(angle * _WaveFrequency + waveTime + randomPhase);
                
                // Secondary wave with different frequency and smooth random phase
                float secondaryPhase = hash(layerIndex + 100.0) * 6.28318;
                float secondaryWave = sin(angle * (_WaveFrequency * 1.7) + (waveTime * 0.8) + secondaryPhase);
                
                // Smooth random amplitude multiplier for each layer
                float randomAmplitude = 0.7 + hash(layerIndex + 200.0) * 0.3; // 0.7 to 1.0 (less variation)
                
                // Combine waves with smoother amplitude
                float totalWave = (primaryWave + secondaryWave * 0.3) * _WaveAmplitude * randomAmplitude * size;
                
                // Apply wave distortion to radius
                currentRadius += totalWave;
                
                // Create smoother hollow star outline
                float outerEdge = 1.0 - smoothstep(currentRadius - _LineThickness * 0.6, currentRadius + _LineThickness * 0.1, dist);
                float innerEdge = smoothstep(currentRadius - _LineThickness * 1.2, currentRadius - _LineThickness * 0.8, dist);
                
                // Combine to create outline
                float starMask = outerEdge * innerEdge;
                
                return starMask;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 center = float2(0.5, 0.5);
                
                fixed4 finalColor = _BackgroundColor;
                float totalStarBrightness = 0.0;
                
                // Current time
                float time = _Time.y * _ExpansionSpeed;
                
                // Create infinite expanding star layers with smoother transitions
                for (int layer = 0; layer < 15; layer++)
                {
                    // Calculate when this star layer started
                    float layerStartTime = float(layer) * _StarInterval;
                    float layerAge = time - layerStartTime;
                    
                    // Calculate the total cycle time
                    float totalCycleTime = 15.0 * _StarInterval;
                    
                    // Make layers cycle continuously with smoother transitions
                    while (layerAge < 0.0)
                    {
                        layerAge += totalCycleTime;
                    }
                    layerAge = fmod(layerAge, totalCycleTime);
                    
                    // Calculate current size with smoother growth curve
                    float growthTime = 4.0 * _StarInterval; // Total time to grow
                    float sizeProgress = saturate(layerAge / growthTime);
                    
                    // Use smoother exponential curve
                    float currentSize = _StartSize + (smoothstep(0.0, 1.0, sizeProgress) * (_MaxSize - _StartSize));
                    
                    // Only render if within reasonable size and time
                    if (layerAge <= growthTime && currentSize >= _StartSize)
                    {
                        // Generate smooth wavy star shape
                        float starValue = starShape(uv, center, currentSize, _StarPoints, float(layer));
                        
                        if (starValue > 0.0)
                        {
                            // Smooth fade out as star gets bigger
                            float fadeProgress = sizeProgress;
                            float fadeFactor = 1.0;
                            
                            if (fadeProgress > _FadeStart)
                            {
                                float fadeRange = 1.0 - _FadeStart;
                                float localFade = (fadeProgress - _FadeStart) / fadeRange;
                                fadeFactor = 1.0 - smoothstep(0.0, 1.0, localFade);
                            }
                            
                            // Apply brightness and smooth fade
                            starValue *= _StarBrightness * fadeFactor;
                            totalStarBrightness += starValue;
                        }
                    }
                }
                
                // Smooth blend with background
                finalColor.rgb = lerp(_BackgroundColor.rgb, _StarColor.rgb, saturate(totalStarBrightness));
                finalColor.a = _BackgroundColor.a + saturate(totalStarBrightness) * _StarColor.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
}