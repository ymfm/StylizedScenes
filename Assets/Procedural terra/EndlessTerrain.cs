using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class EndlessTerrain : MonoBehaviour
{
    const float scale = 5f;
    const float viewerMoveThreshholdForChunkUpdate = 25f;
    const float sqrViewerMoveThreshholdForChunkUpdate = viewerMoveThreshholdForChunkUpdate*viewerMoveThreshholdForChunkUpdate;
    public LODInfo[] detailLevels;
    public static float maxViewDst = 450;
    public Transform viewer;
    static MapGenerate mapGenerate;
    public static Vector2 viewerPosition;
    public Vector2 viewerPositionOld;
    private int chunkSize;
    private int chunkVisibleInViewDst;
    public Material mapMaterial;
    Dictionary<Vector2,TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2,TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    
    void Start()
    {
        mapGenerate = FindAnyObjectByType<MapGenerate>();

        maxViewDst = detailLevels[detailLevels.Length-1].visibleDsThresh;
        chunkSize = MapGenerate.mapChunkSize -1;
        chunkVisibleInViewDst = Mathf.RoundToInt(maxViewDst/chunkSize);
        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x,viewer.position.z)/scale;
        if((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThreshholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        for(int i = 0; i < terrainChunksVisibleLastUpdate.Count; i ++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }

        terrainChunksVisibleLastUpdate.Clear();

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
                    terrainChunkDictionary.Add(viewedChunkCoord,new TerrainChunk(viewedChunkCoord,chunkSize,detailLevels,transform,mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        Vector2 position;
        GameObject meshObject;
        Bounds bounds;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        LODInfo[] DetailLevels;
        LODMesh[] LODMeshes;
        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord,int size,LODInfo[] detailLevels,Transform parent,Material material)
        {
            this.DetailLevels = detailLevels;
            position = coord * size;
            bounds = new Bounds(position,Vector2.one*size);
            Vector3 positionV3 = new Vector3(position.x,0,position.y);
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);

            LODMeshes = new LODMesh[detailLevels.Length];
            for(int i = 0;i<detailLevels.Length;i++)
            {
                LODMeshes[i] = new LODMesh(detailLevels[i].lod,UpdateTerrainChunk);
            }
            mapGenerate.RequestMapData(position,OnMapDataReceived);
        }
        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.color,MapGenerate.mapChunkSize,MapGenerate.mapChunkSize);
            meshRenderer.material.mainTexture = texture;
            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk() {
            if(mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDst;
                if(visible)
                {
                    int lodIndex = 0;
                    for(int i = 0; i<DetailLevels.Length-1; i++)
                    {
                        if(viewerDstFromNearestEdge>DetailLevels[i].visibleDsThresh)
                        {
                            lodIndex = i + 1;
                        }else
                        {
                            break;
                        }
                    }
                    if(lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = LODMeshes[lodIndex];
                        if(lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }else if(!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                    terrainChunksVisibleLastUpdate.Add(this);
                }
                SetVisible(visible);
            }

        }
        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updataCallback;
        public LODMesh(int lod,System.Action updataCallback)
        {
            this.lod = lod;
            this.updataCallback = updataCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreatMesh();
            hasMesh = true;
            updataCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerate.RequestMeshData(mapData,lod,OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDsThresh;

    }
}
