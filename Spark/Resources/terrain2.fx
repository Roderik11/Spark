#include "common"

cbuffer scene
{
	float4x4 View;
	float4x4 Projection;
}

cbuffer object
{
	float4x4 World;
}

float4 Rect;
float2 Tiling;

int Level;
int MapSize;
float MaxHeight;

Texture2D Height;
Texture2D Textures[3];
Texture2D Splats[4];

SamplerState sampData;
SamplerState sampHeight;
SamplerState sampHeightFilter;

struct VS_INPUT
{
	float3 Position		: POSITION;
	float3 Normal		: NORMAL;
	float2 UV			: TEXCOORD0;
	float3 Tangent		: TANGENT;
	float3 BiTangent	: BINORMAL;
};

struct VS_OUTPUT
{
	float4 Position		: SV_POSITION;
	float2 UV			: TEXCOORD0;
	float3 Depth		: TEXCOORD1;
	float2 UV2			: TEXCOORD2;
};

struct PS_OUTPUT
{
	float4 Albedo		: SV_TARGET0;
	float4 Normals		: SV_TARGET1;
	float  Depth : SV_TARGET2;
	float4 Data			: SV_TARGET3;
	float fDepth : SV_DEPTH;
};

float3 blend(float4 texture1, float a1, float4 texture2, float a2)
{
	float depth = 0.2;
	float ma = max(texture1.a + a1, texture2.a + a2) - depth;

	float b1 = max(texture1.a + a1 - ma, 0);
	float b2 = max(texture2.a + a2 - ma, 0);

	return (texture1.rgb * b1 + texture2.rgb * b2) / (b1 + b2);
}

float4 SmoothHeight(sampler texSam, float2 uv)
{
	float texelSize = 1.0f / 4097.0f; //size of one texel
	float textureSize = 4097;

	float4 height00 = Height.SampleLevel(texSam, uv, 0);
	float4 height10 = Height.SampleLevel(texSam, uv + float2(texelSize, 0), 0);
	float4 height01 = Height.SampleLevel(texSam, uv + float2(0, texelSize), 0);
	float4 height11 = Height.SampleLevel(texSam, uv + float2(texelSize, texelSize), 0);

	float2 f = frac(uv.xy * textureSize);
	float4 tA = lerp(height00, height10, f.x);
	float4 tB = lerp(height01, height11, f.x);
	return lerp(tA, tB, f.y);
}

float3 GetNormal(float2 uv)
{
	float texelSize = 1.0f / (MapSize); //size of one texel
	float texelAspect = 1;

	float4 h;
	h[0] = Height.SampleLevel(sampHeightFilter, uv + texelSize * float2(0, -1), 0).r * texelAspect;
	h[1] = Height.SampleLevel(sampHeightFilter, uv + texelSize * float2(-1, 0), 0).r * texelAspect;
	h[2] = Height.SampleLevel(sampHeightFilter, uv + texelSize * float2(1, 0), 0).r * texelAspect;
	h[3] = Height.SampleLevel(sampHeightFilter, uv + texelSize * float2(0, 1), 0).r * texelAspect;

	float3 n;
	n.z = h[0] - h[3];
	n.x = h[1] - h[2];
	n.y = 1 / pow(2, Level - 1) * 1;

	return normalize(n);
}

//Vertex Shader
VS_OUTPUT VS(VS_INPUT input)
{
	VS_OUTPUT output;

	float2 uv = float2(Rect.x + input.UV.x * (Rect.z - Rect.x), Rect.y + (1 - input.UV.y) * (Rect.w - Rect.y));
	//float height = Height.SampleLevel(sampHeight, uv, 0).r;
	float height = Height.SampleLevel(sampHeight, input.UV, 0).r;
	input.Position += float4(0, height * MaxHeight, 0, 0);

	float4 worldPosition = mul(float4(input.Position, 1), World);
	float4 viewPosition = mul(worldPosition, View);

	output.UV2 = uv;
	output.Position = mul(viewPosition, Projection);
	output.Depth = float3(output.Position.zw, viewPosition.z);
	output.UV = input.UV;
	//output.UV.y = 1 - output.UV.y;

	return output;
}

//Pixel Shader
PS_OUTPUT PSf(VS_OUTPUT input)
{
	PS_OUTPUT output;

	//half4 vDiffuseMap = Height.Sample(sampHeight, input.UV);
	//half4 vDiffuseMap = Textures[0].Sample(sampData, input.UV2);
	half4 vDiffuseMap = Textures[0].Sample(sampData, input.UV * Tiling);
	half4 vNormalMap = Textures[1].Sample(sampData, input.UV);
	half4 vDataMap = Textures[2].Sample(sampData, input.UV);

	float3 vNormal = GetNormal(input.UV);
	vNormal = mul(vNormal, (float3x3)View);

	output.Albedo = float4(vDiffuseMap.rgb, 1);
	output.Normals = float4(vNormal.xyz, 1);
	output.Data = float4(vDataMap.r, vDataMap.g, vDataMap.b, vDataMap.a);
	output.Depth = log2(1 * input.Depth.y) / log2(1 * 100000000);
	output.fDepth = output.Depth;

	return output;
}

//Pixel Shader
PS_OUTPUT PS(VS_OUTPUT input)
{
	PS_OUTPUT output;

	half4 vDiffuseMap = Textures[0].Sample(sampData, input.UV);
	half4 vNormalMap = Textures[1].Sample(sampData, input.UV);
	half4 vDataMap = Textures[2].Sample(sampData, input.UV);

	float3 vNormal = GetNormal(input.UV);

	half4 c1 = Splats[0].Sample(sampData, input.UV * Tiling);
	half4 c2 = Splats[1].Sample(sampData, input.UV * Tiling * 0.5f);
	//half4 c3 = Splats[2].Sample(sampData, input.UV * Tiling);

	float n = clamp(pow(vNormal.y, 2), 0, 1);

	//output.Albedo = float4(diffuse.rgb * pow(vNormal.y, 2), 1);
	output.Albedo = half4(blend(c1, n, c2, 1 - n), 1) * vDiffuseMap * 2;
	//half4(blend(float4(0,0,0,1), 0, diffuse, clamp(vNormal.y, 0.2f, 1)), 1);

	vNormal = mul(vNormal, (float3x3)View);

	output.Normals = float4(vNormal.xyz, 1);
	output.Data = float4(vDataMap.r, vDataMap.g, vDataMap.b, vDataMap.a);
	output.Depth = log2(1 * input.Depth.y) / log2(1 * 100000000);
	output.fDepth = output.Depth;

	return output;
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