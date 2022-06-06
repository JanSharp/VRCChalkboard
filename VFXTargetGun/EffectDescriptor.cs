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
        private bool[] activeEffects;
        private int[] activeEffectIndexes;
        private int activeCount;
        private float effectDuration;
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
            activeEffects = new bool[4];
            activeEffectIndexes = new int[4];
            var psParent = this.transform.GetChild(0);
            particleSystemParents[0] = psParent;
            var ps = psParent.GetChild(0).GetComponent<ParticleSystem>();
            particleSystems[0] = ps;
            var psMain = ps.main;
            // I have no idea what `psMain.startLifetimeMultiplier` actually means. It clearly isn't a multiplier.
            // it might also only apply to curves, but I don't know what to do with that information
            // basically, it gives me a 5 when the lifetime is 5. I set it to 2, it gives me a 2 back, as expected, but it also set the constant lifetime to 2.
            // that is not how a multiplier works
            effectDuration = psMain.duration + psMain.startLifetime.constantMax;
            loop = ps.main.loop;
            count = 1;
        }

        public void SelectThisEffect()
        {
            gun.SetUIActive(false);
            gun.SelectedEffect = this;
        }

        private void GrowArrays()
        {
            var length = particleSystemParents.Length;
            var newLength = length * 2;
            var newParticleSystemParents = new Transform[newLength];
            var newParticleSystems = new ParticleSystem[newLength];
            var newActiveEffects = new bool[newLength];
            var newActiveEffectIndexes = new int[newLength];
            for (int i = 0; i < length; i++)
            {
                newParticleSystemParents[i] = particleSystemParents[i];
                newParticleSystems[i] = particleSystems[i];
                newActiveEffects[i] = activeEffects[i];
                newActiveEffectIndexes[i] = activeEffectIndexes[i];
            }
            particleSystemParents = newParticleSystemParents;
            particleSystems = newParticleSystems;
            activeEffects = newActiveEffects;
            activeEffectIndexes = newActiveEffectIndexes;
        }

        private void CreateNewEffect()
        {
            if (count == particleSystemParents.Length)
                GrowArrays();
            var obj = VRCInstantiate(particleSystemParents[0].gameObject);
            var transform = obj.transform;
            transform.parent = this.transform;
            particleSystemParents[count] = transform;
            particleSystems[count++] = transform.GetChild(0).GetComponent<ParticleSystem>();
        }

        public void PlayEffect(Vector3 position, Quaternion rotation)
        {
            int index;
            if (activeCount == count)
            {
                index = count;
                CreateNewEffect();
            }
            else
            {
                index = 0; // just to make C# compile, since the below loop should always find an index anyway
                // find first inactive effect
                for (int i = 0; i < count; i++)
                    if (!activeEffects[i])
                    {
                        index = i;
                        break;
                    }
            }
            activeEffects[index] = true;
            activeEffectIndexes[activeCount++] = index;
            particleSystemParents[index].SetPositionAndRotation(position, rotation);
            particleSystems[index].Play();
            this.SendCustomEventDelayedSeconds(nameof(FinishEffect), effectDuration);
        }

        public void FinishEffect()
        {
            int index = activeEffectIndexes[0];
            for (int i = 1; i < activeCount; i++)
                activeEffectIndexes[i - 1] = activeEffectIndexes[i];
            activeCount--;
            activeEffects[index] = false;
        }
    }
}
