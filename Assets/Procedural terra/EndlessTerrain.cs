using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EndlessTerrain : MonoBehaviour
{
    public const float maxViewDst = 300;
    public Transform viewer;
    public static Vector2 viewerPosition;
    private int chunkSize;
    private int chunkVisibleInViewDst;
    Dictionary<Vector2,TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2,TerrainChunk>();

    void Start()
    {
        chunkSize = MapGenerate.mapChunkSize -1;
        chunkVisibleInViewDst = Mathf.RoundToInt(maxViewDst/chunkSize);
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x,viewer.position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x/chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y/chunkSize);

        for(int yOffset = -chunkVisibleInViewDst; yOffset<= chunkVisibleInViewDst; yOffset++)
        {
            for(int xOffset = -chunkVisibleInViewDst; xOffset<= chunkVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset,currentChunkCoordY + yOffset);

                if(terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                }else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord,new TerrainChunk(viewedChunkCoord,chunkSize));
                }
            }
        }
    }

    public class TerrainChunk
    {
        Vector2 position;
        GameObject meshObject;
        Bounds bounds;
        public TerrainChunk(Vector2 coord,int size)
        {
            position = coord * size;
            bounds = new Bounds(position,Vector2.one*size);
            Vector3 positionV3 = new Vector3(position.x,0,position.y);
            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = positionV3;
            meshObject.transform.localScale = Vector3.one*size/10f;
            SetVisible(false);
        }

        public void UpdateTerrainChunk() {
            float viewerDstFromNearestEdge =  bounds.SqrDistance(viewerPosition);
            bool visible = viewerDstFromNearestEdge <= maxViewDst;
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

    }
}
