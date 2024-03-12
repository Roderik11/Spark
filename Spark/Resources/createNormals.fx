#include "common.fx"

Texture2D MainTexture;
SamplerState sampData;
float HeightTexel;

float4 GetNormalAndSteepness(Texture2D tex, float2 uv)
{
    float4 h;
    h[0] = tex.SampleLevel(sampData, uv + float2(0, -HeightTexel), 0).r;
    h[1] = tex.SampleLevel(sampData, uv + float2(-HeightTexel, 0), 0).r;
    h[2] = tex.SampleLevel(sampData, uv + float2(HeightTexel, 0), 0).r;
    h[3] = tex.SampleLevel(sampData, uv + float2(0, HeightTexel), 0).r;

    float height = tex.SampleLevel(sampData, uv, 0).r;

	// Compute the differentials by stepping over 1 in both directions.
    float dx = h[2] - height;
    float dy = h[3] - height;

    float3 n;
    n.z = h[0] - h[3];
    n.x = h[1] - h[2];
    n.y = 0.005f;
    n = normalize(n);
    
    return float4(n.x, n.y, n.z, sqrt(dx * dx + dy * dy));
}

//float GetSteepness(Texture2D tex, float2 uv)
//{
//    float h = tex.SampleLevel(sampData, uv, 0).r;

//	// Compute the differentials by stepping over 1 in both directions.
//    float dx = tex.SampleLevel(sampData, uv + float2(HeightTexel, 0), 0).r - h;
//    float dy = tex.SampleLevel(sampData, uv + float2(0, HeightTexel), 0).r - h;

//	// The "steepness" is the magnitude of the gradient vector
//	// For a faster but not as accurate computation, you can just use abs(dx) + abs(dy)
//    return sqrt(dx * dx + dy * dy);
//}

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
    return GetNormalAndSteepness(MainTexture, In.UV);
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
