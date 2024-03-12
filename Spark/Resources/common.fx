static const int KERNEL_HALF = 2;
static const float KERNEL_DIV = 1.0f / ((KERNEL_HALF * 2 + 1) * (KERNEL_HALF * 2 + 1));

float3 FOG(float3 color, float depth)
{
    float3 fogColor = float3(0.5f, 0.6f, 0.7f);
    //float fog = 1 - saturate((4000 - depth) / (4000 - 300)); // linear
    float density = 0.001f;
	//fog = 1 - 1 / pow(2, linDepth * density); // exponential
    float fog = 1 - 1 / pow(2, pow(depth * density, 2)); // exponential squared
    fog = depth > 10000 ? 0 : fog;
	
    return lerp(color, fogColor, fog);
}

float GetShadowFactor(in Texture2DArray tex, in SamplerState samplerState, in float2 uv, float depth, int sliceIndex)
{
    float result = 0;
    float2 texCoords = uv;
	
    float width;
    float height;
    float slices;
    tex.GetDimensions(width, height, slices);
	
    float texelSize = 1 / width;
	
    for (int y = -KERNEL_HALF; y <= KERNEL_HALF; ++y)
    {
        for (int x = -KERNEL_HALF; x <= KERNEL_HALF; ++x)
        {
            texCoords = uv + float2(x, y) * texelSize;
            result += (depth < tex.SampleLevel(samplerState, float3(texCoords, sliceIndex), 0).r + 0.0001f);
        }
    }

    result *= KERNEL_DIV;

    return result;
}

float4 posFromDepth(in float2 uv, in float depth, in float4x4 cameraInverse)
{
    float x = uv.x * 2 - 1;
    float y = (1 - uv.y) * 2 - 1;
    float4 wpos = float4(x, y, depth, 1.0f);

    wpos = mul(wpos, cameraInverse);
    wpos /= wpos.w;

    return wpos;
}

float view_depth(in float2 uv, in float depth, in float4x4 projInverse)
{
    float x = uv.x * 2 - 1;
    float y = (1 - uv.y) * 2 - 1;
    float4 wpos = float4(x, y, depth, 1.0f);

    wpos = mul(wpos, projInverse);
    wpos /= wpos.w;

    return wpos.z;
}

float linearize_depth(in float d, in float zNear, in float zFar)
{
    return zNear * zFar / (zFar + d * (zNear - zFar));
}

float lineardepth(in float d, in float4x4 proj)
{
    return proj._43 / (d - proj._33);
}

float3 blend_linear(in float3 n1, in float3 n2)
{
	float3 r = (n1 + n2) / 2;// * 2 - 2;
	return normalize(r);
}

float3 blend_overlay(float4 n1, float4 n2)
{
	n1 = n1 * 4 - 2;
	float4 a = n1 >= 0 ? -1 : 1;
	float4 b = n1 >= 0 ? 1 : 0;
	n1 = 2 * a + n1;
	n2 = n2 * a + b;
	float3 r = n1 * n2 - a;
	return normalize(r);
}

float3 blend_pd(float4 n1, float4 n2)
{
	n1 = n1 * 2 - 1;
	n2 = n2.xyzz*float4(2, 2, 2, 0) + float4(-1, -1, -1, 0);
	float3 r = n1.xyz*n2.z + n2.xyw*n1.z;
	return normalize(r);
}

float3 blend_whiteout(float4 n1, float4 n2)
{
	n1 = n1 * 2 - 1;
	n2 = n2 * 2 - 1;
	float3 r = float3(n1.xy + n2.xy, n1.z*n2.z);
	return normalize(r);
}

float3 blend_udn(float3 n1, float3 n2)
{
	float3 c = float3(2, 1, 0);
	float3 r;
	r = n2 * c.yyz + n1.xyz;
	r = r * c.xxx - c.xxy;
	return normalize(r);
}

float3 blend_rnm(float4 n1, float4 n2)
{
	float3 t = n1.xyz*float3(2, 2, 2) + float3(-1, -1, 0);
	float3 u = n2.xyz*float3(-2, -2, 2) + float3(1, 1, -1);
	float3 r = t * dot(t, u) - u * t.z;
	return normalize(r);
}

