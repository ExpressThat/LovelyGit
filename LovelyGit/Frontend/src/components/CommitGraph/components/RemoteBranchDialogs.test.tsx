// @vitest-environment jsdom

import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
	CheckoutRemoteBranchDialog,
	DeleteRemoteBranchDialog,
} from "./RemoteBranchDialogs";

describe("CheckoutRemoteBranchDialog", () => {
	it("suggests the tracking name and rejects an existing local branch", async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		render(
			<CheckoutRemoteBranchDialog
				existingBranchNames={["feature/demo"]}
				isBusy={false}
				onConfirm={onConfirm}
				onOpenChange={vi.fn()}
				remoteBranchName="origin/feature/demo"
			/>,
		);

		const input = screen.getByLabelText("Local branch name");
		expect(input).toHaveValue("feature/demo");
		expect(
			screen.getByRole("button", { name: "Create tracking branch" }),
		).toBeDisabled();

		await user.clear(input);
		await user.type(input, "feature/local");
		await user.click(
			screen.getByRole("button", { name: "Create tracking branch" }),
		);
		expect(onConfirm).toHaveBeenCalledWith("feature/local");
	});

	it("accepts the native input event emitted by the desktop WebView", () => {
		const onConfirm = vi.fn();
		render(
			<CheckoutRemoteBranchDialog
				existingBranchNames={["main"]}
				isBusy={false}
				onConfirm={onConfirm}
				onOpenChange={vi.fn()}
				remoteBranchName="origin/main"
			/>,
		);

		fireEvent.input(screen.getByLabelText("Local branch name"), {
			target: { value: "perf/remote-checkout" },
		});
		fireEvent.click(
			screen.getByRole("button", { name: "Create tracking branch" }),
		);

		expect(onConfirm).toHaveBeenCalledWith("perf/remote-checkout");
	});
});

describe("DeleteRemoteBranchDialog", () => {
	it("makes shared deletion explicit and requires confirmation", async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		render(
			<DeleteRemoteBranchDialog
				isBusy={false}
				onConfirm={onConfirm}
				onOpenChange={vi.fn()}
				remoteBranchName="origin/feature/demo"
			/>,
		);

		expect(
			screen.getByText(/shared remote for every collaborator/i),
		).toBeVisible();
		await user.click(
			screen.getByRole("button", { name: "Delete remote branch" }),
		);
		await waitFor(() => expect(onConfirm).toHaveBeenCalledOnce());
	});
});
