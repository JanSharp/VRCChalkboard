using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class VFXTargetGun : UdonSharpBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float maxDistance = 250f;
        // 0: Default, 4: Water, 8: Interactive, 11: Environment, 13: Pickup
        [SerializeField] private LayerMask rayLayerMask = (1 << 0) | (1 << 4) | (1 << 8) | (1 << 11) | (1 << 13);
        private Color deselectedColor;
        [SerializeField] private Color inactiveColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveLoopColor = new Color(0.2f, 0.7f, 1f);
        [SerializeField] private Color activeLoopColor = Color.cyan;
        [Header("Internal")]
        [SerializeField] private RectTransform buttonGrid;
        public RectTransform ButtonGrid => buttonGrid;
        [SerializeField] private int columnCount = 4;
        [SerializeField] private GameObject buttonPrefab;
        public GameObject ButtonPrefab => buttonPrefab;
        [SerializeField] private float buttonHeight = 90f;
        [SerializeField] private Transform effectsParent;
        [SerializeField] private GameObject uiCanvas;
        [SerializeField] private UdonBehaviour uiToggle;
        [SerializeField] private GameObject gunMesh;
        [SerializeField] private VRC_Pickup pickup;
        [SerializeField] private Transform aimPoint;
        [SerializeField] private Transform targetIndicator;
        [SerializeField] private Renderer uiToggleRenderer;
        [SerializeField] private Toggle keepOpenToggle;
        public Toggle KeepOpenToggle => keepOpenToggle;
        [SerializeField] private TextMeshPro selectedEffectNameText;
        [SerializeField] private TextMeshProUGUI legendText;

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
                if (value == selectedEffect)
                    return;
                if (selectedEffect != null)
                    selectedEffect.Selected = false;
                else if (IsHeld)
                    UManager.Register(this);
                selectedEffect = value; // update `selectedEffect` before setting `Selected` to true on an effect descriptor
                if (value == null)
                {
                    UManager.Deregister(this);
                    UpdateColors();
                    selectedEffectNameText.text = "";
                    IsTargetIndicatorActive = false;
                }
                else
                {
                    value.Selected = true;
                    selectedEffectNameText.text = value.EffectName;
                }
                if (!isReceiving)
                {
                    Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                    RequestSerialization();
                }
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
                    if (!initialized && selectedEffectIndex != -1)
                        Init();
                    if (SelectedEffect != null)
                        UManager.Register(this);
                    BecomeOwner(); // preemptive transfer to spread ownership of objects out between players
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
        private bool isVisible;
        public bool IsVisible
        {
            get => isVisible;
            set
            {
                isVisible = value;
                if (!value)
                {
                    SetUIActive(false);
                    pickup.Drop();
                }
                pickup.pickupable = value;
                gunMesh.SetActive(value);
                uiToggle.gameObject.SetActive(value);
                selectedEffectNameText.gameObject.SetActive(value);
            }
        }

        public ColorBlock InactiveColor { get; private set; }
        public ColorBlock ActiveColor { get; private set; }
        public ColorBlock InactiveLoopColor { get; private set; }
        public ColorBlock ActiveLoopColor { get; private set; }

        private uint ToHex(Color32 c32, bool includeAlpha) {
            if(!includeAlpha) return ((uint)c32.r << 16) | ((uint)c32.g << 8) | (uint)c32.b;
            return ((uint)c32.r << 24) | ((uint)c32.g << 16) | ((uint)c32.b << 8) | (uint)c32.a;
        }

        private void Init()
        {
            initialized = true;
            deselectedColor = uiToggleRenderer.material.color;
            InactiveColor = MakeColorBlock(inactiveColor);
            ActiveColor = MakeColorBlock(activeColor);
            InactiveLoopColor = MakeColorBlock(inactiveLoopColor);
            ActiveLoopColor = MakeColorBlock(activeLoopColor);
            legendText.text = $"[<b><u>Selected</u></b>]"
                + $" [<b><color=#{ToHex(inactiveColor, false):X6}>Inactive</color></b>]"
                + $" [<b><color=#{ToHex(activeColor, false):X6}>Active</color></b>]"
                + $" [<b><color=#{ToHex(inactiveLoopColor, false):X6}>Inactive Loop</color></b>]"
                + $" [<b><color=#{ToHex(activeLoopColor, false):X6}>Active Loop</color></b>]";
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
                    descriptor.Init(this, i);
            }
            int rows = (count + columnCount - 1) / columnCount;
            buttonGrid.sizeDelta = new Vector2(buttonGrid.sizeDelta.x, buttonHeight * rows);
            if (selectedEffectIndex != -1)
                SelectedEffect = descriptors[selectedEffectIndex];
        }

        private ColorBlock MakeColorBlock(Color color)
        {
            var colors = new ColorBlock();
            colors.normalColor = color;
            colors.highlightedColor = color * new Color(0.95f, 0.95f, 0.95f);
            colors.pressedColor = color * new Color(0.75f, 0.75f, 0.75f);
            colors.selectedColor = color * new Color(0.95f, 0.95f, 0.95f);
            colors.disabledColor = color * new Color(0.75f, 0.75f, 0.75f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            // Debug.Log($"colors.normalColor: {colors.normalColor}, colors.highlightedColor: {colors.highlightedColor}, colors.pressedColor: {colors.pressedColor}, colors.selectedColor: {colors.selectedColor}, colors.disabledColor: {colors.disabledColor}");
            return colors;
        }

        public void DeselectEffect()
        {
            SelectedEffect = null;
            if (!KeepOpenToggle.isOn)
                CloseUI();
        }

        public void ToggleUI() => SetUIActive(!uiCanvas.activeSelf);
        public void CloseUI() => SetUIActive(false);
        public void SetUIActive(bool active)
        {
            if (!initialized && active)
                Init();
            uiCanvas.SetActive(active);
            uiToggle.InteractionText = active ? "Hide UI" : "Select Effect";
            if (active)
                BecomeOwner();
        }

        private void BecomeOwner()
        {
            var localPlayer = Networking.LocalPlayer;
            Networking.SetOwner(localPlayer, this.gameObject);
            if (initialized)
                foreach (var descriptor in descriptors)
                    Networking.SetOwner(localPlayer, descriptor.gameObject);
        }

        public void UseSelectedEffect()
        {
            if (selectedEffect == null)
                return;
            if (!IsTargetIndicatorActive)
            {
                // allow disabling of loop effects without pointing at any object
                if (selectedEffect.Loop && selectedEffect.ActiveCount != 0)
                    selectedEffect.StopLoopEffect();
                return;
            }
            selectedEffect.PlayEffect(targetIndicator.position, targetIndicator.rotation);
            if (selectedEffect.Loop)
                IsTargetIndicatorActive = false;
        }

        public void UpdateColors()
        {
            if (SelectedEffect == null)
            {
                uiToggleRenderer.material.color = deselectedColor;
                return;
            }
            Color color;
            if (SelectedEffect.ActiveCount == 0)
                color = SelectedEffect.Loop ? inactiveLoopColor : inactiveColor;
            else
                color = SelectedEffect.Loop ? activeLoopColor : activeColor;
            color.a = deselectedColor.a;
            uiToggleRenderer.material.color = color;
        }

        public void CustomUpdate()
        {
            // don't show an indicator if the loop is currently active
            if (selectedEffect.Loop && selectedEffect.ActiveCount != 0)
                return;
            RaycastHit hit;
            if (Physics.Raycast(aimPoint.position, aimPoint.forward, out hit, maxDistance, rayLayerMask.value))
            {
                targetIndicator.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal, aimPoint.forward));
                IsTargetIndicatorActive = true;
            }
            else
            {
                IsTargetIndicatorActive = false;
            }
        }



        [UdonSynced] private int selectedEffectIndex = -1;
        private bool isReceiving;

        public override void OnPreSerialization()
        {
            selectedEffectIndex = SelectedEffect == null ? -1 : SelectedEffect.Index;
        }

        public override void OnDeserialization()
        {
            isReceiving = true;
            if (!initialized && IsHeld) // someone else pressed a button while this client was holding it and didn't have the UI open before
                Init();
            if (initialized)
                SelectedEffect = selectedEffectIndex == -1 ? null : descriptors[selectedEffectIndex];
            isReceiving = false;
        }
    }
}
