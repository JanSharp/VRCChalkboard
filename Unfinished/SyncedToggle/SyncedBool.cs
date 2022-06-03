
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SyncedBool : UdonSharpBehaviour
{
    [HideInInspector] public string fieldName;
    [HideInInspector] public UdonSharpBehaviour targetUdonBehaviour;

    [UdonSynced]
    [FieldChangeCallback(nameof(Value))]
    private bool value;
    private bool oldValue;
    public bool OldValue => oldValue;
    public bool NewValue => value;
    public bool Value
    {
        get => value;
        set
        {
            oldValue = this.value;
            this.value = value;
            targetUdonBehaviour.SendCustomEvent($"On{fieldName}Changed");
        }
    }

    /// <summary>
    /// <para>Use this in `Start()` to set the initial value, since you can't do it when declaring the variable.</para>
    /// <para>This bypasses the OnChange event</para>
    /// </summary>
    public void SetInitialValue(bool value)
    {
        this.value = value;
    }
}
