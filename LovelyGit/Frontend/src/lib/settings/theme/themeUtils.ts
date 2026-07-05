import type { Settings } from "../Settings";

export type AppliedTheme =
	| "Morning"
	| "Midnight"
	| "Harbor"
	| "Forest"
	| "Ember"
	| "Rose"
	| "Copper"
	| "Orchid";

export function getSystemTheme() {
	return window.matchMedia?.("(prefers-color-scheme: dark)").matches
		? "Midnight"
		: "Morning";
}

export function calculateTheme(
	theme: Settings["Theme"],
	systemTheme?: "dark" | "light" | null,
) {
	if (theme === "System") {
		if (systemTheme === "dark") {
			return "Midnight";
		}
		if (systemTheme === "light") {
			return "Morning";
		}
		return getSystemTheme();
	}

	if (theme === "Light") {
		return "Morning";
	}

	if (theme === "Dark") {
		return "Midnight";
	}

	return theme;
}

export function isDarkTheme(theme: AppliedTheme) {
	return (
		theme === "Midnight" ||
		theme === "Forest" ||
		theme === "Ember" ||
		theme === "Orchid"
	);
}

const appThemeValues: AppliedTheme[] = [
	"Morning",
	"Midnight",
	"Harbor",
	"Forest",
	"Ember",
	"Rose",
	"Copper",
	"Orchid",
];

export function applyThemeToDocument(theme: AppliedTheme) {
	document.documentElement.classList.toggle("dark", isDarkTheme(theme));
	for (const value of appThemeValues) {
		document.documentElement.classList.toggle(
			`theme-${value.toLowerCase()}`,
			theme === value,
		);
	}
	document.documentElement.dataset.theme = theme;
}
