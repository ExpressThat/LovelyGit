// @vitest-environment jsdom
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { StartBisectDialog } from "./StartBisectDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({
	toast: { error: vi.fn(), loading: vi.fn(() => "toast"), success: vi.fn() },
}));

describe("StartBisectDialog", () => {
	beforeEach(() => vi.clearAllMocks());

	it("marks the selected commit good and HEAD bad", async () => {
		const user = userEvent.setup();
		const onOpenChange = vi.fn();
		const onRepositoryChanged = vi.fn();
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			badCommit: "b".repeat(40),
			currentCommit: "c".repeat(40),
			currentSubject: "Midpoint",
			firstBadCommit: null,
			goodCommits: ["a".repeat(40)],
			isActive: true,
			startingReference: "main",
		});
		render(
			<StartBisectDialog
				commit={row}
				onOpenChange={onOpenChange}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId="repo"
			/>,
		);

		await user.click(screen.getByRole("button", { name: "Start bisect" }));

		await waitFor(() =>
			expect(sendRequestWithResponse).toHaveBeenCalledWith(
				{
					arguments: {
						action: "Start",
						goodCommit: row.commit.hash,
						repositoryId: "repo",
					},
					commandType: "ManageBisect",
				},
				{ timeoutMs: 30_000 },
			),
		);
		expect(onOpenChange).toHaveBeenCalledWith(false);
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
	});

	it("keeps the confirmation retryable without refreshing after Git fails", async () => {
		const user = userEvent.setup();
		const onOpenChange = vi.fn();
		const onRepositoryChanged = vi.fn();
		vi.mocked(sendRequestWithResponse).mockRejectedValue(
			new Error("Local changes would be overwritten"),
		);
		render(
			<StartBisectDialog
				commit={row}
				onOpenChange={onOpenChange}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId="repo"
			/>,
		);

		await user.click(screen.getByRole("button", { name: "Start bisect" }));

		await waitFor(() =>
			expect(toast.error).toHaveBeenCalledWith(
				"Local changes would be overwritten",
				{ id: "toast" },
			),
		);
		expect(screen.getByRole("button", { name: "Start bisect" })).toBeEnabled();
		expect(onRepositoryChanged).not.toHaveBeenCalled();
		expect(onOpenChange).not.toHaveBeenCalled();
	});
});

const row = {
	commit: {
		hash: "a".repeat(40),
		message: "Known good revision",
	},
} as CommitGraphRow;
