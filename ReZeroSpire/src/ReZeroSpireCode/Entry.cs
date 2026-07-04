using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using ReZeroSpire.ReZeroSpireCode.Setting;
using STS2RitsuLib;
using STS2RitsuLib.Audio;
using STS2RitsuLib.Interop;

namespace ReZeroSpire.ReZeroSpireCode;

[ModInitializer(nameof(Init))]
public static class Entry
{
    public const string ModId = "ReZeroSpire";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();

        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        FmodStudioDeferredBankRegistration.RegisterBank("res://ReZeroSpire/audios/ReZeroSpire.bank");
        FmodStudioDeferredBankRegistration.RegisterStudioGuidMappings("res://ReZeroSpire/audios/GUIDs.txt");

        ReZeroSpireSettingsPage.Register();

        new Harmony(ModId).PatchAll(assembly);

        Logger.Info("ReZeroSpire Initialized");
    }
}
