using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EffectButtonData : UdonSharpBehaviour
    {
        public TextMeshProUGUI text;
        public Button button;
        public GameObject stopButton;
        public TextMeshProUGUI stopButtonText;
        public TextMeshProUGUI activeCountText;
        [HideInInspector] public EffectDescriptor descriptor;

        public void OnClick() => descriptor.SelectThisEffect();

        public void OnStopClick() => descriptor.StopAllEffects();
    }
}
