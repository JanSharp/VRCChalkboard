using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SpawnPooledObjectOnInteract : UdonSharpBehaviour
    {
        public InstObjectPool pool;

        public override void Interact()
        {
            pool.Spawn();
        }
    }
}
