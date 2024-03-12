#include "common.fx"
#include "lighting.fx"

cbuffer scene
{
	float3   CameraPosition;
	float4x4 Projection;
	float4x4 View;
	float4x4 ViewInverse;
	float4x4 CameraInverse;
}

cbuffer object
{
	float4x4 World;
	float3 LightColor;
	float  LightIntensity;
	float  Range;
}

Texture2D Textures[3];
SamplerState sampData;

TextureCube texReflection;
SamplerState sampReflection;

struct VertexInput
{
	float4 vPosition	: POSITION0;
	float2 vUV			: TEXCOORD0;
};

struct VertexOutput
{
	float4 vPosition	: SV_POSITION;
	float2 vUV			: TEXCOORD0;
	float4 vScreen		: TEXCOORD1;
	float3 vLightWorld  : TEXCOORD2;
};

VertexOutput VS(VertexInput IN)
{
	VertexOutput OUT = (VertexOutput)0;

	float4 vWorld = mul(float4(IN.vPosition.xyz, 1.0f), World);
	float4 vView = mul(vWorld, View);

	OUT.vPosition = OUT.vScreen = mul(vView, Projection);
	OUT.vLightWorld = World[3].xyz;
	OUT.vUV = IN.vUV;

	return OUT;
}

float4 PS(VertexOutput IN) : SV_TARGET
{
	float4 vResult = float4(0, 0, 0, 0);

	float2 vUV = IN.vScreen.xy / IN.vScreen.w;
	vUV = 0.5f * (half2(vUV.x, -vUV.y) + 1);

	float4 vNormals = Textures[0].Sample(sampData, vUV);
	float4 vDepth = Textures[1].Sample(sampData, vUV);
	//float4 vData = Textures[2].Sample(sampData, vUV);

	float3 vPosition = posFromDepth(vUV, vDepth.r, CameraInverse);
	float3 vLight = IN.vLightWorld - vPosition;

	float atten = 1 - saturate(length(vLight) / Range);

	vLight = normalize(vLight);

	float3 vNormal = normalize(mul(vNormals.xyz, (float3x3)ViewInverse));

	//float NdotL = saturate(dot(vLight, vNormal));
	//return float4(LightColor * atten * atten * LightIntensity * NdotL, 0);
	return float4(PhongLightingNoSpecular(vNormal, vLight, LightColor * LightIntensity, atten), 0);
}

technique11 Standard
{
	pass Light
	{
		SetVertexShader(CompileShader(vs_4_0, VS()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PS()));
	}
}

//if(vPositionVS.z > IN.vView.z || vPositionVS.z >= 0.999f)
//{
//	float3 vV = IN.vWorld.xyz - CameraPosition;
//	float3 vL = IN.vLightWorld - CameraPosition;

//	float fVdotV = dot(vV, vV);
//	float fVdotL = dot(vV, vL);
//	float fVolumetricAttenuation = saturate(fVdotL/fVdotV);
//	float3 fVolumetricAttenuationVector = (fVolumetricAttenuation * vV) - vL;
//	fVolumetricAttenuation = dot(fVolumetricAttenuationVector, fVolumetricAttenuationVector);
//	//fVolumetricAttenuation = 1.0f /( (fVolumetricAttenuation * 10.0f) + 1.0f );
//	fVolumetricAttenuation = 1 - saturate(fVolumetricAttenuation / (IN.fRange * 4));

//	vResult = float4(LightColor * fVolumetricAttenuation * 0.01f, 0);
//}