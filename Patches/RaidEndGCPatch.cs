using System;
using System.Collections;
using BepInEx.Logging;
using EFT;
using HarmonyLib;
using SPTOptimizer.Config;
using SPTOptimizer.Utils;
using UnityEngine;

namespace SPTOptimizer.Patches;

/// <summary>
/// Triggers a deep garbage collection cycle, unloads all orphaned AssetBundles,
/// textures and audio clips, and reclaims OS working set memory when exiting a raid.
/// </summary>
public static class RaidEndGCPatch
{
    private static ManualLogSource? _log;
    private static MonoBehaviour? _runner;
    private static bool _raidEndCleanupInProgress;

    public static void Apply(ManualLogSource log, Harmony harmony, MonoBehaviour runner)
    {
        _log = log;
        _runner = runner;

        try
        {
            var abstractGameType = typeof(AbstractGame);
            var stopMethod = AccessTools.Method(abstractGameType, "Stop");
            if (stopMethod != null)
            {
                var postfix = new HarmonyMethod(typeof(RaidEndGCPatch), nameof(OnAbstractGameStopPostfix));
                harmony.Patch(stopMethod, postfix: postfix);
                _log.LogInfo("[RaidEndGC] Successfully patched AbstractGame.Stop");
            }
            else
            {
                _log.LogWarning("[RaidEndGC] AbstractGame.Stop method not found, trying GameWorld.OnDestroy fallback.");
                var gameWorldType = typeof(GameWorld);
                var onDestroyMethod = AccessTools.Method(gameWorldType, "OnDestroy");
                if (onDestroyMethod != null)
                {
                    var postfix = new HarmonyMethod(typeof(RaidEndGCPatch), nameof(OnAbstractGameStopPostfix));
                    harmony.Patch(onDestroyMethod, postfix: postfix);
                    _log.LogInfo("[RaidEndGC] Successfully patched GameWorld.OnDestroy");
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning($"[RaidEndGC] Failed to patch raid end: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnAbstractGameStopPostfix()
    {
        if (!OptimizerConfig.EnableRaidEndGC.Value || _raidEndCleanupInProgress)
            return;

        _runner?.StartCoroutine(RaidEndCleanupRoutine());
    }

    private static IEnumerator RaidEndCleanupRoutine()
    {
        _raidEndCleanupInProgress = true;

        // Wait 2 frames to let the game world tear down properly
        yield return null;
        yield return null;

        _log?.LogInfo("[RaidEndGC] Raid finished. Starting deep post-raid memory cleanup...");

        // 1. Initial managed GC
        PeriodicGCComponent.RunGC("raid-end-pre-unload");

        // 2. Unload all unreferenced native textures, meshes and audio clips from RAM/VRAM
        if (OptimizerConfig.UnloadUnusedAssetsOnRaidEnd.Value)
        {
            var op = Resources.UnloadUnusedAssets();
            yield return op;
            _log?.LogInfo("[RaidEndGC] UnloadUnusedAssets completed.");
        }

        // 3. Final GC compaction
        PeriodicGCComponent.RunGC("raid-end-post-unload");

        // 4. Force OS kernel to reclaim unused physical RAM pages
        MemoryTrimmer.TrimWorkingSet("raid-end", forceInRaid: true);

        _log?.LogInfo("[RaidEndGC] Post-raid deep memory cleanup completed.");
        _raidEndCleanupInProgress = false;
    }
}
