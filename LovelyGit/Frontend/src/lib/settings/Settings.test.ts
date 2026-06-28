import { describe, expect, it } from "vitest";
import { DEFAULT_SETTINGS } from "./Settings";

describe("DEFAULT_SETTINGS", () => {
	it("keeps whitespace-only diff changes visible by default", () => {
		expect(DEFAULT_SETTINGS.CommitDiffIgnoreWhitespace).toBe(false);
	});

	it("starts conflict file navigation in path mode", () => {
		expect(DEFAULT_SETTINGS.ConflictFileViewMode).toBe("Path");
	});
});
