Shader "Hidden/PixelateSwitch"
{
    Properties
    {
        _Mode ("Mode (0:Off,1:Pixelate,2:Pixel+Halftone)", Int) = 1
        _PixelWidth  ("Pixel Width",  Float) = 320
        _PixelHeight ("Pixel Height", Float) = 180
        _ColorSteps  ("Color Steps (Posterize)", Float) = 8
        _HalftoneScale ("Halftone Dot Scale", Float) = 1.0
        _HalftoneStrength ("Halftone Strength", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags{ "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "PixelateSwitchPass"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // URP Core
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Source texture from Blit
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                int   _Mode;
                float _PixelWidth;
                float _PixelHeight;
                float _ColorSteps;
                float _HalftoneScale;
                float _HalftoneStrength;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings Vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float3 Posterize(float3 c, float steps)
            {
                steps = max(1.0, steps);
                return floor(c * steps) / steps;
            }

            // 簡易ハーフトーン（スクリーン座標上の円ドット）
            float HalftoneMask(float2 uv, float scale)
            {
                // スクリーン座標系で格子を作る
                float2 p = uv * scale;
                float2 g = frac(p) - 0.5;        // 各セルの中心を原点に
                float  r = length(g) * 2.0;      // 半径（0〜約1）
                // 中心が黒く周辺が白いドット（円形）
                float dot = saturate(1.0 - smoothstep(0.3, 0.5, r));
                return dot;
            }

            float4 Frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                if (_Mode == 0) return col;

                // ピクセレート：画面を指定の粗さに量子化
                float2 pixelCount = float2(max(8.0, _PixelWidth), max(8.0, _PixelHeight));
                float2 pixelUV = floor(uv * pixelCount) / pixelCount;
                col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelUV);

                // ポスタライズ（色段階を粗く）
                col.rgb = Posterize(col.rgb, _ColorSteps);

                if (_Mode == 2)
                {
                    // ハーフトーン（明るさに応じてドットの濃さを変える）
                    float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                    // 画面解像度に対して一定見え方になるよう、PixelWidth/Heightから大体のスケールを導出
                    float approxScale = lerp(64.0, 512.0, saturate(_HalftoneScale)); // ざっくり調整用
                    float dotMask = HalftoneMask(pixelUV, approxScale);
                    // 明るい所はドット薄く、暗い所はドット濃く
                    float mixAmt = _HalftoneStrength * (1.2 - lum);
                    col.rgb = lerp(col.rgb, col.rgb * (1.0 - dotMask), mixAmt);
                }

                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
