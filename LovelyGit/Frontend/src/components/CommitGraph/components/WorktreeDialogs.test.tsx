// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { RepositoryWorktreeItem } from "@/generated/types";
import type { WorktreeMutationController } from "../hooks/useWorktreeMutations";
import { CreateWorktreeDialog } from "./CreateWorktreeDialog";
import { LockWorktreeDialog } from "./LockWorktreeDialog";
import { RemoveWorktreeDialog } from "./RemoveWorktreeDialog";
import { WorktreeManagementDialogs } from "./WorktreeManagementDialogs";

describe("CreateWorktreeDialog", () => {
	it("lazy-loads branch selection when opened from the sidebar", async () => {
		render(
			<WorktreeManagementDialogs
				controller={
					{
						busyPath: null,
						createBranchName: "",
						lockTarget: null,
						removeTarget: null,
						setCreateBranchName: vi.fn(),
					} as unknown as WorktreeMutationController
				}
				repositoryRefs={{
					branchUpstreams: [],
					compactRefsGzipBase64: null,
					currentBranchName: "main",
					refs: [localRef("main")],
					remotePrefixes: [],
					stashes: [],
					worktrees: [linkedWorktree()],
				}}
			/>,
		);

		expect(
			await screen.findByRole("dialog", { name: "Create worktree" }),
		).toBeInTheDocument();
	});

	it("selects a local branch when opened from the worktrees section", async () => {
		const user = userEvent.setup();
		const onBranchChange = vi.fn();
		render(
			<CreateWorktreeDialog
				branchName=""
				branches={["main", "feature/demo"]}
				existingWorktree={null}
				isBusy={false}
				onBranchChange={onBranchChange}
				onChooseDestination={async () => null}
				onClose={vi.fn()}
				onCreate={vi.fn()}
				onOpenExisting={vi.fn()}
			/>,
		);

		await user.click(screen.getByRole("combobox", { name: "Worktree branch" }));
		await user.keyboard("{ArrowDown}{ArrowDown}{Enter}");
		expect(onBranchChange).toHaveBeenCalledWith("feature/demo");
	});

	it("chooses an empty folder and creates the worktree", async () => {
		const user = userEvent.setup();
		const onCreate = vi.fn();
		render(
			<CreateWorktreeDialog
				branchName="feature/demo"
				branches={["main", "feature/demo"]}
				existingWorktree={null}
				isBusy={false}
				onChooseDestination={async () => "C:/repo-demo"}
				onBranchChange={vi.fn()}
				onClose={vi.fn()}
				onCreate={onCreate}
				onOpenExisting={vi.fn()}
			/>,
		);

		await user.click(
			screen.getByRole("button", { name: "Browse for worktree destination" }),
		);
		expect(
			screen.getByRole("textbox", { name: "Worktree destination" }),
		).toHaveValue("C:/repo-demo");
		await user.click(screen.getByRole("button", { name: "Create worktree" }));
		expect(onCreate).toHaveBeenCalledWith("C:/repo-demo");
	});

	it("offers to open a branch that already has a worktree", async () => {
		const user = userEvent.setup();
		const worktree = linkedWorktree();
		const onOpenExisting = vi.fn();
		render(
			<CreateWorktreeDialog
				branchName="feature/demo"
				branches={["feature/demo"]}
				existingWorktree={worktree}
				isBusy={false}
				onChooseDestination={async () => null}
				onBranchChange={vi.fn()}
				onClose={vi.fn()}
				onCreate={vi.fn()}
				onOpenExisting={onOpenExisting}
			/>,
		);

		await user.click(screen.getByRole("button", { name: "Open worktree" }));
		expect(onOpenExisting).toHaveBeenCalledWith(worktree);
	});
});

describe("Worktree lifecycle dialogs", () => {
	it("submits a lock reason", async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		render(
			<LockWorktreeDialog
				isBusy={false}
				onClose={vi.fn()}
				onConfirm={onConfirm}
				worktree={linkedWorktree()}
			/>,
		);
		await user.type(screen.getByRole("textbox"), "External drive");
		await user.click(screen.getByRole("button", { name: "Lock worktree" }));
		expect(onConfirm).toHaveBeenCalledWith("External drive");
	});

	it("requires an explicit force choice before destructive removal", async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		render(
			<RemoveWorktreeDialog
				isBusy={false}
				onClose={vi.fn()}
				onConfirm={onConfirm}
				worktree={linkedWorktree()}
			/>,
		);
		await user.click(
			screen.getByRole("checkbox", {
				name: "Force remove worktree with changes",
			}),
		);
		await user.click(
			screen.getByRole("button", { name: "Force remove worktree" }),
		);
		expect(onConfirm).toHaveBeenCalledWith(true);
	});
});

function linkedWorktree(): RepositoryWorktreeItem {
	return {
		branchName: "feature/demo",
		isCurrent: false,
		isLocked: false,
		lockReason: "",
		path: "C:/repo-demo",
	};
}

function localRef(name: string) {
	return {
		commitHash: "a".repeat(40),
		kind: "Local" as const,
		name,
		remoteUrl: null,
	};
}
