using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// NOTE: this should probably be a menu item instead of a component, but for now this works

namespace JanSharp
{
    public class UIButtonColorChanger : MonoBehaviour { }

    #if UNITY_EDITOR
    [CustomEditor(typeof(UIButtonColorChanger))]
    public class UIButtonColorChangerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Update Colors", "Sets all colors on the button using the Normal Color of the button.")))
            {
                var target = (UIButtonColorChanger)this.target;
                Selectable selectable = target.GetComponent<Selectable>();
                var color = selectable.colors.normalColor;
                Undo.RecordObject(selectable, "Set UI Button colors");
                var colors = selectable.colors;
                colors.normalColor = color;
                colors.highlightedColor = color * new Color(0.95f, 0.95f, 0.95f);
                colors.pressedColor = color * new Color(0.75f, 0.75f, 0.75f);
                colors.selectedColor = color * new Color(0.95f, 0.95f, 0.95f);
                colors.disabledColor = color * new Color(0.75f, 0.75f, 0.75f, 0.5f);
                selectable.colors = colors;
            }
        }
    }
    #endif
}
