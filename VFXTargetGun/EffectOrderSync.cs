using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EffectOrderSync : UdonSharpBehaviour
    {
        // This script is not perfect. If 2 players are interacting with the same gun at the same time
        // and they rapidly change ownership of this object back and forth, I'm certain that some order
        // could get reused which would most likely lead to dropped actions which is really bad
        // however all things considered the chance of this happening is incredibly low, so low in fact
        // that I don't think it's worth a more complex system to deal with those rare cases
        [UdonSynced] [HideInInspector] public uint currentTopOrder;
    }
}
