#include "common.fx"

cbuffer object
{
	float InvLogZ;
}

Texture2D Textures[4];
SamplerState sampPoint;
SamplerState sampBilinear;

struct VS_INPUT
{
	float3 Position	: POSITION0;
	float2 UV			: TEXCOORD0;
};

struct VS_OUTPUT
{
	float4 Position	: SV_POSITION;
	float2 UV			: TEXCOORD0;
};

VS_OUTPUT VS(VS_INPUT IN)
{
	VS_OUTPUT OUT;

	OUT.Position = float4(IN.Position, 1);
	OUT.UV = IN.UV;

	return OUT;
}

float reconstruct_depth(float depth)
{
	return pow(2.0f, depth * InvLogZ) - 1.0f;
}

float4 PS(VS_OUTPUT In) : SV_TARGET
{
	const float total_strength = 0.75;
	const float base = 0.2;

	const float area = 0.0075;
	const float falloff = 0.001;

	const float radius = 0.002;

	const int samples = 16;
	float3 sample_sphere[samples] = {
		float3(0.5381, 0.1856,-0.4319), float3(0.1379, 0.2486, 0.4430),
		float3(0.3371, 0.5679,-0.0057), float3(-0.6999,-0.0451,-0.0019),
		float3(0.0689,-0.1598,-0.8547), float3(0.0560, 0.0069,-0.1843),
		float3(-0.0146, 0.1402, 0.0762), float3(0.0100,-0.1924,-0.0344),
		float3(-0.3577,-0.5301,-0.4358), float3(-0.3169, 0.1063, 0.0158),
		float3(0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
		float3(0.7119,-0.0154,-0.0918), float3(-0.0533, 0.0596,-0.5411),
		float3(0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847,-0.0271)
	};

	float3 random =  normalize(Textures[2].SampleLevel(sampBilinear, In.UV * 8, 0).rgb);

	float4 color = Textures[3].SampleLevel(sampPoint, In.UV, 0);
	float depth = Textures[0].SampleLevel(sampPoint, In.UV, 0).r;

	//depth *= map(depth, 0, 1.0f, 1, 0);

	float3 position = float3(In.UV, depth);
	float3 normal = Textures[1].SampleLevel(sampPoint, In.UV, 0);

	float radius_depth = radius / depth;
	float occlusion = 0.0;
	for (int i = 0; i < samples; i++) {

		float3 ray = radius_depth * reflect(sample_sphere[i], random);
		float3 hemi_ray = position + sign(dot(ray, normal)) * ray;

		float occ_depth = Textures[0].SampleLevel(sampPoint, saturate(hemi_ray.xy), 0).r;
		//occ_depth *= map(occ_depth, 0, 1.0f, 1, 0);

		float difference = depth - occ_depth;

		float contribution = step(falloff, difference) * (1.0 - smoothstep(falloff, area, difference));

		occlusion += contribution * map(depth, 0.22f, 0.33f, 1, 0);
	}

	float ao = 1.0 - total_strength * occlusion * (1.0 / samples);
	float4 adjust = saturate(ao);

	return color * adjust * adjust;
}

float4 PSCombine(VS_OUTPUT In) : SV_TARGET
{
	//float4 color = Textures[0].SampleLevel(sampData, In.UV, 0);
	float4 ao = Textures[0].SampleLevel(sampPoint, In.UV, 0);

	//color.rgb *= ao;
	return ao;
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

technique11 T2
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_4_0, VS()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PSCombine()));
	}
}