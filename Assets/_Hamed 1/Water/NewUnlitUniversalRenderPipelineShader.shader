Shader "URP/ToonWaterLit"
{
    Properties
    {
        // Colors
        _ShallowColor("Shallow Color", Color) = (0.3, 0.6, 0.8, 1)
        _DeepColor("Deep Color", Color) = (0.1, 0.3, 0.5, 1)
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        
        // Toon Lighting
        _ToonRamp("Toon Ramp", 2D) = "white" {}
        _RampSmoothness("Ramp Smoothness", Range(0, 1)) = 0.1
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.5
        
        // Waves
        _WaveSpeed("Wave Speed", Float) = 1.0
        _WaveHeight("Wave Height", Float) = 0.1
        _WaveFrequency("Wave Frequency", Float) = 1.0
        _WaveDirection("Wave Direction", Vector) = (1, 0, 0, 0)
        
        // Surface
        _Smoothness("Smoothness", Range(0, 1)) = 0.8
        _Transparency("Transparency", Range(0, 1)) = 0.7
        _FresnelPower("Fresnel Power", Range(0, 10)) = 5.0
        
        // Distortion
        _DistortionStrength("Distortion Strength", Float) = 0.1
        _DistortionSpeed("Distortion Speed", Float) = 0.5
        
        // Foam
        _FoamDistance("Foam Distance", Float) = 0.5
        _FoamSpeed("Foam Speed", Float) = 0.2
        _FoamNoiseScale("Foam Noise Scale", Float) = 1.0
        
        // Ripple
        _RippleSpeed("Ripple Speed", Float) = 1.0
        _RippleScale("Ripple Scale", Float) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float fogFactor : TEXCOORD5;
                float4 screenPos : TEXCOORD6;
            };
            
            // Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float4 _SpecularColor;
                float4 _WaveDirection;
                
                float _WaveSpeed;
                float _WaveHeight;
                float _WaveFrequency;
                float _Smoothness;
                float _Transparency;
                float _FresnelPower;
                float _DistortionStrength;
                float _DistortionSpeed;
                float _FoamDistance;
                float _FoamSpeed;
                float _FoamNoiseScale;
                float _RippleSpeed;
                float _RippleScale;
                float _ShadowThreshold;
                float _RampSmoothness;
            CBUFFER_END
            
            TEXTURE2D(_ToonRamp);
            SAMPLER(sampler_ToonRamp);
            
            // Simple noise function for waves
            float2 gradientNoiseDir(float2 p)
            {
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }
            
            float gradientNoise(float2 p)
            {
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(gradientNoiseDir(ip), fp);
                float d01 = dot(gradientNoiseDir(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(gradientNoiseDir(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(gradientNoiseDir(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
            }
            
            // Gerstner wave function
            float3 GerstnerWave(float3 position, float2 direction, float speed, float frequency, float amplitude)
            {
                float time = _Time.y * speed;
                float wave = frequency * (dot(direction, position.xz) + time);
                float cosWave = cos(wave);
                float sinWave = sin(wave);
                
                float3 result;
                result.x = direction.x * amplitude * cosWave;
                result.y = amplitude * sinWave;
                result.z = direction.y * amplitude * cosWave;
                
                return result;
            }
            
            // Toon lighting function
            float3 ToonLighting(Light light, float3 normal, float3 viewDir, float3 albedo)
            {
                // Lambert diffuse
                float NdotL = saturate(dot(normal, light.direction));
                
                // Apply toon ramp
                float ramp = smoothstep(_ShadowThreshold - _RampSmoothness, 
                                       _ShadowThreshold + _RampSmoothness, 
                                       NdotL);
                
                // Sample ramp texture if provided
                float4 rampSample = SAMPLE_TEXTURE2D(_ToonRamp, sampler_ToonRamp, float2(ramp, 0.5));
                float3 rampColor = rampSample.rgb;
                
                // Specular (toon style)
                float3 halfDir = normalize(light.direction + viewDir);
                float spec = pow(saturate(dot(normal, halfDir)), _FresnelPower * 10);
                spec = step(0.5, spec) * _Smoothness;
                
                // Combine
                float3 diffuse = light.color * rampColor * light.distanceAttenuation * light.shadowAttenuation;
                float3 specular = spec * _SpecularColor * light.color;
                
                return (diffuse + specular) * albedo;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Wave animation
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                
                // Apply multiple waves for more complex water surface
                float3 waveOffset = float3(0, 0, 0);
                waveOffset += GerstnerWave(worldPos, normalize(_WaveDirection.xy), _WaveSpeed, _WaveFrequency, _WaveHeight);
                waveOffset += GerstnerWave(worldPos, float2(0.7, 0.3), _WaveSpeed * 0.8, _WaveFrequency * 1.3, _WaveHeight * 0.7);
                waveOffset += GerstnerWave(worldPos, float2(-0.3, 0.9), _WaveSpeed * 1.2, _WaveFrequency * 0.7, _WaveHeight * 0.5);
                
                // Calculate normal from wave derivatives
                float2 waveDerivX = float2(
                    _WaveFrequency * _WaveDirection.x * _WaveHeight * -sin(_WaveFrequency * (dot(_WaveDirection.xy, worldPos.xz) + _Time.y * _WaveSpeed)),
                    0
                );
                
                float2 waveDerivZ = float2(
                    0,
                    _WaveFrequency * _WaveDirection.y * _WaveHeight * -sin(_WaveFrequency * (dot(_WaveDirection.xy, worldPos.xz) + _Time.y * _WaveSpeed))
                );
                
                // Apply wave offset
                worldPos += waveOffset;
                
                // Transform to clip space
                output.positionHCS = TransformWorldToHClip(worldPos);
                output.positionWS = worldPos;
                output.uv = input.uv;
                
                // Calculate normals
                float3 normalOS = input.normalOS;
                float3 tangentOS = input.tangentOS.xyz;
                float3 bitangentOS = cross(normalOS, tangentOS) * input.tangentOS.w;
                
                // Wave normal calculation
                float3 waveNormal = normalize(float3(-waveDerivX.x, 1.0, -waveDerivZ.y));
                output.normalWS = TransformObjectToWorldNormal(waveNormal);
                
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(worldPos);
                
                // Shadow coordinates
                output.shadowCoord = TransformWorldToShadowCoord(worldPos);
                
                // Screen position for depth
                output.screenPos = ComputeScreenPos(output.positionHCS);
                
                // Fog
                output.fogFactor = ComputeFogFactor(output.positionHCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Get main light
                Light mainLight = GetMainLight(input.shadowCoord);
                
                // Depth-based color
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(input.screenPos.xy / input.screenPos.w), _ZBufferParams);
                float surfaceDepth = input.screenPos.w;
                float waterDepth = sceneDepth - surfaceDepth;
                float depthFactor = saturate(waterDepth / _FoamDistance);
                
                // Color based on depth
                float3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFactor);
                
                // Foam at edges
                float foamNoise = gradientNoise(input.positionWS.xz * _FoamNoiseScale + _Time.y * _FoamSpeed);
                float foam = saturate((1 - depthFactor) + foamNoise - 0.5);
                foam = step(0.7, foam);
                waterColor = lerp(waterColor, _FoamColor.rgb, foam);
                
                // Fresnel effect
                float fresnel = pow(1.0 - saturate(dot(input.normalWS, input.viewDirWS)), _FresnelPower);
                fresnel = smoothstep(0.4, 0.6, fresnel);
                
                // Apply toon lighting
                float3 litColor = ToonLighting(mainLight, input.normalWS, input.viewDirWS, waterColor);
                
                // Additional lights
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    litColor += ToonLighting(light, input.normalWS, input.viewDirWS, waterColor) * 0.5;
                }
                
                // Add fresnel to specular
                litColor += fresnel * _SpecularColor * 0.3;
                
                // Fog
                litColor = MixFog(litColor, input.fogFactor);
                
                // Alpha based on depth and transparency
                float alpha = lerp(_Transparency, 1.0, saturate(depthFactor * 2));
                
                return half4(litColor, alpha);
            }
            ENDHLSL
        }
        
        // Shadow caster pass for shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            float3 _WaveDirection;
            float _WaveSpeed;
            float _WaveHeight;
            float _WaveFrequency;
            
            float3 GerstnerWave(float3 position, float2 direction, float speed, float frequency, float amplitude)
            {
                float time = _Time.y * speed;
                float wave = frequency * (dot(direction, position.xz) + time);
                float cosWave = cos(wave);
                float sinWave = sin(wave);
                
                float3 result;
                result.x = direction.x * amplitude * cosWave;
                result.y = amplitude * sinWave;
                result.z = direction.y * amplitude * cosWave;
                
                return result;
            }
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                
                // Apply waves in shadow pass too
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 waveOffset = GerstnerWave(worldPos, normalize(_WaveDirection.xy), _WaveSpeed, _WaveFrequency, _WaveHeight);
                worldPos += waveOffset;
                
                output.positionHCS = TransformWorldToHClip(worldPos);
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}