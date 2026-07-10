// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { BranchUpstreamDialog } from "./BranchUpstreamDialog";

describe("BranchUpstreamDialog", () => {
	it("shows and unsets the current upstream", async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		renderDialog({ currentUpstream: "origin/main", onConfirm });

		expect(screen.getByText("Currently tracking")).toBeInTheDocument();
		expect(screen.getAllByText("origin/main").length).toBeGreaterThan(0);
		await user.click(screen.getByRole("button", { name: "Unset upstream" }));
		expect(onConfirm).toHaveBeenCalledWith(null);
	});

	it("selects and sets a remote branch", async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		renderDialog({ currentUpstream: null, onConfirm });

		await user.click(screen.getByRole("combobox", { name: "Remote branch" }));
		await user.click(
			await screen.findByRole("option", { name: "origin/release" }),
		);
		await user.click(screen.getByRole("button", { name: "Set upstream" }));
		expect(onConfirm).toHaveBeenCalledWith("origin/release");
	});

	it("explains when no remote branches exist and disables work while busy", () => {
		renderDialog({ currentUpstream: null, isBusy: true, remoteBranches: [] });

		expect(screen.getByText(/No remote branches are available/)).toBeVisible();
		expect(screen.getByRole("button", { name: "Saving" })).toBeDisabled();
	});
});

function renderDialog({
	currentUpstream,
	isBusy = false,
	onConfirm = vi.fn(),
	remoteBranches = ["origin/main", "origin/release"],
}: {
	currentUpstream: string | null;
	isBusy?: boolean;
	onConfirm?: (upstreamName: string | null) => void;
	remoteBranches?: string[];
}) {
	return render(
		<BranchUpstreamDialog
			branchName="feature/demo"
			currentUpstream={currentUpstream}
			isBusy={isBusy}
			onConfirm={onConfirm}
			onOpenChange={vi.fn()}
			remoteBranches={remoteBranches}
		/>,
	);
}
