Shader "SharderUltimate/GemFrame"
{
    // Thay viền (khung bo góc) của ảnh bằng đá quý low-poly: mặt cắt tam giác, gờ sáng mép trong,
    // ánh sáng đảo qua lại làm các mặt loé, sao lấp lánh và một lớp bóng: tráng gương (vệt quét) hoặc satin (_SheenType).
    // _Cover < 1 để đá mọc dần phủ lên viền gốc của ảnh (từng mặt cắt hiện ra, mép phủ loé sáng).
    // Dùng cho UI Image lẫn SpriteRenderer / Quad. Sprite không được đóng atlas (shader cần UV 0..1 của cả ảnh).
    // Khung được tả bằng 2 hình chữ nhật bo góc theo pixel của ảnh gốc (_RefSize), gốc toạ độ ở góc dưới trái,
    // nên ảnh import nhỏ/lớn hơn vẫn đúng.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Frame shape px of source image y from bottom)]
        _RefSize ("Reference Size (w, h px)", Vector) = (390, 476, 0, 0)
        _OuterRect ("Outer Rect (xMin, yMin, xMax, yMax)", Vector) = (5.5, 12.5, 384.5, 475.5)
        _InnerRect ("Inner Rect (xMin, yMin, xMax, yMax)", Vector) = (30, 34, 359, 451)
        _Radius ("Corner Radius (outer, inner)", Vector) = (110, 88, 0, 0)

        [Header(Facets)]
        _FacetSize ("Facet Size (px)", Range(4, 40)) = 17
        _FacetJitter ("Facet Jitter", Range(0, 0.24)) = 0.22
        _FacetSeed ("Facet Seed", Float) = 0
        _Bevel ("Bevel Height (px)", Range(0, 20)) = 7
        _FacetDepth ("Facet Depth (px)", Range(0, 20)) = 7
        _FacetContrast ("Facet Contrast", Range(0, 2)) = 0.9
        _FacetRandom ("Facet Random Shade", Range(0, 1)) = 0.5
        _HueVariation ("Alt Color Variation", Range(0, 1)) = 1
        _FacetLine ("Facet Edge Lines", Range(0, 1)) = 0.18

        [Header(Colors blue violet. Alt colors mix in per facet)]
        _ColorDeep ("Deep (indigo)", Color) = (0.13, 0.06, 0.5, 1)
        _ColorDeepAlt ("Deep Alt (navy)", Color) = (0.06, 0.1, 0.48, 1)
        _ColorMid ("Mid (blue violet)", Color) = (0.34, 0.2, 0.88, 1)
        _ColorMidAlt ("Mid Alt (royal blue)", Color) = (0.2, 0.3, 0.9, 1)
        _ColorLight ("Light (periwinkle)", Color) = (0.56, 0.52, 1, 1)
        _ColorLightAlt ("Light Alt (sky)", Color) = (0.48, 0.64, 1, 1)
        _OutlineColor ("Outline", Color) = (0.14, 0.1, 0.48, 1)
        _OutlineWidth ("Outline Width (px)", Range(0, 4)) = 1.2
        _LipHighlight ("Inner Lip Highlight", Color) = (0.87, 0.9, 1, 1)
        _LipWidth ("Inner Lip Width (0..1 of band)", Range(0, 0.5)) = 0.16

        [Header(Light)]
        _LightAngle ("Light Angle (deg)", Range(0, 360)) = 135
        _LightSwing ("Light Swing (rad)", Range(0, 2)) = 0.9
        _LightSpeed ("Light Swing Speed", Range(0, 5)) = 0.7
        _Tilt ("Tilt (xy) set by script", Vector) = (0, 0, 0, 0)
        _Specular ("Specular", Range(0, 3)) = 1
        _SpecularPower ("Specular Sharpness", Range(4, 128)) = 28

        [Header(Sparkle)]
        _Sparkle ("Sparkle Intensity", Range(0, 4)) = 1.8
        _SparkleCell ("Sparkle Cell (px)", Range(8, 80)) = 26
        _SparkleDensity ("Sparkle Density", Range(0, 1)) = 0.6
        _SparkleSize ("Sparkle Max Size (px)", Range(2, 40)) = 16
        _SparkleSpeed ("Sparkle Speed", Range(0, 5)) = 2

        [Header(Sheen)]
        [Enum(Mirror, 0, Satin, 1, None, 2)] _SheenType ("Sheen Type", Float) = 0
        _SheenColor ("Sheen Color (sea blue)", Color) = (0.35, 0.78, 1, 1)

        [Header(Mirror sheen)]
        _Mirror ("Mirror Intensity", Range(0, 2)) = 1.2
        _MirrorAngle ("Mirror Direction (deg, -90 = straight down)", Range(-180, 180)) = -62
        _MirrorWidth ("Mirror Width (px)", Range(2, 80)) = 24
        _MirrorDuration ("Mirror Pass Duration (s)", Range(0.2, 10)) = 1.8
        _MirrorPeriod ("Mirror Period (s)", Range(0.2, 20)) = 4

        [Header(Satin sheen)]
        _Satin ("Satin Intensity", Range(0, 2)) = 1
        [IntRange] _SatinWaves ("Satin Waves Around Frame", Range(1, 12)) = 4
        _SatinSpeed ("Satin Flow Speed", Range(0, 3)) = 0.6
        _SatinWidth ("Satin Highlight Width", Range(0.05, 1)) = 0.45

        [Header(Color flow)]
        _Flow ("Color Flow (0 palette, 1 flow colors)", Range(0, 1)) = 0
        _FlowColorA ("Flow Color A (cyan)", Color) = (0.38, 0.8, 1, 1)
        _FlowColorB ("Flow Color B (violet)", Color) = (0.62, 0.3, 1, 1)
        _FlowScale ("Flow Blob Size (px)", Range(10, 300)) = 90
        _FlowSpeed ("Flow Speed", Range(0, 3)) = 0.6
        [IntRange] _FlowWaves ("Flow Waves Around Frame", Range(1, 8)) = 2
        _FlowShift ("Whole Frame A/B Swing", Range(0, 1)) = 0.4

        [Header(Outer glow. UI Image needs UIQuadPadding to have room)]
        _Glow ("Outer Glow", Range(0, 2)) = 0
        _GlowWidth ("Glow Width (px)", Range(1, 60)) = 12

        [Header(Cover reveal. Gem grows over the original frame)]
        _Cover ("Cover (0..1, script can animate)", Range(0, 1)) = 1
        _CoverLoop ("Auto Loop Period (s), 0 = use Cover", Range(0, 20)) = 0
        _CoverOrigin ("Start Angle (deg, 90 = top)", Range(-180, 180)) = 90
        _CoverRandom ("Front Raggedness", Range(0, 0.5)) = 0.12
        _CoverSoft ("Facet Pop Softness", Range(0.005, 0.2)) = 0.04
        _CoverFlash ("Front Flash", Range(0, 3)) = 1.2

        [Header(Inner Shadow)]
        _InnerShadow ("Inner Shadow", Range(0, 1)) = 0.45
        _InnerShadowWidth ("Inner Shadow Width (px)", Range(0.5, 20)) = 5

        // UI mask / stencil, giống UI/Default
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"
            "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float4 _RefSize, _OuterRect, _InnerRect, _Radius;
                float _FacetSize, _FacetJitter, _FacetSeed, _Bevel, _FacetDepth;
                half _FacetContrast, _FacetRandom, _HueVariation, _FacetLine;
                half4 _ColorDeep, _ColorDeepAlt, _ColorMid, _ColorMidAlt, _ColorLight, _ColorLightAlt, _OutlineColor, _LipHighlight;
                half _OutlineWidth, _LipWidth;
                half _LightAngle, _LightSwing, _LightSpeed, _Specular, _SpecularPower;
                float4 _Tilt;
                half _Sparkle, _SparkleDensity, _SparkleSpeed;
                float _SparkleCell, _SparkleSize;
                half _SheenType;
                half4 _SheenColor;
                half _Mirror;
                float _MirrorAngle, _MirrorWidth, _MirrorDuration, _MirrorPeriod;
                half _Satin, _SatinWaves, _SatinSpeed, _SatinWidth;
                half _Flow, _FlowSpeed, _FlowWaves, _FlowShift;
                half4 _FlowColorA, _FlowColorB;
                float _FlowScale;
                half _Glow;
                float _GlowWidth;
                float _Cover, _CoverLoop, _CoverOrigin, _CoverRandom, _CoverSoft;
                half _CoverFlash;
                half _InnerShadow, _InnerShadowWidth;
            CBUFFER_END

            float4 _ClipRect; // RectMask2D, Canvas tự gán

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color       : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color       : COLOR;
                float2 uv         : TEXCOORD0;
                float2 positionOS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xy;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            // ---------------------------------------------------------------- utils
            // Hash không dùng sin để ổn định trên mobile
            float Hash12(float2 p)
            {
                float3 p3 = frac(p.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(p.xyx * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash12(i), Hash12(i + float2(1, 0)), f.x),
                            lerp(Hash12(i + float2(0, 1)), Hash12(i + float2(1, 1)), f.x), f.y);
            }

            // SDF hình chữ nhật bo góc, rect = (xMin, yMin, xMax, yMax). < 0 bên trong
            float RoundRectSdf(float2 p, float4 rect, float r)
            {
                float2 c = (rect.xy + rect.zw) * 0.5;
                float2 h = (rect.zw - rect.xy) * 0.5;
                float2 q = abs(p - c) - h + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            float OuterSdf(float2 p) { return RoundRectSdf(p, _OuterRect, _Radius.x); }
            float InnerSdf(float2 p) { return RoundRectSdf(p, _InnerRect, _Radius.y); }

            // Vị trí ngang dải khung: 0 ở mép trong, 1 ở mép ngoài
            float BandS(float dOut, float dIn) { return saturate(dIn / max(dIn - dOut, 1e-3)); }

            // Dòng màu A <-> B: mảng màu (méo bằng noise) trôi vòng quanh khung, cộng nhịp đổi tông của cả khung
            half3 FlowColor(float2 p, float t)
            {
                float2 q = p - (_OuterRect.xy + _OuterRect.zw) * 0.5;
                float ang = atan2(q.y, q.x * (_RefSize.y / _RefSize.x));
                float2 np = p / _FlowScale;
                float ft = t * _FlowSpeed;
                float warp = ValueNoise(np + float2(ft * 0.23, -ft * 0.17)) * 0.65 + ValueNoise(np * 2.1 - ft * 0.31) * 0.35;
                float f = 0.5 + 0.5 * sin(ang * _FlowWaves - ft * 1.2 + warp * 5.0);
                f = smoothstep(0.25, 0.75, saturate(f + sin(ft * 0.7) * _FlowShift));
                return lerp(_FlowColorA.rgb, _FlowColorB.rgb, f);
            }

            // ---------------------------------------------------------------- cover reveal
            // Độ phủ hiện tại: _Cover do script đặt, hoặc tự lặp: mọc trong 60% chu kỳ, giữ, rồi rút nhanh ở 15% cuối
            float CoverAmount(float t)
            {
                if (_CoverLoop <= 0.0) return _Cover;
                float ph = frac(t / _CoverLoop);
                return ph < 0.85 ? saturate(ph / 0.6) : saturate((1.0 - ph) / 0.15);
            }

            // Khoảng cách góc tới điểm bắt đầu phủ, 0..1 (1 = phía đối diện): đá mọc theo 2 chiều quanh khung
            float CoverDistance(float2 p)
            {
                float2 q = p - (_OuterRect.xy + _OuterRect.zw) * 0.5;
                float x = (atan2(q.y, q.x * (_RefSize.y / _RefSize.x)) - radians(_CoverOrigin)) / 6.2831853;
                return abs(frac(x + 0.5) - 0.5) * 2.0;
            }

            // Mép phủ đã giãn để cover = 0 chưa có gì, cover = 1 phủ kín cả mặt có ngưỡng 1
            float CoverFront(float cover) { return cover * (1.0 + 2.0 * _CoverSoft) - _CoverSoft; }

            // Độ phủ trung bình tại một điểm (bỏ qua nhiễu từng mặt), dùng cho glow, bóng đổ, sao
            half CoverAt(float2 p, float cover)
            {
                float thr = CoverDistance(p) * (1.0 - _CoverRandom) + _CoverRandom * 0.5;
                return saturate((CoverFront(cover) - thr) / (_CoverSoft * 3.0));
            }

            // ---------------------------------------------------------------- facets
            // Lưới ô vuông có đỉnh xô lệch (jitter <= 0.24 thì tứ giác vẫn lồi), mỗi ô chia 2 tam giác
            // theo đường chéo ngẫu nhiên. Mỗi đỉnh có độ cao = gờ khung + nhiễu, pháp tuyến mặt suy từ 3 đỉnh
            // nên các mặt kề nhau khớp cạnh như khối pha lê thật.
            float2 LatticeVertex(float2 ij)
            {
                return (ij + (Hash22(ij + _FacetSeed) - 0.5) * 2.0 * _FacetJitter) * _FacetSize;
            }

            float VertexHeight(float2 ij, float2 pos)
            {
                float s = BandS(OuterSdf(pos), InnerSdf(pos));
                return _Bevel * sin(PI * s) + (Hash12(ij + _FacetSeed + 71.3) - 0.5) * _FacetDepth;
            }

            bool Barycentric(float2 p, float2 a, float2 b, float2 c, out float3 w)
            {
                float2 v0 = b - a, v1 = c - a, v2 = p - a;
                float d = v0.x * v1.y - v1.x * v0.y;
                float w1 = (v2.x * v1.y - v1.x * v2.y) / d;
                float w2 = (v0.x * v2.y - v2.x * v0.y) / d;
                w = float3(1.0 - w1 - w2, w1, w2);
                return min(w.x, min(w.y, w.z)) >= -1e-4;
            }

            struct Facet
            {
                float2 ia, ib, ic;  // id đỉnh trên lưới
                float2 a, b, c;     // vị trí đỉnh (px)
                float3 w;           // barycentric
                float2 id;
            };

            Facet FindFacet(float2 p)
            {
                float2 base = floor(p / _FacetSize);
                // Lưới 4x4 đỉnh quanh ô chứa p, mỗi đỉnh chỉ hash một lần
                float2 V[4][4];
                [unroll] for (int vy = 0; vy < 4; vy++)
                [unroll] for (int vx = 0; vx < 4; vx++)
                    V[vy][vx] = LatticeVertex(base + float2(vx - 1, vy - 1));

                // Phòng khi không tìm thấy (không xảy ra nếu jitter <= 0.24): tam giác phẳng quanh p
                Facet f;
                f.ia = base; f.ib = base + float2(1, 0); f.ic = base + float2(0, 1);
                f.a = p; f.b = p + float2(1, 0); f.c = p + float2(0, 1);
                f.w = float3(1, 0, 0); f.id = base;
                bool found = false;

                [unroll] for (int y = 0; y < 3; y++)
                [unroll] for (int x = 0; x < 3; x++)
                {
                    float2 g = base + float2(x - 1, y - 1);
                    float2 i00 = g, i10 = g + float2(1, 0), i01 = g + float2(0, 1), i11 = g + float2(1, 1);
                    float2 p00 = V[y][x], p10 = V[y][x + 1], p01 = V[y + 1][x], p11 = V[y + 1][x + 1];
                    bool flip = Hash12(g + _FacetSeed + 13.7) > 0.5;
                    float3 w;
                    // Tam giác 1
                    float2 qc = flip ? p01 : p11, jc = flip ? i01 : i11;
                    if (!found && Barycentric(p, p00, p10, qc, w))
                    {
                        f.ia = i00; f.ib = i10; f.ic = jc; f.a = p00; f.b = p10; f.c = qc; f.w = w; f.id = g * 2.0;
                        found = true;
                    }
                    // Tam giác 2
                    float2 ra = flip ? p10 : p00, ka = flip ? i10 : i00;
                    if (!found && Barycentric(p, ra, p11, p01, w))
                    {
                        f.ia = ka; f.ib = i11; f.ic = i01; f.a = ra; f.b = p11; f.c = p01; f.w = w; f.id = g * 2.0 + float2(1, 0);
                        found = true;
                    }
                }
                return f;
            }

            // reveal: mặt cắt này đã được đá phủ tới đâu (0..1), glint: độ loé tại mép phủ
            half3 GemColor(float2 p, float dOut, float dIn, float t, float aa, float cover, out half reveal, out half glint)
            {
                Facet f = FindFacet(p);
                float3 A = float3(f.a, VertexHeight(f.ia, f.a));
                float3 B = float3(f.b, VertexHeight(f.ib, f.b));
                float3 C = float3(f.c, VertexHeight(f.ic, f.c));
                float3 n = normalize(cross(B - A, C - A));
                n *= sign(n.z + 1e-6);
                half rnd = Hash12(f.id + _FacetSeed + 0.37);
                half hv = Hash12(f.id + _FacetSeed + 11.1) * _HueVariation;

                // Mỗi mặt hiện ra trọn vẹn khi mép phủ đi qua tâm của nó, lệch ngẫu nhiên nên mép phủ lởm chởm theo mặt cắt
                float thr = CoverDistance((f.a + f.b + f.c) / 3.0) * (1.0 - _CoverRandom)
                          + Hash12(f.id + _FacetSeed + 5.5) * _CoverRandom;
                float front = CoverFront(cover) - thr;
                reveal = saturate(front / _CoverSoft);
                float gx = (front - _CoverSoft * 0.5) / (_CoverSoft * 1.2);
                glint = exp(-gx * gx) * smoothstep(0.0, 0.02, cover) * (1.0 - smoothstep(0.95, 1.0, cover));

                // Đèn đảo qua lại quanh góc _LightAngle, script có thể đẩy thêm bằng _Tilt
                float ang = radians(_LightAngle) + sin(t * _LightSpeed) * _LightSwing;
                float2 L2 = normalize(float2(cos(ang), sin(ang)) + _Tilt.xy);
                float3 L = normalize(float3(L2 * 0.8, 0.6));

                // Mặt hướng về phía đèn sáng hơn, thêm độ sáng ngẫu nhiên từng mặt
                half shade = saturate(0.45 + dot(n.xy, L2) * _FacetContrast + (rnd - 0.5) * _FacetRandom);
                half3 dark = lerp(_ColorDeep.rgb, _ColorDeepAlt.rgb, hv);
                half3 mid = lerp(_ColorMid.rgb, _ColorMidAlt.rgb, hv);
                half3 light = lerp(_ColorLight.rgb, _ColorLightAlt.rgb, hv);
                if (_Flow > 0.0)
                {
                    half3 fc = FlowColor(p, t);
                    dark = lerp(dark, fc * 0.45, _Flow);
                    mid = lerp(mid, fc, _Flow);
                    light = lerp(light, lerp(fc, 1.0.xxx, 0.5), _Flow);
                }
                half3 col = shade < 0.5 ? lerp(dark, mid, shade * 2.0) : lerp(mid, light, shade * 2.0 - 1.0);

                float3 H = normalize(L + float3(0, 0, 1));
                half spec = pow(saturate(dot(n, H)), _SpecularPower) * _Specular;
                col += lerp(1.0.xxx, _ColorLightAlt.rgb, 0.3) * spec;

                // Đường cạnh giữa các mặt: khoảng cách (px) tới cạnh gần nhất của tam giác
                float area2 = abs((f.b.x - f.a.x) * (f.c.y - f.a.y) - (f.c.x - f.a.x) * (f.b.y - f.a.y));
                float eDist = min(min(f.w.x * area2 / length(f.b - f.c), f.w.y * area2 / length(f.c - f.a)),
                                  f.w.z * area2 / length(f.a - f.b));
                half eLine = 1.0 - smoothstep(0.0, aa * 1.2, eDist);
                col += _ColorLight.rgb * eLine * _FacetLine * (0.3 + rnd);

                // Gờ mép trong: dải sáng mịn, mặt quay vào trong nên sáng theo hướng đèn
                float2 e = float2(0.5, 0.0);
                float2 gIn = float2(InnerSdf(p + e.xy) - InnerSdf(p - e.xy), InnerSdf(p + e.yx) - InnerSdf(p - e.yx));
                gIn = normalize(gIn + 1e-5);
                half lipT = BandS(dOut, dIn) / max(_LipWidth, 1e-3);
                half lipMask = 1.0 - smoothstep(0.75, 1.0, lipT);
                half lipLit = 0.8 + 0.35 * dot(-gIn, L2);
                half3 lipCol = lerp(_ColorLight.rgb, _LipHighlight.rgb,
                                    smoothstep(0.2, 0.7, lipT) * (1.0 - smoothstep(0.7, 1.0, lipT))) * lipLit;
                col = lerp(col, lipCol, lipMask * step(1e-3, _LipWidth));

                // Viền tối mảnh ở 2 mép
                half outerLine = 1.0 - smoothstep(_OutlineWidth * 0.5, _OutlineWidth * 1.5, -dOut);
                half innerLine = 1.0 - smoothstep(_OutlineWidth * 0.35, _OutlineWidth * 1.25, dIn);
                col = lerp(col, _OutlineColor.rgb, max(outerLine, innerLine) * 0.9 * step(1e-3, _OutlineWidth));

                float2 cc = (_OuterRect.xy + _OuterRect.zw) * 0.5;
                if (_SheenType < 0.5)
                {
                    // Tráng gương: vệt dày sắc cạnh + vệt mảnh đi sau, quét theo _MirrorAngle (mặc định từ trên xuống, hơi nghiêng).
                    // Mỗi mặt cắt phản chiếu mạnh yếu khác nhau nên vệt bị vỡ theo mặt như gương pha lê
                    float an = radians(_MirrorAngle);
                    float2 dir = float2(cos(an), sin(an));
                    float ext = 0.5 * (abs(dir.x) * _RefSize.x + abs(dir.y) * _RefSize.y) + _MirrorWidth * 3.0;
                    float x = dot(p - cc, dir) - (fmod(t, _MirrorPeriod) / _MirrorDuration * 2.0 * ext - ext);
                    half wide = saturate((_MirrorWidth * 0.5 - abs(x)) / aa + 0.5);
                    half lead = saturate(x / _MirrorWidth + 0.5);
                    half thin = saturate((_MirrorWidth * 0.13 - abs(x + _MirrorWidth * 1.25)) / aa + 0.5);
                    half edge = saturate((1.2 - abs(x - _MirrorWidth * 0.5 + 1.0)) / aa + 0.5) * wide; // cạnh gương ở mép trước
                    half m = saturate((wide * (0.55 + 0.45 * lead) + thin) * lerp(0.6, 1.15, rnd));
                    col = lerp(col, _SheenColor.rgb * (0.8 + 0.4 * lead), saturate(m * _Mirror * 0.75));
                    col += lerp(_SheenColor.rgb, 1.0.xxx, 0.8) * edge * 0.6 * _Mirror;
                }
                else if (_SheenType < 1.5)
                {
                    // Satin: dải bóng mềm uốn lượn chạy vòng quanh khung (số sóng nguyên để không lộ đường nối của atan2),
                    // đường sáng nhất trượt qua lại trên mặt cong của dải, giữa các dải hơi tối lại như nếp vải
                    float2 q = p - cc;
                    float ang = atan2(q.y, q.x * (_RefSize.y / _RefSize.x));
                    float warp = ValueNoise(p / 45.0 + float2(t * 0.15, 0.0)) * 0.7 + ValueNoise(p / 20.0 - t * 0.1) * 0.3;
                    half hi = pow(0.5 + 0.5 * cos(ang * _SatinWaves - t * _SatinSpeed * 1.5708 + warp * 3.0), 2.5 / _SatinWidth);
                    float ridge = 0.5 + 0.25 * sin(ang * 2.0 + t * _SatinSpeed * 1.3 + warp * 2.0);
                    float gr = (BandS(dOut, dIn) - ridge) / 0.3;
                    half cr = exp(-gr * gr);
                    half satin = (hi * (0.4 + 0.6 * cr) + 0.15 * cr) * lerp(0.85, 1.1, rnd);
                    col *= 1.0 - 0.3 * (1.0 - hi) * _Satin;
                    col += (1.0 - col) * lerp(_SheenColor.rgb, 1.0.xxx, 0.3) * saturate(satin * _Satin);
                }
                return col;
            }

            // ---------------------------------------------------------------- sparkles
            // Sao 4 cánh nằm trong dải khung, nhấp nháy lệch pha. Cánh sao được phép tràn ra ngoài khung
            half Sparkles(float2 p, float t, float cover)
            {
                float2 base = floor(p / _SparkleCell);
                half acc = 0.0;
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float2 g = base + float2(x, y);
                    if (Hash12(g + _FacetSeed + 9.3) > _SparkleDensity) continue;
                    float2 c = (g + 0.15 + Hash22(g + _FacetSeed + 5.1) * 0.7) * _SparkleCell;
                    if (OuterSdf(c) > -3.0 || InnerSdf(c) < 2.0 || CoverAt(c, cover) < 0.5) continue;

                    float ph = Hash12(g + 3.7) * 6.2831853;
                    float sp = 0.6 + Hash12(g + 1.9) * 1.2;
                    half tw = pow(saturate(sin(t * sp + ph)), 3.0);
                    float len = lerp(_SparkleSize * 0.375, _SparkleSize, Hash12(g + 7.7)) * (0.4 + 0.6 * tw);
                    float2 d = p - c;
                    half r1 = exp(-abs(d.y) / 0.55) * pow(saturate(1.0 - abs(d.x) / len), 2.5);
                    half r2 = exp(-abs(d.x) / 0.55) * pow(saturate(1.0 - abs(d.y) / len), 2.5);
                    half glow = exp(-dot(d, d) / (len * len * 0.06));
                    acc += (r1 + r2 + glow * 0.8) * tw;
                }
                return acc;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 src = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                // UIQuadPadding nới quad ra ngoài ảnh: phần UV ngoài 0..1 là trong suốt
                float2 in01 = step(0.0, IN.uv) * step(IN.uv, 1.0);
                src *= in01.x * in01.y;
                float2 p = IN.uv * _RefSize.xy;
                float t = _Time.y;
                float cover = CoverAmount(t);

                float dOut = OuterSdf(p);
                float dIn = InnerSdf(p);
                // SDF có độ dốc 1 nên fwidth(dOut) ~ kích thước 1 pixel màn hình tính theo px ảnh gốc
                float aa = max(fwidth(dOut), 1e-4);
                half gemMask = saturate(0.5 - dOut / aa) * saturate(0.5 + dIn / aa);

                // Khung đổ bóng nhẹ lên ảnh bên trong (chỉ ở đoạn đá đã phủ)
                half sh = exp(-max(-dIn, 0.0) / _InnerShadowWidth) * _InnerShadow * step(dIn, 0.0);
                if (cover < 1.0) sh *= CoverAt(p, cover);
                half3 rgb = src.rgb * (1.0 - sh);
                half a = src.a;

                // Chỉ tính đá quý trong dải khung (cộng 1 pixel để khử răng cưa). Chỗ đá chưa phủ giữ viền gốc của ảnh
                if (gemMask > 0.0)
                {
                    half reveal, glint;
                    half3 gem = GemColor(p, dOut, dIn, t, aa, cover, reveal, glint);
                    half m = gemMask * reveal;
                    half outA = m + a * (1.0 - m);
                    rgb = (gem * m + rgb * a * (1.0 - m)) / max(outA, 1e-4);
                    a = outA;
                    // Mép phủ loé sáng, lan cả lên phần viền gốc ngay trước mép
                    rgb += lerp(_SheenColor.rgb, 1.0.xxx, 0.5) * glint * _CoverFlash * gemMask;
                }

                // Quầng sáng ngoài khung, cùng màu với khung (theo dòng màu nếu bật)
                if (_Glow > 0.0 && dOut > 0.0)
                {
                    half3 gc = lerp(_ColorMid.rgb, FlowColor(p, t), _Flow);
                    half g = exp(-dOut / _GlowWidth) * _Glow * CoverAt(p, cover);
                    half3 pmg = rgb * a + gc * g;
                    a = saturate(a + g);
                    rgb = pmg / max(a, 1e-4);
                }

                if (dIn > -_SparkleSize && dOut < _SparkleSize && _Sparkle > 0.0)
                {
                    half spk = Sparkles(p, t * _SparkleSpeed, cover) * _Sparkle;
                    half3 pm = rgb * a + lerp(1.0.xxx, _ColorLightAlt.rgb, 0.25) * spk;
                    a = saturate(a + spk);
                    rgb = pm / max(a, 1e-4);
                }

                half4 col = half4(rgb, a) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                    float2 inside = step(_ClipRect.xy, IN.positionOS) * step(IN.positionOS, _ClipRect.zw);
                    col.a *= inside.x * inside.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(col.a - 0.001);
                #endif
                return col;
            }
            ENDHLSL
        }
    }
}
