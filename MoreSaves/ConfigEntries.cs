using System.Runtime.CompilerServices;

using BepInEx.Configuration;

using InControl;

using UnityKey = UnityEngine.KeyCode;

namespace MoreSaves;

internal static class ConfigEntries {
	internal static class Pagination {
		internal static ConfigField<int> MaxPages { get; } = new(
			25,
			"Max number of pages",
			new AcceptableValueRange<int>(1, 50)
		);
	}

	internal static class KeyboardBindings {
		internal static ConfigKeyField PreviousPage { get; } = new(
			UnityKey.None,
			"Previous page binding, uses the Inventory Pane Left binding (\"[\") when set to \"None\""
		);
		internal static ConfigKeyField NextPage { get; } = new(
			UnityKey.None,
			"Next page binding, uses the Inventory Pane Right binding (\"]\") when set to \"None\""
		);
	}

	internal static class ControllerBindings {
		internal static ConfigField<InputControlType> PreviousPage { get; } = new(
			InputControlType.None,
			"Previous page binding, uses the Inventory Pane Left binding (\"LB\"/\"LT\") when set to \"None\""
		);
		internal static ConfigField<InputControlType> NextPage { get; } = new(
			InputControlType.None,
			"Next page binding, uses the Inventory Pane Right binding (\"RB\"/\"RT\") when set to \"None\""
		);
	}

	internal static void Bind(ConfigFile config) {
		config.Bind(Pagination.MaxPages);
		config.Bind(KeyboardBindings.PreviousPage);
		config.Bind(KeyboardBindings.NextPage);
		config.Bind(ControllerBindings.PreviousPage);
		config.Bind(ControllerBindings.NextPage);
	}

	private static void Bind<T>(this ConfigFile config, ConfigField<T> field, [CallerArgumentExpression(nameof(field))] string name = "") {
		if (name.Split('.', StringSplitOptions.RemoveEmptyEntries) is [string section, string key]) {
			field.Bind(config, section, key);
		} else {
			throw new InvalidOperationException($"Unexpected field name: {name}");
		}
	}

	internal class ConfigField<T>(T defaultValue, string description, AcceptableValueBase? acceptableValues = null) {
		internal T DefaultValue { get; private init; } = defaultValue;
		internal string Description { get; private init; } = description;
		internal AcceptableValueBase? AcceptableValues { get; private init; } = acceptableValues;

		private ConfigEntry<T>? entry = null;
		internal ConfigEntry<T> Entry {
			get => entry ?? throw new InvalidOperationException("Not bound to entry");
			private set => entry = value;
		}
		internal T Value => Entry.Value;

		internal void ResetToDefault() => Entry.Value = DefaultValue;

		internal void Bind(ConfigFile config, string section, string key) {
			if (entry != null) {
				throw new InvalidOperationException("Already bound to entry");
			}

			Entry = config.Bind(section, key, DefaultValue, new ConfigDescription(Description, AcceptableValues));
		}
	}

	internal sealed class ConfigKeyField(UnityKey defaultValue, string description)
		: ConfigField<UnityKey>(defaultValue, description, new AcceptableKeyCodes())
	{
		private static readonly Dictionary<UnityKey, Key> mappings = [];
		private static readonly HashSet<UnityKey> validKeyCodes;

		static ConfigKeyField() {
			foreach (UnityKeyboardProvider.KeyMapping mapping in UnityKeyboardProvider.KeyMappings) {
				mappings[mapping.target0] = mapping.source;
				mappings[mapping.target1] = mapping.source; // target1 can be None which we'll handle later
			}

			mappings[UnityKey.None] = Key.None;
			validKeyCodes = [..mappings.Keys];
		}

		internal Key Key => mappings[Value];


		private sealed class AcceptableKeyCodes() : AcceptableValueBase(typeof(UnityKey)) {
			public override bool IsValid(object value) =>
				value is UnityKey keyCode && validKeyCodes.Contains(keyCode);
			public override object Clamp(object value) =>
				IsValid(value) ? value : UnityKey.None;
			public override string ToDescriptionString() =>
				"# Acceptable keys: " + string.Join(", ", validKeyCodes);
		}
	}
}
