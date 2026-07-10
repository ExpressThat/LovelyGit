// @vitest-environment jsdom

import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	applyFontsToDocument,
	getFontOption,
	loadAvailableFontOptions,
} from "./fontUtils";

describe("font utilities", () => {
	beforeEach(() => {
		document.body.innerHTML = '<div id="root"></div>';
		vi.spyOn(HTMLCanvasElement.prototype, "getContext").mockReturnValue({
			font: "",
			measureText: () => ({ width: 1 }),
		} as unknown as CanvasRenderingContext2D);
		delete (window as Window & { queryLocalFonts?: () => Promise<unknown[]> })
			.queryLocalFonts;
	});

	it("returns built-ins followed by unique sorted local families", async () => {
		Object.assign(window, {
			queryLocalFonts: vi
				.fn()
				.mockResolvedValue([
					{ family: "Zed Mono" },
					{ family: "Alpha Serif" },
					{ family: "Zed Mono" },
				]),
		});

		const options = await loadAvailableFontOptions();

		expect(options.map((option) => option.value)).toEqual([
			"Inter",
			"System",
			"Alpha Serif",
			"Zed Mono",
		]);
		expect(options.at(-1)?.stack).toBe('"Zed Mono", monospace');
	});

	it("falls back cleanly when local font permission is denied", async () => {
		Object.assign(window, {
			queryLocalFonts: vi.fn().mockRejectedValue(new Error("denied")),
		});
		const info = vi.spyOn(console, "info").mockImplementation(() => undefined);

		const options = await loadAvailableFontOptions();

		expect(options.slice(0, 2).map((option) => option.value)).toEqual([
			"Inter",
			"System",
		]);
		expect(info).toHaveBeenCalledOnce();
	});

	it("quotes custom family names and chooses a semantic fallback", () => {
		expect(getFontOption('Writer "Display" Serif')).toMatchObject({
			stack: '"Writer \\"Display\\" Serif", serif',
			value: 'Writer "Display" Serif',
		});
	});

	it("applies separate UI and code fonts to every application root", () => {
		applyFontsToDocument("System", "Consolas");

		const root = document.documentElement;
		expect(root.dataset.font).toBe("System");
		expect(root.dataset.codeFont).toBe("Consolas");
		expect(root.style.getPropertyValue("--font-mono")).toBe(
			'"Consolas", monospace',
		);
		expect(document.body.style.fontFamily).toContain("system-ui");
		expect(document.getElementById("root")?.style.fontFamily).toContain(
			"system-ui",
		);
	});
});
