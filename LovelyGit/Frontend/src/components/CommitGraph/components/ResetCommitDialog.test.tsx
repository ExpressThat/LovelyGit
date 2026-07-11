// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { type CommitGraphRow, GitResetMode } from "@/generated/types";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { ResetCommitDialog } from "./ResetCommitDialog";

const mocks = vi.hoisted(() => ({
	sendRequestWithResponse: vi.fn(),
	toastError: vi.fn(),
}));

vi.mock("@/lib/commands", () => ({
	sendRequestWithResponse: mocks.sendRequestWithResponse,
}));
vi.mock("sonner", () => ({
	toast: {
		error: mocks.toastError,
		loading: vi.fn(() => "toast-id"),
		success: vi.fn(),
	},
}));

describe("ResetCommitDialog", () => {
	beforeEach(() => {
		mocks.sendRequestWithResponse.mockReset().mockResolvedValue(undefined);
		mocks.toastError.mockReset();
	});

	it("keeps the dialog state recoverable when reset fails", async () => {
		const user = userEvent.setup();
		const onRepositoryChanged = vi.fn();
		mocks.sendRequestWithResponse.mockRejectedValueOnce(
			new Error("Reset blocked by an active merge."),
		);
		renderDialog({ onOpenWorkingChanges: vi.fn(), onRepositoryChanged });

		await user.click(screen.getByRole("button", { name: "Mixed reset" }));

		await waitFor(() =>
			expect(mocks.toastError).toHaveBeenCalledWith(
				"Reset blocked by an active merge.",
				expect.any(Object),
			),
		);
		expect(onRepositoryChanged).not.toHaveBeenCalled();
		expect(screen.getByRole("button", { name: "Mixed reset" })).toBeEnabled();
	});

	it("defaults to mixed reset and opens the resulting working changes", async () => {
		const user = userEvent.setup();
		const onOpenWorkingChanges = vi.fn();
		renderDialog({ onOpenWorkingChanges });

		expect(
			screen.getByRole("button", { name: /^Mixed reset:/ }),
		).toHaveAttribute("aria-pressed", "true");
		await user.click(screen.getByRole("button", { name: "Mixed reset" }));

		expect(mocks.sendRequestWithResponse).toHaveBeenCalledWith(
			{
				arguments: {
					commitHash: commit.commit.hash,
					repositoryId: "repository-id",
					resetMode: GitResetMode.Mixed,
				},
				commandType: NativeMessageType.ResetCurrentBranchToCommit,
			},
			expect.any(Object),
		);
		expect(onOpenWorkingChanges).toHaveBeenCalledOnce();
	});

	it("requires the exact branch name before a hard reset", async () => {
		const user = userEvent.setup();
		const onOpenWorkingChanges = vi.fn();
		renderDialog({ onOpenWorkingChanges });

		await user.click(screen.getByRole("button", { name: /^Hard reset:/ }));
		const submit = screen.getByRole("button", { name: "Hard reset" });
		expect(submit).toBeDisabled();
		expect(
			screen.getByText(/permanently discards tracked/i),
		).toBeInTheDocument();

		await user.type(
			screen.getByRole("textbox", { name: "Type main to confirm hard reset" }),
			"main",
		);
		await user.click(submit);

		expect(mocks.sendRequestWithResponse).toHaveBeenCalledWith(
			expect.objectContaining({
				arguments: expect.objectContaining({ resetMode: GitResetMode.Hard }),
			}),
			expect.any(Object),
		);
		expect(onOpenWorkingChanges).not.toHaveBeenCalled();
	});
});

const commit: CommitGraphRow = {
	activeLanesAbove: [],
	activeLanesBelow: [],
	colorIndex: 0,
	commit: {
		author: "Lovely Git",
		branches: [],
		date: 0,
		email: "test@example.invalid",
		hash: "1111111111111111111111111111111111111111",
		message: "Target commit",
		parents: [],
		refs: [],
		remoteRepositoryUrl: null,
		remoteUrl: null,
		signatureKind: "None",
		stats: null,
		tags: [],
	},
	edgesAbove: [],
	edgesBelow: [],
	isBranchTip: false,
	isMergeCommit: false,
	lane: 0,
	laneColorsAbove: [],
	laneColorsBelow: [],
	rowIndex: 1,
};

function renderDialog({
	onOpenWorkingChanges,
	onRepositoryChanged = vi.fn(),
}: {
	onOpenWorkingChanges: () => void;
	onRepositoryChanged?: () => void;
}) {
	return render(
		<ResetCommitDialog
			commit={commit}
			currentBranchName="main"
			onOpenChange={vi.fn()}
			onOpenWorkingChanges={onOpenWorkingChanges}
			onRepositoryChanged={onRepositoryChanged}
			repositoryId="repository-id"
		/>,
	);
}
