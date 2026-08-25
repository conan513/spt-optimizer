using System;
using System.Collections;
using BepInEx.Logging;
using SPTOptimizer.Config;
using UnityEngine;

namespace SPTOptimizer.Patches;

/// <summary>
/// Monitors process memory and triggers a GC when usage exceeds the configured threshold.
/// Runs as a coroutine on a persistent GameObject.
/// </summary>
public class LowMemoryWatchdogComponent : MonoBehaviour
{
    private static ManualLogSource? _log;
    private bool _triggerInProgress;

    public static void Initialize(ManualLogSource log)
    {
        _log = log;
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
        long usedMB = GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);

        if (usedMB >= OptimizerConfig.MemoryThresholdMB.Value)
        {
            _log?.LogWarning($"[LowMemoryWatchdog] High memory: {usedMB} MB >= threshold {OptimizerConfig.MemoryThresholdMB.Value} MB. Triggering GC...");
            StartCoroutine(EmergencyGCRoutine());
        }
    }

    private IEnumerator EmergencyGCRoutine()
    {
        _triggerInProgress = true;

        PeriodicGCComponent.RunGC("emergency-watchdog");

        // Wait a frame before unloading unused assets to avoid stutter
        yield return null;

        if (OptimizerConfig.UnloadUnusedAssetsOnRaidEnd.Value)
        {
            var op = Resources.UnloadUnusedAssets();
            yield return op;
            _log?.LogInfo("[LowMemoryWatchdog] UnloadUnusedAssets completed.");
        }

        _triggerInProgress = false;
    }
}
