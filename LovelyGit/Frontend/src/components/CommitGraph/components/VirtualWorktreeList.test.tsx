// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { RepositoryWorktreeItem } from "@/generated/types";
import type { WorktreeMutationController } from "../hooks/useWorktreeMutations";
import { VirtualWorktreeList } from "./VirtualWorktreeList";

const virtualizerInput = vi.hoisted(() => vi.fn());

vi.mock("@tanstack/react-virtual", () => ({
	useVirtualizer: (input: { count: number }) => {
		virtualizerInput(input);
		const renderedCount = Math.min(input.count, 8);
		return {
			getTotalSize: () => input.count * 38,
			getVirtualItems: () =>
				Array.from({ length: renderedCount }, (_, index) => ({
					index,
					start: index * 38,
				})),
		};
	},
}));

describe("VirtualWorktreeList", () => {
	it("mounts only the visible window for a large worktree collection", () => {
		const worktrees = Array.from({ length: 500 }, (_, index) =>
			worktree(`C:/linked/wt-${index}`),
		);
		render(
			<VirtualWorktreeList
				controller={{ busyPath: null } as WorktreeMutationController}
				worktrees={worktrees}
			/>,
		);

		expect(virtualizerInput).toHaveBeenCalledWith(
			expect.objectContaining({ count: 500, overscan: 6 }),
		);
		expect(screen.getAllByRole("button", { name: /worktree at/ })).toHaveLength(
			8,
		);
		expect(screen.queryByText("wt-499")).not.toBeInTheDocument();
	});
});

function worktree(path: string): RepositoryWorktreeItem {
	return {
		branchName: null,
		isCurrent: false,
		isLocked: false,
		lockReason: "",
		path,
	};
}
