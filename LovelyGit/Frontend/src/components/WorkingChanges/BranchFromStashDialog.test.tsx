// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { RepositoryStashItem } from "@/generated/types";
import { BranchFromStashDialog } from "./BranchFromStashDialog";

const stash: RepositoryStashItem = {
	commitHash: "0123456789abcdef",
	createdAtUnixSeconds: 1_700_000_000,
	message: "WIP on main",
	selector: "stash@{0}",
};

describe("BranchFromStashDialog", () => {
	it("submits a trimmed new branch name", async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		render(
			<BranchFromStashDialog
				branchNames={["main"]}
				isBusy={false}
				onClose={vi.fn()}
				onConfirm={onConfirm}
				stash={stash}
			/>,
		);

		await user.type(screen.getByLabelText("Branch name"), " recover/work ");
		await user.click(
			screen.getByRole("button", { name: "Create and restore" }),
		);

		expect(onConfirm).toHaveBeenCalledWith("recover/work");
	});

	it("blocks an existing branch and keeps the dialog dismissible", async () => {
		const user = userEvent.setup();
		const onClose = vi.fn();
		const onConfirm = vi.fn();
		render(
			<BranchFromStashDialog
				branchNames={["main"]}
				isBusy={false}
				onClose={onClose}
				onConfirm={onConfirm}
				stash={stash}
			/>,
		);

		await user.type(screen.getByLabelText("Branch name"), "main");
		await waitFor(() =>
			expect(
				screen.getByText("A local branch with this name already exists."),
			).toBeVisible(),
		);
		expect(
			screen.getByRole("button", { name: "Create and restore" }),
		).toBeDisabled();
		await user.click(screen.getByRole("button", { name: "Close" }));
		expect(onClose).toHaveBeenCalledOnce();
		expect(onConfirm).not.toHaveBeenCalled();
	});
});
