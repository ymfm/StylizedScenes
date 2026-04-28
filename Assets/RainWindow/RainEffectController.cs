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
        if (globalVolume == null || globalVolume.profile == null) return;
        
        // Depth of Field
        if (globalVolume.profile.TryGet<DepthOfField>(out var dof))
        {
            dof.active = rainEnabled;
        }
        
        // Bloom
        if (globalVolume.profile.TryGet<Bloom>(out var bloom))
        {
            bloom.active = rainEnabled;
        }
    }
}