using System;
using System.Collections;
using BepInEx.Logging;
using SPTOptimizer.Config;
using SPTOptimizer.Utils;
using UnityEngine;
using UnityEngine.Scripting;

namespace SPTOptimizer.Patches;

/// <summary>
/// Handles periodic garbage collection cycles, incremental GC slicing,
/// and provides centralized GC trigger methods for other optimizer modules.
/// </summary>
public class PeriodicGCComponent : MonoBehaviour
{
    private static ManualLogSource? _log;
    private static float _lastGCTime;

    public static void Initialize(ManualLogSource log)
    {
        _log = log;
    }

    private void Start()
    {
        StartCoroutine(PeriodicGCRoutine());
    }

    private IEnumerator PeriodicGCRoutine()
    {
        _log?.LogInfo("[PeriodicGC] Started periodic GC routine.");

        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(5, OptimizerConfig.GCIntervalSeconds.Value));

            if (!OptimizerConfig.EnablePeriodicGC.Value)
                continue;

            try
            {
                RunGC("periodic-timer");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[PeriodicGC] Exception during routine: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Executes a managed garbage collection cycle safely.
    /// In raid, uses incremental GC budget if configured to avoid combat micro-stutters.
    /// Outside raid, performs a complete GC collection.
    /// </summary>
    public static void RunGC(string callerReason)
    {
        try
        {
            long beforeMemMB = GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);

            bool inRaid = LowMemoryWatchdogComponent.IsInRaid();

            if (inRaid && OptimizerConfig.UseIncrementalGC.Value && GarbageCollector.isIncremental)
            {
                // Convert ms budget to nanoseconds (1 ms = 1,000,000 ns)
                ulong budgetNs = (ulong)OptimizerConfig.IncrementalGCBudgetMs.Value * 1000000UL;
                GarbageCollector.CollectIncremental(budgetNs);
                _log?.LogDebug($"[PeriodicGC] Incremental GC ran ({callerReason}, budget: {OptimizerConfig.IncrementalGCBudgetMs.Value}ms)");
            }
            else
            {
                // Full generation collection
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: false, compacting: true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: false, compacting: true);

                long afterMemMB = GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);
                long freedMB = beforeMemMB - afterMemMB;

                if (freedMB > 0)
                {
                    _log?.LogDebug($"[PeriodicGC] Full GC ({callerReason}): {beforeMemMB} MB -> {afterMemMB} MB (Freed {freedMB} MB)");
                }
            }

            _lastGCTime = Time.realtimeSinceStartup;
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[PeriodicGC] Error during GC execution ({callerReason}): {ex.Message}");
        }
    }
}
