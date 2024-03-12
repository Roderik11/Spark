Texture2D Albedo <string Property="Albedo";>;
float2 ScreenSize;
float LineWidth;

SamplerState linearSampler;

struct VertexInput
{
	float2 Position : POSITION;
	float2 Size : SIZE;
	float4 Color : COLOR;
	float4 UV : TEXCOORD;
	//int TexIndex: TEXINDEX;
};

struct FragmentInput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR;
    noperspective float2 UV : TEXCOORD;
	//int TexIndex : TEXINDEX;
};


VertexInput VS(VertexInput input)
{
	return input;
}

float4 PS(FragmentInput input) : SV_Target
{
    return Albedo.Sample(linearSampler, input.UV) * input.Color;
}

float4 PS_Line(FragmentInput input) : SV_Target
{
    float fade = input.UV.y * 3 - 1.5f;
    float a = exp2(-2.7 * fade * fade);
    float4 color = input.Color;
    color.a = a;
    return Albedo.Sample(linearSampler, input.UV) * color;
}

[maxvertexcount(4)]
void GS_Quad(point VertexInput sprite[1], inout TriangleStream<FragmentInput> triStream)
{
    VertexInput input = sprite[0];
	
	FragmentInput v;
    v.Color = input.Color;
	//v.TexIndex = sprite[0].TexIndex;

	//bottom left
    v.Position = float4(input.Position.x, input.Position.y - input.Size.y, 0, 1);
    v.UV = input.UV.xw;
	triStream.Append(v);

	//top left
    v.Position = float4(input.Position.x, input.Position.y, 0, 1);
    v.UV = input.UV.xy;
    triStream.Append(v);

	//bottom right
    v.Position = float4(input.Position.x + input.Size.x, input.Position.y - input.Size.y, 0, 1);
    v.UV = input.UV.zw;
    triStream.Append(v);

	//top right
    v.Position = float4(input.Position.x + input.Size.x, input.Position.y, 0, 1);
    v.UV = input.UV.zy;
    triStream.Append(v);
}


[maxvertexcount(4)]
void GS_Line(point VertexInput sprite[1], inout TriangleStream<FragmentInput> triStream)
{
    VertexInput v = sprite[0];
    
    float2 a = v.Position.xy;
    float2 b = v.Size.xy;
    float2 c = normalize(float2(a.y - b.y, b.x - a.x)) / ScreenSize * LineWidth;

    FragmentInput g0;
    g0.Color = v.Color;
    g0.Position = float4(a - c, 0, 1);
    g0.UV = v.UV.xw;
    
    FragmentInput g1;
    g1.Color = v.Color;
    g1.Position = float4(a + c, 0, 1);
    g1.UV = v.UV.xy;
    
    FragmentInput g2;
    g2.Color = v.Color;
    g2.Position = float4(b - c, 0, 1);
    g2.UV = v.UV.zw;
    
    FragmentInput g3;
    g3.Color = v.Color;
    g3.Position = float4(b + c, 0, 1);
    g3.UV = v.UV.zy;

    triStream.Append(g0);
    triStream.Append(g1);
    triStream.Append(g2);
    triStream.Append(g3);
   // triStream.RestartStrip();
}

technique11 T1
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_4_0, VS()));
		SetPixelShader(CompileShader(ps_4_0, PS()));
		SetGeometryShader(CompileShader(gs_4_0, GS_Quad()));
	}

    pass P1
    {
        SetVertexShader(CompileShader(vs_4_0, VS()));
        SetPixelShader(CompileShader(ps_4_0, PS_Line()));
        SetGeometryShader(CompileShader(gs_4_0, GS_Line()));
    }

}
