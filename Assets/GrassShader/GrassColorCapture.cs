using UnityEngine;

// Plan A: procedurally creates a top-down orthographic camera covering a 3x3 area of
// chunks around the viewer, and renders the ground color into a RenderTexture so the
// grass shader can blend with the terrain underneath. The camera follows the viewer,
// and the world-position -> RT-UV mapping is fed to the shader via global variables.
// Usage: attach to any object (e.g. mapCreator) and assign terrainGenerator; the
// camera is created automatically, no manual setup required.
public class GrassColorCapture : MonoBehaviour
{
    [Header("References")]
    public TerrainGenerator terrainGenerator; // reuses its viewer and meshSettings

    [Header("Capture Settings")]
    public int chunksToCover = 3;        // 3x3
    [HideInInspector] public int textureResolution = 1024;
    public float cameraHeight = 300f;    // height above the ground
    public LayerMask terrainLayer = ~0;  // render only the terrain layer
    public Color background = new Color(0.4f, 0.5f, 0.3f, 1f); // fallback ground color where nothing is captured

    [Tooltip("Optional: leave empty to auto-create, or assign your own RenderTexture asset to inspect/reuse it in the Project")]
    public RenderTexture renderTexture;

    [HideInInspector]
    public Vector3 cameraEuler = new Vector3(90f, 0f, 0f); // (90,0,0) = straight down

    Camera cam;
    bool createdRT;

    void Start()
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(textureResolution, textureResolution, 16, RenderTextureFormat.ARGB32)
            {
                name = "GrassColorRT",
                wrapMode = TextureWrapMode.Clamp
            };
            createdRT = true;
        }

        // procedurally create the dedicated capture camera
        var camGO = new GameObject("GrassColorCamera (auto)");
        camGO.transform.SetParent(transform, false);
        camGO.transform.rotation = Quaternion.Euler(cameraEuler);

        cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.cullingMask = terrainLayer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = cameraHeight * 2f;
        cam.targetTexture = renderTexture;

        // provide the RT to the grass shader as a global texture (sample _GrassColorTex)
        Shader.SetGlobalTexture("_GrassColorTex", renderTexture);
    }

    void LateUpdate()
    {
        if (cam == null || terrainGenerator == null ||
            terrainGenerator.viewer == null || terrainGenerator.meshSettings == null) return;

        Transform viewer = terrainGenerator.viewer;
        float captureSize = chunksToCover * terrainGenerator.meshSettings.meshWorldSize;
        cam.orthographicSize = captureSize * 0.5f;

        // snap to the texel grid to avoid the RT content shimmering as the camera moves
        float texel = captureSize / textureResolution;
        float cx = Mathf.Round(viewer.position.x / texel) * texel;
        float cz = Mathf.Round(viewer.position.z / texel) * texel;
        cam.transform.position = new Vector3(cx, cameraHeight, cz);
        cam.transform.rotation = Quaternion.Euler(cameraEuler);

        // feed the grass shader: capture center (world xz) and coverage size, used to map world pos -> RT UV
        Shader.SetGlobalVector("_GrassCaptureCenter", new Vector4(cx, 0f, cz, 0f));
        Shader.SetGlobalFloat("_GrassCaptureSize", captureSize);
    }

    void OnDestroy()
    {
        if (createdRT && renderTexture != null) renderTexture.Release();
        if (cam != null) Destroy(cam.gameObject);
    }
}
