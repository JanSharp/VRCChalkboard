
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MusicDescriptor : UdonSharpBehaviour
{
    public float fadeInSeconds;
    public float fadeOutSeconds;
    public float updateIntervalInSeconds = 0.1f;

    private MusicManager manager;
    private AudioSource audioSource;
    private float maxVolume;
    private float lastFadeInTime;
    private bool fadingIn;
    private float lastFadeOutTime;
    private bool fadingOut;

    public void Init(MusicManager manager)
    {
        this.manager = manager;
        audioSource = (AudioSource)GetComponent(typeof(AudioSource));
        if (audioSource == null)
        {
            Debug.LogError($"Music Descriptor {name} is missing an AudioSource component.");
            return;
        }
        maxVolume = audioSource.volume;
        audioSource.volume = 0;
    }

    public void SwitchMusicToThis()
    {
        manager.SwitchMusic(this);
    }

    public void Play()
    {
        if (!audioSource.isPlaying)
            audioSource.Play();
        fadingIn = true;
        fadingOut = false;
        lastFadeInTime = Time.time - updateIntervalInSeconds;
        FadeIn();
    }

    public void FadeIn()
    {
        if (!fadingIn)
            return;
        float currentVolume = audioSource.volume;
        float volumePerSecond = maxVolume / fadeInSeconds;
        float currentTime = Time.time;
        float deltaTime = currentTime - lastFadeInTime;
        lastFadeInTime = currentTime;
        float step = volumePerSecond * deltaTime;
        currentVolume = Mathf.Min(currentVolume + step, maxVolume);
        audioSource.volume = currentVolume;
        if (currentVolume == maxVolume)
        {
            fadingIn = false;
            return;
        }
        SendCustomEventDelayedSeconds(nameof(FadeIn), updateIntervalInSeconds);
    }

    public void Stop()
    {
        if (!audioSource.isPlaying)
            return;
        fadingIn = false;
        fadingOut = true;
        lastFadeOutTime = Time.time - updateIntervalInSeconds;
        FadeOut();
    }

    public void FadeOut()
    {
        if (!fadingOut)
            return;
        float currentVolume = audioSource.volume;
        float volumePerSecond = maxVolume / fadeOutSeconds;
        float currentTime = Time.time;
        float deltaTime = currentTime - lastFadeOutTime;
        lastFadeOutTime = currentTime;
        float step = volumePerSecond * deltaTime;
        currentVolume = Mathf.Max(currentVolume - step, 0);
        audioSource.volume = currentVolume;
        if (currentVolume == 0)
        {
            fadingOut = false;
            audioSource.Stop();
            return;
        }
        SendCustomEventDelayedSeconds(nameof(FadeOut), updateIntervalInSeconds);
    }
}
