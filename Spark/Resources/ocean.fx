#include "common.fx"

cbuffer scene
{
    float4x4 View;
    float4x4 Projection;
    float3 CameraPosition;
}

cbuffer object
{
    float4x4 World;
}

SamplerState sampData;

Texture2D Albedo <string Property="Albedo";>;
Texture2D Normal <string Property="Normal";>;
Texture2D Data <string Property="Data";>;
float4 Color <string Property="Color";>;
int Fog;

struct VertexInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 UV : TEXCOORD0;
    float3 Tangent : TANGENT;
    float3 BiTangent : BINORMAL;
};

struct VertexOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float Depth : TEXCOORD1;
    float3 WorldPosition : TEXCOORD2;
    float3x3 TBN : TEXCOORD3;
};

struct FragmentOutput
{
    float4 Albedo : SV_TARGET0;
	//float4 Normals		: SV_TARGET1;
	//float3 Depth		: SV_TARGET2;
	//float4 Data			: SV_TARGET3;
};

VertexOutput VS(VertexInput input)
{
    VertexOutput output;

    float4 worldPosition = mul(float4(input.Position, 1), World);
    float4 viewPosition = mul(worldPosition, View);

    output.WorldPosition = worldPosition.xyz;
    output.Position = mul(viewPosition, Projection);
    output.Depth = output.Position.z / output.Position.w;
    output.TBN = mul(float3x3(input.Tangent, input.BiTangent, input.Normal), (float3x3) mul(World, View));
    output.UV = input.UV;
    output.UV.y = 1 - output.UV.y;
    return output;
}

FragmentOutput PS(VertexOutput input)
{
    FragmentOutput output;

    float4 vDiffuseMap = Albedo.Sample(sampData, input.UV);
    //float4 vNormalMap = Normal.Sample(sampData, input.UV);
    //float4 vDataMap = Data.Sample(sampData, input.UV);
	//float3 vNormal = normalize(mul(vNormalMap.xyz * 2 - 1, input.TBN));
	//clip(vDiffuseMap.a - 0.2f);
  
    float3 color = vDiffuseMap.rgb * Color.rgb;
    float depth = distance(input.WorldPosition, CameraPosition) + 0.1f;
    
    if (Fog > 0)
    {
        color = FOG(color, depth);
    }
    
    output.Albedo = float4(color, 1);
	//output.Normals = float4(vNormal.xyz, vNormalMap.a);
	//output.Data = float4(vDataMap.r, vDataMap.g, vDataMap.b, vDataMap.a);
	//output.Depth = input.Depth;

    return output;
}

technique11 Standard
{
    pass Transparent
    {
        SetVertexShader(CompileShader(vs_4_0, VS()));
        SetGeometryShader(NULL);
        SetPixelShader(CompileShader(ps_4_0, PS()));
    }
}