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

cbuffer cbAnimMatrices
{
	float4x4 Bones[64];
};

Texture2D Albedo <string Property="Albedo";>;
Texture2D Specular <string Property="Specular";>;
Texture2D Normal <string Property="Normal";>;
Texture2D Height <string Property="Height";>;
Texture2D Occlusion <string Property="Occlusion";>;
Texture2D Emissive <string Property="Emissive";>;
Texture2D Data <string Property="Data";>;

SamplerState sampData;
StructuredBuffer<float4x4> matrixBuffer;

struct VertexInput
{
	float3 Position		: POSITION;
	float3 Normal		: NORMAL;
	float2 UV			: TEXCOORD0;
	float3 Tangent		: TANGENT;
	float3 BiTangent	: BINORMAL;

	uint4 BoneIDs		: BONEIDS;
	float4 Weights		: BONEWEIGHTS;

	float4x4 Transform  : TRANSFORM;
};

struct ShadowVertexInput
{
    float3 Position : POSITION;
    float2 UV : TEXCOORD0;

    uint4 BoneIDs : BONEIDS;
    float4 Weights : BONEWEIGHTS;

    float4x4 Transform : TRANSFORM;
};

struct VertexOutput
{
	float4 Position		: SV_POSITION;
	float2 UV			: TEXCOORD0;
	float  Depth		: TEXCOORD1;
	float3x3 TBN		: TEXCOORD2;
};

struct ShadowVertexOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float Depth : TEXCOORD1;
};

struct FragmentOutput
{
    float4 Albedo   : SV_TARGET0;
	float4 Normals	: SV_TARGET1;
	float  Depth	: SV_TARGET2;
    float4 Data		: SV_TARGET3;
};

struct ShadowOutput
{
    float Depth : SV_DEPTH;
};


// Albedo		float4
// Normal		float3
// Emissive		float3
// Height		float
// Smoothness	float
// Metalness	float
// AO			float

VertexOutput GetVertexOutput(VertexInput input, float4x4 worldMatrix)
{
    float4 worldPosition = mul(float4(input.Position, 1), worldMatrix);
    float4 viewPosition = mul(worldPosition, View);

    VertexOutput output;
    output.Position = mul(viewPosition, Projection);
    output.Depth = output.Position.z / output.Position.w;
    output.TBN = mul(float3x3(input.Tangent, input.BiTangent, input.Normal), (float3x3) mul(worldMatrix, View));
    output.UV = input.UV;
    output.UV.y = 1 - output.UV.y;
    return output;
}

ShadowVertexOutput GetVertexOutputShadow(ShadowVertexInput input, float4x4 worldMatrix)
{
    float4 worldPosition = mul(float4(input.Position, 1), worldMatrix) - float4(CameraPosition.xyz, 0);
    float4 viewPosition = mul(worldPosition, View);

    ShadowVertexOutput output;
    output.Position = mul(viewPosition, Projection);
    output.Depth = output.Position.z / output.Position.w;
    output.UV = input.UV;
    output.UV.y = 1 - output.UV.y;
    return output;
}

void BoneTransformation(inout VertexInput input)
{
    float4 pos = float4(input.Position, 1);
    float4 skinned = float4(0, 0, 0, 0);
    skinned += mul(pos, Bones[input.BoneIDs.x]) * input.Weights.x;
    skinned += mul(pos, Bones[input.BoneIDs.y]) * input.Weights.y;
    skinned += mul(pos, Bones[input.BoneIDs.z]) * input.Weights.z;
    skinned += mul(pos, Bones[input.BoneIDs.w]) * input.Weights.w;
    input.Position = skinned.xyz;
}

void BoneTransformation(inout ShadowVertexInput input)
{
    float4 pos = float4(input.Position, 1);
    float4 skinned = float4(0, 0, 0, 0);
    skinned += mul(pos, Bones[input.BoneIDs.x]) * input.Weights.x;
    skinned += mul(pos, Bones[input.BoneIDs.y]) * input.Weights.y;
    skinned += mul(pos, Bones[input.BoneIDs.z]) * input.Weights.z;
    skinned += mul(pos, Bones[input.BoneIDs.w]) * input.Weights.w;
    input.Position = skinned.xyz;
}

