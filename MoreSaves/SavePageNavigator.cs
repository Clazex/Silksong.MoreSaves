namespace MoreSaves;

[RequireComponent(typeof(UIManager))]
internal sealed class SavePageNavigator : MonoBehaviour {
	private const int BULK_NAVIGATE_STRIDE = 5;
	private const float BULK_NAVIGATE_COOLDOWN = 0.5f;

	private static NavigationActionSet Actions => NavigationActionSet.Instance;

	private UIManager ui;
	private InputHandler ih;

	private float bulkNavCooldown = 0f;

	private void Awake() {
		ui = GetComponent<UIManager>();
		ih = ui.ih;
		NavigationActionSet.Reload(); // Actually setup the instance
		Plugin.Logger.LogDebug("Navigator awake");
	}

	private void Update() {
		if (bulkNavCooldown > 0f) {
			bulkNavCooldown -= Time.unscaledDeltaTime;
		}

		if (!ih.acceptingInput || !ui.saveProfileControls || !ui.saveProfileControls.interactable) {
			return;
		}

		if (!Actions.PrevIsPressed && !Actions.NextIsPressed) {
			return;
		}

		if (Actions.PrevIsPressed && Actions.NextIsPressed) {
			return;
		}

		if (Actions.PrevWasPressed || Actions.NextWasPressed) {
			Plugin.Logger.LogDebug("Attempt navigating");
			AttemptNavigate(Actions.NextWasPressed);
			return;
		}

		if (Actions.PrevWasRepeated || Actions.NextWasRepeated) {
			if (bulkNavCooldown > 0f) {
				return;
			}

			bulkNavCooldown = BULK_NAVIGATE_COOLDOWN;
			Plugin.Logger.LogDebug("Attempt bulk navigating");
			AttemptNavigate(Actions.NextWasRepeated, BULK_NAVIGATE_STRIDE);
		}
	}

	private static void AttemptNavigate(bool forward, int stride = 1) {
		int target = Math.Clamp(
			SavePageState.CurrentPage + ((forward ? 1 : -1) * stride),
			0,
			ConfigEntries.Pagination.MaxPages.Value - 1
		);

		if (target == SavePageState.CurrentPage) {
			Plugin.Logger.LogDebug("Page unchanged");
			return;
		}

		// We do not check IsChangingPage here since input should be blocked then
		SavePageState.StartNavigation(target);
	}
}
