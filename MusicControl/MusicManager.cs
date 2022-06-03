
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MusicManager : UdonSharpBehaviour
{
    public MusicDescriptor[] descriptors;
    public MusicDescriptor spawnMusic;

    private MusicDescriptor currentlyPlaying;
    private bool musicIsMuted;

    void Start()
    {
        if (descriptors == null || descriptors.Length == 0)
        {
            Debug.LogError($"MusicManager {name} is missing music descriptors.");
            return;
        }
        for (int i = 0; i < descriptors.Length; i++)
            descriptors[i].Init(this);
        SwitchMusic(spawnMusic);
    }

    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if (player.isLocal)
            SwitchMusic(spawnMusic);
    }

    public void ToggleMuteMusic() => SetMutedMusic(!musicIsMuted);

    public void MuteMusic() => SetMutedMusic(true);

    public void UnMuteMusic() => SetMutedMusic(false);

    public void SetMutedMusic(bool value)
    {
        if (musicIsMuted != value)
        {
            if (musicIsMuted)
            {
                if (currentlyPlaying != null)
                    currentlyPlaying.Play();
            }
            else
            {
                if (currentlyPlaying != null)
                    currentlyPlaying.Stop();
            }
            musicIsMuted = value;
        }
    }

    public void SwitchMusic(MusicDescriptor toSwitchTo)
    {
        if (currentlyPlaying != null)
            currentlyPlaying.Stop();
        if (toSwitchTo != null && !musicIsMuted)
            toSwitchTo.Play();
        currentlyPlaying = toSwitchTo;
    }
}
