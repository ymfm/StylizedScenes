#ifndef RAIN_LIGHTING_INCLUDED
#define RAIN_LIGHTING_INCLUDED

float3 HeightToNormal(float mask, float2 uv, float strength)
{
    float2 duv = fwidth(uv) + 1e-5;
    float dhdu = ddx(mask) / duv.x;
    float dhdv = ddy(mask) / duv.y;
    float3 n = float3(-dhdu * strength, -dhdv * strength, 1.0);
    return normalize(n);
}

void RainNormal_float(
    float  Mask,
    float2 UV,
    float  NormalStrength,
    float3 LightDir,
    float3 LightColor,
    float3 WaterColor,
    float  Ambient,
    float  RimPower,
    float  RimIntensity,
    float  HighlightPower,
    float  HighlightIntensity,
    out float3 Color,
    out float  Alpha)
{
    float3 N = HeightToNormal(Mask, UV, NormalStrength);

    float3 L = normalize(-LightDir);
    float  NdotL = saturate(dot(N, L));

    float rim = pow(1.0 - saturate(N.z), RimPower) * RimIntensity;

    float3 V = float3(0, 0, 1);
    float3 H = normalize(L + V);
    float  spec = pow(saturate(dot(N, H)), HighlightPower) * HighlightIntensity;

    float3 brightWater = lerp(WaterColor, float3(1,1,1), 0.6);
    float  shading = Ambient + NdotL * (1.0 - Ambient);
    
    float3 baseTint  = brightWater * shading;
    float3 highlight = (rim + spec) * float3(1,1,1);

    Color = baseTint + highlight;

    Alpha = saturate(Mask + rim * 0.3);
}

#endif