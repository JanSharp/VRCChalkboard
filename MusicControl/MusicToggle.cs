using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MusicToggle : UdonSharpBehaviour
    {
        public MusicDescriptor musicForThisArea;
        private uint id;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            id = musicForThisArea.AddThisMusic();
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            musicForThisArea.Manager.RemoveMusic(id);
        }
    }
}
