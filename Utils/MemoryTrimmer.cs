using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using SPTOptimizer.Config;
using SPTOptimizer.Patches;

namespace SPTOptimizer.Utils;

/// <summary>
/// Interacts with the OS memory manager (Windows NT WorkingSet and Linux / Wine glibc malloc_trim).
/// </summary>
public static class MemoryTrimmer
{
    private static ManualLogSource? _log;
    private static bool _mallocTrimFailed;
    private static bool _emptyWorkingSetFailed;

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern int EmptyWorkingSet(IntPtr hProcess);

    [DllImport("libc", EntryPoint = "malloc_trim", SetLastError = true)]
    private static extern int malloc_trim_libc(IntPtr pad);

    [DllImport("msvcrt.dll", EntryPoint = "malloc_trim", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern int malloc_trim_msvcrt(IntPtr pad);

    public static void Initialize(ManualLogSource log)
    {
        _log = log;
    }

    public static long GetProcessWorkingSetMB()
    {
        try
        {
            using var proc = Process.GetCurrentProcess();
            return proc.WorkingSet64 / (1024 * 1024);
        }
        catch
        {
            return 0;
        }
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
            long beforeMB = GetProcessWorkingSetMB();

            // 1. Try Windows EmptyWorkingSet
            if (!_emptyWorkingSetFailed)
            {
                try
                {
                    using var proc = Process.GetCurrentProcess();
                    EmptyWorkingSet(proc.Handle);
                }
                catch (Exception ex)
                {
                    _emptyWorkingSetFailed = true;
                    _log?.LogDebug($"[RAMCleaner] EmptyWorkingSet not available ({ex.Message}), trying Linux malloc_trim...");
                }
            }

            // 2. Try Linux / glibc malloc_trim if enabled
            if (OptimizerConfig.EnableLinuxMallocTrim.Value && !_mallocTrimFailed)
            {
                TrimLinuxGlibcHeap();
            }

            long afterMB = GetProcessWorkingSetMB();
            long freedMB = beforeMB - afterMB;
            if (freedMB > 0)
            {
                _log?.LogInfo($"[RAMCleaner] Trimmed memory ({callerReason}): {beforeMB} MB -> {afterMB} MB (Freed {freedMB} MB RAM)");
            }
            else
            {
                _log?.LogDebug($"[RAMCleaner] Memory trim executed ({callerReason}): Current WorkingSet = {afterMB} MB");
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[RAMCleaner] Failed to trim working set: {ex.Message}");
        }
    }

    private static void TrimLinuxGlibcHeap()
    {
        try
        {
            malloc_trim_libc(IntPtr.Zero);
        }
        catch
        {
            try
            {
                malloc_trim_msvcrt(IntPtr.Zero);
            }
            catch
            {
                _mallocTrimFailed = true;
                _log?.LogDebug("[RAMCleaner] malloc_trim not supported in current environment.");
            }
        }
    }
}