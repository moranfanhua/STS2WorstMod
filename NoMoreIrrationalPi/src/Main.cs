using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace NoMoreIrrationalPi;

[ModInitializer(nameof(Initialize))]
public static class NoMoreIrrationalPiMain
{
    private const string ModId = "NoMoreIrrationalPi";
    private static Timer s_detectTimer = null!;
    private static Logger s_logger = null!;

    private static readonly string[] TargetModIds =
    {
        "ModConfig",
        "DamageMeter",
        "Rewind",
        "QuickLink",
        "SpeedX",
    };

    private const string TargetAuthor = "皮一下就很凡";

    public static void Initialize()
    {
        s_logger = new Logger(ModId, LogType.Generic);
        s_logger.Info("Arming deferred detection timer...");
        s_detectTimer = new Timer(Tick, null, 5000, Timeout.Infinite);
    }

    private static void Tick(object _)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        bool detected = false;
        string detectedInfo = "";

        foreach (var asm in assemblies)
        {
            string asmName;
            try
            {
                asmName = asm.GetName().Name ?? "";
            }
            catch
            {
                continue;
            }

            foreach (var targetId in TargetModIds)
            {
                if (string.Equals(asmName, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    detected = true;
                    detectedInfo += $"\n  [ID] {asmName}";
                    break;
                }
            }

            try
            {
                var descAttr = asm.GetCustomAttribute<AssemblyDescriptionAttribute>();
                if (descAttr?.Description != null && descAttr.Description.Contains(TargetAuthor))
                {
                    if (!detected)
                    {
                        detected = true;
                        detectedInfo += $"\n  [Desc] {asmName}";
                    }
                }
            }
            catch { }

            try
            {
                var companyAttr = asm.GetCustomAttribute<AssemblyCompanyAttribute>();
                if (companyAttr?.Company != null && companyAttr.Company.Contains(TargetAuthor))
                {
                    if (!detected)
                    {
                        detected = true;
                        detectedInfo += $"\n  [Company] {asmName}";
                    }
                }
            }
            catch { }

            try
            {
                var copyrightAttr = asm.GetCustomAttribute<AssemblyCopyrightAttribute>();
                if (copyrightAttr?.Copyright != null && copyrightAttr.Copyright.Contains(TargetAuthor))
                {
                    if (!detected)
                    {
                        detected = true;
                        detectedInfo += $"\n  [Copyright] {asmName}";
                    }
                }
            }
            catch { }

            try
            {
                var titleAttr = asm.GetCustomAttribute<AssemblyTitleAttribute>();
                if (titleAttr?.Title != null && titleAttr.Title.Contains(TargetAuthor))
                {
                    if (!detected)
                    {
                        detected = true;
                        detectedInfo += $"\n  [Title] {asmName}";
                    }
                }
            }
            catch { }

            try
            {
                var productAttr = asm.GetCustomAttribute<AssemblyProductAttribute>();
                if (productAttr?.Product != null && productAttr.Product.Contains(TargetAuthor))
                {
                    if (!detected)
                    {
                        detected = true;
                        detectedInfo += $"\n  [Product] {asmName}";
                    }
                }
            }
            catch { }
        }

        s_logger.Info($"Deferred check — detected: {detected}");

        if (detected)
        {
            s_logger.Warn("Target mod(s) or author detected!");
            s_logger.Warn($"Details:{detectedInfo}");
            s_logger.Warn("Crashing process in 5 seconds...");
            s_detectTimer = new Timer(CrashNow, null, 5000, Timeout.Infinite);
        }
        else
        {
            s_logger.Info("No target mods detected — rechecking in 5 seconds...");
            s_detectTimer = new Timer(Tick, null, 5000, Timeout.Infinite);
        }
    }

    private static void CrashNow(object _)
    {
        s_logger.Error("==================================================");
        s_logger.Error("FATAL: Incompatible mod(s) detected!");
        s_logger.Error("==================================================");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            TerminateProcess(GetCurrentProcess(), 1);
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
}
