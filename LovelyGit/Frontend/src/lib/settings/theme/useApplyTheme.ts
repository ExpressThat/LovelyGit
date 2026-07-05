import { useEffect, useState } from "react";
import useSystemTheme from "react-use-system-theme";
import { useSetting } from "@/lib/settings/settingsStore";
import { applyThemeToDocument, calculateTheme } from "./themeUtils";

export function useApplyTheme() {
	const theme = useSetting("Theme");
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
		const appliedTheme = calculateTheme(theme, systemPreference);
		applyThemeToDocument(appliedTheme);
	}, [theme, systemPreference]);
}
