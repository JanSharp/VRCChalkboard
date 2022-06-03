
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MusicToggle : UdonSharpBehaviour
{
    public MusicDescriptor musicForThisArea;

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        musicForThisArea.PushThisMusic();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        musicForThisArea.RemoveThisMusic();
    }
}
