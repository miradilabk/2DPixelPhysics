Shader "ExampleShader"
{
    SubShader
    {
        Pass
        {
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature _RENDERING_FADE
            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 color: COLOR0;
            };

            uniform StructuredBuffer<float2> PositionsBuffer;
            uniform StructuredBuffer<float2> VelocitiesBuffer;
            uniform StructuredBuffer<float> FoamFactorsBuffer;
            uniform Texture2D<float4> gradient;
			SamplerState linear_clamp_sampler;

            v2f vert(appdata_base v, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                v2f o;
                uint instanceID = GetIndirectInstanceID(svInstanceID);
                float4 wpos = v.vertex + float4(PositionsBuffer[instanceID].xy, 0, 0);
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.uv = v.texcoord;
                o.color = gradient.SampleLevel(linear_clamp_sampler, float2(FoamFactorsBuffer[instanceID], 0.5f), 0);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5f;
                float dist = length(uv)*2;
                if (dist > 0.1f) discard;
                return float4(i.color,1);
            }
            ENDCG
        }
    }
}