using UnityEngine;

// Some chunks rain. Decides which chunks are rainy by their coordinate. While the player is
// inside a rainy chunk it:
//   - enables the rain particles (a prefab that follows the player so the rain stays overhead)
//   - turns on the global rain look via RainController.SetRain (screen droplets + Volume)
// RainController is a separate, persistent scene object so its scene references (globalVolume /
// rendererData) stay valid. The particles are a separate prefab so they can follow the player.
public class RainWeatherManager : MonoBehaviour
{
    [Header("References")]
    public TerrainGenerator terrainGenerator;  // reuses its viewer and meshSettings
    public RainController rainController;       // scene object: controls droplets + Volume
    public GameObject rainParticlesPrefab;     // particle rig (rain + ripples), without RainController

    [Header("Rainy Chunks")]
    [Tooltip("Fraction of chunks that rain")]
    [Range(0f, 1f)] public float rainChance = 0.3f;
    public int seed = 12345;

    [Header("Particles Follow Player")]
    [Tooltip("Offset of the rain particles relative to the player (to keep rain overhead)")]
    public Vector3 followOffset = Vector3.zero;
    [Tooltip("Follow the player's Y too (recommended on hilly terrain)")]
    public bool followHeight = true;

    GameObject particles;

    void Start()
    {
        if (rainParticlesPrefab != null)
        {
            particles = Instantiate(rainParticlesPrefab);
            particles.name = "RainParticles (runtime)";
            particles.SetActive(false);
        }
    }

    void Update()
    {
        if (terrainGenerator == null || terrainGenerator.viewer == null ||
            terrainGenerator.meshSettings == null) return;

        float size = terrainGenerator.meshSettings.meshWorldSize;
        if (size <= 0f) return;

        Transform viewer = terrainGenerator.viewer;
        Vector2 coord = new Vector2(Mathf.Round(viewer.position.x / size), Mathf.Round(viewer.position.z / size));
        bool rainy = IsRainy(coord);

        // rain particles follow the player while in a rainy chunk
        if (particles != null)
        {
            if (rainy)
            {
                Vector3 pos = viewer.position + followOffset;
                if (!followHeight) pos.y = followOffset.y;
                particles.transform.position = pos;
                if (!particles.activeSelf) particles.SetActive(true);
            }
            else if (particles.activeSelf)
            {
                particles.SetActive(false);
            }
        }

        // global rain look (screen droplets + Volume)
        if (rainController != null) rainController.SetRain(rainy);
    }

    // deterministic random per chunk coord: a given chunk is always the same
    bool IsRainy(Vector2 coord)
    {
        var rng = new System.Random(seed ^ coord.GetHashCode());
        return rng.NextDouble() < rainChance;
    }
}
