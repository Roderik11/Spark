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

	int A = p[I.x % 256] + I.y;
	int AA = p[A % 256];
	int AB = p[(A + 1) % 256];
	int	B = p[(I.x + 1) % 256] + I.y;
	int BA = p[B % 256];
	int BB = p[(B + 1) % 256];

	return lerp(lerp(grad(AA, pos),
		grad(BA, pos + float2(-1, 0)), fades.x),
		lerp(grad(AB, pos + float2(0, -1)),
			grad(BB, pos + float2(-1, -1)), fades.x), fades.y);
}

float noise(float3 pos)
{
	int3 I = int3(floor(pos)) & 255;

	pos = frac(pos);

	float3 fades = fade(pos);

	int A = p[I.x % 256] + I.y;
	int AA = p[A % 256] + I.z;
	int AB = p[(A + 1) % 256] + I.z;
	int B = p[(I.x + 1) % 256] + I.y;
	int BA = p[B % 256] + I.z;
	int BB = p[(B + 1) % 256] + I.z;

	return lerp(lerp(lerp(grad(AA, pos),
		grad(BA, pos + float3(-1, 0, 0)), fades.x),
		lerp(grad(AB, pos + float3(0, -1, 0)),
			grad(BB, pos + float3(-1, -1, 0)), fades.x), fades.y),
		lerp(lerp(grad(AA + 1, pos + float3(0, 0, -1)),
			grad(BA + 1, pos + float3(-1, 0, -1)), fades.x),
			lerp(grad(AB + 1, pos + float3(0, -1, -1)),
				grad(BB + 1, pos + float3(-1, -1, -1)), fades.x), fades.y), fades.z);
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

RWTexture2D<float> output;

[numthreads(32, 32, 1)]
void NoiseMap(uint3 groupID : SV_GroupID, uint3 threadID : SV_DispatchThreadID)
{
	output[threadID.xy] = fBm(float3(threadID.x, 0, threadID.y), 2);
}

////
//// Description : Array and textureless GLSL 2D/3D/4D simplex
////               noise functions.
////      Author : Ian McEwan, Ashima Arts.
////  Maintainer : ijm
////     Lastmod : 20110822 (ijm)
////     License : Copyright (C) 2011 Ashima Arts. All rights reserved.
////               Distributed under the MIT License. See LICENSE file.
////               https://github.com/ashima/webgl-noise
////
//
//static const float2  C = float2(1.0f/6.0f, 1.0f/3.0f) ;
//static const float4  D = float4(0.0f, 0.5f, 1.0f, 2.0f);
//
//static const float PI = 3.14159265f;
//static const float SEED = 412423;
//
//float3 mod289(float3 x) {
//	return x - floor(x * (1.0f / 289.0f)) * 289.0f;
//}
//
//float4 mod289(float4 x) {
//	return x - floor(x * (1.0f / 289.0f)) * 289.0f;
//}
//
//float4 permute(float4 x) {
//	return mod289(((x*34.0f)+1.0f)*x);
//}
//
//float4 taylorInvSqrt(float4 r)
//{
//	return 1.79284291400159f - 0.85373472095314f * r;
//}
//
//float snoise(float3 v)
//{
//	// First corner
//	float3 i  = floor(v + dot(v, C.yyy) );
//	float3 x0 =   v - i + dot(i, C.xxx) ;
//
//	// Other corners
//	float3 g = step(x0.yzx, x0.xyz);
//	float3 l = 1.0 - g;
//	float3 i1 = min( g.xyz, l.zxy );
//	float3 i2 = max( g.xyz, l.zxy );
//
//	//   x0 = x0 - 0.0 + 0.0 * C.xxx;
//	//   x1 = x0 - i1  + 1.0 * C.xxx;
//	//   x2 = x0 - i2  + 2.0 * C.xxx;
//	//   x3 = x0 - 1.0 + 3.0 * C.xxx;
//	float3 x1 = x0 - i1 + C.xxx;
//	float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
//	float3 x3 = x0 - D.yyy;      // -1.0+3.0*C.x = -0.5 = -D.y
//
//	// Permutations
//	i = mod289(i);
//	float4 p = permute( permute( permute(
//		i.z + float4(0.0f, i1.z, i2.z, 1.0f ))
//		+ i.y + float4(0.0f, i1.y, i2.y, 1.0f ))
//		+ i.x + float4(0.0f, i1.x, i2.x, 1.0f ));
//
//	// Gradients: 7x7 points over a square, mapped onto an octahedron.
//	// The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
//	float n_ = 0.142857142857f; // 1.0/7.0
//	float3  ns = n_ * D.wyz - D.xzx;
//
//	float4 j = p - 49.0 * floor(p * ns.z * ns.z);  //  mod(p,7*7)
//
//	float4 x_ = floor(j * ns.z);
//	float4 y_ = floor(j - 7.0f * x_ );    // mod(j,N)
//
//	float4 x = x_ *ns.x + ns.yyyy;
//	float4 y = y_ *ns.x + ns.yyyy;
//	float4 h = 1.0f - abs(x) - abs(y);
//
//	float4 b0 = float4( x.xy, y.xy );
//	float4 b1 = float4( x.zw, y.zw );
//
//	//float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
//	//float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
//	float4 s0 = floor(b0)*2.0f + 1.0f;
//	float4 s1 = floor(b1)*2.0f + 1.0f;
//	float4 sh = -step(h, float4(0,0,0,0));
//
//	float4 a0 = b0.xzyw + s0.xzyw*sh.xxyy;
//	float4 a1 = b1.xzyw + s1.xzyw*sh.zzww;
//
//	float3 p0 = float3(a0.xy,h.x);
//	float3 p1 = float3(a0.zw,h.y);
//	float3 p2 = float3(a1.xy,h.z);
//	float3 p3 = float3(a1.zw,h.w);
//
//	//Normalise gradients
//	float4 norm = taylorInvSqrt(float4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
//	p0 *= norm.x;
//	p1 *= norm.y;
//	p2 *= norm.z;
//	p3 *= norm.w;
//
//	// Mix final noise value
//	float4 m = max(0.6 - float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0f);
//		m = m * m;
//	return 42.0f * dot( m*m, float4( dot(p0,x0), dot(p1,x1), dot(p2,x2), dot(p3,x3) ) );
//}
//
//RWTexture2D<float> output;
//
//float Noise2D(float x, float y)
//{
//    float a = acos(cos(((x+SEED*9939.0134)*(x+546.1976)+1)/(y*(y+48.9995)+149.7913)) + sin(x+y/71.0013))/PI;
//    float b = sin(a*10000+SEED) + 1;
//    return b * .5;
//}
//
//[numthreads(32, 32, 1)]
//void NoiseMap(uint3 groupID : SV_GroupID, uint3 threadID : SV_DispatchThreadID)
//{
//    output[threadID.xy] = snoise(float3(threadID.x, 0, threadID.y));// Noise2D(threadID.x, threadID.y);
//}
//
////                [numthreads(32, 32, 1)]
////                void NoiseMap(uint3 groupID : SV_GroupID, uint3 threadID : SV_DispatchThreadID)
////                {
////                    output[threadID.xy] = snoise(float3(threadID.x, 0, threadID.y);// Noise2D(threadID.x, threadID.y);
////                }