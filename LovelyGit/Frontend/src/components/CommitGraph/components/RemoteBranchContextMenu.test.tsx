// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { BranchAction } from "./BranchContextMenu";
import { RemoteBranchContextMenu } from "./RemoteBranchContextMenu";

describe("RemoteBranchContextMenu", () => {
	it("routes checkout, comparison, and deletion with the selected remote ref", async () => {
		const user = userEvent.setup();
		const onAction = vi.fn<(action: BranchAction, branch: string) => void>();
		renderMenu({ onAction });

		openMenu();
		await user.click(await screen.findByText("Check out as local branch…"));
		expect(onAction).toHaveBeenCalledWith("checkoutRemote", "origin/feature");

		openMenu();
		await user.click(
			await screen.findByText("Compare main with origin/feature…"),
		);
		expect(onAction).toHaveBeenCalledWith("compare", "origin/feature");

		openMenu();
		await user.click(await screen.findByText("Delete from remote…"));
		expect(onAction).toHaveBeenCalledWith("deleteRemote", "origin/feature");
	});

	it("labels integration direction and disables it without a current branch", async () => {
		const onIntegrate =
			vi.fn<(mode: BranchIntegrationMode, branch: string) => void>();
		const view = renderMenu({ onIntegrate });
		openMenu();
		await userEvent.click(
			await screen.findByText("Merge origin/feature into main"),
		);
		expect(onIntegrate).toHaveBeenCalledWith("merge", "origin/feature");

		view.rerender(menu(null, vi.fn(), onIntegrate));
		openMenu();
		expect(
			(
				await screen.findByText("Merge origin/feature into current branch")
			).closest('[role="menuitem"]'),
		).toHaveAttribute("aria-disabled", "true");
	});
});

function openMenu() {
	fireEvent.contextMenu(screen.getByRole("button", { name: "remote ref" }));
}

function renderMenu({
	onAction = vi.fn(),
	onIntegrate = vi.fn(),
}: {
	onAction?: (action: BranchAction, branch: string) => void;
	onIntegrate?: (mode: BranchIntegrationMode, branch: string) => void;
}) {
	return render(menu("main", onAction, onIntegrate));
}

function menu(
	currentBranchName: string | null,
	onAction: (action: BranchAction, branch: string) => void,
	onIntegrateBranch: (mode: BranchIntegrationMode, branch: string) => void,
) {
	return (
		<RemoteBranchContextMenu
			currentBranchName={currentBranchName}
			disabled={false}
			onAction={onAction}
			onIntegrateBranch={onIntegrateBranch}
			remoteBranchName="origin/feature"
		>
			<button type="button">remote ref</button>
		</RemoteBranchContextMenu>
	);
}
