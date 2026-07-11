import { describe, expect, it } from "vitest";
import { isCommandPaletteShortcut } from "./commandPaletteShortcut";

describe("isCommandPaletteShortcut", () => {
	it("accepts Ctrl or Meta K without conflicting modifiers", () => {
		expect(
			isCommandPaletteShortcut(
				new KeyboardEvent("keydown", { ctrlKey: true, key: "k" }),
			),
		).toBe(true);
		expect(
			isCommandPaletteShortcut(
				new KeyboardEvent("keydown", { key: "K", metaKey: true }),
			),
		).toBe(true);
		expect(
			isCommandPaletteShortcut(
				new KeyboardEvent("keydown", { altKey: true, ctrlKey: true, key: "k" }),
			),
		).toBe(false);
	});
});
// @vitest-environment jsdom
