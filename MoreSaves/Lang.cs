using TeamCherry.Localization;

namespace MoreSaves;

internal static class Lang {
	internal static string Get(string key) => Language.CurrentLanguage() switch {
		LanguageCode.ZH => key switch {
			"CopyIdle" => "复制存档",
			"CopySelect" => "选择要复制的存档",
			"SaveIndex" => "#{0}",
			"SaveIndexWithName" => "#{0}（{1}）",
			"CopySelected" => "已复制存档 {0}",
			_ => key,
		},
		_ => key switch {
			"CopyIdle" => "Copy Save",
			"CopySelect" => "Select Save to Copy",
			"SaveIndex" => "#{0}",
			"SaveIndexWithName" => "#{0} ({1})",
			"CopySelected" => "Copied Save {0}",
			_ => key
		}
	};
}
