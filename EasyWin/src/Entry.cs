using System;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;

namespace EasyWin;

[ModInitializer("Init")]
public class Entry
{
    private static bool _done;

    public static void Init()
    {
        Log.Info("[EasyWin] Initializing, subscribing to RoomEnteredEvent and RunEndedEvent...");
        _ = RitsuLibFramework.SubscribeLifecycle<RoomEnteredEvent>(OnRoomEntered);
        _ = RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => _done = false);
    }

    private static void OnRoomEntered(RoomEnteredEvent evt)
    {
        if (_done) return;
        _done = true;

        try
        {
            if (!RunManager.Instance.IsInProgress)
            {
                Log.Warn("[EasyWin] Run not in progress, skipping.");
                return;
            }

            var serializedRun = RunManager.Instance.OnEnded(isVictory: true);

            NRun.Instance?.ShowGameOverScreen(serializedRun);

            Log.Info("[EasyWin] Victory! Run ended, game over screen shown.");
        }
        catch (Exception ex)
        {
            Log.Error($"[EasyWin] Exception: {ex}");
        }
    }
}
