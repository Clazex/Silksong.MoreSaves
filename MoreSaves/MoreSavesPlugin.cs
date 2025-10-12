using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using HarmonyLib;

namespace MoreSaves;

[BepInAutoPlugin(id: "dev.clazex.moresaves")]
public partial class MoreSavesPlugin : BaseUnityPlugin {
	public static MoreSavesPlugin Instance {
		get => field != null
			? field
			: throw new InvalidOperationException("instance not present");
		private set;
	}

	internal static new ManualLogSource Logger {
		get => field ?? throw new InvalidOperationException("instance not present");
		private set;
	}

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
		Harmony.CreateAndPatchAll(typeof(Patches), Id);

		Logger.LogInfo($"Plugin {Name} ({Id}) v{Version} has loaded!");
	}
}
