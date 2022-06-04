
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DefaultMusicSwitch : UdonSharpBehaviour
{
    public MusicManager musicManager;
    public MusicDescriptor musicToSwitchTo;

    public void Switch()
    {
        musicManager.DefaultMusic = musicToSwitchTo;
    }
}
