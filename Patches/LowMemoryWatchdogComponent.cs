using System;
using System.Collections;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using SPTOptimizer.Config;
using SPTOptimizer.Utils;
using UnityEngine;

namespace SPTOptimizer.Patches;

/// <summary>
/// Monitors process memory (real working set RAM or managed heap) and triggers
/// emergency GC when usage exceeds the configured threshold.
/// </summary>
public class LowMemoryWatchdogComponent : MonoBehaviour
{
    private static ManualLogSource? _log;
    private bool _triggerInProgress;

    public static void Initialize(ManualLogSource log)
    {
        _log = log;
    }

    public static bool IsInRaid()
    {
        try
        {
            return Singleton<AbstractGame>.Instantiated && Singleton<AbstractGame>.Instance != null;
        }
        catch
        {
            return false;
        }
    }

    private void Start()
    {
        StartCoroutine(WatchdogRoutine());
    }

    private IEnumerator WatchdogRoutine()
    {
        _log?.LogInfo("[LowMemoryWatchdog] Started.");

        while (true)
        {
            yield return new WaitForSeconds(OptimizerConfig.WatchdogIntervalSeconds.Value);

            if (!OptimizerConfig.EnableLowMemoryWatchdog.Value || _triggerInProgress)
                continue;

            try
            {
                CheckMemory();
            }
            catch (Exception ex)
            {
                _log?.LogError($"[LowMemoryWatchdog] Exception: {ex.Message}");
            }
        }
    }

    private void CheckMemory()
    {
        long usedMB;
        if (OptimizerConfig.WatchdogUsePhysicalMemory.Value)
        {
            usedMB = MemoryTrimmer.GetProcessWorkingSetMB();
        }
        else
        {
            usedMB = GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);
        }

        if (usedMB >= OptimizerConfig.MemoryThresholdMB.Value)
        {
            _log?.LogWarning($"[LowMemoryWatchdog] High memory: {usedMB} MB >= threshold {OptimizerConfig.MemoryThresholdMB.Value} MB. Triggering emergency GC...");
            StartCoroutine(EmergencyGCRoutine());
        }
    }

    private IEnumerator EmergencyGCRoutine()
    {
        _triggerInProgress = true;

        PeriodicGCComponent.RunGC("emergency-watchdog");

        // Outside of raid, do a full asset unload and memory trim
        if (!IsInRaid())
        {
            yield return null;
            var op = Resources.UnloadUnusedAssets();
            yield return op;
            PeriodicGCComponent.RunGC("emergency-post-unload");
            MemoryTrimmer.TrimWorkingSet("emergency-watchdog", forceInRaid: true);
            _log?.LogInfo("[LowMemoryWatchdog] Full emergency memory cleanup completed.");
        }
        else
        {
            // In-raid: just incremental GC and safe malloc trim
            MemoryTrimmer.TrimWorkingSet("emergency-watchdog-inraid", forceInRaid: false);
            _log?.LogInfo("[LowMemoryWatchdog] In-raid lightweight emergency GC completed.");
        }

        _triggerInProgress = false;
    }
}