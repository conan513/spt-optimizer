using BepInEx.Configuration;

namespace SPTOptimizer.Config;

/// <summary>
/// Centralized configuration entries for the SPT Optimizer plugin.
/// All settings are exposed via the BepInEx config system (F12 Configuration Manager / config file).
/// </summary>
public static class OptimizerConfig
{
    // ── Periodic GC ─────────────────────────────────────────────────────────────
    public static ConfigEntry<bool> EnablePeriodicGC = null!;
    public static ConfigEntry<int>  GCIntervalSeconds = null!;
    public static ConfigEntry<bool> UseIncrementalGC = null!;
    public static ConfigEntry<int>  IncrementalGCBudgetMs = null!;
    public static ConfigEntry<bool> EnableInventoryOpenGC = null!;

    // ── Low-memory watchdog ──────────────────────────────────────────────────────
    public static ConfigEntry<bool> EnableLowMemoryWatchdog = null!;
    public static ConfigEntry<int>  MemoryThresholdMB = null!;
    public static ConfigEntry<int>  WatchdogIntervalSeconds = null!;
    public static ConfigEntry<bool> WatchdogUsePhysicalMemory = null!;

    // ── Texture & Streaming ──────────────────────────────────────────────────────
    public static ConfigEntry<bool> EnableTextureMipStreaming = null!;
    public static ConfigEntry<int>  TextureStreamingBudgetMB = null!;
    /// <summary>
    /// Global texture mipmap resolution limit (Master Texture Limit):
    /// 0 = Full res (default), 1 = Half res (1/2), 2 = Quarter res (1/4), 3 = Eighth res (1/8).
    /// </summary>
    public static ConfigEntry<int>  MasterTextureLimit = null!;
    public static ConfigEntry<int>  StreamingMaxLevelReduction = null!;
    public static ConfigEntry<int>  AsyncUploadBufferSizeMB = null!;
    public static ConfigEntry<int>  AsyncUploadTimeSliceMs = null!;
    public static ConfigEntry<bool> AsyncUploadPersistentBuffer = null!;

    // ── Shadows & Lighting ───────────────────────────────────────────────────────
    public static ConfigEntry<bool>   EnableShadowOptimizations = null!;
    public static ConfigEntry<float>  ShadowDistance = null!;
    public static ConfigEntry<string> ShadowResolutionQuality = null!;
    public static ConfigEntry<int>    ShadowCascadesCount = null!;
    public static ConfigEntry<string> ShadowQualityMode = null!;
    public static ConfigEntry<int>    PixelLightCount = null!;
    public static ConfigEntry<bool>   RealtimeReflectionProbes = null!;

    // ── Geometry, LOD & Detail ───────────────────────────────────────────────────
    public static ConfigEntry<bool>   EnableGeometryOptimizations = null!;
    public static ConfigEntry<float>  LodBias = null!;
    public static ConfigEntry<int>    MaximumLODLevel = null!;
    public static ConfigEntry<string> SkinWeightsMode = null!;
    public static ConfigEntry<int>    ParticleRaycastBudget = null!;
    public static ConfigEntry<bool>   SoftParticles = null!;
    public static ConfigEntry<bool>   SoftVegetation = null!;

    // ── Render & Performance ─────────────────────────────────────────────────────
    public static ConfigEntry<int>    MaxQueuedFrames = null!;
    public static ConfigEntry<string> AnisotropicFilteringMode = null!;
    public static ConfigEntry<int>    TargetFrameRate = null!;
    public static ConfigEntry<int>    VSyncMode = null!;

    // ── Raid-end & Menu Cleanup ──────────────────────────────────────────────────
    public static ConfigEntry<bool> EnableRaidEndGC = null!;
    public static ConfigEntry<bool> UnloadUnusedAssetsOnRaidEnd = null!;
    public static ConfigEntry<bool> EnableMenuAndHideoutCleanup = null!;

    // ── Dead Bot Optimization ────────────────────────────────────────────────────
    public static ConfigEntry<bool> EnableDeadBotOptimization = null!;

    // ── Memory logging ───────────────────────────────────────────────────────────
    public static ConfigEntry<bool> EnableMemoryLog = null!;
    public static ConfigEntry<int>  MemoryLogIntervalSeconds = null!;

    // ── Process Priority ─────────────────────────────────────────────────────────
    public static ConfigEntry<bool>   EnableProcessPriority = null!;
    public static ConfigEntry<string> ProcessPriorityLevel = null!;

