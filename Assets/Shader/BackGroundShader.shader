Shader "Unlit/CircleGridPattern_WithStarsAttached"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorA ("Circle Color A", Color) = (0.075,0.114,0.329,1)
        _ColorB ("Circle Color B", Color) = (0.973,0.843,0.675,1)
        _ColorC ("Diamond Color", Color) = (0.761,0.247,0.102,1)
        _Zoom1 ("Grid 1 Zoom", Float) = 7
        _Zoom2 ("Grid 2 Zoom", Float) = 3
        _Radius1 ("Grid 1 Circle Radius", Float) = 0.23
        _Radius2 ("Grid 2 Diamond Radius", Float) = 0.2
        _AnimSpeed ("Animation Speed", Float) = 1
        _StarColor ("Star Color", Color) = (1,0.9,0.2,1)
        _StarRadius ("Star Radius", Range(0.01,0.5)) = 0.18
        _StarSharpness ("Star Sharpness", Range(2,32)) = 12
        _StarSoftness ("Star Edge Softness", Range(0.001,0.2)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _ColorA, _ColorB, _ColorC;
            float _Zoom1, _Zoom2, _Radius1, _Radius2, _AnimSpeed;
            float4 _StarColor;
            float _StarRadius, _StarSharpness, _StarSoftness;

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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float2 tile(float2 st, float zoom)
            {
                st *= zoom;
                return frac(st);
            }

            float circle(float2 st, float radius)
            {
                float2 pos = 0.5 - st;
                radius *= 0.75;
                float d = dot(pos,pos) * 3.14;
                return 1 - smoothstep(radius - radius*0.05, radius + radius*0.05, d);
            }
            float circlePattern(float2 st, float radius)
            {
                return
                    circle(st + float2(0,-0.5), radius) +
                    circle(st + float2(0, 0.5), radius) +
                    circle(st + float2(-0.5,0), radius) +
                    circle(st + float2( 0.5,0), radius);
            }

            float diamond(float2 st, float radius)
            {
                float2 pos = 0.5 - st;
                radius *= 0.75;
                float d = (abs(pos.x) + abs(pos.y)) * 3.14;
                return 1 - smoothstep(radius - radius*0.05, radius + radius*0.05, d);
            }
            float diamondPattern(float2 st, float radius)
            {
                return
                    diamond(st + float2(0,-0.5), radius) +
                    diamond(st + float2(0, 0.5), radius) +
                    diamond(st + float2(-0.5,0), radius) +
                    diamond(st + float2( 0.5,0), radius);
            }

            float starMask(float2 st, float radius, float sharp, float soft)
            {
                float2 p = st - 0.5;
                float ang = atan2(p.y, p.x);
                float len = length(p);
                float spikes = cos(ang * sharp) * 0.5 + 0.5;
                float edge   = lerp(radius*0.5, radius, spikes);
                return 1 - smoothstep(edge - soft, edge + soft, len);
            }
            float starPattern(float2 st, float radius, float sharp, float soft)
            {
                return
                    starMask(st + float2(0,-0.5), radius, sharp, soft) +
                    starMask(st + float2(0, 0.5), radius, sharp, soft) +
                    starMask(st + float2(-0.5,0), radius, sharp, soft) +
                    starMask(st + float2( 0.5,0), radius, sharp, soft);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                uv.x *= _ScreenParams.x / _ScreenParams.y;

                float t = _Time.y * _AnimSpeed;
                float2 anim1 = float2(cos(t),          sin(t)) * 0.01;
                float2 anim2 = float2(cos(t * 1.3),    sin(t * 1.3)) * 0.02;

                // 1) Circles
                float2 g1 = tile(uv + anim1, _Zoom1);
                float  c1 = circlePattern(g1, _Radius1) - circlePattern(g1, 0.01);
                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, saturate(c1));

                // 2) Diamonds
                float2 g2 = tile(uv + anim2, _Zoom2);
                float  d2 = diamondPattern(g2, _Radius2) - diamondPattern(g2, 0.05);
                col = lerp(col, _ColorC.rgb, saturate(d2));

                // 3) ★ Stars attached at each diamond center ★
                float rawStars   = starPattern(g2, _StarRadius, _StarSharpness, _StarSoftness);
                float diamondHit = saturate(diamondPattern(g2, _Radius2));
                float sMask      = saturate(rawStars * diamondHit);
                col = lerp(col, _StarColor.rgb, sMask);

                // Multiply by sprite alpha for proper transparency
                fixed4 spriteCol = tex2D(_MainTex, i.uv);
                col *= spriteCol.a;

                return float4(col, spriteCol.a);
            }
            ENDCG
        }
    }
}