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
}

cbuffer object
{
	float4x4 World;
}

float4 Rect;
float3 CameraPos;
float2 Tiling;
float HeightTexel;
int Level;
float2 LayerTiling[32];

Texture2D Albedo <string Property="Albedo";>;
Texture2D Normal <string Property="Normal";>;
Texture2D Data	 <string Property="Data";>;
Texture2D Height <string Property="Height";>;

//Texture2D Splatmap <string Property="Splatmap";>;
//Texture2D Indexmap <string Property="Indexmap";>;

Texture2DArray DiffuseMaps <string Property="DiffuseMaps";>;
Texture2DArray NormalMaps  <string Property="NormalMaps";>;
Texture2DArray ControlMaps <string Property="ControlMaps";>;

float MaxHeight <string Property="MaxHeight";>;

SamplerState sampIndex;
SamplerState sampData;
SamplerState sampHeight;
SamplerState sampHeightFilter;

StructuredBuffer<TerrainPatch> terrainData;

struct VertexInput
{
	float3 Position		: POSITION;
	float3 Normal		: NORMAL;
	float2 UV			: TEXCOORD0;
	float3 Tangent		: TANGENT;
	float3 BiTangent	: BINORMAL;
};

struct VertexOutput
{
	float4 Position		: SV_POSITION;
	float2 UV			: TEXCOORD0;
	float  Depth		: TEXCOORD1;
	float2 UV2			: TEXCOORD2;
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

	float3 n;
	n.z = h[0] - h[3];
	n.x = h[1] - h[2];
	n.y = 0.005f;// / pow(2, Level - 1);

	return normalize(n);
}

float GetHeight(float2 uv)
{
	float4 h;
    h[0] = Height.SampleLevel(sampHeightFilter, uv + float2(0, -HeightTexel), 0).r;
    h[1] = Height.SampleLevel(sampHeightFilter, uv + float2(-HeightTexel, 0), 0).r;
    h[2] = Height.SampleLevel(sampHeightFilter, uv + float2(HeightTexel, 0), 0).r;
    h[3] = Height.SampleLevel(sampHeightFilter, uv + float2(0, HeightTexel), 0).r;

	return (h[0] + h[1] + h[2] + h[3]) * 0.25f;
}

float3 textureNoTile(Texture2D tex, float2 uv, float v)
{
	float k = noise(0.5*uv);//  texture(iChannel1, 0.005*x).x; // cheap (cache friendly) lookup

	float2 duvdx = ddx(uv);
	float2 duvdy = ddy(uv);

	float l = k * 8.0;
	float i = floor(l);
	float f = frac(l);

	float2 offa = sin(float2(3.0, 7.0)*(i + 0.0)); // can replace with any other hash
	float2 offb = sin(float2(3.0, 7.0)*(i + 1.0)); // can replace with any other hash

	float3 cola = tex.SampleGrad(sampData, uv + v * offa, duvdx, duvdy); //textureGrad(iChannel0, x + v * offa, duvdx, duvdy).xyz;
	float3 colb = tex.SampleGrad(sampData, uv + v * offb, duvdx, duvdy); //textureGrad(iChannel0, x + v * offb, duvdx, duvdy).xyz;

	return lerp(cola, colb, smoothstep(0.2, 0.8, f - 0.1*sum(cola - colb)));
}

float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal)
{
	float steepness = wave.z;
	float wavelength = wave.w;
	float k = 2 * 3.141592f / wavelength;
	float c = sqrt(9.8 / k);
	float2 d = normalize(wave.xy);
	float f = k * (dot(d, p.xz) - c * Time);
	float a = steepness / k;

	//p.x += d.x * (a * cos(f));
	//p.y = a * sin(f);
	//p.z += d.y * (a * cos(f));

	//float steepnessSinF = steepness * sin(f);
	//float steepnessCosF = steepness * cos(f);
	//float aCosF = a * cos(f);

	tangent += float3(
		-d.x * d.x * (steepness * sin(f)),
		d.x * (steepness * cos(f)),
		-d.x * d.y * (steepness * sin(f))
		);
	binormal += float3(
		-d.x * d.y * (steepness * sin(f)),
		d.y * (steepness * cos(f)),
		-d.y * d.y * (steepness * sin(f))
		);
	return float3(
		d.x * (a * cos(f)),
		a * sin(f),
		d.y * (a * cos(f))
		);
}

