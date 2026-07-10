// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { RepositoryWorktreeItem } from "@/generated/types";
import { CreateWorktreeDialog } from "./CreateWorktreeDialog";
import { LockWorktreeDialog } from "./LockWorktreeDialog";
import { RemoveWorktreeDialog } from "./RemoveWorktreeDialog";

describe("CreateWorktreeDialog", () => {
	it("chooses an empty folder and creates the worktree", async () => {
		const user = userEvent.setup();
		const onCreate = vi.fn();
		render(
			<CreateWorktreeDialog
				branchName="feature/demo"
				existingWorktree={null}
				isBusy={false}
				onChooseDestination={async () => "C:/repo-demo"}
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
				existingWorktree={worktree}
				isBusy={false}
				onChooseDestination={async () => null}
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
