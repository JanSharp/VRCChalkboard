
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class UpdateManager : UdonSharpBehaviour
{
    private const string internalIndexFieldName = "customUpdateInternalIndex";
    private const string customUpdateMethodName = "CustomUpdate";
    private const int initialListenersLength = 128;

    // can't use UdonBehaviour nor UdonSharpBehaviour arrays because it's not supported
    private Component[] listeners = new Component[initialListenersLength];
    private int listenerCount = 0;

    private void Update()
    {
        for (int i = 0; i < listenerCount; i++)
        {
            ((UdonSharpBehaviour)listeners[i]).SendCustomEvent(customUpdateMethodName);
        }
    }

    public void Register(UdonSharpBehaviour listener)
    {
        if (listenerCount == listeners.Length)
            GrowListeners();
        listeners[listenerCount] = listener;
        listener.SetProgramVariable(internalIndexFieldName, listenerCount);
        listenerCount++;
    }

    public void Deregister(UdonSharpBehaviour listener)
    {
        int index = (int)listener.GetProgramVariable(internalIndexFieldName);
        // move current top into the gap
        listenerCount--;
        listeners[index] = listeners[listenerCount];
        ((UdonSharpBehaviour)listeners[index]).SetProgramVariable(internalIndexFieldName, index);
        listeners[listenerCount] = null;
    }

    private void GrowListeners()
    {
        Component[] grownListeners = new Component[listeners.Length * 2];
        for (int i = 0; i < listeners.Length; i++)
            grownListeners[i] = listeners[i];
        listeners = grownListeners;
    }
}
