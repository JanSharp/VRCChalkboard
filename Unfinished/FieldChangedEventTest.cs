using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FieldChangedEventTest : UdonSharpBehaviour
    {
        [UdonSynced]
        [FieldChangeCallback(nameof(Value))]
        public int value;
        public int Value
        {
            get => value;
            set
            {
                Debug.Log($"<dlt> old value: {this.value}, new value: {value}");
                this.value = value;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                RequestSerialization();
                Debug.Log("<dlt> Requesting serialization");
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                Value++;
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            Debug.Log($"<dlt> {player.displayName} joined the world");
        }

        public override void OnPreSerialization()
        {
            Debug.Log($"<dlt> OnPreSerialization, value: {value}");
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            Debug.Log($"<dlt> OnPostSerialization, value: {value}, success: {result.success}, byteCount: {result.byteCount}");
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            Debug.Log($"<dlt> OnDeserialization, value: {value}");
        }
    }
}
