using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UpdatableData : ScriptableObject
{
    public event System.Action OnValuesUpdata;
    public bool autoUpdata;
    #if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (autoUpdata)
        {
            UnityEditor.EditorApplication.update += NotifyOfUpdataValues;
        }
    }

    public void NotifyOfUpdataValues()
    {
        UnityEditor.EditorApplication.update -= NotifyOfUpdataValues;
        if(OnValuesUpdata != null)
        {
            OnValuesUpdata();
        }
    }
    #endif
}
