// Ghi stencil vào vùng cửa sổ portal, không vẽ màu. Chỉ mặt trước ghi (Cull Back)
// nên nhìn từ sau lưng thẻ thì không thấy được thế giới bên trong.
// Có ZTest nên vật nằm trước cửa sổ vẫn che được cửa sổ. Queue do builder đặt (sau mặt thẻ, trước nội dung).
// Cắt thêm theo viền thẻ: cửa sổ đặt to hơn thẻ thì portal cũng không tràn ra ngoài góc bo của thẻ.
Shader "SharderUltimate/PortalMask"
{
    Properties
    {
        _StencilRef ("Stencil Ref", Int) = 1
        // HoloCardView set qua MaterialPropertyBlock
        _WindowShape ("Window (halfX, halfY, corner, type)", Vector) = (0, 0, 0, 0)
        _WindowPoly ("Polygon (sides, rotation rad)", Vector) = (6, 0, 0, 0)
        _CardRect ("Card (center from window xy, corner)", Vector) = (0, 0, 0, 0)
        _CardHalf ("Card Half Size", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry-10" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            ColorMask 0
            ZWrite Off
            Cull Back
            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WindowShape.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _WindowShape, _WindowPoly, _CardRect, _CardHalf;
            CBUFFER_END

            struct Varyings { float4 positionCS : SV_POSITION; float2 local : TEXCOORD0; };

            Varyings vert(float4 positionOS : POSITION)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(positionOS.xyz);
                OUT.local = positionOS.xy;   // mesh nằm trong không gian cửa sổ, gốc tại tâm cửa sổ
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (_WindowShape.x > 0.0)
                {
                    // Lấn nhẹ 0.004 ra ngoài lỗ để không hở khe với mặt thẻ
                    float d = WindowDistance(IN.local, _WindowShape, _WindowPoly) - 0.004;
                    if (_CardHalf.x > 0.0) d = max(d, SdRoundedBox(IN.local - _CardRect.xy, _CardHalf.xy, _CardRect.z));
                    clip(-d);
                }
                return 0;
            }
            ENDHLSL
        }
    }
}
