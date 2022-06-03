using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MusicManager : UdonSharpBehaviour
{
    public MusicDescriptor[] descriptors;
    [SerializeField] private MusicDescriptor defaultMusic;
    [SerializeField] private bool syncCurrentDefaultMusic;
    public MusicDescriptor DefaultMusic
    {
        get => defaultMusic;
        set
        {
            if (defaultMusic == value)
                return;
            ReplaceMusic(0, value);
            defaultMusic = value;
            defaultMusicIndex = value.Index;
            if (syncCurrentDefaultMusic && !receivingData)
            {
                Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                RequestSerialization();
            }
        }
    }
    [UdonSynced]
    private int defaultMusicIndex;
    private bool receivingData;

    public override void OnDeserialization()
    {
        receivingData = true;
        DefaultMusic = descriptors[defaultMusicIndex];
        receivingData = false;
    }

    private MusicDescriptor currentlyPlaying;
    /// <summary>
    /// This isn't actually truly a stack. The only real difference is that popping doesn't pop off the top of the stack
    /// but instead it removes a specific descriptor that's closest to top. If it was at top, music switches to the new top.
    /// Default/Default also breaks the rules because it always lives at index 0, even if it is null or when it gets replaced.
    /// </summary>
    private MusicDescriptor[] musicList;
    private int musicListCount;
    private bool muted;
    public bool Muted
    {
        get => muted;
        set
        {
            if (muted == value)
                return;
            if (muted)
            {
                if (currentlyPlaying != null)
                    currentlyPlaying.Play();
            }
            else
            {
                if (currentlyPlaying != null)
                    currentlyPlaying.Stop();
            }
            muted = value;
        }
    }

    void Start()
    {
        if (descriptors == null || descriptors.Length == 0)
        {
            Debug.LogError($"MusicManager {name} is missing music descriptors.");
            return;
        }
        for (int i = 0; i < descriptors.Length; i++)
            descriptors[i].Init(this, i);
        musicList = new MusicDescriptor[8];
        PushMusic(DefaultMusic);
    }

    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            musicListCount = 1;
            SwitchToTop();
        }
    }

    // for convenience, specifically for hooking them up with GUI buttons
    public void ToggleMuteMusic() => Muted = !Muted;
    public void MuteMusic() => Muted = true;
    public void UnMuteMusic() => Muted = false;

    private void SwitchMusic(MusicDescriptor toSwitchTo)
    {
        if (toSwitchTo == currentlyPlaying)
            return;
        if (currentlyPlaying != null)
            currentlyPlaying.Stop();
        if (toSwitchTo != null && !Muted)
            toSwitchTo.Play();
        currentlyPlaying = toSwitchTo;
    }

    private void SwitchToTop()
    {
        SwitchMusic(musicList[musicListCount - 1]);
    }

    public void PushMusic(MusicDescriptor toPush)
    {
        if (musicListCount == musicList.Length)
            GrowMusicList();
        musicList[musicListCount] = toPush;
        musicListCount++;
        SwitchToTop();
    }

    private void GrowMusicList()
    {
        var newMusicList = new MusicDescriptor[musicList.Length * 2];
        for (int i = 0; i < musicList.Length; i++)
            newMusicList[i] = musicList[i];
        musicList = newMusicList;
    }

    public void RemoveMusic(MusicDescriptor toPop)
    {
        for (int i = musicListCount - 1; i >= 0; i--)
        {
            if (musicList[i] == toPop)
            {
                musicListCount--;
                for (int j = i; j < musicListCount; j++)
                    musicList[j] = musicList[j + 1];
                if (i == musicListCount)
                    SwitchToTop();
                return;
            }
        }
        Debug.LogError("Attempt to PopMusic a descriptor that is not in the music stack.");
    }

    private void ReplaceMusic(int index, MusicDescriptor descriptor)
    {
        musicList[index] = descriptor;
        if (index == musicListCount - 1)
            SwitchToTop();
    }
}
