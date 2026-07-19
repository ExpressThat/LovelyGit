// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RepositoryOperationCommandResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { BranchIntegrationDialog } from "./BranchIntegrationDialog";

const toast = vi.hoisted(() => ({
	error: vi.fn(),
	loading: vi.fn(() => "toast-1"),
	success: vi.fn(),
	warning: vi.fn(),
}));

vi.mock("sonner", () => ({ toast }));
vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

const send = vi.mocked(sendRequestWithResponse);

describe("BranchIntegrationDialog", () => {
	beforeEach(() => {
		send.mockReset();
		for (const method of Object.values(toast)) {
			method.mockClear();
		}
	});

	it("protects the merge action while pending and reports completion", async () => {
		const user = userEvent.setup();
		const pending = deferred<RepositoryOperationCommandResponse>();
		const callbacks = renderDialog("merge");
		send.mockReturnValueOnce(pending.promise);

		await user.click(screen.getByRole("button", { name: "Merge" }));
		expect(send).toHaveBeenCalledWith(
			{
				arguments: { branchName: "incoming", repositoryId: "repo-1" },
				commandType: "MergeBranchIntoCurrent",
			},
			{ timeoutMs: 120_000 },
		);
		expect(
			screen.getByRole("button", { name: "Merge running" }),
		).toBeDisabled();

		pending.resolve({ isCompleted: true, message: null, operation: null });
		await waitFor(() =>
			expect(callbacks.onRepositoryChanged).toHaveBeenCalled(),
		);
		expect(callbacks.onOpenChange).toHaveBeenCalledWith(null);
		expect(callbacks.onOpenWorkingChanges).not.toHaveBeenCalled();
		expect(toast.success).toHaveBeenCalledWith("Merged incoming into main", {
			id: "toast-1",
		});
	});

	it("opens working changes when Git pauses for conflicts", async () => {
		const user = userEvent.setup();
		const callbacks = renderDialog("merge");
		send.mockResolvedValueOnce({
			isCompleted: false,
			message: "Resolve both conflicted files.",
			operation: "Merge",
		});

		await user.click(screen.getByRole("button", { name: "Merge" }));
		await waitFor(() =>
			expect(callbacks.onOpenWorkingChanges).toHaveBeenCalledOnce(),
		);
		expect(callbacks.onOpenChange).toHaveBeenCalledWith(null);
		expect(callbacks.onRepositoryChanged).toHaveBeenCalledOnce();
		expect(toast.warning).toHaveBeenCalledWith("Merge paused for conflicts", {
			description: "Resolve both conflicted files.",
			id: "toast-1",
		});
		expect(screen.getByRole("button", { name: "Merge" })).toBeEnabled();
	});

	it("re-enables a failed merge and allows a successful retry", async () => {
		const user = userEvent.setup();
		const callbacks = renderDialog("merge");
		send
			.mockRejectedValueOnce(new Error("Local changes would be overwritten."))
			.mockResolvedValueOnce({
				isCompleted: true,
				message: null,
				operation: null,
			});

		await user.click(screen.getByRole("button", { name: "Merge" }));
		await waitFor(() =>
			expect(screen.getByRole("button", { name: "Merge" })).toBeEnabled(),
		);
		expect(toast.error).toHaveBeenCalledWith(
			"Local changes would be overwritten.",
			{ id: "toast-1" },
		);
		expect(callbacks.onOpenChange).not.toHaveBeenCalled();

		await user.click(screen.getByRole("button", { name: "Merge" }));
		await waitFor(() =>
			expect(callbacks.onOpenChange).toHaveBeenCalledWith(null),
		);
		expect(send).toHaveBeenCalledTimes(2);
	});

	it("describes and dispatches a fixed-target rebase", async () => {
		const user = userEvent.setup();
		renderDialog("rebase");
		send.mockResolvedValueOnce({
			isCompleted: true,
			message: null,
			operation: null,
		});

		expect(
			screen.getByRole("heading", { name: "Rebase main onto incoming?" }),
		).toBeVisible();
		await user.click(screen.getByRole("button", { name: "Rebase" }));
		await waitFor(() => expect(send).toHaveBeenCalled());
		expect(send.mock.calls[0]?.[0]).toEqual({
			arguments: { branchName: "incoming", repositoryId: "repo-1" },
			commandType: "RebaseCurrentBranchOntoBranch",
		});
	});
});

function renderDialog(mode: "merge" | "rebase") {
	const callbacks = {
		onOpenChange: vi.fn(),
		onOpenWorkingChanges: vi.fn(),
		onRepositoryChanged: vi.fn(),
	};
	render(
		<BranchIntegrationDialog
			branches={[]}
			currentBranchName="main"
			mode={mode}
			{...callbacks}
			repositoryId="repo-1"
			targetBranchName="incoming"
		/>,
	);
	return callbacks;
}

function deferred<T>() {
	let resolve!: (value: T) => void;
	let reject!: (reason?: unknown) => void;
	const promise = new Promise<T>((resolvePromise, rejectPromise) => {
		resolve = resolvePromise;
		reject = rejectPromise;
	});
	return { promise, reject, resolve };
}
