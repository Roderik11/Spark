float3 PhongLightingNoSpecular(float3 vNormal, float3 vLight, float3 vKey, float fLightAmount)
{
    float NdotL = pow(saturate(dot(vLight, vNormal)), 2);
    float3 vKeyColor = float3(.1f, .1f, .1f) + NdotL * vKey;
    return vKeyColor * fLightAmount;
}

/*
float4 EnvironmentLighting(float3 vLight, float3 vView, float3 vNormal, float3 vPosition, float ReflectionAmount, float Shinyness, float EmissiveAmount, float3 vLightColor, float LightIntensity)
{
	float DS = 3;
	float SS = 40;

	float NL = pow(saturate(dot(vNormal, vLight)), DS);
	float NH = saturate(dot(vNormal, normalize(vView + vLight)));
	float NV = pow(1 - saturate(dot(vNormal, vView)), 2);

	float3 Albedo = LightIntensity * NL * vLightColor;
	float Specular = LightIntensity * pow(saturate(NH), SS) * NL * (0.5f + Shinyness);

	float3 vEnvironment = texReflection.SampleLevel(sampReflection, vPosition, 10); // saturate(0.1f + NV + NL) *

	return float4(Albedo + vEnvironment.rgb * 0.25f + EmissiveAmount + NV * vEnvironment, Specular);
}

float4 SpaceLighting(float3 vLight, float3 vView, float3 vNormal, float3 vPosition, float ReflectionAmount, float Shinyness, float EmissiveAmount, float3 vLightColor, float LightIntensity)
{
	float DS = 3;
	float SS = 40;

	float NL = pow(saturate(dot(vNormal, vLight)), 1);
	NL = smoothstep(0.0f, 0.75f, NL);

	float NH = saturate(dot(vNormal, normalize(vView + vLight)));
	float NV = pow(1 - saturate(dot(vNormal, vView)), 3);

	// GAH, doesnt work, fresnel must be additive!

	float Specular = LightIntensity * pow(saturate(NH), SS) * NL;// * (0.5f + Shinyness);

	float3 vEnvironment = texReflection.SampleLevel(sampReflection, vPosition, 10);

	float4 vResult = 0;

	vResult.rgb = vEnvironment * 0.5f + vLightColor * NL * 1;
	vResult.rgb += vEnvironment * NV * 4; //(NV * (1 - NL)) * 5;// * ReflectionAmount;
	vResult.a = Specular * ReflectionAmount;

	return vResult;
}
*/

//float NL = saturate(dot(vNormal, vLight));
//float S = saturate(NL * 1.5f);
//float S2 = saturate(NL * 5);
////NL = pow(NL, 2);
//float NH = saturate(dot(vNormal, normalize(vView + vLight)));
//float NV = 1;// + saturate(pow(1 - saturate(abs(dot(vNormal, vView))), 3));
//
//float3 vResult = vEnvironment * NV + (NL.xxx * 1.5f) * S2;
//return float4(vResult, NH * S);