using System.Collections.Generic;
using UnityEngine;

// Scatters grass over the procedural terrain chunks using GPU instancing.
// Attach to any object; no changes to TerrainGenerator / TerrainChunk are required.
// Each frame it scans the TerrainGenerator's children (each child is a chunk). The first
// time it sees a chunk it samples the mesh surface by density, filters by height band and
// slope, builds instance matrices and caches them, then draws them every frame with
// Graphics.DrawMeshInstanced (skipped automatically while a chunk is hidden).
public class GrassScatter : MonoBehaviour
{
    [Header("References")]
    public TerrainGenerator terrainGenerator;  // its children are the chunks
    public GameObject grassPrefab;             // grass prefab (Mesh and Material are extracted from it)
    // Note: height band / chunk size / viewer all reuse the settings already on terrainGenerator.

    Mesh grassMesh;          // extracted from grassPrefab
    Material grassMaterial;  // extracted from grassPrefab (must have GPU Instancing enabled)

    [Header("Grass Height Band (matches the texture layers' Start Height)")]
    [Range(0f, 1f)] public float minHeightPercent = 0.119f; // Element 2 Start Height
    [Range(0f, 1f)] public float maxHeightPercent = 0.30f;  // grass stops here (~ Element 4 Start Height)

    [Header("Slope")]
    [Range(0f, 1f)] public float maxSlope = 0.45f;          // 0 = flat ground only, 1 = any slope
    [Tooltip("How much grass tilts toward the surface normal. 0 = always upright, 1 = fully aligned to the slope")]
    [Range(0f, 1f)] public float alignToNormal = 1f;

    [Header("Edge Softening")]
    [Tooltip("Fade grass density out near the TOP height edge (grass -> higher terrain). 0 = hard cut. The bottom/water edge is intentionally left as a hard cut")]
    [Range(0f, 0.5f)] public float edgeFade = 0.2f;
    [Tooltip("Fade grass density out as the surface steepens toward the Max Slope limit. 0 = hard cut, higher = wider buffer on steep ground")]
    [Range(0f, 1f)] public float slopeFade = 0.3f;

    [Header("Density (main knob)")]
    [Tooltip("Grass clumps per square unit; higher = denser")]
    public float density = 2f;
    public int maxInstancesPerChunk = 50000;                // per-chunk cap to avoid extreme density

    [Header("Appearance Randomization")]
    public Vector2 scaleRange = new Vector2(0.7f, 1.3f);
    public int seed = 0;

    [Header("Performance")]
    [Tooltip("Only draw grass within this radius of the viewer. 0 = no culling (fills the whole view distance, very expensive). 100-200 recommended")]
    public float maxDrawDistance = 150f;
    [Tooltip("Whether grass casts shadows. Shadows are expensive; keep off")]
    public bool castShadows = false;

    [Header("Distance Density Falloff (sparser far away)")]
    [Tooltip("Density ratio kept at the far edge. 0 = almost no grass at the edge, 1 = no falloff")]
    [Range(0f, 1f)] public float farDensity = 0.15f;
    [Tooltip("Falloff begins at this fraction of Max Draw Distance (full density closer than this)")]
    [Range(0f, 1f)] public float densityFalloffStart = 0.35f;

    class ChunkGrass
    {
        public Mesh builtFrom;                              // which mesh it was built from (LOD swaps change the mesh)
        public List<Matrix4x4[]> batches = new List<Matrix4x4[]>();
        public int total;                                  // total instance count (random order, so first N = a uniform subset)
    }

    readonly Dictionary<Transform, ChunkGrass> cache = new Dictionary<Transform, ChunkGrass>();