float3 blend_unity(float4 n1, float4 n2)
{
	n1 = n1.xyzz*float4(2, 2, 2, -2) + float4(-1, -1, -1, 1);
	n2 = n2 * 2 - 1;
	float3 r;
	r.x = dot(n1.zxx, n2.xyz);
	r.y = dot(n1.yzy, n2.xyz);
	r.z = dot(n1.xyw, -n2.xyz);
	return normalize(r);
}

float noise(float2 uv)
{
	return 2.0 * frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453) - 1.0;
}

float sum(float3 v) { return v.x + v.y + v.z; }

half3 blend(half4 texture1, float a1, half4 texture2, float a2)
{
	float depth = 0.2;
	float ma = max(texture1.a + a1, texture2.a + a2) - depth;

	float b1 = max(texture1.a + a1 - ma, 0);
	float b2 = max(texture2.a + a2 - ma, 0);

	return (texture1.rgb * b1 + texture2.rgb * b2) / (b1 + b2);
}

float remap(float value, float from1, float to1, float from2, float to2)
{
    return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
}

float map(float value, float min1, float max1, float min2, float max2)
{
	// Convert the current value to a percentage
	// 0% - min1, 100% - max1
	float perc = (value - min1) / (max1 - min1);

	// Do the same operation backwards with min2 and max2
	value = perc * (max2 - min2) + min2;

	return clamp(value, min(min2, max2), max(min2, max2));
}

//Normal Encoding Function
half3 EncodeNormal(half3 n)
{
	return 0.5f * (normalize(n) + 1.0f);
}

//Normal Decoding Function
half3 DecodeNormal(half3 enc)
{
	return enc * 2 - 1;
}

// get normalized device coords
float2 ConvertScreenToProjection(float4 vScreen)
{
	return float2(-vScreen.x / vScreen.w / 2.0f + 0.5f, -vScreen.y / vScreen.w / 2.0f + 0.5f);
}

// get UV coords
half2 ConvertScreenToUV(float4 vScreen)
{
	half2 vScreenCoord = vScreen.xy / vScreen.w;
	vScreenCoord = 0.5h * (half2(vScreenCoord.x, -vScreenCoord.y) + 1);

	return vScreenCoord;
}

// get viewspace position from linear depth
float3 PositionVSFromDepth(float3 vView, float fDepth)
{
	return fDepth * vView.xyz / vView.z;
}

float4 SampleLinear2D(Texture2D tex, SamplerState samp, float2 uv)
{
	return pow(tex.Sample(samp, uv), 2.2f);
}

float4 SampleLinearCube(TextureCube tex, SamplerState samp, float3 uv)
{
	return pow(tex.Sample(samp, uv), 2.2f);
}

float4 SampleLinearCubeLevel(TextureCube tex, SamplerState samp, float3 uv, float level)
{
	return pow(tex.SampleLevel(samp, uv, level), 2.2f);
}

float3 SignedOctEncode(float3 n)
{
    float3 OutN;

    n /= (abs(n.x) + abs(n.y) + abs(n.z));

    OutN.y = n.y * 0.5f + 0.5f;
    OutN.x = n.x * 0.5f + OutN.y;
    OutN.y = n.x * -0.5f + OutN.y;

    OutN.z = saturate(n.z * 10000000000.0f);
    return OutN;
}

float3 SignedOctDecode(float3 n)
{
    float3 OutN;

    OutN.x = (n.x - n.y);
    OutN.y = (n.x + n.y) - 1.0f;
    OutN.z = n.z * 2.0f - 1.0f;
    OutN.z = OutN.z * (1.0f - abs(OutN.x) - abs(OutN.y));
 
    OutN = normalize(OutN);
    return OutN;
}

//float4 LightDirection = { 100.0f, 100.0f, 100.0f, 1.0f };
//float4 LightColor = { 1.0f, 1.0f, 1.0f, 1.0f };
//float4 LightColorAmbient = { 0.0f, 0.0f, 0.0f, 1.0f };
//float4 FogColor = { 1.0f, 1.0f, 1.0f, 1.0f };
//
//float fDensity;
//bool isSkydome;
//
//float SunLightness = 0.2;
//float sunRadiusAttenuation = 256;
//float largeSunLightness = 0.2;
//float largeSunRadiusAttenuation = 3;
//float dayToSunsetSharpness = 1.5;
//float hazeTopAltitude = 20;

