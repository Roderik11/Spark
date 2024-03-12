#include "common.fx"

Texture2D MainTexture;
Texture2D brushTexture;
SamplerState sampData;
float2 brushPosition;
float brushScale;
float brushStrength;

struct VS_INPUT
{
	float3 Position	: POSITION0;
	float2 UV		: TEXCOORD0;
};

struct VS_OUTPUT
{
	float4 Position	: SV_POSITION;
	float2 UV		: TEXCOORD0;
};

VS_OUTPUT VS(VS_INPUT In)
{
	VS_OUTPUT OUT;

	OUT.Position = float4(In.Position, 1);
	OUT.UV = In.UV;
	return OUT;
}

float4 PS(VS_OUTPUT In) : SV_TARGET
{
	float height = MainTexture.SampleLevel(sampData, In.UV, 0).r;
	float2 brushUV = In.UV + (brushPosition - 0.5f * brushScale) / brushScale;
	float u = map(In.UV.x, brushPosition.x - 0.5f * brushScale, brushPosition.x + 0.5f * brushScale, 0, 1);
	float v = map(In.UV.y, brushPosition.y - 0.5f * brushScale, brushPosition.y + 0.5f * brushScale, 0, 1);
	brushUV = float2(u, v);

	float brush = brushTexture.SampleLevel(sampData, brushUV, 0).r;

	float d = distance(In.UV, brushPosition);

	float change = brush * brushStrength; // smoothstep(brushScale, 0, d) * .0001f * brushStrength;

	float c = change + height; // min(1, change + height);
	return float4(c, c, c, c);
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
