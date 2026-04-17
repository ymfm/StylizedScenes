using UnityEngine;

[ExecuteInEditMode]
public class GrassInteract : MonoBehaviour
{
    private string shaderVariableName = "_Player";
    public bool debugMode;
    void Update()
    {
        Shader.SetGlobalVector("_Player", this.transform.position);
        //Debug.Log(this.transform.position);
    }
}