// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { CommitOperationDialog } from "./CommitOperationDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({
	toast: {
		error: vi.fn(),
		loading: vi.fn(() => "toast"),
		success: vi.fn(),
		warning: vi.fn(),
	},
}));

describe("CommitOperationDialog", () => {
	beforeEach(() => vi.clearAllMocks());

	it("submits the displayed cherry-pick order as one sequencer operation", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			isCompleted: true,
			message: null,
			operation: null,
		});
		const onOpenChange = vi.fn();
		const commits = [row("a", "Oldest"), row("b", "Newest")];
		renderDialog({ commits, onOpenChange });

		expect(screen.getByText("Cherry-pick 2 commits on main?")).toBeVisible();
		expect(screen.getByText("Oldest")).toBeVisible();
		expect(screen.getByText("Newest")).toBeVisible();
		await user.click(screen.getByRole("button", { name: "Cherry-pick" }));

		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			expect.objectContaining({
				arguments: {
					commitHashes: commits.map((commit) => commit.commit.hash),
					repositoryId: "repo",
				},
				commandType: "CherryPickCommit",
			}),
			expect.any(Object),
		);
		await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(null));
	});

	it("opens conflict resolution and remains retryable after a failure", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("operation failed"))
			.mockResolvedValueOnce({
				isCompleted: false,
				message: "resolve conflicts",
				operation: "Revert",
			});
		const onOpenWorkingChanges = vi.fn();
		renderDialog({
			commits: [row("a", "Newest"), row("b", "Oldest")],
			mode: "revert",
			onOpenWorkingChanges,
		});

		const action = screen.getByRole("button", { name: "Revert" });
		await user.click(action);
		await waitFor(() => expect(action).toBeEnabled());
		await user.click(action);

		await waitFor(() => expect(onOpenWorkingChanges).toHaveBeenCalledOnce());
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(2);
	});

	it("bounds the maximum selected commit preview", () => {
		const view = renderDialog({
			commits: Array.from({ length: 100 }, (_, index) =>
				row(index.toString(16).padStart(2, "0"), `Commit ${index}`),
			),
		});

		expect(
			view.container.ownerDocument.querySelector(
				"[data-commit-operation-list='virtual']",
			),
		).toBeInTheDocument();
		expect(
			view.container.ownerDocument.querySelectorAll(
				"[data-commit-operation-row]",
			).length,
		).toBeLessThanOrEqual(8);
		expect(screen.getByText("Commit 0")).toBeVisible();
		expect(screen.queryByText("Commit 99")).toBeNull();
	});
});

function renderDialog(
	overrides: Partial<Parameters<typeof CommitOperationDialog>[0]> = {},
) {
	return render(
		<CommitOperationDialog
			commits={[row("a", "Commit")]}
			currentBranchName="main"
			mode="cherry-pick"
			onOpenChange={vi.fn()}
			onOpenWorkingChanges={vi.fn()}
			onRepositoryChanged={vi.fn()}
			repositoryId="repo"
			{...overrides}
		/>,
	);
}

function row(digit: string, message: string) {
	return { commit: { hash: digit.repeat(40), message } } as CommitGraphRow;
}
