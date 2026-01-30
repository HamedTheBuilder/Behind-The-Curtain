Shader "Custom/URP_LitTheaterCurtain"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.5, 0.05, 0.05, 1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.3
        
        [Header(Wind Settings)]
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1
        _WindStrength ("Wind Strength", Range(0, 0.5)) = 0.15
        _BillowFrequency ("Billow Frequency", Range(0.5, 5)) = 1.5
        _FoldCount ("Fold Count", Range(1, 20)) = 8
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                float _WindSpeed;
                float _WindStrength;
                float _BillowFrequency;
                float _FoldCount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // Wind animation
                float moveFactor = 1.0 - IN.uv.y;
                moveFactor = moveFactor * moveFactor;
                
                float time = _Time.y * _WindSpeed;
                float folds = sin(IN.uv.x * _FoldCount * 3.14159);
                float billow = sin(time * _BillowFrequency + IN.uv.x * 2.0);
                billow += sin(time * _BillowFrequency * 0.7 + IN.uv.x * 3.5) * 0.5;
                
                float zOffset = (folds * 0.3 + billow) * _WindStrength * moveFactor;
                float xOffset = sin(time * _BillowFrequency * 0.5 + IN.uv.x * 1.5) 
                               * _WindStrength * 0.3 * moveFactor;
                
                IN.positionOS.xyz += float3(xOffset, 0, zOffset);
                
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                
                // Main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3 lighting = mainLight.color * mainLight.shadowAttenuation * 
                                saturate(dot(IN.normalWS, mainLight.direction));
                
                // Ambient
                half3 ambient = SampleSH(IN.normalWS);
                
                // Additional lights
                uint additionalLightsCount = GetAdditionalLightsCount();
                for (uint i = 0; i < additionalLightsCount; i++)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);
                    lighting += light.color * light.distanceAttenuation * light.shadowAttenuation *
                               saturate(dot(IN.normalWS, light.direction));
                }
                
                half3 finalColor = baseColor.rgb * (ambient + lighting);
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                float _WindSpeed;
                float _WindStrength;
                float _BillowFrequency;
                float _FoldCount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                
                float moveFactor = 1.0 - IN.uv.y;
                moveFactor = moveFactor * moveFactor;
                
                float time = _Time.y * _WindSpeed;
                float folds = sin(IN.uv.x * _FoldCount * 3.14159);
                float billow = sin(time * _BillowFrequency + IN.uv.x * 2.0);
                billow += sin(time * _BillowFrequency * 0.7 + IN.uv.x * 3.5) * 0.5;
                
                float zOffset = (folds * 0.3 + billow) * _WindStrength * moveFactor;
                float xOffset = sin(time * _BillowFrequency * 0.5 + IN.uv.x * 1.5) 
                               * _WindStrength * 0.3 * moveFactor;
                
                IN.positionOS.xyz += float3(xOffset, 0, zOffset);
                
                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(
                    TransformObjectToWorld(IN.positionOS.xyz),
                    TransformObjectToWorldNormal(IN.normalOS),
                    _MainLightPosition.xyz));
                    
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}