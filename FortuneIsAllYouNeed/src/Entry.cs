using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace FortuneIsAllYouNeed;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    public const string ModId = "FortuneIsAllYouNeed";
    public static Logger Logger { get; private set; } = null!;

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        Logger = RitsuLibFramework.CreateLogger(ModId);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(OnCombatEnded);

        Logger.Info("Fortune Is All You Need Initialized");
    }

    private static void OnCombatEnded(CombatEndedEvent evt)
    {
        try
        {
            foreach (dynamic player in evt.RunState.Players)
            {
                Transformer.TransformAllCards(player, Logger);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error during card transformation: {ex}");
        }
    }
}
