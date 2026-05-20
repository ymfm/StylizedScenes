Shader "Dissolve/StencilWriter"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-100" }
        // 关键：Queue 比所有不透明物体都早，先盖章再画场景

        Pass
        {
            // 不写颜色
            ColorMask 0
            // 不写深度（避免遮挡后面物体）
            ZWrite Off
            // 深度测试也关掉，让球完整盖章不被地形挡住
            ZTest Always
            // 双面（防止相机进入球内时盖章失效）
            Cull Off

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}