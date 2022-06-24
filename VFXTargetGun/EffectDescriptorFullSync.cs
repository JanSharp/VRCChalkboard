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

        [UdonSynced] private int[] syncedIndexes;
        [UdonSynced] private uint[] syncedOrder;
        [UdonSynced] private Vector3[] syncedPositions;
        [UdonSynced] private Quaternion[] syncedRotations;
        [UdonSynced] private float[] syncedTimes;
        [UdonSynced] private uint currentTopOrder;

        public override void OnPreSerialization()
        {
            var isToggle = descriptor.IsToggle;
            var count = descriptor.ActiveCount;
            syncedIndexes = new int[count];
            syncedOrder = new uint[count];
            syncedPositions = new Vector3[count];
            syncedRotations = new Quaternion[count];
            syncedTimes = new float[count];
            currentTopOrder = descriptor.currentTopOrder;

            int syncedI = 0;
            for (int i = 0; i < descriptor.MaxCount; i++)
            {
                if (descriptor.ActiveEffects[i])
                {
                    syncedIndexes[syncedI] = i;
                    var order = descriptor.EffectOrder[i];
                    if (isToggle && descriptor.ActiveEffects[i])
                        order |= 0x80000000;
                    syncedOrder[syncedI] = order;
                    syncedPositions[syncedI] = descriptor.EffectParents[i].position;
                    syncedRotations[syncedI] = descriptor.EffectParents[i].rotation;
                    if (descriptor.HasParticleSystems)
                        syncedTimes[syncedI] = descriptor.ParticleSystems[i][0].time;
                    if (++syncedI == count)
                        break;
                }
            }
        }

        public override void OnDeserialization()
        {
            descriptor.syncedIndexes = syncedIndexes;
            descriptor.syncedOrder = syncedOrder;
            descriptor.syncedPositions = syncedPositions;
            descriptor.syncedRotations = syncedRotations;
            descriptor.syncedTimes = syncedTimes;
            descriptor.currentTopOrder = currentTopOrder;
            descriptor.OnDeserialization();
        }
    }
}
