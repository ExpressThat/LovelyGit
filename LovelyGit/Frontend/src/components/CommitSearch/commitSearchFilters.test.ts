import { describe, expect, it } from "vitest";
import {
	hasCommitSearchFilter,
	isCommitSearchDateRangeValid,
	toSearchBoundaries,
} from "./commitSearchFilters";

describe("commit search filters", () => {
	it("uses UTC day boundaries and makes the until day inclusive", () => {
		expect(
			toSearchBoundaries({
				afterDate: "2024-06-01",
				author: "",
				beforeDate: "2024-06-30",
				scope: "",
			}),
		).toEqual({
			afterUnixSeconds: 1717200000,
			beforeUnixSeconds: 1719792000,
		});
	});

	it("recognizes filter-only searches and invalid reversed ranges", () => {
		expect(
			hasCommitSearchFilter({
				author: "Alice",
				afterDate: "",
				beforeDate: "",
				scope: "",
			}),
		).toBe(true);
		expect(
			isCommitSearchDateRangeValid({
				afterDate: "2024-07-02",
				author: "",
				beforeDate: "2024-07-01",
				scope: "",
			}),
		).toBe(false);
	});
});
