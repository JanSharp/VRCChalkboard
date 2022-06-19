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
        [SerializeField] [HideInInspector] private float effectDuration; // used by once effects
        [SerializeField] [HideInInspector] private float effectLifetime; // used by loop effects
        [SerializeField] [HideInInspector] private EffectDescriptorFullSync fullSync;
        [SerializeField] [HideInInspector] private GameObject originalEffectObject;

        private const int OnceEffect = 0;
        private const int LoopEffect = 1;
        private const int ObjectEffect = 2;
        public int EffectType => effectType;
        public bool IsOnce => effectType == OnceEffect;
        public bool IsLoop => effectType == LoopEffect;
        public bool IsObject => effectType == ObjectEffect;

        public bool IsToggle => !IsOnce;
        private bool HasParticleSystems => !IsObject;

        private Transform[] effectParents;
        private ParticleSystem[][] particleSystems;
        private bool[] activeEffects;
        private bool[] fadingOut;
        private int maxCount;
        private int fadingOutCount;
        private int FadingOutCount
        {
            get => fadingOutCount;
            set
            {
                if (fadingOutCount == 0 || value == 0)
                {
                    fadingOutCount = value;
                    // only update colors if the references to the gun and the UI even exists
                    if (gun != null)
                        UpdateColors();
                }
                else
                    fadingOutCount = value;
                if (gun != null)
                    SetActiveCountText();
            }
        }
        private int[] toFinishIndexes;
        private int toFinishCount;
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
                if (gun != null)
                    SetActiveCountText();
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
            bool active = ActiveCount != 0 || FadingOutCount != 0;
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
                buttonData.stopButton.gameObject.SetActive(ActiveCount != 0);

            // update the gun if this is the currently selected effect
            if (Selected)
                gun.UpdateColors();
        }

        private void SetActiveCountText()
        {
            var totalCount = ActiveCount + FadingOutCount;
            buttonData.activeCountText.text = totalCount == 0 ? "" : totalCount.ToString();
        }

        public void Init(VFXTargetGun gun, int index)
        {
            this.gun = gun;
            Index = index;
            InitEffect();
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
            void LogErrMsg() => Debug.LogError($"The {nameof(EffectDescriptor)} requires 2 children."
                + $" The first child must be the 'EffectParent' which is either the parent for a collection of particle systems,"
                + $" or the parent for an object. To be exact it is considered to be an object whenever there are no particle systems."
                + $" The second child must have the {nameof(EffectDescriptorFullSync)} Udon Behaviour on it");
            if (this.transform.childCount < 2)
            {
                LogErrMsg();
                return false;
            }
            originalEffectObject = this.transform.GetChild(0).gameObject;
            fullSync = this.transform.GetChild(1)?.GetUdonSharpComponent<EffectDescriptorFullSync>();
            if (fullSync == null)
            {
                LogErrMsg();
                return false;
            }
            fullSync.descriptor = this;
            var particleSystems = this.transform.GetChild(0).GetComponentsInChildren<ParticleSystem>();
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
                            lifetime = main.startLifetime.curve.keys.Max(k => k.value);
                            break;
                        case ParticleSystemCurveMode.TwoCurves:
                            lifetime = main.startLifetime.curveMax.keys.Max(k => k.value);
                            break;
                        default:
                            lifetime = 0f; // to make the compiler happy
                            break;
                    }
                    effectLifetime = Mathf.Max(effectLifetime, lifetime);
                    // I have no idea what `psMain.startLifetimeMultiplier` actually means. It clearly isn't a multiplier.
                    // it might also only apply to curves, but I don't know what to do with that information
                    // basically, it gives me a 5 when the lifetime is 5. I set it to 2, it gives me a 2 back, as expected, but it also set the constant lifetime to 2.
                    // that is not how a multiplier works
                }
                effectDuration = particleSystems[0].main.duration + effectLifetime;
            }
            this.ApplyProxyModifications();
            return true;
        }
        #endif

        private bool effectInitialized;
        private void InitEffect()
        {
            if (effectInitialized)
                return;
            effectInitialized = true;
            effectParents = new Transform[4];
            if (HasParticleSystems)
                particleSystems = new ParticleSystem[4][];
            activeEffects = new bool[4];
            if (IsLoop)
                fadingOut = new bool[4];
            toFinishIndexes = new int[4];
            // syncing
            effectOrder = new uint[4];
            requestedSyncs = new bool[4];
            requestedIndexes = new int[4];
            maxCount = 4;
        }

        private void MakeButton()
        {
            var button = VRCInstantiate(gun.ButtonPrefab);
            button.transform.SetParent(gun.ButtonGrid, false);
            buttonData = (EffectButtonData)button.GetComponent(typeof(UdonBehaviour));
            buttonData.descriptor = this;
            buttonData.text.text = effectName;
            buttonData.stopButtonText.text = HasParticleSystems ? "Stop All" : "Delete All";
            UpdateColors();
            SetActiveCountText();
        }

        public void SelectThisEffect()
        {
            var toggle = gun.KeepOpenToggle; // put it in a local var first because UdonSharp is being picky and weird
            if (!toggle.isOn)
                gun.CloseUI();
            gun.SelectedEffect = this;
        }

        private void GrowArrays(int newLength)
        {
            maxCount = newLength;
            // since this is making the C# VM perform the loops and copying this is most likely faster than having our own loop
            var newEffectParents = new Transform[newLength];
            effectParents.CopyTo(newEffectParents, 0);
            effectParents = newEffectParents;
            if (HasParticleSystems)
            {
                var newParticleSystems = new ParticleSystem[newLength][];
                particleSystems.CopyTo(newParticleSystems, 0);
                particleSystems = newParticleSystems;
            }
            var newActiveEffects = new bool[newLength];
            activeEffects.CopyTo(newActiveEffects, 0);
            activeEffects = newActiveEffects;
            if (IsLoop)
            {
                var newFadingOut = new bool[newLength];
                fadingOut.CopyTo(newFadingOut, 0);
                fadingOut = newFadingOut;
            }
            var newToFinishIndexes = new int[newLength];
            toFinishIndexes.CopyTo(newToFinishIndexes, 0);
            toFinishIndexes = newToFinishIndexes;
            // syncing
            var newEffectOrder = new uint[newLength];
            effectOrder.CopyTo(newEffectOrder, 0);
            effectOrder = newEffectOrder;
            var newRequestedSyncs = new bool[newLength];
            requestedSyncs.CopyTo(newRequestedSyncs, 0);
            requestedSyncs = newRequestedSyncs;
            var newRequestedIndexes = new int[newLength];
            requestedIndexes.CopyTo(newRequestedIndexes, 0);
            requestedIndexes = newRequestedIndexes;
        }

        private void EnsureIsInRange(int index)
        {
            if (index >= maxCount)
            {
                while (index >= maxCount)
                    maxCount *= 2;
                GrowArrays(maxCount);
            }
        }

        private Transform GetEffectAtIndex(int index)
        {
            EnsureIsInRange(index);
            var effectTransform = effectParents[index];
            if (effectTransform != null)
                return effectTransform;
            // HACK: workaround for VRChat's weird behaviour when instantiating a copy of an existing object in the world.
            // modifying the position and rotation of the copy ends up modifying the original one for some reason
            // also the particle system Play call doesn't seem to go off on the copy
            // but when using the copy at a later point in time where it is accessed the same way through the arrays it does behave as a copy
            // which ultimately just makes me believe it is VRCInstantiate not behaving. So, my solution is to simply not use the original object
            // except for creating copies of it and then modifying the copies. It's a waste of memory and performance
            // and an unused game object in the world but what am I supposed to do
            var obj = VRCInstantiate(originalEffectObject);
            effectTransform = obj.transform;
            effectTransform.parent = this.transform;
            effectParents[index] = effectTransform;
            if (HasParticleSystems)
                particleSystems[index] = effectTransform.GetComponentsInChildren<ParticleSystem>();
            return effectTransform;
        }

        public void PlayEffect(Vector3 position, Quaternion rotation)
        {
            if (randomizeRotation)
                rotation = rotation * Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);

            int index;
            if (ActiveCount == maxCount)
                index = maxCount; // this will end up growing the arrays and creating a new effect
            else
            {
                // TODO: think about better solutions for this that don't require a loop
                index = 0; // just to make C# compile, since the below loop should always find an index anyway
                // find first inactive effect
                for (int i = 0; i < maxCount; i++)
                    if (!activeEffects[i] && (!IsLoop || !fadingOut[i]))
                    {
                        index = i;
                        break;
                    }
            }
            PlayEffectInternal(index, position, rotation);
            RequestSync(index);
        }

        public void StopAllEffects()
        {
            if (ActiveCount == 0)
                return;
            for (int i = 0; i < maxCount; i++)
                if (activeEffects[i])
                {
                    if (IsToggle)
                        StopToggleEffect(i);
                    else
                    {
                        // TODO: stop once effect
                    }
                }
        }

        public void StopToggleEffect(int index)
        {
            StopToggleEffectInternal(index);
            RequestSync(index);
        }

        private void StopToggleEffectInternal(int index)
        {
            if (!activeEffects[index])
                return;
            activeEffects[index] = false;
            ActiveCount--;
            if (IsLoop)
            {
                fadingOut[index] = true;
                FadingOutCount++;
                foreach (var ps in particleSystems[index])
                    ps.Stop();
                toFinishIndexes[toFinishCount++] = index;
                this.SendCustomEventDelayedSeconds(nameof(EffectRanOut), effectDuration);
            }
            else // IsObject
                effectParents[index].gameObject.SetActive(false);
        }

        private void PlayEffectInternal(int index, Vector3 position, Quaternion rotation)
        {
            var effectTransform = GetEffectAtIndex(index);
            if (activeEffects[index])
                return;
            ActiveCount++;
            activeEffects[index] = true;
            effectTransform.SetPositionAndRotation(position, rotation);
            if (IsObject)
                effectTransform.gameObject.SetActive(true);
            else
            {
                foreach (var ps in particleSystems[index])
                    ps.Play();
                if (IsOnce)
                {
                    toFinishIndexes[toFinishCount++] = index;
                    this.SendCustomEventDelayedSeconds(nameof(EffectRanOut), effectDuration);
                }
            }
        }

        public void EffectRanOut()
        {
            int index = toFinishIndexes[0];
            for (int i = 1; i < toFinishCount; i++)
                toFinishIndexes[i - 1] = toFinishIndexes[i];
            toFinishCount--;
            if (IsLoop)
            {
                fadingOut[index] = false;
                FadingOutCount--;
            }
            else // IsOnce
            {
                ActiveCount--;
                activeEffects[index] = false;
            }
        }



        // FIXME: when a loop effect is still fading out syncing (placing new ones) just doesn't seem work at all
        // FIXME: delete all toggle effects doesn't sync if you're not already the owner even though RequestSync sets owner [...]
        // this probably means that RequestSync _somehow_ doesn't get run at all in this case, because the receiving side's logic is identical

        // incremental syncing
        private uint[] effectOrder;
        private uint currentTopOrder;
        private bool[] requestedSyncs;
        private int[] requestedIndexes;
        private int requestedCount;
        [UdonSynced] private int[] syncedIndexes;
        [UdonSynced] private uint[] syncedOrder;
        [UdonSynced] private Vector3[] syncedPositions;
        [UdonSynced] private Quaternion[] syncedRotations;
        [UdonSynced] private float[] syncedTimes;
        private const uint ActiveBit = 0x80000000;
        private const float MaxLoopDelay = 0.15f;
        private const float MaxDelay = 0.5f;
        private const float StaleEffectTime = 15f;
        private int[] delayedIndexes;
        private Vector3[] delayedPositions;
        private Quaternion[] delayedRotations;
        private int delayedCount;

        private bool incrementalSyncingInitialized;
        private void InitIncrementalSyncing()
        {
            if (incrementalSyncingInitialized)
                return;
            incrementalSyncingInitialized = true;
            delayedIndexes = new int[4];
            delayedPositions = new Vector3[4];
            delayedRotations = new Quaternion[4];
        }

        private void RequestSync(int index)
        {
            if (requestedSyncs[index])
                return;
            requestedSyncs[index] = true;
            requestedIndexes[requestedCount++] = index;
            effectOrder[index] = ++currentTopOrder;
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            RequestSerialization();
        }

        public override void OnPreSerialization()
        {
            // nothing to sync yet
            if (!effectInitialized)
                return;
            if (syncedPositions == null || syncedPositions.Length != requestedCount)
            {
                syncedIndexes = new int[requestedCount];
                syncedOrder = new uint[requestedCount];
                syncedPositions = new Vector3[requestedCount];
                syncedRotations = new Quaternion[requestedCount];
                syncedTimes = new float[requestedCount];
            }
            var time = Time.time;
            for (int i = 0; i < requestedCount; i++)
            {
                var requestIndex = requestedIndexes[i];
                var effectTransform = effectParents[requestIndex];
                syncedIndexes[i] = requestIndex;
                var order = effectOrder[requestIndex];
                if (IsToggle && activeEffects[i])
                    order |= ActiveBit;
                syncedOrder[i] = order;
                syncedPositions[i] = effectTransform.position;
                syncedRotations[i] = effectTransform.rotation;
                if (HasParticleSystems)
                    syncedTimes[i] = particleSystems[requestIndex][0].time;
                requestedSyncs[requestIndex] = false;
            }
            requestedCount = 0;
        }

        public override void OnDeserialization()
        {
            int syncedCount;
            if (syncedPositions == null || (syncedCount = syncedIndexes.Length) == 0)
                return;
            InitEffect();
            InitIncrementalSyncing();
            float delay = Mathf.Min(syncedTimes[syncedCount - 1], MaxDelay);
            for (int i = 0; i < syncedCount; i++)
            {
                var effectIndex = syncedIndexes[i];
                var order = syncedOrder[i];
                bool active;
                if (IsToggle)
                {
                    active = (order & ActiveBit) != 0;
                    order &= ~ActiveBit;
                }
                else
                    active = true;
                EnsureIsInRange(effectIndex);
                if (effectOrder[effectIndex] >= order)
                    continue;
                effectOrder[effectIndex] = order;
                float time = delay - syncedTimes[i];
                if (!HasParticleSystems || !active || time <= 0f)
                {
                    if (IsToggle)
                    {
                        if (active)
                        {
                            PlayEffectInternal(effectIndex, syncedPositions[i], syncedRotations[i]);
                            if (IsLoop)
                            {
                                time = Mathf.Max(0f, syncedTimes[i] - MaxLoopDelay);
                                foreach (var ps in particleSystems[0])
                                    ps.time = time;
                            }
                        }
                        else
                            StopToggleEffectInternal(effectIndex);
                    }
                    else // IsOnce
                    {
                        if (effectDuration + StaleEffectTime + time > 0f) // prevent old effects from playing, specifically for late joiners
                            PlayEffectInternal(effectIndex, syncedPositions[i], syncedRotations[i]);
                    }
                }
                else // only for effects with particle systems when they get activated
                {
                    if (delayedCount == delayedPositions.Length)
                        GrowDelayedArrays();
                    delayedIndexes[delayedCount] = effectIndex;
                    delayedPositions[delayedCount] = syncedPositions[i];
                    delayedRotations[delayedCount++] = syncedRotations[i];
                    SendCustomEventDelayedSeconds(nameof(PlayEffectDelayed), time);
                }
            }
        }

        private void GrowDelayedArrays()
        {
            int newLength = delayedIndexes.Length * 2;
            var newDelayedIndexes = new int[newLength];
            delayedIndexes.CopyTo(newDelayedIndexes, 0);
            delayedIndexes = newDelayedIndexes;
            var newDelayedPositions = new Vector3[newLength];
            delayedPositions.CopyTo(newDelayedPositions, 0);
            delayedPositions = newDelayedPositions;
            var newDelayedRotations = new Quaternion[newLength];
            delayedRotations.CopyTo(newDelayedRotations, 0);
            delayedRotations = newDelayedRotations;
        }

        public void PlayEffectDelayed()
        {
            PlayEffectInternal(delayedIndexes[0], delayedPositions[0], delayedRotations[0]);
            for (int i = 1; i < delayedCount; i++)
            {
                delayedIndexes[i - 1] = delayedIndexes[i];
                delayedPositions[i - 1] = delayedPositions[i];
                delayedRotations[i - 1] = delayedRotations[i];
            }
            delayedCount--;
        }
    }
}
