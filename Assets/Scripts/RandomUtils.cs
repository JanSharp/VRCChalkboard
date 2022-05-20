using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RandomUtils : MonoBehaviour
{
    public GameObject objectToSpawn;
    public string objectToSpawnName;
}

#if UNITY_EDITOR
[CustomEditor(typeof(RandomUtils))]
public class RandomUtilsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        if (GUILayout.Button(new GUIContent("Spawn 1000 objects")))
        {
            var target = (RandomUtils)base.target;
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 100; x++)
                {
                    var position = new Vector3(5f + y * 5f, 0.5f, x - 50f);
                    var rotation = target.objectToSpawn.transform.rotation;
                    GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(target.objectToSpawn, target.transform);
                    if (obj != null)
                        obj.transform.SetPositionAndRotation(position, rotation);
                    else
                        obj = Instantiate(target.objectToSpawn, position, rotation, target.transform);
                    obj.name = $"{(string.IsNullOrWhiteSpace(target.objectToSpawnName) ? target.objectToSpawn.name : target.objectToSpawnName)}_{x}_{y}";
                }
        }
    }
}
#endif
