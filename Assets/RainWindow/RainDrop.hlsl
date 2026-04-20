#ifndef RAIN_LAYER2_INCLUDED
#define RAIN_LAYER2_INCLUDED

void RainLayer2_float(
    float2 UV,
    float Time,
    float ColCount,
    float RowCount,
    float SlideSpeed,
    float Accelerate,
    float DropSize,
    float TrailLength,
    float TrailOpacity,
    float DriftAmount,
    float WobbleAmount,
    float TrailBend,
    float TrailWidth,
    out float Out)
{
    float2 gridUV = UV * float2(ColCount, RowCount);
    float2 cellID = floor(gridUV);
    float2 st = frac(gridUV);

    float rand  = frac(sin(cellID.x * 35.2 + cellID.y * 78.3) * 43758.5);
    float rand2 = frac(sin(cellID.x * 12.9 + cellID.y * 56.7) * 12345.6);
    float rand3 = frac(sin(cellID.x * 91.4 + cellID.y * 23.1) * 54321.7);

    float speedMul = 0.6 + rand * 0.8;
    float ti = frac(Time * SlideSpeed * speedMul + rand);
    float dropY = 1.0 - pow(ti, Accelerate);

    float baseX = 0.5 + (rand2 - 0.5) * DriftAmount;
    float wobble = sin(ti * 6.28 * 2.0 + rand3 * 6.28) * WobbleAmount;
    float dropX = baseX + wobble;

    float2 offset = st - float2(dropX, dropY);
    float dist = length(offset);
    float head = smoothstep(DropSize, DropSize * 0.3, dist);

    float trailY = st.y - dropY;
    float trailDx = st.x - dropX;
    float trailFade = saturate(1.0 - trailY / TrailLength);
    float trailWidth = trailFade * DropSize * TrailWidth;
    float trail = smoothstep(trailWidth, 0, abs(trailDx)) * trailFade * TrailOpacity;
    trail *= step(0, trailY);

    Out = saturate(head + trail);
}

#endif