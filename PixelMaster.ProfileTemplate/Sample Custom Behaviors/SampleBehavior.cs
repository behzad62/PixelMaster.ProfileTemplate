using FluentBehaviourTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using PixelMaster.Core.Wow.Objects;
using PixelMaster.Core.Managers;
using System.Threading;
using PixelMaster.Services.Input;
using PixelMaster.Services.Behaviors;
using PixelMaster.Core.Exceptions;
using PixelMaster.Core.Behaviors.Transport;

namespace PixelMaster.Core.Behaviors.QuestBehaviors
{
    /// <summary>
    /// This class is a sample custome behavior template that can be used to create custome behaviors.
    /// </summary>
    public class MoveToBehavior : BaseContext, ICustomBehaviorContext
    {
        public MoveToBehavior() : base()
        {

        }
        private string behaviorName;
        private bool avoidEnemies;
        private bool canFly;
        private bool ignoreCombat;
        private bool ignoreCombatIfMounted;
        private float maxDistanceToHotspot;
        private int mapId;
        private int questId;
        private string questName;
        public IReadOnlyList<Vector3> hotspots { get; }
        private IReadOnlyDictionary<string, string> parameters;
        private ILocalPlayer Me => ObjectManager.Instance.Player;
        private int lastVisitedHotspot = 0;
        private Vector3 NextHotspot = Vector3.Zero;
        private bool didOnStart = false;
        private Status mainStatus = Status.Invalid;
        /// <summary>
        /// This method will be called by the profile to pass parameters to this behavior.
        /// Behavior then can parse the passed parameters to retrive necessary data.
        /// </summary>
        /// <param name="parameters">Parameters passed by the profile</param>
        public void SetParameters(Dictionary<string, string> parameters)
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            this.parameters = parameters;
            ParseParameters();

        }
        /// <summary>
        /// This method cancles this behavior and usually is called after behavior is done executing.
        /// Any async task must be cancled before this method returns.
        /// There is no need to modify this method unless it is necessary.
        /// </summary>
        public override async Task CancelTask()
        {
            if (isCanceling || disposed)
                return;
            isCanceling = true;
            _cancellation.Cancel();
            await taskTracker.GetCombinedTask();
            IsCanceled = true;
            ConsoleDebug($"{behaviorName} is Canceled.");
        }
        /// <summary>
        /// This method is called by the parent behavior when ever necessary.
        /// i.e. when player engages in combat, the parent behavior pauses this behavior and laters
        /// after combat finished, this behavior will be resumed again.
        /// After this method is called the behavior must pause immediately and save it's state 
        /// if needed then return from the MainRoutine.
        /// There is no need to modify this method unless it is necessary.
        /// </summary>
        public override async Task PauseTask()
        {
            if (isPausing || isCanceling || disposed)
                return;
            isPausing = true;
            _cancellation.Cancel();
            await taskTracker.GetCombinedTask();
            IsPaused = true;
            ConsoleDebug($"{behaviorName} is Paused.");
        }
        /// <summary>
        /// This method is called by the parent behavior to resume this behavior.
        /// After called, MainRoutine will be called again and should be able to
        /// resume it's previous state before the pause. 
        /// There is no need to modify this method unless it is necessary.
        /// </summary>
        public override void ResumeTask()
        {
            if (!IsPaused || isCanceling || disposed)
                return;
            _cancellation.Dispose();
            _cancellation = new CancellationTokenSource();
            IsPaused = false;
            isPausing = false;
            mainStatus = Status.Invalid;
            ConsoleDebug($"{behaviorName} Resumed.");
        }
        /// <summary>
        /// Every behavior is responsible to reset it's state after this method is called.
        /// After reset, the behavior state should be like the first time it was started.
        /// Generally any class variable that is used to save the state of the behavior should reset it's value here.
        /// </summary>
        public override void ResetTask()
        {
            base.ResetTask();
            lastVisitedHotspot = 0;
            NextHotspot = Vector3.Zero;
            didOnStart = false;
            mainStatus = Status.Invalid;
            ParseParameters();
        }
        /// <summary>
        /// This method is called by the parent behavior at each iteration to advance the behavior logic.
        /// There is no need to modify this method unless it is necessary.
        /// </summary>
        /// <returns></returns>
        public override Status Tick()
        {
            if (mainStatus == Status.Invalid)
                Run();

            return mainStatus;
        }

