using System;
using System.Diagnostics;
using BepInEx.Logging;
using SPTOptimizer.Config;

namespace SPTOptimizer.Utils;

/// <summary>
/// Adjusts the operating system process priority of Escape from Tarkov.
/// </summary>
public static class ProcessOptimizer
{
    private static ManualLogSource? _log;

    public static void Initialize(ManualLogSource log)
    {
        _log = log;

        if (!OptimizerConfig.EnableProcessPriority.Value)
            return;

        ApplyPriority();

        OptimizerConfig.ProcessPriorityLevel.SettingChanged += (_, _) => ApplyPriority();
    }

    private static void ApplyPriority()
    {
        if (!OptimizerConfig.EnableProcessPriority.Value)
            return;

        try
        {
            using var proc = Process.GetCurrentProcess();
            var levelStr = OptimizerConfig.ProcessPriorityLevel.Value;

            ProcessPriorityClass targetClass = levelStr switch
            {
                "High" => ProcessPriorityClass.High,
                "AboveNormal" => ProcessPriorityClass.AboveNormal,
                _ => ProcessPriorityClass.Normal
            };

            proc.PriorityClass = targetClass;
            _log?.LogInfo($"[ProcessOptimizer] Set Tarkov process priority to {targetClass}.");
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[ProcessOptimizer] Could not set process priority: {ex.Message}");
        }
    }
}
