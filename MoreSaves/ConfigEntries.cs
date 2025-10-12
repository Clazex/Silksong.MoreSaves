using System.Runtime.CompilerServices;

using BepInEx.Configuration;

using InControl;

using UnityEngine.Assertions;

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
			KeyCode.None,
			"Previous page binding, uses the Inventory Pane Left binding (\"[\") when set to \"None\""
		);
		internal static ConfigKeyField NextPage { get; } = new(
			KeyCode.None,
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
		string[] parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
		Assert.AreEqual(2, parts.Length);
		field.Bind(config, parts[0], parts[1]);
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

	internal sealed class ConfigKeyField(KeyCode defaultValue, string description)
		: ConfigField<KeyCode>(defaultValue, description, new AcceptableKeyCodes())
	{
		private static readonly Dictionary<KeyCode, Key> mappings = [];
		private static readonly HashSet<KeyCode> validKeyCodes;

		static ConfigKeyField() {
			foreach (UnityKeyboardProvider.KeyMapping mapping in UnityKeyboardProvider.KeyMappings) {
				mappings[mapping.target0] = mapping.source;
				mappings[mapping.target1] = mapping.source; // target1 can be None which we'll handle later
			}

			mappings[KeyCode.None] = Key.None;
			validKeyCodes = [..mappings.Keys];
		}

		internal Key Key => mappings[Value];


		private sealed class AcceptableKeyCodes() : AcceptableValueBase(typeof(KeyCode)) {
			public override bool IsValid(object value) =>
				value is KeyCode keyCode && validKeyCodes.Contains(keyCode);
			public override object Clamp(object value) =>
				IsValid(value) ? value : KeyCode.None;
			public override string ToDescriptionString() =>
				"# Acceptable keys: " + string.Join(", ", validKeyCodes);
		}
	}
}
