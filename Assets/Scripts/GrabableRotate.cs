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
    public Transform toRotate;
    [Tooltip("Maximum amount of degrees the object to rotate is allowed to deviate from the original local rotation. 360 and above means unlimited, 0 or below means not at all.")]
    public float maximumRotationDeviation = 360f;

    private UpdateManager updateManager;
    private Transform dummyTransform;
    private VRC_Pickup pickup;
    private Quaternion initialLocalRotation;
    private float initialDistance;
    private float nextSyncTime;
    private const float SyncInterval = 0.2f;
    private bool isRegistered;
    private bool isReceiving;
    private float lerpStartTime;
    private Quaternion lerpStartRotation;
    private Quaternion prevRotation;
    private VRCPlayerApi holdingPlayer;
    private VRCPlayerApi HoldingPlayer
    {
        get => holdingPlayer;
        set
        {
            holdingPlayer = value;
            holdingPlayerIsInVR = value.IsUserInVR();
        }
    }
    private bool holdingPlayerIsInVR;

    /// <summary>
    /// <para>bit 0: is held</para>
    /// <para>bit 1: 0 means left hand, 1 means right hand (only used when the holding player is in VR)</para>
    /// </summary>
    [UdonSynced] private byte syncedData;
    private const byte IsHeldFlag = 1 << 0;
    private const byte HeldHandFlag = 1 << 1;
    private bool currentlyHeld; // synced through syncedData
    private HumanBodyBones currentHandBone; // synced through syncedData
    /// <summary>
    /// Used as the target rotation for interpolation when the holding user is in VR.
    /// Otherwise used as the rotation offset between the held hand rotation and the pickup rotation.
    /// </summary>
    [UdonSynced] private Quaternion syncedRotation;
    /// <summary>
    /// The offset from the held hand position and the pickup position;
    /// Only used when the holding user is in VR.
    /// </summary>
    [UdonSynced] private Vector3 syncedPosition;

    private byte ToHeldHandFlag(HumanBodyBones bone)
    {
        if (bone == HumanBodyBones.RightHand)
            return HeldHandFlag;
        return 0;
    }

    private HumanBodyBones ToHeldHandBone(byte flags)
    {
        if ((flags & HeldHandFlag) != 0)
            return HumanBodyBones.RightHand;
        return HumanBodyBones.LeftHand;
    }

    // for UpdateManager
    private int customUpdateInternalIndex;

    public void Start()
    {
        pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
        var updateManagerObj = GameObject.Find("/UpdateManager");
        updateManager = updateManagerObj == null ? null : (UpdateManager)updateManagerObj.GetComponent(typeof(UdonBehaviour));
        if (updateManager == null)
            Debug.LogError("GrabableRotate requires a GameObject that must be at the root of the scene with the exact name 'UpdateManager' which has the 'UpdateManager' UdonBehaviour.");
        initialLocalRotation = toRotate.localRotation;
        dummyTransform = updateManager.transform;
        initialDistance = (this.transform.position - toRotate.position).magnitude;
        SnapBack();
    }

    public void Snap(float distance)
    {
        this.transform.position = toRotate.position + (toRotate.rotation * (Vector3.back * distance));
        this.transform.rotation = toRotate.rotation;
    }

    private void SnapBack() => Snap(initialDistance);

    public override void OnPickup()
    {
        HoldingPlayer = Networking.LocalPlayer;
        Networking.SetOwner(HoldingPlayer, this.gameObject);
        isReceiving = false;
        currentlyHeld = true;
        currentHandBone = pickup.currentHand == VRC_Pickup.PickupHand.Right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand;
        RequestSerialization();
        Register();
        if (holdingPlayerIsInVR)
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
            if (holdingPlayerIsInVR && currentlyHeld) // when not currentlyHeld interpolate instead
            {
                // figure out the position of the pickup based on the current bone position
                this.transform.SetPositionAndRotation(
                    holdingPlayer.GetBonePosition(currentHandBone),
                    holdingPlayer.GetBoneRotation(currentHandBone)
                );
                this.transform.position += this.transform.TransformDirection(syncedPosition);
                this.transform.rotation *= syncedRotation;
                LookAtThisTransform();
            }
            else
            {
                var percent = (Time.time - lerpStartTime) / (SyncInterval + 0.05f);
                Quaternion extraRotationSinceLastFrame = Quaternion.Inverse(prevRotation) * toRotate.rotation;
                toRotate.rotation = Quaternion.Lerp(lerpStartRotation, syncedRotation, percent) * extraRotationSinceLastFrame;
                if (percent >= 1f)
                {
                    SnapBack();
                    pickup.pickupable = true;
                    Deregister();
                }
                prevRotation = toRotate.rotation;
            }
        }
        else
        {
            LookAtThisTransform();
            if (!holdingPlayerIsInVR && Time.time >= nextSyncTime)
            {
                RequestSerialization();
                nextSyncTime = Time.time + SyncInterval;
            }
        }
    }

    private void LookAtThisTransform()
    {
        toRotate.LookAt(toRotate.position - (this.transform.position - toRotate.position));
        Quaternion deviation = Quaternion.Inverse(initialLocalRotation) * toRotate.localRotation;
        float angle;
        Vector3 axis;
        deviation.ToAngleAxis(out angle, out axis);
        if (angle > maximumRotationDeviation)
        {
            deviation = Quaternion.AngleAxis(Mathf.Max(0f, maximumRotationDeviation), axis);
            toRotate.localRotation = initialLocalRotation * deviation;
        }
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        // just for safety because I do not trust VRC
        // would be hilarious if this ended up running after OnDeserialization
        // causing the entire logic to be pointless/broken. Funny indeed
        pickup.pickupable = true;
        SnapBack();
    }

    public override void OnPreSerialization()
    {
        syncedData = (byte)((currentlyHeld ? IsHeldFlag : 0) + ToHeldHandFlag(currentHandBone));
        if (holdingPlayerIsInVR && currentlyHeld)
        {
            Vector3 bonePosition = holdingPlayer.GetBonePosition(currentHandBone);
            Quaternion boneRotation = holdingPlayer.GetBoneRotation(currentHandBone);
            syncedRotation = Quaternion.Inverse(boneRotation) * this.transform.rotation;
            dummyTransform.SetPositionAndRotation(bonePosition, boneRotation);
            syncedPosition = dummyTransform.InverseTransformDirection(this.transform.position - bonePosition);
        }
        else
            syncedRotation = toRotate.rotation;
    }

    public override void OnDeserialization()
    {
        currentlyHeld = (syncedData & IsHeldFlag) != 0;
        currentHandBone = ToHeldHandBone(syncedData);
        if (currentlyHeld)
            pickup.pickupable = false;
        if (currentlyHeld)
        {
            isReceiving = true;
            HoldingPlayer = Networking.GetOwner(this.gameObject);
            if (holdingPlayerIsInVR)
            {
                Register();
            }
            else
            {
                lerpStartRotation = toRotate.rotation;
                prevRotation = toRotate.rotation;
                Register();
                lerpStartTime = Time.time;
            }
        }
        else if (holdingPlayerIsInVR && isRegistered)
        {
            // start interpolation when a player in VR dropped the pickup
            lerpStartRotation = toRotate.rotation;
            prevRotation = toRotate.rotation;
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
        if (GUILayout.Button(new GUIContent("Snap in line", "Snap to the back of the Transform 'To Rotate'. "
            + "This script relies on this pickup object being perfectly in line with the Transform it is rotating, "
            + "so this button allows you to snap it in place before entering play mode.")))
        {
            target.Snap((target.transform.position - target.toRotate.position).magnitude);
        }

        EditorGUILayout.Space();
        var pickup = target.GetComponent<VRC.SDK3.Components.VRCPickup>();
        if (pickup == null)
        {
            if (GUILayout.Button(new GUIContent("Add VRC Pickup", "Adds VRC Pickup component and configures it "
                + "and the necessary Rigidbody. Sets: useGravity = false; isKinematic = true; ExactGrip = null; orientation = Grip;\n"
                + "Setting orientation to grip and ExactGrip to null makes the pickup stay where it is while being picked up => "
                + "it doesn't move to the hand.\n"
                + "Unlike configuring it afterwards, this also initializes interactionText to 'Rotate' and sets AutoHold to 'no'.")))
            {
                AddAndConfigureComponents(target);
            }
        }
        else
        {
            if (GUILayout.Button(new GUIContent("Configure VRC Pickup", "Configures the attached VRC Pickup "
                + "and Rigidbody components. Sets: useGravity = false; isKinematic = true; ExactGrip = null; orientation = Grip;\n"
                + "Setting orientation to grip and ExactGrip to null makes the pickup stay where it is while being picked up => "
                + "it doesn't move to the hand.")))
            {
                ConfigureComponents(target, pickup, target.GetComponent<Rigidbody>());
            }
        }
    }

    public static void AddAndConfigureComponents(GrabableRotate target)
    {
        var rigidbody = target.GetComponent<Rigidbody>();
        rigidbody = rigidbody != null ? rigidbody : target.gameObject.AddComponent<Rigidbody>();
        var pickup = target.GetComponent<VRC.SDK3.Components.VRCPickup>();
        if (pickup == null)
        {
            pickup = target.gameObject.AddComponent<VRC.SDK3.Components.VRCPickup>();
            pickup.InteractionText = "Rotate";
            pickup.AutoHold = VRC_Pickup.AutoHoldMode.No;
        }
        ConfigureComponents(target, pickup, rigidbody);
    }

    public static void ConfigureComponents(GrabableRotate target, VRC.SDK3.Components.VRCPickup pickup, Rigidbody rigidbody)
    {
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        pickup.ExactGrip = null;
        pickup.orientation = VRC_Pickup.PickupOrientation.Grip;
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
