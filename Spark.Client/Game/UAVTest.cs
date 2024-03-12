using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace Spark.Client
{
    public class UAVTest : Component
    {
        private Texture texture;
        private ComputeShader computeShader;
        private ConstantBufferBox constantBuffer;

        const string baseShader = @"
// A direct port of Ken Perlin's Java reference to HLSL
// with the gradients changed to be an array lookup instead of calculating 
// and permutation table NOT duplicated (all lookups perform a '% 256' first)

// permutation table: 0-255 randomly arranged
static const int p[] = { 151,160,137,91,90,15,
131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
};

// gradients for 3d noise
static const float3 g[] = {
    1,1,0,
    -1,1,0,
    1,-1,0,
    -1,-1,0,
    1,0,1,
    -1,0,1,
    1,0,-1,
    -1,0,-1, 
    0,1,1,
    0,-1,1,
    0,1,-1,
    0,-1,-1,
    1,1,0,
    0,-1,1,
    -1,1,0,
    0,-1,-1,
};

float2 fade(float2 t) { return t * t * t * (t * (t * 6 - 15) + 10); }
float3 fade(float3 t) { return t * t * t * (t * (t * 6 - 15) + 10); }

float grad(float x, float2 pos)
{
	return dot(g[p[x % 256] % 16], pos);
}

float grad(float x, float3 pos)
{
	return dot(g[p[x % 256] % 16], pos);
}

float noise(float2 pos)
{
	int2 I = int2(floor(pos)) & 255;
	
	pos = frac(pos);
	
	float2 fades = fade(pos);
	
	int A  = p[I.x      % 256] + I.y;
	int AA = p[A        % 256];
	int AB = p[(A + 1)  % 256];
	int	B  = p[(I.x + 1)% 256] + I.y;
	int BA = p[B        % 256];
	int BB = p[(B + 1)  % 256]; 

	return lerp(lerp(grad(AA, pos), 
	                 grad(BA, pos + float2(-1,  0)), fades.x), 
				lerp(grad(AB, pos + float2( 0, -1)), 
				     grad(BB, pos + float2(-1, -1)), fades.x), fades.y);
}

float noise(float3 pos) 
{
	int3 I = int3(floor(pos)) & 255;
	  
	pos = frac(pos);
	
	float3 fades = fade(pos);
		  
	int A  = p[I.x     % 256] + I.y;
	int AA = p[A       % 256] + I.z;
	int AB = p[(A + 1) % 256] + I.z; 
	int B  = p[(I.x+1) % 256] + I.y;
	int BA = p[B       % 256] + I.z;
	int BB = p[(B + 1) % 256] + I.z;

	return lerp(lerp(lerp(grad(AA,   pos),
						  grad(BA,   pos + float3(-1,  0,  0)), fades.x),
				     lerp(grad(AB,   pos + float3( 0, -1,  0)), 
						  grad(BB,   pos + float3(-1, -1,  0)), fades.x), fades.y),
				lerp(lerp(grad(AA+1, pos + float3( 0,  0, -1)), 
				   		  grad(BA+1, pos + float3(-1,  0, -1)), fades.x),
					 lerp(grad(AB+1, pos + float3( 0, -1, -1)),
						  grad(BB+1, pos + float3(-1, -1, -1)), fades.x), fades.y), fades.z);
}

float fBm(float2 pos, int octaves)
{
	float lacunarity = 2.0;
	float gain = 0.5;

	float freq = 1.0;
	float amp = 0.5;
	float sum = 0;
	for (int i = 0; i < octaves; i++)
	{
		sum += noise(pos * freq) * amp;
		freq *= lacunarity;
		amp *= gain;
	}
	return sum;
}

float fBm(float3 pos, int octaves)
{
	float lacunarity = 2.0;
	float gain = 0.5;

	float freq = 2.0;
	float amp = 0.5;
	float sum = 0;
	for (int i = 0; i < octaves; i++)
	{
		sum += noise(pos * freq) * amp;
		freq *= lacunarity;
		amp *= gain;
	}
	return sum;
}

RWTexture2D<float> output;

[numthreads(32, 32, 1)]
void NoiseMap(uint3 groupID : SV_GroupID, uint3 threadID : SV_DispatchThreadID)
{
    output[threadID.xy] = fBm(float3(threadID.x, 0, threadID.y) / 1024, 8);
}

";

        protected override void Awake()
        {
            string shaderSource = baseShader;

            Texture2D res = new Texture2D(Engine.Device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.R32_Float,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Height = 1024,
                Width = 1024,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default

            });

            Texture result = new Texture
            {
                Resource = res,
                View = new ShaderResourceView(Engine.Device, res)
            };

            UnorderedAccessView uav = new UnorderedAccessView(Engine.Device, result.Resource);

            using (ShaderBytecode integrateBytecode = ShaderBytecode.Compile(shaderSource, "NoiseMap", "cs_5_0", ShaderFlags.None, EffectFlags.None, ""))
            {
                computeShader = new ComputeShader(Engine.Device, integrateBytecode);
               // ShaderReflection reflection = new ShaderReflection(integrateBytecode);
               // constantBuffer = new ConstantBufferBox(reflection.GetConstantBuffer(0));
            }

            //constantBuffer.SetParameter("SEED", 1234);

            Engine.Device.ImmediateContext.ComputeShader.Set(computeShader);
            Engine.Device.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, uav);
           // Engine.Device.ImmediateContext.ComputeShader.SetConstantBuffer(0, constantBuffer.Buffer);
            Engine.Device.ImmediateContext.Dispatch(32, 32, 1);

           // Texture2D.ToFile(Engine.Device.ImmediateContext, result.Resource, ImageFileFormat.Dds, "G:\\uav.dds");

            base.Awake();
        }
    }
}
