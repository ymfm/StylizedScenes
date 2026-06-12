using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

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
            EditorUtility.SetDirty(target);
        }
    }
}