static const float2 mesh_dim = float2(32, 32);
static const float minLodDistance = 31;

float getMorphValue(float dist)
{
    float low = 0.0;
    if (Level != 0)
    {
        low = minLodDistance * pow(2, Level - 1);
    }
	
    float high = minLodDistance * pow(2, Level);
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

	TerrainPatch patch = terrainData[instanceID];

	float4 rect = patch.rect;
	float2 uv = float2(rect.x + input.UV.x * (rect.z - rect.x), rect.y + (1 - input.UV.y) * (rect.w - rect.y));
    float height = Height.SampleLevel(sampHeightFilter, uv, 0).r;

	input.Position += float4(0, height * MaxHeight, 0, 0);

	float4x4 InstanceWorld = patch.transform;// mul(transpose(input.Transform), World);
	float4 worldPosition = mul(float4(input.Position, 1), InstanceWorld);

	//float3 gridPoint = worldPosition.xyz;
	//float3 tangent = float3(1, 0, 0);
	//float3 binormal = float3(0, 0, 1);
	//float3 p = gridPoint;
	//p += GerstnerWave(float4(1, 0, 0.5, 10), gridPoint, tangent, binormal);
	//worldPosition.xyz = p;

	float4 viewPosition = mul(worldPosition, View);

	output.Position = mul(viewPosition, Projection);
	output.Depth = output.Position.z / output.Position.w;// float3(output.Position.zw, viewPosition.z);
	output.UV = input.UV;
	output.UV.y = 1 - output.UV.y;
	output.UV2 = uv;
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
            int layer = startIndex + j;
            float2 tiling = 1700 / LayerTiling[layer];
			
            if (weights[j] <= 0)
                continue;

            float2 texuv = uv * tiling;
			float4 c = DiffuseMaps.Sample(sampData, float3(texuv, layer));
            float4 n = NormalMaps.Sample(sampData, float3(texuv, layer));
			
            color += c * weights[j];
            normal += n * weights[j];
        }
    }
	
    normal = float4(normal.xzy, 1);
    normal.x = normal.x * 2 - 1;
    normal.z = normal.z * 2 - 1;
    normal = normalize(normal);

    vNormal = blend_linear(vNormal, normal.xyz);
    vNormal = mul(vNormal, (float3x3) View);

    output.Albedo = float4(color.rgb * 2.2f , 1);
    output.Normals = float4(vNormal.xyz, 1);
    output.Data = float4(vDataMap.r, vDataMap.g, vDataMap.b, vDataMap.a);
    output.Depth = input.Depth;

    return output;
}

//FragmentOutput PS(VertexOutput input)
//{
//    FragmentOutput output;

//    float height = Height.SampleLevel(sampHeightFilter, input.UV2, 0).r;
//    float3 vNormal = GetNormal(input.UV2);
//    float4 vDataMap = Data.Sample(sampData, input.UV);

//    float4 indices = Indexmap.Sample(sampData, input.UV2, 0) * 32.0f;
//    float4 weights = Splatmap.Sample(sampData, input.UV2, 0);
	
//    float2 tiling = Tiling * pow(2, input.Level);
	
//    float4 color1 = SplatDiffuse.Sample(sampData, float3(input.UV * tiling, indices.x));
//    float4 color2 = SplatDiffuse.Sample(sampData, float3(input.UV * tiling, indices.y));
//    float4 color3 = SplatDiffuse.Sample(sampData, float3(input.UV * tiling, indices.z));
//    float4 color4 = SplatDiffuse.Sample(sampData, float3(input.UV * tiling, indices.w));