    void Update()
    {
        if (terrainGenerator == null || grassPrefab == null) return;

        // extract the grass mesh and material from the prefab (once)
        if (grassMesh == null || grassMaterial == null)
        {
            var mf = grassPrefab.GetComponentInChildren<MeshFilter>();
            var mr = grassPrefab.GetComponentInChildren<MeshRenderer>();
            if (mf != null) grassMesh = mf.sharedMesh;
            if (mr != null) grassMaterial = mr.sharedMaterial;
            if (grassMesh == null || grassMaterial == null) return;
            grassMaterial.enableInstancing = true; // required for DrawMeshInstanced
        }

        Transform parent = terrainGenerator.transform;

        // convert the height band to world Y (reuse terrainGenerator's heightMapSettings)
        float minY = 0f, maxY = 1f;
        HeightMapSettings hms = terrainGenerator.heightMapSettings;
        if (hms != null) { minY = hms.minHeight; maxY = hms.maxHeight; }
        float bandMin = Mathf.Lerp(minY, maxY, minHeightPercent);
        float bandMax = Mathf.Lerp(minY, maxY, maxHeightPercent);
        float minNormalY = 1f - maxSlope;

        Vector3 viewerPos = terrainGenerator.viewer != null ? terrainGenerator.viewer.position : Vector3.zero;

        for (int c = 0; c < parent.childCount; c++)
        {
            Transform chunk = parent.GetChild(c);
            if (!chunk.gameObject.activeInHierarchy) continue;

            // distance culling: skip far chunks entirely (no build, no draw) -- key for performance
            if (maxDrawDistance > 0f && (chunk.position - viewerPos).sqrMagnitude > maxDrawDistance * maxDrawDistance)
                continue;

            MeshFilter mf = chunk.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            // first time seen, or the chunk mesh changed (LOD swap): rebuild grass
            if (!cache.TryGetValue(chunk, out ChunkGrass cg) || cg.builtFrom != mf.sharedMesh)
            {
                cg = BuildChunk(mf.sharedMesh, chunk, bandMin, bandMax, minNormalY);
                cache[chunk] = cg;
            }

            // distance density falloff: draw a smaller fraction farther away
            // (instances are in random order, so drawing the first N is a uniform thinning)
            float t = 1f;
            if (maxDrawDistance > 0f)
            {
                float dist = Vector3.Distance(chunk.position, viewerPos);
                float startD = densityFalloffStart * maxDrawDistance;
                if (dist > startD)
                    t = Mathf.Lerp(1f, farDensity, Mathf.InverseLerp(startD, maxDrawDistance, dist));
            }
            int toDraw = Mathf.RoundToInt(cg.total * t);
            if (toDraw <= 0) continue;

            var shadowMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            int drawn = 0;
            for (int b = 0; b < cg.batches.Count && drawn < toDraw; b++)
            {
                int n = Mathf.Min(cg.batches[b].Length, toDraw - drawn);
                Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, cg.batches[b],
                    n, null, shadowMode, false);
                drawn += n;
            }
        }
    }

    ChunkGrass BuildChunk(Mesh mesh, Transform chunk, float bandMin, float bandMax, float minNormalY)
    {
        var cg = new ChunkGrass { builtFrom = mesh };

        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        if (tris.Length < 3) return cg;

        Matrix4x4 l2w = chunk.localToWorldMatrix;

        float size = (terrainGenerator.meshSettings != null) ? terrainGenerator.meshSettings.meshWorldSize : 100f;
        int target = Mathf.Min(maxInstancesPerChunk, Mathf.RoundToInt(size * size * density));
        if (target <= 0) return cg;

        // deterministic random based on chunk coord, so results stay stable across frames / scene reloads
        var rng = new System.Random(seed ^ chunk.position.GetHashCode());
        int triCount = tris.Length / 3;

        var list = new List<Matrix4x4>(target);
        int attempts = target * 3; // some samples get filtered out by height/slope, so try a few extra
        for (int i = 0; i < attempts && list.Count < target; i++)
        {
            int t = rng.Next(triCount) * 3;
            Vector3 a = verts[tris[t]], b = verts[tris[t + 1]], cc = verts[tris[t + 2]];

            // uniform random point inside the triangle (barycentric)
            float r1 = (float)rng.NextDouble();
            float r2 = (float)rng.NextDouble();
            if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
            Vector3 local = a + r1 * (b - a) + r2 * (cc - a);
            Vector3 world = l2w.MultiplyPoint3x4(local);

            if (world.y < bandMin || world.y > bandMax) continue;

            Vector3 nrm = l2w.MultiplyVector(Vector3.Cross(b - a, cc - a)).normalized;
            if (nrm.y < minNormalY) continue;

            // soft edges: thin out grass density to create buffer zones instead of hard cuts
            float keepProb = 1f;
            // top height edge (grass -> higher terrain); the bottom/water edge stays a hard cut on purpose
            if (edgeFade > 0f)
            {
                float fade = (bandMax - bandMin) * edgeFade;
                if (fade > 0f)
                    keepProb = Mathf.Min(keepProb, Mathf.Clamp01((bandMax - world.y) / fade));
            }
            // slope edge: fade out as the surface steepens toward the Max Slope limit
            if (slopeFade > 0f)
            {
                float slopeWindow = (1f - minNormalY) * slopeFade;
                if (slopeWindow > 0f)
                    keepProb = Mathf.Min(keepProb, Mathf.Clamp01((nrm.y - minNormalY) / slopeWindow));
            }
            if (keepProb < 1f && rng.NextDouble() > keepProb) continue;

            float yaw = (float)rng.NextDouble() * 360f;
            float s = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)rng.NextDouble());
            // tilt the grass "up" between world-up and the surface normal by alignToNormal, then random yaw around it
            Vector3 up = Vector3.Slerp(Vector3.up, nrm, alignToNormal).normalized;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.AngleAxis(yaw, Vector3.up);
            list.Add(Matrix4x4.TRS(world, rot, Vector3.one * s));
        }

        cg.total = list.Count;

        // up to 1023 per batch (DrawMeshInstanced limit)
        for (int i = 0; i < list.Count; i += 1023)
        {
            int n = Mathf.Min(1023, list.Count - i);
            var arr = new Matrix4x4[n];
            list.CopyTo(i, arr, 0, n);
            cg.batches.Add(arr);
        }
        return cg;
    }
}
