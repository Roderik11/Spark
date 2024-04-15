#include "common.fx"

cbuffer scene
{
	float4x4 ProjectionInverse;
	float4x4 View;
    float4x4 ViewInverse;
    float4x4 Projection;
    float4x4 CameraInverse;
    float3 ViewDirection;
    float3 CameraPosition;
    float Time;
    float WaterHeight;
}

cbuffer object
{
	float3 LightColor;
	float3 LightDirection;
	float  LightIntensity;
    float4x4 LightSpaces[4];
    float2 Cascades[4];
}

Texture2D Albedo <string Property="Albedo";>;
Texture2D Normal <string Property="Normal";>;
Texture2D Depth <string Property="Depth";>;

Texture2D waterNormals <string Property="Waternormal";>;
Texture2DArray ShadowMap <string Property="Shadowmap";>;
TextureCube textureSkybox <string Property = "Skybox";>;

SamplerState sampData;

struct VertexInput
{
	float4 vPosition	: POSITION0;
	float4 vColor		: COLOR0;
	float2 vUV			: TEXCOORD0;
};

struct VertexOutput
{
	float4 vPosition	: SV_POSITION;
	float2 vUV			: TEXCOORD0;
	float3 vLight		: TEXCOORD2;
};


bool IntersectRayPlane(float3 rayOrigin, float3 rayDirection, float3 posOnPlane, float3 planeNormal, out float3 intersectionPoint)
{
    float rDotn = dot(rayDirection, planeNormal);

    //parallel to plane or pointing away from plane?
    if (rDotn < 0.0000001)
        return false;
 
    float s = dot(planeNormal, (posOnPlane - rayOrigin)) / rDotn;
	
    intersectionPoint = rayOrigin + s * rayDirection;

    return true;
}

float readShadowMap(float4 worldPos, float wdepth)
{
    uint sliceIndex = 4;

    float d = length(worldPos);
    //float d = linearize_depth(wdepth, 0.1f, 104096);
    //float d = lineardepth(wdepth);
	
    for (int i = 0; i < 4; i++)
    {
        if (d < Cascades[i].y)
        {
            sliceIndex = i;
            break;
        }
    }
    if (sliceIndex > 3)
        return 1;
	
    float4 lightViewPosition = mul(worldPos, LightSpaces[sliceIndex]);
	lightViewPosition /= lightViewPosition.w;

	float depth = lightViewPosition.z;

    if (lightViewPosition.x < -1.0f || lightViewPosition.x > 1.0f ||
		lightViewPosition.y < -1.0f || lightViewPosition.y > 1.0f ||
		lightViewPosition.z < 0.0f || lightViewPosition.z > 1.0f)
        return 1;

	float2 texCoords;
	texCoords.x =  lightViewPosition.x * 0.5f + 0.5f;
	texCoords.y = -lightViewPosition.y * 0.5f + 0.5f;

    float shadow = GetShadowFactor(ShadowMap, sampData, texCoords, depth, sliceIndex);
	return shadow;
}


VertexOutput VS(VertexInput IN)
{
	VertexOutput OUT = (VertexOutput)0;

	OUT.vPosition = IN.vPosition;
	OUT.vUV = IN.vUV;

	return OUT;
}

float4 PS(VertexOutput IN, float4 pos: SV_Position) : SV_TARGET
{
    float4 vAlbedo = Albedo.Sample(sampData, IN.vUV);
    float4 vNormals = Normal.Sample(sampData, IN.vUV);
	float vDepth = Depth.Sample(sampData, IN.vUV).r;

	// float3 vNormal = normalize(vNormals.xyz);
	// float3 vLight = normalize(IN.vLight);
    
	float4 vPosition = posFromDepth(IN.vUV, vDepth, CameraInverse);
    float3 intersectionPoint = vPosition.xyz;
  
    IntersectRayPlane(vPosition.xyz, -normalize(vPosition.xyz), float3(0, WaterHeight - CameraPosition.y, 0), float3(0, 1, 0), intersectionPoint);

    float h = max(0, intersectionPoint.y - vPosition.y);
    float vol = distance(intersectionPoint, vPosition.xyz);
    float fade = map(h, 0, 2, 0, 1);
    
    float3 transmissionCoEff = float3(.5f, .8f, .86f);
    float meanBackScatterDist = 8;
    float backScatterBlend = map(vol, 0, 0.5f * meanBackScatterDist, 0, 1);
    float3 backScatterColor = pow(transmissionCoEff, meanBackScatterDist * 2);
   
    float3 viewpos = vPosition.xyz;
    float dist = distance(viewpos, CameraPosition.xyz);
    
    float2 wuv1 = (intersectionPoint.xz + CameraPosition.xz) / 25;
    float2 wuv2 = wuv1 * 2.7f;
    
    float2 dir1 = normalize(float2(-.1f, .6f));
    float2 dir2 = normalize(float2(-.2f, .5f));
    
    float mip = min(8, dist / 10);

    //float3 sample1 = waterNormals.SampleLevel(sampData, wuv1 + dir1 * Time * 0.04f, 7).xzy;
    //float3 sample2 = waterNormals.SampleLevel(sampData, wuv2 + dir2 * Time * -0.04f, 7).xzy;
    
    float3 sample1 = waterNormals.Sample(sampData, wuv1 + dir1 * Time * 0.04f).xzy;
    float3 sample2 = waterNormals.Sample(sampData, wuv2 + dir2 * Time * -0.04f).xzy;

    float3 waterNormal = (sample1 + sample2) / 2;
    waterNormal.xz = waterNormal.xz * 2 - 1;
    waterNormal = normalize(waterNormal);
    //waterNormal = mul(waterNormal, (float3x3) View);
    //waterNormal = normalize(waterNormal);
    
    float3 reflectionVector = reflect(normalize(viewpos), waterNormal);
    float4 reflectiveColor = float4(textureSkybox.Sample(sampData, reflectionVector).rgb, 1);
    float4 refractiveColor = vAlbedo; 
    refractiveColor.rgb *= pow(transmissionCoEff, vol);
    refractiveColor.rgb = lerp(refractiveColor.rgb, backScatterColor, backScatterBlend).rgb;
    
    // if (refractiveColor.r == 0 && refractiveColor.g == 0 && refractiveColor.b == 0)
    //    refractiveColor = float4(textureSkybox.Sample(samplerLinearWrap, -input.reflection).rgb, 1);

    float3 waterPoint = intersectionPoint + float3(0, CameraPosition.y, 0);
    float fresnel = pow(dot(normalize(waterPoint), waterNormal), 2);
    float4 fresnelColor = lerp(reflectiveColor, refractiveColor, fresnel);
    
    return float4(fresnelColor.xyz, fade);
    //return fresnelColor;
    //return float4(fresnel, fresnel, fresnel, fade);
}


technique11 Standard
{
	pass Transparent
	{
		SetVertexShader(CompileShader(vs_4_0, VS()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PS()));
	}
}
