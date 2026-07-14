import { beforeEach, describe, expect, it } from "vitest";
import type { WorkingTreeChangesResponse } from "@/generated/types";
import {
	clearWorkingTreeChangesCache,
	getCachedWorkingTreeChanges,
	setCachedWorkingTreeChanges,
} from "./workingTreeChangesCache";

describe("workingTreeChangesCache", () => {
	beforeEach(clearWorkingTreeChangesCache);

	it("retains bounded results for instant repository restoration", () => {
		const changes = response(500);
		expect(setCachedWorkingTreeChanges("repo", changes)).toBe(true);
		expect(getCachedWorkingTreeChanges("repo")).toBe(changes);
	});

	it("does not retain oversized working-tree lists", () => {
		expect(setCachedWorkingTreeChanges("repo", response(501))).toBe(false);
		expect(getCachedWorkingTreeChanges("repo")).toBeNull();
	});

	it("evicts least-recently-used results to bound total files", () => {
		setCachedWorkingTreeChanges("first", response(500));
		setCachedWorkingTreeChanges("second", response(500));
		getCachedWorkingTreeChanges("first");
		setCachedWorkingTreeChanges("third", response(1));

		expect(getCachedWorkingTreeChanges("first")).not.toBeNull();
		expect(getCachedWorkingTreeChanges("second")).toBeNull();
		expect(getCachedWorkingTreeChanges("third")).not.toBeNull();
	});
});

function response(totalCount: number): WorkingTreeChangesResponse {
	return {
		isComplete: true,
		staged: Array.from({ length: totalCount }, (_, index) => ({
			additions: 1,
			deletions: 0,
			group: "Staged",
			isBinary: false,
			oldPath: null,
			path: `file-${index}`,
			status: "Modified",
		})),
		unstaged: [],
		untracked: [],
		unmerged: [],
		totalCount,
	};
}
