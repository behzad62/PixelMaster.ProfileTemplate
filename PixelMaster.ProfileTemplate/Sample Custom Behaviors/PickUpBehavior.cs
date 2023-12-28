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
using PixelMaster.Services.Settings;
using PixelMaster.Core.Wow;
using PixelMaster.Core.Interfaces;
using PixelMaster.Core.Behaviors.Transport;

namespace PixelMaster.Core.Behaviors.QuestBehaviors
{
    public class PickUpBehavior : BaseContext
    {
        public PickUpBehavior(
           int MapId,
           int QuestId,
           int EntryId,
           string QuestName,
           bool IgnoreCheck,
           float MinDistance,
           float MaxDistance,
           int WaitTime,
           List<Vector3> Hotspots)
        {
            this.mapId = MapId;
            this.questId = QuestId;
            this.entryId = EntryId;
            this.questName = QuestName;
            this.ignoreCheck = IgnoreCheck;
            this.minDistance = MinDistance;
            this.maxDistance = MaxDistance;
            this.waitTime = WaitTime;
            this.hotspots = Hotspots;
        }

        public int mapId { get; }
        public int questId { get; }
        public int entryId { get; }
        public string questName { get; }
        public float minDistance { get; }
        public float maxDistance { get; }
        public int waitTime { get; }
        public bool ignoreCheck { get; }
        public IReadOnlyList<Vector3> hotspots { get; }

        private readonly PlayerUnit Me = ObjectManager.Instance.Player;
        //private readonly IDataService dataService = CoreModule.DataService;
        private bool closeNextFrame = true;
        private WoWObjectWithPosition ObjectTarget = null;
        private int lastVisitedHotspot = 0;
        private Vector3 NextHotspot = Vector3.Zero;
        private bool didOnStart = false;
        int counter = 0;
        private Status mainStatus = Status.Invalid;
        private BlackLister<WowGUID> blacklists = new BlackLister<WowGUID>();
        void OnStart()
        {
            foreach (var s in from s in hotspots.Select((x, i) => new { Value = x, Index = i })
                              where Vector3.DistanceSquared(s.Value, Me.Position) < Vector3.DistanceSquared(hotspots[lastVisitedHotspot], Me.Position)
                              select s)
            {
                lastVisitedHotspot = s.Index;
            }
            ConsoleLog($"PickUp: Started for QuestId {questId} {questName}!");
            didOnStart = true;
        }
        public override async Task CancelTask()
        {
            if (isCanceling || disposed)
                return;
            isCanceling = true;
            _cancellation.Cancel();
            await taskTracker.GetCombinedTask();
            IsCanceled = true;
            ConsoleDebug($"PickUp is Canceled.");
        }

