import { describe, expect, it } from "vitest";
import { copyScrollPosition } from "./ConflictScrollSync";

describe("copyScrollPosition", () => {
	it("syncs horizontal and vertical scroll positions", () => {
		const source = { scrollLeft: 120, scrollTop: 48 };
		const target = { scrollLeft: 0, scrollTop: 0 };

		copyScrollPosition(source, target);

		expect(target).toEqual(source);
	});

	it("ignores a missing target", () => {
		const source = { scrollLeft: 8, scrollTop: 16 };

		expect(() => copyScrollPosition(source, null)).not.toThrow();
	});
});
