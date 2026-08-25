using System;
using BepInEx.Logging;
using SPTOptimizer.Config;
using UnityEngine;

namespace SPTOptimizer.Patches;

/// <summary>
/// Manages graphics settings, texture mipmap streaming, and quality parameters to
/// minimize VRAM and system memory usage while maintaining high texture quality.
/// </summary>
public static class GraphicsOptimizer
{
    private static ManualLogSource? _log;

    public static void Initialize(ManualLogSource log)
    {
        _log = log;

        // Apply immediately
        ApplySettings();

        // Hook config changes so adjustments in F12 menu take effect in real time
        OptimizerConfig.EnableTextureMipStreaming.SettingChanged += (_, _) => ApplySettings();
        OptimizerConfig.TextureStreamingBudgetMB.SettingChanged += (_, _) => ApplySettings();
        OptimizerConfig.MasterTextureLimit.SettingChanged += (_, _) => ApplySettings();
        OptimizerConfig.EnableQualityTweaks.SettingChanged += (_, _) => ApplySettings();
        OptimizerConfig.ShadowDistance.SettingChanged += (_, _) => ApplySettings();
        OptimizerConfig.PixelLightCount.SettingChanged += (_, _) => ApplySettings();
        OptimizerConfig.LodBias.SettingChanged += (_, _) => ApplySettings();
        OptimizerConfig.MaxQueuedFrames.SettingChanged += (_, _) => ApplySettings();
    }

    public static void ApplySettings()
    {
        try
        {
            ApplyTextureStreaming();
            ApplyQualityTweaks();
            _log?.LogInfo("[GraphicsOptimizer] Settings successfully applied.");
        }
        catch (Exception ex)
        {
            _log?.LogError($"[GraphicsOptimizer] Failed to apply settings: {ex.Message}");
        }
    }

    private static void ApplyTextureStreaming()
    {
        if (OptimizerConfig.EnableTextureMipStreaming.Value)
        {
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = OptimizerConfig.TextureStreamingBudgetMB.Value;
            QualitySettings.streamingMipmapsAddAllCameras = true;
            QualitySettings.streamingMipmapsMaxLevelReduction = 2;
            QualitySettings.streamingMipmapsMaxFileIORequests = 1024;
            QualitySettings.globalTextureMipmapLimit = OptimizerConfig.MasterTextureLimit.Value;

            // Increase async upload buffer size to avoid frame drops during streaming
            QualitySettings.asyncUploadBufferSize = 32; // 32MB buffer
            QualitySettings.asyncUploadTimeSlice = 4;   // 4ms per frame

            _log?.LogDebug($"[GraphicsOptimizer] Texture Streaming ENABLED (Budget: {OptimizerConfig.TextureStreamingBudgetMB.Value}MB, MipLimit: {OptimizerConfig.MasterTextureLimit.Value})");
        }
        else
        {
            QualitySettings.streamingMipmapsActive = false;
            QualitySettings.globalTextureMipmapLimit = OptimizerConfig.MasterTextureLimit.Value;
            _log?.LogDebug("[GraphicsOptimizer] Texture Streaming DISABLED");
        }
    }

    private static void ApplyQualityTweaks()
    {
        if (!OptimizerConfig.EnableQualityTweaks.Value)
            return;

        QualitySettings.shadowDistance = OptimizerConfig.ShadowDistance.Value;
        QualitySettings.pixelLightCount = OptimizerConfig.PixelLightCount.Value;
        QualitySettings.lodBias = OptimizerConfig.LodBias.Value;
        QualitySettings.maxQueuedFrames = OptimizerConfig.MaxQueuedFrames.Value;

        _log?.LogDebug($"[GraphicsOptimizer] Quality tweaks applied: Shadows={QualitySettings.shadowDistance}m, PixelLights={QualitySettings.pixelLightCount}, LODBias={QualitySettings.lodBias}, MaxQueuedFrames={QualitySettings.maxQueuedFrames}");
    }
}
