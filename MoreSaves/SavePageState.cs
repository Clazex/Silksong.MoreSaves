using UnityEngine.UI;

namespace MoreSaves;

internal static class SavePageState {
	internal const int SLOTS_PER_PAGE = 4;

	internal static readonly WaitForSecondsRealtime loadSaveWait = new(0.2f);

	internal static SaveSlotButton[] buttons = [];

	internal static int CurrentPage { get; private set; } = 0;
	private static int LastPage { get; set; } = 0;
	internal static int CurrentIndexBase => CurrentPage * SLOTS_PER_PAGE;
	private static bool IsChangingPage => CurrentPage != LastPage;

	private static int GetLastSlotIndex() =>
		Platform.Current.LocalSharedData.GetInt("lastProfileIndex", 0);

	private static int GetPageForSlot(int slot) =>
		Mathf.FloorToInt((slot - 1f) / 4f);

	internal static SaveSlotButton GetButtonForSlot(int slot) =>
		buttons[(slot - 1) % 4];

	internal static string GenerateSlotNumberText(SaveSlotButton button) =>
		$"{button.SaveSlotIndex}.";

	internal static void SetPageByLastIndex() =>
		LastPage = CurrentPage = GetPageForSlot(GetLastSlotIndex());

	internal static void StartNavigation(int targetPage) {
		if (targetPage < 0 || targetPage >= ConfigEntries.Pagination.MaxPages.Value) {
			throw new ArgumentOutOfRangeException(nameof(targetPage));
		}

		if (IsChangingPage) {
			throw new InvalidOperationException("Page is already changing");
		}

		InputHandler.Instance.StopUIInput();
		CurrentPage = targetPage;
		Plugin.Logger.LogDebug($"Start navigating to page {targetPage}");
		UIManager.instance.StartCoroutine(NavigationCoro());
	}

	private static IEnumerator NavigationCoro() {
		GameManager gm = GameManager.instance;
		UIManager ui = UIManager.instance;
		int highlight = GetLastSlotIndex();

		foreach (SaveSlotButton button in buttons) {
			button.ResetButton(gm, false); // Only do preload
		}

		yield return loadSaveWait;
		foreach (SaveSlotButton button in buttons) {
			button.Prepare(gm);
			if (button.SaveSlotIndex == highlight) {
				ui.saveSlots.itemToHighlight = button;
				ui.saveSlots.HighlightDefault();
			}
		}

		LastPage = CurrentPage;
		Plugin.Logger.LogDebug($"Navigating finished");
		InputHandler.Instance.StartUIInput();
	}
}
