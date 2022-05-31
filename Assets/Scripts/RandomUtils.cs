using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RandomUtils : MonoBehaviour
{
    public GameObject objectToSpawn;
    public string objectToSpawnName;

    [Header("Material Switcher")]
    [Tooltip("This will replace all materials on a mesh renderer where their shader name matches the given name.")]
    public GameObject[] mainParents;
    public string oldShaderName;
    public Material newMaterial;
}

#if UNITY_EDITOR
[CustomEditor(typeof(RandomUtils))]
public class RandomUtilsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var target = (RandomUtils)base.target;
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        if (GUILayout.Button(new GUIContent("Spawn 1000 objects")))
        {
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

        if (GUILayout.Button(new GUIContent("Switch Materials")))
        {
            if (target.mainParents == null
                || target.oldShaderName == null
                || target.newMaterial == null)
            {
                Debug.LogError("In order to change materials you must provide the 'Main Parent', "
                    + "the 'Old Shader Name' and the 'New Material' which should replace the old material. "
                    + "This will replace all materials on a mesh renderer where their shader name matches the given name."
                );
            }
            else
                foreach (GameObject obj in target.mainParents)
                    RecursivelyChangeMaterial(target, obj.transform);
        }
    }

    private void RecursivelyChangeMaterial(RandomUtils utils, Transform transformToWalk)
    {
        foreach (Transform child in transformToWalk)
            RecursivelyChangeMaterial(utils, child);

        MeshRenderer renderer = transformToWalk.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var shader = materials[i].shader;
                if (materials[i].shader.name == utils.oldShaderName)
                    materials[i] = utils.newMaterial;
                // FIXME: find a way to compare shared materials with a provided material
            }
            renderer.sharedMaterials = materials;
        }
    }
}
#endif
