using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SPTOptimizer.Config;
using SPTOptimizer.Patches;
using SPTOptimizer.Utils;
using UnityEngine;

namespace SPTOptimizer;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.spt.optimizer";
    public const string PluginName = "SPT Optimizer";
    public const string PluginVersion = "1.1.0";

    internal static ManualLogSource Log = null!;
    private Harmony? _harmony;

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"Loading {PluginName} v{PluginVersion}...");

        // 1. Initialize configuration (Config entries will appear in F12 Configuration Manager)
        OptimizerConfig.Initialize(Config);

        // 2. Initialize and attach persistent MonoBehaviour components
        PeriodicGCComponent.Initialize(Log);
        LowMemoryWatchdogComponent.Initialize(Log);
        MemoryLogComponent.Initialize(Log);
        MemoryTrimmer.Initialize(Log);

        gameObject.AddComponent<PeriodicGCComponent>();
        gameObject.AddComponent<LowMemoryWatchdogComponent>();
        gameObject.AddComponent<MemoryLogComponent>();
        DontDestroyOnLoad(gameObject);

        // 3. Apply Graphics & Process settings
        GraphicsOptimizer.Initialize(Log);
        ProcessOptimizer.Initialize(Log);

        // 4. Apply Harmony Patches
        try
        {
            _harmony = new Harmony(PluginGuid);
            RaidEndGCPatch.Apply(Log, _harmony, this);
            MenuAndHideoutCleanupPatch.Apply(Log, _harmony, this);
            DeadBotOptimizerPatch.Apply(Log, _harmony, this);
            CorpseLootSafetyPatch.Apply(Log, _harmony);
            Log.LogInfo("[Harmony] All patches successfully initialized.");
        }
        catch (System.Exception ex)
        {
            Log.LogError($"[Harmony] Failed to apply patches: {ex.Message}");
        }

        Log.LogInfo($"{PluginName} loaded successfully! Press F12 in-game to access settings.");
    }
}

public static class PluginInfo
{
    public const string PLUGIN_GUID = "com.spt.optimizer";
    public const string PLUGIN_NAME = "SPT Optimizer";
    public const string PLUGIN_VERSION = "1.1.0";
}