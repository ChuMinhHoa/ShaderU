Shader "SharderUltimate/HoloCard"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Front (alpha = cắt lỗ/bo góc)", 2D) = "white" {}
        [NoScaleOffset] _BackTex ("Back", 2D) = "white" {}
        [NoScaleOffset] _HoloMask ("Holo Mask (R)", 2D) = "white" {}
        _HoloTex ("Holo Color", 2D) = "white" {}
        [NoScaleOffset] _PatternTex ("Facet Pattern", 2D) = "gray" {}

        [Header(Holo)]
        _HoloStrength ("Holo Strength", Range(0, 1.5)) = 1
        _OffsetAmount ("View Offset Amount", Range(0, 2)) = 0.6
        _Distortion ("Noise Distortion", Range(0, 1)) = 0.25
        _NoiseScale ("Noise Scale", Float) = 4
        _ScrollSpeed ("Scroll Speed", Vector) = (0.02, 0.01, 0, 0)
        _PatternOffset ("Facet View Offset", Range(0, 1)) = 0.15
        _BackHolo ("Back Holo Strength", Range(0, 1.5)) = 0.6

        [Header(Effect)]
        [Enum(Rainbow, 0, Crystal, 1, Metal, 2, Ice, 3, ThinFilm, 4, Glitter, 5, Hologram, 6)] _Effect ("Holo Effect", Float) = 0

        [Header(Crystal effect)]
        _FacetHueSpread ("Hue Spread Per Facet", Range(0, 4)) = 1.5
        _FacetHueShift ("Hue Shift By View", Range(0, 6)) = 2.5
        _FacetHoloMix ("Blend Holo Texture", Range(0, 1)) = 0.25
        _FacetSparkle ("Facet Sparkle (HDR)", Range(0, 6)) = 2.5
        _FacetSparkleSharp ("Sparkle Sharpness", Range(2, 40)) = 14
        _FacetEdge ("Facet Edge Lines (HDR)", Range(0, 4)) = 1.2
        _FacetContrast ("Facet Contrast", Range(0, 1)) = 0.7

        [Header(Metal effect)]
        _MetalDark ("Metal Shadow Color", Color) = (0.32, 0.2, 0.06, 1)
        _MetalLight ("Metal Light Color", Color) = (1, 0.84, 0.45, 1)
        _MetalTilt ("Facet Tilt Response", Range(0, 6)) = 2.5
        _MetalShine ("Metal Shine (HDR)", Range(0, 6)) = 2
        _MetalSharp ("Shine Sharpness", Range(2, 60)) = 18

        [Header(Ice effect)]
        _IceTint ("Ice Tint", Color) = (0.55, 0.85, 1, 1)
        [HDR] _IceCrackColor ("Crack Glow (HDR)", Color) = (0.5, 1.2, 2, 1)
        _IceScale ("Crack Cell Density", Range(0.5, 6)) = 2
        _IceWidth ("Crack Width", Range(0.005, 0.15)) = 0.045
        _IceCrack ("Crack Strength", Range(0, 4)) = 1.6
        _IceIdle ("Idle Crack Visibility", Range(0, 1)) = 0.12
        _IceSpeed ("Spread Speed (card units/s)", Range(0.5, 8)) = 3
        _IceFrost ("Frost Amount", Range(0, 1)) = 0.7
        _IceFrostTilt ("Frost Change By Tilt", Range(0, 4)) = 2
        _IceBlur ("Frost Blur (uv)", Range(0, 0.04)) = 0.012
        _IceHit ("Impact (uv xy, age s, <0 = none) set by CardTilt3D", Vector) = (0.5, 0.5, -1, 0)
        _IceHover ("Hover (uv xy, amount) set by CardTilt3D", Vector) = (0.5, 0.5, 0, 0)

        [Header(Thin film effect)]
        _FilmMin ("Thickness Min (nm)", Range(0, 1500)) = 180
        _FilmMax ("Thickness Max (nm)", Range(0, 1500)) = 700
        _FilmIOR ("Film IOR", Range(1, 2.5)) = 1.33
        _FilmScale ("Swirl Scale", Range(0.1, 4)) = 1
        _FilmFlow ("Swirl Flow Speed", Range(0, 1)) = 0.06
        _FilmViewShift ("Thickness Shift By Tilt (nm)", Range(0, 1000)) = 380
        _FilmStrength ("Film Color Amount", Range(0, 1)) = 0.75
        _FilmSheen ("Bright Band Sheen (HDR)", Range(0, 4)) = 0.8

        [Header(Glitter effect)]
        _GlitterDensity ("Flakes Per Card Width", Range(10, 200)) = 55
        _GlitterSize ("Flake Size", Range(0.05, 0.5)) = 0.35
        _GlitterTilt ("Tilt Response", Range(0.5, 8)) = 3.5
        _GlitterWindow ("Sparkle Window (phần số hạt loé)", Range(0.01, 0.25)) = 0.05
        _GlitterBright ("Flake Brightness (HDR)", Range(0, 12)) = 6
        _GlitterColor ("Flake Color", Color) = (1, 0.93, 0.8, 1)
        _GlitterRainbow ("Rainbow Flakes", Range(0, 1)) = 0.6
        _GlitterGrain ("Unlit Flake Grain", Range(0, 0.5)) = 0.12
        _GlitterStar ("Star Sparkles (HDR)", Range(0, 12)) = 4
        _GlitterStarDensity ("Stars Per Card Width", Range(2, 30)) = 7
        _GlitterCursor ("Cursor Light Boost", Range(0, 4)) = 1.5
        _GlitterBase ("Base Holo Amount", Range(0, 1)) = 0.35

        [Header(Hologram effect)]
        [HDR] _HoloTint ("Hologram Tint (HDR)", Color) = (0.3, 0.95, 1.3, 1)
        _HoloTintAmount ("Tint Amount", Range(0, 1)) = 0.6
        _ScanDensity ("Scanlines Per Card Height", Range(20, 400)) = 150
        _ScanStrength ("Scanline Darkness", Range(0, 1)) = 0.3
        _ScanSpeed ("Scanline Scroll Speed", Range(-2, 2)) = 0.04
        _ScanBand ("Scan Band Glow (HDR)", Range(0, 4)) = 0.4
        _ScanBandSpeed ("Scan Band Sweeps Per Second", Range(0, 2)) = 0.3
        _ScanBandWidth ("Scan Band Width", Range(0.01, 0.5)) = 0.1
        _HexGrid ("Hex Grid (HDR)", Range(0, 2)) = 0.35
        _HexScale ("Hex Cells Per Card Width", Range(4, 60)) = 18
        _Flicker ("Flicker", Range(0, 1)) = 0.12
        _Glitch ("Glitch Amount", Range(0, 1)) = 0.6
        _GlitchRate ("Glitch Bursts Per Second", Range(0, 4)) = 0.5
        _RgbSplit ("RGB Split (uv)", Range(0, 0.03)) = 0.002
        _QuadRect ("Quad Rect On Card (uv x, y, w, h), poster đặt riêng", Vector) = (0, 0, 1, 1)

        [Header(Light)]
        _Glare ("Glare", Range(0, 3)) = 0.8
        _EdgeGlow ("Edge Glow (HDR)", Range(0, 6)) = 2.5
        _EdgeWidth ("Edge Width (uv)", Range(0.001, 0.08)) = 0.025
        _Corner ("Corner Radius (uv)", Range(0, 0.2)) = 0.06
        _Aspect ("Aspect (w/h)", Float) = 0.714

        [Header(Window set by HoloCardView)]
        _CardSize ("Card Size (w, h)", Vector) = (2.5, 3.5, 0, 0)
        _WindowCenter ("Window Center UV (xy)", Vector) = (0.5, 0.5, 0, 0)
        _WindowShape ("Window (halfX, halfY, corner, type), halfX = 0 tắt", Vector) = (0, 0, 0, 0)
        _WindowPoly ("Polygon (sides, rotation rad)", Vector) = (6, 0, 0, 0)
        _RingWidth ("Ring Width", Range(0, 0.2)) = 0.056
        _RingColor ("Ring Color", Color) = (1, 0.85, 0.5, 1)

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WindowShape.hlsl"

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
            TEXTURE2D(_BackTex);    SAMPLER(sampler_BackTex);
            TEXTURE2D(_HoloMask);   SAMPLER(sampler_HoloMask);
            TEXTURE2D(_HoloTex);    SAMPLER(sampler_HoloTex);
            TEXTURE2D(_PatternTex); SAMPLER(sampler_PatternTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _HoloTex_ST;
                float4 _ScrollSpeed;
                half _HoloStrength, _OffsetAmount, _Distortion, _NoiseScale, _PatternOffset, _BackHolo;
                half _Effect, _FacetHueSpread, _FacetHueShift, _FacetHoloMix, _FacetSparkle, _FacetSparkleSharp, _FacetEdge, _FacetContrast;
                half _IceScale, _IceWidth, _IceCrack, _IceIdle, _IceSpeed, _IceFrost, _IceFrostTilt, _IceBlur;
                half4 _IceTint, _IceCrackColor;
                float4 _IceHit, _IceHover;
                half _MetalTilt, _MetalShine, _MetalSharp;
                half4 _MetalDark, _MetalLight;
                half _FilmMin, _FilmMax, _FilmIOR, _FilmScale, _FilmFlow, _FilmViewShift, _FilmStrength, _FilmSheen;
                half _GlitterDensity, _GlitterSize, _GlitterTilt, _GlitterWindow, _GlitterBright, _GlitterRainbow, _GlitterGrain;
                half _GlitterStar, _GlitterStarDensity, _GlitterCursor, _GlitterBase;
                half4 _GlitterColor;
                half4 _HoloTint;
                half _HoloTintAmount, _ScanDensity, _ScanStrength, _ScanSpeed, _ScanBand, _ScanBandSpeed, _ScanBandWidth;
                half _HexGrid, _HexScale, _Flicker, _Glitch, _GlitchRate, _RgbSplit;
                float4 _QuadRect;
                half _Glare, _EdgeGlow, _EdgeWidth, _Corner, _Aspect;
                float4 _CardSize, _WindowCenter, _WindowShape, _WindowPoly;
                half _RingWidth;
                half4 _RingColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS = n.normalWS;
                OUT.tangentWS = n.tangentWS;
                OUT.bitangentWS = n.bitangentWS;
                OUT.uv = IN.uv;
                return OUT;
            }

            // Value noise thay cho node Gradient Noise trong Shader Graph
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }
            float Fbm(float2 p) { return ValueNoise(p) * 0.65 + ValueNoise(p * 2.03 + 17.0) * 0.35; }

            // Voronoi: x = khoảng cách tới tâm gần nhất, y = tâm gần thứ hai. y - x ~ 0 trên đường biên ô = vết nứt
            float2 Voronoi(float2 p)
            {
                float2 g = floor(p), f = frac(p);
                float f1 = 8.0, f2 = 8.0;
                for (int j = -1; j <= 1; j++)
                    for (int i = -1; i <= 1; i++)
                    {
                        float2 o = float2(i, j);
                        float2 r = o + float2(Hash(g + o), Hash(g + o + 37.3)) - f;
                        float d = dot(r, r);
                        if (d < f1) { f2 = f1; f1 = d; }
                        else if (d < f2) f2 = d;
                    }
                return float2(sqrt(f1), sqrt(f2));
            }

            half3 BlendOverlay(half3 a, half3 b)
            {
                return lerp(2.0 * a * b, 1.0 - 2.0 * (1.0 - a) * (1.0 - b), step(0.5, a));
            }

            // Giao thoa màng mỏng (bong bóng xà phòng / váng dầu): độ phản xạ theo hiệu quang trình opd (nm).
            // Mỗi kênh lấy trung bình 2 bước sóng nên ở bậc giao thoa cao màu nhạt dần như thật
            half3 ThinFilm(float opd)
            {
                float3 a = 0.5 - 0.5 * cos(6.2831853 * opd / float3(680.0, 560.0, 480.0));
                float3 b = 0.5 - 0.5 * cos(6.2831853 * opd / float3(610.0, 510.0, 440.0));
                return (a + b) * 0.5;
            }

            // Một lớp kim tuyến: mỗi ô lưới có một hạt ở vị trí, hướng và màu ngẫu nhiên.
            // star = 1: hạt có thêm tia chữ thập. grain = độ phủ hạt (kể cả hạt chưa loé), làm vân kim tuyến nền
            half3 GlitterLayer(float2 p, float2 view, float size, float window, float seed, half star, half rainbow, half3 tint, out half grain)
            {
                float2 g = floor(p), f = frac(p);
                half3 sum = 0;
                grain = 0;
                for (int j = -1; j <= 1; j++)
                    for (int i = -1; i <= 1; i++)
                    {
                        float2 o = float2(i, j);
                        float2 cell = g + o;
                        float h1 = Hash(cell + seed), h2 = Hash(cell + seed + 19.7);
                        float h3 = Hash(cell + seed + 41.3), h4 = Hash(cell + seed + 73.1);
                        float2 d = o + 0.2 + 0.6 * float2(h1, h2) - f;    // từ điểm ảnh tới tâm hạt (đơn vị ô)
                        // Pha loé tuần hoàn theo hướng nhìn chiếu lên một hướng ngẫu nhiên của hạt: ở góc nào cũng có
                        // khoảng window phần số hạt đang loé, nghiêng thẻ thì từng hạt lần lượt tắt / bật
                        float a = h3 * 6.2831853;
                        float ph = frac(dot(view, float2(cos(a), sin(a))) + h4);
                        half lit = 1.0 - smoothstep(window * 0.5, window, abs(ph - 0.5));
                        half twinkle = 0.55 + 0.45 * sin(_Time.y * (1.5 + h1 * 4.0) + h2 * 6.2831853);
                        half3 prism = 0.5 + 0.5 * cos(6.2831853 * (h3 + h4 + view.x * 0.3 + float3(0.0, 0.33, 0.67)));
                        half3 col = lerp(tint, prism, rainbow);

                        half disk = saturate(1.0 - length(d) / size);
                        // Tia chữ thập: dài theo một trục, rất mảnh theo trục kia; nửa số sao xoay 45 độ
                        float2 q = abs(h1 > 0.5 ? float2(d.x + d.y, d.x - d.y) * 0.7071 : d);
                        half rays = exp(-q.x * 5.0 - q.y * 90.0) + exp(-q.y * 5.0 - q.x * 90.0);
                        half shape = lerp(disk * disk, disk * disk * disk + rays * 0.6, star);
                        sum += col * shape * lit * twinkle;
                        grain += disk * disk * (0.5 + 0.5 * h4);
                    }
                return sum;
            }

            // Khoảng cách tới cạnh ô lục giác gần nhất (đơn vị ô), 0 = nằm trên cạnh
            float HexEdge(float2 p)
            {
                const float2 s = float2(1.0, 1.7320508);
                float4 c = floor(float4(p, p - float2(0.5, 1.0)) / s.xyxy) + 0.5;
                float4 h = float4(p - c.xy * s, p - (c.zw + 0.5) * s);
                float2 q = abs(dot(h.xy, h.xy) < dot(h.zw, h.zw) ? h.xy : h.zw);
                return 0.5 - max(dot(q, s * 0.5), q.x);
            }

            // Hologram: lấy mẫu ảnh với 3 kênh lệch ngang nhau (tách RGB)
            #define SAMPLE_SPLIT(tex, samp, p, s) half3(SAMPLE_TEXTURE2D(tex, samp, (p) + float2(s, 0)).r, \
                SAMPLE_TEXTURE2D(tex, samp, p).g, SAMPLE_TEXTURE2D(tex, samp, (p) - float2(s, 0)).b)

            half4 frag(Varyings IN, bool isFront : SV_IsFrontFace) : SV_Target
            {
                float2 uv = IN.uv;
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 N = normalize(IN.normalWS);
                float3 T = normalize(IN.tangentWS);
                float3 B = normalize(IN.bitangentWS);
                if (!isFront) { N = -N; T = -T; }

                // Hướng nhìn trong tangent space = "View Direction" node, dùng để trượt texture
                float2 viewTS = float2(dot(V, T), dot(V, B));

                // Hologram: tính glitch trước khi lấy mẫu ảnh. Dải glitch / scanline tính theo toạ độ trên cả thẻ
                // (_QuadRect) để poster nổi phía trước khớp dải với mặt thẻ
                float2 cardRectUV = _QuadRect.xy + IN.uv * _QuadRect.zw;
                float2 glitchShift = 0;
                float rgbSplit = 0;
                half glitch = 0, glitchBar = 0;
                if (_Effect > 5.5)
                {
                    float slot = fmod(floor(_Time.y * 10.0), 4096.0);   // 10 lát thời gian mỗi giây, mỗi lát có thể glitch
                    half burst = step(1.0 - _GlitchRate * 0.1, Hash(float2(slot, 3.7)));
                    // Bấm lên thẻ: glitch mạnh lúc đầu (CardTilt3D đặt tuổi cú chạm vào _IceHit.z)
                    burst = max(burst, _IceHit.z >= 0.0 ? 1.0 - smoothstep(0.15, 0.45, _IceHit.z) : 0.0);
                    glitch = burst * _Glitch;
                    float row = floor(cardRectUV.y * 28.0 + Hash(float2(slot, 9.1)) * 28.0);
                    float rowRnd = Hash(float2(row, slot + 0.5));
                    glitchShift.x = (rowRnd - 0.5) * 0.12 * step(0.55, Hash(float2(row + 3.3, slot))) * glitch / _QuadRect.z;
                    glitchBar = step(0.85, rowRnd) * glitch * ValueNoise(float2(cardRectUV.x * 90.0, row + slot));
                    rgbSplit = (_RgbSplit + glitch * 0.012) / _QuadRect.z;
                }

                half4 baseTex;
                half mask;
                half holoAmount;
                if (isFront)
                {
                    baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                    if (_Effect > 5.5) baseTex.rgb = SAMPLE_SPLIT(_MainTex, sampler_MainTex, uv + glitchShift, rgbSplit);
                    mask = SAMPLE_TEXTURE2D(_HoloMask, sampler_HoloMask, uv).r;
                    holoAmount = _HoloStrength;

                    // Cửa sổ: cắt lỗ và vẽ viền vàng có gờ nổi quanh lỗ
                    if (_WindowShape.x > 0.0)
                    {
                        float dist = WindowDistance((uv - _WindowCenter.xy) * _CardSize.xy, _WindowShape, _WindowPoly);
                        baseTex.a = min(baseTex.a, step(0.0, dist));     // lỗ: để clip chung bên dưới cắt
                        float d = max(dist, 0.0);
                        float t = saturate(d / max(_RingWidth, 1e-4));
                        half ring = 1.0 - smoothstep(0.85, 1.0, t);
                        half shade = 0.65 + 0.45 * sin(t * PI);           // gờ nổi: sáng ở giữa, tối 2 mép
                        float g = (t - 0.55) / 0.08;                       // không dùng pow: cơ số âm cho NaN
                        shade *= lerp(1.0, 0.45, exp(-g * g));            // rãnh tối mảnh
                        baseTex.rgb = lerp(baseTex.rgb, _RingColor.rgb * shade, ring);
                        mask *= 1.0 - ring * 0.75;
                    }
                }
                else
                {
                    uv.x = 1.0 - uv.x;
                    baseTex = SAMPLE_TEXTURE2D(_BackTex, sampler_BackTex, uv);
                    if (_Effect > 5.5) baseTex.rgb = SAMPLE_SPLIT(_BackTex, sampler_BackTex, uv + glitchShift, rgbSplit);
                    mask = 1.0;
                    holoAmount = _BackHolo;
                }
                clip(baseTex.a - 0.5);

                // Holo: texture màu, UV bị bẻ bằng noise, trượt theo hướng nhìn + thời gian
                float n = Fbm(uv * _NoiseScale);
                float2 holoUV = uv * _HoloTex_ST.xy + _HoloTex_ST.zw
                              + (n - 0.5) * _Distortion
                              + viewTS * _OffsetAmount
                              + _Time.y * _ScrollSpeed.xy;
                half3 holo = SAMPLE_TEXTURE2D(_HoloTex, sampler_HoloTex, holoUV).rgb;

                // Lớp facet low-poly cũng trượt nhẹ theo góc nhìn, làm holo có cảm giác pha lê
                float2 facetUV = uv + viewTS * _PatternOffset;
                half facet = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, facetUV).r;
                half3 crystalAdd = 0;
                half holoMix = 1.0;     // mức trộn lớp holo màu vào ảnh (hiệu ứng nào cần nhẹ hơn thì giảm)
                half shade = 1.0;       // nhân sáng/tối sau khi trộn (scanline, nhấp nháy)
                half holoVis = saturate(mask * holoAmount);
                if (_Effect > 5.5)
                {
                    // Hologram: ảnh chiếu sci-fi. Ám màu, scanline cuộn, dải quét sáng chạy từ trên xuống,
                    // lưới lục giác hiện rõ khi dải quét đi qua, nhấp nháy và glitch xé dòng (tính ở trên)
                    float y = cardRectUV.y;
                    float lines = y * _ScanDensity + _Time.y * _ScanSpeed * _ScanDensity;
                    half scan = (0.5 + 0.5 * sin(lines * 6.2831853)) * saturate(2.0 - fwidth(lines) * 2.5);   // dòng < ~1 px thì mờ đi, tránh moire
                    float bandPos = 1.15 - frac(_Time.y * _ScanBandSpeed) * 1.3;    // ra ngoài mép thẻ một đoạn = khoảng nghỉ giữa 2 lượt quét
                    float bd = (y - bandPos) / _ScanBandWidth;
                    half band = exp(-bd * bd) + 0.3 * exp(-bd * 0.8) * step(0.0, bd);   // vệt sáng mờ dần phía sau dải

                    float2 hp = cardRectUV * _CardSize.xy * (_HexScale / _CardSize.x);
                    float he = HexEdge(hp);
                    half hexLine = 1.0 - smoothstep(0.0, 0.05 + fwidth(he) * 1.5, he);
                    half hexVis = (0.25 + band) * (0.4 + 0.6 * ValueNoise(hp * 0.35 + _Time.y * 0.4));

                    // Ám màu theo độ sáng ảnh, tint chuẩn hoá về kênh lớn nhất 1 để không làm ảnh sáng thêm (phần HDR để cho glow)
                    half lum = dot(baseTex.rgb, half3(0.3, 0.59, 0.11));
                    half3 tintN = _HoloTint.rgb / max(max(_HoloTint.r, _HoloTint.g), max(_HoloTint.b, 1e-3));
                    baseTex.rgb = lerp(baseTex.rgb, lum * tintN * 1.1, _HoloTintAmount * holoVis);
                    holo = _HoloTint.rgb * lerp(0.6, 1.1, facet);
                    holoMix = 0.2;      // nhẹ để chữ trên poster không bị bạc màu
                    shade = (1.0 - scan * _ScanStrength) * (1.0 - _Flicker * ValueNoise(float2(_Time.y * 15.0, 1.7)) - glitch * 0.2);
                    crystalAdd = _HoloTint.rgb * (band * _ScanBand * 0.6 + hexLine * hexVis * _HexGrid + glitchBar * 1.5);
                }
                else if (_Effect > 4.5)
                {
                    // Glitter: kim tuyến nhiều màu, mỗi hạt loé ở một góc nghiêng riêng + vài ngôi sao chữ thập.
                    // Rê chuột như rọi đèn: quanh con trỏ nhiều hạt loé hơn và sáng hơn
                    float2 cardUV = uv * _CardSize.xy;
                    half hov = (1.0 - smoothstep(0.0, 0.9, length((uv - _IceHover.xy) * _CardSize.xy))) * _IceHover.z;
                    float2 view = viewTS * _GlitterTilt;
                    float window = _GlitterWindow * (1.0 + hov * _GlitterCursor * 0.5);
                    half grain, starGrain;
                    half3 flakes = GlitterLayer(cardUV * (_GlitterDensity / _CardSize.x), view, _GlitterSize, window,
                                                0.0, 0.0, _GlitterRainbow, _GlitterColor.rgb, grain);
                    half3 stars = GlitterLayer(cardUV * (_GlitterStarDensity / _CardSize.x), view * 1.3, 0.12, window * 0.8,
                                               7.0, 1.0, _GlitterRainbow * 0.5, _GlitterColor.rgb, starGrain);
                    holo *= lerp(0.7, 1.2, facet);
                    holoMix = _GlitterBase;
                    crystalAdd = (flakes * _GlitterBright + stars * _GlitterStar) * (1.0 + hov * _GlitterCursor)
                               + grain * _GlitterGrain * lerp(1.0.xxx, holo, 0.5);
                }
                else if (_Effect > 3.5)
                {
                    // Thin film: độ dày màng chảy xoáy chậm (domain warp). Nghiêng thẻ đổi độ dày + góc khúc xạ
                    // nên các dải màu giao thoa trượt và đổi màu như bong bóng xà phòng
                    float2 fp = uv * _CardSize.xy * _FilmScale;
                    float ft = _Time.y * _FilmFlow;
                    float2 warp = float2(Fbm(fp + float2(ft, -ft * 0.7)), Fbm(fp + float2(5.2 - ft * 0.6, 1.3 + ft)));
                    float th = Fbm(fp * 0.8 + (warp - 0.5) * 2.5 + ft * 0.3);
                    float thickness = lerp(_FilmMin, _FilmMax, smoothstep(0.15, 0.85, th)) + dot(viewTS, float2(0.6, 0.8)) * _FilmViewShift;
                    float NoV = saturate(dot(N, V));
                    float cosT = sqrt(saturate(1.0 - (1.0 - NoV * NoV) / (_FilmIOR * _FilmIOR)));   // góc khúc xạ trong màng (Snell)
                    half3 film = ThinFilm(2.0 * _FilmIOR * thickness * cosT);
                    holo = lerp(film, holo * film * 2.0, _FacetHoloMix);
                    holoMix = _FilmStrength;
                    half3 f2 = film * film;
                    crystalAdd = f2 * f2 * _FilmSheen;      // dải màu sáng nhất bắt bloom
                }
                else if (_Effect > 2.5)
                {
                    // Ice: băng mờ đục đổi theo góc nghiêng, vết nứt Voronoi phát sáng lan ra từ điểm chạm
                    float2 cardUV = uv * _CardSize.xy;
                    float2 vp = cardUV * _IceScale;
                    float2 v1 = Voronoi(vp + n * 0.35);
                    float2 v2 = Voronoi(vp * 2.3 + 11.0);
                    half crack = saturate((1.0 - smoothstep(0.0, _IceWidth, v1.y - v1.x))
                                        + 0.5 * (1.0 - smoothstep(0.0, _IceWidth * 0.8, v2.y - v2.x)));

                    // Lan ra từ điểm chạm: bán kính tăng theo tuổi cú chạm, mép lan méo bằng noise cho tự nhiên
                    float dHit = length((uv - _IceHit.xy) * _CardSize.xy) + (n - 0.5) * 0.6;
                    float radius = max(_IceHit.z, 0.0) * _IceSpeed;
                    half spread = _IceHit.z >= 0.0 ? 1.0 - smoothstep(radius - 0.5, radius, dHit) : 0.0;
                    float g = (dHit - radius) / 0.12;
                    half front = _IceHit.z >= 0.0 ? exp(-g * g) * (1.0 - smoothstep(3.0, 5.0, radius)) : 0.0;
                    // Rê chuột: vết nứt hé ra quanh con trỏ
                    float dHover = length((uv - _IceHover.xy) * _CardSize.xy);
                    half hov = (1.0 - smoothstep(0.0, 1.0, dHover)) * _IceHover.z;
                    half vis = saturate(_IceIdle + spread + hov * 0.6);

                    // Băng mờ: nghiêng theo hướng này thì đục, hướng kia thì trong. Vùng đã nứt trong hơn
                    half frostTilt = saturate(0.5 + dot(viewTS, float2(0.7, 0.7)) * _IceFrostTilt * 0.5);
                    half frost = _IceFrost * (0.35 + 0.65 * frostTilt) * (0.6 + 0.4 * n) * (1.0 - spread * 0.5);
                    if (isFront)
                    {
                        float r = _IceBlur * frost;
                        half3 blur = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(r, 0)).rgb
                                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(r, 0)).rgb
                                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, r)).rgb
                                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, r)).rgb) * 0.25;
                        half f = saturate(frost * mask);
                        baseTex.rgb = lerp(baseTex.rgb, blur, f);
                        baseTex.rgb = lerp(baseTex.rgb, _IceTint.rgb, f * 0.35);
                    }

                    half3 icy = _IceTint.rgb * (0.65 + 0.7 * facet);
                    holo = lerp(icy, holo, 0.2);
                    half shimmer = 0.8 + 0.2 * sin(_Time.y * 2.0 + facet * 20.0);
                    crystalAdd = _IceCrackColor.rgb * (crack * vis * shimmer * _IceCrack + front * (0.4 + crack) * 1.5)
                               + frost * 0.08;
                }
                else if (_Effect > 1.5)
                {
                    // Metal: mỗi mặt là một mảnh kim loại nghiêng riêng (hướng suy từ mức xám), bắt sáng khi thẻ nghiêng
                    float ang = facet * 6.2831853 * 3.0;
                    float2 facetN = float2(cos(ang), sin(ang));
                    float lit = dot(viewTS, facetN) * _MetalTilt;
                    half ramp = saturate(0.5 + lit * 0.5 + (facet - 0.5) * 0.4);
                    half3 metal = lerp(_MetalDark.rgb, _MetalLight.rgb, ramp * ramp * (3.0 - 2.0 * ramp));
                    holo = lerp(metal, holo * metal * 1.6, _FacetHoloMix);

                    half shine = pow(saturate(lit), _MetalSharp);
                    crystalAdd = lerp(_MetalLight.rgb, 1.0.xxx, 0.5) * shine * _MetalShine;
                    half edgeLine = saturate(fwidth(facet) * 14.0);
                    crystalAdd += edgeLine * _FacetEdge * _MetalLight.rgb * 0.6;
                }
                else if (_Effect > 0.5)
                {
                    // Crystal: mỗi mặt kim cương (một mức xám phẳng trong facets.png) có hue riêng, hue trượt theo góc nhìn
                    float hue = facet * _FacetHueSpread + dot(viewTS, float2(0.8, 0.6)) * _FacetHueShift + _Time.y * 0.03;
                    half3 prism = 0.5 + 0.5 * cos(6.2831853 * (hue + float3(0.0, 0.33, 0.67)));
                    holo = lerp(prism, holo, _FacetHoloMix);
                    holo *= lerp(1.0, lerp(0.35, 1.5, facet), _FacetContrast);

                    // Mặt nào đang hướng về phía người xem thì loé sáng, pha lệch theo từng mặt
                    float phase = facet * 37.0 + dot(viewTS, float2(1.0, 0.7)) * 9.0 + _Time.y * 0.6;
                    half glint = pow(saturate(sin(phase) * 0.5 + 0.5), _FacetSparkleSharp);
                    crystalAdd = prism * glint * _FacetSparkle;

                    // Đường giao giữa các mặt: chỗ mức xám nhảy đột ngột
                    half edgeLine = saturate(fwidth(facet) * 14.0);
                    crystalAdd += edgeLine * _FacetEdge * lerp(1.0.xxx, prism, 0.5);
                }
                else
                {
                    holo *= lerp(0.55, 1.35, facet);
                }

                half3 col = baseTex.rgb;
                half3 holoBlend = BlendOverlay(col, holo) * 0.6 + holo * 0.55;
                col = lerp(col, holoBlend, holoVis * holoMix);
                col *= lerp(1.0, shade, holoVis);
                col += crystalAdd * holoVis;

                // Viền thẻ phát sáng cầu vồng (HDR để bloom bắt)
                float2 p = (IN.uv - 0.5) * float2(_Aspect, 1.0);
                float2 q = abs(p) - float2(_Aspect, 1.0) * 0.5 + _Corner;
                float dist = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _Corner;
                half edge = 1.0 - smoothstep(0.0, _EdgeWidth, -dist);
                col += holo * edge * _EdgeGlow * step(0.001, holoAmount);

                // Vệt sáng phản chiếu
                float3 L = normalize(float3(-0.4, 0.6, -0.7));
                half spec = pow(saturate(dot(reflect(-V, N), L)), 20.0) * _Glare;
                col += spec;

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
