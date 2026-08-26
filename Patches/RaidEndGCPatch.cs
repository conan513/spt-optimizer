using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPTOptimizer.Config;
using SPTOptimizer.Utils;
using UnityEngine;
using UnityEngine.Scripting;

namespace SPTOptimizer.Patches;

/// <summary>
/// Patches the local game cleanup method to trigger a full GC + asset unload
/// when the player exits a raid. This is the primary memory recovery mechanism.
/// </summary>
public static class RaidEndGCPatch
{
    private static ManualLogSource? _log;
    private static HarmonyLib.Harmony? _harmony;
    private static MonoBehaviour? _runner; // coroutine runner

    public static void Apply(ManualLogSource log, HarmonyLib.Harmony harmony, MonoBehaviour runner)
    {
        _log     = log;
        _harmony = harmony;
        _runner  = runner;

        // Find the LocalGame type via the EFT types
        var localGameType = typeof(AbstractGame).Assembly
            .GetType("EFT.LocalGame");

        if (localGameType == null)
        {
            _log.LogWarning("[RaidEndGC] Could not find EFT.LocalGame type. Patch skipped.");
            return;
        }

        // The cleanup/dispose method is called when the game session ends
        // "Dispose" or "Stop" on LocalGame triggers the cleanup
        var stopMethod = AccessTools.Method(localGameType, "Stop");
        if (stopMethod == null)
        {
            _log.LogWarning("[RaidEndGC] Could not find LocalGame.Stop method. Trying 'Dispose'...");
            stopMethod = AccessTools.Method(localGameType, "Dispose");
        }

        if (stopMethod == null)
        {
            _log.LogWarning("[RaidEndGC] Could not find LocalGame Stop/Dispose. Patch skipped.");
            return;
        }

        var postfix = new HarmonyMethod(typeof(RaidEndGCPatch), nameof(OnRaidEnd));
        harmony.Patch(stopMethod, postfix: postfix);
        _log.LogInfo($"[RaidEndGC] Patched {localGameType.Name}.{stopMethod.Name}");
    }

    [HarmonyPostfix]
    private static void OnRaidEnd()
    {
        if (!OptimizerConfig.EnableRaidEndGC.Value)
            return;

        _log?.LogInfo("[RaidEndGC] Raid ended – triggering memory cleanup...");
        _runner?.StartCoroutine(RaidEndCleanup());
    }

    private static IEnumerator RaidEndCleanup()
    {
        // Allow the game to settle for 2 frames before collecting
        yield return null;
        yield return null;

        PeriodicGCComponent.RunGC("raid-end");

        if (OptimizerConfig.UnloadUnusedAssetsOnRaidEnd.Value)
        {
            _log?.LogInfo("[RaidEndGC] Starting Resources.UnloadUnusedAssets()...");
            var op = Resources.UnloadUnusedAssets();
            yield return op;
            _log?.LogInfo("[RaidEndGC] UnloadUnusedAssets complete.");
        }

        // Second GC pass after asset unload finalizes freed objects
        PeriodicGCComponent.RunGC("raid-end-post-unload");

        // Trim working set to release physical memory back to Windows
        MemoryTrimmer.TrimWorkingSet("raid-end", forceInRaid: true);

        _log?.LogInfo("[RaidEndGC] Memory cleanup complete.");
    }
}
