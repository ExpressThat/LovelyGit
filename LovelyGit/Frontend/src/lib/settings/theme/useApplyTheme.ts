import { useEffect, useState } from "react";
import useSystemTheme from "react-use-system-theme";
import { useSetting } from "@/lib/settings/settingsStore";
import {
	applyThemeToDocument,
	calculateAppearanceSide,
	calculateTheme,
} from "./themeUtils";

export function useApplyTheme() {
	const theme = useSetting("Theme");
	const lightTheme = useSetting("LightTheme");
	const darkTheme = useSetting("DarkTheme");
	const lightAccent = useSetting("LightAccent");
	const lightBackground = useSetting("LightBackground");
	const lightForeground = useSetting("LightForeground");
	const darkAccent = useSetting("DarkAccent");
	const darkBackground = useSetting("DarkBackground");
	const darkForeground = useSetting("DarkForeground");
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
		const appliedTheme = calculateTheme(
			theme,
			lightTheme,
			darkTheme,
			systemPreference,
		);
		const side = calculateAppearanceSide(theme, systemPreference);
		applyThemeToDocument(
			appliedTheme,
			side === "dark"
				? {
						accent: darkAccent,
						background: darkBackground,
						foreground: darkForeground,
					}
				: {
						accent: lightAccent,
						background: lightBackground,
						foreground: lightForeground,
					},
		);
	}, [
		darkAccent,
		darkBackground,
		darkForeground,
		darkTheme,
		lightAccent,
		lightBackground,
		lightForeground,
		lightTheme,
		theme,
		systemPreference,
	]);
}
