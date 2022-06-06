using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VFXTargetGun : UdonSharpBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private LayerMask rayLayerMask = -1; // everything
        [Header("Internal")]
        [SerializeField] private RectTransform buttonGrid;
        public RectTransform ButtonGrid => buttonGrid;
        [SerializeField] private int columnCount;
        [SerializeField] private GameObject buttonPrefab;
        public GameObject ButtonPrefab => buttonPrefab;
        [SerializeField] private float buttonHeight;
        [SerializeField] private Transform effectsParent;
        [SerializeField] private GameObject uiCanvas;
        [SerializeField] private UdonBehaviour uiToggle;
        [SerializeField] private Transform aimPoint;
        [SerializeField] private Transform targetIndicator;

        // for UpdateManager
        private int customUpdateInternalIndex;
        private UpdateManager uManager;
        private UpdateManager UManager
        {
            get
            {
                if (uManager != null)
                    return uManager;
                uManager = GameObject.Find("/UpdateManager").GetComponent<UpdateManager>();
                return uManager;
            }
        }
        private bool initialized;
        private EffectDescriptor[] descriptors;
        private EffectDescriptor selectedEffect;
        public EffectDescriptor SelectedEffect
        {
            get => selectedEffect;
            set
            {
                if (selectedEffect == null && IsHeld)
                    UManager.Register(this);
                selectedEffect = value;
            }
        }
        private bool isHeld;
        public bool IsHeld
        {
            get => isHeld;
            set
            {
                if (isHeld == value)
                    return;
                isHeld = value;
                if (value)
                {
                    if (SelectedEffect != null)
                        UManager.Register(this);
                }
                else
                {
                    IsTargetIndicatorActive = false;
                    UManager.Deregister(this);
                }
            }
        }
        private bool isTargetIndicatorActive;
        private bool IsTargetIndicatorActive
        {
            get => isTargetIndicatorActive;
            set
            {
                if (isTargetIndicatorActive == value)
                    return;
                isTargetIndicatorActive = value;
                targetIndicator.gameObject.SetActive(value);
            }
        }

        private void Init()
        {
            initialized = true;
            int count = effectsParent.childCount;
            descriptors = new EffectDescriptor[count];
            for (int i = 0; i < count; i++)
            {
                var descriptor = (EffectDescriptor)effectsParent.GetChild(i).GetComponent(typeof(UdonBehaviour));
                descriptors[i] = descriptor;
                if (descriptors[i] == null)
                    Debug.LogError($"The child #{i + 1} ({effectsParent.GetChild(i).name}) "
                        + "of the effects descriptor parent does not have an EffectDescriptor.");
                else
                    descriptor.Init(this);
            }
            int rows = (count + columnCount - 1) / columnCount;
            buttonGrid.sizeDelta = new Vector2(buttonGrid.sizeDelta.x, buttonHeight * rows);
        }

        public void ToggleUI() => SetUIActive(!uiCanvas.activeSelf);
        public void SetUIActive(bool active)
        {
            if (!initialized && active)
                Init();
            uiCanvas.SetActive(active);
            uiToggle.InteractionText = active ? "Select Effect" : "Hide UI";
        }

        public void UseSelectedEffect()
        {
            if (selectedEffect == null || !IsTargetIndicatorActive)
                return;
            selectedEffect.PlayEffect(targetIndicator.position, targetIndicator.rotation);
        }

        public void CustomUpdate()
        {
            RaycastHit hit;
            if (Physics.Raycast(aimPoint.position, aimPoint.forward, out hit, maxDistance, rayLayerMask.value))
            {
                targetIndicator.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                IsTargetIndicatorActive = true;
            }
            else
            {
                IsTargetIndicatorActive = false;
            }
        }
    }
}
