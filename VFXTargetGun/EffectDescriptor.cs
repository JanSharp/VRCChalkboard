using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EffectDescriptor : UdonSharpBehaviour
    {
        public string effectName;
        private EffectButtonData data;
        private VFXTargetGun gun;

        public void Init(VFXTargetGun gun)
        {
            this.gun = gun;
            var button = VRCInstantiate(gun.buttonPrefab);
            button.transform.SetParent(gun.buttonGrid, false);
            data = (EffectButtonData)button.GetComponent(typeof(UdonBehaviour));
            data.descriptor = this;
            data.text.text = effectName;
        }

        public void SelectThisEffect()
        {
            Debug.Log($"Selected {effectName}");
        }
    }
}
