using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DeSpawnOnTriggerEnter : UdonSharpBehaviour
    {
        public string namePart;

        void OnTriggerEnter(Collider other)
        {
            if (!other.name.Contains(namePart))
                return;
            var pool = other.transform.parent.parent.GetComponent<InstObjectPool>();
            pool.DeSpawn(other.gameObject);
        }
    }
}
