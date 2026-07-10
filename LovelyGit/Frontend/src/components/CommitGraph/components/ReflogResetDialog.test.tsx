// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GitReflogEntry } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { ReflogResetDialog } from "./ReflogResetDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

describe("ReflogResetDialog", () => {
	beforeEach(() => vi.clearAllMocks());

	it("runs a mixed reset to the reflog commit", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(undefined);
		const onClose = vi.fn();
		const onRepositoryChanged = vi.fn();
		renderDialog({ onClose, onRepositoryChanged });

		await user.click(screen.getByRole("button", { name: "Mixed reset" }));

		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			{
				arguments: {
					commitHash: entry.newHash,
					repositoryId: "repo",
					resetMode: "Mixed",
				},
				commandType: "ResetCurrentBranchToCommit",
			},
			expect.anything(),
		);
		expect(onClose).toHaveBeenCalledOnce();
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
	});

	it("requires the current branch name before a hard reset", async () => {
		const user = userEvent.setup();
		renderDialog({});
		await user.click(screen.getByRole("button", { name: /^Hard reset:/ }));
		const resetButton = screen.getByRole("button", { name: "Hard reset" });
		expect(resetButton).toBeDisabled();

		await user.type(
			screen.getByRole("textbox", { name: "Type main to confirm hard reset" }),
			"main",
		);
		expect(resetButton).toBeEnabled();
	});
});

const entry: GitReflogEntry = {
	actorEmail: "ross@example.invalid",
	actorName: "Ross",
	message: "reset: moving to HEAD~1",
	newHash: "abcdef1234567890abcdef1234567890abcdef12",
	oldHash: "1234567890abcdef1234567890abcdef12345678",
	selector: "main@{0}",
	timestampUnixSeconds: 1_700_000_000,
	timezone: "+0000",
};

function renderDialog({
	onClose = vi.fn(),
	onRepositoryChanged = vi.fn(),
}: {
	onClose?: () => void;
	onRepositoryChanged?: () => void;
}) {
	return render(
		<ReflogResetDialog
			currentBranchName="main"
			entry={entry}
			onClose={onClose}
			onOpenWorkingChanges={vi.fn()}
			onRepositoryChanged={onRepositoryChanged}
			repositoryId="repo"
		/>,
	);
}
