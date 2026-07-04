using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using ReZeroSpire.ReZeroSpireCode.Helper;

namespace ReZeroSpire.ReZeroSpireCode.Patch;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetActInternal))]
public static class EnterActPatch
{
    public static void Postfix(int actIndex)
    {
        Entry.Logger.Info($"EnterAct: actIndex={actIndex}");
        CheckpointHelper.CaptureNextSave = true;
    }
}
