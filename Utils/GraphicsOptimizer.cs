using System;
using BepInEx.Logging;
using SPTOptimizer.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SPTOptimizer.Utils;

/// <summary>
/// Controls Unity QualitySettings and graphics engine parameters,
/// including texture mipmap streaming, master texture limit, shadow parameters,
/// LOD bias, and async texture upload buffering.
/// </summary>
public static class GraphicsOptimizer
{
    private static ManualLogSource? _log;
    private static bool _initialized;

    public static void Initialize(ManualLogSource log)
    {
        _log = log;

        if (_initialized)
            return;

        _initialized = true;

        // Apply settings immediately
        ApplyAllSettings("initial-startup");

        // Re-apply whenever a new scene/raid loads because EFT resets QualitySettings on scene transitions
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Bind live setting changed listeners (F12 Configuration Manager)
        HookSettingListeners();

        _log.LogInfo("[GraphicsOptimizer] Initialized graphics and texture streaming optimizer.");
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyAllSettings($"scene-loaded:{scene.name}");
    }

    private static void HookSettingListeners()
    {
        OptimizerConfig.MasterTextureLimit.SettingChanged += (_, _) => ApplyTextureSettings("config-change");
        OptimizerConfig.EnableTextureMipStreaming.SettingChanged += (_, _) => ApplyTextureSettings("config-change");
        OptimizerConfig.TextureStreamingBudgetMB.SettingChanged += (_, _) => ApplyTextureSettings("config-change");
        OptimizerConfig.StreamingMaxLevelReduction.SettingChanged += (_, _) => ApplyTextureSettings("config-change");
        OptimizerConfig.AsyncUploadBufferSizeMB.SettingChanged += (_, _) => ApplyTextureSettings("config-change");
        OptimizerConfig.AsyncUploadTimeSliceMs.SettingChanged += (_, _) => ApplyTextureSettings("config-change");
        OptimizerConfig.AsyncUploadPersistentBuffer.SettingChanged += (_, _) => ApplyTextureSettings("config-change");

        OptimizerConfig.EnableShadowOptimizations.SettingChanged += (_, _) => ApplyShadowSettings("config-change");
        OptimizerConfig.ShadowDistance.SettingChanged += (_, _) => ApplyShadowSettings("config-change");
        OptimizerConfig.ShadowCascadesCount.SettingChanged += (_, _) => ApplyShadowSettings("config-change");
        OptimizerConfig.PixelLightCount.SettingChanged += (_, _) => ApplyShadowSettings("config-change");
        OptimizerConfig.RealtimeReflectionProbes.SettingChanged += (_, _) => ApplyShadowSettings("config-change");

        OptimizerConfig.EnableGeometryOptimizations.SettingChanged += (_, _) => ApplyGeometrySettings("config-change");
        OptimizerConfig.LodBias.SettingChanged += (_, _) => ApplyGeometrySettings("config-change");
        OptimizerConfig.MaximumLODLevel.SettingChanged += (_, _) => ApplyGeometrySettings("config-change");
        OptimizerConfig.SoftParticles.SettingChanged += (_, _) => ApplyGeometrySettings("config-change");
        OptimizerConfig.SoftVegetation.SettingChanged += (_, _) => ApplyGeometrySettings("config-change");

        OptimizerConfig.MaxQueuedFrames.SettingChanged += (_, _) => ApplyRenderSettings("config-change");
        OptimizerConfig.VSyncMode.SettingChanged += (_, _) => ApplyRenderSettings("config-change");
    }

    public static void ApplyAllSettings(string reason)
    {
        try
        {
            ApplyTextureSettings(reason);
            ApplyShadowSettings(reason);
            ApplyGeometrySettings(reason);
            ApplyRenderSettings(reason);
            _log?.LogDebug($"[GraphicsOptimizer] Applied all graphics settings ({reason}).");
        }
        catch (Exception ex)
        {
            _log?.LogError($"[GraphicsOptimizer] Error applying graphics settings: {ex.Message}");
        }
    }

