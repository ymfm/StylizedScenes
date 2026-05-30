using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerate))]
public class mapGeneratorEditor : Editor

{
    public override void OnInspectorGUI()
    {
        MapGenerate mapGen = (MapGenerate)target;

        if(DrawDefaultInspector())
        {
            if(mapGen.autoUpdate)
            {
                mapGen.GenerateMap();
            }
        }
        if(GUILayout.Button("Generate"))
        {
            mapGen.GenerateMap();
        }
    }
}