//    float4 normal1 = SplatNormal.Sample(sampData, float3(input.UV * tiling, indices.x));
//    float4 normal2 = SplatNormal.Sample(sampData, float3(input.UV * tiling, indices.y));
//    float4 normal3 = SplatNormal.Sample(sampData, float3(input.UV * tiling, indices.z));
//    float4 normal4 = SplatNormal.Sample(sampData, float3(input.UV * tiling, indices.w));
	
//    float4 color = color1 * weights.x + color2 * weights.y + color3 * weights.z + color4 * weights.w;;
//    float4 normal = normal1 * weights.x + normal2 * weights.y + normal3 * weights.z + normal4 * weights.w;;
	
//    normal = float4(normal.xzy, 1);
//    normal.x = normal.x * 2 - 1;
//    normal.z = normal.z * 2 - 1;
//    normal = normalize(normal);

//    vNormal = blend_linear(vNormal, normal.xyz);
//    vNormal = mul(vNormal, (float3x3) View);

//    output.Albedo = float4(color.rgb, 1);
//    output.Normals = float4(vNormal.xyz, 1);
//    output.Data = float4(vDataMap.r, vDataMap.g, vDataMap.b, vDataMap.a);
//    output.Depth = input.Depth;

//    return output;
//}

//FragmentOutput PS(VertexOutput input)
//{
//	FragmentOutput output;

//	//half4 vDiffuseMap = Albedo.Sample(sampData, input.UV2);
//	float4 vDataMap = Data.Sample(sampData, input.UV);

//    //float3 vNormal = Normal.SampleLevel(sampHeight, input.UV2, 0).xyz;
//    float3 vNormal = GetNormal(input.UV2);
//    float height = Height.SampleLevel(sampHeightFilter, input.UV2, 0).r;

//	float2 tiling = Tiling * pow(2, input.Level);
//    float4 c1 = SplatDiffuse.Sample(sampData, float3(input.UV * tiling, 0));
//    float4 c2 = SplatDiffuse.Sample(sampData, float3(input.UV * tiling * 0.5f, 1));
//    float4 c3 = SplatDiffuse.Sample(sampData, float3(input.UV * tiling, 3));

//    float4 n1 = SplatNormal.Sample(sampData, float3(input.UV * tiling, 0));
//    float4 n2 = SplatNormal.Sample(sampData, float3(input.UV * tiling * 0.5f, 1));
//    float4 n3 = SplatNormal.Sample(sampData, float3(input.UV * tiling, 3));

//	float n = clamp(pow(vNormal.y, 2), 0, 1);
//    float l = map(height, 7.0f / MaxHeight, 13.0f / MaxHeight, 0, 1);

//    float4 color = float4(blend(c1, n, c2, 1 - n), 1);
//    color = float4(blend(color, l, c3, 1 - l), 1);

//    float4 normal = float4(blend(n1, n, n2, 1 - n), 1);
//    normal = float4(blend(normal, l, n3, 1 - l), 1);

//    normal = float4(normal.xzy, 1);
//	normal.x = normal.x * 2 - 1;
//	normal.z = normal.z * 2 - 1;
//	normal = normalize(normal);

//	vNormal = blend_linear(vNormal, normal.xyz);
//	vNormal = mul(vNormal, (float3x3)View);

//	output.Albedo = float4(color.rgb, 1);
//	output.Normals = float4(vNormal.xyz, 1);
//	output.Data = float4(vDataMap.r, vDataMap.g, vDataMap.b, vDataMap.a);
//	output.Depth = input.Depth;

//	return output;
//}

technique11 Standard
{
	pass Opaque
	{
		SetVertexShader(CompileShader(vs_5_0, VS()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_5_0, PS()));
	}
}
