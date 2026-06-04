using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier,AnimationCurve curve,int levelOfDetail)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width-1)/-2f;
        float topLeftZ = (width-1)/-2f;
        AnimationCurve heightCurve = new AnimationCurve (curve.keys);
        int meshSimplificationIncrement = (levelOfDetail == 0)?1:levelOfDetail * 2;
        int verticsePerline = (width-1)/meshSimplificationIncrement + 1;
 
        MeshData meshData = new MeshData(verticsePerline,verticsePerline);
        int vertexIndex = 0;

        for(int y = 0; y<height ;y+= meshSimplificationIncrement)
        {
            for(int x = 0; x<width ;x+= meshSimplificationIncrement)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x,heightCurve.Evaluate(heightMap[x,y])*heightMultiplier, topLeftZ + y);

                meshData.uvs[vertexIndex] = new Vector2(x/(float)width,y/(float)height);
                if(x<width-1&&y<height-1)
                {
                    meshData.AddTriangle(vertexIndex,vertexIndex+ verticsePerline +1,vertexIndex+verticsePerline);
                    meshData.AddTriangle(vertexIndex+verticsePerline+1,vertexIndex,vertexIndex+1);
                }
                vertexIndex++;

            }
        }
        return meshData;
    }
}
public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    public int triangleIndex;
    public MeshData(int meshWidth,int meshHeight)
    {
        vertices = new Vector3[meshWidth*meshHeight];
        uvs = new Vector2[meshWidth*meshHeight];
        triangles = new int[(meshWidth-1)*(meshHeight-1)*6];
    }

    public void AddTriangle(int a,int b,int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    } 

    public Mesh CreatMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateBounds();
        return mesh;
    }
}
