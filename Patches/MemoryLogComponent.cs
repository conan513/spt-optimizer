using System;
using System.Collections;
using BepInEx.Logging;
using SPTOptimizer.Config;
using SPTOptimizer.Utils;
using UnityEngine;

namespace SPTOptimizer.Patches;

/// <summary>
/// Optional memory usage logger. Logs GC heap, physical process memory (WorkingSet)
/// and Unity profiler memory periodically to the BepInEx console.
/// </summary>
public class MemoryLogComponent : MonoBehaviour
{
    private static ManualLogSource? _log;

    public static void Initialize(ManualLogSource log)
    {
        _log = log;
    }

    private void Start()
    {
        StartCoroutine(LogRoutine());
    }

    private IEnumerator LogRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(OptimizerConfig.MemoryLogIntervalSeconds.Value);

            if (!OptimizerConfig.EnableMemoryLog.Value)
                continue;

            try
            {
                LogMemory();
            }
            catch (Exception ex)
            {
                _log?.LogError($"[MemoryLog] Exception: {ex.Message}");
            }
        }
    }

    private static void LogMemory()
    {
        long workingSetMB = MemoryTrimmer.GetProcessWorkingSetMB();
        long gcHeapMB   = GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);
        long monoUsedMB = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024 * 1024);
        long monoHeapMB = UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong() / (1024 * 1024);
        long totalAllocMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        long totalReservedMB = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);

        _log?.LogInfo($"[MemoryLog] Process RAM: {workingSetMB} MB | " +
                      $"GC Heap: {gcHeapMB} MB | " +
                      $"Mono: {monoUsedMB}/{monoHeapMB} MB | " +
                      $"Unity Alloc/Reserved: {totalAllocMB}/{totalReservedMB} MB");
    }
}