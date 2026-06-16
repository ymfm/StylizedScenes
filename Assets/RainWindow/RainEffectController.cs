using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RainController : MonoBehaviour
{
    [Header("RendererFeature")]
    public ScriptableRendererData rendererData;
    public string featureName = "FullScreenPassRendererFeature";
    
    [Header("Volume")]
    public Volume globalVolume;
    
    [Header("Toggle")]
    public bool rainEnabled = true;
    
    void OnEnable()   => Apply();
    void OnValidate() => Apply();

    // 物体被禁用(离开下雨地块)时,强制关掉全屏滤镜,避免残留。
    // rainEnabled 本身不变,下次启用时仍按它应用。
    void OnDisable()
    {
        bool prev = rainEnabled;
        rainEnabled = false;
        Apply();
        rainEnabled = prev;
    }

    // 运行时开关雨(进入/离开下雨地块时调用)
    public void SetRain(bool on)
    {
        if (rainEnabled == on) return;
        rainEnabled = on;
        Apply();
    }

    void Apply()
    {
        ApplyRendererFeature();
        ApplyVolumeOverrides();
    }
    
    void ApplyRendererFeature()
    {
        if (rendererData == null) return;
        
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature.name == featureName)
            {
                if (feature.isActive != rainEnabled)
                {
                    feature.SetActive(rainEnabled);
                    rendererData.SetDirty();
                }
                return;
            }
        }
    }
    
    void ApplyVolumeOverrides()
    {
        if (globalVolume == null) return;

        // Toggle the whole Volume component on/off (controls all of its overrides at once)
        globalVolume.enabled = rainEnabled;
    }
}