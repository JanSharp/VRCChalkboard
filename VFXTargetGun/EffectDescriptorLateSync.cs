using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EffectDescriptorLateSync : UdonSharpBehaviour
    {
        public EffectDescriptor descriptor;
    }
}
