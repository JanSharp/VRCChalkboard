
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SyncedToggleUsingSyncedBool : UdonSharpBehaviour
{
    private SyncedBool state;

    void Start()
    {
        state.fieldName = "State";
        state.targetUdonBehaviour = this;
        // state.SetInitialValue(false); // for when the initial value isn't `false`
    }

    public void Toggle()
    {
        state.Value = !state.Value;
        RequestSerialization();
    }

    public void OnStateChanged()
    {
        if (state.OldValue == state.NewValue)
            return;
        // at this point `Value` is already updated to be the new value
        if (state.Value) // do something if it just got flipped to true
        {

        }
        else // do something if it just got flipped to false
        {

        }
    }
}
