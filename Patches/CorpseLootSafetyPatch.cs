using System;
using System.Reflection;
using BepInEx.Logging;
using EFT;
using EFT.Interactive;
using HarmonyLib;

namespace SPTOptimizer.Patches;

/// <summary>
/// Prevents NullReferenceExceptions from crashing bot inventory operations (such as Fika / MiyakoCarryService bot looting)
/// when weapons or items are removed from dead corpses whose bone transforms/ragdolls were settled, culled, or cleaned.
/// EFT's Corpse.RemoveLootItem -> Player.ReleaseHand -> Player+Garbage.RestoreShift can fail with
/// Transform+Enumerator.MoveNext NRE if child transforms were modified or detached.
/// </summary>
public static class CorpseLootSafetyPatch
{
    private static ManualLogSource? _log;

    public static void Apply(ManualLogSource log, Harmony harmony)
    {
        _log = log;

        // 1. Patch Corpse.RemoveLootItem with a Finalizer to suppress NREs during item removal
        try
        {
            var corpseType = typeof(Corpse);
            var removeLootItemMethod = AccessTools.Method(corpseType, "RemoveLootItem");
            if (removeLootItemMethod != null)
            {
                var finalizer = new HarmonyMethod(typeof(CorpseLootSafetyPatch), nameof(OnCorpseRemoveLootItemFinalizer));
                harmony.Patch(removeLootItemMethod, finalizer: finalizer);
                _log.LogInfo("[CorpseLootSafety] Successfully patched Corpse.RemoveLootItem");
            }
            else
            {
                _log.LogWarning("[CorpseLootSafety] Corpse.RemoveLootItem method not found.");
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning($"[CorpseLootSafety] Failed to patch Corpse.RemoveLootItem: {ex.Message}");
        }

        // 2. Patch Player.ReleaseHand with a Finalizer as an extra safeguard for culled/ragdoll corpses
        try
        {
            var playerType = typeof(Player);
            var releaseHandMethod = AccessTools.Method(playerType, "ReleaseHand");
            if (releaseHandMethod != null)
            {
                var finalizer = new HarmonyMethod(typeof(CorpseLootSafetyPatch), nameof(OnPlayerReleaseHandFinalizer));
                harmony.Patch(releaseHandMethod, finalizer: finalizer);
                _log.LogInfo("[CorpseLootSafety] Successfully patched Player.ReleaseHand");
            }
            else
            {
                _log.LogWarning("[CorpseLootSafety] Player.ReleaseHand method not found.");
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning($"[CorpseLootSafety] Failed to patch Player.ReleaseHand: {ex.Message}");
        }
    }

    [HarmonyFinalizer]
    private static Exception? OnCorpseRemoveLootItemFinalizer(Exception? __exception)
    {
        if (__exception != null)
        {
            _log?.LogDebug($"[CorpseLootSafety] Handled exception in Corpse.RemoveLootItem: {__exception.Message}");
            return null; // Suppress the exception so SafeRemoveItemEvent succeeds smoothly
        }
        return null;
    }

    [HarmonyFinalizer]
    private static Exception? OnPlayerReleaseHandFinalizer(Exception? __exception)
    {
        if (__exception != null)
        {
            _log?.LogDebug($"[CorpseLootSafety] Handled exception in Player.ReleaseHand: {__exception.Message}");
            return null; // Suppress the exception
        }
        return null;
    }
}
