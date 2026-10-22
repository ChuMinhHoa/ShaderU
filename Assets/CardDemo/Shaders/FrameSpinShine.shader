Shader "SharderUltimate/FrameSpinShine"
{
    // Tráng gương xoay tròn cho khung có ô cửa trong suốt: dải sáng hình chữ nhật đi qua tâm khung và quay quanh tâm
    // như một lớp phủ, nên luôn sáng đối xứng ở 2 phía khung. Hình dải lấy từ ảnh light (_LightTex: ngang ảnh = ngang dải,
    // dọc ảnh = dọc dải; màu từ RGB, độ sáng từ alpha), kéo theo _BandWidth / _LightLength.
    // Light vừa phủ màu vừa làm sáng kiểu Screen nên không cháy trắng, vân mặt cắt của khung vẫn còn.
    // Chỉ làm sáng phần đặc của khung (alpha cao): ô cửa trong suốt và bóng đổ mờ giữ nguyên, nên hình đặt bên dưới khung
    // không bị ảnh hưởng. _PanelShine chọn có tráng phần viền dày ở dưới (vùng nằm trong mép trong của vành khung) hay không.
    // Glow: cùng ảnh light kéo rộng theo _GlowWidth, cộng thêm lên chỗ dải đi qua (_Glow), chỉ trong phần đặc của khung.
    // Dùng cho UI Image lẫn SpriteRenderer. Sprite không được đóng atlas (shader cần UV 0..1 của cả ảnh).
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Spin)]
        _RefSize ("Reference Size (w, h px of source image)", Vector) = (390, 510, 0, 0)
        _Center ("Spin Center (uv xy), Frame Half Size (uv zw)", Vector) = (0.5, 0.48, 0.485, 0.452)
        _Speed ("Speed (rev/s, negative = counter clockwise)", Range(-2, 2)) = 0.15
        _StartAngle ("Start Angle (deg, 90 = vertical band)", Range(-180, 180)) = 90
        [IntRange] _Count ("Band Count (spread over half a turn)", Range(1, 4)) = 1

        [Header(Light)]
        [NoScaleOffset] _LightTex ("Light Texture (band across x, along y)", 2D) = "black" {}
        _LightTint ("Light Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 3)) = 1
        _Tint ("Color Cover (0 brighten only, 1 full light color)", Range(0, 1)) = 0.6
        _BandWidth ("Light Width (px, bright part of the light image)", Range(4, 300)) = 70
        _LightLength ("Light Length (px along the band)", Range(50, 2000)) = 680
        _RingFocus ("Ring Focus (dim parts near center)", Range(0, 1)) = 0.2
        _TexResponse ("Follow Facets (texture brightness)", Range(0, 1)) = 0.25
        _AlphaCut ("Alpha Mask Start (skip soft shadow)", Range(0, 1)) = 0.45

        [Header(Glow where the band passes. Same light image stretched wider)]
        _Glow ("Glow On Frame", Range(0, 3)) = 0.6
        _GlowWidth ("Glow Width (px)", Range(10, 400)) = 110

        [Header(Bottom panel. Inner edge of the frame ring px of source image y from bottom)]
        _PanelShine ("Bottom Panel Shine (1 on, 0 off)", Range(0, 1)) = 1
        _InnerRect ("Ring Inner Rect (xMin, yMin, xMax, yMax)", Vector) = (30, 34, 359, 451)
        _InnerRadius ("Ring Inner Corner Radius (px)", Float) = 88
        _PanelSoft ("Panel Edge Softness (px)", Range(0.5, 8)) = 1.5

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
            TEXTURE2D(_LightTex); SAMPLER(sampler_LightTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float4 _Center;
                float _Speed, _StartAngle, _Count;
                half4 _LightTint;
                half _Intensity, _Tint;
                float _BandWidth, _LightLength;
                half _RingFocus, _TexResponse, _AlphaCut;
                half _Glow;
                float _GlowWidth;
                half _PanelShine;
                float4 _RefSize, _InnerRect;
                float _InnerRadius, _PanelSoft;
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

            // SDF hình chữ nhật bo góc, rect = (xMin, yMin, xMax, yMax). < 0 bên trong
            float RoundRectSdf(float2 p, float4 rect, float r)
            {
                float2 c = (rect.xy + rect.zw) * 0.5;
                float2 h = (rect.zw - rect.xy) * 0.5;
                float2 q = abs(p - c) - h + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            // Cộng dồn ảnh light của _Count dải chia đều nửa vòng. Toạ độ trong dải (px ảnh gốc): d ngang dải, s dọc dải.
            // Ảnh light trải ngang 2 lần _BandWidth (phần sáng của ảnh chiếm khoảng nửa giữa, nên bề rộng sáng ~ _BandWidth),
            // glow lấy cùng ảnh trải ngang 2 lần _GlowWidth. Trả về RGB đã nhân alpha (xyz) và tổng alpha (w).
            // Mỗi dải là đường thẳng qua tâm khung nên xoay 180 độ là trùng lại, vì vậy chỉ cần phần lẻ của số vòng
            // (giữ chính xác khi _Time lớn). Tính theo px ảnh gốc để dải là chữ nhật thật, không bị kéo dãn theo tỉ lệ khung
            void BandLight(float2 uv, float t, out half4 light, out half4 glow)
            {
                float2 p = (uv - _Center.xy) * _RefSize.xy;
                float spin = radians(_StartAngle) - frac(t * _Speed * 2.0) * PI; // _Speed > 0 quay theo chiều kim đồng hồ
                light = 0.0;
                glow = 0.0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    if (k >= _Count) break;
                    float a = spin + k * PI / _Count;
                    float sa = sin(a), ca = cos(a);
                    float d = dot(p, float2(-sa, ca));
                    float v = dot(p, float2(ca, sa)) / _LightLength + 0.5;
                    half4 l = SAMPLE_TEXTURE2D_LOD(_LightTex, sampler_LightTex, float2(d / (2.0 * _BandWidth) + 0.5, v), 0);
                    half4 g = SAMPLE_TEXTURE2D_LOD(_LightTex, sampler_LightTex, float2(d / (2.0 * _GlowWidth) + 0.5, v), 0);
                    light += half4(l.rgb * l.a, l.a);
                    glow += half4(g.rgb * g.a, g.a);
                }
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 src = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                half4 light, glow;
                BandLight(IN.uv, _Time.y, light, glow);
                half shine = light.a;
                half3 lightCol = light.rgb / max(light.a, 1e-3) * _LightTint.rgb; // màu trung bình của light tại pixel

                // Vành ngoài sáng đủ, phần nằm gần tâm dịu bớt để vệt không loang thành mảng lớn.
                // r ~ 1 ở mép khung: chuẩn bậc 4 cho dạng gần chữ nhật mà vẫn trơn, không gãy theo đường chéo
                float2 rq = (IN.uv - _Center.xy) / max(_Center.zw, 1e-3);
                rq *= rq;
                float r = sqrt(sqrt(dot(rq, rq)));
                half area = lerp(1.0, smoothstep(0.4, 0.95, r), _RingFocus);

                // Phần viền dày ở dưới = vùng đặc nằm trong mép trong của vành khung (phía trên là ô cửa trong suốt)
                float dIn = RoundRectSdf(IN.uv * _RefSize.xy, _InnerRect, _InnerRadius);
                half panel = smoothstep(-_PanelSoft, _PanelSoft, -dIn);
                area *= lerp(1.0, _PanelShine, panel);
                shine *= area;

                // Mặt cắt sáng của ảnh bắt ánh mạnh hơn, mặt tối nhẹ hơn
                half lum = dot(src.rgb, half3(0.2126, 0.7152, 0.0722));
                #ifndef UNITY_COLORSPACE_GAMMA
                    lum = sqrt(lum); // đưa về gần sRGB để ngưỡng như nhau ở cả 2 color space
                #endif
                shine *= max(0.0, 1.0 + (lum - 0.5) * 2.0 * _TexResponse);

                // Chỉ phủ lên phần đặc của khung: ô cửa trong suốt và bóng đổ mờ quanh khung không bị làm sáng
                half solid = smoothstep(_AlphaCut, 1.0, src.a);
                shine *= solid;

                // Nén mềm thay vì cắt ở 1 để đỉnh vệt không thành mảng phẳng.
                // Phủ màu light (giữ vân mặt cắt theo độ sáng gốc) rồi làm sáng kiểu Screen cùng màu
                half amount = 1.0 - exp(-shine * _Intensity);
                half3 mirror = lightCol * (0.55 + 0.9 * lum);
                half3 rgb = lerp(src.rgb, mirror, amount * _Tint);
                rgb += (1.0 - rgb) * lightCol * amount;

                // Glow trên khung: cộng thẳng ảnh light kéo rộng nên chỗ dải đi qua rực hơn mức Screen cho phép, mép mềm theo ảnh.
                // Chỉ trong phần đặc của khung, không tràn ra ngoài hay vào ô cửa
                rgb += glow.rgb * _LightTint.rgb * area * solid * _Glow;

                half4 col = half4(rgb, src.a) * IN.color;

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
