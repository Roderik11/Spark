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

SamplerState linearSampler;

Texture2D Albedo <string Property="Albedo";>;

float3 LightDirection <string Property="LightDirection";>;
float3 LightColor <string Property="LightColor";>;
float3 MaterialColor <string Property="MaterialColor";>;

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
	float3 Normal		: NORMAL;
    float2 UV           : TEXCOORD0;
	float  Depth		: TEXCOORD1;
};

struct ShadowVertexOutput
{
    float4 Position : SV_POSITION;
    float2 UV       : TEXCOORD0;
    float Depth     : TEXCOORD1;
};

struct FragmentOutput
{
	float4 Color : SV_TARGET;
};

struct ShadowOutput
{
    float Depth : SV_DEPTH;
};


VertexOutput GetVertexOutput(VertexInput input, float4x4 worldMatrix)
{
    float4 worldPosition = mul(float4(input.Position, 1), worldMatrix);
    float4 viewPosition = mul(worldPosition, View);

    VertexOutput output;
    output.Position = mul(viewPosition, Projection);
    //output.Normal = input.Normal;
    float3x3 vwp = (float3x3) mul(worldMatrix, View);
    vwp = mul(vwp, (float3x3) Projection);
    
    output.Normal = normalize(mul(input.Normal, vwp));
    output.Depth = output.Position.z / output.Position.w;
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
    // Normalize the normal vector
    float3 normal = normalize(input.Normal);

    // Normalize the light direction vector
    float3 lightDir = normalize(-LightDirection);

    // Calculate the view direction vector (assuming camera at origin)
    float3 viewDir = normalize(CameraPosition-input.Position.xyz);

    // Calculate the half vector for specular reflection
    float3 halfVector = normalize(viewDir + lightDir);

    // Calculate the diffuse term
    float diffuseFactor = max(0.0, dot(normal, lightDir));

    // Calculate the specular term using the Blinn-Phong model
    float specularFactor = pow(max(0.0, dot(normal, halfVector)), 2);

    // Calculate the final color
    float3 ambientColor = 0.2 * LightColor;
    float3 diffuseColor = 0.7 * LightColor * diffuseFactor;
    float3 specularColor = 0.5 * LightColor * specularFactor;

    float4 diffuse = Albedo.Sample(linearSampler, input.UV);
    
    float3 finalColor = diffuse.rgb * (ambientColor + diffuseColor + specularColor);

    FragmentOutput output;
    output.Color = float4(finalColor, 1.0);
    //output.Color = float4(1,1,1, 1.0);

	return output;
}

ShadowOutput PS_Shadow(ShadowVertexOutput input)
{
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

RasterizerState DoubleSided
{
    CullMode = None;
};

technique11 Standard
{
    pass Opaque
    {
        SetRasterizerState(DoubleSided);
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