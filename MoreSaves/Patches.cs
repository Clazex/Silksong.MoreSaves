using System.Reflection.Emit;

using HarmonyLib;

using UnityEngine.UI;

namespace MoreSaves;

internal static class Patches {
	[HarmonyPatch(typeof(UIManager), nameof(UIManager.Start))]
	[HarmonyWrapSafe]
	[HarmonyPrefix]
	private static void Setup(UIManager __instance) {
		SavePageState.slotButtons = [
			__instance.slotOne,
			__instance.slotTwo,
			__instance.slotThree,
			__instance.slotFour,
		];

		SavePageState.SetPageByLastIndex(); // Affects which to preload

		foreach (SaveSlotButton button in SavePageState.slotButtons) {
			Text text = button.slotNumberText.GetComponent<Text>();
			text.horizontalOverflow = HorizontalWrapMode.Overflow; // Make multiple digits fit in
			text.alignment = TextAnchor.LowerLeft;
			text.transform.Translate(0.34f, 0f, 0f); // Adjust position per the alignment change
			text.text = SavePageState.GenerateSlotNumberText(button);
		}

		__instance.gameObject.AddComponent<SavePageNavigator>();

		Plugin.Logger.LogDebug("Setup complete");
	}

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
	private static IEnumerable<CodeInstruction> ModifyHideProfileSequence(IEnumerable<CodeInstruction> insts) => new CodeMatcher(insts)
		.MatchForward( // Make them disappear at the same time
			false,
			[new(OpCodes.Ldc_R4, 0.165f)] // Do match the operand here since Repeat will not throw if not found
		)
		.Repeat(matcher => matcher.SetOperandAndAdvance(0f))
		.InstructionEnumeration();


	[HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.SaveSlotIndex), MethodType.Getter)]
	[HarmonyWrapSafe]
	[HarmonyPostfix]
	private static void OffsetSlotIndex(ref int __result) =>
		__result += SavePageState.CurrentIndexBase;

	[HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.Awake))]
	[HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.Prepare))]
	[HarmonyWrapSafe]
	[HarmonyPrefix]
	private static void UpdateSlotNumberText(SaveSlotButton __instance) =>
		__instance.slotNumberText.GetComponent<Text>().text = SavePageState.GenerateSlotNumberText(__instance);

	
	[HarmonyPatch(typeof(Platform), nameof(Platform.IsSaveSlotIndexValid))]
	[HarmonyWrapSafe]
	[HarmonyPostfix]
	private static void MakeAllIndicesValid(int slotIndex, ref bool __result) =>
		__result = slotIndex >= 0;
}
