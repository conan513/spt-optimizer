using System.Collections;
using BepInEx.Logging;
using EFT;
using HarmonyLib;
using SPTOptimizer.Config;
using UnityEngine;

namespace SPTOptimizer.Patches;

/// <summary>
/// When bots or players die in a raid, their corpses linger with active cloth simulation,
/// complex animators, and unculled skinned mesh renderers.
/// This patch waits for the ragdoll to settle, then disables heavy simulation components
/// to eliminate frame drops and memory bandwidth bottlenecks in long raids with many kills.
/// </summary>
public static class DeadBotOptimizerPatch
{
    private static ManualLogSource? _log;
    private static MonoBehaviour? _runner;

    public static void Apply(ManualLogSource log, Harmony harmony, MonoBehaviour runner)
    {
        _log = log;
        _runner = runner;

        try
        {
            var playerType = typeof(Player);
            var onDeadMethod = AccessTools.Method(playerType, "OnDead");
            if (onDeadMethod != null)
            {
                var postfix = new HarmonyMethod(typeof(DeadBotOptimizerPatch), nameof(OnPlayerDeadPostfix));
                harmony.Patch(onDeadMethod, postfix: postfix);
                _log.LogInfo("[DeadBotOptimizer] Successfully patched Player.OnDead");
            }
            else
            {
                _log.LogWarning("[DeadBotOptimizer] Player.OnDead method not found.");
            }
        }
        catch (System.Exception ex)
        {
            _log.LogWarning($"[DeadBotOptimizer] Failed to patch Player.OnDead: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnPlayerDeadPostfix(Player __instance)
    {
        if (!OptimizerConfig.EnableDeadBotOptimization.Value || __instance == null)
            return;

        // Skip local human player to avoid any UI or camera glitches
        if (__instance.IsYourPlayer)
            return;

        _runner?.StartCoroutine(OptimizeDeadCorpseRoutine(__instance));
    }

    private static IEnumerator OptimizeDeadCorpseRoutine(Player player)
    {
        // Allow 6 seconds for the ragdoll physics to naturally settle
        yield return new WaitForSeconds(6f);

        if (player == null || player.gameObject == null)
            yield break;

        try
        {
            // 1. Disable expensive Cloth simulations on clothing / gear
            var cloths = player.GetComponentsInChildren<Cloth>(true);
            foreach (var cloth in cloths)
            {
                if (cloth != null && cloth.enabled)
                {
                    cloth.enabled = false;
                }
            }

            // 2. Prevent SkinnedMeshRenderers from updating when offscreen
            var renderers = player.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in renderers)
            {
                if (smr != null)
                {
                    smr.updateWhenOffscreen = false;
                }
            }

            // 3. Optimize Animators
            var animators = player.GetComponentsInChildren<Animator>(true);
            foreach (var anim in animators)
            {
                if (anim != null && anim.enabled)
                {
                    anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                }
            }

            _log?.LogDebug($"[DeadBotOptimizer] Optimized dead corpse '{player.Profile?.Nickname}' (Cloths: {cloths.Length}, Meshes: {renderers.Length})");
        }
        catch (System.Exception ex)
        {
            _log?.LogDebug($"[DeadBotOptimizer] Error optimizing corpse: {ex.Message}");
        }
    }
}