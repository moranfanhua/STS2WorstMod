using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using ReZeroSpire.ReZeroSpireCode.Helper;

namespace ReZeroSpire.ReZeroSpireCode.Patch;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewSingleplayer))]
public static class NewRunResetPatch
{
    public static void Postfix()
    {
        CheckpointHelper.DeathCount = 0;
        Entry.Logger.Info("New run started — death count reset.");
    }
}
