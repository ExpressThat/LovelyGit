import { describe, expect, it } from "vitest";
import type { RepositoryWorktreeItem } from "@/generated/types";
import { filterWorktrees } from "./WorktreeSection";

describe("filterWorktrees", () => {
	it("matches branch name, path, and lock reason", () => {
		const worktrees = [
			worktree("C:/repo", "main"),
			worktree("C:/repo-linked", "feature/search", "maintenance"),
		];

		expect(filterWorktrees(worktrees, "feature")).toEqual([worktrees[1]]);
		expect(filterWorktrees(worktrees, "repo")).toEqual(worktrees);
		expect(filterWorktrees(worktrees, "maintenance")).toEqual([worktrees[1]]);
		expect(filterWorktrees(worktrees, "missing")).toEqual([]);
	});
});

function worktree(
	path: string,
	branchName: string,
	lockReason = "",
): RepositoryWorktreeItem {
	return {
		branchName,
		isCurrent: path === "C:/repo",
		isLocked: lockReason.length > 0,
		lockReason,
		path,
	};
}
