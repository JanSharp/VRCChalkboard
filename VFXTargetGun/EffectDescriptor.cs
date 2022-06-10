using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EffectDescriptor : UdonSharpBehaviour
    {
        [SerializeField] private string effectName;
        public string EffectName => effectName;

        private GameObject originalParticleSystemParent;
        private Transform[] particleSystemParents;
        private ParticleSystem[] particleSystems;
        private int count;
        private bool[] activeEffects;
        private int[] activeEffectIndexes;
        private int activeCount;
        public int ActiveCount
        {
            get => activeCount;
            private set
            {
                if (activeCount == 0 || value == 0)
                {
                    activeCount = value;
                    // only update colors if the references to the gun and the UI even exists
                    if (gun != null)
                        UpdateColors();
                }
                else
                    activeCount = value;
            }
        }
        private float effectDuration;
        private bool loop;
        public bool Loop => loop;

        private bool selected;
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                UpdateColors();
            }
        }

        // these 3 are only set for people who have opened the UI at some point
        private EffectButtonData buttonData;
        private VFXTargetGun gun;
        public int Index { get; private set; }

        private void UpdateColors()
        {
            // update button color and style
            buttonData.text.text = Selected ? ("<u>" + effectName + "</u>") : effectName;
            if (ActiveCount == 0)
                buttonData.button.colors = loop ? gun.InactiveLoopColor : gun.InactiveColor;
            else
                buttonData.button.colors = loop ? gun.ActiveLoopColor : gun.ActiveColor;
            
            if (loop)
                buttonData.stopButton.gameObject.SetActive(activeCount != 0);

            // update the gun if this is the currently selected effect
            if (Selected)
                gun.UpdateColors();
        }

        public void Init(VFXTargetGun gun, int index)
        {
            this.gun = gun;
            Index = index;
            InitParticleSystem(); // init first so the button knows what color to use (looped or not)
            MakeButton();
        }

        private bool particleSystemInitialized;
        private void InitParticleSystem()
        {
            if (particleSystemInitialized)
                return;
            particleSystemInitialized = true;
            particleSystemParents = new Transform[4];
            particleSystems = new ParticleSystem[4];
            activeEffects = new bool[4];
            activeEffectIndexes = new int[4];
            positionsStage = new Vector3[4];
            rotationsStage = new Quaternion[4];
            startTimesStage = new float[4];
            var psParent = this.transform.GetChild(0);
            originalParticleSystemParent = psParent.gameObject;
            var ps = psParent.GetChild(0).GetComponent<ParticleSystem>();
            var psMain = ps.main;
            // I have no idea what `psMain.startLifetimeMultiplier` actually means. It clearly isn't a multiplier.
            // it might also only apply to curves, but I don't know what to do with that information
            // basically, it gives me a 5 when the lifetime is 5. I set it to 2, it gives me a 2 back, as expected, but it also set the constant lifetime to 2.
            // that is not how a multiplier works
            effectDuration = psMain.duration + psMain.startLifetime.constantMax;
            loop = ps.main.loop;
            if (loop)
            {
                particleSystemParents[0] = psParent;
                particleSystems[0] = ps;
                count = 1;
            }
            else
            {
                count = 0;
            }
        }

        private void MakeButton()
        {
            var button = VRCInstantiate(gun.ButtonPrefab);
            button.transform.SetParent(gun.ButtonGrid, false);
            buttonData = (EffectButtonData)button.GetComponent(typeof(UdonBehaviour));
            buttonData.descriptor = this;
            buttonData.text.text = effectName;
            UpdateColors();
        }

        public void SelectThisEffect()
        {
            var toggle = gun.KeepOpenToggle; // UdonSharp being picky and weird
            if (!toggle.isOn)
                gun.CloseUI();
            gun.SelectedEffect = this;
        }

        private void GrowArrays()
        {
            var newLength = particleSystemParents.Length * 2;
            var newParticleSystemParents = new Transform[newLength];
            var newParticleSystems = new ParticleSystem[newLength];
            var newActiveEffects = new bool[newLength];
            var newActiveEffectIndexes = new int[newLength];
            for (int i = 0; i < count; i++)
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
            // HACK: workaround for VRChat's weird behaviour when instantiating a copy of an existing object in the world.
            // modifying the position and rotation of the copy ends up modifying the original one for some reason
            // also the particle system Play call doesn't seem to go off on the copy
            // but when using the copy at a later point in time where it is accessed the same way through the arrays it does behave as a copy
            // which ultimately just makes me believe it is VRCInstantiate not behaving. So, my solution is to simply not use the original object
            // except for creating copies of it and then modifying the copies. It's a waste of memory and performance
            // and an unused game object in the world but what am I supposed to do
            var obj = VRCInstantiate(originalParticleSystemParent);
            var transform = obj.transform;
            transform.parent = this.transform;
            particleSystemParents[count] = transform;
            particleSystems[count++] = transform.GetChild(0).GetComponent<ParticleSystem>();
        }

        public void PlayEffect(Vector3 position, Quaternion rotation)
        {
            PlayEffectInternal(position, rotation);
            if (loop)
            {
                if (ActiveCount == 1)
                {
                    positionsStage[0] = position;
                    rotationsStage[0] = rotation;
                    startTimesStage[0] = Time.time;
                    stagedCount = 1;
                }
                else
                    stagedCount = 0;
            }
            else
            {
                if (stagedCount == positionsStage.Length)
                    GrowStageArrays();
                positionsStage[stagedCount] = position;
                rotationsStage[stagedCount] = rotation;
                startTimesStage[stagedCount++] = Time.time;
            }
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            RequestSerialization();
        }

        public void StopLoopEffect()
        {
            StopLoopEffectInternal();
            stagedCount = 0;
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            RequestSerialization();
        }

        private void StopLoopEffectInternal()
        {
            if (ActiveCount == 0)
                return;
            particleSystems[0].Stop();
            ActiveCount = 0;
        }

        private void PlayEffectInternal(Vector3 position, Quaternion rotation)
        {
            if (loop)
            {
                if (ActiveCount == 1)
                    StopLoopEffectInternal();
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



        // incremental syncing
        private Vector3[] positionsStage;
        private Quaternion[] rotationsStage;
        private float[] startTimesStage;
        private int stagedCount;
        [UdonSynced] private Vector3[] syncedPositions;
        [UdonSynced] private Quaternion[] syncedRotations;
        [UdonSynced] private float[] syncedStartTimes;
        private const float MaxLoopDelay = 0.15f;
        private const float MaxDelay = 0.5f;
        private Vector3[] delayedPositions;
        private Quaternion[] delayedRotations;
        private int delayedCount;

        private bool incrementalSyncingInitialized;
        private void InitIncrementalSyncing()
        {
            if (incrementalSyncingInitialized)
                return;
            incrementalSyncingInitialized = true;
            delayedPositions = new Vector3[4];
            delayedRotations = new Quaternion[4];
        }

        private void GrowStageArrays()
        {
            var newLength = positionsStage.Length * 2;
            var newPositionsStage = new Vector3[newLength];
            var newRotationsStage = new Quaternion[newLength];
            var newStartTimesStage = new float[newLength];
            for (int i = 0; i < stagedCount; i++)
            {
                newPositionsStage[i] = positionsStage[i];
                newRotationsStage[i] = rotationsStage[i];
                newStartTimesStage[i] = startTimesStage[i];
            }
            positionsStage = newPositionsStage;
            rotationsStage = newRotationsStage;
            startTimesStage = newStartTimesStage;
        }

        public override void OnPreSerialization()
        {
            // nothing to sync yet
            if (!particleSystemInitialized)
                return;
            syncedPositions = new Vector3[stagedCount];
            syncedRotations = new Quaternion[stagedCount];
            syncedStartTimes = new float[stagedCount];
            var time = Time.time;
            for (int i = 0; i < stagedCount; i++)
            {
                syncedPositions[i] = positionsStage[i];
                syncedRotations[i] = rotationsStage[i];
                syncedStartTimes[i] = startTimesStage[i] - time;
            }
            if (!loop) // don't reset for loops
                stagedCount = 0;
        }

        public override void OnDeserialization()
        {
            if (syncedPositions == null)
                return;
            bool isZero = syncedPositions.Length == 0;
            // only init where there is actually an effect being played
            // to save some performance specifically when loading into the world
            if (!isZero)
                InitParticleSystem();
            if (loop) // if it isn't initialized it won't enter this block
            {
                if (isZero)
                    StopLoopEffectInternal();
                else
                {
                    PlayEffectInternal(syncedPositions[0], syncedRotations[0]);
                    particleSystems[0].time = Mathf.Max(0f, -syncedStartTimes[0] - MaxLoopDelay);
                }
                return;
            }
            else if (isZero) // and if it isn't initialized then this will just return
                return;
            InitIncrementalSyncing();
            float delay = Mathf.Min(-syncedStartTimes[0], MaxDelay);
            for (int i = 0; i < syncedPositions.Length; i++)
            {
                float startTime = syncedStartTimes[i] + delay;
                if (startTime <= 0f)
                    PlayEffectInternal(syncedPositions[i], syncedRotations[i]);
                else
                {
                    if (delayedCount == delayedPositions.Length)
                        GrowDelayedArrays();
                    delayedPositions[delayedCount] = syncedPositions[i];
                    delayedRotations[delayedCount++] = syncedRotations[i];
                    SendCustomEventDelayedSeconds(nameof(PlayEffectDelayed), startTime);
                }
            }
        }

        private void GrowDelayedArrays()
        {
            int newLength = delayedPositions.Length * 2;
            var newDelayedPositions = new Vector3[newLength];
            var newDelayedRotations = new Quaternion[newLength];
            for (int i = 0; i < delayedCount; i++)
            {
                newDelayedPositions[i] = delayedPositions[i];
                newDelayedRotations[i] = delayedRotations[i];
            }
            delayedPositions = newDelayedPositions;
            delayedRotations = newDelayedRotations;
        }

        public void PlayEffectDelayed()
        {
            PlayEffectInternal(delayedPositions[0], delayedRotations[0]);
            for (int i = 1; i < delayedCount; i++)
            {
                delayedPositions[i - 1] = delayedPositions[i];
                delayedRotations[i - 1] = delayedRotations[i];
            }
            delayedCount--;
        }
    }
}
