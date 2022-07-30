using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
#endif

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Chalkboard : UdonSharpBehaviour
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
        , IOnBuildCallback
    #endif
    {
        public Transform bottomLeft;
        public Transform topRight;
        public Material material;
        public Color boardColor;

        [HideInInspector] public int boardId;
        [HideInInspector] [SerializeField] private ChalkboardManager chalkboardManager;
        [HideInInspector] [System.NonSerialized] public Texture2D texture;
        private Color[] initialPixels;
        private bool fastUpdating;
        private bool slowUpdating;
        private ulong[] allActions;
        private int allActionsCount;
        private ulong currentActions;
        private int currentActionsIndex;
        private const int PointBitCount = 21;
        private const int AxisBitCount = 10;
        private const ulong PointHasPrev = 0x100000UL;
        private const ulong PointBits = 0x1fffffUL;
        private const ulong UnusedPoint = PointBits;
        private const int IntHasPrev = 0x100000;
        private const int IntPointBits = 0x1fffff;
        private const int IntUnusedAction = 0; // x + y
        private const int IntCaughtUpAction = 1; // x + y
        private const int IntSwitchToChalkY = 1; // just y
        private const int IntAxisBits = 0x3ff;
        private Chalk prevChalk;
        private int prevX;
        private int prevY;
        private bool waitingToStartSending;
        private bool sending;
        [UdonSynced]
        private ulong syncedData;
        private int currentSyncedIndex;
        private int actionsCountRequiredToSync;
        private bool catchingUp;
        private bool catchingUpWithTheQueue;
        private Chalk receivedChalk;
        private int receivedPrevX;
        private int receivedPrevY;
        private const float LateJoinerSyncDelay = 10f; // TODO: set this higher for the real world
        private ulong[] catchUpQueue;
        private int catchUpQueueCount;
        private int catchUpQueueIndex;

        private void Start()
        {
            allActions = new ulong[64];
            texture = (Texture2D)material.mainTexture;
            // ok so this seems to be using about 9MB for a 1024x512 texture
            // if there was no overhead that should be 2MB
            // note that this is also based on one single test in the editor with 3 boards in the scene
            // however considering this saves us having to loop through all pixels to reset the color
            // well I really can't say it is a good option, but it is an option that allows us to
            // reset the board to the initial state in 2ms instead of 1.5s which it is when looping
            // through all pixels to reset them takes (first a GetPixels call, then the loop, then SetPixels)
            initialPixels = texture.GetPixels();
        }

        // fast is kept completely separate from slow because when multiple people are drawing
        // we wouldn't want to inconsistently switch between slow and fast updates
        // instead it'll just update a bit quicker 4 times per frame while updating 15 times
        // per frame in general, so a total of ~19 updates. Still irregular but at least
        // it's updating quickly and the logic is simple

        public void UpdateTextureFast()
        {
            if (fastUpdating)
                return;
            fastUpdating = true;
            SendCustomEventDelayedSeconds(nameof(UpdateTextureFastDelayed), 1f / 15f);
        }

        public void UpdateTextureFastDelayed()
        {
            texture.Apply();
            fastUpdating = false;
        }

        public void UpdateTextureSlow()
        {
            if (slowUpdating)
                return;
            slowUpdating = true;
            SendCustomEventDelayedSeconds(nameof(UpdateTextureSlowDelayed), 1f / 4f);
        }

        public void UpdateTextureSlowDelayed()
        {
            texture.Apply();
            slowUpdating = false;
        }

        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        [InitializeOnLoad]
        public static class OnBuildRegister
        {
            static OnBuildRegister() => JanSharp.OnBuildUtil.RegisterType<Chalkboard>();
        }
        bool IOnBuildCallback.OnBuild()
        {
            chalkboardManager = GameObject.Find("/ChalkboardManager")?.GetUdonSharpComponent<ChalkboardManager>();
            boardId = chalkboardManager?.GetBoardId(this) ?? -1;
            if (chalkboardManager == null)
                Debug.LogError("Chalkboard requires a GameObject that must be at the root of the scene"
                        + " with the exact name 'ChalkboardManager' which has the 'ChalkboardManager' UdonBehaviour.",
                    UdonSharpEditorUtility.GetBackingUdonBehaviour(this));

            this.ApplyProxyModifications();
            // EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(this));
            return chalkboardManager != null;
        }
        #endif

        public void Clear()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ClearInternal));
        }

        public void ClearInternal()
        {
            texture.SetPixels(initialPixels);
            allActions = new ulong[64]; // to free up that memory
            allActionsCount = 0;
            currentActions = 0;
            currentActionsIndex = 0;
            prevChalk = null;
            prevX = 0;
            prevY = 0;
            waitingToStartSending = false;
            sending = false;
            catchingUp = false;
            catchingUpWithTheQueue = false;
            receivedChalk = null; // just to clear the reference, really. syncing doesn't happen anymore
            catchUpQueue = null;
            catchUpQueueCount = 0; // reset to stop CatchUpWithQueue if it's running
            catchUpQueueIndex = 0; // reset to stop CatchUpWithQueue if it's running
            UpdateTextureSlow();
        }



        public void DrawPoint(Chalk chalk, int x, int y)
        {
            UseChalk(chalk);
            AddAction(x | (y << AxisBitCount));
            if (!catchingUp && !catchingUpWithTheQueue)
                DrawPointInternal(chalk, x, y);
            prevX = x;
            prevY = y;
        }

        public void DrawLine(Chalk chalk, int fromX, int fromY, int toX, int toY)
        {
            UseChalk(chalk);
            if (fromX != prevX || fromY != prevY)
                AddAction(fromX | (fromY << AxisBitCount));
            AddAction(toX | (toY << AxisBitCount) | IntHasPrev);
            if (!catchingUp && !catchingUpWithTheQueue)
                DrawLineInternal(chalk, fromX, fromY, toX, toY);
            prevX = toX;
            prevY = toY;
        }

        private void UseChalk(Chalk chalk)
        {
            if (chalk == prevChalk)
                return;
            prevChalk = chalk;
            // y == 1 is an invalid point, so it means "switch to chalk [x]" instead
            AddAction(chalk.chalkId | (IntSwitchToChalkY << AxisBitCount));
            prevX = 0;
            prevY = 0;
        }

        private void AddAction(int action)
        {
            currentActions |= ((ulong)action) << (currentActionsIndex * PointBitCount);
            if (++currentActionsIndex == 3)
            {
                AddToAllActions(currentActions);
                currentActions = 0;
                currentActionsIndex = 0;
            }
        }

        private void AddToAllActions(ulong actions)
        {
            if (catchingUp || catchingUpWithTheQueue)
            {
                if (catchUpQueueCount == catchUpQueue.Length)
                {
                    var newCatchUpQueue = new ulong[catchUpQueueCount * 2];
                    catchUpQueue.CopyTo(newCatchUpQueue, 0);
                    catchUpQueue = newCatchUpQueue;
                }
                catchUpQueue[catchUpQueueCount++] = actions;
            }
            else
            {
                AddToAllActionsInternal(actions);
            }
        }

        private void AddToAllActionsInternal(ulong actions)
        {
            if (allActionsCount == allActions.Length)
            {
                var newAllActions = new ulong[allActionsCount * 2];
                allActions.CopyTo(newAllActions, 0);
                allActions = newAllActions;
            }
            allActions[allActionsCount++] = actions;
        }

        private void DrawPointInternal(Chalk chalk, int x, int y)
        {
            if (chalk.isSponge)
            {
                texture.SetPixels(x - chalk.halfSize, y - chalk.halfSize, chalk.size, chalk.size, chalk.colors);
            }
            else
            {
                int blX = x - chalk.halfSize;
                int blY = y - chalk.halfSize;
                int trX = x + chalk.halfSize;
                int trY = y + chalk.halfSize;
                chalk.colors[0] = texture.GetPixel(blX, blY);
                chalk.colors[4] = texture.GetPixel(trX, blY);
                chalk.colors[20] = texture.GetPixel(blX, trY);
                chalk.colors[24] = texture.GetPixel(trX, trY);
                texture.SetPixels(blX, blY, chalk.size, chalk.size, chalk.colors);
            }
        }

        private void DrawLineInternal(Chalk chalk, int fromX, int fromY, int toX, int toY)
        {
            Vector2 delta = new Vector2(toX - fromX, toY - fromY);
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) // horizontal
            {
                int stepX = System.Math.Sign(delta.x) * chalk.lineDrawingFrequency;
                float stepY = (delta.y / Mathf.Abs(delta.x)) * chalk.lineDrawingFrequency;
                float y = fromY;
                if (fromX < toX)
                    for (int x = fromX + stepX; x <= toX - 1; x += stepX)
                        DrawPointInternal(chalk, x, Mathf.RoundToInt(y += stepY));
                else
                    for (int x = fromX + stepX; x >= toX + 1; x += stepX)
                        DrawPointInternal(chalk, x, Mathf.RoundToInt(y += stepY));
            }
            else // vertical
            {
                int stepY = System.Math.Sign(delta.y) * chalk.lineDrawingFrequency;
                float stepX = (delta.x / Mathf.Abs(delta.y)) * chalk.lineDrawingFrequency;
                float x = fromX;
                if (fromY < toY)
                    for (int y = fromY + stepY; y <= toY - 1; y += stepY)
                        DrawPointInternal(chalk, Mathf.RoundToInt(x += stepX), y);
                else
                    for (int y = fromY + stepY; y >= toY + 1; y += stepY)
                        DrawPointInternal(chalk, Mathf.RoundToInt(x += stepX), y);
            }
            DrawPointInternal(chalk, toX, toY);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isMaster) // first player joining, we have to ignore it because they can't send to themselves so they would get stuck
                return;
            if (Networking.IsOwner(this.gameObject))
            {
                SendCustomEventDelayedSeconds(nameof(RequestSerializationDelayed), LateJoinerSyncDelay); // honestly... I'm annoyed
                sending = false;
                waitingToStartSending = true;
            }
            else if (player.isLocal) // technically this doesn't have to be an else if - I don't think - but it makes no sense to ever do both
            {
                catchingUp = true;
                catchUpQueue = new ulong[8];
            }
        }

        public override void OnPreSerialization()
        {
            if (!sending)
            {
                Debug.Log($"<dlt> ICU VRC trying to screw me, no I'm not syncing data right now.");
                syncedData = 0;
                return;
            }
            Debug.Log($"<dlt> sending {currentSyncedIndex + 1}/{actionsCountRequiredToSync + 1}");
            if (currentSyncedIndex >= actionsCountRequiredToSync)
            {
                syncedData = (ulong)IntCaughtUpAction;
                sending = false;
                return;
            }
            syncedData = allActions[currentSyncedIndex++];
            SendCustomEventDelayedFrames(nameof(RequestSerializationDelayed), 1);
        }

        public void RequestSerializationDelayed()
        {
            if (waitingToStartSending)
            {
                // it is at this point that the joined player can actually receive the packets we are sending
                // so initialize how far we need to sync here instead of in on player joined
                AddToAllActions(currentActions);
                currentActions = 0;
                currentActionsIndex = 0;
                actionsCountRequiredToSync = allActionsCount;
                currentSyncedIndex = 0;
                waitingToStartSending = false;
                sending = true;
            }
            RequestSerialization();
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            Debug.Log($"<dlt> on post: success: {result.success}, byteCount: {result.byteCount}");
        }

        public override void OnDeserialization()
        {
            if (!catchingUp)
            {
                // TODO: handle currentSyncedIndex
                return;
            }
            ProcessActions(syncedData);
        }

        private void ProcessActions(ulong actions)
        {
            bool doUpdateTexture = false;
            for (int i = 0; i < 3; i++)
            {
                int point = (int)((actions >> (i * PointBitCount)) & PointBits);
                if (point == IntUnusedAction)
                    break;
                if (point == IntCaughtUpAction)
                {
                    Debug.Log($"<dlt> we caught up with all actions that happened before we joined!");
                    catchingUp = false;
                    catchingUpWithTheQueue = true;
                    SendCustomEventDelayedFrames(nameof(CatchUpWithQueue), 1);
                    return;
                }
                doUpdateTexture = true;
                int x = point & IntAxisBits;
                int y = (point >> AxisBitCount) & IntAxisBits;
                if (y == IntSwitchToChalkY)
                {
                    Debug.Log($"<dlt> processing switch to chalk id: {x}");
                    receivedChalk = chalkboardManager.chalks[x];
                }
                else
                {
                    Debug.Log($"<dlt> processing point x: {x}, y: {y} hasPrev: {((point & IntHasPrev) != 0)}");
                    if (receivedChalk == null)
                        Debug.Log($"<dlt> processing point before receiving any switch to a chalk?!");
                    else if ((point & IntHasPrev) != 0)
                        DrawLineInternal(receivedChalk, receivedPrevX, receivedPrevY, x, y);
                    else
                        DrawPointInternal(receivedChalk, x, y);
                    receivedPrevX = x;
                    receivedPrevY = y;
                }
            }
            AddToAllActionsInternal(actions);
            if (doUpdateTexture)
                UpdateTextureSlow();
        }

        public void CatchUpWithQueue()
        {
            Debug.Log($"<dlt> CatchUpWithQueue {catchUpQueueIndex}/{catchUpQueueCount}");
            if (catchUpQueueIndex == catchUpQueueCount)
            {
                Debug.Log($"<dlt> we are fully caught up!");
                catchingUpWithTheQueue = false;
                catchUpQueue = null; // free that memory
                return;
            }
            ProcessActions(catchUpQueue[catchUpQueueIndex++]);
            SendCustomEventDelayedFrames(nameof(CatchUpWithQueue), 1);
        }
    }
}
