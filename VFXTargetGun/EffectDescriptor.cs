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

        // these 2 only exist for people who have opened the UI at some point
        private EffectButtonData buttonData;
        private VFXTargetGun gun;

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

        public void Init(VFXTargetGun gun)
        {
            this.gun = gun;
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
            var newPositionsStage = new Vector3[newLength];
            var newRotationsStage = new Quaternion[newLength];
            var newStartTimesStage = new float[newLength];
            for (int i = 0; i < count; i++)
            {
                newParticleSystemParents[i] = particleSystemParents[i];
                newParticleSystems[i] = particleSystems[i];
                newActiveEffects[i] = activeEffects[i];
                newActiveEffectIndexes[i] = activeEffectIndexes[i];
            }
            // see PlayEffect for the reason why we need a Math.Min here
            for (int i = 0; i < System.Math.Min(stagedCount, positionsStage.Length); i++)
            {
                newPositionsStage[i] = positionsStage[i];
                newRotationsStage[i] = rotationsStage[i];
                newStartTimesStage[i] = startTimesStage[i];
            }
            particleSystemParents = newParticleSystemParents;
            particleSystems = newParticleSystems;
            activeEffects = newActiveEffects;
            activeEffectIndexes = newActiveEffectIndexes;
            positionsStage = newPositionsStage;
            rotationsStage = newRotationsStage;
            startTimesStage = newStartTimesStage;
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
            PlayEffectInternal(position, rotation); // may grow arrays, so add to stage afterwards
            // just to prevent gigantic lag spikes from causing errors
            // Like i mean gigantic. for this to become an issue the person holding the gun would hve to
            // play an effect, wait for it to finish, and play it again and do that more times than the
            // current length of the positionsStage. At this point overwriting the first one in the array
            // is fine, it's already timed out anyway. It does technically lead to dropped effects in terms of syncing
            // but again, this is just to prevent errors for a very very rare edge case
            // because of the way delayed effects are played this could also lead to effects being played out of order
            // but I believe that the start time would always go into the negative anyway so all effects get played at the same time
            // but once again, rare edge case. All it needs to do is not break
            var index = (stagedCount++) % positionsStage.Length;
            positionsStage[index] = position;
            rotationsStage[index] = rotation;
            startTimesStage[index] = Time.time;
            RequestSerialization();
        }

        private void PlayEffectInternal(Vector3 position, Quaternion rotation)
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



        // incremental syncing
        private Vector3[] positionsStage;
        private Quaternion[] rotationsStage;
        private float[] startTimesStage;
        private int stagedCount;
        [UdonSynced] private Vector3[] syncedPositions;
        [UdonSynced] private Quaternion[] syncedRotations;
        [UdonSynced] private float[] syncedStartTimes;
        private const float MaxDelay = 1f;
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

        public override void OnPreSerialization()
        {
            // nothing to sync yet
            if (!particleSystemInitialized)
                return;
            syncedPositions = new Vector3[stagedCount];
            syncedRotations = new Quaternion[stagedCount];
            syncedStartTimes = new float[stagedCount];
            var time = Time.time;
            // see PlayEffect for the reason why we need a Math.Min here
            for (int i = 0; i < System.Math.Min(stagedCount, positionsStage.Length); i++)
            {
                syncedPositions[i] = positionsStage[i];
                syncedRotations[i] = rotationsStage[i];
                syncedStartTimes[i] = startTimesStage[i] - time;
            }
            stagedCount = 0;
        }

        public override void OnDeserialization()
        {
            // nothing was synced
            if (syncedPositions == null)
                return;
            InitParticleSystem();
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
