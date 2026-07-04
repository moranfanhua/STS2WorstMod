using System;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ReZeroSpire.ReZeroSpireCode.Helper;

namespace ReZeroSpire.ReZeroSpireCode.Patch;

[HarmonyPatch(typeof(NGame), "LoadMainMenu")]
public static class MainMenuAutoContinuePatch
{
    public static bool Prefix(NGame __instance)
    {
        if (!CheckpointHelper.AutoContinue) return true;
        CheckpointHelper.AutoContinue = false;

        Entry.Logger.Info("Skipping main menu, loading checkpoint run.");
        TaskHelper.RunSafely(LoadCheckpointAsync(__instance));
        return false;
    }

    private static async Task LoadCheckpointAsync(NGame game)
    {
        try
        {
            var result = SaveManager.Instance.LoadRunSave();
            if (!result.Success || result.SaveData == null)
            {
                Entry.Logger.Error("Failed to load checkpoint.");
                Traverse.Create(game).Method("LoadMainMenu").GetValue();
                return;
            }

            var save = result.SaveData;
            save.PreFinishedRoom = null;
            var state = RunState.FromSerializable(save);

            await RunManager.Instance.SetUpSavedSingleplayer(state, save);
            await PreloadManager.LoadRunAssets(
                state.Players.Select(p => p.Character));
            await PreloadManager.LoadActAssets(state.Act);
            RunManager.Instance.Launch();
            game.RootSceneContainer.SetCurrentScene(NRun.Create(state));
            await RunManager.Instance.GenerateMap();
            await RunManager.Instance.LoadIntoLatestMapCoord(null);
            await game.Transition.FadeIn();

            Entry.Logger.Info("Checkpoint loaded — run resumed from act start.");
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"LoadCheckpointAsync: {ex}");
            try { Traverse.Create(game).Method("LoadMainMenu").GetValue(); } catch { /* lost */ }
        }
    }
}
