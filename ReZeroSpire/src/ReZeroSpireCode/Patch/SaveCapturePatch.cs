using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using ReZeroSpire.ReZeroSpireCode.Helper;

namespace ReZeroSpire.ReZeroSpireCode.Patch;

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveRun), typeof(AbstractRoom), typeof(bool))]
public static class SaveCapturePatch
{
    public static async void Postfix(Task __result)
    {
        try { await __result; } catch { return; }
        CheckpointHelper.CopySaveToCheckpoint();
    }
}
