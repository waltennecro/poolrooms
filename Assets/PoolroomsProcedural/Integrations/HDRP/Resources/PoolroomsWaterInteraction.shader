Shader "Hidden/Poolrooms/WaterInteraction"
{
    Properties
    {
        [HideInInspector] _AffectDeformation("Deformation",Float)=1
        [HideInInspector] _AffectsFoam("Foam",Float)=1
        [HideInInspector] _AffectsSimulationMask("Mask",Float)=0
        [HideInInspector] _AffectsLargeCurrent("Large current",Float)=0
        [HideInInspector] _AffectsRipplesCurrent("Ripples current",Float)=0
    }
    HLSLINCLUDE
    #pragma target 4.5
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    struct Attributes { uint vertexID : SV_VertexID; };
    struct Varyings { float4 positionCS : SV_POSITION;float2 uv : TEXCOORD0; };
    Varyings Vert(Attributes input)
    {
        Varyings output;output.positionCS=GetFullScreenTriangleVertexPosition(input.vertexID);
        output.uv=GetFullScreenTriangleTexCoord(input.vertexID);return output;
    }
    int _RippleCount;
    float4 _DecalArea; // world centre XZ, size XY
    float4 _Ripples[12]; // world origin XZ, age, amplitude / maximum height
    float4 _Pools[12]; // world centre XZ, half extents
    float4 _Shapes[12]; // rounded corner radius, inverse lifetime, strength, impact flag
    float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
    float2 Evaluate(float2 uv)
    {
        float2 world=(uv-.5)*_DecalArea.zw+_DecalArea.xy;
        float height=0,foam=0;
        [loop] for(int i=0;i<min(_RippleCount,12);i++)
        {
            float4 e=_Ripples[i],pool=_Pools[i],shape=_Shapes[i];
            // Rounded pool SDF: suppress waves at the rim and in other disconnected pools.
            float2 q=abs(world-pool.xy)-pool.zw+shape.x;
            float sdf=length(max(q,0))+min(max(q.x,q.y),0)-shape.x;
            float boundary=saturate(-sdf/.12);
            float life=saturate(1-e.z*shape.y);life*=life;
            float distance=length(world-e.xy),front=.12+e.z*1.45;
            float phase=distance-front;
            float bands=.62*sin(distance*14-e.z*sqrt(9.81*14))+
                .25*sin(distance*23-e.z*sqrt(9.81*23))+
                .13*sin(distance*37-e.z*sqrt(9.81*37));
            float packet=exp(-phase*phase/.09)*bands;
            height+=packet*e.w*life*boundary/(1+distance*.65);
            // A short, broken foam patch only after a strong contact. No ocean foam.
            float splash=saturate((shape.z-.22)*1.5)*shape.w;
            float speckle=smoothstep(.42,.78,Hash(floor(world*35)));
            foam+=exp(-phase*phase/.025)*splash*exp(-e.z*5)*speckle*boundary;
        }
        return float2(clamp(height,-1,1),saturate(foam));
    }
    float4 Deform(Varyings input):SV_Target { return float4(Evaluate(input.uv).x,0,0,0); }
    // HDRP 17.3 WaterDecal.shader reads surface/deep foam from atlas Y/Z.
    float4 Foam(Varyings input):SV_Target { return float4(0,Evaluate(input.uv).y,0,0); }
    ENDHLSL
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "ShaderGraphTargetId"="WaterDecalSubTarget" }
        Cull Off ZWrite Off ZTest Always Blend Off
        Pass
        {
            Name "Deformation"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Deform
            ENDHLSL
        }
        Pass
        {
            Name "Foam"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Foam
            ENDHLSL
        }
    }
    Fallback Off
}
