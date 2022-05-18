
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MusicToggle : UdonSharpBehaviour
{
    public MusicDescriptor musicToSwitchToOnEnter;
    public MusicDescriptor musicToSwitchToOnExit;

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (musicToSwitchToOnEnter != null && player.isLocal)
            musicToSwitchToOnEnter.SwitchMusicToThis();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (musicToSwitchToOnExit != null && player.isLocal)
            musicToSwitchToOnExit.SwitchMusicToThis();
    }
}
