
#define ItemSyncDebug

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DebugController : UdonSharpBehaviour
{
    [UdonSynced] private float smallMagnitudeDiff = 0.005f;
    [UdonSynced] private float smallAngleDiff = 5f;
    [UdonSynced] private float consistentOffsetDuration = 0.3f;
    [UdonSynced] private int consistentOffsetFrameCount = 4;
    [UdonSynced] private float handMovementAngleDiff = 20f;
    [UdonSynced] private float interpolationDuration = 0.2f;

    public InputField smallMagnitudeDiffGUI;
    public InputField smallAngleDiffGUI;
    public InputField consistentOffsetDurationGUI;
    public InputField consistentOffsetFrameCountGUI;
    public InputField handMovementAngleDiffGUI;
    public InputField interpolationDurationGUI;
    [Space]
    public TextMeshPro itemStatesText;

    private JanItemSync[] items;
    private JanItemSync[] nonIdleItems;
    private int itemCount = 0;
    private int nonIdleCount = 0;

    public void Start()
    {
        if (items == null)
            items = new JanItemSync[128];
        if (nonIdleItems == null)
            nonIdleItems = new JanItemSync[128];
        UpdateGUI();
    }

    private void UpdateGUI()
    {
        smallMagnitudeDiffGUI.text = smallMagnitudeDiff.ToString();
        smallAngleDiffGUI.text = smallAngleDiff.ToString();
        consistentOffsetDurationGUI.text = consistentOffsetDuration.ToString();
        consistentOffsetFrameCountGUI.text = consistentOffsetFrameCount.ToString();
        handMovementAngleDiffGUI.text = handMovementAngleDiff.ToString();
        interpolationDurationGUI.text = interpolationDuration.ToString();
    }

    #if ItemSyncDebug
    public void Register(JanItemSync item)
    {
        if (items == null)
            items = new JanItemSync[128];
        if (itemCount == items.Length)
            GrowItems();
        items[itemCount] = item;
        item.debugIndex = itemCount;
        itemCount++;
    }

    public void Deregister(JanItemSync item)
    {
        if (items == null)
            items = new JanItemSync[128];
        int index = item.debugIndex;
        // move current top into the gap
        itemCount--;
        items[index] = items[itemCount];
        items[index].debugIndex = index;
        items[itemCount] = null;
    }

    private void GrowItems()
    {
        JanItemSync[] grownItems = new JanItemSync[items.Length * 2];
        for (int i = 0; i < items.Length; i++)
            grownItems[i] = items[i];
        items = grownItems;

        JanItemSync[] grownNonIdleItems = new JanItemSync[nonIdleItems.Length * 2];
        for (int i = 0; i < nonIdleItems.Length; i++)
            grownNonIdleItems[i] = nonIdleItems[i];
        nonIdleItems = grownNonIdleItems;
    }

    public void RegisterNonIdle(JanItemSync item)
    {
        if (nonIdleItems == null)
            nonIdleItems = new JanItemSync[128];
        nonIdleItems[nonIdleCount] = item;
        item.debugNonIdleIndex = nonIdleCount;
        nonIdleCount++;
    }

    public void DeregisterNonIdle(JanItemSync item)
    {
        if (nonIdleItems == null)
            nonIdleItems = new JanItemSync[128];
        int index = item.debugNonIdleIndex;
        // move current top into the gap
        nonIdleCount--;
        nonIdleItems[index] = nonIdleItems[nonIdleCount];
        nonIdleItems[index].debugNonIdleIndex = index;
        nonIdleItems[nonIdleCount] = null;
    }

    public void UpdateItemStatesText()
    {
        string str = null;
        for (int i = 0; i < System.Math.Min(100, nonIdleCount); i++)
        {
            str = str == null ? "" : str + "\n";
            var item = nonIdleItems[i];
            str += $"{Networking.GetOwner(item.gameObject).displayName}: '{item.name}':   {item.StateToString(item.State)}";
        }
        itemStatesText.text = str;
    }
    #endif

    private void UpdateItems()
    {
        for (int i = 0; i < itemCount; i++)
        {
            var item = items[i];
            item.SmallMagnitudeDiff = smallMagnitudeDiff;
            item.SmallAngleDiff = smallAngleDiff;
            item.ConsistentOffsetDuration = consistentOffsetDuration;
            item.ConsistentOffsetFrameCount = consistentOffsetFrameCount;
            item.HandMovementAngleDiff = handMovementAngleDiff;
            item.InterpolationDuration = interpolationDuration;
        }
    }

    public override void OnDeserialization()
    {
        UpdateItems();
        UpdateGUI();
    }

    public void OnSmallMagnitudeDiffEndText()
    {
        float value;
        if (float.TryParse(smallMagnitudeDiffGUI.text, out value))
            smallMagnitudeDiff = value;
        UpdateItems();
        RequestSerialization();
    }

    public void OnSmallAngleDiffEndText()
    {
        float value;
        if (float.TryParse(smallAngleDiffGUI.text, out value))
            smallAngleDiff = value;
        UpdateItems();
        RequestSerialization();
    }

    public void OnConsistentOffsetDurationEndText()
    {
        float value;
        if (float.TryParse(consistentOffsetDurationGUI.text, out value))
            consistentOffsetDuration = value;
        UpdateItems();
        RequestSerialization();
    }

    public void OnConsistentOffsetFrameCountEndText()
    {
        int value;
        if (int.TryParse(consistentOffsetFrameCountGUI.text, out value))
            consistentOffsetFrameCount = value;
        UpdateItems();
        RequestSerialization();
    }

    public void OnHandMovementAngleDiffEndText()
    {
        float value;
        if (float.TryParse(handMovementAngleDiffGUI.text, out value))
            handMovementAngleDiff = value;
        UpdateItems();
        RequestSerialization();
    }

    public void OnInterpolationDurationEndText()
    {
        float value;
        if (float.TryParse(interpolationDurationGUI.text, out value))
            interpolationDuration = value;
        UpdateItems();
        RequestSerialization();
    }
}
