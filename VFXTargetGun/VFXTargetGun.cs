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
        [SerializeField] private RectTransform itemUIContainer;
        [SerializeField] private RectTransform screenUIContainer;
        [SerializeField] private RectTransform mainWindow;
        [SerializeField] private RectTransform confirmationWindow;
        [SerializeField] private RectTransform helpWindow;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private GameObject uiToggle;
        [SerializeField] private UdonBehaviour placeDeleteModeToggle;
        [SerializeField] private GameObject gunMesh;
        [SerializeField] private VRC_Pickup pickup;
        public VRC_Pickup Pickup => pickup;
        [SerializeField] private Transform aimPoint;
        [SerializeField] private Transform placeIndicator;
        [SerializeField] private GameObject placeIndicatorForwardsArrow;
        [SerializeField] private Transform deleteIndicator;
        [SerializeField] private Transform laser;
        [SerializeField] private Transform secondLaser;
        private float laserBaseScale;
        [SerializeField] private Renderer uiToggleRenderer;
        [SerializeField] private Toggle keepOpenToggle;
        public Toggle KeepOpenToggle => keepOpenToggle;
        [SerializeField] private TextMeshPro selectedEffectNameTextLeftHand;
        [SerializeField] private TextMeshPro selectedEffectNameTextRightHand;
        [SerializeField] private TextMeshProUGUI legendText;
        [SerializeField] private Button placeModeButton;
        [SerializeField] private Button deleteModeButton;
        [SerializeField] private Button editModeButton;
        [SerializeField] private Sprite selectedSprite;

        // set OnBuild
        [SerializeField] [HideInInspector] private MeshRenderer[] gunMeshRenderers;
        [SerializeField] [HideInInspector] private Sprite normalSprite;

        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        [InitializeOnLoad]
        public static class OnBuildRegister
        {
            static OnBuildRegister() => JanSharp.OnBuildUtil.RegisterType<VFXTargetGun>();
        }
        bool IOnBuildCallback.OnBuild()
        {
            if (gunMesh == null || placeModeButton == null)
            {
                Debug.LogError("VFX Target gun requires all internal references to be set in the inspector.");
                return false;
            }
            gunMeshRenderers = gunMesh.GetComponentsInChildren<MeshRenderer>();
            normalSprite = placeModeButton.image.sprite;
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
                IsPlaceIndicatorActive = false;
                IsDeleteIndicatorActive = false;
                SetModeButtonTextUnderline(mode, false);
                mode = value;
                SetModeButtonTextUnderline(mode, true);
                var color = GetModeColor(mode);
                foreach (var renderer in gunMeshRenderers)
                    foreach (var mat in renderer.materials)
                        mat.color = color;
                placeDeleteModeToggle.InteractionText = IsPlaceMode ? "Switch to Delete" : "Switch to Place";
                UpdateUseText();
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
        private void SetModeButtonTextUnderline(int mode, bool isSelected)
        {
            switch (mode)
            {
                case PlaceMode:
                    SetModeButtonTextUnderlineInternal(placeModeButton, isSelected);
                    break;
                case DeleteMode:
                    SetModeButtonTextUnderlineInternal(deleteModeButton, isSelected);
                    break;
                case EditMode:
                    SetModeButtonTextUnderlineInternal(editModeButton, isSelected);
                    break;
            }
        }
        private void SetModeButtonTextUnderlineInternal(Button button, bool isSelected)
        {
            button.image.sprite = isSelected ? selectedSprite : normalSprite;
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
                selectedEffect = value; // update `selectedEffect` before setting `Selected` to true on an effect descriptor
                if (value == null)
                {
                    UpdateColors();
                    selectedEffectNameTextLeftHand.text = "";
                    selectedEffectNameTextRightHand.text = "";
                    IsPlaceIndicatorActive = false;
                    IsDeleteIndicatorActive = false;
                    laser.gameObject.SetActive(false);
                }
                else
                {
                    value.Selected = true;
                    selectedEffectNameTextLeftHand.text = value.EffectName;
                    selectedEffectNameTextRightHand.text = value.EffectName;
                    if (IsHeld)
                        laser.gameObject.SetActive(true);
                    deleteIndicator.localScale = value.effectScale;
                    placeIndicatorForwardsArrow.SetActive(!value.randomizeRotation);
                }
                UpdateUseText();
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
                    if (!Networking.LocalPlayer.IsUserInVR())
                    {
                        mainWindow.SetParent(screenUIContainer, false);
                        confirmationWindow.SetParent(screenUIContainer, false);
                        helpWindow.SetParent(screenUIContainer, false);
                        uiCanvasCollider.enabled = false;
                    }
                    if (!initialized && selectedEffectIndex != -1)
                        Init();
                    UManager.Register(this);
                    if (SelectedEffect != null)
                        laser.gameObject.SetActive(true);
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
                    lastHoldingPlayerId = Networking.LocalPlayer.playerId;
                    lastHeldTime = Time.time;
                    RequestSerialization();
                }
                else
                {
                    if (!Networking.LocalPlayer.IsUserInVR())
                    {
                        mainWindow.SetParent(itemUIContainer, false);
                        confirmationWindow.SetParent(itemUIContainer, false);
                        helpWindow.SetParent(itemUIContainer, false);
                        if (itemUIContainer.gameObject.activeSelf)
                            uiCanvasCollider.enabled = true;
                    }
                    IsPlaceIndicatorActive = false;
                    IsDeleteIndicatorActive = false;
                    UManager.Deregister(this);
                    laser.gameObject.SetActive(false);
                }
            }
        }
        private int deleteTargetIndex;
        private bool isDeleteIndicatorActive;
        private bool IsDeleteIndicatorActive
        {
            get => isDeleteIndicatorActive;
            set
            {
                if (isDeleteIndicatorActive == value)
                    return;
                isDeleteIndicatorActive = value;
                deleteIndicator.gameObject.SetActive(value);
                secondLaser.gameObject.SetActive(value);
                UpdateUseText();
            }
        }
        private bool isPlaceIndicatorActive;
        private bool IsPlaceIndicatorActive
        {
            get => isPlaceIndicatorActive;
            set
            {
                if (isPlaceIndicatorActive == value)
                    return;
                isPlaceIndicatorActive = value;
                placeIndicator.gameObject.SetActive(value);
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
                    if (IsHeld) // Only drop if the local player is holding this gun otherwise I believe turning visibility off
                        pickup.Drop(); // would make every currently held gun get dropped
                }
                pickup.pickupable = value;
                // don't toggle the GameObject the pickup and object sync are on because it quite literally breaks VRChat
                // (it throws a null reference exception internally somewhere). So instead this this is using a child that
                // then has all the actual children as its children
                pickup.transform.GetChild(0).gameObject.SetActive(value);
            }
        }

        private bool holdingTab;
        private const float HoldingTabDelay = 0.6f;
        private const float RepeatingTabDelay = 1f / 25f;
        private float nextTabTime;

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
            legendText.text = $"[<b><color=#{ToHex(activeColor, false):X6}>once</color>: <color=#{ToHex(activeColor, false):X6}>on</color>/<color=#{ToHex(inactiveColor, false):X6}>off</color></b>] "
                + $"[<b><color=#{ToHex(activeLoopColor, false):X6}>loop</color>: <color=#{ToHex(activeLoopColor, false):X6}>on</color>/<color=#{ToHex(inactiveLoopColor, false):X6}>off</color></b>] "
                + $"[<b><color=#{ToHex(activeObjectColor, false):X6}>object</color>: <color=#{ToHex(activeObjectColor, false):X6}>on</color>/<color=#{ToHex(inactiveObjectColor, false):X6}>off</color></b>]";
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
            placeDeleteModeToggle.gameObject.SetActive(true);
            laserBaseScale = laser.localScale.z;
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

        private void UpdateUseText()
        {
            if (SelectedEffect == null)
                pickup.UseText = "";
            else
            {
                switch (Mode)
                {
                    case PlaceMode:
                        pickup.UseText = $"Place {SelectedEffect.EffectName}";
                        break;
                    case DeleteMode:
                        if (SelectedEffect.IsToggle)
                            pickup.UseText = IsDeleteIndicatorActive ? $"Delete {SelectedEffect.EffectName}" : "";
                        else
                            pickup.UseText = ""; // once effects currently cannot be deleted
                        break;
                    case EditMode:
                        pickup.UseText = $"Edit {SelectedEffect.EffectName}";
                        break;
                    default:
                        pickup.UseText = "";
                        break;
                }
            }
        }

        public void DeselectEffect()
        {
            SelectedEffect = null;
            if (!KeepOpenToggle.isOn)
                CloseUI();
        }

        public void DeleteEverything() => confirmationWindow.gameObject.SetActive(true);
        public void CancelDeleteEverything() => confirmationWindow.gameObject.SetActive(false);
        public void ConfirmDeleteEverything()
        {
            confirmationWindow.gameObject.SetActive(false);
            // TODO: spread work out across frames (probably)
            foreach (var descriptor in descriptors)
                descriptor.StopAllEffects();
        }

        public void ShowHelp() => helpWindow.gameObject.SetActive(true);
        public void HideHelp() => helpWindow.gameObject.SetActive(false);
        private void ToggleHelp()
        {
            if (helpWindow.gameObject.activeSelf)
                HideHelp();
            else
                ShowHelp();
        }

        public void ToggleUI() => SetUIActive(!itemUIContainer.gameObject.activeSelf);
        public void CloseUI() => SetUIActive(false);
        public void SetUIActive(bool active)
        {
            if (!initialized && active)
                Init();
            itemUIContainer.gameObject.SetActive(active);
            screenUIContainer.gameObject.SetActive(active);
            if (Networking.LocalPlayer != null && !Networking.LocalPlayer.IsUserInVR() && !IsHeld)
                uiCanvasCollider.enabled = active;
            uiToggle.gameObject.SetActive(!active);
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
            if (SelectedEffect == null)
                return;
            if (IsPlaceMode)
            {
                if (IsPlaceIndicatorActive)
                {
                    SelectedEffect.PlayEffect(placeIndicator.position, placeIndicator.rotation);
                    IsPlaceIndicatorActive = false;
                }
            }
            else if (IsDeleteMode)
            {
                if (IsDeleteIndicatorActive)
                {
                    SelectedEffect.StopToggleEffect(deleteTargetIndex);
                    IsDeleteIndicatorActive = false;
                }
            }
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

        private void ScrollToSelectedEffect()
        {
            if (SelectedEffect == null)
                return;

            /*
            logic behind keeping the selected effect withing the 2nd or 3rd row

            we need to calculate the distance from the selected effect button to the center of the scroll view.
            if that distance exceeds a certain value (45) then the distance must be clamped to 45 and the new content position
            can be calculated from there.

            so what do we need?

            the current position of the content
            the current position of the selected effect button

            the position in the content that is the current center of the scroll view
            is half the scroll view height (so 180) + the current position of the content

            the position of the button is its current row * the button height (so 90) + half the button height (so 45)

            the distance from the button to the center is the position of the button - the current center position of the content

            if the value is positive the button is above the center, if the value is negative the button is underneath the center

            if the absolute value of the distance is > half the button height (so 45) we have to clamp it, calculate the ultimate
            difference between 45 and the current distance and then apply that inverted difference to the content position,
            while making sure to clamp the contents position to 0 and the largest valid number, which is calculated somehow
            */

            var currentContentPosition = scrollRect.content.anchoredPosition.y;

            // selected button position in content
            var currentRow = SelectedEffect.Index / columnCount;
            var currentButtonPosition = ((float)currentRow + 0.5f) * buttonHeight;

            var contentPositionScrollViewCenter = currentContentPosition + buttonHeight * 2f;

            var buttonDistanceFromCenter = currentButtonPosition - contentPositionScrollViewCenter;

            if (Mathf.Abs(buttonDistanceFromCenter) > buttonHeight / 2f)
            {
                var clampedPositionFromCenter = buttonDistanceFromCenter < 0f ? buttonHeight / -2f : buttonHeight / 2f;
                var positionDiff = buttonDistanceFromCenter - clampedPositionFromCenter;

                var rows = (descriptors.Length + columnCount - 1) / columnCount;
                Canvas.ForceUpdateCanvases();
                scrollRect.content.anchoredPosition = Vector2.up * Mathf.Clamp(currentContentPosition + positionDiff, 0f, Mathf.Max(0f, ((float)(rows - 4)) * buttonHeight));
            }
        }

        private void ProcessAlphaNumericKeyDown(int key)
        {
            int index = (key - 1 + 10) % 10; // make 1 => 0, ..., 0 => 9 (and the rest in between is also moved down 1)
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                index += 10;
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                index += 20;
            if (!initialized)
                Init();
            SelectedEffect = index >= descriptors.Length ? null : descriptors[index];
            ScrollToSelectedEffect();
        }

        private void ProcessTab()
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (SelectedEffect == null)
                    SelectedEffect = descriptors[descriptors.Length - 1];
                else
                    SelectedEffect = descriptors[(SelectedEffect.Index - 1 + descriptors.Length) % descriptors.Length];
                ScrollToSelectedEffect();
            }
            else
            {
                if (SelectedEffect == null)
                    SelectedEffect = descriptors[0];
                else
                    SelectedEffect = descriptors[(SelectedEffect.Index + 1) % descriptors.Length];
                ScrollToSelectedEffect();
            }
        }

        public void CustomUpdate()
        {
            if (holdingTab)
            {
                if (Input.GetKey(KeyCode.Tab))
                {
                    var time = Time.time;
                    if (time >= nextTabTime)
                    {
                        ProcessTab();
                        nextTabTime = time + RepeatingTabDelay;
                    }
                }
                else
                    holdingTab = false;
            }

            if (Input.anyKeyDown) // since Udon is slow, check if anything was even pressed first before figuring out which one it was
            {
                // misc
                if (Input.GetKeyDown(KeyCode.E))
                    ToggleUI();

                // mode selection
                if (Input.GetKeyDown(KeyCode.F))
                {
                    if (!initialized)
                        Init();
                    if (IsPlaceMode)
                        Mode = DeleteMode;
                    else
                        Mode = PlaceMode;
                }
                // if (Input.GetKeyDown(KeyCode.R)) // can't use R
                //     SwitchToEditMode();

                // effect selection
                if (Input.GetKeyDown(KeyCode.Q))
                    SelectedEffect = null;
                else if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (!initialized)
                        Init();
                    ProcessTab();
                    holdingTab = true;
                    nextTabTime = Time.time + HoldingTabDelay;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha1))
                    ProcessAlphaNumericKeyDown(1);
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                    ProcessAlphaNumericKeyDown(2);
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                    ProcessAlphaNumericKeyDown(3);
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                    ProcessAlphaNumericKeyDown(4);
                else if (Input.GetKeyDown(KeyCode.Alpha5))
                    ProcessAlphaNumericKeyDown(5);
                else if (Input.GetKeyDown(KeyCode.Alpha6))
                    ProcessAlphaNumericKeyDown(6);
                else if (Input.GetKeyDown(KeyCode.Alpha7))
                    ProcessAlphaNumericKeyDown(7);
                else if (Input.GetKeyDown(KeyCode.Alpha8))
                    ProcessAlphaNumericKeyDown(8);
                else if (Input.GetKeyDown(KeyCode.Alpha9))
                    ProcessAlphaNumericKeyDown(9);
                else if (Input.GetKeyDown(KeyCode.Alpha0))
                    ProcessAlphaNumericKeyDown(0);
            }

            if (SelectedEffect == null)
                return;

            RaycastHit hit;
            if (Physics.Raycast(aimPoint.position, aimPoint.forward, out hit, maxDistance, rayLayerMask.value))
            {
                laser.localScale = new Vector3(1f, 1f, (aimPoint.position - hit.point).magnitude * laserBaseScale);
                if (IsPlaceMode)
                {
                    placeIndicator.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal, aimPoint.forward));
                    IsPlaceIndicatorActive = true;
                }
                else if (IsDeleteMode)
                {
                    // NOTE: this whole logic is very most likely a big performance concern, mostly because of GetNearestActiveEffect
                    if (!SelectedEffect.IsToggle || SelectedEffect.ActiveCount == 0)
                    {
                        IsDeleteIndicatorActive = false;
                        return;
                    }
                    Transform effectParent;
                    Transform mainEffectTransform = ((Component)SelectedEffect).transform; // UdonSharp being picky
                    if (hit.transform.IsChildOf(mainEffectTransform))
                    {
                        effectParent = hit.transform;
                        while (true)
                        {
                            var parent = effectParent.parent;
                            if (parent == mainEffectTransform)
                                break;
                            effectParent = parent;
                        }
                        deleteTargetIndex = effectParent.GetSiblingIndex() - 2;
                    }
                    else
                    {
                        deleteTargetIndex = SelectedEffect.GetNearestActiveEffect(hit.point);
                        effectParent = SelectedEffect.EffectParents[deleteTargetIndex];
                    }
                    Vector3 position = effectParent.position + effectParent.TransformDirection(SelectedEffect.effectLocalCenter);
                    if (SelectedEffect.doLimitDistance && (position - hit.point).magnitude > Mathf.Max(1f, SelectedEffect.effectScale.x * 0.65f))
                    {
                        IsDeleteIndicatorActive = false;
                        return;
                    }
                    deleteIndicator.position = position;
                    secondLaser.localScale = new Vector3(1f, 1f, (aimPoint.position - position).magnitude * laserBaseScale);
                    secondLaser.LookAt(position);
                    IsDeleteIndicatorActive = true;
                }
            }
            else
            {
                laser.localScale = new Vector3(1f, 1f, maxDistance * laserBaseScale);
                IsPlaceIndicatorActive = false;
                IsDeleteIndicatorActive = false;
            }
        }



        public int LastHoldingPlayerId => lastHoldingPlayerId;
        private float lastHeldTime = float.NaN;
        public float LastHeldTime => lastHeldTime;
        [UdonSynced] private int lastHoldingPlayerId;
        [UdonSynced] private float lastHeldTimeOffset;
        [UdonSynced] private int selectedEffectIndex = -1;
        private bool isReceiving;

        public override void OnPreSerialization()
        {
            selectedEffectIndex = SelectedEffect == null ? -1 : SelectedEffect.Index;
            lastHeldTimeOffset = lastHeldTime - Time.time;
        }

        public override void OnDeserialization()
        {
            isReceiving = true;
            if (!initialized && IsHeld) // someone else pressed a button while this client was holding it and didn't have the UI open before
                Init();
            if (initialized)
                SelectedEffect = selectedEffectIndex == -1 ? null : descriptors[selectedEffectIndex];
            lastHeldTime = lastHeldTimeOffset + Time.time;
            isReceiving = false;
        }
    }
}
