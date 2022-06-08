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
        [HideInInspector]
        public EffectDescriptor descriptor;

        public void OnClick() => descriptor.SelectThisEffect();

        public void OnStopClick() => descriptor.PlayEffect(new Vector3(), new Quaternion());
    }
}
