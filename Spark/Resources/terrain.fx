#include "common.fx"

struct TerrainPatch
{
	float4x4 transform;
	float4 rect;
	float2 level;
};

cbuffer scene
{
	float Time;
	float4x4 View;
	float4x4 Projection;
    float4x4 ViewProjection;
}

cbuffer terrain
{
    float3 CameraPos;
    float HeightTexel;
    float2 LayerTiling[32];
}

Texture2D Albedo <string Property="Albedo";>;
Texture2D Normal <string Property="Normal";>;
Texture2D Data	 <string Property="Data";>;
Texture2D Height <string Property="Height";>;

Texture2DArray DiffuseMaps <string Property="DiffuseMaps";>;
Texture2DArray NormalMaps  <string Property="NormalMaps";>;
Texture2DArray ControlMaps <string Property="ControlMaps";>;

float MaxHeight <string Property="MaxHeight";>;

SamplerState sampData;
SamplerState sampHeight;
SamplerState sampHeightFilter;

StructuredBuffer<TerrainPatch> terrainData;

struct VertexInput
{
  	float3 Position		: POSITION;
};

struct VertexOutput
{
	float4 Position		: SV_POSITION;
	float2 UV			: TEXCOORD0;
	float2 UV2			: TEXCOORD1;
    float Depth			: TEXCOORD2;
	float Level			: TEXCOORD3;
};

struct FragmentOutput
{
	float4 Albedo		: SV_TARGET0;
	float4 Normals		: SV_TARGET1;
	float  Depth		: SV_TARGET2;
	float4 Data			: SV_TARGET3;
};

float3 GetNormal(float2 uv)
{
	float4 h;
	
    h[0] = Height.SampleLevel(sampHeightFilter, uv + float2(0, -HeightTexel), 0).r;
    h[1] = Height.SampleLevel(sampHeightFilter, uv + float2(-HeightTexel, 0), 0).r;
    h[2] = Height.SampleLevel(sampHeightFilter, uv + float2(HeightTexel, 0), 0).r;
    h[3] = Height.SampleLevel(sampHeightFilter, uv + float2(0, HeightTexel), 0).r;

    //h = Height.Gather(sampHeightFilter, uv + float2(0.5f, 0.5f) * HeightTexel);
	
	float3 n;
	n.z = h[0] - h[3];
	n.x = h[1] - h[2];
	n.y = 0.005f;// / pow(2, Level - 1);

	return normalize(n);
}


static const float2 mesh_dim = float2(32, 32);
static const float minLodDistance = 31;

float getMorphValue(float dist, int level)
{
    float low = 0.0;
	
    if (level != 0)
    {
        low = minLodDistance * pow(2, level - 1);
    }
	
    float high = minLodDistance * pow(2, level);
    float delta = high - low;
    float factor = (dist - low) / delta;
    return clamp(factor / 0.5 - 1.0, 0.0, 1.0);
}

float2 morphVertex(float2 vertex, float morphValue)
{
    float2 fraction = frac(vertex * mesh_dim * 0.5) * 2.0 / mesh_dim;
    return vertex - fraction * morphValue;
}


VertexOutput VS(VertexInput input, uint instanceID : SV_InstanceID)
{
	VertexOutput output;

    float2 uv = (input.Position.xz + float2(16, 16)) * 0.03125f; // 1/32;
    uv.y = 1 - uv.y;
	
	TerrainPatch patch = terrainData[instanceID];
	float4 rect = patch.rect;
	float2 heightUV = rect.xy + uv * float2(rect.z - rect.x, rect.w - rect.y);
    float height = Height.SampleLevel(sampHeightFilter, heightUV, 0).r;

    float4 worldPosition = mul(float4(input.Position, 1), patch.transform);
    worldPosition.y += height * MaxHeight;
    worldPosition = mul(worldPosition, ViewProjection);
    
    output.Position = worldPosition;
    output.Depth = worldPosition.z / worldPosition.w;
    output.UV = uv;
    output.UV2 = heightUV;
	output.Level = patch.level.x;
	return output;
}

FragmentOutput PS(VertexOutput input)
{
    FragmentOutput output;

    float height = Height.SampleLevel(sampHeightFilter, input.UV2, 0).r;
    float3 vNormal = GetNormal(input.UV2);
    float4 vDataMap = Data.Sample(sampData, input.UV);

    float4 color;
    float4 normal;
	
    float2 uv = input.UV2;
    uv.y = 1 - uv.y;
	
    for (int i = 0; i < 4; ++i)
    {
		int startIndex = i * 4;
		
		float4 weights = ControlMaps.Sample(sampData, float3(uv, i));

		for (int j = 0; j < 4; ++j)
        {		
            if (weights[j] <= 0)
                continue;

            int layer = startIndex + j;
            float2 texuv = uv * LayerTiling[layer];
			
			float4 c = DiffuseMaps.Sample(sampData, float3(texuv, layer));
            float4 n = NormalMaps.Sample(sampData, float3(texuv, layer));
			
            color += c * weights[j];
            normal += n * weights[j];
        }
    }
	
    normal = float4(normal.xzy, 1);
    normal.xz = normal.xz * 2 - 1;
    normal = normalize(normal);

    vNormal = blend_linear(vNormal, normal.xyz);
    vNormal = mul(vNormal, (float3x3) View);

    output.Albedo = float4(color.rgb , 1);
    output.Normals = float4(vNormal.xyz, 1);
    output.Data = float4(vDataMap.r, vDataMap.g, vDataMap.b, vDataMap.a);
    output.Depth = input.Depth;

    return output;
}


technique11 Standard
{
	pass Opaque
	{
		SetVertexShader(CompileShader(vs_5_0, VS()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_5_0, PS()));
	}
}
