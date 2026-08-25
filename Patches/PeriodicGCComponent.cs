using System;
using System.Collections;
using BepInEx.Logging;
using SPTOptimizer.Config;
using UnityEngine;
using UnityEngine.Scripting;

namespace SPTOptimizer.Patches;

/// <summary>
/// MonoBehaviour component that drives the periodic garbage collection coroutine.
/// Attached to a persistent GameObject created by the plugin.
/// </summary>
public class PeriodicGCComponent : MonoBehaviour
{
    private static ManualLogSource? _log;

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
        _log?.LogInfo("[PeriodicGC] Coroutine started.");

        while (true)
        {
            yield return new WaitForSeconds(OptimizerConfig.GCIntervalSeconds.Value);

            if (!OptimizerConfig.EnablePeriodicGC.Value)
                continue;

            try
            {
                RunGC("periodic");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[PeriodicGC] Exception during GC: {ex.Message}");
            }
        }
    }

    internal static void RunGC(string reason)
    {
        if (OptimizerConfig.UseIncrementalGC.Value)
        {
            // Enable incremental mode and run a time-sliced collection
            GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
            ulong budgetNs = (ulong)(OptimizerConfig.IncrementalGCBudgetMs.Value * 1_000_000L);
            GarbageCollector.CollectIncremental(budgetNs);
            _log?.LogDebug($"[GC] Incremental GC triggered ({reason}), budget: {OptimizerConfig.IncrementalGCBudgetMs.Value}ms");
        }
        else
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            _log?.LogDebug($"[GC] Full GC triggered ({reason})");
        }
    }
}
