using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerate : MonoBehaviour
{
    public enum DrawMode{NoiseMap,ColorMap,Mesh}
    public DrawMode drawMode;
    public const int mapChunkSize = 241;
    [Range(0,6)]
    public int levelOfDetail;
    public float noiseScale;
    public bool autoUpdate;
    public int octaves;
     [Range(0,1)]
    public float persistance;
    public float lacunarity;
    public int seed;
    public Vector2 offset;
    public float heightMultiplier;
    public AnimationCurve meshHeightCurve;
    public TerrainType[] regions;

    public void GenerateMap()
    {
        float [,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize,mapChunkSize,seed,noiseScale,octaves,persistance,lacunarity,offset);
        Color[] colorsMap = new Color[mapChunkSize*mapChunkSize];
        for(int y = 0; y < mapChunkSize;y++)
        {
            for(int x = 0; x < mapChunkSize;x++)
            {
                float currentHeight = noiseMap[x,y];
                for(int i = 0; i<regions.Length;i++)
                {
                    if(currentHeight <= regions[i].heght)
                    {
                        colorsMap[y*mapChunkSize+x] = regions[i].color;
                        break;
                    }
                }
            }
        }
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeight(noiseMap));
        }else if(drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(colorsMap,mapChunkSize,mapChunkSize));
        }else if(drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap,
            heightMultiplier,meshHeightCurve,levelOfDetail),TextureGenerator.TextureFromColorMap(colorsMap,mapChunkSize,mapChunkSize));
        }
    }

    void OnValidate()
    {
        if(lacunarity<1)
        {
            lacunarity = 1;
        }

        if(octaves<0)
        {
            octaves = 0;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float heght;
    public Color color;
}