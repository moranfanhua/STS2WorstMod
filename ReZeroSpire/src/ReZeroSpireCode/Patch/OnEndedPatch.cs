using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using ReZeroSpire.ReZeroSpireCode.Helper;
using ReZeroSpire.ReZeroSpireCode.Setting;

namespace ReZeroSpire.ReZeroSpireCode.Patch;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.OnEnded))]
public static class OnEndedPatch
{
    public static void Prefix(bool isVictory)
    {
        CheckpointHelper.WasVictory = isVictory;
    }

    public static void Postfix(bool isVictory)
    {
        if (isVictory)
        {
            AudioHelper.StopBgm();
            return;
        }

        if (RunManager.Instance.IsAbandoned) return;
        if (RunManager.Instance.NetService.Type != NetGameType.Singleplayer) return;

        // SkipArchitect: don't restore if we're in the last act
        if (ReZeroSpireSettingsPage.SkipArchitect && IsLastAct())
            return;

        CheckpointHelper.Restore();
    }

    private static bool IsLastAct()
    {
        var state = Traverse.Create(RunManager.Instance)
            .Property("State").GetValue<RunState>();
        if (state == null) return false;
        return state.CurrentActIndex >= state.Acts.Count - 1;
    }
}
