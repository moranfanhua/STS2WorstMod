using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using ReZeroSpire.ReZeroSpireCode.Helper;

namespace ReZeroSpire.ReZeroSpireCode.Patch;

[HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.EnterInternal))]
public static class BossRoomPatch
{
    public static void Postfix(CombatRoom __instance)
    {
        var encounterId = __instance.Encounter.Id.Entry;
        var roomType = __instance.Encounter.RoomType;
        Entry.Logger.Info($"CombatRoom.EnterInternal: id={encounterId}, roomType={roomType}");

        AudioHelper.TrySwapBossBgm(__instance);
    }
}
