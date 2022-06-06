using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EffectButtonData : UdonSharpBehaviour
    {
        public Text text;
        public Button button;
        [HideInInspector]
        public EffectDescriptor descriptor;

        public void OnClick() => descriptor.SelectThisEffect();
    }
}