        /// <summary>
        /// This method is designed to call the MainRoutine asynchronously and waits for it's result.
        /// Depending on the result of the MainRoutine this behavior status will be set.
        /// There is no need to modify this method unless it is necessary.
        /// </summary>
        private async void Run()
        {
            try
            {
                mainStatus = Status.Running;
                taskTracker.TrackTask();
                var sucess = await MainRoutine();
                mainStatus = sucess ? Status.Success : Status.Failure;
            }
            catch (OperationCanceledException)
            {
                if (!isPausing)
                    mainStatus = Status.Failure;
            }
            catch (Exception ex)
            {
                ConsoleDebug($"{behaviorName}: Exception {ex.Message}!");
                logService.LogError(ex);
                mainStatus = Status.Failure;
            }
            finally
            {
                taskTracker.UnTrackTask();
            }
        }
        /// <summary>
        /// This method can be used to do the initializing logic of the behavior
        /// Custme behaviors can modify this method to do the starting logic and initializing.
        /// </summary>
        void OnStart()
        {
            lastVisitedHotspot = 0;
            foreach (var s in from s in hotspots.Select((x, i) => new { Value = x, Index = i })
                              where Vector3.DistanceSquared(s.Value, Me.Position) < Vector3.DistanceSquared(hotspots[lastVisitedHotspot], Me.Position)
                              select s)
            {
                lastVisitedHotspot = s.Index;
            }
            ConsoleLog($"MoveTo: Started for QuestId {questId} {questName}!");
            didOnStart = true;
        }
        /// <summary>
        /// This is the main routing of the behavior
        /// Custom behaviors should put their main logic here to run.
        /// The following code is provided as a sample only.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> MainRoutine()
        {
            //Useful APIs for the custom behaviors are as follows:

            //includes various game objects and methods to retrive objects
            var objectManager = ObjectManager.Instance;
            //usually player static data
            var player = ObjectManager.Instance.Player;
            //current map info
            var map = ObjectManager.Instance.CurrentMap;
            //player dynamic actions APIs
            var playerActionsAPI = ActionManager.Instance.PlayerActions;
            //player movement APIs
            var playerMovementAPIs = ActionManager.Instance.PlayerMovement;
            //APIs to gain mouse access
            var mouseAccessAPI = ActionManager.Instance.MouseAccess;
            //Dynamic settings to set. i.e. disable and enable combat, looting and more
            var dynamicSettings = BottingSessionManager.Instance.DynamicSettings;
            //NavigationHelpers static class;

            //Although mouse access is not used in this sample, some APIs require mouse access token.
            //How to gain mouse access token is shown here as an example.
            //IMouseAccessToken mouseAccess = null;
            try
            {
                if ((await isQuestCompleted().ConfigureAwait(false) || await haveQuest().ConfigureAwait(false) == false) && questId > 0)
                {
                    ConsoleLog($"{behaviorName} for QuestId {questId}: is Done!");
                    return true;
                }
                if (!didOnStart)
                    OnStart();
                //Example of how to reserve mouse access token
                //mouseAccess = ActionManager.Instance.MouseAccess.ReserveExclusiveMouseAccess(behaviorName);
                //Waiting for mouse access
                //await mouseAccess.WaitForAccess(_cancellation.Token);
                //Behavior logic loop.
                while (true)
                {
                    if (NextHotspot == Vector3.Zero)
                    {
                        ConsoleDebug($"{behaviorName} no destiny is loaded, Lets pick the next destiny.");
                        GetToNextSpot();
                    }
                    if (NextHotspot != Vector3.Zero && Vector3.DistanceSquared(NextHotspot, Me.Position) > maxDistanceToHotspot * maxDistanceToHotspot)
                    {
                        ConsoleDebug($"MoveTo: im far away from destiny lets move.");
                        await MoveToNextSpot();
                    }
                    if (NextHotspot != Vector3.Zero && Vector3.DistanceSquared(NextHotspot, Me.Position) <= maxDistanceToHotspot * maxDistanceToHotspot)
                    {
                        if (NextHotspot == hotspots[hotspots.Count - 1])
                        {
                            ConsoleDebug($"MoveTo: Im at destiny.");
                            return true;
                        }
                        else
                        {
                            ConsoleDebug($"MoveTo: reached {NextHotspot}, loading next hotspot.");
                            NextHotspot = Vector3.Zero;
                            GetToNextSpot();
                        }
                    }
                    //It is highly recommended to wait for the updated data at the end of the while loop
                    //Note that for any async method we use _cancellation.Token
                    //This is necessary in order to cancel the async method as soon as this behavior is paused or cancled.
                    await ObjectManager.Instance.WaitForUpdatedData(_cancellation.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                //At the end it is necessary to release the mouse access.
                //if (mouseAccess != null)
                //    ActionManager.Instance.MouseAccess.ReleaseAccess(mouseAccess);
            }
        }
        async Task<bool> haveQuest()
        {
            return await Me.QuestLog.HasQuestAsync(questId, 1500, _cancellation.Token).ConfigureAwait(false);
        }
        async Task<bool> isQuestCompleted()
        {
            return await Me.QuestLog.IsCompletedAsync(questId, 1500, _cancellation.Token).ConfigureAwait(false);
        }
        async Task MoveToNextSpot()
        {
            await ActionManager.Instance.PlayerMovement.NavigateToTarget(NextHotspot, mapId, maxDistanceToHotspot, _cancellation.Token, avoidEnemies, canFly, ignoreCombat, ignoreCombatIfMounted).ConfigureAwait(false);
        }
        Vector3 GetToNextSpot()
        {
            if (NextHotspot == Vector3.Zero)
            {
                NextHotspot = hotspots[lastVisitedHotspot++];
                lastVisitedHotspot %= hotspots.Count;
            }
            logService.ConsoleDebug($"MoveTo: next hotspot {NextHotspot}.");
            return NextHotspot;
        }
        void ParseParameters()
        {
            //we can parse all necessary data passed from the profile here
            //i.e. mapId and hotspots.
            this.mapId = int.Parse(parameters["MapId"]);
        }
        void ConsoleLog(string txt)
        {
            logService.ConsoleInfo(txt);
        }
        void ConsoleDebug(string txt)
        {
            logService.ConsoleDebug(txt);
        }
        void ConsoleError(string txt)
        {
            logService.ConsoleError(txt);
        }
        void ConsoleErrorAndThrow(string txt)
        {
            logService.ConsoleError(txt);
            throw new InvalidOperationException(txt);
        }
    }
}

