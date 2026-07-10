// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import { type BranchAction, BranchContextMenu } from "./BranchContextMenu";

type BranchActionCallback = (action: BranchAction, branchName: string) => void;
type IntegrateBranchCallback = (
	mode: BranchIntegrationMode,
	branchName: string,
) => void;

describe("BranchContextMenu", () => {
	it("describes branch-aware actions and protects the current branch", async () => {
		renderMenu({ branchName: "main", currentBranchName: "main" });

		fireEvent.contextMenu(screen.getByRole("button", { name: "main ref" }));

		expect(await screen.findByRole("menu")).toBeVisible();
		expect(
			screen.getByText("Merge main into main").closest('[role="menuitem"]'),
		).toHaveAttribute("aria-disabled", "true");
		expect(
			screen.getByText("Rebase main onto main").closest('[role="menuitem"]'),
		).toHaveAttribute("aria-disabled", "true");
		expect(
			screen.getByText("Switch to main").closest('[role="menuitem"]'),
		).toHaveAttribute("aria-disabled", "true");
		expect(
			screen.getByText("Delete local branch…").closest('[role="menuitem"]'),
		).toHaveAttribute("aria-disabled", "true");
	});

	it("routes push, upstream, and integration actions with the selected branch", async () => {
		const user = userEvent.setup();
		const onAction = vi.fn<BranchActionCallback>();
		const onIntegrateBranch = vi.fn<IntegrateBranchCallback>();
		renderMenu({
			branchName: "feature/demo",
			currentBranchName: "main",
			onAction,
			onIntegrateBranch,
		});

		fireEvent.contextMenu(
			screen.getByRole("button", { name: "feature/demo ref" }),
		);
		await user.click(await screen.findByText("Push to origin"));
		expect(onAction).toHaveBeenCalledWith("push", "feature/demo");

		fireEvent.contextMenu(
			screen.getByRole("button", { name: "feature/demo ref" }),
		);
		await user.click(await screen.findByText("Manage upstream…"));
		expect(onAction).toHaveBeenCalledWith("upstream", "feature/demo");

		fireEvent.contextMenu(
			screen.getByRole("button", { name: "feature/demo ref" }),
		);
		await user.click(await screen.findByText("View reflog…"));
		expect(onAction).toHaveBeenCalledWith("reflog", "feature/demo");

		fireEvent.contextMenu(
			screen.getByRole("button", { name: "feature/demo ref" }),
		);
		await user.click(await screen.findByText("Create linked worktree…"));
		expect(onAction).toHaveBeenCalledWith("worktree", "feature/demo");

		fireEvent.contextMenu(
			screen.getByRole("button", { name: "feature/demo ref" }),
		);
		await user.click(await screen.findByText("Merge feature/demo into main"));
		expect(onIntegrateBranch).toHaveBeenCalledWith("merge", "feature/demo");
	});
});

function renderMenu({
	branchName,
	currentBranchName,
	onAction = vi.fn<BranchActionCallback>(),
	onIntegrateBranch = vi.fn<IntegrateBranchCallback>(),
}: {
	branchName: string;
	currentBranchName: string;
	onAction?: BranchActionCallback;
	onIntegrateBranch?: IntegrateBranchCallback;
}) {
	return render(
		<BranchContextMenu
			branchName={branchName}
			currentBranchName={currentBranchName}
			disabled={false}
			onAction={onAction}
			onIntegrateBranch={onIntegrateBranch}
			remoteName="origin"
		>
			<button type="button">{branchName} ref</button>
		</BranchContextMenu>,
	);
}
