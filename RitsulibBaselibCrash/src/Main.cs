using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace RitsulibBaselibCrash;

[ModInitializer(nameof(Initialize))]
public static class RitsulibBaselibCrashMain
{
    private const string ModId = "RitsulibBaselibCrash";
    private static Timer s_crashTimer = null!;
    private static Logger s_logger = null!;

    public static void Initialize()
    {
        s_logger = new Logger(ModId, LogType.Generic);
        s_logger.Info("Arming deferred detection timer...");
        s_crashTimer = new Timer(Tick, null, 5000, Timeout.Infinite);
    }

    private static void Tick(object _)
    {
        var baseLibLoaded = Type.GetType("BaseLib.BaseLibMain, BaseLib") != null;
        var ritsuLibLoaded = Type.GetType("STS2RitsuLib.RitsuLibFramework, STS2-RitsuLib") != null;

        s_logger.Info($"Deferred check — BaseLib: {baseLibLoaded}, RitsuLib: {ritsuLibLoaded}");

        if (baseLibLoaded && ritsuLibLoaded)
        {
            s_logger.Warn("Both BaseLib and RitsuLib confirmed loaded!");
            s_logger.Warn("Crashing process in 5 seconds...");

            s_crashTimer = new Timer(CrashNow, null, 5000, Timeout.Infinite);
        }
        else
        {
            s_logger.Info("Not both present — checking again in 5 seconds...");
            s_crashTimer = new Timer(Tick, null, 5000, Timeout.Infinite);
        }
    }

    private static void CrashNow(object _)
    {
        s_logger.Error("==================================================");
        s_logger.Error("FATAL: BaseLib + RitsuLib Harmony patch conflict!");
        s_logger.Error("==================================================");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            TerminateProcess(GetCurrentProcess(), 1);
        }

        // Fallback: StackOverflowException — uncatchable from .NET 2.0+
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
