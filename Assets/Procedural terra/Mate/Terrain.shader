Shader "Custom/Terrain"
{
    Properties
    {
        testTexture("Texture",2D) = "white"{}
        testScale("Scale",float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Input
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            Varyings vert (Input IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }
            
            const static int maxLayerCount =8;
            const static float epsilon = 1E-4;

            int layerCount;
            float3 baseColours[maxLayerCount];
            float baseStartHeights[maxLayerCount];
            float baseBlends[maxLayerCount];
            float baseColourStrength[maxLayerCount];
            float baseTextureScales[maxLayerCount];

            sampler2D testTexture;
            float testScale;

            TEXTURE2D_ARRAY(baseTextures); 
            SAMPLER(sampler_baseTextures);

            float minHeight;
            float maxHeight;
            float inverseLerp(float a,float b, float value)
            {
                return saturate((value-a)/(b-a));
            }

            float3 triplaner(float3 worldPos, float scale, float3 blenAxes,int textureIndex)
            {
                float3 scaledWorldPos = worldPos / scale;

                float3 xProjection = SAMPLE_TEXTURE2D_ARRAY(baseTextures, sampler_baseTextures, float2(scaledWorldPos.y, scaledWorldPos.z), textureIndex).rgb * blenAxes.x;
                float3 yProjection = SAMPLE_TEXTURE2D_ARRAY(baseTextures, sampler_baseTextures, float2(scaledWorldPos.x, scaledWorldPos.z), textureIndex).rgb * blenAxes.y;
                float3 zProjection = SAMPLE_TEXTURE2D_ARRAY(baseTextures, sampler_baseTextures, float2(scaledWorldPos.x, scaledWorldPos.y), textureIndex).rgb * blenAxes.z;

                return xProjection + yProjection + zProjection;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
                float3 blendAxes = abs(IN.worldNormal);
                blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
                float3 albedo = float3(0, 0, 0);

                for(int i = 0; i < layerCount; i++)
                {
                    float drawStrength = inverseLerp(-baseBlends[i]/2-epsilon,baseBlends[i]/2,heightPercent - baseStartHeights[i]);
                    float3 baseColour = baseColours[i]*baseColourStrength[i];
                    float3 textureColour = triplaner(IN.worldPos,baseTextureScales[i],blendAxes,i) * (1-baseColourStrength[i]);
                    albedo = albedo * (1 - drawStrength) + (baseColour + textureColour) * drawStrength;
                }

                Light mainLight = GetMainLight();
                float3 normal = normalize(IN.worldNormal);
                float NdotL = saturate(dot(normal, mainLight.direction));
                float3 lighting = mainLight.color.rgb * NdotL + 0.2;

                return half4(albedo * lighting, 1);
            }
            ENDHLSL
        }

        // 写入 _CameraDepthTexture（无 SSAO/法线预通道时使用）
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
            };

            struct DepthVaryings
            {
                float4 positionHCS : SV_POSITION;
            };

            DepthVaryings DepthVert (DepthAttributes IN)
            {
                DepthVaryings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half DepthFrag (DepthVaryings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // 写入深度+法线（SSAO / DepthNormalPrepass 模式下使用，这是水能感知地形的关键）
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DNAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct DNVaryings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
            };

            DNVaryings DepthNormalsVert (DNAttributes IN)
            {
                DNVaryings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 DepthNormalsFrag (DNVaryings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                return half4(normalWS, 0.0);
            }
            ENDHLSL
        }

        // 让地形能投射阴影
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionHCS : SV_POSITION;
            };

            ShadowVaryings ShadowVert (ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionHCS = positionCS;
                return OUT;
            }

            half ShadowFrag (ShadowVaryings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}