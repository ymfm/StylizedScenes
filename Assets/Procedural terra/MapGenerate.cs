using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

public class MapGenerate : MonoBehaviour
{   
    public enum DrawMode{NoiseMap,ColorMap,Mesh,FallofMap}
    public DrawMode drawMode;
    public Noise.NormalizeMode normalizeMode;
    public const int mapChunkSize = 241;
    [Range(0,6)]
    public int editorPerviewLOD;
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
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
    MapData GenerateMapData(Vector2 centre )
    {
        float [,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize,mapChunkSize,seed,noiseScale,octaves,persistance,lacunarity,centre+offset,normalizeMode);
        Color[] colorsMap = new Color[mapChunkSize*mapChunkSize];
        for(int y = 0; y < mapChunkSize;y++)
        {
            for(int x = 0; x < mapChunkSize;x++)
            {
                float currentHeight = noiseMap[x,y];
                for(int i = 0; i<regions.Length;i++)
                {
                    if(currentHeight >= regions[i].heght)
                    {
                        colorsMap[y*mapChunkSize+x] = regions[i].color;
                    } else
                    {
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap,colorsMap);
    }

    public void RequestMapData(Vector2 centre,Action<MapData> callBack)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre,callBack);
        };

        new Thread(threadStart).Start();
    }
    void MapDataThread(Vector2 centre,Action<MapData> callBack)
    {
        MapData mapData = GenerateMapData(centre);
        lock(mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callBack,mapData));
        }
    }

    public void RequestMeshData(MapData mapData,int lod,Action<MeshData> callback)
    {
            ThreadStart threadStart = delegate
            {
                MeshDataThread(mapData,lod,callback);
            };

            new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData,int lod,Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap,heightMultiplier,meshHeightCurve,lod);
        lock(meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
        }
    }

    void Update()
    {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0;i<mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if(meshDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0;i<meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeight(mapData.heightMap));
        }else if(drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.color,mapChunkSize,mapChunkSize));
        }else if(drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap,
                heightMultiplier,meshHeightCurve,editorPerviewLOD
                ),TextureGenerator.TextureFromColorMap(mapData.color,mapChunkSize,mapChunkSize));
        }else if(drawMode == DrawMode.FallofMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeight(FallOffGenerator.GenerateFalloffMap(mapChunkSize)));
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

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback,T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
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

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] color;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.color = colorMap;
    }
}