using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Generic;
using System.Linq;
#endif

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ChalkboardManager : UdonSharpBehaviour
    {
        [HideInInspector] public Chalkboard[] chalkboards;
        [HideInInspector] public Chalk[] chalks;
    }

    // TODO: make sure this is unique to each scene
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    internal static class ChalkboardManagerOnBuild
    {
        public static List<Chalkboard> allBoards;
        public static List<Chalk> allChalks;

        static ChalkboardManagerOnBuild() => JanSharp.OnBuildUtil.RegisterType<ChalkboardManager>(OnBuild);

        private static bool OnBuild(ChalkboardManager manager)
        {
            SerializedObject managerProxy = new SerializedObject(manager);
            Cleanup(ref allBoards, managerProxy.FindProperty(nameof(ChalkboardManager.chalkboards)), (board, id) => {
                SerializedObject boardProxy = new SerializedObject(board);
                boardProxy.FindProperty(nameof(Chalkboard.boardId)).intValue = id;
                boardProxy.ApplyModifiedProperties();
            });
            Cleanup(ref allChalks, managerProxy.FindProperty(nameof(ChalkboardManager.chalks)), (chalk, id) => {
                SerializedObject chalkProxy = new SerializedObject(chalk);
                chalkProxy.FindProperty(nameof(Chalk.chalkId)).intValue = id;
                chalkProxy.ApplyModifiedProperties();
            });
            return true;
        }

        private static void Cleanup<T>(ref List<T> allValues, SerializedProperty allValuesProperty, System.Action<T, int> setId)
            where T : UdonSharpBehaviour
        {
            allValues = allValues ?? new List<T>();
            if (allValues.Any(b => b == null))
            {
                allValues.RemoveAll(b => b == null);
                for (int i = 0; i < allValues.Count; i++)
                    setId(allValues[i], i);
            }
            EditorUtil.SetArrayProperty(allValuesProperty, allValues, (p, v) => p.objectReferenceValue = v);
        }

        public static int GetBoardId(ChalkboardManager manager, Chalkboard board)
        {
            return GetId(ref allBoards, manager, nameof(ChalkboardManager.chalkboards), board);
        }

        public static int GetChalkId(ChalkboardManager manager, Chalk chalk)
        {
            return GetId(ref allChalks, manager, nameof(ChalkboardManager.chalks), chalk);
        }

        private static int GetId<T>(ref List<T> allValues, ChalkboardManager manager, string propertyName, T value) where T : Object
        {
            allValues = allValues ?? new List<T>();
            int index = allValues.FindIndex(b => b.Equals(value));
            if (index != -1)
                return index;
            allValues.Add(value);
            SerializedObject managerProxy = new SerializedObject(manager);
            EditorUtil.AppendProperty(managerProxy.FindProperty(propertyName), p => p.objectReferenceValue = value);
            managerProxy.ApplyModifiedProperties();
            return allValues.Count - 1;
        }
    }
    #endif
}
