struct FragmentOutput
{
	float4 Albedo	: SV_TARGET0;
	float4 Normals	: SV_TARGET1;
	float4 Depth	: SV_TARGET2;
	float4 Data		: SV_TARGET3;
	float4 Position : SV_TARGET4;
};

float4 VS(float3 Position : POSITION0) : SV_POSITION
{
    return float4(Position, 1);
}

FragmentOutput PS()
{
    FragmentOutput output;

	output.Albedo = 0.0f;
	
	//Clear Normals to 0(encoded value is 0.5 but can't use normalize on 0, compile error)
	output.Normals.xyz = 0.5f;
	output.Normals.w = 0.0f;

	output.Depth = 0.0f;
	output.Data = 0;
	output.Position = 0.0f;

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