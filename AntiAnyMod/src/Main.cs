using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace AntiAnyMod;

[ModInitializer(nameof(Initialize))]
public static class AntiAnyModMain
{
    private const string ModId = "AntiAnyMod";
    private static Timer s_crashTimer = null!;
    private static Logger s_logger = null!;

    private static readonly HashSet<string> s_whitelistedPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "System",
        "Microsoft",
        "mscorlib",
        "netstandard",
        "Godot",
        "Mono",
        "WindowsBase",
        "Anonymously",
        "xunit",
        "NUnit",
        "Newtonsoft",
    };

    private static readonly HashSet<string> s_whitelistedExact = new(StringComparer.OrdinalIgnoreCase)
    {
        "sts2",
        "0Harmony",
        "GodotSharp",
        "netstandard",
        "Humanizer",
        "MegaCrit.Sts2",
        "MegaCrit",
    };

    private static readonly string[] s_universalWhitelist =
    {
        "GodotSharp",
        "GodotSharpEditor",
        "GodotSourceGenerators",
        "GodotPlugin",
        "System.",
        "System,",
        "Microsoft.",
        "mscorlib",
        "netstandard",
        "WindowsBase",
        "Presentation",
    };

    private static readonly List<string> s_detectedMods = new();
    private static readonly HashSet<string> s_knownModIds = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize()
    {
        s_logger = new Logger(ModId, LogType.Generic);

        s_crashTimer = new Timer(FirstSweep, null, 5000, Timeout.Infinite);
    }

    private static void FirstSweep(object _)
    {
        RunDetection();

        if (s_detectedMods.Count > 0)
        {
            LogDetectionResult();
            s_logger.Warn("[Baselib] Crashing in 5 seconds...");
            s_crashTimer = new Timer(CrashNow, null, 5000, Timeout.Infinite);
        }
        else
        {
            s_logger.Info("First sweep clean. Continuous monitoring armed (every 15s).");
            s_crashTimer = new Timer(PeriodicSweep, null, 15000, Timeout.Infinite);
        }
    }

    private static void PeriodicSweep(object _)
    {
        RunDetection();

        if (s_detectedMods.Count > 0)
        {
            LogDetectionResult();
            s_logger.Warn("Foreign mod detected post-launch! Crashing in 5 seconds...");
            s_crashTimer = new Timer(CrashNow, null, 5000, Timeout.Infinite);
        }
        else
        {
            s_crashTimer = new Timer(PeriodicSweep, null, 15000, Timeout.Infinite);
        }
    }

    private static void RunDetection()
    {
        s_detectedMods.Clear();
        s_knownModIds.Clear();

        DetectByAssemblyScan();
        DetectByModsFolder();
    }

    private static void DetectByAssemblyScan()
    {
        var ourAssembly = Assembly.GetExecutingAssembly();
        var ourName = ourAssembly.GetName().Name ?? "";
        var ourLocation = NormalizePath(ourAssembly.Location);

        Assembly[] assemblies;
        try
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }
        catch
        {
            s_logger.Warn("Failed to enumerate AppDomain assemblies.");
            return;
        }

        int skipped = 0;
        int checked_ = 0;

        foreach (var asm in assemblies)
        {
            string shortName;
            try
            {
                shortName = asm.GetName().Name ?? "";
            }
            catch
            {
                skipped++;
                continue;
            }

            if (string.Equals(shortName, ourName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (MatchesUniversalWhitelist(shortName))
            {
                skipped++;
                continue;
            }

            if (s_whitelistedExact.Contains(shortName))
            {
                skipped++;
                continue;
            }

            if (MatchesWhitelistedPrefix(shortName))
            {
                skipped++;
                continue;
            }

            checked_++;

            bool hasModInitializer = false;
            try
            {
                hasModInitializer = asm.GetCustomAttributes()
                    .Any(a =>
                    {
                        var typeName = a.GetType().FullName ?? "";
                        return typeName == "MegaCrit.Sts2.Core.Modding.ModInitializerAttribute";
                    });
            }
            catch
            {
            }

            if (hasModInitializer)
            {
                s_detectedMods.Add($"{shortName} [ModInitializer]");
                s_knownModIds.Add(shortName);
                continue;
            }

            string? asmLocation;
            try
            {
                asmLocation = NormalizePath(asm.Location);
            }
            catch
            {
                asmLocation = null;
            }

            if (!string.IsNullOrEmpty(asmLocation) &&
                IsFromForeignModDirectory(asmLocation, ourLocation))
            {
                s_detectedMods.Add($"{shortName} (loaded from mod directory)");
                s_knownModIds.Add(shortName);
            }
        }
    }

    private static bool IsFromForeignModDirectory(string asmPath, string ourPath)
    {
        if (IsFromForeignSubfolder(asmPath, ourPath, @"\mods\"))
            return true;

        if (IsFromForeignWorkshopItem(asmPath, ourPath))
            return true;

        return false;
    }

    private static bool IsFromForeignSubfolder(string asmPath, string ourPath, string marker)
    {
        var asmIdx = asmPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (asmIdx < 0) return false;

        var ourIdx = ourPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (ourIdx < 0) return true; 

        var asmTail = asmPath.Substring(asmIdx + marker.Length);
        var ourTail = ourPath.Substring(ourIdx + marker.Length);

        var asmFolder = asmTail.Split('\\')[0];
        var ourFolder = ourTail.Split('\\')[0];

        return !string.Equals(asmFolder, ourFolder, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFromForeignWorkshopItem(string asmPath, string ourPath)
    {
        const string wsMarker = @"\workshop\content\";

        var asmIdx = asmPath.IndexOf(wsMarker, StringComparison.OrdinalIgnoreCase);
        if (asmIdx < 0) return false;

        var ourIdx = ourPath.IndexOf(wsMarker, StringComparison.OrdinalIgnoreCase);
        if (ourIdx < 0) return true;

        var asmTail = asmPath.Substring(asmIdx + wsMarker.Length);
        var ourTail = ourPath.Substring(ourIdx + wsMarker.Length);

        var asmParts = asmTail.Split('\\');
        var ourParts = ourTail.Split('\\');

        if (asmParts.Length < 2 || ourParts.Length < 2)
            return false; // can't determine item IDs

        var asmAppId = asmParts[0];
        var asmItemId = asmParts[1];
        var ourAppId = ourParts[0];
        var ourItemId = ourParts[1];

        return string.Equals(asmAppId, ourAppId, StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(asmItemId, ourItemId, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        return (path ?? "").Replace('/', '\\');
    }

    private static void DetectByModsFolder()
    {
        var ourDllPath = NormalizePath(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(ourDllPath))
            return;

        string? ourModFolder = null;
        string? ourWorkshopItemId = null;

        var ourDllDir = Path.GetDirectoryName(ourDllPath);
        if (ourDllDir != null)
        {
            var manifestBeside = Path.Combine(ourDllDir, "mod_manifest.json");
            if (File.Exists(manifestBeside))
            {
                ourModFolder = Path.GetFileName(ourDllDir);
            }
            else
            {
                var parentDir = Path.GetDirectoryName(ourDllDir);
                if (parentDir != null)
                {
                    var manifestUp = Path.Combine(parentDir, "mod_manifest.json");
                    if (File.Exists(manifestUp))
                        ourModFolder = Path.GetFileName(parentDir);
                }
            }

            ourWorkshopItemId = ExtractWorkshopItemId(ourDllPath);
        }

        if (ourModFolder != null)
        {
            var modsDir = FindModsRoot(ourDllPath);
            if (modsDir != null)
            {
                ScanManifestsInDirectory(modsDir, ourModFolder, ourWorkshopItemId, isWorkshop: false);
            }
        }

        if (ourWorkshopItemId != null)
        {
            var wsContentDir = FindWorkshopContentRoot(ourDllPath);
            if (wsContentDir != null)
            {
                ScanManifestsInDirectory(wsContentDir, ourModFolder, ourWorkshopItemId, isWorkshop: true);
            }
        }

        s_logger.Info($"Mods-folder scan complete. Total flagged: {s_detectedMods.Count}.");
    }

    private static string? FindModsRoot(string dllPath)
    {
        var dir = Path.GetDirectoryName(dllPath);
        while (dir != null)
        {
            var name = Path.GetFileName(dir);
            if (string.Equals(name, "mods", StringComparison.OrdinalIgnoreCase))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    private static string? FindWorkshopContentRoot(string dllPath)
    {
        var idx = dllPath.IndexOf(@"\workshop\content\", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        // Return up to and including the app ID: \workshop\content\2868840
        var tail = dllPath.Substring(idx + @"\workshop\content\".Length);
        var slashIdx = tail.IndexOf('\\');
        if (slashIdx < 0) return null;

        return dllPath.Substring(0, idx + @"\workshop\content\".Length + slashIdx);
    }

    private static string? ExtractWorkshopItemId(string path)
    {
        var idx = path.IndexOf(@"\workshop\content\", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var tail = path.Substring(idx + @"\workshop\content\".Length);
        var parts = tail.Split('\\');
        return parts.Length >= 2 ? parts[1] : null;
    }

    private static void ScanManifestsInDirectory(
        string rootDir, string? ourModFolder, string? ourWorkshopItemId, bool isWorkshop)
    {
        string[] subDirs;
        try
        {
            subDirs = Directory.GetDirectories(rootDir);
        }
        catch
        {
            return;
        }

        foreach (var subDir in subDirs)
        {
            var subDirName = Path.GetFileName(subDir);

            if (!isWorkshop && ourModFolder != null &&
                string.Equals(subDirName, ourModFolder, StringComparison.OrdinalIgnoreCase))
                continue;

            if (isWorkshop && ourWorkshopItemId != null &&
                string.Equals(subDirName, ourWorkshopItemId, StringComparison.OrdinalIgnoreCase))
                continue;

            var manifestPath = Path.Combine(subDir, "mod_manifest.json");
            if (File.Exists(manifestPath))
            {
                ProcessManifest(manifestPath, subDirName, isWorkshop);
                continue;
            }

            if (isWorkshop)
            {
                try
                {
                    foreach (var innerDir in Directory.GetDirectories(subDir))
                    {
                        var innerManifest = Path.Combine(innerDir, "mod_manifest.json");
                        if (File.Exists(innerManifest))
                        {
                            var innerName = Path.GetFileName(innerDir);
                            if (ourModFolder != null &&
                                string.Equals(innerName, ourModFolder, StringComparison.OrdinalIgnoreCase))
                                continue;

                            ProcessManifest(innerManifest, innerName, isWorkshop);
                            break;
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }

    private static void ProcessManifest(string manifestPath, string folderName, bool isWorkshop)
    {
        string modId = folderName;
        try
        {
            var json = File.ReadAllText(manifestPath);
            var idMatch = System.Text.RegularExpressions.Regex.Match(
                json, @"""id""\s*:\s*""([^""]+)""");
            if (idMatch.Success)
                modId = idMatch.Groups[1].Value;
        }
        catch
        {
        }

        if (!s_knownModIds.Contains(modId))
        {
            var source = isWorkshop
                ? $"workshop/{folderName}"
                : $"mods/{folderName}";
            s_detectedMods.Add($"{modId} (manifest in {source})");
            s_knownModIds.Add(modId);
        }
    }

    private static bool MatchesUniversalWhitelist(string name)
    {
        foreach (var entry in s_universalWhitelist)
        {
            if (name.StartsWith(entry, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool MatchesWhitelistedPrefix(string name)
    {
        foreach (var prefix in s_whitelistedPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void LogDetectionResult()
    {
        s_logger.Error("[Baselib] The game will now be terminated.");
    }


    private static void CrashNow(object _)
    {

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            TerminateProcess(GetCurrentProcess(), 1);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            kill(GetCurrentProcess(), 9);
        }
        CauseStackOverflow();
    }

    private static void CauseStackOverflow()
    {
        CauseStackOverflow();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    [DllImport("libc.so.6")]
    private static extern int kill(IntPtr pid, int sig);
}
