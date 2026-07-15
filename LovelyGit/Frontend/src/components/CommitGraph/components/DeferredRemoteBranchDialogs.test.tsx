// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	DeferredCheckoutRemoteBranchDialog,
	DeferredDeleteRemoteBranchDialog,
} from "./DeferredRemoteBranchDialogs";

vi.mock("./RemoteBranchDialogs", () => ({
	CheckoutRemoteBranchDialog: ({
		remoteBranchName,
	}: {
		remoteBranchName: string;
	}) => <div>Checkout {remoteBranchName} loaded</div>,
	DeleteRemoteBranchDialog: ({
		remoteBranchName,
	}: {
		remoteBranchName: string;
	}) => <div>Delete {remoteBranchName} loaded</div>,
}));

describe("DeferredRemoteBranchDialogs", () => {
	it("reveals remote checkout", async () => {
		render(
			<DeferredCheckoutRemoteBranchDialog
				existingBranchNames={["main"]}
				isBusy={false}
				onConfirm={vi.fn()}
				onOpenChange={vi.fn()}
				remoteBranchName="origin/topic"
			/>,
		);
		expect(
			await screen.findByText("Checkout origin/topic loaded"),
		).toBeVisible();
	});

	it("reveals remote deletion", async () => {
		render(
			<DeferredDeleteRemoteBranchDialog
				isBusy={false}
				onConfirm={vi.fn()}
				onOpenChange={vi.fn()}
				remoteBranchName="origin/topic"
			/>,
		);
		expect(await screen.findByText("Delete origin/topic loaded")).toBeVisible();
	});
});
