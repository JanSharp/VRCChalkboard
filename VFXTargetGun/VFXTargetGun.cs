using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VFXTargetGun : UdonSharpBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private LayerMask rayLayerMask = -1; // everything
        [SerializeField] private Color inactiveColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveLoopColor = new Color(0.2f, 0.7f, 1f);
        [SerializeField] private Color activeLoopColor = Color.cyan;
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
        [SerializeField] private Renderer uiToggleRenderer;
        [SerializeField] private Toggle keepOpenToggle;
        public Toggle KeepOpenToggle => keepOpenToggle;
        [SerializeField] private TextMeshPro selectedEffectNameText;
        [SerializeField] private Text legendText;

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
                else if (selectedEffect != null)
                    selectedEffect.Selected = false;
                selectedEffect = value;
                selectedEffect.Selected = true;
                selectedEffectNameText.text = value.EffectName;
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
            InactiveColor = MakeColorBlock(inactiveColor);
            ActiveColor = MakeColorBlock(activeColor);
            InactiveLoopColor = MakeColorBlock(inactiveLoopColor);
            ActiveLoopColor = MakeColorBlock(activeLoopColor);
            legendText.text = $"[<b>Bold=Selected</b>]"
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
                    descriptor.Init(this);
            }
            int rows = (count + columnCount - 1) / columnCount;
            buttonGrid.sizeDelta = new Vector2(buttonGrid.sizeDelta.x, buttonHeight * rows);
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
            if (selectedEffect == null)
                return;
            if (!IsTargetIndicatorActive)
            {
                // allow disabling of loop effects without pointing at any object
                if (selectedEffect.Loop && selectedEffect.ActiveCount != 0)
                    selectedEffect.PlayEffect(new Vector3(), new Quaternion());
                return;
            }
            selectedEffect.PlayEffect(targetIndicator.position, targetIndicator.rotation);
            if (selectedEffect.Loop)
                IsTargetIndicatorActive = false;
        }

        public void UpdateColors()
        {
            if (SelectedEffect.ActiveCount == 0)
                uiToggleRenderer.material.color = SelectedEffect.Loop ? inactiveLoopColor : inactiveColor;
            else
                uiToggleRenderer.material.color = SelectedEffect.Loop ? activeLoopColor : activeColor;
        }

        public void CustomUpdate()
        {
            // don't show an indicator if the loop is currently active
            if (selectedEffect.Loop && selectedEffect.ActiveCount != 0)
                return;
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
