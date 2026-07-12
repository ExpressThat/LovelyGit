import { describe, expect, it } from "vitest";
import { commitGraphContentHeight } from "./useCommitGraphViewport";

describe("commitGraphContentHeight", () => {
	it("keeps a stable placeholder before the first page arrives", () => {
		expect(commitGraphContentHeight(0)).toBe(3_080);
	});

	it("uses the exact fixed-row height for loaded graph ranges", () => {
		expect(commitGraphContentHeight(768)).toBe(16_896);
	});
});
