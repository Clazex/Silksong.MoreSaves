using System.Reflection.Emit;

using HarmonyLib;

using UnityEngine.UI;

namespace MoreSaves;

internal static class Patches {
	[HarmonyPatch(typeof(UIManager), nameof(UIManager.Start))]
	[HarmonyWrapSafe]
	[HarmonyPrefix]
	private static void Setup(UIManager __instance) {
		SavePageState.buttons = [
			__instance.slotOne,
			__instance.slotTwo,
			__instance.slotThree,
			__instance.slotFour,
		];

		SavePageState.SetPageByLastIndex(); // Affects which to preload

		foreach (SaveSlotButton button in SavePageState.buttons) {
			Text text = button.slotNumberText.GetComponent<Text>();
			text.horizontalOverflow = HorizontalWrapMode.Overflow; // Make multiple digits fit in
			text.alignment = TextAnchor.LowerLeft;
			text.transform.Translate(0.34f, 0f, 0f); // Adjust position per the alignment change
			text.text = SavePageState.GenerateSlotNumberText(button);
		}

		if (ConfigEntries.Experimental.SaveClipboard.Value) {
			CopySaveController.Setup(__instance);
		}

		__instance.gameObject.AddComponent<SavePageNavigator>();

		Plugin.Logger.LogDebug("Setup complete");
	}

	#region Menu sequences

	[HarmonyPatch(typeof(UIManager), nameof(UIManager.UIGoToProfileMenu))]
	[HarmonyWrapSafe]
	[HarmonyPrefix]
	private static void UpdatePage() => SavePageState.SetPageByLastIndex();

	[HarmonyPatch(typeof(UIManager), nameof(UIManager.GoToProfileMenu), MethodType.Enumerator)]
	[HarmonyWrapSafe]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> ModifyGoToProfileSequence(IEnumerable<CodeInstruction> insts) => new CodeMatcher(insts)
		.MatchForward(
			true,
			new(OpCodes.Stloc_2),
			new(OpCodes.Ldloc_2)
		)
		.Advance(1)
		.InsertAndAdvance( // Offset index of the button to highlight
			new(OpCodes.Call, Info.OfPropertyGet(nameof(MoreSaves), $"{nameof(MoreSaves)}.{nameof(SavePageState)}", nameof(SavePageState.CurrentIndexBase))),
			new(OpCodes.Sub)
		)
		.MatchForward(
			true,
			new(OpCodes.Callvirt, Info.OfMethod<SaveSlotButton>(nameof(SaveSlotButton.ShowRelevantModeForSaveFileState))),
			new(OpCodes.Ldarg_0),
			new(OpCodes.Ldc_R4 /* , 0.165f */) // Do not match the operand in case of fast menu mods
		)
		.SetOperandAndAdvance(0f) // Make them appear at the same time
		.InstructionEnumeration();

	[HarmonyPatch(typeof(UIManager), nameof(UIManager.HideSaveProfileMenu), MethodType.Enumerator)]
	[HarmonyWrapSafe]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> HideButtonsAtOnce(IEnumerable<CodeInstruction> insts) => new CodeMatcher(insts)
		.MatchForward( // Make them disappear at the same time
			false,
			[new(OpCodes.Ldc_R4, 0.165f)] // Do match the operand here since Repeat will not throw if not found
		)
		.Repeat(matcher => matcher.SetOperandAndAdvance(0f))
		.InstructionEnumeration();

	#endregion

	#region Indices

	[HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.SaveSlotIndex), MethodType.Getter)]
	[HarmonyWrapSafe]
	[HarmonyPostfix]
	private static void OffsetSlotIndex(ref int __result) =>
		__result += SavePageState.CurrentIndexBase;

	[HarmonyPatch(typeof(Platform), nameof(Platform.IsSaveSlotIndexValid))]
	[HarmonyWrapSafe]
	[HarmonyPostfix]
	private static void MakeAllIndicesValid(int slotIndex, ref bool __result) =>
		__result = slotIndex >= 0;

	[HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.Awake))]
	[HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.Prepare))]
	[HarmonyWrapSafe]
	[HarmonyPrefix]
	private static void UpdateSlotNumberText(SaveSlotButton __instance) =>
		__instance.slotNumberText.GetComponent<Text>().text = SavePageState.GenerateSlotNumberText(__instance);

	#endregion

	#region Clipboard

	[HarmonyPatch(typeof(UIManager), nameof(UIManager.UIContinueGame), typeof(int), typeof(SaveGameData))]
	[HarmonyWrapSafe]
	[HarmonyPrefix]
	private static bool SelectSaveNonempty(int slot, SaveGameData saveGameData) =>
		SaveClipboard.Select(slot, saveGameData);

	[HarmonyPatch(typeof(UIManager), nameof(UIManager.UIGoToPlayModeMenu))]
	[HarmonyPatch(typeof(UIManager), nameof(UIManager.StartNewGame))]
	[HarmonyWrapSafe]
	[HarmonyPrefix]
	private static bool SelectSaveEmpty(UIManager __instance) =>
		SaveClipboard.Select(__instance.gm.profileID, null);

	[HarmonyPatch(typeof(RestoreSaveButton), nameof(RestoreSaveButton.SaveSelected))]
	[HarmonyWrapSafe]
	[HarmonyPrefix]
	private static bool SelectRestorePoint(RestoreSaveButton __instance, RestorePointData restorePointData) {
		bool continues = SaveClipboard.Select(__instance.saveSlotButton.SaveSlotIndex, restorePointData.saveGameData, restorePointData.autoSaveName);
		if (!continues) {
			__instance.saveSlotButton.ShowRelevantModeForSaveFileState();
		}

		return continues;
	}

	[HarmonyPatch(typeof(ClearSaveButton), nameof(ClearSaveButton.OnSubmit))]
	[HarmonyWrapSafe]
	[HarmonyPrefix]
	private static void CancelCopyOnClear() =>
		SaveClipboard.CancelCopy();

	[HarmonyPatch(typeof(UIManager), nameof(UIManager.HideSaveProfileMenu))]
	[HarmonyWrapSafe]
	[HarmonyPostfix]
	private static IEnumerator CancelCopyOnReturn(IEnumerator __result) {
		while (__result.MoveNext()) {
			yield return __result.Current;
		}

		SaveClipboard.CancelCopy();
	}

	#endregion
}
