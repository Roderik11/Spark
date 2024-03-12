cbuffer MatrixBuffer :  register(b0)
{
	float LogZ;
	float4x4 View;
	float4x4 Projection;
}

//Vertex Input Structure
struct VS_INPUT
{
	float3 Position		: POSITION;
	float4 Color		: COLOR;
};

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
	float3 Depth		: SV_TARGET2;
	float4 Data			: SV_TARGET3;
	float fDepth : SV_DEPTH;
};

//Vertex Shader
VS_OUTPUT VS(VS_INPUT input)
{
	VS_OUTPUT output;

	float4 worldPosition = float4(input.Position, 1);
	float4 viewPosition = mul(worldPosition, View);

	output.Position = mul(viewPosition, Projection);
	output.Depth = float3(output.Position.zw, output.Position.w);
	output.Color = input.Color;

	return output;
}

//Pixel Shader
PS_OUTPUT PS(VS_OUTPUT input)
{
	PS_OUTPUT output;

	output.Albedo = float4(input.Color.rgb, 1);
	output.Depth = log2(1 + input.Depth.y) * LogZ;// / log2(1 * 100000000);
	//output.Depth = input.Depth.z;
	output.Data = float4(0, 0, 0, 1);
	output.fDepth = output.Depth;

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