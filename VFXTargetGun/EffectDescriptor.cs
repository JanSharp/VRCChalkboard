using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
using System.Linq;
#endif

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EffectDescriptor : UdonSharpBehaviour
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
        , IOnBuildCallback
    #endif
    {
        [SerializeField] private string effectName;
        public string EffectName => effectName;
        [Tooltip(
@"The effect will always face away from/be parallel to the object it is placed on,
however by default the effect also faces away from the gun as much as possible.
When this is true said second rotation is random."
        )]
        public bool randomizeRotation;

        // set OnBuild
        [SerializeField] [HideInInspector] private int effectType;
        [SerializeField] [HideInInspector] private float effectDuration;

        private const int UnknownEffect = 0;
        private const int OnceEffect = 1;
        private const int LoopEffect = 2;
        private const int ObjectEffect = 3;
        public int EffectType => effectType;
        public bool IsUnknown => effectType == UnknownEffect;
        public bool IsOnce => effectType == OnceEffect;
        public bool IsLoop => effectType == LoopEffect;
        public bool IsObject => effectType == ObjectEffect;

        public bool IsToggle => IsLoop || IsObject;

        private GameObject originalParticleSystemParent;
        private Transform[] particleSystemParents;
        private ParticleSystem[][] particleSystems;
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
            bool active = ActiveCount != 0;
            switch (effectType)
            {
                case LoopEffect:
                    buttonData.button.colors = active ? gun.ActiveLoopColor : gun.InactiveLoopColor;
                    break;
                case ObjectEffect:
                    buttonData.button.colors = active ? gun.ActiveObjectColor : gun.InactiveObjectColor;
                    break;
                default:
                    buttonData.button.colors = active ? gun.ActiveColor : gun.InactiveColor;
                    break;
            }

            if (IsToggle)
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

        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        [InitializeOnLoad]
        public static class OnBuildRegister
        {
            static OnBuildRegister() => JanSharp.OnBuildUtil.RegisterType<EffectDescriptor>();
        }
        bool IOnBuildCallback.OnBuild()
        {
            var particleSystems = this.GetComponentsInChildren<ParticleSystem>();
            effectDuration = 0f;
            if (particleSystems.Length == 0)
                effectType = ObjectEffect;
            else
            {
                effectType = OnceEffect;
                foreach (var particleSystem in particleSystems)
                {
                    var main = particleSystem.main;
                    if (main.playOnAwake) // NOTE: this warning is nice and all but it instantly gets cleared if clear on play is enabled
                        Debug.LogWarning($"Particle System '{particleSystem.name}' is playing on awake which is "
                            + $"most likely undesired. (effect obj '{this.name}', effect name '{this.effectName}')");
                    if (main.loop)
                        effectType = LoopEffect;
                    float lifetime;
                    switch (main.startLifetime.mode)
                    {
                        case ParticleSystemCurveMode.Constant:
                            lifetime = main.startLifetime.constant;
                            break;
                        case ParticleSystemCurveMode.TwoConstants:
                            lifetime = main.startLifetime.constantMax;
                            break;
                        case ParticleSystemCurveMode.Curve:
                            lifetime = main.startLifetime.curve.keys.Select(k => k.value).Max();
                            break;
                        case ParticleSystemCurveMode.TwoCurves:
                            lifetime = main.startLifetime.curveMax.keys.Select(k => k.value).Max();
                            break;
                        default:
                            lifetime = 0f; // to make the compiler happy
                            break;
                    }
                    effectDuration = Mathf.Max(effectDuration, main.duration + lifetime);
                    // I have no idea what `psMain.startLifetimeMultiplier` actually means. It clearly isn't a multiplier.
                    // it might also only apply to curves, but I don't know what to do with that information
                    // basically, it gives me a 5 when the lifetime is 5. I set it to 2, it gives me a 2 back, as expected, but it also set the constant lifetime to 2.
                    // that is not how a multiplier works
                }
            }
            this.ApplyProxyModifications();
            return true;
        }
        #endif

        private bool particleSystemInitialized;
        private void InitParticleSystem()
        {
            if (particleSystemInitialized)
                return;
            particleSystemInitialized = true;
            particleSystemParents = new Transform[4];
            particleSystems = new ParticleSystem[4][];
            activeEffects = new bool[4];
            activeEffectIndexes = new int[4];
            positionsStage = new Vector3[4];
            rotationsStage = new Quaternion[4];
            startTimesStage = new float[4];
            var psParent = this.transform.GetChild(0);
            originalParticleSystemParent = psParent.gameObject;
            if (!IsObject)
            {
                if (IsLoop)
                {
                    particleSystemParents[0] = psParent;
                    particleSystems[0] = psParent.GetComponentsInChildren<ParticleSystem>();
                    count = 1;
                }
                else
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
            var newParticleSystems = new ParticleSystem[newLength][];
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
            particleSystems[count++] = transform.GetComponentsInChildren<ParticleSystem>();
        }

        public void PlayEffect(Vector3 position, Quaternion rotation)
        {
            if (randomizeRotation)
                rotation = rotation * Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);

            PlayEffectInternal(position, rotation);
            if (IsToggle)
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
            StopToggleEffectInternal();
            stagedCount = 0;
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            RequestSerialization();
        }

        private void StopToggleEffectInternal()
        {
            if (ActiveCount == 0)
                return;
            if (IsLoop)
                foreach (var ps in particleSystems[0])
                    ps.Stop();
            else
                originalParticleSystemParent.SetActive(false);
            ActiveCount = 0;
        }

        private void PlayEffectInternal(Vector3 position, Quaternion rotation)
        {
            if (IsToggle)
            {
                if (ActiveCount == 1)
                    StopToggleEffectInternal();
                else
                {
                    if (IsLoop)
                    {
                        particleSystemParents[0].SetPositionAndRotation(position, rotation);
                        foreach (var ps in particleSystems[0])
                            ps.Play();
                    }
                    else
                    {
                        originalParticleSystemParent.transform.SetPositionAndRotation(position, rotation);
                        originalParticleSystemParent.SetActive(true);
                    }
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
            foreach (var ps in particleSystems[index])
                ps.Play();
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
        /// <summary>
        /// holds values relative to the current Time.time which ultimately
        /// causes all of them to be negative values. 0 at best, never positive.
        /// </summary>
        [UdonSynced] private float[] syncedStartTimes;
        private const float MaxLoopDelay = 0.15f;
        private const float MaxDelay = 0.5f;
        private const float StaleEffectTime = 15f;
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
            if (!IsToggle) // don't reset for toggles
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
            if (IsToggle) // if it isn't initialized it won't enter this block
            {
                if (isZero)
                    StopToggleEffectInternal();
                else
                {
                    if (ActiveCount == 0)
                    {
                        PlayEffectInternal(syncedPositions[0], syncedRotations[0]);
                        var time = Mathf.Max(0f, -syncedStartTimes[0] - MaxLoopDelay);
                        if (IsLoop)
                            foreach (var ps in particleSystems[0])
                                ps.time = time;
                    }
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
                {
                    if (effectDuration + StaleEffectTime + startTime > 0f) // prevent old effects from playing, specifically for late joiners
                        PlayEffectInternal(syncedPositions[i], syncedRotations[i]);
                }
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
