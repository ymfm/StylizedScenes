 using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D texture)
    {
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width,1,texture.height);
    }
    public void DrawMesh(MeshData meshData,Texture2D tex)
    {
        meshFilter.sharedMesh = meshData.CreatMesh();
        meshRenderer.sharedMaterial.mainTexture = tex;
    }
}
