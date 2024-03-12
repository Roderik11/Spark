cbuffer scene
{
	float LogZ;
	float4x4 View;
	float4x4 Projection;
}

cbuffer object
{
	float4x4 World;
}

Texture2D textures[1];
SamplerState linearSampler;

struct SPRITE_INPUT
{
	float2 Position : POSITION;
	//float2 Size : SIZE;
	//float4 Color : COLOR;
	//float4 UV : TEXCOORD;
	//int TexIndex: TEXINDEX;
};

//pixel shader inputs
struct PS_INPUT
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR;
	float2 UV : TEXCOORD;
	//int TexIndex : TEXINDEX;
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
SPRITE_INPUT VS(SPRITE_INPUT input)
{
	return input;
}

//Pixel Shader
PS_OUTPUT PS(VS_OUTPUT input)
{
	PS_OUTPUT output;

	half4 vDiffuseMap = Textures[0].Sample(sampData, input.UV);
	half4 vNormalMap = Textures[1].Sample(sampData, input.UV);
	half4 vDataMap = Textures[2].Sample(sampData, input.UV);

	float3 vNormal = normalize(mul(vNormalMap.xyz * 2 - 1, input.TBN));

	// output.Albedo = float4(output.Depth.xyz, 1);
	output.Albedo = float4(vDiffuseMap.rgb, 1);
	output.Normals = float4(vNormal.xyz, vNormalMap.a);
	output.Data = float4(vDataMap.r, vDataMap.g, vDataMap.b, vDataMap.a);
	output.Depth = log2(1 + input.Depth) * LogZ;
	output.fDepth = output.Depth;

	return output;
}

[maxvertexcount(4)]
void GS_Quad(point SPRITE_INPUT sprite[1], inout TriangleStream<PS_INPUT> triStream)
{
	PS_INPUT v;
	v.Color = sprite[0].Color;
	//v.TexIndex = sprite[0].TexIndex;

	//create sprite quad
	//--------------------------------------------

	//bottom left
	v.Position = float4(sprite[0].Position[0], sprite[0].Position[1] - sprite[0].Size[1], 0, 1);
	v.UV = sprite[0].UV.xw;
	triStream.Append(v);

	//top left
	v.Position = float4(sprite[0].Position[0], sprite[0].Position[1], 0, 1);
	v.UV = sprite[0].UV.xy;
	triStream.Append(v);

	//bottom right
	v.Position = float4(sprite[0].Position[0] + sprite[0].Size[0], sprite[0].Position[1] - sprite[0].Size[1], 0, 1);
	v.UV = sprite[0].UV.zw;
	triStream.Append(v);

	//top right
	v.Position = float4(sprite[0].Position[0] + sprite[0].Size[0], sprite[0].Position[1], 0, 1);
	v.UV = sprite[0].UV.zy;
	triStream.Append(v);
}


technique11 T0
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_4_0, VS()));
		SetPixelShader(CompileShader(ps_4_0, PS()));
		SetGeometryShader(CompileShader(gs_4_0, GS_Quad()));
	}
}
