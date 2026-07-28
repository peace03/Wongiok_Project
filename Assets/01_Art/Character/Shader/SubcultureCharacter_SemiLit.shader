Shader "Grimoire/Character/SubcultureToon"
{
    // 서브컬쳐(원신/붕괴 계열) 스타일 셀셰이딩 캐릭터 쉐이더
    // - 노멀맵 지원 (Substance Painter OpenGL 탄젠트 노멀 기준)
    // - 2단 램프 라이팅 (Mask Map 기반, Substance 커스텀 채널로 베이크)
    // - 스타일라이즈드 림라이트
    // - 별도 Pass로 아웃라인 (Inverted Hull)
    // - 캐릭터 상징색을 그림자/림 컬러에 연동 가능 (GRIMOIRE 색언어 대응)

    Properties
    {
        [Header(Base)]
        _BaseMap ("Base Color Texture", 2D) = "white" {}
        _BaseColor ("Base Color Tint", Color) = (1,1,1,1)

        [Header(Normal Map)]
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 2)) = 1.0

        [Header(Mask Map)]
        // Substance Painter Custom Channels 로 베이크:
        // R = AO / Shadow Bias   (부위별 그림자 임계값 보정, 어두울수록 그림자 잘 짐)
        // G = Specular Mask      (하이라이트 세기)
        // B = Rim Mask           (림라이트 세기)
        // A = Smoothness         (예비 채널, 현재 셰이더에서는 미사용)
        _MaskMap ("Mask Map (R:AO G:Spec B:Rim A:Smooth)", 2D) = "white" {}

        [Header(Shadow Ramp)]
        _ShadowColor ("Shadow Color", Color) = (0.55, 0.5, 0.65, 1) // 기본은 보라끼 그림자 (진실/직시 톤)
        _ShadowStep ("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSoftness ("Shadow Edge Softness", Range(0.001, 0.5)) = 0.05

        [Header(SemiLit_SceneLightResponse)]
        // 아래 세 값을 전부 0으로 내리면 기존(순수 스타일라이즈드) 동작과 동일해짐
        _LightColorInfluence ("Light Color Influence", Range(0,1)) = 0.5   // 씬 라이트 색/강도를 밝은 영역에 얼마나 반영할지
        _ShadowReceiveIntensity ("Realtime Shadow Receive", Range(0,1)) = 1.0 // 다른 오브젝트가 드리우는 그림자를 토니 램프에 얼마나 스냅시킬지
        _AmbientIntensity ("Ambient (SH) Intensity", Range(0,1)) = 0.25  // 그림자 영역에 환경광을 얼마나 섞을지

        [Header(Specular)]
        _SpecColor ("Specular Color", Color) = (1,1,1,1)
        _SpecPower ("Specular Power", Range(1,256)) = 40
        _SpecIntensity ("Specular Intensity", Range(0,2)) = 1

        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (0.7, 0.3, 0.9, 1) // 캐릭터 상징색으로 교체 권장
        _RimPower ("Rim Power", Range(0.1, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0,3)) = 1.2

        [Header(Face Special Shading)]
        [Toggle(_FACE_SDF)] _UseFaceSDF ("Use Face SDF Shadow (for face material)", Float) = 0
        _FaceSDFMap ("Face SDF Shadow Map", 2D) = "white" {}
        _FaceForward ("Face Forward Dir (World, set via script)", Vector) = (0,0,1,0)

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.05, 0.03, 0.08, 1)
        _OutlineWidth ("Outline Width", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // ------------------------------------------------------------
        // Pass 1: 아웃라인 (Inverted Hull)
        // ------------------------------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                // 화면(카메라) 거리에 비례해 두께 보정 -> 원근에서도 일정한 두께로 보이게
                float dist = distance(vpi.positionWS, _WorldSpaceCameraPos);
                float width = _OutlineWidth * 0.01 * dist;

                float3 offsetPosWS = vpi.positionWS + normalWS * width;
                OUT.positionHCS = TransformWorldToHClip(offsetPosWS);
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        // Pass 2: 메인 톤 셰이딩
        // ------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _FACE_SDF

            // 실시간 그림자 수신용 (Light 모드/캐스케이드에 따라 URP가 알아서 스위칭)
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MaskMap);  SAMPLER(sampler_MaskMap);
            TEXTURE2D(_FaceSDFMap); SAMPLER(sampler_FaceSDFMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _NormalScale;
                float4 _ShadowColor;
                float _ShadowStep;
                float _ShadowSoftness;
                float4 _SpecColor;
                float _SpecPower;
                float _SpecIntensity;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float4 _FaceForward;
                float _LightColorInfluence;
                float _ShadowReceiveIntensity;
                float _AmbientIntensity;
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
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 tangentWS   : TEXCOORD2; // xyz: tangent, w: bitangent sign
                float3 positionWS  : TEXCOORD3;
                float3 viewDirWS   : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5; // 실시간 그림자 수신용
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vpi.positionCS;
                OUT.positionWS  = vpi.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                float3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.tangentWS   = float4(tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.viewDirWS   = GetWorldSpaceViewDir(vpi.positionWS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = GetShadowCoord(vpi);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 geoN = normalize(IN.normalWS);
                float3 T = normalize(IN.tangentWS.xyz);
                float3 B = cross(geoN, T) * IN.tangentWS.w;
                float3x3 TBN = float3x3(T, B, geoN);

                half4 normalTexRaw = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv);
                float3 normalTS = UnpackNormalScale(normalTexRaw, _NormalScale);
                float3 N = normalize(mul(normalTS, TBN)); // 탄젠트 -> 월드 공간

                float3 V = normalize(IN.viewDirWS);

                Light mainLight = GetMainLight(IN.shadowCoord); // shadowCoord를 넘겨야 실시간 그림자(shadowAttenuation)가 채워짐
                float3 L = normalize(mainLight.direction);

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, IN.uv);
                // mask.r = AO/그림자 임계값 보정 / mask.g = 스페큘러 마스크 / mask.b = 림 마스크 / mask.a = 예비(Smoothness)

                // ---- 1) 명암 (2단 램프) ----
                float NdotL;
                #if defined(_FACE_SDF)
                    // 얼굴은 실제 라이팅 방향이 아니라 SDF 텍스처로 그림자 결정
                    // (빛이 어디서 와도 얼굴 음영 형태가 일정하게 유지됨)
                    float sdf = SAMPLE_TEXTURE2D(_FaceSDFMap, sampler_FaceSDFMap, IN.uv).r;
                    float lightSide = dot(normalize(_FaceForward.xyz), L) * 0.5 + 0.5;
                    NdotL = step(sdf, lightSide);
                #else
                    NdotL = dot(N, L) * 0.5 + 0.5;
                #endif

                float threshold = _ShadowStep + (mask.r - 0.5) * 0.5; // mask.r(AO)로 부위별 그림자 경계 보정
                float shadowMask = smoothstep(threshold - _ShadowSoftness, threshold + _ShadowSoftness, NdotL);

                // ---- 세미 라이트 1) 실시간 그림자(다른 오브젝트가 드리우는 그림자) ----
                // 부드러운 PCF 그라데이션 대신 step으로 스냅해서 토니 램프 경계와 이질감 없게 유지
                float realtimeShadow = lerp(1.0, step(0.5, mainLight.shadowAttenuation), _ShadowReceiveIntensity);
                shadowMask *= realtimeShadow;

                // ---- 세미 라이트 2) 씬 라이트 색상/강도를 밝은 영역에 반영 ----
                half3 lightTint = lerp(half3(1,1,1), mainLight.color, _LightColorInfluence);

                half3 litColor    = baseTex.rgb * _BaseColor.rgb * lightTint;
                half3 shadowColor = baseTex.rgb * _BaseColor.rgb * _ShadowColor.rgb; // 그림자색은 스타일 유지 위해 씬 라이트색과 독립
                half3 albedo = lerp(shadowColor, litColor, shadowMask);

                // ---- 세미 라이트 3) 환경광(SH) 소량 추가 ----
                // 그림자 영역이 완전히 고정된 단색이 아니라 주변 환경을 살짝 반영하도록
                half3 ambient = SampleSH(N) * _AmbientIntensity * baseTex.rgb;
                albedo += ambient;

                // ---- 2) 스타일라이즈드 스페큘러 ----
                float3 H = normalize(L + V);
                float NdotH = saturate(dot(N, H));
                float specTerm = step(0.9, pow(NdotH, _SpecPower)) * mask.g * _SpecIntensity;
                half3 specular = _SpecColor.rgb * specTerm;

                // ---- 3) 림 라이트 ----
                float rim = 1.0 - saturate(dot(N, V));
                rim = pow(rim, _RimPower) * mask.b * _RimIntensity;
                // 빛이 있는 방향 쪽에서만 림이 두드러지게 (역광 느낌 강화, 선택적)
                rim *= saturate(dot(N, L) * 0.5 + 0.7);
                half3 rimLight = _RimColor.rgb * rim;

                half3 finalColor = albedo + specular + rimLight;
                return half4(finalColor, baseTex.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
