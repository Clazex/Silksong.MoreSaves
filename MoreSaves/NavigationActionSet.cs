using InControl;

namespace MoreSaves;

internal sealed class NavigationActionSet : PlayerActionSet {
	// InputHandler is (probably) not inited when initing this.
	// A Reload call is necessary to actually setup this, which is taken care
	// of by PageNavigator.Awake.
	internal static NavigationActionSet Instance { get; private set; } = new();

	internal static void Reload() => Instance = new();

	static NavigationActionSet() => Plugin.ConfigChanged += (entry) => {
		if (entry == null || entry.Definition.Section is nameof(ConfigEntries.KeyboardBindings) or nameof(ConfigEntries.ControllerBindings)) {
			Reload();
		}
	};


	private readonly PlayerAction keyboardPrev;
	private readonly PlayerAction keyboardNext;

	private readonly PlayerAction controllerPrev;
	private readonly PlayerAction controllerNext;

	internal bool PrevIsPressed => keyboardPrev.IsPressed || controllerPrev.IsPressed;
	internal bool PrevWasPressed => keyboardPrev.WasPressed || controllerPrev.WasPressed;
	internal bool PrevWasRepeated => keyboardPrev.WasRepeated || controllerPrev.WasRepeated;
	internal bool NextIsPressed => keyboardNext.IsPressed || controllerNext.IsPressed;
	internal bool NextWasPressed => keyboardNext.WasPressed || controllerNext.WasPressed;
	internal bool NextWasRepeated => keyboardNext.WasRepeated || controllerNext.WasRepeated;

	private NavigationActionSet() {
		Plugin.Logger.LogDebug("Creating navigation action set");

		if (ConfigEntries.KeyboardBindings.PreviousPage.Key is Key keyPrev and not Key.None) {
			keyboardPrev = new("Keyboard Prev Page", this);
			keyboardPrev.AddDefaultBinding(keyPrev);
		} else {
			keyboardPrev = InputHandler.Instance.inputActions.PaneLeft;
		}

		if (ConfigEntries.KeyboardBindings.NextPage.Key is Key keyNext and not Key.None) {
			keyboardNext = new("Keyboard Next Page", this);
			keyboardNext.AddDefaultBinding(keyNext);
		} else {
			keyboardNext = InputHandler.Instance.inputActions.PaneRight;
		}

		if (ConfigEntries.ControllerBindings.PreviousPage.Value is InputControlType inputPrev and not InputControlType.None) {
			controllerPrev = new("Controller Prev Page", this);
			controllerPrev.AddDefaultBinding(inputPrev);
		} else {
			controllerPrev = InputHandler.Instance.inputActions.PaneLeft;
		}

		if (ConfigEntries.ControllerBindings.NextPage.Value is InputControlType inputNext and not InputControlType.None) {
			controllerNext = new("Controller Next Page", this);
			controllerNext.AddDefaultBinding(inputNext);
		} else {
			controllerNext = InputHandler.Instance.inputActions.PaneRight;
		}
	}
}
