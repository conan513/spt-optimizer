using BepInEx.Configuration;

namespace SPTOptimizer.Config;

/// <summary>
/// Centralized configuration entries for the SPT Optimizer plugin.
/// All settings are exposed via the BepInEx config system (F1 menu / config file).
/// </summary>
public static class OptimizerConfig
{
    // ── Periodic GC ─────────────────────────────────────────────────────────────
    public static ConfigEntry<bool> EnablePeriodicGC = null!;
    public static ConfigEntry<int>  GCIntervalSeconds = null!;
    public static ConfigEntry<bool> UseIncrementalGC = null!;
    public static ConfigEntry<int>  IncrementalGCBudgetMs = null!;

    // ── Low-memory watchdog ──────────────────────────────────────────────────────
    public static ConfigEntry<bool> EnableLowMemoryWatchdog = null!;
    public static ConfigEntry<int>  MemoryThresholdMB = null!;
    public static ConfigEntry<int>  WatchdogIntervalSeconds = null!;

    // ── Texture streaming ────────────────────────────────────────────────────────
    public static ConfigEntry<bool> EnableTextureMipStreaming = null!;
    public static ConfigEntry<int>  TextureStreamingBudgetMB = null!;
    /// <summary>
    /// masterTextureLimit: 0 = full res, 1 = half res, 2 = quarter res
    /// Lower value = better quality but more VRAM
    /// </summary>
    public static ConfigEntry<int>  MasterTextureLimit = null!;

    // ── Quality tweaks ───────────────────────────────────────────────────────────
    public static ConfigEntry<bool>  EnableQualityTweaks = null!;
    public static ConfigEntry<float> ShadowDistance = null!;
    public static ConfigEntry<int>   PixelLightCount = null!;
    public static ConfigEntry<float> LodBias = null!;
    public static ConfigEntry<int>   MaxQueuedFrames = null!;

    // ── Raid-end GC ──────────────────────────────────────────────────────────────
    public static ConfigEntry<bool> EnableRaidEndGC = null!;
    public static ConfigEntry<bool> UnloadUnusedAssetsOnRaidEnd = null!;

    // ── Memory logging ───────────────────────────────────────────────────────────
    public static ConfigEntry<bool> EnableMemoryLog = null!;
    public static ConfigEntry<int>  MemoryLogIntervalSeconds = null!;

    public static void Initialize(ConfigFile cfg)
    {
        // ── Periodic GC ───────────────────────────────────────────────────────────
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

        // ── Low-memory watchdog ───────────────────────────────────────────────────
        EnableLowMemoryWatchdog = cfg.Bind(
            "2 - Low Memory Watchdog", "Enable", true,
            "Monitor memory usage and trigger GC when it exceeds the threshold.");

        MemoryThresholdMB = cfg.Bind(
            "2 - Low Memory Watchdog", "ThresholdMB", 7168,
            new ConfigDescription("Memory usage (MB) that triggers an emergency GC. Increase if you have more RAM.", new AcceptableValueRange<int>(2048, 32768)));

        WatchdogIntervalSeconds = cfg.Bind(
            "2 - Low Memory Watchdog", "CheckIntervalSeconds", 15,
            new ConfigDescription("How often (seconds) the watchdog checks memory.", new AcceptableValueRange<int>(5, 60)));

        // ── Texture streaming ─────────────────────────────────────────────────────
        EnableTextureMipStreaming = cfg.Bind(
            "3 - Texture Streaming", "Enable", true,
            "Enable Unity texture mipmap streaming. Loads only the required mip levels into VRAM, reducing memory usage significantly.");

        TextureStreamingBudgetMB = cfg.Bind(
            "3 - Texture Streaming", "BudgetMB", 512,
            new ConfigDescription("VRAM budget for texture streaming (MB). Higher = better quality but more VRAM usage.", new AcceptableValueRange<int>(128, 4096)));

        MasterTextureLimit = cfg.Bind(
            "3 - Texture Streaming", "MasterTextureLimit", 0,
            new ConfigDescription("Global texture mip limit: 0=full res, 1=half res, 2=quarter res. 0 is recommended with streaming enabled.", new AcceptableValueRange<int>(0, 3)));

        // ── Quality tweaks ────────────────────────────────────────────────────────
        EnableQualityTweaks = cfg.Bind(
            "4 - Quality Tweaks", "Enable", true,
            "Apply memory/performance quality setting tweaks at startup.");

        ShadowDistance = cfg.Bind(
            "4 - Quality Tweaks", "ShadowDistance", 100f,
            new ConfigDescription("Shadow render distance (meters). Default game value is ~200. Lower = less GPU/CPU usage.", new AcceptableValueRange<float>(20f, 500f)));

        PixelLightCount = cfg.Bind(
            "4 - Quality Tweaks", "PixelLightCount", 3,
            new ConfigDescription("Maximum number of pixel lights affecting an object. Game default is 4.", new AcceptableValueRange<int>(0, 8)));

        LodBias = cfg.Bind(
            "4 - Quality Tweaks", "LodBias", 1.5f,
            new ConfigDescription("LOD bias multiplier. Higher = better detail at distance. Lower = less GPU usage.", new AcceptableValueRange<float>(0.1f, 4f)));

        MaxQueuedFrames = cfg.Bind(
            "4 - Quality Tweaks", "MaxQueuedFrames", 2,
            new ConfigDescription("Max queued frames ahead (affects GPU memory and input latency). 2 is recommended.", new AcceptableValueRange<int>(0, 4)));

        // ── Raid-end GC ───────────────────────────────────────────────────────────
        EnableRaidEndGC = cfg.Bind(
            "5 - Raid End GC", "Enable", true,
            "Trigger a full GC when exiting a raid to free all orphaned memory.");

        UnloadUnusedAssetsOnRaidEnd = cfg.Bind(
            "5 - Raid End GC", "UnloadUnusedAssets", true,
            "Call Resources.UnloadUnusedAssets() after raid. This is slow but frees a LOT of memory. Runs asynchronously.");

        // ── Memory logging ────────────────────────────────────────────────────────
        EnableMemoryLog = cfg.Bind(
            "6 - Memory Log", "Enable", false,
            "Log current memory usage periodically to the BepInEx console/log.");

        MemoryLogIntervalSeconds = cfg.Bind(
            "6 - Memory Log", "IntervalSeconds", 30,
            new ConfigDescription("How often (seconds) to log memory usage.", new AcceptableValueRange<int>(5, 300)));
    }
}
