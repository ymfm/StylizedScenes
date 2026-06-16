using System.Collections.Generic;
using UnityEngine;

// Scatters prefab objects (trees, rocks, plants, etc.) over the procedural terrain chunks.
// Unlike grass, these are instantiated as real GameObjects (so they keep colliders / shadows /
// LOD groups / different meshes). Instances are parented to each chunk, so they load and hide
// together with it. Each chunk is populated once and then cached.
public class PropScatter : MonoBehaviour
{
    [Header("References")]
    public TerrainGenerator terrainGenerator;  // its children are the chunks
    public GameObject[] prefabs;               // one is picked at random for each placement

    [Header("Height Band (matches the texture layers' Start Height)")]
    [Range(0f, 1f)] public float minHeightPercent = 0.12f;
    [Range(0f, 1f)] public float maxHeightPercent = 0.5f;

    [Header("Slope")]
    [Range(0f, 1f)] public float maxSlope = 0.4f;            // 0 = flat ground only, 1 = any slope
    [Tooltip("How much each object tilts toward the surface normal. 0 = always upright (typical for trees)")]
    [Range(0f, 1f)] public float alignToNormal = 0f;

    [Header("Density")]
    [Tooltip("Objects per square unit. Keep low for trees (e.g. 0.005 - 0.03)")]
    public float density = 0.01f;
    public int maxPerChunk = 200;                            // per-chunk cap
    [Tooltip("Minimum distance between objects in world units (within a chunk). 0 = no spacing")]
    public float minSpacing = 5f;

    [Header("Appearance")]
    [Tooltip("Fixed orientation fix so models stand upright (their imported axis may be sideways). Try -90 or 90 on X")]
    public Vector3 prefabEuler = new Vector3(-90f, 0f, 0f);
    [Tooltip("Overall size multiplier on top of the prefab's own scale")]
    public float scaleMultiplier = 1.5f;
    [Tooltip("Lift each object up by this many world units (use if the base sinks into the ground)")]
    public float heightOffset = 0f;
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);     // random variation, multiplies the prefab's scale
    public int seed = 0;

    readonly HashSet<Transform> built = new HashSet<Transform>();

    void Update()
    {
        if (terrainGenerator == null || prefabs == null || prefabs.Length == 0) return;

        Transform parent = terrainGenerator.transform;

        float minY = 0f, maxY = 1f;
        HeightMapSettings hms = terrainGenerator.heightMapSettings;
        if (hms != null) { minY = hms.minHeight; maxY = hms.maxHeight; }
        float bandMin = Mathf.Lerp(minY, maxY, minHeightPercent);
        float bandMax = Mathf.Lerp(minY, maxY, maxHeightPercent);
        float minNormalY = 1f - maxSlope;

        for (int c = 0; c < parent.childCount; c++)
        {
            Transform chunk = parent.GetChild(c);
            if (!chunk.gameObject.activeInHierarchy) continue;   // only populate chunks that are in view
            if (built.Contains(chunk)) continue;                 // populate each chunk once

            MeshFilter mf = chunk.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Build(mf.sharedMesh, chunk, bandMin, bandMax, minNormalY);
            built.Add(chunk);
        }
    }

    void Build(Mesh mesh, Transform chunk, float bandMin, float bandMax, float minNormalY)
    {
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        if (tris.Length < 3) return;

        Matrix4x4 l2w = chunk.localToWorldMatrix;

        float size = (terrainGenerator.meshSettings != null) ? terrainGenerator.meshSettings.meshWorldSize : 100f;
        int target = Mathf.Min(maxPerChunk, Mathf.RoundToInt(size * size * density));
        if (target <= 0) return;

        // keep all instances under one child for a tidy hierarchy
        Transform holder = new GameObject("Props").transform;
        holder.SetParent(chunk, false);

        var rng = new System.Random(seed ^ chunk.position.GetHashCode());
        int triCount = tris.Length / 3;

        var placedPositions = new List<Vector3>(target);
        float spacingSqr = minSpacing * minSpacing;

        int placed = 0;
        int attempts = target * 4; // some samples get filtered out by height/slope/spacing
        for (int i = 0; i < attempts && placed < target; i++)
        {
            int t = rng.Next(triCount) * 3;
            Vector3 a = verts[tris[t]], b = verts[tris[t + 1]], cc = verts[tris[t + 2]];

            float r1 = (float)rng.NextDouble();
            float r2 = (float)rng.NextDouble();
            if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
            Vector3 world = l2w.MultiplyPoint3x4(a + r1 * (b - a) + r2 * (cc - a));

            if (world.y < bandMin || world.y > bandMax) continue;

            Vector3 nrm = l2w.MultiplyVector(Vector3.Cross(b - a, cc - a)).normalized;
            if (nrm.y < minNormalY) continue;

            // minimum spacing: reject if too close to an already-placed object in this chunk
            if (minSpacing > 0f)
            {
                bool tooClose = false;
                for (int p = 0; p < placedPositions.Count; p++)
                    if ((placedPositions[p] - world).sqrMagnitude < spacingSqr) { tooClose = true; break; }
                if (tooClose) continue;
            }

            GameObject prefab = prefabs[rng.Next(prefabs.Length)];
            if (prefab == null) continue;

            // an anchor carries the placement transform; the prefab keeps its own authored pose underneath
            Transform anchor = new GameObject("prop").transform;
            anchor.SetParent(holder, false);
            anchor.position = world + Vector3.up * heightOffset;
            Vector3 up = Vector3.Slerp(Vector3.up, nrm, alignToNormal).normalized;
            float yaw = (float)rng.NextDouble() * 360f;
            anchor.rotation = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.AngleAxis(yaw, Vector3.up);
            float s = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)rng.NextDouble());
            anchor.localScale = Vector3.one * (s * scaleMultiplier);

            GameObject go = Instantiate(prefab, anchor);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(prefabEuler); // optional upright fix for raw sideways models

            placedPositions.Add(world);
            placed++;
        }
    }
}
