using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace ReZeroSpire.ReZeroSpireCode.Setting;

public sealed class ReZeroSpireSettings
{
    public bool SkipArchitect { get; set; } = true;
    public bool EnableBossBgm { get; set; }
    public bool EnableDeathSfx { get; set; }
}

public static class ReZeroSpireSettingsPage
{
    private const string DataKey = "settings";

    public static readonly ModSettingsValueBinding<ReZeroSpireSettings, bool> SkipBinding =
        new("ReZeroSpire", DataKey, STS2RitsuLib.Utils.Persistence.SaveScope.Global,
            s => s.SkipArchitect, (s, v) => s.SkipArchitect = v);
    public static readonly ModSettingsValueBinding<ReZeroSpireSettings, bool> BossBgmBinding =
        new("ReZeroSpire", DataKey, STS2RitsuLib.Utils.Persistence.SaveScope.Global,
            s => s.EnableBossBgm, (s, v) => s.EnableBossBgm = v);
    public static readonly ModSettingsValueBinding<ReZeroSpireSettings, bool> DeathSfxBinding =
        new("ReZeroSpire", DataKey, STS2RitsuLib.Utils.Persistence.SaveScope.Global,
            s => s.EnableDeathSfx, (s, v) => s.EnableDeathSfx = v);

    public static bool SkipArchitect => SkipBinding.Read();
    public static bool EnableBossBgm => BossBgmBinding.Read();
    public static bool EnableDeathSfx => DeathSfxBinding.Read();

    public static void Register()
    {
        ModDataStore.For("ReZeroSpire").Register(
            key: DataKey,
            fileName: "settings.json",
            scope: STS2RitsuLib.Utils.Persistence.SaveScope.Global,
            defaultFactory: () => new ReZeroSpireSettings(),
            autoCreateIfMissing: true);

        RitsuLibFramework.RegisterModSettings("ReZeroSpire", page => page
            .WithTitle(ModSettingsText.Literal("Re:Zero Spire"))
            .AddSection("checkpoint", section => section
                .WithTitle(ModSettingsText.Literal("Setting"))
                .AddToggle("skip_architect",
                    ModSettingsText.Literal("Skip Architect Rewind"), SkipBinding)
                .AddToggle("boss_bgm",
                    ModSettingsText.Literal("Enable Boss BGM"), BossBgmBinding)
                .AddToggle("death_sfx",
                    ModSettingsText.Literal("Enable Death SFX"), DeathSfxBinding)));

        Entry.Logger.Info($"Settings: SkipArchitect={SkipArchitect}, BossBgm={EnableBossBgm}, DeathSfx={EnableDeathSfx}");
    }
}
