Shader "SharderUltimate/CardElemental"
{
    // Card3D + hiệu ứng nguyên tố procedural (Fire / Lightning / Water) chạy ở viền thẻ.
    // Quad lớn hơn card _Pad lần để hiệu ứng tràn được ra ngoài viền; mỗi nguyên tố tự giới hạn phần tràn.
    // _Active (0..1) chuyển mượt từ idle sang active, _Burst là cú bùng lúc vừa kích hoạt; script CardTilt3D điều khiển.
    Properties
    {
        [NoScaleOffset] _FrontTex ("Front", 2D) = "white" {}
        [NoScaleOffset] _BackTex ("Back", 2D) = "white" {}
        [NoScaleOffset] _FoilMask ("Foil Mask", 2D) = "white" {}

        [Header(Element)]
        [Enum(Fire, 0, Lightning, 1, Water, 2)] _Element ("Element", Float) = 0
        _Active ("Active (0 idle, 1 active)", Range(0, 1)) = 0
        _Burst ("Burst (0..1, script: bung len luc kich hoat)", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width (0.05 = chuan)", Range(0.02, 0.12)) = 0.05
        _FxIntensity ("Fx Intensity", Range(0, 2)) = 1.0
        _FxSpeed ("Fx Speed", Range(0, 3)) = 0.8
        [HideInInspector] _FxTimeOffset ("Fx Time Offset (script)", Float) = 0

        [Header(Card)]
        _FoilStrength ("Foil Strength", Range(0, 2)) = 0.1
        _FoilScale ("Foil Scale", Range(0.5, 8)) = 3.0
        _Glare ("Glare", Range(0, 3)) = 0.2
        _Rim ("Rim", Range(0, 2)) = 0.1
        _Parallax ("Parallax", Range(0, 0.1)) = 0.025
        _Corner ("Corner Radius", Range(0, 0.2)) = 0.06
        _Aspect ("Aspect (w/h)", Float) = 0.6667
        _Pad ("Quad Pad (quad / card, >= 1)", Range(1, 2)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            // Premultiplied: trong card alpha 1 (đặc), ngoài card alpha = độ che của hiệu ứng
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FrontTex); SAMPLER(sampler_FrontTex);
            TEXTURE2D(_BackTex);  SAMPLER(sampler_BackTex);
            TEXTURE2D(_FoilMask); SAMPLER(sampler_FoilMask);

            CBUFFER_START(UnityPerMaterial)
                float _FxTimeOffset; // cộng dồn theo thời gian nên cần float
                half _Element, _Active, _Burst, _EdgeWidth, _FxIntensity, _FxSpeed;
                half _FoilStrength, _FoilScale, _Glare, _Rim, _Parallax, _Corner, _Aspect, _Pad;
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
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 tangentWS  : TEXCOORD3;
                float3 bitangentWS: TEXCOORD4;
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

            half3 Rainbow(half h)
            {
                return 0.5 + 0.5 * cos(6.28318 * (h + half3(0.0, 0.33, 0.67)));
            }

            // SDF hộp bo góc của card theo p (tâm card, đơn vị = chiều cao card). < 0 bên trong
            float CardSdfP(float2 p)
            {
                float2 q = abs(p) - float2(_Aspect, 1.0) * 0.5 + _Corner;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _Corner;
            }

            float CardSdf(float2 cuv)
            {
                return CardSdfP((cuv - 0.5) * float2(_Aspect, 1.0));
            }

            // ---------------------------------------------------------------- noise
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float VNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0, a = 0.5;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    v += a * VNoise(p);
                    p = p * 2.03 + float2(17.1, 9.2);
                    a *= 0.5;
                }
                return v;
            }

            // Lưới ô vuông; mỗi ô có thể chứa 1 hạt tròn (dot) hoặc bong bóng (ring), trôi lên theo t
            // coverage: tỉ lệ ô có hạt. Trả về (mask, twinkle)
            float2 Particles(float2 uv, float cells, float riseSpeed, float t, float coverage, float2 sizeRange, float ring)
            {
                float2 g = float2(uv.x * _Aspect, uv.y) * cells;
                g.y -= t * riseSpeed;
                float2 id = floor(g);
                float r = Hash21(id);
                if (r < 1.0 - coverage) return 0;
                float2 center = float2(0.5 + (Hash21(id + 7.1) - 0.5) * 0.3 + sin(t * 2.0 + r * 40.0) * 0.08, 0.5);
                float size = lerp(sizeRange.x, sizeRange.y, Hash21(id + 3.3));
                float dd = length(frac(g) - center);
                float m = ring > 0.5 ? (smoothstep(size, size - 0.03, dd) - smoothstep(size - 0.04, size - 0.1, dd))
                                     : smoothstep(size, 0.0, dd);
                return float2(m, 0.5 + 0.5 * sin(t * 6.0 + r * 60.0));
            }

            // Mọi hàm Fx trả về half4 premultiplied: rgb = màu đã nhân độ che + phần phát sáng, a = độ che ảnh/nền

            // ---------------------------------------------------------------- fire
            // Độ cao mặt trên card tại x (x đã kẹp trong bề rộng card), tính cả góc bo
            float CardTopY(float x)
            {
                float A = _Aspect * 0.5, r = _Corner;
                float ax = abs(x) - (A - r);
                return ax <= 0.0 ? 0.5 : 0.5 - r + sqrt(max(r * r - ax * ax, 0.0));
            }

            // Lửa bốc lên từ viền: "độ gần nguồn lửa" (1 ở viền, giảm dần khi lên cao / ra xa)
            // cộng noise kéo dài theo chiều dọc cuộn lên -> cắt thành từng lưỡi lửa tách rời.
            // Màu theo nhiệt độ (đỏ -> cam -> vàng -> trắng) tính trong không gian sRGB rồi đổi sang linear.
            half4 FireFx(float2 p, float t, float a, float burst, float ws)
            {
                // Nhiễu loạn bốc lên làm cong các lưỡi lửa
                float2 tw = float2(Fbm(p * float2(6.0, 3.0) - float2(0.0, t * 2.0)),
                                   Fbm(p * float2(6.0, 3.0) + float2(3.7, -t * 2.3))) - 0.5;
                float2 q = p + tw * float2(0.04, 0.02);

                float A = _Aspect * 0.5;
                float H = (lerp(0.12, 0.17, a) + burst * 0.12) * ws;    // chiều cao lửa trên cạnh trên
                float W = (lerp(0.03, 0.04, a) + burst * 0.03) * ws;    // bề dày lửa bám cạnh bên
                float wb = 0.01 * ws;                                    // cạnh dưới gần như không có lửa
                float hIn = (lerp(0.012, 0.02, a) + burst * 0.08) * ws; // độ liếm vào trong ảnh

                float xc = clamp(q.x, -A, A);
                float yTop = CardTopY(xc);
                float lateral = abs(q.x - xc);
                float hAbove = max(q.y - yTop, 0.0);
                float below = max(-yTop - q.y, 0.0);
                float sdq = CardSdfP(q);

                // Độ gần nguồn lửa: 1 ở viền, giảm tuyến tính (có thể âm) khi ra xa
                float base = min(min(1.0 - hAbove / H, 1.0 - lateral / W), 1.0 - below / wb);
                if (sdq < 0.0) base = min(base, 1.0 + sdq / hIn);
                // Cạnh bên phía dưới cháy yếu hơn
                base -= (1.0 - saturate(q.y + 0.5)) * 0.35 * step(0.0, sdq);

                // Noise kéo dài theo chiều dọc, cuộn lên nhanh -> các lưỡi lửa tách rời
                float n = Fbm(float2(q.x * 12.0, q.y * 5.0) - float2(0.0, t * 3.0));
                half heat = saturate(base * 1.1 + (n - 0.5) * 1.4);
                heat *= heat;

                half3 col = half3(1.5 * heat, 1.5 * heat * heat * heat, pow(heat, 6.0));
                half alpha = saturate(heat * 1.5) * 0.6;
                // Project dùng Linear: đổi màu sang linear, kẹp 1 để bloom không chói
                return half4(pow(saturate(col), 2.2) * 1.1, alpha);
            }

            // ---------------------------------------------------------------- lightning
            // Đường mảnh khử răng cưa: độ dày tính theo pixel nên không bị vỡ hay nhoè khi xa/gần
            half ThinLine(float field, half pixels)
            {
                float d = abs(field) / max(fwidth(field), 1e-5);
                return saturate(pixels - d * 0.7);
            }

            half4 LightningFx(float2 p, float sd, float t, float a, float burst, float ws)
            {
                half3 blue = half3(0.35, 0.6, 1.0);
                half3 white = half3(0.85, 0.93, 1.0);

                // Thời gian bước nhảy (giật như điện); lấy modulo để hash không mất chính xác theo thời gian
                float tf = fmod(floor(t * lerp(10.0, 16.0, a)), 997.0);
                half flick = 0.75 + 0.25 * Hash21(float2(tf, 1.7));
                float w = (lerp(0.022, 0.035, a) + burst * 0.03) * ws; // biên độ tia quanh viền

                // Hai tia lởm chởm chạy dọc viền, vắt qua lại mép card (SDF có dấu nên trong/ngoài không đối xứng)
                float f1 = sd + w * 0.25 - (Fbm(p * 16.0 + float2(tf * 0.37, tf * 0.71)) - 0.5) * w * 1.8;
                float f2 = sd + w * 0.6 - (Fbm(p * 12.0 + float2(20.0 - tf * 0.53, tf * 0.29)) - 0.5) * w * 2.2;
                half on1 = step(0.12, Hash21(float2(tf, 1.3)));
                half on2 = saturate(step(lerp(0.85, 0.35, a), Hash21(float2(tf, 2.9))) + step(0.1, burst));
                half core = ThinLine(f1, 1.6) * on1 + ThinLine(f2, 1.3) * on2 * 0.8;
                // Quầng quanh tia: tắt hẳn sau ~0.04 chiều cao card
                half glow = exp(-abs(f1) / 0.008) * on1 + exp(-abs(f2) / 0.008) * on2 * 0.7;

                // Active / bùng: mạng tia phân nhánh, chỉ bên trong card
                half vz = sd < 0.0 ? pow(saturate(1.0 + sd / (w * 3.0)), 2.0) : 0.0;
                float nv = Fbm(p * 10.0 + float2(tf * 0.23, 5.0)) - 0.5;
                half vein = ThinLine(nv, 1.2) * vz * saturate(a + burst);

                // Viền sáng mảnh bám sát mép
                half edge = exp(-abs(sd) / 0.006) * 0.25;

                half3 rgb = white * core * 1.4 + blue * (glow * 0.45 + edge) + lerp(blue, white, 0.5) * vein;
                rgb *= flick * (1.0 + burst * 0.5);
                return half4(rgb, saturate(core * 0.5));
            }

            // ---------------------------------------------------------------- water
            float Caustic(float2 uv, float t)
            {
                float2 p = uv * 6.28318 * 1.4 - 250.0;
                float2 i = p;
                float c = 1.0;
                const float inten = 0.0075;
                [unroll] for (int n = 0; n < 4; n++)
                {
                    float tt = t * (1.0 - 3.5 / float(n + 1));
                    i = p + float2(cos(tt - i.x) + sin(tt + i.y), sin(tt - i.y) + cos(tt + i.x));
                    c += 1.0 / length(float2(p.x / (sin(i.x + tt) / inten), p.y / (cos(i.y + tt) / inten)));
                }
                c /= 4.0;
                c = 1.17 - pow(c, 1.4);
                return saturate(pow(abs(c), 8.0));
            }

            half4 WaterFx(float2 cuv, float2 p, float sd, float px, float t, float a, float burst, float ws)
            {
                float w = (lerp(0.04, 0.06, a) + burst * 0.04) * ws;
                half inside = saturate(0.5 - sd / px);
                float depth = max(-sd, 0.0);
                half b = saturate(1.0 - depth / w);
                b *= b * inside;

                // Lớp nước xanh mỏng dọc viền, che nhẹ ảnh
                half tintA = b * 0.3;
                half3 rgb = half3(0.05, 0.35, 0.7) * tintA;

                // Gợn sóng chạy từ mép vào trong
                float warp = Fbm(p * 6.0 + t * 0.4) * 5.0;
                half ripple = pow(0.5 + 0.5 * sin(depth / w * 8.0 - t * 2.5 + warp), 6.0) * b;
                rgb += half3(0.7, 0.95, 1.0) * 0.8 * ripple * lerp(0.4, 1.0, a);

                // Vệt sáng gợn nước (caustic) chỉ trong dải viền
                half cz = saturate(1.0 - depth / (w * 1.5)) * inside;
                rgb += half3(0.45, 0.85, 1.0) * 0.8 * Caustic(cuv, t * 0.5 + 23.0) * cz * cz * lerp(0.5, 1.0, a);

                // Mép nước: một đường sáng mảnh, ra ngoài card chỉ ~0.015 chiều cao
                half lip = exp(-abs(sd) / 0.005);
                rgb += half3(0.6, 0.9, 1.0) * 0.5 * lip;

                // Bong bóng nổi lên trong dải viền
                float2 bb = Particles(cuv, 10.0, 0.8 + a * 0.6, t, lerp(0.08, 0.25, a), float2(0.1, 0.18), 1.0);
                half bz = saturate(1.0 - depth / (w * 1.8)) * inside;
                rgb += half3(0.8, 0.95, 1.0) * 0.8 * bb.x * bz;
                return half4(rgb, tintA);
            }

            half4 frag(Varyings IN, bool isFront : SV_IsFrontFace) : SV_Target
            {
                // Remap UV để card nằm gọn trong [0,1], phần dư của quad là chỗ cho hiệu ứng tràn ra ngoài
                float2 cuv = (IN.uv - 0.5) * _Pad + 0.5;
                float2 p = (cuv - 0.5) * float2(_Aspect, 1.0);
                float sd = CardSdf(cuv);
                float px = max(fwidth(sd), 1e-5);
                // Gradient UV tính trước khi rẽ nhánh để lấy mẫu texture không bị sai mip ở mép card
                float2 dx = ddx(cuv), dy = ddy(cuv);

                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 N = normalize(IN.normalWS);
                if (!isFront) N = -N;

                float a = saturate(_Active);
                float burst = saturate(_Burst);
                float t = (_Time.y + _FxTimeOffset) * _FxSpeed;
                float ws = _EdgeWidth * 20.0;
                // Lửa đã tự chỉnh độ sáng theo active/bùng; điện và nước thì nhân thêm ở đây
                half k = _Element < 0.5 ? _FxIntensity : _FxIntensity * lerp(0.75, 1.0, a) * (1.0 + burst * 0.15);

                half4 fx;
                if (_Element < 0.5)      fx = FireFx(p, t, a, burst, ws);
                else if (_Element < 1.5) fx = LightningFx(p, sd, t, a, burst, ws);
                else                     fx = WaterFx(cuv, p, sd, px, t, a, burst, ws);
                fx.rgb *= k;

                // Phía ngoài card: chỉ hiệu ứng, mờ dần về mép quad để không lộ đường cắt chữ nhật
                float2 qe = min(IN.uv, 1.0 - IN.uv);
                half4 fxOut = fx * smoothstep(0.0, 0.06, min(qe.x, qe.y));
                if (sd > px)
                {
                    clip(max(fxOut.a, max(fxOut.r, max(fxOut.g, fxOut.b))) - 0.003);
                    return fxOut;
                }

                float2 uv = cuv;
                half3 baseCol;
                half mask;
                if (isFront)
                {
                    // Parallax: dịch UV theo hướng nhìn trong tangent space, mạnh hơn ở vùng sáng
                    float3 vTS = float3(dot(V, normalize(IN.tangentWS)), dot(V, normalize(IN.bitangentWS)), dot(V, N));
                    float2 off = vTS.xy / max(vTS.z, 0.3) * _Parallax;
                    half h = dot(SAMPLE_TEXTURE2D_GRAD(_FrontTex, sampler_FrontTex, uv, dx, dy).rgb, half3(0.3, 0.59, 0.11));
                    uv -= off * h;
                    baseCol = SAMPLE_TEXTURE2D_GRAD(_FrontTex, sampler_FrontTex, uv, dx, dy).rgb;
                    mask = SAMPLE_TEXTURE2D_GRAD(_FoilMask, sampler_FoilMask, uv, dx, dy).r;
                }
                else
                {
                    baseCol = SAMPLE_TEXTURE2D_GRAD(_BackTex, sampler_BackTex, float2(1.0 - uv.x, uv.y), dx, dy).rgb;
                    mask = 0.35;
                }

                // Foil cầu vồng nhẹ: chỉ tô thêm màu, không làm trắng ảnh
                half ndv = saturate(dot(N, V));
                half hue = frac(ndv * _FoilScale + dot(uv, float2(0.6, 0.4)) * 1.5 + dot(V.xy, float2(1.5, 1.0)));
                float sweep = dot(uv, float2(0.7, 0.7)) + dot(V.xy, float2(2.0, 1.5));
                half band = pow(0.5 + 0.5 * cos(sweep * 7.5), 3.0);
                half lum = dot(baseCol, half3(0.3, 0.59, 0.11));
                half fk = mask * _FoilStrength * (0.2 + 0.8 * band);
                half3 col = baseCol + Rainbow(hue) * fk * (0.2 + 0.7 * lum);

                // Glare: vệt phản chiếu nhỏ và nhẹ, tránh làm cháy ảnh khi nghiêng
                float3 L = normalize(float3(-0.4, 0.6, -0.7));
                float3 R = reflect(-V, N);
                half spec = pow(saturate(dot(R, L)), 40.0) * _Glare;
                col += spec * (0.3 + 0.7 * mask);

                // Viền sáng (fresnel)
                half rim = pow(1.0 - ndv, 3.0) * _Rim;
                col += rim * half3(0.6, 0.8, 1.0);

                // Hiệu ứng nguyên tố phủ lên ảnh (premultiplied)
                col = col * (1.0 - fx.a) + fx.rgb;

                // Khử răng cưa mép card: trộn giữa card đặc và phần hiệu ứng phía ngoài
                half cov = saturate(0.5 - sd / px);
                return half4(col, 1.0) * cov + fxOut * (1.0 - cov);
            }
            ENDHLSL
        }
    }
}
