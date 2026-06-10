using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UpdatableData),true)]
public class UpdatableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        UpdatableData data =(UpdatableData)target;
        if(GUILayout.Button("Updata"))
        {
            data.NotifyOfUpdataValues();
        }
    }
}
