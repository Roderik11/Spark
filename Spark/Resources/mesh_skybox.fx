#include "common.fx"

cbuffer scene
{
	float4x4 View;
	float4x4 Projection;
	float3   CameraPosition;
}

cbuffer object
{
	float4x4 World;
}

TextureCube texDiffuse <string Property="texDiffuse";>;
SamplerState DiffuseSampler;

struct VertexInput
{
	float3 Position		: POSITION;
	float3 Normal		: NORMAL;
	float2 UV			: TEXCOORD0;
};

struct VertexOutput
{
	float4 Position		: SV_POSITION;
	float3 UV			: TEXCOORD0;
	float3 Depth		: TEXCOORD1;
};

struct FragmentOutput
{
	float4 Albedo		: SV_TARGET0;
	float4 Normals		: SV_TARGET1;
	float  Depth		: SV_TARGET2;
	float4 Data			: SV_TARGET3;
	float fDepth		: SV_DEPTH;
};

VertexOutput VS(VertexInput input)
{
	VertexOutput output;

	float4x4 mat = World;
	mat._41 = CameraPosition.x;
	mat._42 = CameraPosition.y;
	mat._43 = CameraPosition.z;
	mat._44 = 1;

	float4 worldPosition = mul(float4(input.Position, 1), mat);
	float4 viewPosition = mul(worldPosition, View);

	output.Position = mul(viewPosition, Projection);
	output.Depth = float3(output.Position.zw, viewPosition.z);
	output.UV = input.Position.xyz * 2;

	return output;
}

FragmentOutput PS(VertexOutput input)
{
	FragmentOutput output;

	half4 vDiffuseMap = texDiffuse.Sample(DiffuseSampler, input.UV);

	output.Albedo = float4(vDiffuseMap.rgb, 1);
	output.Normals = float4(0, 1, 0, 1);
	output.Depth = 1;
	output.Data = float4(0, 0, 0, 1);
	output.fDepth = 0.999999f;

	return output;
}

RasterizerState FrontCull
{
    CullMode = Front;
    DepthClipEnable = FALSE;
};

technique11 Standard
{
	pass Opaque
	{
        SetRasterizerState(FrontCull);
        SetVertexShader(CompileShader(vs_4_0, VS()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PS()));
	}
}