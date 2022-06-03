
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SyncedToggleOnChange : UdonSharpBehaviour
{
    [UdonSynced]
    [FieldChangeCallback(nameof(State))]
    private bool state;
    private bool State
    {
        get => state;
        set
        {
            if (value == state)
                return;
            state = value;
            if (state) // do something if it just got flipped to true
            {

            }
            else // do something if it just got flipped to false
            {

            }
        }
    }

    public void Toggle()
    {
        State = !State;
        RequestSerialization();
    }
}
