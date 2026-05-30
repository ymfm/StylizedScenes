using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerate : MonoBehaviour
{
    public int mapWidth;
    public int mapHight;
    public float noiseScale;
    public bool autoUpdate;
    public int octaves;
     [Range(0,1)]
    public float persistance;
    public float lacunarity;
    public int seed;
    public Vector2 offset;
    public TerrainType[] region;
        
    #endregion
    public void GenerateMap()
    {
        float [,] noiseMap = Noise.GenerateNoiseMap(mapWidth,mapHight,seed,noiseScale,octaves,persistance,lacunarity,offset);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.DrawNoiseMap(noiseMap);
    }

    void onValidate()
    {
        if(mapWidth < 1)
        {
            mapWidth = 1;
        }

        if(mapHight<1)
        {
            mapHight = 1;
        }

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

public struct TerrainType
{
    public string name;
    public float heght;
    public Color color;
}