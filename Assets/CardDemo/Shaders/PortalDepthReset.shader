// Đặt lại depth về mặt phẳng xa nhất trong vùng cửa sổ portal (pixel có stencil = ref).
// Vẽ sau PortalMask, trước nội dung portal: mọi vật nằm sau cửa sổ (ví dụ thẻ khác) bị "xoá" khỏi depth,
// nên nội dung portal luôn vẽ đè lên được, cửa sổ chỉ hiện thế giới của chính nó.
Shader "SharderUltimate/PortalDepthReset"
{
    Properties
    {
        _StencilRef ("Stencil Ref", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            ColorMask 0
            ZWrite On
            ZTest Always
            Cull Back
            Stencil
            {
                Ref [_StencilRef]
                Comp Equal
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                float4 p = TransformObjectToHClip(positionOS.xyz);
                // Đẩy đỉnh ra sát mặt phẳng xa (không đúng bằng để khỏi bị clip)
            #if UNITY_REVERSED_Z
                p.z = p.w * 1e-6;
            #else
                p.z = p.w * (1.0 - 1e-6);
            #endif
                return p;
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
