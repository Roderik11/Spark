#include "common.fx"
#define Near  4096.0f //64 * 64
#define Far  6400.0f //80 * 80

cbuffer scene
{
	float Time;
	float4x4 View;
	float4x4 Projection;
	float4x4 ViewProjection;
    float3 CameraPosition;
}

cbuffer object
{
	float4x4 World;
}

Texture2D Albedo <string Property="Albedo";>;
Texture2D Normal <string Property="Normal";>;
Texture2D Data	 <string Property="Data";>;
Texture2D Height <string Property="Height";>;
Texture2DArray ControlMaps <string Property="ControlMaps";>;

float MaxHeight <string Property="MaxHeight";>;

SamplerState sampData;
SamplerState sampHeight;

float3 Offset;
float2 MapSize;
float HeightTexel;

struct VertexInput
{
    float3 Position		: POSITION;
    float3 Size			: NORMAL;
    float4 Color		: COLOR;
    float4x4 Transform  : TRANSFORM;
};

struct VertexOutput
{
    float4 Position		: POSITION;
    float4 Color		: COLOR;
    float3 Size			: SIZE;
};

struct GeoOutput
{
	float4 Position		: SV_POSITION;
	float3 Normal		: NORMAL;
	float2 UV			: TEXCOORD0;
	float4 Color		: COLOR;
	float Depth			: TEXCOORD1;
};

struct FragmentOutput
{
	half4 Albedo		: SV_TARGET0;
	half4 Normals		: SV_TARGET1;
	float Depth			: SV_TARGET2;
    half4 Data			: SV_TARGET3;
};

float3 GetNormal(Texture2D tex, float2 uv)
{
    float4 h0 = tex.SampleLevel(sampHeight, uv + float2(0, -HeightTexel), 0).r;
    float4 h1 = tex.SampleLevel(sampHeight, uv + float2(-HeightTexel, 0), 0).r;
    float4 h2 = tex.SampleLevel(sampHeight, uv + float2(HeightTexel, 0), 0).r;
    float4 h3 = tex.SampleLevel(sampHeight, uv + float2(0, HeightTexel), 0).r;

	float3 n;
	n.z = h0 - h3;
	n.x = h1 - h2;
	n.y = 0.005f;

	return normalize(n);
}

float GetSteepness(Texture2D tex, float2 uv)
{
	float h = tex.SampleLevel(sampHeight, uv, 0).r;

	// Compute the differentials by stepping over 1 in both directions.
    float dx = tex.SampleLevel(sampHeight, uv + float2(HeightTexel, 0), 0).r - h;
    float dy = tex.SampleLevel(sampHeight, uv + float2(0, HeightTexel), 0).r - h;

	// The "steepness" is the magnitude of the gradient vector
	// For a faster but not as accurate computation, you can just use abs(dx) + abs(dy)
	return sqrt(dx * dx + dy * dy);
}

VertexOutput VS(VertexInput input)
{
    VertexOutput OUT;
    OUT.Position = mul(float4(input.Position, 1), World);
    OUT.Color = input.Color;
    OUT.Size = input.Size;
    return OUT;
}

VertexOutput VS_Instanced(VertexInput input, uint instanceID : SV_InstanceID)
{
    VertexOutput OUT;
    OUT.Position = mul(float4(input.Position, 1), input.Transform);
    OUT.Color = input.Color;
    OUT.Size = input.Size;
    return OUT;
}

