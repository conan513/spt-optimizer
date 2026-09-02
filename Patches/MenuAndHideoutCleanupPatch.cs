using System;
using System.Collections;
using BepInEx.Logging;
using EFT.UI;
using HarmonyLib;
using SPTOptimizer.Config;
using SPTOptimizer.Utils;
using UnityEngine;

namespace SPTOptimizer.Patches;

/// <summary>
/// Unloads unused assets, textures, and 3D preview meshes when returning to the main menu
/// from the Hideout, Trader screens, Flea Market, or Weapon Modding screens.
/// Also provides safe incremental GC when opening the inventory in-raid.
/// </summary>
public static class MenuAndHideoutCleanupPatch
{
    private static ManualLogSource? _log;
    private static MonoBehaviour? _runner;
    private static bool _cleanupInProgress;
    private static float _lastInventoryGCTime;

    public static void Apply(ManualLogSource log, Harmony harmony, MonoBehaviour runner)
    {
        _log = log;
        _runner = runner;

        // 1. Patch MenuScreen.Show (called whenever returning to main menu)
        try
        {
            var menuScreenType = typeof(MenuScreen);
            var showMethod = AccessTools.Method(menuScreenType, "Show");
            if (showMethod != null)
            {
                var postfix = new HarmonyMethod(typeof(MenuAndHideoutCleanupPatch), nameof(OnMenuScreenShowPostfix));
                harmony.Patch(showMethod, postfix: postfix);
                _log.LogInfo("[MenuCleanup] Successfully patched MenuScreen.Show");
            }
            else
            {
                _log.LogWarning("[MenuCleanup] MenuScreen.Show method not found.");
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning($"[MenuCleanup] Failed to patch MenuScreen.Show: {ex.Message}");
        }

        // 2. Patch InventoryScreen.Show (safe in-raid GC when looting or checking backpack)
        try
        {
            var inventoryScreenType = typeof(InventoryScreen);
            var showMethod = AccessTools.Method(inventoryScreenType, "Show");
            if (showMethod != null)
            {
                var postfix = new HarmonyMethod(typeof(MenuAndHideoutCleanupPatch), nameof(OnInventoryScreenShowPostfix));
                harmony.Patch(showMethod, postfix: postfix);
                _log.LogInfo("[MenuCleanup] Successfully patched InventoryScreen.Show");
            }
            else
            {
                _log.LogWarning("[MenuCleanup] InventoryScreen.Show method not found.");
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning($"[MenuCleanup] Failed to patch InventoryScreen.Show: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnMenuScreenShowPostfix()
    {
        if (!OptimizerConfig.EnableMenuAndHideoutCleanup.Value || _cleanupInProgress)
            return;

        // Skip if in active raid
        if (LowMemoryWatchdogComponent.IsInRaid())
            return;

        _runner?.StartCoroutine(MenuCleanupRoutine());
    }

    private static IEnumerator MenuCleanupRoutine()
    {
        _cleanupInProgress = true;

        // Wait 1 frame for menu transition to complete
        yield return null;

        _log?.LogDebug("[MenuCleanup] Cleaning up unused menu, hideout and trader assets...");

        PeriodicGCComponent.RunGC("menu-enter");

        var op = Resources.UnloadUnusedAssets();
        yield return op;

        PeriodicGCComponent.RunGC("menu-post-unload");

        // Trim physical RAM in menu
        MemoryTrimmer.TrimWorkingSet("menu-return", forceInRaid: true);

        _log?.LogDebug("[MenuCleanup] Menu memory cleanup finished.");
        _cleanupInProgress = false;
    }

    [HarmonyPostfix]
    private static void OnInventoryScreenShowPostfix()
    {
        if (!OptimizerConfig.EnableInventoryOpenGC.Value)
            return;

        // Only run if in raid and rate-limit to once every 15 seconds
        if (!LowMemoryWatchdogComponent.IsInRaid())
            return;

        if (Time.realtimeSinceStartup - _lastInventoryGCTime < 15f)
            return;

        _lastInventoryGCTime = Time.realtimeSinceStartup;
        PeriodicGCComponent.RunGC("inventory-open-safe");
    }
}