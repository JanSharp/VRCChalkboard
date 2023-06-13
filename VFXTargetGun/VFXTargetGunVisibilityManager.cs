using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
using System.Linq;
#endif

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VFXTargetGunVisibilityManager : UdonSharpBehaviour
    {
        public VFXTargetGun[] guns;
        public bool initialVisibility;
        private bool currentVisibility;
        public bool IsVisible
        {
            get => currentVisibility;
            set => SetIsVisible(value);
        }

        void Start()
        {
            currentVisibility = !initialVisibility;
            SetIsVisible(initialVisibility);
        }

        public void SetIsVisible(bool value)
        {
            if (value == currentVisibility)
                return;
            currentVisibility = value;
            foreach (var gun in guns)
                gun.IsVisible = value;
        }

        public void ToggleVisibility() => SetIsVisible(!currentVisibility);
        public void SetInvisible() => SetIsVisible(false);
        public void SetVisible() => SetIsVisible(true);
    }

    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(VFXTargetGunVisibilityManager))]
    public class VFXTargetGunVisibilityManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;
            base.OnInspectorGUI();

            if (GUILayout.Button(new GUIContent("Update Duplicates",
                "Deletes all guns in the array except the first one and copies said first one to replace the deleted ones again.")))
            {
                var target = (VFXTargetGunVisibilityManager)this.target;
                if (target.guns.Any(g => g == null))
                {
                    Debug.LogError($"{target.name} has at least 1 gun in the guns array that is null, aborting updating duplicates.");
                    return;
                }
                var data = target.guns
                    .Skip(1)
                    .Select((g, i) => (i: i + 1, g.transform, g.name, g.transform.position, g.transform.rotation, g.transform.localScale, g.transform.parent))
                    .ToList();
                var original = target.guns[0].gameObject;
                Undo.RecordObject(target, "Update Duplicates");
                foreach (var d in data)
                {
                    Undo.DestroyObjectImmediate(d.transform.gameObject);
                    var copy = Instantiate(original, d.position, d.rotation, d.parent);
                    copy.name = d.name;
                    copy.transform.localScale = d.localScale;
                    target.guns[d.i] = copy.GetComponent<VFXTargetGun>();
                    Undo.RegisterCreatedObjectUndo(copy, "Updated Duplicate");
                }
            }
        }
    }
    #endif
}
