#include "common.fx"

Texture2D MainTexture;
SamplerState sampData;
float slice;

struct VS_INPUT
{
	float3 Position	: POSITION0;
	float2 UV			: TEXCOORD0;
};

struct VS_OUTPUT
{
	float4 Position	: SV_POSITION;
	float2 UV		: TEXCOORD0;
	//uint depthSlice : SV_RenderTargetArrayIndex;
};

VS_OUTPUT VS(VS_INPUT In)
{
	VS_OUTPUT OUT;

	OUT.Position = float4(In.Position, 1);
	OUT.UV = In.UV;
	//OUT.depthSlice = (int)slice;
	return OUT;
}

//Pixel Shader
float4 PS(VS_OUTPUT In) : SV_TARGET
{
	float4 col = MainTexture.SampleLevel(sampData, In.UV, 0);
    return col;
}

technique11 T1
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_4_0, VS()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PS()));
	}
}
