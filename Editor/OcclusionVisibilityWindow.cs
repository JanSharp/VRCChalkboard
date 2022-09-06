using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace JanSharp
{
    public class OcclusionVisibilityWindow : EditorWindow
    {
        [MenuItem("Tools/Occlusion Visibility")]
        public static void ShowVisibilityWindow()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<OcclusionVisibilityWindow>();
            wnd.titleContent = new GUIContent("Occlusion Visibility");
        }

        // private static void UpdateVisibility()
        // {
        //     SceneVisibilityManager.instance.HideAll();
        //     SceneVisibilityManager.instance.Show(Selection.gameObjects, true);
        // }

        public void CreateGUI()
        {
            // rootVisualElement.Add(new Label("Hello"));

            // var toggle = new Toggle("Only Show Selected");
            // toggle.RegisterValueChangedCallback(e => {
            //     if (e.newValue)
            //     {
            //         Selection.selectionChanged += UpdateVisibility;
            //         UpdateVisibility();
            //     }
            //     else
            //     {
            //         Selection.selectionChanged -= UpdateVisibility;
            //         SceneVisibilityManager.instance.ShowAll();
            //     }
            // });
            // rootVisualElement.Add(toggle);

            ///cSpell:ignore occluder, occludee, occluders, occludees

            var occluderToggle = new Toggle("Show Occluders");
            var nonOccluderToggle = new Toggle("Show non-Occluders");
            var occludeeToggle = new Toggle("Show Occludees");
            var nonOccludeeToggle = new Toggle("Show non-Occludees");
            rootVisualElement.Add(occluderToggle);
            rootVisualElement.Add(nonOccluderToggle);
            rootVisualElement.Add(occludeeToggle);
            rootVisualElement.Add(nonOccludeeToggle);

            var updateButton = new Button() { text = "Update Visibility" };
            updateButton.clicked += () =>
            {
                SceneVisibilityManager.instance.HideAll();
                void WalkGameObject(GameObject go)
                {
                    foreach (Transform transform in go.transform)
                        WalkGameObject(transform.gameObject);
                    var flags = GameObjectUtility.GetStaticEditorFlags(go);
                    if (
                        (((flags & StaticEditorFlags.OccluderStatic) != 0)
                            ? occluderToggle.value
                            : nonOccluderToggle.value
                        )
                        || (((flags & StaticEditorFlags.OccludeeStatic) != 0)
                            ? occludeeToggle.value
                            : nonOccludeeToggle.value
                        )
                    )
                    {
                        SceneVisibilityManager.instance.Show(go, false);
                    }
                }
                foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    WalkGameObject(go);
                }
            };
            rootVisualElement.Add(updateButton);
        }
    }
}
