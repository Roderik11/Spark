#include "common.fx"

Texture2D Textures[3];
SamplerState sampData;

TextureCube texReflection;
SamplerState sampReflection;

float3 CameraPosition;
float4x4 InverseViewProjection;
float4x4 matProjectionInverse;
float4x4 matViewInverse;

struct VertexInput
{
	float3 vPosition	: POSITION0;
	float2 vUV			: TEXCOORD0;
};

struct VertexOutput
{
	float4 vPosition	: SV_POSITION;
	float2 vUV			: TEXCOORD0;
	float4 vView		: TEXCOORD1;
};

VertexOutput VS(VertexInput input)
{
	VertexOutput output;
	output.vPosition = float4(input.vPosition, 1);
	output.vUV = input.vUV;
	output.vView = mul(output.vPosition, matProjectionInverse);

	return output;
}

float4 PS(VertexOutput input) : SV_TARGET
{
	float4 output = 0;

	half3 Normal = Textures[0].Sample(sampData, input.vUV).rgb;
	float Depth = Textures[1].Sample(sampData, input.vUV).x;
	float4 Data = Textures[2].Sample(sampData, input.vUV);

	float3 vPositionVS = PositionVSFromDepth(input.vView.xyz, Depth);
	float3 vView = normalize(vPositionVS);

	float3 vWorldView = mul(vPositionVS.xyz, (float3x3)matViewInverse);
	float3 vWorldNormal = normalize(mul(Normal.xyz, (float3x3)matViewInverse));

	float3 vI = reflect(vWorldView, vWorldNormal);
	float NV = 1 - saturate(abs(dot(Normal, vView)));

	float4 vReflection = SampleLinearCubeLevel(texReflection, sampReflection, vI, 8 - Data.r * 8);

	output.rgb = vReflection.rgb * saturate(0.25f + NV);

	return pow(output, 1.0f / 2.2f);
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