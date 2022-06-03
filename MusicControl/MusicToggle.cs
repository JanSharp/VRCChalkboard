
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MusicToggle : UdonSharpBehaviour
{
    public MusicDescriptor musicForThisArea;
    private uint id;

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        id = musicForThisArea.PushThisMusic();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        musicForThisArea.Manager.RemoveMusic(id);
    }
}
