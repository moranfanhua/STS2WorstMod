using System;
using System.IO;
using Godot;

namespace ReZeroSpire.ReZeroSpireCode.Helper;

public static class CheckpointHelper
{
    private const string SaveFile = "current_run.save";
    private const string CheckpointFile = "current_run.checkpoint";

    public static bool WasVictory { get; set; }
    public static bool CaptureNextSave { get; set; }
    public static bool AutoContinue { get; set; }
    public static int DeathCount { get; set; }

    public static void CopySaveToCheckpoint()
    {
        if (!CaptureNextSave) return;
        CaptureNextSave = false;

        try
        {
            var savesDir = FindSavesDir(
                MegaCrit.Sts2.Core.Saves.SaveManager.Instance.CurrentProfileId);
            if (savesDir == null) return;
            var savePath = Path.Combine(savesDir, SaveFile);
            var checkpointPath = Path.Combine(savesDir, CheckpointFile);
            if (File.Exists(savePath))
            {
                File.Copy(savePath, checkpointPath, overwrite: true);
                Entry.Logger.Info($"Checkpoint saved ({new FileInfo(checkpointPath).Length} bytes)");
            }
        }
        catch (Exception ex) { Entry.Logger.Error($"CopySaveToCheckpoint: {ex}"); }
    }

    public static void Restore()
    {
        try
        {
            var savesDir = FindSavesDir(
                MegaCrit.Sts2.Core.Saves.SaveManager.Instance.CurrentProfileId);
            if (savesDir == null) return;
            var savePath = Path.Combine(savesDir, SaveFile);
            var checkpointPath = Path.Combine(savesDir, CheckpointFile);
            if (!File.Exists(checkpointPath)) return;
            File.Copy(checkpointPath, savePath, overwrite: true);
            Entry.Logger.Info("Checkpoint restored.");
        }
        catch (Exception ex) { Entry.Logger.Error($"Restore: {ex}"); }
    }

    public static string? FindSavesDir(int profileId)
    {
        var userDir = OS.GetUserDataDir();
        var steamDir = Path.Combine(userDir, "steam");
        if (!Directory.Exists(steamDir)) return null;
        foreach (var d in Directory.GetDirectories(steamDir))
        {
            var sd = Path.Combine(d, "modded", $"profile{profileId}", "saves");
            if (Directory.Exists(sd)) return sd;
        }
        return null;
    }
}
