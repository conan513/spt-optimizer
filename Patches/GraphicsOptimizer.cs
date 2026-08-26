using System;
using BepInEx.Logging;
using SPTOptimizer.Config;
using UnityEngine;

namespace SPTOptimizer.Patches;

/// <summary>
/// Manages graphics settings, master texture limits, texture mipmap streaming,
/// shadow optimization, and LOD/geometry quality parameters to maximize FPS
/// and minimize VRAM and RAM footprint.
/// </summary>
public static class GraphicsOptimizer
{
    private static ManualLogSource? _log;

    public static void Initialize(ManualLogSource log)
    {
        _log = log;

        // Apply immediately at startup
        ApplySettings();

        // Hook config changes so adjustments in F12 menu take effect in real time
        // ── Texture & Streaming
        OptimizerConfig.MasterTextureLimit.SettingChanged += (_, _) => ApplyTextureStreaming();
        OptimizerConfig.EnableTextureMipStreaming.SettingChanged += (_, _) => ApplyTextureStreaming();
        OptimizerConfig.TextureStreamingBudgetMB.SettingChanged += (_, _) => ApplyTextureStreaming();
        OptimizerConfig.StreamingMaxLevelReduction.SettingChanged += (_, _) => ApplyTextureStreaming();
        OptimizerConfig.AsyncUploadBufferSizeMB.SettingChanged += (_, _) => ApplyTextureStreaming();
        OptimizerConfig.AsyncUploadTimeSliceMs.SettingChanged += (_, _) => ApplyTextureStreaming();
        OptimizerConfig.AsyncUploadPersistentBuffer.SettingChanged += (_, _) => ApplyTextureStreaming();

        // ── Shadows & Lighting
        OptimizerConfig.EnableShadowOptimizations.SettingChanged += (_, _) => ApplyShadowAndLighting();
        OptimizerConfig.ShadowDistance.SettingChanged += (_, _) => ApplyShadowAndLighting();
        OptimizerConfig.ShadowResolutionQuality.SettingChanged += (_, _) => ApplyShadowAndLighting();
        OptimizerConfig.ShadowCascadesCount.SettingChanged += (_, _) => ApplyShadowAndLighting();
        OptimizerConfig.ShadowQualityMode.SettingChanged += (_, _) => ApplyShadowAndLighting();
        OptimizerConfig.PixelLightCount.SettingChanged += (_, _) => ApplyShadowAndLighting();
        OptimizerConfig.RealtimeReflectionProbes.SettingChanged += (_, _) => ApplyShadowAndLighting();

        // ── Geometry & Detail
        OptimizerConfig.EnableGeometryOptimizations.SettingChanged += (_, _) => ApplyGeometryAndDetail();
        OptimizerConfig.LodBias.SettingChanged += (_, _) => ApplyGeometryAndDetail();
        OptimizerConfig.MaximumLODLevel.SettingChanged += (_, _) => ApplyGeometryAndDetail();
        OptimizerConfig.SkinWeightsMode.SettingChanged += (_, _) => ApplyGeometryAndDetail();
        OptimizerConfig.ParticleRaycastBudget.SettingChanged += (_, _) => ApplyGeometryAndDetail();
        OptimizerConfig.SoftParticles.SettingChanged += (_, _) => ApplyGeometryAndDetail();
        OptimizerConfig.SoftVegetation.SettingChanged += (_, _) => ApplyGeometryAndDetail();

        // ── Render & Performance
        OptimizerConfig.MaxQueuedFrames.SettingChanged += (_, _) => ApplyRenderAndPerformance();
        OptimizerConfig.AnisotropicFilteringMode.SettingChanged += (_, _) => ApplyRenderAndPerformance();
        OptimizerConfig.TargetFrameRate.SettingChanged += (_, _) => ApplyRenderAndPerformance();
        OptimizerConfig.VSyncMode.SettingChanged += (_, _) => ApplyRenderAndPerformance();
    }

    public static void ApplySettings()
    {
        try
        {
            ApplyTextureStreaming();
            ApplyShadowAndLighting();
            ApplyGeometryAndDetail();
            ApplyRenderAndPerformance();
            _log?.LogInfo("[GraphicsOptimizer] All graphics optimization settings successfully applied.");
        }
        catch (Exception ex)
        {
            _log?.LogError($"[GraphicsOptimizer] Failed to apply graphics settings: {ex.Message}");
        }
    }

