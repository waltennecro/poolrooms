Shader "Hidden/Poolrooms/Splash"
{
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "PoolroomsDroplets"
            Tags { "LightMode"="ForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off ZTest LEqual
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            struct Attributes
            { float3 positionOS : POSITION;float4 color : COLOR;float2 uv : TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            { float4 positionCS : SV_POSITION;float4 color : COLOR;float2 uv : TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS);output.uv=input.uv;output.color=input.color;
                return output;
            }
            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 p=input.uv-.5;
                float alpha=saturate((.25-dot(p,p))*5)*input.color.a*.6;
                float glint=exp(-dot(p-float2(-.12,.14),p-float2(-.12,.14))*60);
                return float4(input.color.rgb*(.7+glint*.3),alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
