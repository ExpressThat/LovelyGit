// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DeleteBranchDialog } from "./DeleteBranchDialog";
import { RenameBranchDialog } from "./RenameBranchDialog";

describe("RenameBranchDialog", () => {
	it("rejects duplicate names and submits a new branch name", async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		render(
			<RenameBranchDialog
				branchName="feature/old"
				existingBranchNames={["main", "feature/old", "feature/existing"]}
				isBusy={false}
				onConfirm={onConfirm}
				onOpenChange={vi.fn()}
			/>,
		);

		const input = screen.getByRole("textbox", { name: "New branch name" });
		await user.clear(input);
		await user.type(input, "feature/existing");
		expect(
			screen.getByText("A local branch with this name already exists."),
		).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: "Rename branch" }),
		).toBeDisabled();

		await user.clear(input);
		await user.type(input, "feature/new");
		await user.click(screen.getByRole("button", { name: "Rename branch" }));
		expect(onConfirm).toHaveBeenCalledWith("feature/new");
	});
});

describe("DeleteBranchDialog", () => {
	it("requires an explicit force choice for destructive deletion", async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		render(
			<DeleteBranchDialog
				branchName="feature/unmerged"
				isBusy={false}
				onConfirm={onConfirm}
				onOpenChange={vi.fn()}
			/>,
		);

		expect(screen.getByRole("button", { name: "Delete branch" })).toBeVisible();
		await user.click(
			screen.getByRole("button", { name: "Force delete unmerged branch" }),
		);
		expect(await screen.findByText(/difficult to recover/i)).toBeVisible();
		await user.click(
			screen.getByRole("button", { name: "Force delete branch" }),
		);
		expect(onConfirm).toHaveBeenCalledWith(true);
	});
});
