import { describe, expect, it } from "vitest";
import { clampContextLines } from "./DiffToolbarControls";

describe("clampContextLines", () => {
	it("keeps context lines within the supported range", () => {
		expect(clampContextLines(-4)).toBe(0);
		expect(clampContextLines(3.8)).toBe(3);
		expect(clampContextLines(160)).toBe(99);
	});
});
