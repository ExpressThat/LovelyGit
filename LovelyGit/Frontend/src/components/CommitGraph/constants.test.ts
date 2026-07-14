import { describe, expect, it } from "vitest";
import { OVERSCAN } from "./constants";

describe("commit graph virtualization", () => {
	it("keeps a small render buffer for smooth scrolling without mounting extra viewports", () => {
		expect(OVERSCAN).toBeGreaterThanOrEqual(4);
		expect(OVERSCAN).toBeLessThanOrEqual(12);
	});
});
