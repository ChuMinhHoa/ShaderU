// Nhân vật trong portal, có thể tràn ra khỏi cửa sổ (pop-out).
// Pass "Inside": vẽ trong vùng stencil của cửa sổ, giống PortalContent bán trong suốt.
// Pass "PopOut": vẽ ở pixel stencil KHÁC _StencilRef (đúng phần bù của pass Inside tới từng pixel, không hở viền
//   vàng ở mép cửa sổ), chiếu từng pixel theo tia nhìn lên mặt phẳng thẻ, chỉ vẽ phần phía trên đường cắt
//   -> đầu / tay đè lên khung thẻ, chân vẫn bị khung che.
//   _PopMask (ảnh) và _PopZones (vùng elip kéo trong Scene view), đều theo UV ảnh nhân vật,
//   chọn phần nào được tràn ra -> dùng được với mọi ảnh nhân vật.
//   Hai vùng không chồng nhau nên không bị đậm gấp đôi. Pop-out tắt thì pass này bỏ hết pixel.
Shader "SharderUltimate/PortalPopOut"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
        _StencilRef ("Stencil Ref", Int) = 1
        // HoloCardView set qua MaterialPropertyBlock
        _WindowShape ("Window (halfX, halfY, corner, type)", Vector) = (0, 0, 0, 0)
        _WindowPoly ("Polygon (sides, rotation rad)", Vector) = (6, 0, 0, 0)
        _PopOut ("Pop Out (line y, feather, enabled)", Vector) = (0, 0.05, 0, 0)
        [NoScaleOffset] _PopMask ("Pop Out Mask (R: trắng = được tràn ra)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "WindowShape.hlsl"

        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        TEXTURE2D(_PopMask); SAMPLER(sampler_PopMask);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            float4 _WindowShape, _WindowPoly, _PopOut;
            float4x4 _PortalWorldToLocal;   // không gian cửa sổ: gốc tại tâm cửa sổ, mặt thẻ là z = 0
        CBUFFER_END

        // Vùng elip trên ảnh nhân vật được phép tràn ra (UV: tâm xy, bán kính zw), số vùng ở _PopOut.w.
        // Mảng không khai báo được trong Properties, HoloCardView set qua MaterialPropertyBlock
        float4 _PopZones[4];
        float4 _PopZoneSoftness;   // dải mờ của từng vùng (x, y, z, w = vùng 1..4), theo tỉ lệ bán kính

        struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
        struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
            OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            return OUT;
        }

        half4 SampleCharacter(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;
        }
        ENDHLSL

        Pass
        {
            Name "Inside"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Stencil
            {
                Ref [_StencilRef]
                Comp Equal
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragInside

            half4 fragInside(Varyings IN) : SV_Target
            {
                return SampleCharacter(IN.uv);
            }
            ENDHLSL
        }

        Pass
        {
            Name "PopOut"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            // Ranh giới với pass Inside lấy từ cùng stencil của mesh mask. Trước đây so bằng công thức elip,
            // lệch với mesh đa giác của mask tới cả pixel nên hở thành hàng chấm, viền vàng mặt thẻ lộ qua tóc
            Stencil
            {
                Ref [_StencilRef]
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragPopOut

            half4 fragPopOut(Varyings IN) : SV_Target
            {
                if (_PopOut.z < 0.5 || _WindowShape.x <= 0.0) discard;

                // Giao điểm của tia camera -> pixel với mặt phẳng thẻ (z = 0 trong không gian cửa sổ)
                float3 cam = mul(_PortalWorldToLocal, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 pos = mul(_PortalWorldToLocal, float4(IN.positionWS, 1.0)).xyz;
                float denom = cam.z - pos.z;
                if (abs(denom) < 1e-5) discard;
                float2 hit = lerp(cam, pos, cam.z / denom).xy;

                // Sâu trong cửa sổ thì chắc chắn thuộc pass Inside; sát mép để stencil quyết định
                if (WindowDistance(hit, _WindowShape, _WindowPoly) <= -0.01) discard;

                half4 col = SampleCharacter(IN.uv);
                col.a *= smoothstep(_PopOut.x, _PopOut.x + max(_PopOut.y, 1e-4), hit.y);
                // Mask vẽ theo ảnh nhân vật: chỉ phần tô trắng mới được tràn ra, phần đen chỉ hiện trong cửa sổ
                col.a *= SAMPLE_TEXTURE2D(_PopMask, sampler_PopMask, IN.uv).r;

                // Vùng elip: có vùng thì chỉ phần nằm trong ít nhất một vùng mới được tràn ra
                int zoneCount = (int)_PopOut.w;
                if (zoneCount > 0)
                {
                    half inZone = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (i >= zoneCount) break;
                        float2 d = (IN.uv - _PopZones[i].xy) / max(_PopZones[i].zw, 1e-4);
                        // Rõ hoàn toàn trong (1 - softness) bán kính, mờ dần tới 0 ở mép vùng
                        inZone = max(inZone, 1.0 - smoothstep(1.0 - _PopZoneSoftness[i], 1.0, length(d)));
                    }
                    col.a *= inZone;
                }
                return col;
            }
            ENDHLSL
        }
    }
}
