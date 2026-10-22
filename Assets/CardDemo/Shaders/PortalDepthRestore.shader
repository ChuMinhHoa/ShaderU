// Ghi lại depth của mặt cửa sổ sau khi đã vẽ xong nội dung portal (PortalDepthReset đã đặt depth về xa nhất).
// Nhờ vậy những thứ vẽ sau (ví dụ phần pop-out của nhân vật ở thẻ phía sau) bị mặt cửa sổ của thẻ phía trước
// che đúng, thay vì vẽ đè lên nó. Chỉ ghi ở pixel có stencil = ref (cửa sổ thật sự nhìn thấy).
Shader "SharderUltimate/PortalDepthRestore"
{
    Properties
    {
        _StencilRef ("Stencil Ref", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Transparent+700" "RenderPipeline" = "UniversalPipeline" }

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
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
