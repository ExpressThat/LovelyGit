import { describe, expect, it, vi } from "vitest";

describe("theme catalog", () => {
	it("defers palette realization until color data is requested", async () => {
		vi.resetModules();
		const cosine = vi.spyOn(Math, "cos");
		const catalog = await import("./themeCatalog");

		expect(catalog.themeOptions).toHaveLength(98);
		for (const option of catalog.themeOptions) {
			expect(option.value).not.toBe("");
			expect(option.label).not.toBe("");
			expect(typeof option.isDark).toBe("boolean");
		}
		expect(cosine).not.toHaveBeenCalled();

		const ruby = catalog.getThemeOption("RubyLight");
		expect(ruby).toMatchObject({
			accent: "#8c4060",
			background: "#fef4f6",
			card: "#f7e6ea",
			foreground: "#261519",
		});
		expect(ruby.variables.primary).toBe("oklch(0.48 0.11 356)");
		expect(cosine).toHaveBeenCalledTimes(4);

		expect(ruby.accent).toBe("#8c4060");
		expect(cosine).toHaveBeenCalledTimes(4);
	});

	it("preserves representative dark palette values", async () => {
		const { getThemeOption } = await import("./themeCatalog");

		expect(getThemeOption("Midnight")).toMatchObject({
			accent: "#63adf6",
			background: "#070b13",
			card: "#121a26",
			foreground: "#eef4fd",
		});
	});
});