    private static void ApplyTextureStreaming()
    {
        try
        {
            // Apply Master Texture Limit (0=Full res, 1=Half, 2=Quarter, 3=Eighth)
            QualitySettings.globalTextureMipmapLimit = OptimizerConfig.MasterTextureLimit.Value;

            if (OptimizerConfig.EnableTextureMipStreaming.Value)
            {
                QualitySettings.streamingMipmapsActive = true;
                QualitySettings.streamingMipmapsMemoryBudget = OptimizerConfig.TextureStreamingBudgetMB.Value;
                QualitySettings.streamingMipmapsAddAllCameras = true;
                QualitySettings.streamingMipmapsMaxLevelReduction = OptimizerConfig.StreamingMaxLevelReduction.Value;
                QualitySettings.streamingMipmapsMaxFileIORequests = 1024;

                // Async texture upload parameters
                QualitySettings.asyncUploadBufferSize = OptimizerConfig.AsyncUploadBufferSizeMB.Value;
                QualitySettings.asyncUploadTimeSlice = OptimizerConfig.AsyncUploadTimeSliceMs.Value;
                QualitySettings.asyncUploadPersistentBuffer = OptimizerConfig.AsyncUploadPersistentBuffer.Value;

                _log?.LogDebug($"[GraphicsOptimizer] Texture Streaming ENABLED (Budget: {OptimizerConfig.TextureStreamingBudgetMB.Value}MB, MasterMipLimit: {OptimizerConfig.MasterTextureLimit.Value}, MaxLevelReduction: {OptimizerConfig.StreamingMaxLevelReduction.Value}, AsyncBuffer: {OptimizerConfig.AsyncUploadBufferSizeMB.Value}MB)");
            }
            else
            {
                QualitySettings.streamingMipmapsActive = false;
                QualitySettings.asyncUploadBufferSize = OptimizerConfig.AsyncUploadBufferSizeMB.Value;
                QualitySettings.asyncUploadTimeSlice = OptimizerConfig.AsyncUploadTimeSliceMs.Value;
                QualitySettings.asyncUploadPersistentBuffer = OptimizerConfig.AsyncUploadPersistentBuffer.Value;

                _log?.LogDebug($"[GraphicsOptimizer] Texture Streaming DISABLED (MasterMipLimit: {OptimizerConfig.MasterTextureLimit.Value})");
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[GraphicsOptimizer] Error applying texture streaming settings: {ex.Message}");
        }
    }

    private static void ApplyShadowAndLighting()
    {
        if (!OptimizerConfig.EnableShadowOptimizations.Value)
            return;

        try
        {
            QualitySettings.shadowDistance = OptimizerConfig.ShadowDistance.Value;
            QualitySettings.pixelLightCount = OptimizerConfig.PixelLightCount.Value;
            QualitySettings.shadowCascades = OptimizerConfig.ShadowCascadesCount.Value;
            QualitySettings.realtimeReflectionProbes = OptimizerConfig.RealtimeReflectionProbes.Value;

            if (Enum.TryParse<ShadowResolution>(OptimizerConfig.ShadowResolutionQuality.Value, true, out var shadowRes))
            {
                QualitySettings.shadowResolution = shadowRes;
            }

            if (Enum.TryParse<ShadowQuality>(OptimizerConfig.ShadowQualityMode.Value, true, out var shadowQual))
            {
                QualitySettings.shadows = shadowQual;
            }

            _log?.LogDebug($"[GraphicsOptimizer] Shadows & Lighting applied: Dist={QualitySettings.shadowDistance}m, Res={QualitySettings.shadowResolution}, Cascades={QualitySettings.shadowCascades}, Shadows={QualitySettings.shadows}, PixelLights={QualitySettings.pixelLightCount}, ReflectionProbes={QualitySettings.realtimeReflectionProbes}");
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[GraphicsOptimizer] Error applying shadow & lighting settings: {ex.Message}");
        }
    }

    private static void ApplyGeometryAndDetail()
    {
        if (!OptimizerConfig.EnableGeometryOptimizations.Value)
            return;

        try
        {
            QualitySettings.lodBias = OptimizerConfig.LodBias.Value;
            QualitySettings.maximumLODLevel = OptimizerConfig.MaximumLODLevel.Value;
            QualitySettings.particleRaycastBudget = OptimizerConfig.ParticleRaycastBudget.Value;
            QualitySettings.softParticles = OptimizerConfig.SoftParticles.Value;
            QualitySettings.softVegetation = OptimizerConfig.SoftVegetation.Value;

            if (Enum.TryParse<SkinWeights>(OptimizerConfig.SkinWeightsMode.Value, true, out var skinWeights))
            {
                QualitySettings.skinWeights = skinWeights;
            }

            _log?.LogDebug($"[GraphicsOptimizer] Geometry & Detail applied: LODBias={QualitySettings.lodBias}, MaxLODLevel={QualitySettings.maximumLODLevel}, SkinWeights={QualitySettings.skinWeights}, SoftParticles={QualitySettings.softParticles}, SoftVeg={QualitySettings.softVegetation}, ParticleRaycasts={QualitySettings.particleRaycastBudget}");
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[GraphicsOptimizer] Error applying geometry & detail settings: {ex.Message}");
        }
    }

    private static void ApplyRenderAndPerformance()
    {
        try
        {
            QualitySettings.maxQueuedFrames = OptimizerConfig.MaxQueuedFrames.Value;
            QualitySettings.vSyncCount = OptimizerConfig.VSyncMode.Value;

            if (Enum.TryParse<AnisotropicFiltering>(OptimizerConfig.AnisotropicFilteringMode.Value, true, out var aniso))
            {
                QualitySettings.anisotropicFiltering = aniso;
            }

            if (OptimizerConfig.TargetFrameRate.Value > 0)
            {
                Application.targetFrameRate = OptimizerConfig.TargetFrameRate.Value;
            }
            else if (OptimizerConfig.TargetFrameRate.Value == -1)
            {
                Application.targetFrameRate = -1;
            }

            _log?.LogDebug($"[GraphicsOptimizer] Render & Performance applied: MaxQueuedFrames={QualitySettings.maxQueuedFrames}, Aniso={QualitySettings.anisotropicFiltering}, VSync={QualitySettings.vSyncCount}, TargetFPS={Application.targetFrameRate}");
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[GraphicsOptimizer] Error applying render & performance settings: {ex.Message}");
        }
    }
}
