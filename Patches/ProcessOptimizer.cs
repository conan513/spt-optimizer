using System;
using System.Diagnostics;
using BepInEx.Logging;
using SPTOptimizer.Config;

namespace SPTOptimizer.Patches;

/// <summary>
/// Adjusts Windows process priority and scheduler settings for the game process.
/// Gives EFT higher execution priority over background applications to eliminate CPU frame dips.
/// </summary>
public static class ProcessOptimizer
{
    private static ManualLogSource? _log;

    public static void Initialize(ManualLogSource log)
    {
        _log = log;
        ApplyPriority();

        OptimizerConfig.EnableProcessPriority.SettingChanged += (_, _) => ApplyPriority();
        OptimizerConfig.ProcessPriorityLevel.SettingChanged += (_, _) => ApplyPriority();
    }

    public static void ApplyPriority()
    {
        if (!OptimizerConfig.EnableProcessPriority.Value)
            return;

        try
        {
            using var proc = Process.GetCurrentProcess();
            var targetPriority = OptimizerConfig.ProcessPriorityLevel.Value switch
            {
                "High" => ProcessPriorityClass.High,
                "Normal" => ProcessPriorityClass.Normal,
                _ => ProcessPriorityClass.AboveNormal
            };

            if (proc.PriorityClass != targetPriority)
            {
                proc.PriorityClass = targetPriority;
                _log?.LogInfo($"[ProcessOptimizer] Process priority set to: {targetPriority}");
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[ProcessOptimizer] Could not adjust process priority: {ex.Message}");
        }
    }
}
