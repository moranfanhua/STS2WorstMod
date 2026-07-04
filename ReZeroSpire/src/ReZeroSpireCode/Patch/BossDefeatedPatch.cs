using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using ReZeroSpire.ReZeroSpireCode.Helper;

namespace ReZeroSpire.ReZeroSpireCode.Patch;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
public static class BossDefeatedPatch
{
    public static void Prefix()
    {
        AudioHelper.StopBgm();
    }
}
