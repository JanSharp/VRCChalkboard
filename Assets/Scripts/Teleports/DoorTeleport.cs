using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
#endif

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DoorTeleport : UdonSharpBehaviour
{
    public Transform source;
    public Transform target;

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        Quaternion rotationDiffBetweenDoors = Quaternion.Inverse(source.rotation * Quaternion.Euler(0f, 180f, 0f)) * target.rotation;
        Vector3 playerVelocity = player.GetVelocity();
        player.TeleportTo(target.position, player.GetRotation() * rotationDiffBetweenDoors, VRC_SceneDescriptor.SpawnOrientation.Default, false);
        player.SetVelocity(rotationDiffBetweenDoors * playerVelocity);
    }

    // alright, well this works. really well actually
    // but there is one major issue
    // it makes hooking it all up in the inspector all the more of a pain
    // using prefabs doesn't really help with it either
    // so we have an issue
    // we need an editor tool that allows us to easily create linked doors
    // if only it were that easy, right?
    // it really isn't easy, unfortunately
    // hmm
    // I'm thinking about a nice point and click feature, you know
    // but unity really doesn't make that easy
    // at least I do not know about a straight forward way to make a script that runs in the editor
    // that handles click events only if it is selected or something
    // it's just not simple...
    // but what if it is simple? and i just don't know about it?
    // surely
}

#if UNITY_EDITOR && !COMPILER_UDONSHARP
[CustomEditor(typeof(DoorTeleport))]
public class DoorTeleportEditor : Editor
{
    private bool configuringCollider;
    private int controlId;

    void OnEnable()
    {
        configuringCollider = false;
        controlId = GUIUtility.GetControlID(FocusType.Passive);
    }

    public override void OnInspectorGUI()
    {
        var target = this.target as DoorTeleport;
        if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
            return;
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        configuringCollider = EditorGUILayout.Toggle(new GUIContent("Configure BoxCollider", "Do Stuff"), configuringCollider);
    }

    void OnSceneGUI()
    {
        var target = this.target as DoorTeleport;
        if (!configuringCollider)
            return;
        var e = Event.current;
        switch (e.type)
        {
            case EventType.Layout:
                HandleUtility.AddDefaultControl(controlId);
                break;
            case EventType.MouseUp:
                var cam = SceneView.currentDrawingSceneView.camera;
                Vector2 mousePos = e.mousePosition;
                mousePos.y = cam.pixelHeight - mousePos.y;
                if (Physics.Raycast(cam.ScreenPointToRay(mousePos), out var hit, 1000f, -1))
                {
                    // TODO: expand box collider;
                }
                e.Use();
                break;
        }
    }
}
#endif