[maxvertexcount(4)]
void GS_Quad(point VertexOutput vert[1], inout TriangleStream<GeoOutput> triStream)
{
    VertexOutput input = vert[0];

    float4 worldPosition = input.Position; // mul(float4(input.Position, 1), World);
    float2 uv = float2(worldPosition.x + Offset.x, worldPosition.z + Offset.z) / MapSize;
    float2 uv2 = uv;
    uv2.y = 1 - uv.y;

    //float4 weights0 = ControlMaps.SampleLevel(sampData, float3(uv, 0), 0);
    //float occl = weights0.x + weights0.y + weights0.z + weights0.w;
    float4 weights = ControlMaps.SampleLevel(sampData, float3(uv2, 1), 0);
	
    float3 normal = GetNormal(Height, uv);
    float angle = dot(normal, float3(0, 1, 0));
	//float slope = GetSteepness(Textures[3], uv);

    float limit = 0.66f;
    float grow = map(angle, limit, 1, 0, 1);

    grow = clamp(grow * 10, 0, 1);
	//grow = grow * grow;

    if (grow < 0.001f)
        return;

    float2 diff = worldPosition.xz - CameraPosition.xz;
    float distance = dot(diff, diff);

    float elevation = Height.SampleLevel(sampHeight, uv, 0).r;
    float l = map(elevation, 8.5f / MaxHeight, 10.5f / MaxHeight, 0, 1);

    grow *= map(distance, Near, Far, 1, 0);
    grow *= l;
    grow = saturate(weights.y);

    if (grow < 0.3f)
        return;
	
    GeoOutput v;

    worldPosition.y = elevation * MaxHeight - Offset.y;

    float2 gridpos = float2(input.Size.z % 2, floor(input.Size.z * 0.5f));
    uv = 0.5f * gridpos;
	//float4 viewPosition = mul(worldPosition, View);
	//float4 pos = mul(viewPosition, Projection);
    float4 pos = mul(worldPosition, ViewProjection);

    float width = input.Size.x * grow;
    float height = input.Size.y * grow;
    float halfwidth = width * 0.5f;

    float sway = 0; // sin(pos.x + Time * 1.723) * sin(pos.x + Time * 3.6372)  * halfwidth * 0.125f;
    float sway2 = 0; // cos(pos.z + Time * 2.129) * cos(pos.z + Time * 3.427) * halfwidth * 0.125f;

	//normal.y = clamp(normal.y - sway, -1, 1);

	//--------------------------------------------
	//quad
	//bottom left
    v.Position = pos + float4(-halfwidth, 0, 0, 0);
    v.Depth = v.Position.z / v.Position.w;
    v.UV = uv + float2(0, 1) * 0.5f;
    v.Normal = normal;
    v.Color = input.Color;
    triStream.Append(v);

	//top left
    v.Position = pos + float4(-halfwidth + sway, height, sway2, sway2);
    v.Depth = v.Position.z / v.Position.w;
    v.UV = uv + float2(0, 0);
    v.Normal = normal;
    v.Color = input.Color;
    triStream.Append(v);

	//bottom right
    v.Position = pos + float4(halfwidth, 0, 0, 0);
    v.Depth = v.Position.z / v.Position.w;
    v.UV = uv + float2(1, 1) * 0.5f;
    v.Normal = normal;
    v.Color = input.Color;
    triStream.Append(v);

	//top right
    v.Position = pos + float4(halfwidth + sway, height, sway2, sway2);
    v.Depth = v.Position.z / v.Position.w;
    v.UV = uv + float2(1, 0) * 0.5f;
    v.Normal = normal;
    v.Color = input.Color;
    triStream.Append(v);
}

FragmentOutput PS(GeoOutput input)
{
	half4 vDiffuseMap = Albedo.Sample(sampData, input.UV);

	clip(vDiffuseMap.a - 0.6f);

	half4 vNormalMap = Normal.Sample(sampData, input.UV);
    half4 vDataMap =  Data.Sample(sampData, input.UV);

	//float3 vNormal = normalize(mul(vNormalMap.xyz * 2 - 1, input.TBN));
	//float3 vNormal = mul(vNormalMap, (float3x3)View);

	float factor = 1;
	float3 norm = input.Normal;
	norm.xz *= factor;
	norm = normalize(norm);

	float3 vNormal = mul(norm, (float3x3)View);
														   
	FragmentOutput output;
    output.Albedo = float4(vDiffuseMap.rgb * input.Color.rgb, vDiffuseMap.a);
	output.Normals = float4(vNormal.xyz, 1);// vNormalMap.a);
	output.Data = float4(vDataMap.r, vDataMap.g, vDataMap.b, vDataMap.a);
	output.Depth = input.Depth;

	return output;
}

technique11 Standard
{
	pass Opaque
	{
		SetVertexShader(CompileShader(vs_5_0, VS()));
		SetGeometryShader(CompileShader(gs_5_0, GS_Quad()));
		SetPixelShader(CompileShader(ps_5_0, PS()));
	}
}

technique11 Instanced
{
    pass Opaque
    {
        SetVertexShader(CompileShader(vs_5_0, VS_Instanced()));
        SetGeometryShader(CompileShader(gs_5_0, GS_Quad()));
        SetPixelShader(CompileShader(ps_5_0, PS()));
    }
}