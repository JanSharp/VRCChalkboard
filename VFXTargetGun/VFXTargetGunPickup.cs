using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VFXTargetGunPickup : UdonSharpBehaviour
    {
        public VFXTargetGun gun;
        private float lastUseTime;

        public override void OnPickupUseDown()
        {
            var time = Time.time;
            if (time - lastUseTime >= 0.175f)
            {
                lastUseTime = time;
                gun.UseSelectedEffect();
            }
        }

        public override void OnPickup() => gun.IsHeld = true;

        public override void OnDrop() => gun.IsHeld = false;
    }
}
