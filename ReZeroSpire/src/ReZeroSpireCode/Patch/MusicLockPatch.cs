using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;
using ReZeroSpire.ReZeroSpireCode.Helper;

namespace ReZeroSpire.ReZeroSpireCode.Patch;

[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.UpdateMusic))]
public static class MusicLockUpdatePatch
{
    public static bool Prefix()
    {
        if (AudioHelper.IsPlayingCustomBgm)
            return false;

        return true;
    }
}

[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.StopMusic))]
public static class MusicLockStopPatch
{
    public static void Prefix()
    {
    }
}
