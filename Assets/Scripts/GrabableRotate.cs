using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

///cSpell:ignore grabable, lerp

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GrabableRotate : UdonSharpBehaviour
{
    [SerializeField] private Transform toRotate;
    public string interactionText = "Rotate";

    private UpdateManager updateManager;
    private VRC_Pickup pickup;
    private float initialDistance;
    private float nextSyncTime;
    private const float SyncInterval = 0.2f;
    private bool isRegistered;
    private bool isReceiving;
    private float lerpStartTime;
    private Quaternion lerpStartRotation;
    private Quaternion prevRotation;

    [UdonSynced] private bool currentlyHeld;
    [UdonSynced] private Quaternion syncedRotation;

    // for UpdateManager
    private int customUpdateInternalIndex;

    public void Start()
    {
        pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
        var updateManagerObj = GameObject.Find("/UpdateManager");
        updateManager = updateManagerObj == null ? null : (UpdateManager)updateManagerObj.GetComponent(typeof(UdonBehaviour));
        if (updateManager == null)
            Debug.LogError("GrabableRotate requires a GameObject that must be at the root of the scene with the exact name 'UpdateManager' which has the 'UpdateManager' UdonBehaviour.");
        // initialOffset = this.transform.position - toRotate.position;
        // initialRotation = toRotate.rotation;
        initialDistance = (this.transform.position - toRotate.position).magnitude;
        SnapBack();
    }

    private void SnapBack()
    {
        this.transform.position = toRotate.position + (toRotate.rotation * (Vector3.back * initialDistance));
        this.transform.rotation = toRotate.rotation;
    }

    public override void OnPickup()
    {
        Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        isReceiving = false;
        currentlyHeld = true;
        RequestSerialization();
        Register();
        nextSyncTime = Time.time + SyncInterval;
    }

    public override void OnDrop()
    {
        currentlyHeld = false;
        RequestSerialization();
        SnapBack();
        Deregister();
    }

    public void CustomUpdate()
    {
        if (isReceiving)
        {
            var percent = (Time.time - lerpStartTime) / (SyncInterval + 0.05f);
            Quaternion extraRotationSinceLastFrame = Quaternion.Inverse(prevRotation) * toRotate.rotation;
            toRotate.rotation = Quaternion.Lerp(lerpStartRotation, syncedRotation, percent) * extraRotationSinceLastFrame;
            if (percent >= 1f)
                Deregister();
            prevRotation = toRotate.rotation;
        }
        else
        {
            toRotate.LookAt(toRotate.position - (this.transform.position - toRotate.position));
            if (Time.time >= nextSyncTime)
            {
                RequestSerialization();
                nextSyncTime = Time.time + SyncInterval;
            }
        }
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        // just for safety because I do not trust VRC
        // would be hilarious if this ended up running after OnDeserialization
        // causing the entire logic to be pointless/broken. Funny indeed
        pickup.pickupable = true;
    }

    public override void OnPreSerialization()
    {
        syncedRotation = toRotate.rotation;
    }

    public override void OnDeserialization()
    {
        pickup.pickupable = !currentlyHeld;
        if (currentlyHeld)
        {
            isReceiving = true;
            lerpStartRotation = toRotate.rotation;
            prevRotation = toRotate.rotation;
            Register();
            lerpStartTime = Time.time;
        }
    }

    public void Register()
    {
        if (isRegistered)
            return;
        isRegistered = true;
        updateManager.Register(this);
    }

    public void Deregister()
    {
        if (!isRegistered)
            return;
        isRegistered = false;
        updateManager.Deregister(this);
    }
}

#if !COMPILER_UDONSHARP && UNITY_EDITOR
[CustomEditor(typeof(GrabableRotate))]
public class GrabableRotateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GrabableRotate target = this.target as GrabableRotate;
        if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
            return;
        EditorGUILayout.Space();
        base.OnInspectorGUI(); // draws public/serializable fields

        EditorGUILayout.Space();
        var pickup = target.GetComponent<VRC.SDK3.Components.VRCPickup>();
        if (pickup == null)
        {
            if (GUILayout.Button(new GUIContent("Add VRC Pickup", "Adds VRC Pickup component and configures it and the necessary Rigidbody.")))
                AddAndConfigureComponents(target);
        }
        else
        {
            if (GUILayout.Button(new GUIContent("Configure VRC Pickup", "Configures the attached VRC Pickup and Rigidbody components.")))
                ConfigureComponents(target, pickup, target.GetComponent<Rigidbody>());
        }
    }

    public static void AddAndConfigureComponents(GrabableRotate target)
    {
        var rigidbody = target.GetComponent<Rigidbody>();
        rigidbody = rigidbody != null ? rigidbody : target.gameObject.AddComponent<Rigidbody>();
        var pickup = target.GetComponent<VRC.SDK3.Components.VRCPickup>();
        pickup = pickup != null ? pickup : target.gameObject.AddComponent<VRC.SDK3.Components.VRCPickup>();
        ConfigureComponents(target, pickup, rigidbody);
    }

    public static void ConfigureComponents(GrabableRotate target, VRC.SDK3.Components.VRCPickup pickup, Rigidbody rigidbody)
    {
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        pickup.AutoHold = VRC_Pickup.AutoHoldMode.No;
        pickup.ExactGrip = null;
        pickup.InteractionText = target.interactionText;
        pickup.orientation = VRC_Pickup.PickupOrientation.Grip;
        pickup.pickupable = true;
    }
}

// didn't work for vrc builds unfortunately
// nor does it do anything on play, but that was expected
// public class GrabableRotateOnBuild : UnityEditor.Build.IPreprocessBuildWithReport
// {
//     public int callbackOrder => 0;
//     public void OnPreprocessBuild(BuildReport report)
//     {
//         foreach (var obj in GameObject.FindObjectsOfType<UdonBehaviour>())
//             foreach (var grabableRotate in obj.GetUdonSharpComponents<GrabableRotate>())
//                 GrabableRotateEditor.AddAndConfigureComponents(grabableRotate);
//     }
// }
#endif
