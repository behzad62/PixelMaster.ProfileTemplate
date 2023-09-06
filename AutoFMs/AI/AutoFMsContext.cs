using FluentBehaviourTree;
using PixelMaster.Core.Behaviors;
using PixelMaster.Core;
using PixelMaster.Core.Behaviors.Transport;
using PixelMaster.Core.Managers;
using PixelMaster.Core.Wow.Objects;
using PixelMaster.Services.Behaviors;
using PixelMaster.Services.Input;
using PixelMaster.Services.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PixelMaster.Core.Wow;

namespace Plugins.Plugins.AI
{
    /// <summary>
    /// This is the class that defines this plugin behavior. Like any custom behavior it derives from the 'BaseContext' class. 
    /// </summary>
    public class AutoFMsBehavior : BaseContext
    {
        public AutoFMsBehavior() : base()
        {
            npcBlackLister = new BlackLister<WowGUID>();
        }
        private BlackLister<WowGUID> npcBlackLister;
        private readonly PlayerUnit Me = ObjectManager.Instance.Player;
        private bool didOnStart = false;
        private Status mainStatus = Status.Invalid;

        //This part can be used in all plugins as it is.
        #region Fixed
        public override async Task CancelTask()
        {
            if (isCanceling || disposed)
                return;
            isCanceling = true;
            _cancellation.Cancel();
            await taskTracker.GetCombinedTask();
            IsCanceled = true;
            ConsoleDebug($"is Canceled.");
        }
        public override async Task PauseTask()
        {
            if (isPausing || isCanceling || disposed)
                return;
            isPausing = true;
            _cancellation.Cancel();
            await taskTracker.GetCombinedTask();
            IsPaused = true;
            ConsoleDebug($"is Paused.");
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
            ConsoleDebug($"Resumed.");
        }
        public override void ResetTask()
        {
            base.ResetTask();
            didOnStart = false;
            mainStatus = Status.Invalid;
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
                ConsoleError($"Exception {ex.Message}!");
                logService.LogError(ex);
                mainStatus = Status.Failure;
            }
            finally
            {
                taskTracker.UnTrackTask();
            }
        }
        #endregion
        //---------------------------------------------
        void OnStart()
        {
            ConsoleInfo("Started!");
            didOnStart = true;
        }
        //This methods runs the behavior of this plugin until it is finished or paused.
        private async Task<bool> MainRoutine()
        {
            if (!didOnStart)
                OnStart();
            IMouseAccessToken? mouseAccess = null;

            try
            {
                var flyMaster = GetNearbyFlightMaster();
                if (flyMaster is null)
                    return false;
                mouseAccess = ActionManager.Instance.MouseAccess.ReserveExclusiveMouseAccess("Learn FP");
                await mouseAccess.WaitForAccess(5000, _cancellation.Token).ConfigureAwait(false);
                var mapId = ObjectManager.Instance.CurrentMap.MapID;
                int i = 0;
                while (i++ < 3)
                {
                    try
                    {

                        int timeout = 5000;
                        await ActionManager.Instance.PlayerMovement.NavigateToTarget(mouseAccess, flyMaster.Position, mapId, 4, _cancellation.Token, false, false, true).ConfigureAwait(false);
                        if (ObjectManager.Instance.Player.IsMoving)
                            await ActionManager.Instance.PlayerMovement.StopRunning(_cancellation.Token).ConfigureAwait(false);
                        await OpenTaxiMap(mouseAccess, flyMaster, timeout, _cancellation.Token).ConfigureAwait(false);
                        TaxiSaver.Instance.SetFlightMasterAsLearned(flyMaster.Id);
                        await TaxiSaver.Instance.UpdateTaxiMapInfo(flyMaster.Id, _cancellation.Token).ConfigureAwait(false);
                        //TaxiSaver.Instance.UpdateLearnedFMsFromTaxiMap();
                        ConsoleInfo($"Successfully learned new taxi node {flyMaster}");
                        return true;
                    }
                    catch (OperationCanceledException)
                    {
                        if (_cancellation.IsCancellationRequested)
                            throw;
                    }
                    catch (TimeoutException ex)
                    {
                        LogError(ex);
                        npcBlackLister.BlackList(flyMaster.WowGuid, TimeSpan.FromMinutes(5));
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                        npcBlackLister.BlackList(flyMaster.WowGuid, TimeSpan.FromMinutes(5));
                    }
                    await ObjectManager.Instance.WaitForUpdatedData(_cancellation.Token).ConfigureAwait(false);
                }
                ConsoleWarning($"Failed to learn the new taxi node {flyMaster}");
                return false;
            }
            finally
            {
                if (mouseAccess != null)
                    ActionManager.Instance.MouseAccess.ReleaseAccess(mouseAccess);
            }
        }
        internal bool AnyNearbyTaxiToLearn()
        {
            if (!ObjectManager.Instance.IsMemoryDataAvailable)
                return false;
            var wantedObjects = ObjectManager.Instance.GetVisibleUnits().Where(o => o.IsFlightMaster);
            var playerFaction = ObjectManager.Instance.Player.PlayerFaction;
            foreach (var npc in wantedObjects)
            {
                if (!npc.Faction.IsEnemyWith(playerFaction) && !TaxiSaver.Instance.IsFlightMasterLearned(npc.Id) && !npcBlackLister.IsBlackListed(npc.WowGuid))
                {
                    return true;
                }

            }
            return false;
        }
        private WowUnit? GetNearbyFlightMaster()
        {
            if (!ObjectManager.Instance.IsMemoryDataAvailable)
                return null;
            var playerFaction = ObjectManager.Instance.Player.PlayerFaction;
            return ObjectManager.Instance.GetVisibleUnits().FirstOrDefault(o => o.IsFlightMaster && !o.Faction.IsEnemyWith(playerFaction) && !npcBlackLister.IsBlackListed(o.WowGuid));

        }
        private async Task OpenTaxiMap(IMouseAccessToken accessToken, WowUnit flightMaster, int timeout, CancellationToken cancellationToken)
        {
            int timePassed = 0;
            while (!ObjectManager.Instance.IsTaxiMapOpen)
            {
                await ActionManager.Instance.PlayerActions.CancelForm(cancellationToken).ConfigureAwait(false);
                if (timePassed % 1000 == 0)
                {
                    await ActionManager.Instance.PlayerActions.ClearTarget(cancellationToken).ConfigureAwait(false);
                    await ActionManager.Instance.PlayerActions.InteractWithUnit(accessToken, flightMaster, cancellationToken).ConfigureAwait(false);
                }
                await ActionManager.Instance.AddonInterface.SetEncodingModeDefault(cancellationToken).ConfigureAwait(false);
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                timePassed += 100;
                if (timeout > 0 && timePassed > timeout)
                {
                    throw new TimeoutException($"Opening taximap for FM '{flightMaster.Name}' timed out");
                }
                if (ObjectManager.Instance.IsGossipOpen)
                {
                    string cmd = "/colorify gossipicon 132057";
                    await ActionManager.Instance.RunCommand(cmd, cancellationToken).ConfigureAwait(false);
                    await Task.Delay(300, cancellationToken).ConfigureAwait(false);
                    var errorMsg = ObjectManager.Instance.LastErrorMessage;
                    if (errorMsg != null && errorMsg.Contains("flight locations connected"))
                        break;
                }
            }

        }
        //private void BlacklistGUID(WowGUID guid, TimeSpan duration, string reason)
        //{
        //    npcBlackLister.BlackList(guid, duration);
        //    CoreModule.Log.ConsoleWarning($"Blacklisting flight master with GUID {guid:X} for {(int)duration.TotalSeconds} seconds. {reason}.");
        //}
        //private bool IsGUIDBlacklisted(WowGUID guid)
        //{
        //    return npcBlackLister.IsBlackListed(guid);
        //}
        void ConsoleDebug(string txt)
        {
            logService.ConsoleDebug($"{nameof(AutoFMs)}: {txt}");
        }
        void ConsoleInfo(string txt)
        {
            logService.ConsoleInfo($"{nameof(AutoFMs)}: {txt}");
        }
        void ConsoleError(string txt)
        {
            logService.ConsoleError($"{nameof(AutoFMs)}: {txt}");
        }
        void ConsoleWarning(string txt)
        {
            logService.ConsoleWarning($"{nameof(AutoFMs)}: {txt}");
        }
        void ConsoleErrorAndThrow(string txt)
        {
            logService.ConsoleError($"{nameof(AutoFMs)}: {txt}");
            throw new InvalidOperationException(txt);
        }
    }
}