    // ── RAM Cleaner & Linux Malloc Trim ──────────────────────────────────────────
    public static ConfigEntry<bool> EnableActiveRAMCleaner = null!;
    public static ConfigEntry<bool> EnableLinuxMallocTrim = null!;

    public static void Initialize(ConfigFile cfg)
    {
        // ── 1. Periodic GC ─────────────────────────────────────────────────────────
        EnablePeriodicGC = cfg.Bind(
            "1 - Periodic GC", "Enable", true,
            "Enable periodic incremental garbage collection to keep memory usage low.");

        UseIncrementalGC = cfg.Bind(
            "1 - Periodic GC", "UseIncrementalGC", true,
            "Use Unity's incremental GC (lower frame stutters). If false, forces a full GC.Collect().");

        GCIntervalSeconds = cfg.Bind(
            "1 - Periodic GC", "IntervalSeconds", 60,
            new ConfigDescription("How often (seconds) to run the periodic GC.", new AcceptableValueRange<int>(10, 300)));

        IncrementalGCBudgetMs = cfg.Bind(
            "1 - Periodic GC", "IncrementalBudgetMs", 25,
            new ConfigDescription("Nanosecond budget for incremental GC (in ms, converted internally).", new AcceptableValueRange<int>(5, 100)));

        EnableInventoryOpenGC = cfg.Bind(
            "1 - Periodic GC", "EnableInventoryOpenGC", true,
            "Trigger a safe incremental GC cycle when opening the inventory or looting (avoids combat stutters).");

        // ── 2. Low-memory watchdog ─────────────────────────────────────────────────
        EnableLowMemoryWatchdog = cfg.Bind(
            "2 - Low Memory Watchdog", "Enable", true,
            "Monitor memory usage and trigger GC when it exceeds the threshold.");

        WatchdogUsePhysicalMemory = cfg.Bind(
            "2 - Low Memory Watchdog", "UsePhysicalMemory", true,
            "Monitor real total process memory (WorkingSet / RAM) instead of just the C# managed heap.");

        MemoryThresholdMB = cfg.Bind(
            "2 - Low Memory Watchdog", "ThresholdMB", 7168,
            new ConfigDescription("Memory usage (MB) that triggers an emergency GC. Increase if you have more RAM.", new AcceptableValueRange<int>(2048, 32768)));

        WatchdogIntervalSeconds = cfg.Bind(
            "2 - Low Memory Watchdog", "CheckIntervalSeconds", 15,
            new ConfigDescription("How often (seconds) the watchdog checks memory.", new AcceptableValueRange<int>(5, 60)));

        // ── 3. Texture & Streaming ─────────────────────────────────────────────────
        MasterTextureLimit = cfg.Bind(
            "3 - Texture & Streaming", "MasterTextureLimit", 0,
            new ConfigDescription("Master Texture Limit (Global mipmap limit): 0=Full resolution, 1=Half res (1/2), 2=Quarter res (1/4), 3=Eighth res (1/8). Setting to 1 or 2 drastically lowers VRAM usage on low/mid-tier GPUs.", new AcceptableValueRange<int>(0, 3)));

        EnableTextureMipStreaming = cfg.Bind(
            "3 - Texture & Streaming", "EnableMipStreaming", true,
            "Enable Unity texture mipmap streaming. Loads only required mip levels into VRAM based on camera distance.");

        TextureStreamingBudgetMB = cfg.Bind(
            "3 - Texture & Streaming", "StreamingBudgetMB", 1024,
            new ConfigDescription("VRAM budget for texture streaming (MB). Higher = better texture clarity, lower = less VRAM footprint.", new AcceptableValueRange<int>(128, 8192)));

        StreamingMaxLevelReduction = cfg.Bind(
            "3 - Texture & Streaming", "StreamingMaxLevelReduction", 2,
            new ConfigDescription("Maximum mip levels that texture streaming can discard when budget is exceeded (0-4).", new AcceptableValueRange<int>(0, 4)));

        AsyncUploadBufferSizeMB = cfg.Bind(
            "3 - Texture & Streaming", "AsyncUploadBufferSizeMB", 32,
            new ConfigDescription("Ring buffer size in MB for asynchronous texture upload. Higher values (32-64MB) prevent stutters when textures load from disk.", new AcceptableValueRange<int>(4, 256)));

        AsyncUploadTimeSliceMs = cfg.Bind(
            "3 - Texture & Streaming", "AsyncUploadTimeSliceMs", 4,
            new ConfigDescription("CPU time slice in milliseconds per frame dedicated to async texture uploading.", new AcceptableValueRange<int>(1, 33)));

        AsyncUploadPersistentBuffer = cfg.Bind(
            "3 - Texture & Streaming", "AsyncUploadPersistentBuffer", true,
            "Keep the async upload ring buffer persistent in memory to avoid frequent allocation and deallocation overhead.");

        // ── 4. Shadows & Lighting ──────────────────────────────────────────────────
        EnableShadowOptimizations = cfg.Bind(
            "4 - Shadows & Lighting", "Enable", true,
            "Apply shadow and lighting optimizations to reduce draw calls and GPU render passes.");

        ShadowDistance = cfg.Bind(
            "4 - Shadows & Lighting", "ShadowDistance", 80f,
            new ConfigDescription("Shadow render distance (meters). Game default is ~150-200m. Lower values (50-90m) provide huge FPS gains outdoors.", new AcceptableValueRange<float>(10f, 500f)));

        ShadowResolutionQuality = cfg.Bind(
            "4 - Shadows & Lighting", "ShadowResolution", "Medium",
            new ConfigDescription("Shadow map resolution quality.", new AcceptableValueList<string>("Default", "Low", "Medium", "High", "VeryHigh")));

        ShadowCascadesCount = cfg.Bind(
            "4 - Shadows & Lighting", "ShadowCascades", 2,
            new ConfigDescription("Directional shadow cascade count (0 = None, 2 = 2 Cascades, 4 = 4 Cascades). Lowering to 2 or 0 provides major CPU/GPU gains.", new AcceptableValueList<int>(0, 2, 4)));

        ShadowQualityMode = cfg.Bind(
            "4 - Shadows & Lighting", "ShadowQuality", "All",
            new ConfigDescription("Shadow filtering type (Disable = no shadows, HardOnly = sharp shadows, All = soft shadows).", new AcceptableValueList<string>("Default", "Disable", "HardOnly", "All")));

        PixelLightCount = cfg.Bind(
            "4 - Shadows & Lighting", "PixelLightCount", 2,
            new ConfigDescription("Maximum number of forward pixel lights affecting any single object (game default is 4).", new AcceptableValueRange<int>(0, 8)));

        RealtimeReflectionProbes = cfg.Bind(
            "4 - Shadows & Lighting", "RealtimeReflectionProbes", false,
            "Allow realtime reflection probes to update dynamically. Disabling saves substantial CPU draw calls and GPU rendering time.");

        // ── 5. Geometry, LOD & Detail ──────────────────────────────────────────────
        EnableGeometryOptimizations = cfg.Bind(
            "5 - Geometry & Detail", "Enable", true,
            "Apply geometry, level of detail (LOD), and particle performance optimizations.");

        LodBias = cfg.Bind(
            "5 - Geometry & Detail", "LodBias", 1.2f,
            new ConfigDescription("LOD distance bias multiplier. Lower values switch to lower polygon meshes earlier, boosting FPS on dense maps.", new AcceptableValueRange<float>(0.1f, 4.0f)));

        MaximumLODLevel = cfg.Bind(
            "5 - Geometry & Detail", "MaximumLODLevel", 0,
            new ConfigDescription("Maximum LOD level to use (0 = Full LOD0 detail, 1 = Skip LOD0 and use LOD1 as maximum, etc.). Set to 1 for high FPS gain on low-end systems.", new AcceptableValueRange<int>(0, 3)));

        SkinWeightsMode = cfg.Bind(
            "5 - Geometry & Detail", "SkinWeights", "TwoBones",
            new ConfigDescription("Number of bones affecting vertices in skeletal animation. TwoBones offers significant CPU/GPU performance improvement over FourBones.", new AcceptableValueList<string>("Default", "OneBone", "TwoBones", "FourBones", "Unlimited")));

        ParticleRaycastBudget = cfg.Bind(
            "5 - Geometry & Detail", "ParticleRaycastBudget", 256,
            new ConfigDescription("Maximum particle collision raycast calculations per frame.", new AcceptableValueRange<int>(16, 4096)));

        SoftParticles = cfg.Bind(
            "5 - Geometry & Detail", "SoftParticles", false,
            "Enable soft edge particle blending against scene geometry. Disabling improves FPS during smoke, explosions, and muzzle flashes.");

        SoftVegetation = cfg.Bind(
            "5 - Geometry & Detail", "SoftVegetation", false,
            "Enable soft edges for tree leaves and vegetation. Disabling yields a small performance uplift in dense forests.");

        // ── 6. Render & Performance ────────────────────────────────────────────────
        MaxQueuedFrames = cfg.Bind(
            "6 - Render & Performance", "MaxQueuedFrames", 2,
            new ConfigDescription("Max queued frames ahead in GPU driver (affects GPU latency and buffer memory). 2 is balanced.", new AcceptableValueRange<int>(0, 4)));

        AnisotropicFilteringMode = cfg.Bind(
            "6 - Render & Performance", "AnisotropicFiltering", "Enable",
            new ConfigDescription("Texture anisotropic filtering mode.", new AcceptableValueList<string>("Default", "Disable", "Enable", "ForceEnable")));

        TargetFrameRate = cfg.Bind(
            "6 - Render & Performance", "TargetFrameRate", -1,
            new ConfigDescription("Unity target framerate cap (-1 = Uncapped/Game default). Setting a consistent cap stabilizes 1% lows and frame pacing.", new AcceptableValueRange<int>(-1, 360)));

        VSyncMode = cfg.Bind(
            "6 - Render & Performance", "VSyncCount", 0,
            new ConfigDescription("Vertical sync count (0 = Off / Unlocked, 1 = Every VBlank / Monitor refresh rate, 2 = Every second VBlank).", new AcceptableValueList<int>(0, 1, 2)));

        // ── 7. Raid-end & Menu GC ──────────────────────────────────────────────────
        EnableRaidEndGC = cfg.Bind(
            "7 - Raid End & Menu GC", "Enable", true,
            "Trigger a full GC when exiting a raid to free all orphaned memory.");

        UnloadUnusedAssetsOnRaidEnd = cfg.Bind(
            "7 - Raid End & Menu GC", "UnloadUnusedAssets", true,
            "Call Resources.UnloadUnusedAssets() after raid. This is slow but frees a LOT of memory. Runs asynchronously.");

        EnableMenuAndHideoutCleanup = cfg.Bind(
            "7 - Raid End & Menu GC", "EnableMenuAndHideoutCleanup", true,
            "Unload unused 3D models and textures when leaving Trader screens or Hideout to prevent menu memory bloat.");

        // ── 8. Dead Bot Optimization ───────────────────────────────────────────────
        EnableDeadBotOptimization = cfg.Bind(
            "8 - Dead Bot Optimization", "Enable", true,
            "Disable cloth simulation and offscreen skeletal updates on dead bot corpses to save CPU and RAM in long raids.");

        // ── 9. Memory logging ──────────────────────────────────────────────────────
        EnableMemoryLog = cfg.Bind(
            "9 - Memory Log", "Enable", false,
            "Log current memory usage periodically to the BepInEx console/log.");

        MemoryLogIntervalSeconds = cfg.Bind(
            "9 - Memory Log", "IntervalSeconds", 30,
            new ConfigDescription("How often (seconds) to log memory usage.", new AcceptableValueRange<int>(5, 300)));

        // ── 10. Process Priority ───────────────────────────────────────────────────
        EnableProcessPriority = cfg.Bind(
            "10 - Process Priority", "Enable", true,
            "Automatically set Tarkov's process priority to give it CPU scheduling preference.");

        ProcessPriorityLevel = cfg.Bind(
            "10 - Process Priority", "PriorityLevel", "AboveNormal",
            new ConfigDescription("Process priority level.", new AcceptableValueList<string>("Normal", "AboveNormal", "High")));

        // ── 11. RAM Cleaner & Linux Malloc Trim ────────────────────────────────────
        EnableActiveRAMCleaner = cfg.Bind(
            "11 - RAM Cleaner", "Enable", true,
            "Empty working set memory to force OS to reclaim unused physical pages (safely outside of raid).");

        EnableLinuxMallocTrim = cfg.Bind(
            "11 - RAM Cleaner", "EnableLinuxMallocTrim", true,
            "Call glibc malloc_trim(0) under Proton/Linux/Wine to release unmapped heap memory back to the Linux kernel.");
    }
}