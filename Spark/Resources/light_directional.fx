#include "common.fx"
#include "lighting.fx"

cbuffer scene
{
	float3   CameraPosition;
	float4x4 ProjectionInverse;
	float4x4 View;
	float4x4 Projection;
    float4x4 CameraInverse;
}

cbuffer object
{
	float3 LightColor;
	float3 LightDirection;
	float  LightIntensity;
    float4x4 LightSpaces[4];
    float2 Cascades[4];
}

Texture2D Textures[4];
Texture2DArray ShadowMap;
SamplerState sampData;

struct VertexInput
{
	float4 Position	: POSITION0;
	float4 Color	: COLOR0;
	float2 UV		: TEXCOORD0;
};

struct VertexOutput
{
	float4 Position	: SV_POSITION;
	float2 UV		: TEXCOORD0;
	float3 View		: TEXCOORD1;
	float3 Light	: TEXCOORD2;
};

float readShadowMap(float4 worldPos, float wdepth)
{
    uint sliceIndex = 4;

    float d = length(worldPos);
    //float d = linearize_depth(wdepth, 0.1f, 104096);
    //float d = lineardepth(wdepth);
    //float d = LinearizeDepth(wdepth, 0.1f, 104096);
	
    for (int i = 0; i < 4; i++)
    {
        if (d < Cascades[i].y)
        {
            sliceIndex = i;
            break;
        }
    }
	
    if (sliceIndex > 3)
        return 1;
	
    float4 lightViewPosition = mul(worldPos, LightSpaces[sliceIndex]);
	lightViewPosition /= lightViewPosition.w;

	float depth = lightViewPosition.z;

    if (lightViewPosition.x < -1.0f || lightViewPosition.x > 1.0f ||
		lightViewPosition.y < -1.0f || lightViewPosition.y > 1.0f ||
		lightViewPosition.z < 0.0f || lightViewPosition.z > 1.0f)
        return 1;

	float2 texCoords;
	texCoords.x =  lightViewPosition.x * 0.5f + 0.5f;
	texCoords.y = -lightViewPosition.y * 0.5f + 0.5f;

    float shadow = GetShadowFactor(ShadowMap, sampData, texCoords, depth, sliceIndex);
	return shadow;
}


VertexOutput VS(VertexInput IN)
{
	VertexOutput OUT = (VertexOutput)0;

	OUT.Position = IN.Position;
	OUT.UV = IN.UV;
	OUT.View = mul(IN.Position, ProjectionInverse);
	OUT.Light = mul(-LightDirection, (float3x3)View);

	return OUT;
}

float4 PS(VertexOutput IN, float4 pos: SV_Position) : SV_TARGET
{
	float4 normals = Textures[0].Sample(sampData, IN.UV);
	float4 depth = Textures[1].Sample(sampData, IN.UV);
	//float4 data = Textures[2].Sample(sampData, IN.UV);

	float3 normal = normalize(normals.xyz);
	float3 light = normalize(IN.Light);
	
	float4 position = posFromDepth(IN.UV, depth.r, CameraInverse);
    float shadow = readShadowMap(position, depth.r);

	float lightAmount = remap(shadow, 0, 1, 0.3f, LightIntensity);
	return float4(PhongLightingNoSpecular(normal, light, LightColor, lightAmount), 0);
	//return float4(dot(vLight, vNormal).xxx, 0);
	//return float4(1,1,1,0);
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