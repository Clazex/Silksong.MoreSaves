using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using HarmonyLib;

namespace MoreSaves;

[BepInAutoPlugin(id: "dev.clazex.moresaves")]
public sealed partial class MoreSavesPlugin : BaseUnityPlugin {
	public static MoreSavesPlugin Instance { get; private set; } = null!;

	internal static new ManualLogSource Logger { get; private set; } = null!;

	private static Harmony Harmony { get; } = new(Id);

	internal static event Action<ConfigEntryBase?>? ConfigChanged;
	private static void InvokeConfigChanged(ConfigEntryBase? entry) {
		if (entry != null) {
			Logger.LogDebug($"Config changed: {entry.Definition.Section}.{entry.Definition.Key}");
		} else {
			Logger.LogDebug("Config changed");
		}

		ConfigChanged?.Invoke(entry);
	}

	private void Awake() {
		Instance = this;
		Logger = base.Logger;

		ConfigEntries.Bind(Config);
		Config.ConfigReloaded += (_, _) => InvokeConfigChanged(null);
		Config.SettingChanged += (_, args) => InvokeConfigChanged(args.ChangedSetting);
		Harmony.PatchAll(typeof(Patches));

		Logger.LogInfo($"Plugin {Name} ({Id}) v{Version} has loaded!");
	}

	private void OnDestroy() {
#if !DEBUG
		Logger.LogWarning("Unload called in release build");
#endif
		Harmony.UnpatchSelf();
		Logger.LogInfo($"Plugin {Name} has unloaded!");
	}
}
