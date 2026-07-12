// @vitest-environment jsdom
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { StartBisectDialog } from "./StartBisectDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({
	toast: { error: vi.fn(), loading: vi.fn(() => "toast"), success: vi.fn() },
}));

describe("StartBisectDialog", () => {
	it("marks the selected commit good and HEAD bad", async () => {
		const user = userEvent.setup();
		const onOpenChange = vi.fn();
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
	});
});

const row = {
	commit: {
		hash: "a".repeat(40),
		message: "Known good revision",
	},
} as CommitGraphRow;
