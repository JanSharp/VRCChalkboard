using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EffectDescriptor : UdonSharpBehaviour
    {
        [SerializeField] private string effectName;

        private Transform[] particleSystemParents;
        private ParticleSystem[] particleSystems;
        private int count;
        private float duration;
        private bool loop;

        private EffectButtonData data;
        private VFXTargetGun gun;

        public void Init(VFXTargetGun gun)
        {
            this.gun = gun;
            // make button
            var button = VRCInstantiate(gun.ButtonPrefab);
            button.transform.SetParent(gun.ButtonGrid, false);
            data = (EffectButtonData)button.GetComponent(typeof(UdonBehaviour));
            data.descriptor = this;
            data.text.text = effectName;
            // get particle system
            particleSystemParents = new Transform[4];
            particleSystems = new ParticleSystem[4];
            var psParent = this.transform.GetChild(0);
            particleSystemParents[0] = psParent;
            particleSystems[0] = psParent.GetChild(0).GetComponent<ParticleSystem>();
            count = 1;
        }

        public void SelectThisEffect()
        {
            gun.SetUIActive(false);
            gun.SelectedEffect = this;
        }

        public void Use(Transform target)
        {
            particleSystemParents[0].SetPositionAndRotation(target.position, target.rotation);
            particleSystems[0].Play();
        }
    }
}
