using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class RandomUtils : MonoBehaviour
{
    [Header("Spawn 1000 Objects")]
    public GameObject objectToSpawn;
    public string objectToSpawnName;

    [Header("Switch Materials")]
    [Tooltip("This will replace all references to the Old Material on the provided GameObjects and all nested children on a mesh with the New Material.")]
    public GameObject[] mainParents;
    public Material oldMaterial;
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

        if (GUILayout.Button(new GUIContent("Spawn 1000 Objects")))
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
                || target.mainParents.Length == 0
                || target.mainParents.Any(o => o == null)
                || target.oldMaterial == null
                || target.newMaterial == null)
            {
                Debug.LogError("In order to change materials you must provide 'Main Parents' (at least 1 and no nulls), "
                    + "the 'Old Material' and the 'New Material' which should replace said old material."
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
                if (materials[i] == utils.oldMaterial)
                    materials[i] = utils.newMaterial;
            }
            renderer.sharedMaterials = materials;
        }
    }
}
#endif
