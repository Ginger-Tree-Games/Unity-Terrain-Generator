using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainMeshGenerator : MonoBehaviour
{
    public enum NormalizeMode {Local, Global};

    public NormalizeMode mode = NormalizeMode.Local;

    Mesh m_terrain_mesh;

    public const int mapChunkSize = 121;

    /*
    [Range(0,6)]
    public int levelOfDetail;*/

    public int seed = 0;

    public int xOffset = 0;
    public int zOffset = 0;

    [Min(0.0001f)]
    [Tooltip("Base perlin x frequency. Value 1 is no change.")]
    public float perlinXFrequency = .3f;

    [Min(0.0001f)]
    [Tooltip("Base perlin z frequency. Value 1 is no change.")]
    public float perlinZFrequency = .3f;

    [Min(0.0001f)]
    [Tooltip("Amplitude of the generation.")]
    [Range(0.0001f, 10.0F)]
    public float perlinAmplitude = 1f;

    public AnimationCurve amplitudeCurve;

    public Gradient gradient;

    //[Tooltip("Enables higher detail generation that attempts to make the mesh look more realistic.")]
    //public bool complexGeneration = false;

    [Min(1)]
    public int octaves = 1;

    [Min(0.0001f)]
    [Tooltip("Scales the frequency of subdetail on the mesh.")]
    public float lacunarityScaler = 1f;

    [Tooltip("Scales the amplitude of the subdetails on the mesh.")]
    [Range(0.0001f, 1.0F)]
    public float persistanceScaler = 1f;

    NoiseGenerator noiseMaker = new NoiseGenerator();
    float[,] currentNoise;

    Queue<MapThreadInfo> m_data_queue = new Queue<MapThreadInfo>();

    // Start is called before the first frame update
    void Start()
    {

    }

    private void Update()
    {
        if(m_data_queue.Count > 0)
        {
            for(int i = 0; i < m_data_queue.Count; i++)
            {
                MapThreadInfo data = m_data_queue.Dequeue();
                data.callback(data.parameter);
            }
        }
    }

    private void OnValidate()
    {

    }

    public void RequestMeshData(Action<MeshData> callback, Vector3 positon, int levelOfDetail)
    {
        ThreadStart threadStart = delegate { MapDataThread(callback, positon, levelOfDetail); };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Action<MeshData> callback, Vector3 positon, int levelOfDetail)
    {
        MeshData data = CreateMeshData(positon, levelOfDetail);

        lock (m_data_queue)
        {
            m_data_queue.Enqueue(new MapThreadInfo(callback, data));
        }
    }

    public void BuildMeshObject(MeshData mesh_data)
    {
        //ceate new object

        if(m_terrain_mesh != null)
            m_terrain_mesh.Clear();

        m_terrain_mesh = new Mesh();

        GetComponent<MeshFilter>().mesh = m_terrain_mesh;

        m_terrain_mesh.vertices = mesh_data.vertices;
        m_terrain_mesh.triangles = mesh_data.triangles;
        //m_terrain_mesh.uv = m_uvs;
        m_terrain_mesh.colors = mesh_data.colors;
        

        m_terrain_mesh.RecalculateNormals();
    }

    public void RecalculateMesh()
    {
    }

    public void CreateMaterial(float[,] noise_map)
    {
        int width = noise_map.GetLength(0);
        int height = noise_map.GetLength(1);

        Texture2D texture = new Texture2D(width, height);

        Color[] noise_color = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                noise_color[y * width + x] = Color.Lerp(Color.black, Color.white, noise_map[y, x]);
            }
        }

        texture.SetPixels(noise_color);
        texture.Apply();

        GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texture;
        GetComponent<MeshRenderer>().transform.localScale = new Vector3(width, 1, height);
    }

    public MeshData CreateMeshData(Vector3 position, int levelOfDetail)
    {
        AnimationCurve new_curve = new AnimationCurve(amplitudeCurve.keys);

        System.Random rng = new System.Random(seed);
        Vector2[] octave_offsets = new Vector2[octaves];
        octave_offsets[0] = new Vector2(0, 0);

        float maxPossibleHeight = 0;

        //Set increment of detail to 1,2,4,6,8,10, or 12
        int simplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticesPerLine = (mapChunkSize - 1) / simplificationIncrement + 1;

        for (int i = 1; i < octaves; i++)
        {
            float x_offset = rng.Next(-10000, 10000);
            float z_offset = rng.Next(-10000, 10000);
            octave_offsets[i] = new Vector2(x_offset, z_offset);

            maxPossibleHeight += perlinAmplitude * Mathf.Pow(persistanceScaler, i - 1);
        }

        MeshData mesh_data = new MeshData(verticesPerLine, verticesPerLine, position);

        float max_local_noise = 0;
        float min_local_noise = 0;

        for (int index = 0, z = 0; z < mapChunkSize; z+= simplificationIncrement)
        {
            for (int x = 0; x < mapChunkSize; x+= simplificationIncrement)
            {
                float y = 0f;

                float noise = 0f;

                for (int i = 0; i < octaves; i++)
                {

                    noise = Mathf.PerlinNoise((x + position.x + octave_offsets[i].x) * perlinXFrequency * Mathf.Pow(lacunarityScaler, i) ,
                       (z + position.z + octave_offsets[i].y) * perlinZFrequency * Mathf.Pow(lacunarityScaler, i) ) * 2 - 1;

                    y += noise * Mathf.Pow(persistanceScaler, i);
                }

                if (y > max_local_noise)
                {
                    max_local_noise = y;
                }
                else if (y < min_local_noise)
                {
                    min_local_noise = y;
                }

                mesh_data.vertices[index] = new Vector3(x, y, z);
                

                if(x < mapChunkSize - 1 && z < mapChunkSize - 1)
                {
                    mesh_data.MakeTriangle(index, index + verticesPerLine, index + 1);
                    mesh_data.MakeTriangle(index + verticesPerLine, index + verticesPerLine + 1, index + 1);
                }

                index++;
            }
        }

        for (int index = 0, z = 0; z < mapChunkSize; z += simplificationIncrement)
        {
            for (int x = 0; x < mapChunkSize; x += simplificationIncrement)
            {
                if (mode == NormalizeMode.Local)
                {
                    mesh_data.vertices[index].y = Mathf.InverseLerp(min_local_noise, max_local_noise, mesh_data.vertices[index].y);
                }
                else
                {
                    float normalizedHieght = (mesh_data.vertices[index].y + 1f) / (2f * maxPossibleHeight / 13f);

                    mesh_data.vertices[index].y = normalizedHieght;
                }

                mesh_data.vertices[index].y = new_curve.Evaluate(mesh_data.vertices[index].y) * perlinAmplitude;

                index++;
            }
        }

        for (int index = 0, z = 0; z < mapChunkSize; z += simplificationIncrement)
        {
            for (int x = 0; x < mapChunkSize; x += simplificationIncrement)
            {
                //m_uvs[index] = new Vector2((float)x / xSize, (float)z / zSize);
                float height = Mathf.InverseLerp(min_local_noise, max_local_noise, mesh_data.vertices[index].y);

                Vector3 colorDistanceVector = new Vector3(mesh_data.vertices[index].x + position.x, 0, mesh_data.vertices[index].z + position.z);

                mesh_data.colors[index] = gradient.Evaluate(Vector3.Distance(Vector3.zero, colorDistanceVector) / (WorldGenHandler.MAX_DISTANCE));
                index++;
            }
        }

        return mesh_data;
    }

    struct MapThreadInfo
    {
        public readonly Action<MeshData> callback;

        public readonly MeshData parameter;

        public MapThreadInfo(Action<MeshData> new_callback, MeshData new_parameter)
        {
            callback = new_callback;
            parameter = new_parameter;
        }
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    public Color[] colors;
    public Vector3 position;

    int triangleIndex;

    public MeshData(int xSize, int zSize, Vector3 mesh_position)
    {
        vertices = new Vector3[xSize * zSize];
        triangles = new int[(xSize-1) * (zSize-1) * 6];
        colors = new Color[vertices.Length];
        uvs = new Vector2[vertices.Length];
        triangleIndex = 0;
        position = mesh_position;
    }

    public void MakeTriangle(int a, int b,int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }
}
