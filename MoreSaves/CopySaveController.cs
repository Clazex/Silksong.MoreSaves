using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using static MoreSaves.SaveClipboard;

namespace MoreSaves;

[RequireComponent(typeof(MenuButton))]
internal sealed class CopySaveController: MonoBehaviour {
	internal static CopySaveController Instance {
		get => field != null ? field : throw new InvalidOperationException("instance not present");
		private set => field = value;
	}

	internal static void Setup(UIManager ui) {
		GameObject control = ui.saveProfileControls.gameObject;
		GameObject buttonBackGo = control.transform.Find("BackButton").gameObject;
		MenuButton buttonBack = buttonBackGo.GetComponent<MenuButton>();

		GameObject buttonCopyGo = Instantiate(buttonBackGo, control.transform);
		buttonCopyGo.name = "CopySaveButton";
		buttonCopyGo.transform.Translate(0f, -1.25f, 0f);
		DestroyImmediate(buttonCopyGo.GetComponent<EventTrigger>());
		buttonCopyGo.AddComponent<CopySaveController>();

		CancelCopy();

		MenuButton buttonCopy = buttonCopyGo.GetComponent<MenuButton>();
		buttonCopy.buttonType = MenuButton.MenuButtonType.Activate; // Don't force deselect

		buttonBack.navigation = buttonBack.navigation with {
			selectOnDown = buttonCopy
		};
		buttonCopy.navigation = buttonCopy.navigation with {
			selectOnUp = buttonBack
		};
	}

	internal static void OnStateChanged(ClipboardState state) {
		SetSaveButtonsDeselect(state == ClipboardState.Idle);
		Instance.RefreshText();
	}

	private static void SetSaveButtonsDeselect(bool doDeselect) {
		MenuButton.MenuButtonType type = doDeselect
			? MenuButton.MenuButtonType.Proceed
			: MenuButton.MenuButtonType.Activate;
		CoreLoop.InvokeNext(() => {
			foreach (SaveSlotButton button in SavePageState.buttons) {
				button.buttonType = type;
			}
		});
	}

	private static string GetText() => State switch {
		ClipboardState.Idle => Lang.Get("CopyIdle"),
		ClipboardState.CopySelect => Lang.Get("CopySelect"),
		ClipboardState.PasteSelect => string.Format(Lang.Get("CopySelected"), SaveName),
		_ => throw new InvalidOperationException()
	};


	private GameObject textChild;
	private Text text;
	private FixVerticalAlign textAligner;

	private void Awake() {
		Instance = this;

		MenuButton button = GetComponent<MenuButton>();
		button.cancelAction = GlobalEnums.CancelAction.GoToMainMenu;
		UnityEvent @event = new();
		@event.AddListener(new(StartCopy));
		button.OnSubmitPressed = @event;

		textChild = transform.Find("Menu Button Text").gameObject;
		text = textChild.GetComponent<Text>();
		textAligner = textChild.GetComponent<FixVerticalAlign>();
		DestroyImmediate(textChild.GetComponent<AutoLocalizeTextUI>());
		GameManager.instance.RefreshLanguageText += RefreshText;
		RefreshText();
	}

	private void OnDestroy() =>
		GameManager.instance.RefreshLanguageText -= RefreshText;

	private void RefreshText() {
		text.text = GetText();
		textAligner.AlignText();
	}
}
