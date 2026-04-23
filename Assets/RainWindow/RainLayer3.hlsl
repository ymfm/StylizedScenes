#ifndef RAIN_LAYER3_INCLUDED
#define RAIN_LAYER3_INCLUDED

float2 hash2_L3(float2 p)
{
    return frac(sin(float2(
        dot(p, float2(127.1, 311.7)),
        dot(p, float2(269.5, 183.3))
    )) * 43758.5);
}

void RainLayer3_float(
    float2 UV,
    float Time,
    float Density,
    float DropSize,
    float LifeDuration,      
    float SpawnChance,       
    float FadeInRange,       
    float FadeOutRange,      
    out float Out)
{
    float2 gridUV = UV * float2(Density, Density);
    float2 cellID = floor(gridUV);
    float2 st = frac(gridUV);

    float2 h = hash2_L3(cellID);
    float rand = h.x;
    float randStart = h.y;

    float lifePhase = Time / LifeDuration + randStart * 5.0;
    float lifeCount = floor(lifePhase);
    float ti = frac(lifePhase);

    float2 h2 = hash2_L3(cellID + float2(lifeCount * 1.7, lifeCount * 2.3));
    float rand2 = h2.x;
    float rand3 = h2.y;
    float spawnRand = frac(h2.x * 7.3 + h2.y * 3.1);

    float safeMargin = DropSize * 1.5;
    float dropX = lerp(safeMargin, 1.0 - safeMargin, rand2);
    float dropY = lerp(safeMargin, 1.0 - safeMargin, rand3);

    float2 offset = st - float2(dropX, dropY);
    float dist = length(offset);
    float head = smoothstep(DropSize, DropSize * 0.3, dist);

    float fadeIn = smoothstep(0.0, FadeInRange, ti);
    float fadeOut = smoothstep(1.0, 1.0 - FadeOutRange, ti);
    float visibility = fadeIn * fadeOut;

    float spawnMask = spawnRand < SpawnChance ? 1.0 : 0.0;

    Out = saturate(head * visibility * spawnMask);
}

#endif