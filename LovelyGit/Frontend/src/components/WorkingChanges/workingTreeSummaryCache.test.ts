import { beforeEach, describe, expect, it } from "vitest";
import {
	cacheCompleteWorkingTreeSummary,
	clearWorkingTreeSummaryCache,
	getCachedWorkingTreeSummary,
	invalidateWorkingTreeSummary,
	setCachedWorkingTreeSummary,
} from "./workingTreeSummaryCache";

describe("workingTreeSummaryCache", () => {
	beforeEach(clearWorkingTreeSummaryCache);

	it("stores and invalidates repository summaries", () => {
		setCachedWorkingTreeSummary("repo", {
			hasChanges: true,
			isComplete: false,
			shouldPreloadChanges: true,
			totalCount: 3,
		});
		expect(getCachedWorkingTreeSummary("repo")?.totalCount).toBe(3);

		invalidateWorkingTreeSummary("repo");

		expect(getCachedWorkingTreeSummary("repo")).toBeNull();
	});

	it("stores complete results without retaining file lists", () => {
		cacheCompleteWorkingTreeSummary("repo", 2);

		expect(getCachedWorkingTreeSummary("repo")).toEqual({
			hasChanges: true,
			isComplete: true,
			shouldPreloadChanges: true,
			totalCount: 2,
		});
	});

	it("evicts the least recently used repository", () => {
		for (const [index, repositoryId] of ["a", "b", "c", "d"].entries()) {
			cacheCompleteWorkingTreeSummary(repositoryId, index);
		}
		expect(getCachedWorkingTreeSummary("a")?.totalCount).toBe(0);
		cacheCompleteWorkingTreeSummary("e", 5);

		expect(getCachedWorkingTreeSummary("b")).toBeNull();
		expect(getCachedWorkingTreeSummary("a")?.totalCount).toBe(0);
	});
});
