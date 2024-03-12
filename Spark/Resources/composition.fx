#include "common.fx"

Texture2D Textures[4];
Texture2D texReflection;
SamplerState sampData;

float3 CameraPosition;
float4x4 InverseViewProjection;
float4x4 matProjectionInverse;
float4x4 matViewInverse;
int Fog;

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

VertexOutput VS(VertexInput IN)
{
	VertexOutput OUT;

	OUT.vPosition = float4(IN.vPosition, 1);
	OUT.vUV = IN.vUV;
	OUT.vView = mul(OUT.vPosition, matProjectionInverse);

	return OUT;
}

float4 PS(VertexOutput IN) : SV_TARGET
{
	float4 OUT = 0;

	float4 vLighting = Textures[2].Sample(sampData, IN.vUV);
	float3 vDiffuse = 0;
	float3 vSpecular = 0;
	float3 vReflection = 0;

	float4 vData = Textures[1].Sample(sampData, IN.vUV);
	float3 Color = SampleLinear2D(Textures[0], sampData, IN.vUV);
    float sceneDepth = Textures[3].Sample(sampData, IN.vUV).r;
    //float3 Color = Textures[0].Sample(sampData, IN.vUV);

	// vData.r
	// matte <-> glossy
	// reflection high mip <-> reflection low mip

	// vData.g
	// plastic <-> metal
	// reflection additive <-> reflection multiplicative

	// vData.b
	// non reflective <-> reflective
	// reflection low intensity <-> reflection high intensity

	// vData.a
	// non-emissive <-> emissive
	// high usage of light <-> low usage of light
    float4 position = posFromDepth(IN.vUV, sceneDepth, InverseViewProjection);

	vLighting.a = saturate(vLighting.a);

	vDiffuse = lerp(Color * vLighting.rgb * (1 - vData.r), Color, vData.a);
	vSpecular = pow(vLighting.a, 10) * vLighting.rgb * vData.b;
	vSpecular = lerp(vSpecular, pow(vSpecular, 3) * Color, vData.g);

	vReflection = texReflection.Sample(sampData, IN.vUV).rgb * vData.b;
	vReflection = lerp(vReflection, vReflection * Color, vData.g);
    float3 result = vDiffuse + vSpecular;// + vReflection;

	if(Fog > 0)
		result = FOG(result, length(position));
	
    OUT.rgb = result;
	//OUT.rgb = Color;

	return pow(OUT, 1.0f / 2.2f);
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