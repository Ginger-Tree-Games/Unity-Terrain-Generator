using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainMeshGenerator))]
public class TerrainMeshGeneratorEditor : Editor
{
    private void OnSceneGUI()
    {

    }

    public override void OnInspectorGUI()
    {
        TerrainMeshGenerator mesh_generator = target as TerrainMeshGenerator;

        DrawDefaultInspector();

        if (GUILayout.Button("Update Mesh"))
        {
            mesh_generator.RecalculateMesh();
        }
    }
}
