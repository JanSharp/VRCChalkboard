
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SyncedToggleOnDeserialization : UdonSharpBehaviour
{
    private bool prevState;
    [UdonSynced]
    private bool state;

    private void SetState(bool state)
    {
        if (state == prevState)
            return;
        this.state = state;
        this.prevState = state;
        if (state) // do something if it just got flipped to true
        {

        }
        else // do something if it just got flipped to false
        {

        }
    }

    public void Toggle()
    {
        SetState(!state);
        RequestSerialization();
    }

    public override void OnDeserialization()
    {
        SetState(state);
    }
}
