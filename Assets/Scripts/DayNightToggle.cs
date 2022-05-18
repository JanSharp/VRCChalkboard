
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DayNightToggle : UdonSharpBehaviour
{
    public float dayStuff;
    
    public float nightStuff;

    [UdonSynced]
    [FieldChangeCallback(nameof(isNightStateOnChange))]
    private bool isNightState;

    private bool isNightStateOnChange
    {
        get
        {
            return isNightState;
        }
        set
        {
            bool oldValue = isNightState;
            isNightState = value;
            if (oldValue != value)
            {
                SkyChange();
            }
        }
    }

    public void ToggleDay()
    {
        Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        isNightStateOnChange = true;
        RequestSerialization();
    }

    public void ToggleNight()
    {
        Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        isNightStateOnChange = false;
        RequestSerialization();
    }

    public void SkyChange()
    {
        if (isNightState)
        {
            RenderSettings.fogDensity = dayStuff;
        }
        else
        {
            RenderSettings.fogDensity = nightStuff;
        }
    }
}
