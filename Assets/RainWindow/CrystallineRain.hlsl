#ifndef CRYSTALLINE_RAIN_INCLUDED
#define CRYSTALLINE_RAIN_INCLUDED

TEXTURE2D(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);
void CrystallineRain_float(
    float  Mask,
    float2 ScreenUV,
    float  RefractionStrength,
    float3 LightDir,
    float  SpecularPower,
    float  SpecularIntensity,
    float  FresnelPower,
    float  FresnelIntensity,
    out float3 Color,
    out float  Alpha)
{
    const float3 LightColor = float3(1.0, 1.0, 1.0);
    const float3 WaterColor = float3(1.0, 1.0, 1.0);

    float2 d_mask = float2(ddx(Mask), ddy(Mask));

    float2 N_xy = -d_mask;
    float3 N = normalize(float3(N_xy.x, N_xy.y, 1.0));

    float2 refractedUV = ScreenUV + (N.xy * RefractionStrength);
    
    float3 background = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, refractedUV).rgb;
    float3 L = normalize(-LightDir);
    float3 V = float3(0, 0, 1);
    float3 H = normalize(L + V);
    float  spec = pow(saturate(dot(N, H)), SpecularPower) * SpecularIntensity;

    float fresnel = pow(1.0 - saturate(dot(N, V)), FresnelPower);

    float3 baseColor = lerp(background, background * WaterColor, 0.2);

    baseColor = lerp(baseColor, baseColor * (1.0 - FresnelIntensity), fresnel);

    Color = baseColor + (spec * LightColor);

    Alpha = saturate(Mask * 2.0);
}

#endif // CRYSTALLINE_RAIN_INCLUDED