import type { Settings } from "../Settings";
import {
	getThemeOption,
	type ThemeVariables,
	themeOptions,
} from "./themeCatalog";

export type AppliedTheme = string;
export type AppearanceSide = "dark" | "light";
export type ThemeOverrides = {
	accent?: string;
	background?: string;
	foreground?: string;
};

export function getSystemTheme() {
	return window.matchMedia?.("(prefers-color-scheme: dark)").matches
		? "dark"
		: "light";
}

export function calculateTheme(
	theme: Settings["Theme"],
	lightTheme = "Morning",
	darkTheme = "Midnight",
	systemTheme?: "dark" | "light" | null,
) {
	const resolvedLightTheme = resolveStoredTheme(lightTheme, "Morning");
	const resolvedDarkTheme = resolveStoredTheme(darkTheme, "Midnight");

	if (theme === "System") {
		if (systemTheme === "dark") {
			return resolvedDarkTheme;
		}
		if (systemTheme === "light") {
			return resolvedLightTheme;
		}
		return getSystemTheme() === "dark" ? resolvedDarkTheme : resolvedLightTheme;
	}

	if (theme === "Light") {
		return resolvedLightTheme;
	}

	if (theme === "Dark") {
		return resolvedDarkTheme;
	}

	return theme;
}

function resolveStoredTheme(theme: string, fallbackTheme: string) {
	return theme === "System" || theme === "Light" || theme === "Dark"
		? fallbackTheme
		: theme;
}

export function calculateAppearanceSide(
	theme: Settings["Theme"],
	systemTheme?: AppearanceSide | null,
): AppearanceSide {
	if (theme === "System") {
		return systemTheme ?? getSystemTheme();
	}

	if (theme === "Light") {
		return "light";
	}

	if (theme === "Dark") {
		return "dark";
	}

	return isDarkTheme(theme) ? "dark" : "light";
}

export function isDarkTheme(theme: AppliedTheme) {
	return getThemeOption(theme).isDark;
}

export function applyThemeToDocument(
	theme: AppliedTheme,
	overrides: ThemeOverrides = {},
) {
	const option = getThemeOption(theme);
	document.documentElement.classList.toggle("dark", option.isDark);
	for (const value of themeOptions) {
		document.documentElement.classList.toggle(
			`theme-${value.value.toLowerCase()}`,
			option.value === value.value,
		);
	}
	applyThemeVariables(option.variables);
	applyThemeOverrides(overrides);
	document.documentElement.dataset.theme = option.value;
}

function applyThemeVariables(variables: ThemeVariables) {
	setCssVariable("background", variables.background);
	setCssVariable("foreground", variables.foreground);
	setCssVariable("card", variables.card);
	setCssVariable("card-foreground", variables.cardForeground);
	setCssVariable("popover", variables.popover);
	setCssVariable("popover-foreground", variables.popoverForeground);
	setCssVariable("primary", variables.primary);
	setCssVariable("primary-foreground", variables.primaryForeground);
	setCssVariable("secondary", variables.secondary);
	setCssVariable("secondary-foreground", variables.secondaryForeground);
	setCssVariable("muted", variables.muted);
	setCssVariable("muted-foreground", variables.mutedForeground);
	setCssVariable("accent", variables.accent);
	setCssVariable("accent-foreground", variables.accentForeground);
	setCssVariable("border", variables.border);
	setCssVariable("input", variables.input);
	setCssVariable("ring", variables.ring);
	setCssVariable("sidebar", variables.sidebar);
	setCssVariable("sidebar-foreground", variables.sidebarForeground);
	setCssVariable("sidebar-primary", variables.sidebarPrimary);
	setCssVariable(
		"sidebar-primary-foreground",
		variables.sidebarPrimaryForeground,
	);
	setCssVariable("sidebar-accent", variables.sidebarAccent);
	setCssVariable(
		"sidebar-accent-foreground",
		variables.sidebarAccentForeground,
	);
	setCssVariable("sidebar-border", variables.sidebarBorder);
	setCssVariable("sidebar-ring", variables.sidebarRing);
}

function setCssVariable(name: string, value: string) {
	document.documentElement.style.setProperty(`--${name}`, value);
}

function applyThemeOverrides(overrides: ThemeOverrides) {
	if (overrides.background) {
		setCssVariable("background", overrides.background);
		setCssVariable("card", surfaceMix(6));
		setCssVariable("popover", surfaceMix(4));
		setCssVariable("secondary", surfaceMix(12));
		setCssVariable("muted", surfaceMix(14));
		setCssVariable("accent", surfaceMix(20));
		setCssVariable("border", surfaceMix(26));
		setCssVariable("input", surfaceMix(24));
		setCssVariable("sidebar", surfaceMix(5));
		setCssVariable("sidebar-accent", surfaceMix(18));
		setCssVariable("sidebar-border", surfaceMix(26));
	}

	if (overrides.foreground) {
		setCssVariable("foreground", overrides.foreground);
		setCssVariable("card-foreground", overrides.foreground);
		setCssVariable("popover-foreground", overrides.foreground);
		setCssVariable("sidebar-foreground", overrides.foreground);
	}

	if (overrides.accent) {
		setCssVariable("primary", overrides.accent);
		setCssVariable("ring", overrides.accent);
		setCssVariable("sidebar-primary", overrides.accent);
		setCssVariable("sidebar-ring", overrides.accent);
	}
}

function surfaceMix(foregroundPercent: number) {
	return `color-mix(in oklch, var(--background) ${100 - foregroundPercent}%, var(--foreground) ${foregroundPercent}%)`;
}
