using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OnirismSpraypaint;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class SpraypaintPlugin : BaseUnityPlugin
{
    const KeyCode defaultSpray = KeyCode.T;
    const KeyCode defaultCycle = KeyCode.LeftAlt;
    public static event Action? OnUnload;
    public static Transform DecalFolder = new GameObject().transform;
    static DecalInstance? DecalBeingPosed;

    static ConfigEntry<KeyCode>? SprayCfg;
    static ConfigEntry<KeyCode>? CycleCfg;
    static KeyCode SprayKey => SprayCfg?.Value ?? defaultSpray;
    static KeyCode CycleKey => CycleCfg?.Value ?? defaultCycle;

    void Awake()
    {
        new Log(base.Logger);
        Log.Info($"Starting {MyPluginInfo.PLUGIN_GUID}...");
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Log.Debug("Setting up decal parent");
        DecalFolder.name = "Placed Decals";
        DecalFolder.parent = this.transform;
        //do we need to ensure the position and rotation are zereod out?
        Log.Debug("Reading config...");
        SprayCfg = this.Config.Bind("Keybinds", "Spray Key", defaultSpray, "Key used to apply spray.");
        CycleCfg = this.Config.Bind("Keybinds", "Cycle Key", defaultCycle, "Key used to cycle through available sprays.");
        Log.Debug("Loading user files...");
        SprayLoader.LoadImages();
        Log.Info($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void HandleSceneLoaded(Scene _, LoadSceneMode __) => SprayLoader.Save();

    void Update()
    {
        if (Input.GetKeyDown(CycleKey)) Cycle();
        if (!Input.GetKeyDown(SprayKey)) return;

        if (!DecalBeingPosed)
        {
            if (SprayLoader.Current() is not (var filename, var mat)) { Log.Warning("No loaded sprays when spraying."); return; }

            DecalBeingPosed = DecalInstance.New(mat, filename);
            DecalBeingPosed.Pose();
            return;
        }

        DecalBeingPosed.Place();
        DecalBeingPosed = null;
    }

    void Cycle() 
    {
        Log.Debug("SpraypaintPlugin.Cycle()");
        SprayLoader.Cycle();
        if (!DecalBeingPosed) return;
        if (SprayLoader.Current() is not (var filename, var mat)) return;

        DecalBeingPosed.SetMaterial(mat, filename);
    }

    void OnDestroy()
    {
        Log.Info($"{MyPluginInfo.PLUGIN_GUID}.OnDestroy()");
        OnUnload?.Invoke();
    }

}

internal class Log
{
    static ManualLogSource? i;
    internal Log(ManualLogSource manualLogSource) => i = manualLogSource; 
    internal static void Debug(string message) => i?.LogDebug(message);
    internal static void Info(string message) => i?.LogInfo(message);
    internal static void Message(string message) => i?.LogMessage(message);
    internal static void Warning(string message) => i?.LogWarning(message);
    internal static void Error(string message) => i?.LogError(message);
    internal static void Fatal(string message) => i?.LogFatal(message);
}