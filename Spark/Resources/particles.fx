cbuffer scene
{
	float4x4 View;
	float4x4 Projection;
}

struct Particle
{
	float3 position;
	float3 velocity;
	float4 color;
	//float age;
};

StructuredBuffer<Particle> particles;

//Vertex Output Structure
struct VS_OUTPUT
{
	float4 Position		: SV_POSITION;
	float4 Color		: TEXCOORD0;
	float3 Depth		: TEXCOORD1;
};

//Pixel Output Structure
struct PS_OUTPUT
{
	float4 Albedo		: SV_TARGET0;
	float4 Normals		: SV_TARGET1;
	float  Depth		: SV_TARGET2;
	float4 Data			: SV_TARGET3;
};

//Vertex Shader
VS_OUTPUT VS(uint id : SV_VERTEXID)
{
	VS_OUTPUT output;

	float4 worldPosition = float4(particles[id].position, 1);
	float4 viewPosition = mul(worldPosition, View);

	output.Position = mul(viewPosition, Projection);
	output.Depth = float3(output.Position.zw, viewPosition.z);
	output.Color = particles[id].color;

	return output;
}

//Pixel Shader
PS_OUTPUT PS(VS_OUTPUT input)
{
	PS_OUTPUT output;

	output.Albedo = float4(input.Color.rgb, 1);
	output.Depth = input.Depth.z;
	output.Data = float4(0, 0, 0, 1);

	return output;
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