        public override async Task PauseTask()
        {
            if (isPausing || isCanceling || disposed)
                return;
            isPausing = true;
            _cancellation.Cancel();
            await taskTracker.GetCombinedTask();
            IsPaused = true;
            ConsoleDebug($"PickUp is Paused.");
        }
        public override void ResumeTask()
        {
            if (!IsPaused || isCanceling || disposed)
                return;
            _cancellation.Dispose();
            _cancellation = new CancellationTokenSource();
            IsPaused = false;
            isPausing = false;
            mainStatus = Status.Invalid;
            ConsoleDebug($"PickUp Resumed.");
        }
        public override void ResetTask()
        {
            base.ResetTask();
            closeNextFrame = true;
            ObjectTarget = null;
            lastVisitedHotspot = 0;
            NextHotspot = Vector3.Zero;
            didOnStart = false;
            counter = 0;
            mainStatus = Status.Invalid;
            blacklists = new BlackLister<WowGUID>();
        }
        public override Status Tick()
        {
            if (mainStatus == Status.Invalid)
                Run();

            return mainStatus;
        }
        private async void Run()
        {
            try
            {
                mainStatus = Status.Running;
                taskTracker.TrackTask();

                //if (!didOnStart)
                //  OnStart();

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
                ConsoleDebug($"PickUp: Exception {ex.Message}!");
                logService.LogError(ex);
                mainStatus = Status.Failure;
            }
            finally
            {
                taskTracker.UnTrackTask();
            }
        }
        private async Task<bool> MainRoutine()
        {
            IMouseAccessToken mouseAccess = null;
            var player = ObjectManager.Instance.Player;
            try
            {
                if ((!ignoreCheck && await isQuestCompleted().ConfigureAwait(false)) || await haveQuest().ConfigureAwait(false))
                {
                    ConsoleLog($"Pick QuestId {questId}: is Done!");
                    return true;
                }
                if (!didOnStart)
                    OnStart();

                while (true)
                {
                    if (closeNextFrame)
                    {
                        await CloseFramesAsync();
                        closeNextFrame = false;
                    }
                    ObjectTarget = FindNextTargets().FirstOrDefault();
                    if (ObjectTarget == null && NextHotspot == Vector3.Zero)
                    {
                        ConsoleDebug($"PickUp no Entry in memory no destiny in memory Lets pick next destiny.");
                        GetToNextSpot();
                    }
                    /*if (ObjectTarget == null && NextHotspot != Vector3.Zero && Vector3.DistanceSquared(NextHotspot, Me.Position) > 3.5f * 3.5f)
                    {
                        ConsoleDebug($"PickUp no Entry in memory and far away from destiny lets move.");
                        await MoveToNextSpot(mouseAccess);
                    }*/
                    if (ObjectTarget == null && NextHotspot != Vector3.Zero && !player.IsOnTaxi && Vector3.DistanceSquared(NextHotspot, Me.Position) > maxDistance * maxDistance)
                    {
                        ConsoleDebug($"PickUp no NPC near and far away from destiny lets move.");
                        var stopOnFoundObject = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);
                        try
                        {
                            if (mouseAccess != null)
                            {
                                ActionManager.Instance.MouseAccess.ReleaseAccess(mouseAccess);
                                mouseAccess = null;
                            }
                            var findObjectTask = FindNPC(stopOnFoundObject.Token);
                            var movementTask = ActionManager.Instance.PlayerMovement.NavigateToTarget(NextHotspot, mapId, maxDistance, stopOnFoundObject.Token, true, true);
                            await Task.WhenAny(findObjectTask, movementTask).ConfigureAwait(false);
                            stopOnFoundObject.Cancel();
                            await Task.WhenAll(findObjectTask, movementTask).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            if (_cancellation.IsCancellationRequested)
                                throw;
                        }
                        finally
                        {
                            stopOnFoundObject.Dispose();
                        }
                    }


                    if (ObjectTarget == null && NextHotspot != Vector3.Zero && Vector3.DistanceSquared(NextHotspot, Me.Position) <= maxDistance * maxDistance)
                    {
                        ConsoleDebug($"PickUp Im at destiny but no Entry here, removing destiny from memory.");
                        NextHotspot = Vector3.Zero;
                    }
                    if (ObjectTarget != null && !CanInteract(ObjectTarget))
                    {
                        ConsoleDebug($"PickUp Entry in memory and cant interact with it lets remove it.");
                        ObjectTarget = FindNextTargets().FirstOrDefault();
                    }
                    if (ObjectTarget != null && !player.IsOnTaxi && ObjectTarget.DistanceSquaredToPlayer > maxDistance * maxDistance)
                    {
                        ConsoleDebug($"PickUp Entry in memory and far away from it lets move to it.");
                        try
                        {
                            if (mouseAccess != null)
                            {
                                ActionManager.Instance.MouseAccess.ReleaseAccess(mouseAccess);
                                mouseAccess = null;
                            }
                            await KillNearbyEnemies(ObjectTarget).ConfigureAwait(false);
                            if (mouseAccess is null || mouseAccess.AccessStatus != MouseAccessStatus.Granted)
                            {
                                mouseAccess = ActionManager.Instance.MouseAccess.ReserveExclusiveMouseAccess("InteractWithObject");
                                await mouseAccess.WaitForAccess(_cancellation.Token);
                            }
                            if (ObjectTarget is WowUnit)
                                await ActionManager.Instance.PlayerMovement.GetCloseToUnit(mouseAccess, ObjectTarget as WowUnit, maxDistance, _cancellation.Token).ConfigureAwait(false);
                            else
                                await ActionManager.Instance.PlayerMovement.NavigateToTarget(mouseAccess, ObjectTarget.Position, ObjectManager.Instance.CurrentMap.MapID, maxDistance, _cancellation.Token, true).ConfigureAwait(false);
                            await ActionManager.Instance.PlayerMovement.Land(true, _cancellation.Token).ConfigureAwait(false);
                            await ActionManager.Instance.PlayerActions.Dismount(_cancellation.Token).ConfigureAwait(false);
                            ObjectTarget = FindNextTargets().FirstOrDefault();//to refresh position
                        }
                        catch (Exception ex)
                        {
                            ObjectTarget = null;
                            if (ex is OperationCanceledException)
                            {
                                if (_cancellation.IsCancellationRequested)
                                    throw;
                            }
                        }
                    }
                    if (ObjectTarget != null && !player.IsOnTaxi && ObjectTarget.DistanceSquaredToPlayer <= maxDistance * maxDistance)
                    {
                        ConsoleDebug($"PickUp interacting with Entry for QuestId {questId} {questName}.");
                        //ToDo disable all flags in flags list
                        try
                        {
                            if (mouseAccess is null || mouseAccess.AccessStatus != MouseAccessStatus.Granted)
                            {
                                mouseAccess = ActionManager.Instance.MouseAccess.ReserveExclusiveMouseAccess("InteractWithObject");
                                await mouseAccess.WaitForAccess(_cancellation.Token);
                            }
                            if (counter >= 6)
                            {
                                ConsoleWarning($"PickUp: failed to accept quest after 6 tries.");
                                return false;
                            }
                            if (counter >= 3)//help to accept quest
                            {
                                ConsoleLog($"PickUp: failed to accept quest. It could happen due to lag. Repositioning the player.");
                                await ActionManager.Instance.PlayerActions.ClearTarget(_cancellation.Token).ConfigureAwait(false);
                                await RepositionPlayer(mouseAccess, _cancellation.Token).ConfigureAwait(false);
                            }
                            ConsoleLog($"PickUp: Interacting with {ObjectTarget.Name} for  QuestId {questId} {questName}!");
                            await AcceptQuest(mouseAccess, ObjectTarget, questId, 1500, _cancellation.Token).ConfigureAwait(false);
                            if (waitTime > 0)
                                await Task.Delay(waitTime, _cancellation.Token).ConfigureAwait(false);

                            ObjectTarget = null;
                        }
                        catch (WowActionFailedException e)
                        {
                            ConsoleDebug($"PickUp Got an exception while interacting to npc. Exception {e}");
                            ObjectTarget = null;
                        }
                        catch (WowTaskFailedException e)
                        {
                            ConsoleDebug($"PickUp: error happened while accepting quest. Exception {e.Message}");
                            ObjectTarget = null;
                        }
                        catch (PathNotFoundException e)
                        {
                            ConsoleDebug($"PickUp Got an exception while pathing to npc. Exception {e}");
                            ObjectTarget = null;
                            if (hotspots.Count > 1)
                                GetToNextSpot();
                        }
                        catch (Exception ex)
                        {
                            ObjectTarget = null;
                            if (ex is OperationCanceledException)
                            {
                                if (_cancellation.IsCancellationRequested)
                                    throw;
                            }
                            ConsoleDebug($"PickUp Got an exception. Exception {ex}");
                        }
                        finally
                        {
                            counter++;
                        }
                        await Task.Delay(500, _cancellation.Token).ConfigureAwait(false);
                        if (await haveQuest().ConfigureAwait(false) || await isQuestCompleted().ConfigureAwait(false))
                        {
                            await CloseFramesAsync().ConfigureAwait(false);
                            return true;
                        }
                    }
                    //better to slow things abit
                    await ObjectManager.Instance.WaitForUpdatedData(_cancellation.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                if (mouseAccess != null)
                    ActionManager.Instance.MouseAccess.ReleaseAccess(mouseAccess);

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
        async Task<bool> IsTaskCompleted()
        {
            return await Me.QuestLog.HasQuestAsync(questId, 1500, _cancellation.Token).ConfigureAwait(false) || await Me.QuestLog.IsCompletedAsync(questId, 1500, _cancellation.Token).ConfigureAwait(false);
        }
        IEnumerable<WoWObjectWithPosition> FindNextTargets()
        {
            var mobs = ObjectManager.Instance.GetVisibleUnits().Where(obj => obj.Id == this.entryId).ToList();
            var objects = ObjectManager.Instance.GetVisibleObjects().Where(obj => obj.Id == this.entryId).ToList();//GetVisibleGameObjects()

            //IEnumerable<WoWObject> combined = mobs.Union(objects);
            objects.Sort((o1, o2) => o1.DistanceSquaredToPlayer.CompareTo(o2.DistanceSquaredToPlayer));
            return objects;
        }
        async Task CloseFramesAsync()
        {
            //ToDo Close Any Opened Frames
            if (ObjectManager.Instance.IsGossipOpen)
            {
                await ActionManager.Instance.AddonInterface.CloseGameFrames(_cancellation.Token).ConfigureAwait(false);
                await Task.Delay(200, _cancellation.Token).ConfigureAwait(false);
            }
        }
        bool CanInteract(WoWObject obj)
        {
            //ToDo if is interactable obj return true
            return true;
        }
        /*async Task MoveToNextSpot(IMouseAccessToken mouseAccess)
        {
            await ActionManager.Instance.PlayerMovement.NavigateToTarget(mouseAccess, NextHotspot, ObjectManager.Instance.CurrentMap.MapID, 3.5f, _cancellation.Token).ConfigureAwait(false);
        }*/
        private async Task FindNPC(CancellationToken cancellation)
        {
            while (true)
            {
                var mobs = ObjectManager.Instance.GetVisibleUnits().Where(npc => npc.Id == this.entryId).ToList();
                if (mobs.Any())
                {
                    mobs.Sort((o1, o2) => o1.DistanceSquaredToPlayer.CompareTo(o2.DistanceSquaredToPlayer));
                    ObjectTarget = mobs.First();
                    return;
                }
                await Task.Delay(1000, cancellation).ConfigureAwait(false);

            }
        }

        Vector3 GetToNextSpot()
        {
            if (NextHotspot == Vector3.Zero)
            {
                NextHotspot = hotspots[lastVisitedHotspot++];
                lastVisitedHotspot %= hotspots.Count;
            }
            logService.ConsoleDebug($"PickUp: next hotspot {NextHotspot}.");
            return NextHotspot;
        }
        private async Task RepositionPlayer(IMouseAccessToken accessToken, CancellationToken cancellationToken)
        {
            var player = ObjectManager.Instance.Player;
            int distance = 10;
            var positionBehind = NavigationHelpers.GetTargetPoint(player.Position, distance, player.Facing + Math.PI, true);
            if (NavigationHelpers.IsPathWalkable(positionBehind) && NavigationHelpers.IsSafeLocation(positionBehind, 0, out int enemiesCount2))
            {
                try
                {
                    await ActionManager.Instance.PlayerMovement.RunBackward(distance - 2, cancellationToken);
                    logService.ConsoleDebug("RunBackward finished.");
                    return;
                }
                catch (PlayerStuckException) { }
            }
            logService.ConsoleDebug("Position behind is not walkable.");
            var safePosition = GetPlaceAroundPlayer(player.Position, 45);
            if (safePosition.HasValue)
            {
                await ActionManager.Instance.PlayerMovement.NavigateToTarget(accessToken, safePosition.Value, ObjectManager.Instance.CurrentMap.MapID, 1.5f, cancellationToken).ConfigureAwait(false);
            }
        }
        private Vector3? GetPlaceAroundPlayer(in Vector3 startPosition, double maxSafePlaceDistance)
        {
            double targetVector = ObjectManager.Instance.Player.Facing + Math.PI;
            double currentDistance = 8;
            while (currentDistance <= maxSafePlaceDistance)
            {
                for (int i = 0; i < 12; i++)
                {
                    var targetPoint = NavigationHelpers.GetTargetPoint(startPosition, currentDistance, targetVector, true);
                    if (Vector3.DistanceSquared(targetPoint, startPosition) <= maxSafePlaceDistance * maxSafePlaceDistance
                        && NavigationHelpers.IsPathWalkable(targetPoint))
                    {
                        return targetPoint;
                    }
                    targetVector += Math.PI / 6;
                }
                currentDistance += 5;
            }
            return null;
        }
        private async Task AcceptQuest(IMouseAccessToken mouseAccess, WoWObjectWithPosition questGiver, int questID, int timeout, CancellationToken cancellationToken)
        {
            try
            {
                bool questAccepted = await OpenQuestWindow(mouseAccess, questGiver, timeout, cancellationToken).ConfigureAwait(false);
                if (questAccepted)
                    return;
            }
            catch (TimeoutException ex)
            {
                throw new WowTaskFailedException(WowTaskType.AcceptQuest, ex.Message);
            }
            string cmd = $"/run {CoreModule.Settings.AddonName}.quests:Accept({questID})";
            await ActionManager.Instance.AddonInterface.ResetLastTaskStatus(cancellationToken).ConfigureAwait(false);
            timeout = await SendCommandToAddon(cmd, timeout, WowTaskType.AcceptQuest, cancellationToken).ConfigureAwait(false);
            await WaitForResult(timeout, WowTaskType.AcceptQuest, cancellationToken).ConfigureAwait(false);
            if (ActionManager.Instance.AddonInterface.LastAddonTaskStatus == LastTaskStatus.Failed)
                throw new WowTaskFailedException(WowTaskType.AcceptQuest, $"Accepting quest '{questID}' failed.");
        }
        private async Task<bool> OpenQuestWindow(IMouseAccessToken mouseAccess, WoWObjectWithPosition questGiver, int timeout, CancellationToken cancellationToken)
        {
            int timePassed = 0;
            if (questGiver.Type == WoWObjectType.GameObject)
            {
                await ActionManager.Instance.PlayerActions.InteractWithObject(mouseAccess, questGiver, 10000, 0, true, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await ActionManager.Instance.PlayerActions.InteractWithUnit(mouseAccess, questGiver as WowUnit, cancellationToken).ConfigureAwait(false);
            }
            await ActionManager.Instance.AddonInterface.SetEncodingModeDefault(cancellationToken).ConfigureAwait(false);
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            if (await IsTaskCompleted().ConfigureAwait(false))
                return true;
            timePassed += 100;
            while (!ObjectManager.Instance.IsGossipOpen)
            {
                if (ObjectManager.Instance.Player.IsInShapeShiftForm)
                    await ActionManager.Instance.PlayerActions.CancelForm(cancellationToken).ConfigureAwait(false);
                if (timePassed % 1000 == 0)
                {
                    if (questGiver.Type == WoWObjectType.GameObject)
                    {
                        await ActionManager.Instance.PlayerActions.InteractWithObject(mouseAccess, questGiver, 10000, 0, true, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await ActionManager.Instance.PlayerActions.InteractWithUnit(mouseAccess, questGiver as WowUnit, cancellationToken).ConfigureAwait(false);
                    }
                }
                await ActionManager.Instance.AddonInterface.SetEncodingModeDefault(cancellationToken).ConfigureAwait(false);
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                if (await IsTaskCompleted().ConfigureAwait(false))
                    return true;
                timePassed += 100;
                if (timeout > 0 && timePassed > timeout)
                {
                    throw new TimeoutException($"Opening quest window for NPC '{questGiver.Name}' timed out");
                }
            }
            return false;
        }
        private static async Task<int> SendCommandToAddon(string cmd, int timeout, WowTaskType taskType, CancellationToken cancellationToken)
        {
            int timePassed = 0;
            while (ActionManager.Instance.AddonInterface.LastAddonTaskStatus == LastTaskStatus.NotStarted)
            {
                if (timeout > 0 && timePassed > timeout)
                    throw new WowTaskFailedException(taskType, "Task timed out.");
                if (!ObjectManager.Instance.IsGossipOpen)
                    throw new WowTaskFailedException(taskType, "Task failed, gossip is not open!");
                await ActionManager.Instance.RunCommand(cmd.ToString(), cancellationToken).ConfigureAwait(false);
                await Task.Delay(100, cancellationToken).ConfigureAwait(false); //wait for cmd to be recognized by the wow client
                timePassed += 100;
            }
            if (timeout > 0)
                timeout -= timePassed;
            return timeout;
        }
        private static async Task WaitForResult(int timeout, WowTaskType taskType, CancellationToken cancellationToken)
        {
            if (timeout < 0)
                throw new WowTaskFailedException(taskType, "Task timed out.");
            int timePassed = 0;
            while (ActionManager.Instance.AddonInterface.LastAddonTaskStatus == LastTaskStatus.Started)
            {
                if (timeout > 0 && timePassed > timeout)
                {
                    //if we are not getting result from the addon then something might went wrong
                    await ActionManager.Instance.ReloadUI(cancellationToken).ConfigureAwait(false);
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    while (!ObjectManager.Instance.IsDataAvailable)
                        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                    if (ObjectManager.Instance.EncodingMode == WowDataEncodingMode.Clean)
                        await ActionManager.Instance.AddonInterface.SetEncodingModeDefault(cancellationToken).ConfigureAwait(false);
                    throw new WowTaskFailedException(taskType, "Task timed out.");
                }
                if (!ObjectManager.Instance.IsTrainerOpen)
                    throw new WowTaskFailedException(taskType, "Task failed, gossip is not open!");
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                timePassed += 50;
            }
        }
        async Task KillNearbyEnemies(WoWObjectWithPosition objectTarget)
        {
            if (BottingSessionManager.Instance.DynamicSettings.CombatDisabled)
                return;
            bool waitReported = false;
            var player = ObjectManager.Instance.Player;
            while (true)
            {
                if (objectTarget.DistanceSquaredToPlayer < 25)
                    break;
                var nearbyEnemies = objectTarget.GetNearbyEnemies().Where(enemy => !blacklists.IsBlackListed(enemy.WowGuid) && !enemy.ShouldAvoidMob()).ToList();
                if (nearbyEnemies.Any())
                {
                    int minHP = 50;
                    int minMana = 50;
                    if (nearbyEnemies.Count > 2)
                    {
                        minHP = 90;
                        minMana = 90;
                    }
                    else if (nearbyEnemies.Count > 1)
                    {
                        minHP = 70;
                        minMana = 70;
                    }
                    bool needsMana = player.NeedsMana(minMana);
                    bool needsFood = player.NeedsFood(minHP);
                    if (needsMana || needsFood)
                    {
                        if (!waitReported)
                        {
                            waitReported = true;
                            logService.ConsoleInfo($"There are enemies nearby loot, Waiting to recover HP/Mana before trying to interact.");
                        }
                        if (needsMana && !player.IsDrinking && !player.IsSwimming)
                        {
                            await ActionManager.Instance.PerformAction(Wow.WowAction.Drink, _cancellation.Token).ConfigureAwait(false);
                            await Task.Delay(100, _cancellation.Token).ConfigureAwait(false);
                        }
                        if (needsFood && !player.IsEating && !player.IsSwimming)
                        {
                            await ActionManager.Instance.PerformAction(Wow.WowAction.Eat, _cancellation.Token).ConfigureAwait(false);
                            await Task.Delay(100, _cancellation.Token).ConfigureAwait(false);
                        }
                        if (player.IsUnderWater)
                        {
                            await ActionManager.Instance.PlayerMovement.Ascend(1000, _cancellation.Token).ConfigureAwait(false);
                        }
                        await Task.Delay(500, _cancellation.Token);
                        continue;
                    }
                    nearbyEnemies.Sort((e1, e2) => e1.DistanceSquaredToPlayer.CompareTo(e2.DistanceSquaredToPlayer));
                    var enemy = nearbyEnemies.First();
                    try
                    {
                        await ActionManager.Instance.PlayerActions.FightUnit(enemy, _cancellation.Token).ConfigureAwait(false);
                    }
                    catch (TargetingFailedException ex)
                    {
                        logService.ConsoleDebug($"Pickup.{nameof(KillNearbyEnemies)}- Failed to target enemy. Error: {ex.Message}");
                        blacklists.BlackList(enemy.WowGuid, TimeSpan.FromSeconds(15));
                    }
                    catch (PullingTargetFailedException ex)
                    {
                        logService.ConsoleDebug($"Pickup.{nameof(KillNearbyEnemies)}- Failed to pull enemy. Error: {ex.Message}");
                        blacklists.BlackList(enemy.WowGuid, TimeSpan.FromSeconds(15));
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException)
                        {
                            if (_cancellation.IsCancellationRequested)
                                throw;
                        }
                        logService.ConsoleDebug($"Pickup.{nameof(KillNearbyEnemies)}- Failed to fight enemy. Error: {ex.Message}");
                        blacklists.BlackList(enemy.WowGuid, TimeSpan.FromSeconds(15));
                    }
                }
                else
                    break;
            }

        }
        void ConsoleLog(string txt)
        {
            logService.ConsoleInfo(txt);
        }
        void ConsoleDebug(string txt)
        {
            logService.ConsoleDebug(txt);
        }
        void ConsoleWarning(string txt)
        {
            logService.ConsoleWarning(txt);
        }
        void ConsoleErrorAndThrow(string txt)
        {
            throw new InvalidOperationException(txt);
        }
    }
}