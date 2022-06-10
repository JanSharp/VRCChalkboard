using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VRCObjectPoolSpawner : UdonSharpBehaviour
    {
        public VRC.SDK3.Components.VRCObjectPool pool;

        public override void Interact() => Spawn();
        public void Spawn()
        {
            Networking.SetOwner(Networking.LocalPlayer, pool.gameObject);
            pool.TryToSpawn();
        }
    }
}