//
//float4 Scattering(float3 vNormal, float3 vEye, float vLightDir, float3 vLightColor, float4 DiffuseColor, float altitude)
//{
//	float4 colorOutput = float4(0,0,0,1);
//	float4 colorAmbient = DiffuseColor;
//
//	// Calculate light/eye/normal vectors
//	float eyeAlt = altitude;
//	float3 eyeVec = normalize(vEye);
//	float3 normal = normalize(vNormal);
//	float3 lightVec = normalize(vLightDir);
//
//	// Calculate the amount of direct light	
//	float NdotL = max(dot(normal, -lightVec), 0);
//
//	//float4 colorDiffuse = DiffuseColor * (NdotL * vLightColor) + LightColorAmbient * DiffuseColor;
//	float4 colorDiffuse = (NdotL * vLightColor);
//	colorOutput += colorDiffuse;
//	colorOutput.a = 1.0f;
//
//	// Calculate sun highlight...	
//	float sunHighlight = pow(max(0, dot(lightVec, -eyeVec)), sunRadiusAttenuation) * SunLightness;
//	// Calculate a wider sun highlight 
//	float largeSunHighlight = pow(max(0, dot(lightVec, -eyeVec)), largeSunRadiusAttenuation) * largeSunLightness;
//
//	// Calculate 2D angle between pixel to eye and sun to eye
//	float3 flatLightVec = normalize(float3(lightVec.x, 0, lightVec.z));
//	float3 flatEyeVec = normalize(float3(eyeVec.x, 0, eyeVec.z));
//	float diff = dot(flatLightVec, -flatEyeVec);
//
//	// Based on camera altitude, the haze will look different and will be lower on the horizon.
//	// This is simulated by raising YAngle to a certain power based on the difference between the
//	// haze top and camera altitude. 
//	// This modification of the angle will show more blue sky above the haze with a sharper separation.
//	// Lerp between 0.25 and 1.25
//	float val = lerp(0.25, 1.25, min(1, hazeTopAltitude / max(0.0001, eyeAlt)));
//	// Apply the power to sharpen the edge between haze and blue sky
//	float YAngle = pow(max(0, -eyeVec.y), val);
//
//	// Fetch the 3 colors we need based on YAngle and angle from eye vector to the sun
//	float4 fogColorDay = tex2D(SurfSamplerSkyTextureDay, float2(1 - (diff + 1) * 0.5, 1 - YAngle));
//	float4 fogColorSunset = tex2D(SurfSamplerSkyTextureSunset, float2(1 - (diff + 1) * 0.5, 1 - YAngle));
//	float4 fogColorNight = tex2D(SurfSamplerSkyTextureNight, float2(1 - (diff + 1) * 0.5, 1 - YAngle));
//
//	float4 fogColor;
//
//	// If the light is above the horizon, then interpolate between day and sunset
//	// Otherwise between sunset and night
//	if (lightVec.y > 0)
//	{
//		// Transition is sharpened with dayToSunsetSharpness to make a more realistic cut 
//		// between day and sunset instead of a linear transition
//		fogColor = lerp(fogColorDay, fogColorSunset, min(1, pow(1 - lightVec.y, dayToSunsetSharpness)));
//	}
//	else
//	{
//		// Slightly different scheme for sunset/night.
//		fogColor = lerp(fogColorSunset, fogColorNight, min(1, -lightVec.y * 4));
//	}
//
//	// Add sun highlights
//	fogColor += sunHighlight + largeSunHighlight;
//
//	// Apply fog on output color
//	colorOutput = lerp(fogColor, colorOutput, IN.Fog);
//
//	// Make sun brighter for the skybox...
//	if (isSkydome)
//		colorOutput = fogColor + sunHighlight;
//
//	return colorOutput;
//}


//// get worldspace position from perspective depth
//float4 PositionWSFromDepth()
//{
//	float4 Position = 1.0f;
//	Position.x = input.UV.x * 2.0f - 1.0f;
//	Position.y = -(input.UV.y * 2.0f - 1.0f);
//	Position.z = Depth;
//	Position = mul(Position, InverseViewProjection);
//	Position.xyz /= Position.w;
//}

/*
– Vertex Shader –

Output.Ptrick = Pproj.xyw;

– Pixel Shader –

float4x4 matProj; // standard projection matrix

static const float2 Clever = 1.0 / matProj._11_22;

Ptrick.xy /= Ptrick.z;
Ptrick.xy *= Clever * LinearZ;
Ptrick.z = LinearZ;
*/