using UnityEngine;

public class Snow : MonoBehaviour
{
    [Header("RT 设置")]
    public RenderTexture RT;
    public Texture drawImg;         // 脚印贴图（白色圆点 / 鞋印）

    [Header("角色跟踪")]
    public Transform character;     // 要跟踪的角色

    [Header("脚印参数")]
    public float stampSize = 64f;           // 脚印大小（像素）
    public Color stampColor = Color.white;  // 脚印颜色
    public float stepDistance = 0.3f;       // 走多远盖一个新脚印（世界单位）

    [Header("Raycast 参数")]
    public float rayStartHeight = 1f;       // 从角色上方多高发射线
    public float rayMaxDistance = 3f;       // 射线最大距离

    [Header("淡出参数")]
    [Range(0.9f, 1.0f)]
    public float fadeAmount = 0.995f;       // 每帧衰减系数（1=不淡出，越小淡得越快）

    // 内部状态
    private Vector3 lastStampPos;
    private Material fadeMaterial;
    private RenderTexture tempRT;

    void Start()
    {
        // 把 RT 设给雪地材质
        GetComponent<Renderer>().material.SetTexture("_MainTex", RT);

        // 创建淡化材质
        Shader fadeShader = Shader.Find("Hidden/FadeShader");
        if (fadeShader == null)
        {
            Debug.LogError("找不到 Hidden/FadeShader！请确认 FadeShader.shader 已创建。");
            return;
        }
        fadeMaterial = new Material(fadeShader);

        // 创建临时 RT（淡化时需要双缓冲）
        tempRT = new RenderTexture(RT.width, RT.height, 0, RT.format);

        // 清空 RT
        ClearRT(RT, Color.black);

        // 记录角色初始位置
        if (character != null)
            lastStampPos = character.position;
    }

    void Update()
    {
        if (character == null) return;

        // 1. 淡化整张 RT
        FadeRT();

        // 2. 检查角色是否走了足够远
        float dist = Vector3.Distance(character.position, lastStampPos);
        if (dist >= stepDistance)
        {
            TryStampAtCharacter();
            lastStampPos = character.position;
        }
    }

    void TryStampAtCharacter()
    {
        // 从角色上方向下发射线
        Vector3 rayOrigin = character.position + Vector3.up * rayStartHeight;
        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance))
        {
            // 确认打中的是雪地本身（不是其他 Collider）
            if (hit.collider.gameObject != this.gameObject) return;

            // 拿到 UV 并转成 RT 像素坐标
            float x = hit.textureCoord.x * RT.width;
            float y = (1 - hit.textureCoord.y) * RT.height;

            DrawStamp(x, y);
        }
    }

    void FadeRT()
    {
        fadeMaterial.SetFloat("_FadeAmount", fadeAmount);
        Graphics.Blit(RT, tempRT, fadeMaterial);
        Graphics.Blit(tempRT, RT);
    }

    void DrawStamp(float x, float y)
    {
        RenderTexture.active = RT;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, RT.width, RT.height, 0);

        x -= stampSize * 0.5f;
        y -= stampSize * 0.5f;
        Rect rect = new Rect(x, y, stampSize, stampSize);

        Graphics.DrawTexture(rect, drawImg, new Rect(0, 0, 1, 1), 0, 0, 0, 0, stampColor);

        GL.PopMatrix();
        RenderTexture.active = null;
    }

    void ClearRT(RenderTexture rt, Color color)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, color);
        RenderTexture.active = prev;
    }

    void OnDestroy()
    {
        if (tempRT != null) tempRT.Release();
        if (fadeMaterial != null) Destroy(fadeMaterial);
    }
}