using UnityEngine.UI;

namespace MoreSaves;

internal static class SaveClipboard {
	internal enum ClipboardState {
		Idle,
		CopySelect,
		PasteSelect
	}

	internal static ClipboardState State {
		get;
		private set {
			field = value;
			CopySaveController.OnStateChanged(value);
		}
	} = ClipboardState.Idle;

	private static int saveIndex = 0;
	private static AutoSaveName name = AutoSaveName.NONE;
	private static SaveGameData? data = null;

	internal static string SaveName => name == AutoSaveName.NONE
		? string.Format(Lang.Get("SaveIndex"), saveIndex)
		: string.Format(Lang.Get("SaveIndexWithName"), saveIndex, GameManager.GetFormattedAutoSaveNameString(name));

	internal static void StartCopy() {
		if (State == ClipboardState.Idle) {
			State = ClipboardState.CopySelect;
			Plugin.Logger.LogDebug("Copy started");
		} else {
			CancelCopy();
		}
	}

	internal static void CancelCopy() {
		if (State == ClipboardState.Idle) {
			Plugin.Logger.LogDebug("Not copying, remain idle");
			return;
		}

		saveIndex = 0;
		data = null;
		State = ClipboardState.Idle;
		Plugin.Logger.LogDebug("Copy cancelled");
	}

	/// <returns>Continues executing original function</returns>
	internal static bool Select(int slot, SaveGameData? saveData, AutoSaveName autoSaveName = AutoSaveName.NONE) {
		if (State == ClipboardState.Idle) {
			return true;
		}

		if (saveData == null) {
			if (State == ClipboardState.PasteSelect) {
				PasteSave(slot);
			} else {
				CancelCopy();
			}
		} else {
			if (State == ClipboardState.CopySelect) {
				CopySave(slot, saveData, autoSaveName);
			} else {
				CancelCopy();
			}
		}

		return false;
	}

	private static void CopySave(int slot, SaveGameData saveGameData, AutoSaveName autoSaveName) {
		if (State != ClipboardState.CopySelect) {
			Plugin.Logger.LogError($"State {State} is invalid to copy save");
			CancelCopy();
			return;
		}

		saveIndex = slot;
		name = autoSaveName;
		data = saveGameData;
		State = ClipboardState.PasteSelect;
		Plugin.Logger.LogDebug($"Copied save {SaveName}");
	}

	private static void PasteSave(int slot) {
		Plugin.Logger.LogDebug("Start pasting");
		CopySaveController.Instance.StartCoroutine(PasteSaveCoro(slot));
	}

	private static IEnumerator PasteSaveCoro(int slot) {
		GameManager gm = GameManager.instance;
		InputHandler ih = InputHandler.Instance;
		ih.StopUIInput();

		bool? result = null;
		Platform.Current.WriteSaveSlot(slot, gm.GetBytesForSaveData(data), (success) => result = success);
		yield return new WaitUntil(() => result.HasValue);

		SaveSlotButton button = SavePageState.GetButtonForSlot(slot);
		button.ResetButton(gm, false);
		yield return SavePageState.loadSaveWait;
		button.Prepare(gm);

		if (result!.Value) {
			State = ClipboardState.Idle;
			Plugin.Logger.LogDebug($"Pasted save into {slot}");
		} else {
			Plugin.Logger.LogError($"Failed to paste save into {slot}");
		}

		ih.StartUIInput();
	}
}
