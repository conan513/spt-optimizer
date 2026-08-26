using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using SPTOptimizer.Config;
using SPTOptimizer.Patches;

namespace SPTOptimizer.Utils;

/// <summary>
/// Interacts with the Windows memory manager.
/// NOTE: Working set trimming in-raid causes severe hard page-fault stuttering.
/// It is only safely executed in menus or when exiting a raid.
/// </summary>
public static class MemoryTrimmer
{
    private static ManualLogSource? _log;

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern int EmptyWorkingSet(IntPtr hProcess);

    public static void Initialize(ManualLogSource log)
    {
        _log = log;
    }

    public static void TrimWorkingSet(string callerReason, bool forceInRaid = false)
    {
        if (!OptimizerConfig.EnableActiveRAMCleaner.Value)
            return;

        // Never trim working set during an active raid unless forced, as it causes hard page faults and micro-stutters
        if (!forceInRaid && LowMemoryWatchdogComponent.IsInRaid())
        {
            _log?.LogDebug($"[RAMCleaner] Skipped working set trim in-raid to prevent micro-lag ({callerReason}).");
            return;
        }

        try
        {
            using var proc = Process.GetCurrentProcess();
            long beforeMB = proc.WorkingSet64 / (1024 * 1024);

            int result = EmptyWorkingSet(proc.Handle);
            if (result != 0)
            {
                proc.Refresh();
                long afterMB = proc.WorkingSet64 / (1024 * 1024);
                long freedMB = beforeMB - afterMB;
                _log?.LogInfo($"[RAMCleaner] Trimmed working set ({callerReason}): {beforeMB} MB -> {afterMB} MB (Freed {freedMB} MB RAM)");
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[RAMCleaner] Failed to trim working set: {ex.Message}");
        }
    }
}
