using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class WorldGenHandler : MonoBehaviour
{
    public const float MAX_DISTANCE = 1000;  

    [Min(0.00001f)]
    public static float TerrainScale = 1f;

    public LODInfo[] detailLevels;

    const float movementThresholdCheck = 25f;
    const float sqrMovementThresholdCheck = movementThresholdCheck * movementThresholdCheck;

    public static float maxViewDistance = 400f;
    public Transform viewer;

    public static Vector2 viewerPosition;

    Vector2 viewerPositionOld;

    public Material terrainMaterial;

    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> lastVisibleTerrainChunks = new List<TerrainChunk>();

    public static TerrainMeshGenerator generator;

    // Start is called before the first frame update
    void Start()
    {
        generator = FindObjectOfType<TerrainMeshGenerator>();
        maxViewDistance = detailLevels[detailLevels.Length - 1].distanceThreshold;
        chunkSize = TerrainMeshGenerator.mapChunkSize;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / TerrainScale;

        if((viewerPositionOld - viewerPosition).sqrMagnitude > sqrMovementThresholdCheck)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        for(int i = 0; i < lastVisibleTerrainChunks.Count; i ++)
        {
            lastVisibleTerrainChunks[i].SetVisible(false);
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        //Check surrounding chunks
        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 currentChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if(terrainChunkDictionary.ContainsKey(currentChunkCoord))
                {
                    terrainChunkDictionary[currentChunkCoord].UpdateChunk();

                    
                }
                else
                {
                    //Create chunk
                    terrainChunkDictionary.Add(currentChunkCoord, new TerrainChunk(currentChunkCoord, chunkSize - 1, detailLevels, transform, terrainMaterial));
                }
            }

        }

    }

    class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer renderer;
        MeshFilter filter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            
            this.detailLevels = detailLevels;
            position = coord * size;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            bounds = new Bounds(position, Vector2.one * size);

            //meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject = new GameObject("Terrain Chunk");
            filter = meshObject.AddComponent<MeshFilter>();
            renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.material = material;

            meshObject.transform.position = positionV3 * TerrainScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * TerrainScale;

            lodMeshes = new LODMesh[detailLevels.Length];
            for(int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].LOD, UpdateChunk);
            }

            SetVisible(false);

            UpdateChunk();
        }

        void OnMeshDataRecieved(MeshData data)
        {
            Mesh new_mesh = new Mesh();

            filter.mesh = new_mesh;

            new_mesh.vertices = data.vertices;
            new_mesh.triangles = data.triangles;
            new_mesh.colors = data.colors;

            new_mesh.RecalculateNormals();
        }

        public void UpdateChunk()
        {
            float viewerDistanceFromEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromEdge <= maxViewDistance;   

            if (visible)
            {
                int lodindex = 0;
                bool searching = true;

                for (int i = 0; i < detailLevels.Length - 1 && searching; i++)
                {
                    if (viewerDistanceFromEdge > detailLevels[i].distanceThreshold)
                    {
                        lodindex = i + 1;
                    }
                    else
                    {
                        searching = false;
                    }
                }

                if (lodindex != previousLODIndex)
                {
                    LODMesh lodmesh = lodMeshes[lodindex];

                    if (lodmesh.hasMesh)
                    {
                        previousLODIndex = lodindex;
                        filter.mesh = lodmesh.mesh;
                    }
                    else if (!lodmesh.hasRequestedMesh)
                    {
                        lodmesh.RequestData(meshObject.transform.position);
                    }
                }

                lastVisibleTerrainChunks.Add(this);
            }

            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool isVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh = false;
        public bool hasMesh = false;
        int lod;

        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnDataReceived(MeshData data)
        {
            mesh = new Mesh();

            mesh.vertices = data.vertices;
            mesh.triangles = data.triangles;
            mesh.colors = data.colors;

            mesh.RecalculateNormals();

            hasMesh = true;

            updateCallback();
        }

        public void RequestData(Vector3 positionV3)
        {
            generator.RequestMeshData(OnDataReceived, positionV3, lod);
            hasRequestedMesh = true;
        }

    }

    [System.Serializable]
    public struct LODInfo
    {
        public int LOD;
        public float distanceThreshold;
    }

}