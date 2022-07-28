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
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
        , IOnBuildCallback
    #endif
    {
        [HideInInspector] public Chalkboard[] chalkboards;

        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        private List<Chalkboard> allBoards;

        [InitializeOnLoad]
        public static class OnBuildRegister
        {
            static OnBuildRegister() => JanSharp.OnBuildUtil.RegisterType<ChalkboardManager>();
        }
        bool IOnBuildCallback.OnBuild()
        {
            allBoards = allBoards ?? new List<Chalkboard>();
            if (allBoards.Any(b => b == null))
            {
                allBoards.RemoveAll(b => b == null);
                for (int i = 0; i < allBoards.Count; i++)
                {
                    allBoards[i].boardId = i;
                    allBoards[i].ApplyProxyModifications();
                    // EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(allBoards[i]));
                }
            }
            chalkboards = allBoards.ToArray();
            this.ApplyProxyModifications();
            // EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(this));
            return true;
        }

        public int GetBoardId(Chalkboard board)
        {
            allBoards = allBoards ?? new List<Chalkboard>();
            int index = allBoards.FindIndex(b => b == board);
            if (index != -1)
                return index;
            allBoards.Add(board);
            chalkboards = allBoards.ToArray();
            this.ApplyProxyModifications();
            // EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(this));
            return allBoards.Count - 1;
        }
        #endif
    }
}
