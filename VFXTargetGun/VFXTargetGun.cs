using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
using System.Linq;
#endif

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class VFXTargetGun : UdonSharpBehaviour
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
        , IOnBuildCallback
    #endif
    {
        [Header("Configuration")]
        [SerializeField] private float maxDistance = 250f;
        // 0: Default, 4: Water, 8: Interactive, 11: Environment, 13: Pickup
        [SerializeField] private LayerMask rayLayerMask = (1 << 0) | (1 << 4) | (1 << 8) | (1 << 11) | (1 << 13);
        private Color deselectedColor;
        [SerializeField] private Color inactiveColor = new Color(0.73725f, 0.42353f, 0.85098f);
        [SerializeField] private Color activeColor = new Color(0.89412f, 0.60000f, 1.00000f);
        [SerializeField] private Color inactiveLoopColor = new Color(0.14118f, 0.69804f, 0.25882f);
        [SerializeField] private Color activeLoopColor = new Color(0.09020f, 0.90196f, 0.26275f);
        [SerializeField] private Color inactiveObjectColor = new Color(0.85098f, 0.54902f, 0.25490f);
        [SerializeField] private Color activeObjectColor = new Color(0.94902f, 0.54510f, 0.14118f);
        [Header("Internal")]
        [SerializeField] private RectTransform buttonGrid;
        public RectTransform ButtonGrid => buttonGrid;
        [SerializeField] private int columnCount = 4;
        [SerializeField] private GameObject buttonPrefab;
        public GameObject ButtonPrefab => buttonPrefab;
        [SerializeField] private float buttonHeight = 90f;
        [SerializeField] private Transform effectsParent;
        [SerializeField] private BoxCollider uiCanvasCollider;
        [SerializeField] private UdonBehaviour uiToggle;
        [SerializeField] private UdonBehaviour placeDeleteModeToggle;
        [SerializeField] private GameObject gunMesh;
        [SerializeField] private VRC_Pickup pickup;
        [SerializeField] private Transform aimPoint;
        [SerializeField] private Transform targetIndicator;
        [SerializeField] private Renderer uiToggleRenderer;
        [SerializeField] private Toggle keepOpenToggle;
        public Toggle KeepOpenToggle => keepOpenToggle;
        [SerializeField] private TextMeshPro selectedEffectNameTextLeftHand;
        [SerializeField] private TextMeshPro selectedEffectNameTextRightHand;
        [SerializeField] private TextMeshProUGUI legendText;
        [SerializeField] private Button placeModeButton;
        [SerializeField] private Button deleteModeButton;
        [SerializeField] private Button editModeButton;
        [SerializeField] private GameObject confirmationWindow;

        // set OnBuild
        [SerializeField] [HideInInspector] private MeshRenderer[] gunMeshRenderers;
        [SerializeField] [HideInInspector] private TextMeshProUGUI placeModeButtonText;
        [SerializeField] [HideInInspector] private TextMeshProUGUI deleteModeButtonText;
        [SerializeField] [HideInInspector] private TextMeshProUGUI editModeButtonText;

        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        [InitializeOnLoad]
        public static class OnBuildRegister
        {
            static OnBuildRegister() => JanSharp.OnBuildUtil.RegisterType<VFXTargetGun>();
        }
        bool IOnBuildCallback.OnBuild()
        {
            if (gunMesh == null)
            {
                Debug.LogError("VFX Target gun requires all internal references to be set in the inspector.");
                return false;
            }
            gunMeshRenderers = gunMesh.GetComponentsInChildren<MeshRenderer>();
            TextMeshProUGUI GetButtonText(Transform button) => button.GetChild(0).GetComponent<TextMeshProUGUI>();
            placeModeButtonText = GetButtonText(placeModeButton.transform);
            deleteModeButtonText = GetButtonText(deleteModeButton.transform);
            editModeButtonText = GetButtonText(editModeButton.transform);
            this.ApplyProxyModifications();
            return true;
        }
        #endif

        private const int UnknownMode = 0;
        private const int PlaceMode = 1;
        private const int DeleteMode = 2;
        private const int EditMode = 3;
        private int mode = UnknownMode;
        public int Mode
        {
            get => mode;
            set
            {
                if (value == mode)
                    return;
                SetModeButtonTextUnderline(mode, false);
                mode = value;
                SetModeButtonTextUnderline(mode, true);
                var color = GetModeColor(mode);
                foreach (var renderer in gunMeshRenderers)
                    foreach (var mat in renderer.materials)
                        mat.color = color;
                placeDeleteModeToggle.InteractionText = IsPlaceMode ? "Switch to Delete" : "Switch to Place";
            }
        }
        public bool IsPlaceMode => Mode == PlaceMode;
        public bool IsDeleteMode => Mode == DeleteMode;
        public bool IsEditMode => Mode == EditMode;

        private void SwitchToMode(int mode)
        {
            Mode = mode;
            if (!keepOpenToggle.isOn)
                SetUIActive(false);
        }
        public void SwitchToPlaceMode() => SwitchToMode(PlaceMode);
        public void SwitchToDeleteMode() => SwitchToMode(DeleteMode);
        public void SwitchToEditMode() => SwitchToMode(EditMode);
        public void SwitchToPlaceModeKeepingUIOpen() => Mode = PlaceMode;
        public void SwitchToDeleteModeKeepingUIOpen() => Mode = DeleteMode;
        public void SwitchToEditModeKeepingUIOpen() => Mode = EditMode;

        /// <summary>
        /// Simply does nothing if the given mode is UnknownMode.
        /// </summary>
        private void SetModeButtonTextUnderline(int mode, bool isUnderlined)
        {
            switch (mode)
            {
                case PlaceMode:
                    SetModeButtonTextUnderlineInternal(placeModeButtonText, isUnderlined);
                    break;
                case DeleteMode:
                    SetModeButtonTextUnderlineInternal(deleteModeButtonText, isUnderlined);
                    break;
                case EditMode:
                    SetModeButtonTextUnderlineInternal(editModeButtonText, isUnderlined);
                    break;
            }
        }
        private void SetModeButtonTextUnderlineInternal(TextMeshProUGUI text, bool isUnderlined)
        {
            text.text = isUnderlined
                ? $"<u>{text.text}</u>"
                : text.text.Substring(3, text.text.Length - 7);
        }

        private Color GetModeColor(int mode)
        {
            switch (mode)
            {
                case PlaceMode:
                    return placeModeButton.colors.normalColor;
                case DeleteMode:
                    return deleteModeButton.colors.normalColor;
                case EditMode:
                    return editModeButton.colors.normalColor;
                default:
                    return Color.white;
            }
        }
        private Color GetCurrentModeColor() => GetModeColor(Mode);

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
                    selectedEffectNameTextLeftHand.text = "";
                    selectedEffectNameTextRightHand.text = "";
                    IsTargetIndicatorActive = false;
                }
                else
                {
                    value.Selected = true;
                    selectedEffectNameTextLeftHand.text = value.EffectName;
                    selectedEffectNameTextRightHand.text = value.EffectName;
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
                    Vector3 togglePos = placeDeleteModeToggle.transform.localPosition;
                    if (pickup.currentHand == VRC_Pickup.PickupHand.Left)
                    {
                        selectedEffectNameTextLeftHand.gameObject.SetActive(true);
                        selectedEffectNameTextRightHand.gameObject.SetActive(false);
                        togglePos.x = Mathf.Abs(togglePos.x);
                    }
                    else
                    {
                        selectedEffectNameTextLeftHand.gameObject.SetActive(false);
                        selectedEffectNameTextRightHand.gameObject.SetActive(true);
                        togglePos.x = -Mathf.Abs(togglePos.x);
                    }
                    placeDeleteModeToggle.transform.localPosition = togglePos;
                }
                else
                {
                    IsTargetIndicatorActive = false;
                    UManager.Deregister(this);
                }
            }
        }
        private int deletionTargetIndex;
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
                if (!value || initialized) // only turn _on_ if if the gun is initialized
                    placeDeleteModeToggle.gameObject.SetActive(value);
                selectedEffectNameTextRightHand.gameObject.SetActive(value);
            }
        }

        public ColorBlock InactiveColor { get; private set; }
        public ColorBlock ActiveColor { get; private set; }
        public ColorBlock InactiveLoopColor { get; private set; }
        public ColorBlock ActiveLoopColor { get; private set; }
        public ColorBlock InactiveObjectColor { get; private set; }
        public ColorBlock ActiveObjectColor { get; private set; }

        private uint ToHex(Color32 c32, bool includeAlpha) {
            if(!includeAlpha) return ((uint)c32.r << 16) | ((uint)c32.g << 8) | (uint)c32.b;
            return ((uint)c32.r << 24) | ((uint)c32.g << 16) | ((uint)c32.b << 8) | (uint)c32.a;
        }

        private void Init()
        {
            initialized = true;
            Mode = PlaceMode;
            deselectedColor = uiToggleRenderer.material.color;
            InactiveColor = MakeColorBlock(inactiveColor);
            ActiveColor = MakeColorBlock(activeColor);
            InactiveLoopColor = MakeColorBlock(inactiveLoopColor);
            ActiveLoopColor = MakeColorBlock(activeLoopColor);
            InactiveObjectColor = MakeColorBlock(inactiveObjectColor);
            ActiveObjectColor = MakeColorBlock(activeObjectColor);
            legendText.text = $"[<b><u>Selected</u></b>] "
                + $"[<b><color=#{ToHex(inactiveColor, false):X6}>once</color>: <color=#{ToHex(activeColor, false):X6}>on</color>/<color=#{ToHex(inactiveColor, false):X6}>off</color></b>] "
                + $"[<b><color=#{ToHex(inactiveLoopColor, false):X6}>loop</color>: <color=#{ToHex(activeLoopColor, false):X6}>on</color>/<color=#{ToHex(inactiveLoopColor, false):X6}>off</color></b>] "
                + $"[<b><color=#{ToHex(inactiveObjectColor, false):X6}>object</color>: <color=#{ToHex(activeObjectColor, false):X6}>on</color>/<color=#{ToHex(inactiveObjectColor, false):X6}>off</color></b>]";
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
            if (IsVisible)
                placeDeleteModeToggle.gameObject.SetActive(true);
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

        public void DeleteEverything() => confirmationWindow.SetActive(true);
        public void CancelDeleteEverything() => confirmationWindow.SetActive(false);
        public void ConfirmDeleteEverything()
        {
            confirmationWindow.SetActive(false);
            // TODO: spread work out across frames (probably)
            foreach (var descriptor in descriptors)
                descriptor.StopAllEffects();
        }

        public void ToggleUI() => SetUIActive(!uiCanvasCollider.gameObject.activeSelf);
        public void CloseUI() => SetUIActive(false);
        public void SetUIActive(bool active)
        {
            if (!initialized && active)
                Init();
            uiCanvasCollider.gameObject.SetActive(active);
            uiCanvasCollider.enabled = active;
            uiToggle.DisableInteractive = active;
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
            if (SelectedEffect == null || !IsTargetIndicatorActive)
                return;
            if (IsPlaceMode)
                SelectedEffect.PlayEffect(targetIndicator.position, targetIndicator.rotation);
            else if (IsDeleteMode)
                SelectedEffect.StopToggleEffect(deletionTargetIndex);
        }

        public void UpdateColors()
        {
            if (SelectedEffect == null)
            {
                uiToggleRenderer.material.color = deselectedColor;
                return;
            }
            Color color;
            bool active = SelectedEffect.ActiveCount != 0;
            if (SelectedEffect.IsLoop)
                color = active ? activeLoopColor : inactiveLoopColor;
            else if (SelectedEffect.IsObject)
                color = active ? activeObjectColor : inactiveObjectColor;
            else
                color = active ? activeColor : inactiveColor;
            color.a = deselectedColor.a;
            uiToggleRenderer.material.color = color;
        }

        public void CustomUpdate()
        {
            RaycastHit hit;
            if (Physics.Raycast(aimPoint.position, aimPoint.forward, out hit, maxDistance, rayLayerMask.value))
            {
                if (IsPlaceMode)
                {
                    targetIndicator.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal, aimPoint.forward));
                    IsTargetIndicatorActive = true;
                }
                else if (IsDeleteMode)
                {
                    // NOTE: this whole logic is very most likely a big performance concern, mostly because of GetNearestActiveEffect
                    if (SelectedEffect == null || !SelectedEffect.IsToggle || SelectedEffect.ActiveCount == 0)
                    {
                        IsTargetIndicatorActive = false;
                        return;
                    }
                    deletionTargetIndex = SelectedEffect.GetNearestActiveEffect(hit.point);
                    Vector3 position = SelectedEffect.EffectParents[deletionTargetIndex].position;
                    if (Physics.Raycast(aimPoint.position, (position - aimPoint.position).normalized, out hit, maxDistance, rayLayerMask.value))
                        targetIndicator.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal, aimPoint.forward));
                    else
                        targetIndicator.SetPositionAndRotation(position, Quaternion.identity);
                    IsTargetIndicatorActive = true;
                }
            }
            else
                IsTargetIndicatorActive = false;
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
