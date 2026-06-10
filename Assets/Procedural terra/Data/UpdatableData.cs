using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UpdatableData : ScriptableObject
{
    public event System.Action OnValuesUpdata;
    public bool autoUpdata;

    protected virtual void OnValidate()
    {
        if (autoUpdata)
        {
            NotifyOfUpdataValues();
        }
    }

    public void NotifyOfUpdataValues()
    {
        if(OnValuesUpdata != null)
        {
            OnValuesUpdata();
        }
    }
}
