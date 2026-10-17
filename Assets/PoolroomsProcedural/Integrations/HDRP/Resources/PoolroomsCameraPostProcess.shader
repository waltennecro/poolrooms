Shader "Hidden/Poolrooms/CameraPostProcess"
{
    Properties { _MainTex("Camera color", 2DArray) = "grey" {} }
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

    struct Attributes { uint vertexID : SV_VertexID; UNITY_VERTEX_INPUT_INSTANCE_ID };
    struct Varyings { float4 positionCS : SV_POSITION; float2 texcoord : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS=GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord=GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }
    TEXTURE2D_X(_MainTex);
    float4 _Analog; // scanline amount, color bleed in pixels, jitter in pixels, lifted blacks
    float _Diffusion,_EffectTime,_Wetness,_WetSeed,_WetElapsed,_FlareIntensity;
    float _FlareSize,_GhostIntensity;
    int _FlareCount,_GhostCount;
    float4 _FlarePositions[4]; // viewport x/y, occlusion-tested strength, reflected flag
    float4 _FlareColors[4]; // RGB from the actual light, W from apparent emitter size

    float Hash(float n) { return frac(sin(n*127.1+31.7)*43758.5453); }
    float3 Source(float2 uv)
    {
        // Use HDRP's clamped/scaled UV helper for dynamic resolution, DLSS and RTHandle padding.
        uv=ClampAndScaleUVForBilinearPostProcessTexture(saturate(uv));
        return SAMPLE_TEXTURE2D_X(_MainTex,s_linear_clamp_sampler,uv).rgb;
    }
    float3 Optics(float2 uv,float aspect)
    {
        float3 addition=0;
        [loop] for(int i=0;i<min(_FlareCount,4);i++)
        {
            float4 source=_FlarePositions[i];float2 lightUV=source.xy;
            // WorldToViewportPoint uses bottom-left coordinates; fullscreen texture UVs are
            // flipped on D3D/Metal/Vulkan. Keep each glint aligned with the actual lamp.
            #if UNITY_UV_STARTS_AT_TOP
                lightUV.y=1-lightUV.y;
            #endif
            float reflected=saturate(source.w);
            float apparent=clamp(_FlareColors[i].w*_FlareSize,.35,2.4);
            float2 delta=(uv-lightUV)*float2(aspect,1)/apparent;
            // Reflected sources form a flatter glint; the camera reflection checks the real water.
            delta.y*=lerp(1,1.65,reflected);
            float d2=dot(delta,delta);
            float core=exp(-d2/0.000012)*.85;
            float corona=exp(-d2/0.0018)*.22;
            float halo=exp(-d2/0.006)*.065;
            float streak=exp(-abs(delta.y)*650)*exp(-abs(delta.x)*10)*.17;
            streak+=exp(-abs(delta.x)*850)*exp(-abs(delta.y)*28)*.055*(1-reflected*.65);
            float3 ghosts=0;
            [unroll] for(int j=0;j<2;j++)
            {
                if(j>=_GhostCount)continue;
                float2 ghost=.5-(lightUV-.5)*(.42+j*.53);
                float2 g=(uv-ghost)*float2(aspect,1);
                float radius=(.015+j*.014)*apparent;
                float ring=exp(-abs(length(g)-radius)*420)*.018;
                float disc=exp(-dot(g,g)/(radius*radius))*.025;
                // Small coating differences, tinted by the real source rather than a fixed blue.
                float3 coating=j==0?float3(.93,1,.87):float3(.88,.94,1);
                ghosts+=coating*(ring+disc)*_GhostIntensity*(1-reflected*.55);
            }
            float2 centerDelta=(lightUV-.5)*float2(aspect,1);
            float alignment=.65+.35*saturate(1-length(centerDelta));
            float3 tint=_FlareColors[i].rgb;
            addition+=tint*((core+corona+halo+streak)+ghosts)*source.z*_FlareIntensity*alignment;
        }
        // Bound only the added optical glare, preserving the original HDRP scene color.
        return addition/(1+Luminance(addition));
    }
    float4 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv=input.texcoord;
        float aspect=_ScreenSize.x/max(_ScreenSize.y,1);
        float2 pixel=_ScreenSize.zw;
        float edgeShade=0;
        if(_Wetness>0.0001)
        {
            [unroll] for(int i=0;i<6;i++)
            {
                float seed=i*1.37+_WetSeed*7.91;
                float2 center=float2(.08+.84*Hash(seed),.12+.75*Hash(seed+3));
                center.y-=_WetElapsed*(.002+.003*Hash(seed+4));
                float radius=.018+.026*Hash(seed+8);
                float2 delta=(uv-center)*float2(aspect,1.25);
                float r=length(delta)/radius;
                float shape=saturate(1-r*r);
                // A small refractive lens with a soft rim; it fades instead of obscuring vision.
                uv+=delta*shape*_Wetness*.24/float2(aspect,1.25);
                edgeShade+=exp(-abs(r-.88)*22)*_Wetness*.07;
            }
        }
        float rows=min(_ScreenSize.y,480);
        float row=floor(uv.y*rows);
        float jitter=sin(row*.027+_EffectTime*1.7)*_Analog.z;
        uv.x+=jitter*pixel.x;
        float3 color=Source(uv);
        if(_Analog.y>0.0001)
        {
            color.r=lerp(color.r,Source(uv+float2(pixel.x*_Analog.y,0)).r,.55);
            color.b=lerp(color.b,Source(uv-float2(pixel.x*_Analog.y,0)).b,.4);
        }
        if(_Diffusion>0.0001)
        {
            float2 offset=pixel*2.5;
            float3 soft=(Source(uv+float2(offset.x,0))+Source(uv-float2(offset.x,0))+
                Source(uv+float2(0,offset.y))+Source(uv-float2(0,offset.y)))*.25;
            color=lerp(color,soft,_Diffusion);
        }
        float scan=.5+.5*cos(uv.y*rows*PI);
        color*=1-scan*_Analog.x;
        color=color*(1-_Analog.w)+float3(.4,.36,.31)*_Analog.w;
        color*=1-saturate(edgeShade);
        color+=Optics(input.texcoord,aspect);
        return float4(max(color,0),1);
    }
    ENDHLSL
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" }
        Pass
        {
            Name "PoolroomsCameraOptics"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
