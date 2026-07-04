import { describe, expect, it } from "vitest";
import { DEFAULT_SETTINGS } from "./Settings";

describe("DEFAULT_SETTINGS", () => {
	it("keeps whitespace-only diff changes visible by default", () => {
		expect(DEFAULT_SETTINGS.CommitDiffIgnoreWhitespace).toBe(false);
	});
});
