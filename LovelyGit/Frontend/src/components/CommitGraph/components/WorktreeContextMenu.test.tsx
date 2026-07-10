// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type {
	RepositoryWorktreeItem,
	WorktreeMutationAction,
} from "@/generated/types";
import { WorktreeContextMenu } from "./WorktreeContextMenu";

describe("WorktreeContextMenu", () => {
	it("routes linked worktree actions", async () => {
		const user = userEvent.setup();
		const onAction = vi.fn();
		const worktree = linkedWorktree();
		renderMenu(worktree, onAction);

		fireEvent.contextMenu(screen.getByRole("button", { name: "worktree row" }));
		await user.click(await screen.findByText("Lock worktree…"));

		expect(onAction).toHaveBeenCalledWith("Lock", worktree);
	});

	it("protects the current worktree while keeping access actions available", async () => {
		const worktree = { ...linkedWorktree(), isCurrent: true };
		renderMenu(worktree, vi.fn());

		fireEvent.contextMenu(screen.getByRole("button", { name: "worktree row" }));

		expect(await screen.findByText("Open in LovelyGit")).toHaveAttribute(
			"aria-disabled",
			"true",
		);
		expect(screen.getByText("Lock worktree…")).toHaveAttribute(
			"aria-disabled",
			"true",
		);
		expect(screen.getByText("Remove worktree…")).toHaveAttribute(
			"aria-disabled",
			"true",
		);
		expect(screen.getByText("Reveal in file manager")).not.toHaveAttribute(
			"aria-disabled",
		);
	});
});

function renderMenu(
	worktree: RepositoryWorktreeItem,
	onAction: (
		action: WorktreeMutationAction,
		worktree: RepositoryWorktreeItem,
	) => void,
) {
	return render(
		<WorktreeContextMenu
			disabled={false}
			onAction={onAction}
			worktree={worktree}
		>
			<button type="button">worktree row</button>
		</WorktreeContextMenu>,
	);
}

function linkedWorktree(): RepositoryWorktreeItem {
	return {
		branchName: "feature/worktree",
		isCurrent: false,
		isLocked: false,
		lockReason: "",
		path: "C:/repo-worktree",
	};
}
