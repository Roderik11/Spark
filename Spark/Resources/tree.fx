#include "common.fx"

cbuffer scene
{
	float Time;
	float4x4 View;
	float4x4 Projection;
	float3 CameraPosition;
}

cbuffer object
{
	float4x4 World;
}

Texture2D Albedo <string Property="Albedo";>;
Texture2D Normal <string Property="Normal";>;
Texture2D Data   <string Property="Data";>;

SamplerState sampData;
StructuredBuffer<float4x4> matrixBuffer;

struct VertexInput
{
	float3 Position		: POSITION;
	float3 Normal		: NORMAL;
	float2 UV			: TEXCOORD0;
	float3 Tangent		: TANGENT;
	float3 BiTangent	: BINORMAL;
	float4x4 Transform  : TRANSFORM;
};

struct VertexOutput
{
	float4 Position		: SV_POSITION;
	float2 UV			: TEXCOORD0;
	float  Depth		: TEXCOORD1;
    float3x3 TBN        : TRANSFORM;
};

struct FragmentOutput
{
	half4 Albedo		: SV_TARGET0;
	half4 Normals		: SV_TARGET1;
	float Depth			: SV_TARGET2;
    half4 Data			: SV_TARGET3;
};

struct ShadowVertexInput
{
    float3 Position : POSITION;
    float2 UV : TEXCOORD0;
    float4x4 Transform : TRANSFORM;
};

struct ShadowVertexOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float Depth : TEXCOORD1;
};

struct ShadowOutput
{
    float Depth : SV_DEPTH;
};

float rnd(float value)
{
    return frac(value * 12.7326) * .1f;
}

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

void WindTransformation(inout float3 position, float4x4 worldMatrix)
{
    float3 pivot = float3(worldMatrix[0][0] * 12, 0, worldMatrix[0][2] * 17);
    float sway = sin(pivot.x + Time * 0.73) * cos(pivot.x + Time * 1.37) * pow(position.y * 0.04f, 2) * .13f;
    float sway2 = sin(pivot.z + Time * 1.37) * cos(pivot.z + Time * 0.73) * pow(position.y * 0.04f, 2) * .13f;
    float3 dir = float3(sway, 0, sway2);
    float diff = dot(normalize(dir), normalize(position));
    position += float4(sway, length(dir) * -diff - 3.5f, sway2, 0);
}

VertexOutput VS(VertexInput input, uint instanceID : SV_InstanceID)
{
    WindTransformation(input.Position, World);
    return GetVertexOutput(input, World);
}

ShadowVertexOutput VS_Shadow(ShadowVertexInput input)
{
    WindTransformation(input.Position, World);
    return GetVertexOutputShadow(input, World);
}

VertexOutput VS_Instanced(VertexInput input, uint instanceID : SV_InstanceID)
{
    WindTransformation(input.Position, input.Transform);
    return GetVertexOutput(input, input.Transform);
}

ShadowVertexOutput VS_Instanced_Shadow(ShadowVertexInput input, uint instanceID : SV_InstanceID)
{
    WindTransformation(input.Position, input.Transform);
    return GetVertexOutputShadow(input, input.Transform);
}


FragmentOutput PS(VertexOutput input, bool front : SV_IsFrontFace)
{
    float4 diffuse = Albedo.Sample(sampData, input.UV);
    float4 normalMap = Normal.Sample(sampData, input.UV);
	float4 data = Data.Sample(sampData, input.UV);

	float3 normal = normalize(mul(normalMap.xyz * 2 - 1, input.TBN));
	normal = lerp(-normal, normal, front);

    clip(diffuse.a - 0.2f);

    FragmentOutput output;
	output.Albedo =  float4(diffuse.rgb, 1);
	output.Normals = float4(normal.xyz, normalMap.a);
	output.Data = float4(data.r, data.g, data.b, data.a);
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
        //SetRasterizerState(BackCull);
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
