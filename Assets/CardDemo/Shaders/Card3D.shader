Shader "SharderUltimate/Card3D"
{
    Properties
    {
        [NoScaleOffset] _FrontTex ("Front", 2D) = "white" {}
        [NoScaleOffset] _BackTex ("Back", 2D) = "white" {}
        [NoScaleOffset] _FoilMask ("Foil Mask", 2D) = "white" {}
        _FoilStrength ("Foil Strength", Range(0, 2)) = 0.8
        _FoilScale ("Foil Scale", Range(0.5, 8)) = 3.0
        _Glare ("Glare", Range(0, 3)) = 1.2
        _Rim ("Rim", Range(0, 2)) = 0.5
        _Parallax ("Parallax", Range(0, 0.1)) = 0.025
        _Corner ("Corner Radius", Range(0, 0.2)) = 0.06
        _Aspect ("Aspect (w/h)", Float) = 0.6667
    }
    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FrontTex); SAMPLER(sampler_FrontTex);
            TEXTURE2D(_BackTex);  SAMPLER(sampler_BackTex);
            TEXTURE2D(_FoilMask); SAMPLER(sampler_FoilMask);

            CBUFFER_START(UnityPerMaterial)
                half _FoilStrength, _FoilScale, _Glare, _Rim, _Parallax, _Corner, _Aspect;
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

            half4 frag(Varyings IN, bool isFront : SV_IsFrontFace) : SV_Target
            {
                // Bo góc bằng SDF hộp bo tròn trong không gian UV
                float2 halfSize = float2(_Aspect, 1.0) * 0.5;
                float2 p = (IN.uv - 0.5) * float2(_Aspect, 1.0);
                float2 q = abs(p) - halfSize + _Corner;
                float dist = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _Corner;
                clip(-dist);

                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 N = normalize(IN.normalWS);
                if (!isFront) N = -N;

                float2 uv = IN.uv;
                half3 baseCol;
                half mask;
                if (isFront)
                {
                    // Parallax: dịch UV theo hướng nhìn trong tangent space, mạnh hơn ở vùng sáng
                    float3 vTS = float3(dot(V, normalize(IN.tangentWS)), dot(V, normalize(IN.bitangentWS)), dot(V, N));
                    float2 off = vTS.xy / max(vTS.z, 0.3) * _Parallax;
                    half h = dot(SAMPLE_TEXTURE2D(_FrontTex, sampler_FrontTex, uv).rgb, half3(0.3, 0.59, 0.11));
                    uv -= off * h;
                    baseCol = SAMPLE_TEXTURE2D(_FrontTex, sampler_FrontTex, uv).rgb;
                    mask = SAMPLE_TEXTURE2D(_FoilMask, sampler_FoilMask, uv).r;
                }
                else
                {
                    baseCol = SAMPLE_TEXTURE2D(_BackTex, sampler_BackTex, float2(1.0 - uv.x, uv.y)).rgb;
                    mask = 0.35;
                }

                // Foil cầu vồng: màu đổi theo góc nhìn, chỉ hiện rõ trong dải sáng quét qua card
                half ndv = saturate(dot(N, V));
                half hue = frac(ndv * _FoilScale + dot(uv, float2(0.6, 0.4)) * 1.5 + dot(V.xy, float2(1.5, 1.0)));
                float sweep = dot(uv, float2(0.7, 0.7)) + dot(V.xy, float2(2.0, 1.5));
                half band = pow(0.5 + 0.5 * cos(sweep * 7.5), 3.0);
                half lum = dot(baseCol, half3(0.3, 0.59, 0.11));
                half k = mask * _FoilStrength * (0.2 + 0.8 * band);
                half3 col = baseCol + Rainbow(hue) * k * (0.2 + 0.7 * lum);

                // Glare: vệt sáng phản chiếu từ đèn giả
                float3 L = normalize(float3(-0.4, 0.6, -0.7));
                float3 R = reflect(-V, N);
                half spec = pow(saturate(dot(R, L)), 18.0) * _Glare;
                col += spec * (0.4 + 0.6 * mask);

                // Viền sáng (fresnel)
                half rim = pow(1.0 - ndv, 3.0) * _Rim;
                col += rim * half3(0.6, 0.8, 1.0);

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
