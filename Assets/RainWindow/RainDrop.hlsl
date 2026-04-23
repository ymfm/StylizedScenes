#ifndef RAIN_LAYER2_INCLUDED
#define RAIN_LAYER2_INCLUDED

float2 hash2(float2 p)
{
    return frac(sin(float2(
        dot(p, float2(127.1, 311.7)),
        dot(p, float2(269.5, 183.3))
    )) * 43758.5);
}

void RainLayer2_float(
    float2 UV,
    float Time,
    float ColCount,
    float RowCount,
    float SlideSpeed,
    float Accelerate,
    float DropSize,
    float TrailOpacity,
    float WobbleAmount,
    float TrailLength,
    float TrailWidth,
    float XRandRange,
    float FadeInRange,
    float FadeOutRange,
    float SpawnChance,
    out float Out)
{
    float2 gridUV = UV * float2(ColCount, RowCount);
    float2 cellID = floor(gridUV);
    float2 st = frac(gridUV);

    float2 h = hash2(cellID);
    float rand = h.x;
    float randStart = h.y;

    float speedMul = 0.4 + rand * 0.8;
    float phase = Time * SlideSpeed * speedMul + randStart * 5.0;
    float lifeCount = floor(phase);
    float ti = frac(phase);
    float spawnRand = hash2(cellID + float2(lifeCount * 3.1, lifeCount * 4.7)).x;

    float2 h2 = hash2(cellID + float2(lifeCount * 1.7, lifeCount * 2.3));
    float rand2 = h2.x;
    float rand3 = h2.y;

    float dropX = clamp(0.5 + (rand2 - 0.5) * XRandRange, DropSize * 1.5, 1.0 - DropSize * 1.5);
    float dropY = lerp(1.0 - DropSize * 1.5, DropSize * 1.5, pow(ti, Accelerate));
    
    float wobble = sin(ti * 6.28 * 2.0 + rand3 * 6.28) * WobbleAmount;
    dropX += wobble;

    float2 offset = st - float2(dropX, dropY);
    float dist = length(offset);
    float head = smoothstep(DropSize, DropSize * 0.3, dist);

    float trailY = st.y - dropY;
    float trailDx = st.x - dropX;

    float trailFade = saturate(1.0 - trailY / TrailLength);
    float trailWidthFinal = trailFade * DropSize * TrailWidth;

    float trail = smoothstep(trailWidthFinal, 0, abs(trailDx)) * trailFade * TrailOpacity;
    trail *= step(0, trailY);

    float fadeIn  = smoothstep(1.0, 1.0 - FadeInRange, dropY);
    float fadeOut = smoothstep(0.0, FadeOutRange, dropY);
    float visibility = fadeIn * fadeOut;
    head *= visibility;
    trail *= visibility;

    float spawnMask = spawnRand < SpawnChance ? 1.0 : 0.0;
    head *= spawnMask;
    trail *= spawnMask;

    Out = saturate(head + trail);
}

#endif