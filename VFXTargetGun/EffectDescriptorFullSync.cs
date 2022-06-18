using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EffectDescriptorFullSync : UdonSharpBehaviour
    {
        // set by EffectDescriptor's OnBuild
        [SerializeField] [HideInInspector] public EffectDescriptor descriptor;
    }
}
