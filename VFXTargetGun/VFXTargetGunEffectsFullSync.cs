using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class VFXTargetGunEffectsFullSync : UdonSharpBehaviour
    {
        // set by EffectDescriptor's OnBuild
        [SerializeField] [HideInInspector] public EffectDescriptor descriptor;

        [UdonSynced] private ulong[] syncedData;
        [UdonSynced] private Vector3[] syncedPositions;
        [UdonSynced] private Quaternion[] syncedRotations;
        [UdonSynced] private uint currentTopOrder;

        public override void OnPreSerialization()
        {
            var isToggle = descriptor.IsToggle;
            var count = descriptor.ActiveCount;
            syncedData = new ulong[count];
            syncedPositions = new Vector3[count];
            syncedRotations = new Quaternion[count];
            currentTopOrder = descriptor.currentTopOrder;

            int syncedI = 0;
            for (int i = 0; i < descriptor.MaxCount; i++)
            {
                if (descriptor.ActiveEffects[i])
                {
                    syncedData[syncedI] = descriptor.CombineSyncedData(
                        0,
                        i,
                        descriptor.HasParticleSystems ? descriptor.ParticleSystems[i][0].time : 0f,
                        descriptor.ActiveEffects[i],
                        descriptor.EffectOrder[i]
                    );
                    syncedPositions[syncedI] = descriptor.EffectParents[i].position;
                    syncedRotations[syncedI] = descriptor.EffectParents[i].rotation;
                    if (++syncedI == count)
                        break;
                }
            }
        }

        public override void OnDeserialization()
        {
            descriptor.syncedData = syncedData;
            descriptor.syncedPositions = syncedPositions;
            descriptor.syncedRotations = syncedRotations;
            descriptor.currentTopOrder = currentTopOrder;
            descriptor.OnDeserialization();
        }
    }
}
