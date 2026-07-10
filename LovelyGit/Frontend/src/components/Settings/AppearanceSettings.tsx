import { useCallback, useEffect, useMemo, useState } from "react";
import {
	builtInFontOptions,
	type FontOption,
	getFontOption,
	loadAvailableFontOptions,
} from "@/lib/settings/font/fontUtils";
import {
	setSetting,
	setSettings,
	useSetting,
} from "@/lib/settings/settingsStore";
import {
	getThemeOption,
	themeOptions,
} from "@/lib/settings/theme/themeCatalog";
import { CodePreview, ModePreview } from "./AppearancePreviews";
import { ThemePanel } from "./ThemePanel";

const lightThemeOptions = themeOptions.filter((option) => !option.isDark);
const darkThemeOptions = themeOptions.filter((option) => option.isDark);

export function AppearanceSettings() {
	const mode = useSetting("Theme");
	const lightTheme = useSetting("LightTheme");
	const darkTheme = useSetting("DarkTheme");
	const legacyFont = useSetting("Font");
	const legacyUiFont = useSetting("UiFont") || legacyFont;
	const legacyCodeFont = useSetting("CodeFont") || legacyFont;
	const lightUiFont = useSetting("LightUiFont") || legacyUiFont;
	const lightCodeFont = useSetting("LightCodeFont") || legacyCodeFont;
	const darkUiFont = useSetting("DarkUiFont") || legacyUiFont;
	const darkCodeFont = useSetting("DarkCodeFont") || legacyCodeFont;
	const lightAccent = useSetting("LightAccent");
	const lightBackground = useSetting("LightBackground");
	const lightForeground = useSetting("LightForeground");
	const darkAccent = useSetting("DarkAccent");
	const darkBackground = useSetting("DarkBackground");
	const darkForeground = useSetting("DarkForeground");
	const [availableFonts, setAvailableFonts] =
		useState<FontOption[]>(builtInFontOptions);
	const [dropdownBoundary, setDropdownBoundary] = useState<Element | null>(
		null,
	);
	const setAppearanceRoot = useCallback((element: HTMLDivElement | null) => {
		setDropdownBoundary(element?.closest("section.custom-scrollbar") ?? null);
	}, []);

	useEffect(() => {
		let isActive = true;
		loadAvailableFontOptions().then((options) => {
			if (!isActive) {
				return;
			}

			setAvailableFonts(options);
		});

		return () => {
			isActive = false;
		};
	}, []);

	const legacyThemeOption =
		mode === "System" || mode === "Light" || mode === "Dark"
			? null
			: getThemeOption(mode);
	const displayMode = legacyThemeOption
		? legacyThemeOption.isDark
			? "Dark"
			: "Light"
		: mode;
	const selectedLightTheme =
		legacyThemeOption && !legacyThemeOption.isDark
			? legacyThemeOption
			: getThemeOption(lightTheme, "Morning");
	const selectedDarkTheme = legacyThemeOption?.isDark
		? legacyThemeOption
		: getThemeOption(darkTheme, "Midnight");
	const previewCodeFont =
		displayMode === "Dark" ? darkCodeFont : lightCodeFont || darkCodeFont;
	const codeFontOption = useMemo(
		() => getFontOption(previewCodeFont),
		[previewCodeFont],
	);

	return (
		<div className="grid gap-6" ref={setAppearanceRoot}>
			<div>
				<h3 className="font-semibold text-lg">Appearance</h3>
				<p className="text-muted-foreground text-sm">
					Choose the app mode, paired palettes, and typefaces.
				</p>
			</div>

			<div className="grid grid-cols-3 gap-3">
				<ModePreview
					active={displayMode === "System"}
					label="System"
					onSelect={() => void setSetting("Theme", "System")}
					type="system"
				/>
				<ModePreview
					active={displayMode === "Light"}
					label="Light"
					onSelect={() => void setSetting("Theme", "Light")}
					type="light"
				/>
				<ModePreview
					active={displayMode === "Dark"}
					label="Dark"
					onSelect={() => void setSetting("Theme", "Dark")}
					type="dark"
				/>
			</div>

			<CodePreview
				codeFont={codeFontOption.stack}
				darkTheme={selectedDarkTheme}
				lightTheme={selectedLightTheme}
			/>

			<div className="grid gap-4">
				<ThemePanel
					accent={lightAccent}
					background={lightBackground}
					codeFont={lightCodeFont}
					fontOptions={availableFonts}
					foreground={lightForeground}
					onAccentChange={(value) => void setSetting("LightAccent", value)}
					onBackgroundChange={(value) =>
						void setSetting("LightBackground", value)
					}
					onCodeFontChange={(value) => void setSetting("LightCodeFont", value)}
					onForegroundChange={(value) =>
						void setSetting("LightForeground", value)
					}
					onThemeChange={(value) =>
						void setSettings({
							LightAccent: "",
							LightBackground: "",
							LightForeground: "",
							LightTheme: value,
						})
					}
					onUiFontChange={(value) => void setSetting("LightUiFont", value)}
					selectedTheme={selectedLightTheme}
					themeOptions={lightThemeOptions}
					title="Light theme"
					uiFont={lightUiFont}
					dropdownBoundary={dropdownBoundary}
				/>
				<ThemePanel
					accent={darkAccent}
					background={darkBackground}
					codeFont={darkCodeFont}
					fontOptions={availableFonts}
					foreground={darkForeground}
					onAccentChange={(value) => void setSetting("DarkAccent", value)}
					onBackgroundChange={(value) =>
						void setSetting("DarkBackground", value)
					}
					onCodeFontChange={(value) => void setSetting("DarkCodeFont", value)}
					onForegroundChange={(value) =>
						void setSetting("DarkForeground", value)
					}
					onThemeChange={(value) =>
						void setSettings({
							DarkAccent: "",
							DarkBackground: "",
							DarkForeground: "",
							DarkTheme: value,
						})
					}
					onUiFontChange={(value) => void setSetting("DarkUiFont", value)}
					selectedTheme={selectedDarkTheme}
					themeOptions={darkThemeOptions}
					title="Dark theme"
					uiFont={darkUiFont}
					dropdownBoundary={dropdownBoundary}
				/>
			</div>
		</div>
	);
}
