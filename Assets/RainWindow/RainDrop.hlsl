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
    float DriftAmount,
    float WobbleAmount,
    float TrailBend,
    float TrailLength,
    float TrailWidth,
    float XRandRange,
    out float Out)
{
    float2 gridUV = UV * float2(ColCount, RowCount);
    float2 cellID = floor(gridUV);
    float2 st = frac(gridUV);

    // 基础随机值
    float2 h = hash2(cellID);
    float rand = h.x;
    float randStart = h.y;

    // 速度错开
    float speedMul = 0.4 + rand * 0.8;
    float phase = Time * SlideSpeed * speedMul + randStart * 5.0;
    float lifeCount = floor(phase);
    float ti = frac(phase);

    // 每次循环重新随机
    float2 h2 = hash2(cellID + float2(lifeCount * 1.7, lifeCount * 2.3));
    float rand2 = h2.x;
    float rand3 = h2.y;

    // 雨滴位置：限制在格子内 0.15~0.85 的安全范围内
    float dropX = clamp(0.5 + (rand2 - 0.5) * XRandRange, DropSize * 1.5, 1.0 - DropSize * 1.5);
    float dropY = lerp(1.0 - DropSize * 1.5, DropSize * 1.5, pow(ti, Accelerate));
    
    // 横向 wobble
    float wobble = sin(ti * 6.28 * 2.0 + rand3 * 6.28) * WobbleAmount;
    dropX += wobble;

    // 头部距离场
    float2 offset = st - float2(dropX, dropY);
    float dist = length(offset);
    float head = smoothstep(DropSize, DropSize * 0.3, dist);

    // 拖尾
    float trailY = st.y - dropY;
    float trailDx = st.x - dropX;

    float trailFade = saturate(1.0 - trailY / TrailLength);
    float trailWidthFinal = trailFade * DropSize * TrailWidth;

    float trail = smoothstep(trailWidthFinal, 0, abs(trailDx)) * trailFade * TrailOpacity;
    trail *= step(0, trailY);

    // 淡入淡出（基于 ti）
    float visibility = smoothstep(0.0, 0.1, ti) * smoothstep(1.0, 0.9, ti);
    head *= visibility;
    trail *= visibility;

    Out = saturate(head + trail);
}

#endif