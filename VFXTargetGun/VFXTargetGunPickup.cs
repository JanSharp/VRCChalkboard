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

        public override void OnPickupUseDown() => gun.UseSelectedEffect();

        public override void OnPickup() => gun.IsHeld = true;

        public override void OnDrop() => gun.IsHeld = false;
    }
}
