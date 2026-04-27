using UnityEngine;
using UnityEngine.Rendering.Universal;

public class RainController : MonoBehaviour
{
    public ScriptableRendererData rendererData;
    public string featureName = "FullScreenPassRendererFeature";
    public bool rainEnabled = true;
    
    void OnEnable()  => Apply();
    void OnValidate() => Apply();
    
    void Apply()
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
}