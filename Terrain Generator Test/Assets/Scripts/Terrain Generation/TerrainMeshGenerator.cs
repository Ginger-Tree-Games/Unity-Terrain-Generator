using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TerrainMeshGenerator : MonoBehaviour
{
    Mesh m_terrain_mesh;
    Vector3[] m_vertices;
    int[] m_triangles;

    public int xSize = 10;
    public int zSize = 10;

    [Tooltip("Base perlin x frequency. Value 1 is no change.")]
    public float perlinXFrequency = .3f;

    [Tooltip("Base perlin z frequency. Value 1 is no change.")]
    public float perlinZFrequency = .3f;

    [Tooltip("Amplitude of the generation.")]
    [Range(0.0F, 10.0F)]
    public float perlinAmplitude = 1f;

    [Tooltip("Enables higher detail generation that attempts to make the mesh look more realistic.")]
    public bool complexGeneration = false;

    [Tooltip("Scales the frequency of subdetail on the mesh.")]
    public float lacunarityScaler = 1f;

    [Tooltip("Scales the amplitude of the subdetails on the mesh.")]
    [Range(0.0F, 1.0F)]
    public float persistanceScaler = 1f;

    // Start is called before the first frame update
    void Start()
    {
        CreateShape();
        UpdateMesh();
    }

    void CreateShape()
    {
        if(m_terrain_mesh == null)
        {
            m_terrain_mesh = new Mesh();
        }
        GetComponent<MeshFilter>().mesh = m_terrain_mesh;

        m_vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for(int index = 0, z = 0; z < zSize + 1; z++)
        {
            for (int x = 0; x < xSize + 1; x++)
            {
                float y = Mathf.PerlinNoise(x * perlinXFrequency, z * perlinZFrequency) * perlinAmplitude;

                if(complexGeneration)
                {
                    y += Mathf.PerlinNoise(x * perlinXFrequency * lacunarityScaler, z * perlinZFrequency * lacunarityScaler) 
                        * perlinAmplitude * persistanceScaler;

                    y += Mathf.PerlinNoise(x * perlinXFrequency * (lacunarityScaler * lacunarityScaler), z * perlinZFrequency * (lacunarityScaler * lacunarityScaler))
                        * perlinAmplitude * (persistanceScaler * persistanceScaler) ;
                }

                m_vertices[index] = new Vector3(x, y, z);
                index++;
            }
        }

        m_triangles = new int[xSize * zSize * 6];

        int vertex_offset = 0;
        int vertices_done = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                m_triangles[vertices_done] = vertex_offset;
                m_triangles[vertices_done + 1] = vertex_offset + xSize + 1;
                m_triangles[vertices_done + 2] = vertex_offset + 1;
                m_triangles[vertices_done + 3] = vertex_offset + 1;
                m_triangles[vertices_done + 4] = vertex_offset + xSize + 1;
                m_triangles[vertices_done + 5] = vertex_offset + xSize + 2;

                vertex_offset++;
                vertices_done += 6;
            }
            vertex_offset++;
        }
    }

    public void UpdateMesh()
    {
        m_terrain_mesh.Clear();

        m_terrain_mesh.vertices = m_vertices;
        m_terrain_mesh.triangles = m_triangles;

        m_terrain_mesh.RecalculateNormals();
    }

    public void RecalculateMesh()
    {
        CreateShape();
        UpdateMesh();
    }
}
