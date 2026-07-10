// @vitest-environment jsdom

import { beforeEach, describe, expect, it } from "vitest";
import {
	applyThemeToDocument,
	calculateAppearanceSide,
	calculateTheme,
} from "./themeUtils";

describe("theme utilities", () => {
	beforeEach(() => {
		document.documentElement.className = "";
		document.documentElement.removeAttribute("style");
		delete document.documentElement.dataset.theme;
	});

	it("resolves system, light, dark, and named theme choices", () => {
		expect(calculateTheme("System", "Morning", "Midnight", "dark")).toBe(
			"Midnight",
		);
		expect(calculateTheme("Light", "Morning", "Midnight")).toBe("Morning");
		expect(calculateTheme("Dark", "Morning", "Midnight")).toBe("Midnight");
		expect(calculateTheme("Catppuccin", "Morning", "Midnight")).toBe(
			"Catppuccin",
		);
	});

	it("prevents mode names from becoming stored concrete themes", () => {
		expect(calculateTheme("Light", "System", "Dark")).toBe("Morning");
		expect(calculateTheme("Dark", "Light", "System")).toBe("Midnight");
	});

	it("calculates the appearance side for mode and named themes", () => {
		expect(calculateAppearanceSide("System", "dark")).toBe("dark");
		expect(calculateAppearanceSide("Light")).toBe("light");
		expect(calculateAppearanceSide("Dark")).toBe("dark");
		expect(calculateAppearanceSide("Morning")).toBe("light");
		expect(calculateAppearanceSide("Midnight")).toBe("dark");
	});

	it("applies semantic theme variables and custom emphasis overrides", () => {
		applyThemeToDocument("Midnight", {
			accent: "oklch(0.7 0.2 250)",
			background: "oklch(0.1 0.01 250)",
			foreground: "oklch(0.95 0.01 250)",
		});

		const root = document.documentElement;
		expect(root).toHaveClass("dark", "theme-midnight");
		expect(root.dataset.theme).toBe("Midnight");
		expect(root.style.getPropertyValue("--background")).toBe(
			"oklch(0.1 0.01 250)",
		);
		expect(root.style.getPropertyValue("--card")).toContain("color-mix");
		expect(root.style.getPropertyValue("--primary")).toBe("oklch(0.7 0.2 250)");
		expect(root.style.getPropertyValue("--sidebar-foreground")).toBe(
			"oklch(0.95 0.01 250)",
		);
	});
});
