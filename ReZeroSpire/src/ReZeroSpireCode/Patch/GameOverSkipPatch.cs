using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using ReZeroSpire.ReZeroSpireCode.Helper;
using ReZeroSpire.ReZeroSpireCode.Setting;

namespace ReZeroSpire.ReZeroSpireCode.Patch;

[HarmonyPatch(typeof(NRun), nameof(NRun.ShowGameOverScreen))]
public static class GameOverSkipPatch
{
    public static bool Prefix()
    {
        if (RunManager.Instance.IsAbandoned) return true;
        if (RunManager.Instance.NetService.Type != NetGameType.Singleplayer) return true;
        if (CheckpointHelper.WasVictory) return true;

        if (ReZeroSpireSettingsPage.SkipArchitect && IsLastAct())
        {
            Entry.Logger.Info("Death in final act — SkipArchitect enabled, showing normal game over.");
            return true;
        }

        Entry.Logger.Info("Death — returning to main menu, auto-continue enabled.");

        AudioHelper.PlayDeathSound();
        CheckpointHelper.AutoContinue = true;

        var game = NGame.Instance;
        if (game != null)
            TaskHelper.RunSafely(game.ReturnToMainMenuAfterRun());
        return false;
    }

    private static bool IsLastAct()
    {
        var state = Traverse.Create(RunManager.Instance)
            .Property("State").GetValue<RunState>();
        if (state == null) return false;

        return state.CurrentActIndex >= state.Acts.Count - 1;
    }
}
