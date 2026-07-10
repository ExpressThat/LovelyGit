// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { createElement } from "react";
import { describe, expect, it, vi } from "vitest";
import type { RepositoryWorktreeItem } from "@/generated/types";
import type { WorktreeMutationController } from "../hooks/useWorktreeMutations";
import { filterWorktrees, WorktreeSection } from "./WorktreeSection";

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

describe("WorktreeSection", () => {
	it("opens a linked worktree on double click", () => {
		const linked = worktree("C:/repo-linked", "feature/search");
		const manage = vi.fn();
		render(
			createElement(WorktreeSection, {
				controller: {
					busyPath: null,
					manage,
				} as unknown as WorktreeMutationController,
				query: "",
				worktrees: [linked],
			}),
		);

		fireEvent.doubleClick(
			screen.getByRole("button", {
				name: "feature/search worktree at C:/repo-linked",
			}),
		);
		expect(manage).toHaveBeenCalledWith("Open", linked);
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
