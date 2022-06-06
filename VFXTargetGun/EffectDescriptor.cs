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
        private int ActiveCount
        {
            get => activeCount;
            set
            {
                if (activeCount == 0)
                {
                    if (value != 0)
                    {
                        var colors = buttonData.button.colors;
                        colors.normalColor = new Color(0, 1, 0);
                        buttonData.button.colors = colors;
                    }
                }
                else if (value == 0)
                {
                    var colors = buttonData.button.colors;
                    colors.normalColor = new Color(1, 1, 1);
                    buttonData.button.colors = colors;
                }
                activeCount = value;
            }
        }
        private float effectDuration;
        private bool loop;

        private EffectButtonData buttonData;
        private VFXTargetGun gun;

        public void Init(VFXTargetGun gun)
        {
            this.gun = gun;
            // make button
            var button = VRCInstantiate(gun.ButtonPrefab);
            button.transform.SetParent(gun.ButtonGrid, false);
            buttonData = (EffectButtonData)button.GetComponent(typeof(UdonBehaviour));
            buttonData.descriptor = this;
            buttonData.text.text = effectName;
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
            if (loop)
            {
                if (ActiveCount == 1)
                {
                    particleSystems[0].Stop();
                    ActiveCount = 0;
                }
                else
                {
                    particleSystemParents[0].SetPositionAndRotation(position, rotation);
                    particleSystems[0].Play();
                    ActiveCount = 1;
                }
                return;
            }
            // not looped, allow creating multiple
            int index;
            if (ActiveCount == count)
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
            activeEffectIndexes[ActiveCount++] = index;
            particleSystemParents[index].SetPositionAndRotation(position, rotation);
            particleSystems[index].Play();
            this.SendCustomEventDelayedSeconds(nameof(FinishEffect), effectDuration);
        }

        public void FinishEffect()
        {
            int index = activeEffectIndexes[0];
            for (int i = 1; i < ActiveCount; i++)
                activeEffectIndexes[i - 1] = activeEffectIndexes[i];
            ActiveCount--;
            activeEffects[index] = false;
        }
    }
}
