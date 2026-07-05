import { useEffect, useState } from "react";
import useSystemTheme from "react-use-system-theme";
import { useSetting } from "@/lib/settings/settingsStore";
import { calculateAppearanceSide } from "@/lib/settings/theme/themeUtils";
import { applyFontsToDocument } from "./fontUtils";

export function useApplyFont() {
	const theme = useSetting("Theme");
	const font = useSetting("Font");
	const uiFont = useSetting("UiFont");
	const codeFont = useSetting("CodeFont");
	const lightUiFont = useSetting("LightUiFont") || uiFont || font;
	const lightCodeFont = useSetting("LightCodeFont") || codeFont || font;
	const darkUiFont = useSetting("DarkUiFont") || uiFont || font;
	const darkCodeFont = useSetting("DarkCodeFont") || codeFont || font;
	const systemTheme = useSystemTheme("dark");
	const [systemPreference, setSystemPreference] = useState<
		"dark" | "light" | null
	>(systemTheme);

	useEffect(() => {
		const mediaQuery = window.matchMedia?.("(prefers-color-scheme: dark)");
		if (!mediaQuery) {
			return;
		}

		const onChange = () => {
			setSystemPreference(mediaQuery.matches ? "dark" : "light");
		};

		mediaQuery.addEventListener("change", onChange);
		onChange();
		return () => {
			mediaQuery.removeEventListener("change", onChange);
		};
	}, []);

	useEffect(() => {
		const side = calculateAppearanceSide(theme, systemPreference);
		applyFontsToDocument(
			side === "dark" ? darkUiFont : lightUiFont,
			side === "dark" ? darkCodeFont : lightCodeFont,
		);
	}, [
		darkCodeFont,
		darkUiFont,
		lightCodeFont,
		lightUiFont,
		theme,
		systemPreference,
	]);
}
