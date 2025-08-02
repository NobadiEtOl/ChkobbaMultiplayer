Shader "Unlit/PatternLinesNoise_ScaleAware"
{
    Properties
    {
        _MainColor ("Background Color", Color) = (0.8, 0.6, 0.4, 1)
        _LineColor ("Line Color", Color) = (0.12, 0.05, 0.03, 1)
        _AccentColor ("Accent Color", Color) = (0.5, 0.3, 0.1, 1)
        _ScaleX ("Pattern Scale X", Float) = 10
        _ScaleY ("Pattern Scale Y", Float) = 3
        _LineScale ("Line Scale", Float) = 10
        _NoiseStrength ("Noise Strength", Float) = 1
        _Speed ("Animation Speed", Float) = 1
        _ColorMode ("Color Mode (0=Original, 1=Enhanced)", Range(0,1)) = 0
        _BandWidth ("Band Width", Float) = 0.18
        _BandColor ("Band Edge Color", Color) = (0.7, 0.7, 0.7, 1)
        _BandIntensity ("Band Edge Intensity", Range(0,1)) = 0.3
        
        // Scale-aware properties
        _ObjectScale ("Object Scale", Vector) = (1, 1, 1, 1)
        _ScaleFactor ("Scale Factor", Float) = 1
        _AspectRatio ("Aspect Ratio", Float) = 1
        _ThicknessFactor ("Thickness Factor", Float) = 1
        _NormalizedScale ("Normalized Scale", Vector) = (1, 1, 1, 1)
        
        // Rotation support for 90° and 270°
        _Rotation90 ("90° Rotation", Range(0,1)) = 0
        _Rotation270 ("270° Rotation", Range(0,1)) = 0
        _AutoDetectRotation ("Auto Detect Rotation", Range(0,1)) = 1
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
            float4 _AccentColor;
            float _ScaleX;
            float _ScaleY;
            float _LineScale;
            float _NoiseStrength;
            float _Speed;
            float _ColorMode;
            float _BandWidth;
            float4 _BandColor;
            float _BandIntensity;
            
            // Scale-aware properties
            float4 _ObjectScale;
            float _ScaleFactor;
            float _AspectRatio;
            float _ThicknessFactor;
            float4 _NormalizedScale;
            
            // Rotation properties
            float _Rotation90;
            float _Rotation270;
            float _AutoDetectRotation;

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

            // Scale-aware pattern generation
            float lines(float2 pos, float t)
            {
                // Apply scale-aware adjustments
                float adjustedLineScale = _LineScale * _ScaleFactor;
                
                // Adjust for extreme aspect ratios
                if (_AspectRatio > 10.0) // Very thin objects
                {
                    adjustedLineScale *= _AspectRatio * 0.1;
                }
                
                pos *= adjustedLineScale;

                // Add multiple sine waves for richer lines
                float baseLine = sin((pos.x + t) * 3.1415);
                float wave1 = 0.3 * sin(pos.y * 2.0 + t * 0.7);
                float wave2 = 0.15 * sin(pos.x * 2.5 - t * 1.2 + pos.y * 1.3);
                float wave3 = 0.1 * sin((pos.x + pos.y) * 4.0 + t * 0.3);

                float combined = baseLine + wave1 + wave2 + wave3;

                // Add some sharpness and banding
                float sharp = smoothstep(0.0, 0.5, abs(combined));
                float band = smoothstep(0.2, 0.7, abs(frac(combined * 0.5 + 0.5) - 0.5));

                // Mix for more visual interest
                return lerp(sharp, band, 0.5 + 0.5 * sin(t + pos.x * 0.2));
            }

            float accentPattern(float2 pos, float t)
            {
                float wave = sin(pos.x * 2.0 + t * 0.7 + sin(pos.y * 2.5 + t * 1.3));
                return smoothstep(0.3, 0.7, 0.5 + 0.5 * wave);
            }

            // Auto-detect rotation based on object scale
            float detectRotation()
            {
                if (_AutoDetectRotation > 0.5)
                {
                    // If object is very thin (high aspect ratio), likely rotated 90° or 270°
                    if (_AspectRatio > 5.0)
                    {
                        // Check if it's more like 90° or 270° based on scale values
                        if (_ObjectScale.x < _ObjectScale.y * 0.1)
                        {
                            return 1.0; // 90° rotation
                        }
                        else if (_ObjectScale.y < _ObjectScale.x * 0.1)
                        {
                            return 2.0; // 270° rotation
                        }
                    }
                }
                return 0.0; // No rotation
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 st = i.uv;
                st.y *= _ScreenParams.y / _ScreenParams.x;

                // Apply scale-aware UV adjustments
                float2 adjustedUV = st;
                
                // Adjust UV based on object scale
                if (_ObjectScale.x > 0.0 && _ObjectScale.y > 0.0)
                {
                    // Normalize UVs based on object scale
                    adjustedUV.x *= _ObjectScale.x;
                    adjustedUV.y *= _ObjectScale.y;
                }
                
                // Apply aspect ratio correction
                if (_AspectRatio > 1.0)
                {
                    adjustedUV.x *= _AspectRatio;
                }
                else if (_AspectRatio < 1.0)
                {
                    adjustedUV.y *= 1.0 / _AspectRatio;
                }

                float2 pos = adjustedUV.yx * float2(_ScaleX, _ScaleY);

                // Apply rotation if detected or manually set
                float rotationType = detectRotation();
                if (rotationType > 0.0 || _Rotation90 > 0.5 || _Rotation270 > 0.5)
                {
                    float rotationAngle = 0.0;
                    
                    if (_Rotation90 > 0.5 || rotationType == 1.0)
                    {
                        rotationAngle = 1.5708; // 90° in radians
                    }
                    else if (_Rotation270 > 0.5 || rotationType == 2.0)
                    {
                        rotationAngle = 4.7124; // 270° in radians
                    }
                    
                    if (rotationAngle != 0.0)
                    {
                        pos = rotate2d(pos, rotationAngle);
                    }
                }

                float t = _Time.y * _Speed;

                float n = noise(pos) * _NoiseStrength;
                pos = rotate2d(pos, n);

                float pattern = lines(pos, t);

                // Accent pattern for extra color
                float accent = accentPattern(pos * 0.7, t);

                // Blend three colors based on pattern and accent
                float3 colorA = _MainColor.rgb;
                float3 colorB = _LineColor.rgb;
                float3 colorC = _AccentColor.rgb;

                // Use pattern for main stripes, accent for wavy overlay
                float blendAB = pattern;
                float blendBC = accent * 0.7;

                float3 baseColor = lerp(colorA, colorB, blendAB);
                float3 finalColor = lerp(baseColor, colorC, blendBC * (1.0 - blendAB) * 0.8);

                // Enhanced mode with band edges (copied from Ebru shader logic)
                if (_ColorMode > 0.5)
                {
                    float bandPos = pattern * _LineScale;
                    float bandEdge = smoothstep(_BandWidth, _BandWidth * 0.7, frac(bandPos));
                    finalColor = lerp(finalColor, _BandColor.rgb, bandEdge * _BandIntensity);
                }

                // Add a subtle vignette for depth
                float2 center = float2(0.5, 0.5);
                float vignette = smoothstep(0.9, 0.5, distance(st, center));
                finalColor *= lerp(1.0, 0.7, vignette * 0.5);

                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }
} 