VertexOutput VS(VertexInput input)
{
    return GetVertexOutput(input, World);
}

ShadowVertexOutput VS_Shadow(ShadowVertexInput input)
{
    return GetVertexOutputShadow(input, World);
}

VertexOutput VS_Instanced(VertexInput input, uint instanceID : SV_InstanceID)
{
    return GetVertexOutput(input, input.Transform);
}

ShadowVertexOutput VS_Instanced_Shadow(ShadowVertexInput input, uint instanceID : SV_InstanceID)
{
    return GetVertexOutputShadow(input, input.Transform);
}

VertexOutput VS_Skinned(VertexInput input)
{
    BoneTransformation(input);
	return GetVertexOutput(input, World);
}

ShadowVertexOutput VS_Skinned_Shadow(ShadowVertexInput input)
{
    BoneTransformation(input);
    return GetVertexOutputShadow(input, World);
}

VertexOutput VS_Skinned_Instanced(VertexInput input, in uint instanceID : SV_InstanceID)
{
	BoneTransformation(input);
    return GetVertexOutput(input, input.Transform);
}

ShadowVertexOutput VS_Skinned_Instanced_Shadow(ShadowVertexInput input, in uint instanceID : SV_InstanceID)
{
    BoneTransformation(input);
    return GetVertexOutputShadow(input, input.Transform);
}

FragmentOutput PS(VertexOutput input)
{
	float4 vDiffuseMap = Albedo.Sample(sampData, input.UV);
	float4 aoc = Occlusion.Sample(sampData, input.UV);
	
	clip(vDiffuseMap.a - 0.2f);
	
    float4 vNormalMap = Normal.Sample(sampData, input.UV);
    float4 vDataMap = Data.Sample(sampData, input.UV);

	float3 vNormal = normalize(mul(vNormalMap.xyz * 2 - 1, input.TBN));

    FragmentOutput output;
	output.Albedo = float4(vDiffuseMap.rgb * aoc.r, 1);
	output.Normals = float4(vNormal.xyz, vNormalMap.a);
	output.Data = float4(vDataMap.r, vDataMap.g, vDataMap.b, vDataMap.a);
	output.Depth = input.Depth;

	return output;
}

ShadowOutput PS_Shadow(ShadowVertexOutput input)
{
    float alpha = Albedo.Sample(sampData, input.UV).a;

    clip(alpha - 0.2f);

    ShadowOutput output;
    output.Depth = input.Depth;

    return output;
}


RasterizerState FrontCull
{
    CullMode = Front;
    DepthClipEnable = FALSE;
};

RasterizerState BackCull
{
    CullMode = Back;
};

technique11 Standard
{
    pass Opaque
    {
        SetRasterizerState(BackCull);
        SetVertexShader(CompileShader(vs_5_0, VS()));
        SetGeometryShader(NULL);
        SetPixelShader(CompileShader(ps_5_0, PS()));
    }

    pass Shadow
    {
        //SetRasterizerState(FrontCull);
        SetVertexShader(CompileShader(vs_5_0, VS_Shadow()));
        SetGeometryShader(NULL);
        SetPixelShader(CompileShader(ps_5_0, PS_Shadow()));
    }
}

technique11 Instanced
{
    pass Opaque
    {
        //SetRasterizerState(BackCull);
        SetVertexShader(CompileShader(vs_5_0, VS_Instanced()));
        SetGeometryShader(NULL);
        SetPixelShader(CompileShader(ps_5_0, PS()));
    }

    pass Shadow
    {
        //SetRasterizerState(FrontCull);
        SetVertexShader(CompileShader(vs_5_0, VS_Instanced_Shadow()));
        SetGeometryShader(NULL);
        SetPixelShader(CompileShader(ps_5_0, PS_Shadow()));
    }
}

technique11 Skinned
{
	pass Opaque
	{
        SetRasterizerState(BackCull);
        SetVertexShader(CompileShader(vs_5_0, VS_Skinned()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_5_0, PS()));
	}

    pass Shadow
    {
        SetRasterizerState(FrontCull);
        SetVertexShader(CompileShader(vs_5_0, VS_Skinned_Shadow()));
        SetGeometryShader(NULL);
        SetPixelShader(CompileShader(ps_5_0, PS_Shadow()));
    }

}