    public static void ApplyTextureSettings(string reason)
    {
        try
        {
            // Global master texture mipmap limit (0 = Full, 1 = Half, 2 = Quarter, 3 = Eighth)
            QualitySettings.masterTextureLimit = Mathf.Clamp(OptimizerConfig.MasterTextureLimit.Value, 0, 3);

            // Texture Mipmap Streaming
            QualitySettings.streamingMipmapsActive = OptimizerConfig.EnableTextureMipStreaming.Value;
            if (OptimizerConfig.EnableTextureMipStreaming.Value)
            {
                QualitySettings.streamingMipmapsMemoryBudget = OptimizerConfig.TextureStreamingBudgetMB.Value;
                QualitySettings.streamingMipmapsMaxLevelReduction = OptimizerConfig.StreamingMaxLevelReduction.Value;
                QualitySettings.streamingMipmapsMaxFileIORequests = 1024;
                QualitySettings.streamingMipmapsAddAllCameras = true;
            }

            // Async Upload Buffer
            QualitySettings.asyncUploadBufferSize = OptimizerConfig.AsyncUploadBufferSizeMB.Value;
            QualitySettings.asyncUploadTimeSlice = OptimizerConfig.AsyncUploadTimeSliceMs.Value;
            QualitySettings.asyncUploadPersistentBuffer = OptimizerConfig.AsyncUploadPersistentBuffer.Value;

            _log?.LogDebug($"[GraphicsOptimizer] Texture settings applied ({reason}): MasterLimit={QualitySettings.masterTextureLimit}, Streaming={QualitySettings.streamingMipmapsActive} (Budget: {QualitySettings.streamingMipmapsMemoryBudget}MB, MaxReduction: {QualitySettings.streamingMipmapsMaxLevelReduction})");
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[GraphicsOptimizer] Failed to apply texture settings: {ex.Message}");
        }
    }

    public static void ApplyShadowSettings(string reason)
    {
        if (!OptimizerConfig.EnableShadowOptimizations.Value)
            return;

        try
        {
            QualitySettings.shadowDistance = OptimizerConfig.ShadowDistance.Value;
            QualitySettings.shadowCascades = OptimizerConfig.ShadowCascadesCount.Value;
            QualitySettings.pixelLightCount = OptimizerConfig.PixelLightCount.Value;
            QualitySettings.realtimeReflectionProbes = OptimizerConfig.RealtimeReflectionProbes.Value;

            if (Enum.TryParse<ShadowResolution>(OptimizerConfig.ShadowResolutionQuality.Value, true, out var shadowRes))
            {
                QualitySettings.shadowResolution = shadowRes;
            }

            if (Enum.TryParse<ShadowQuality>(OptimizerConfig.ShadowQualityMode.Value, true, out var shadowQuality))
            {
                QualitySettings.shadows = shadowQuality;
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[GraphicsOptimizer] Failed to apply shadow settings: {ex.Message}");
        }
    }

    public static void ApplyGeometrySettings(string reason)
    {
        if (!OptimizerConfig.EnableGeometryOptimizations.Value)
            return;

        try
        {
            QualitySettings.lodBias = OptimizerConfig.LodBias.Value;
            QualitySettings.maximumLODLevel = OptimizerConfig.MaximumLODLevel.Value;
            QualitySettings.softParticles = OptimizerConfig.SoftParticles.Value;
            QualitySettings.softVegetation = OptimizerConfig.SoftVegetation.Value;

            if (Enum.TryParse<SkinWeights>(OptimizerConfig.SkinWeightsMode.Value, true, out var skinWeights))
            {
                QualitySettings.skinWeights = skinWeights;
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[GraphicsOptimizer] Failed to apply geometry settings: {ex.Message}");
        }
    }

    public static void ApplyRenderSettings(string reason)
    {
        try
        {
            QualitySettings.maxQueuedFrames = OptimizerConfig.MaxQueuedFrames.Value;
            QualitySettings.vSyncCount = OptimizerConfig.VSyncMode.Value;

            if (OptimizerConfig.TargetFrameRate.Value > 0)
            {
                Application.targetFrameRate = OptimizerConfig.TargetFrameRate.Value;
            }

            if (Enum.TryParse<AnisotropicFiltering>(OptimizerConfig.AnisotropicFilteringMode.Value, true, out var aniso))
            {
                QualitySettings.anisotropicFiltering = aniso;
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[GraphicsOptimizer] Failed to apply render settings: {ex.Message}");
        }
    }
}
