// Bóng tối mờ sát mép cửa sổ portal, tính theo từng pixel từ khoảng cách tới viền phần nhìn thấy
// (giao của cửa sổ và mặt thẻ): dày đều với mọi shape, không bị chồng lớp ở góc như khi dựng bằng mesh.
Shader "SharderUltimate/PortalVignette"
{
    Properties
    {
        _StencilRef ("Stencil Ref", Int) = 1
        _VignetteColor ("Color", Color) = (0, 0, 0, 1)
        // HoloCardView set qua MaterialPropertyBlock
        _Vignette ("Vignette (width, strength)", Vector) = (0.22, 0.8, 0, 0)
        _WindowShape ("Window (halfX, halfY, corner, type)", Vector) = (0, 0, 0, 0)
        _WindowPoly ("Polygon (sides, rotation rad)", Vector) = (6, 0, 0, 0)
        _CardRect ("Card (center from window xy, corner)", Vector) = (0, 0, 0, 0)
        _CardHalf ("Card Half Size", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
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
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WindowShape.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _VignetteColor;
                float4 _Vignette, _WindowShape, _WindowPoly, _CardRect, _CardHalf;
            CBUFFER_END

            struct Varyings { float4 positionCS : SV_POSITION; float2 local : TEXCOORD0; };

            Varyings vert(float4 positionOS : POSITION)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(positionOS.xyz);
                OUT.local = positionOS.xy;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (_WindowShape.x <= 0.0) return 0;
                float d = VisibleWindowDistance(IN.local, _WindowShape, _WindowPoly, _CardRect, _CardHalf.xy);
                half t = saturate(1.0 + d / max(_Vignette.x, 1e-4));   // 0 ở sâu bên trong, 1 tại mép
                return half4(_VignetteColor.rgb, t * t * _Vignette.y);
            }
            ENDHLSL
        }
    }
}
