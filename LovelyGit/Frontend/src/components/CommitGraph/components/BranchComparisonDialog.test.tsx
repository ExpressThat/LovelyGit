// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { BranchComparisonResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { BranchComparisonDialog } from "./BranchComparisonDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

describe("BranchComparisonDialog", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads native divergence, switches sections, and starts integration", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(response);
		const onClose = vi.fn();
		const onIntegrate = vi.fn();
		render(
			<BranchComparisonDialog
				currentBranchName="main"
				onClose={onClose}
				onIntegrate={onIntegrate}
				repositoryId="repo-id"
				targetBranchName="feature/demo"
			/>,
		);

		expect(await screen.findByText("Current work")).toBeVisible();
		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: {
				currentCommitHash: null,
				repositoryId: "repo-id",
				targetBranchName: "feature/demo",
				targetCommitHash: null,
			},
			commandType: "GetBranchComparison",
		});
		await user.click(screen.getByRole("button", { name: /Changed files/ }));
		expect(await screen.findByText("src/new.ts")).toBeVisible();
		await user.click(
			screen.getByRole("button", { name: "Merge feature/demo into main" }),
		);
		expect(onClose).toHaveBeenCalledOnce();
		expect(onIntegrate).toHaveBeenCalledWith("merge", "feature/demo");
	});
});

const response: BranchComparisonResponse = {
	aheadCommits: [
		{
			authorName: "Ross",
			authorUnixSeconds: 1,
			hash: "111111111",
			subject: "Current work",
		},
	],
	aheadCount: 1,
	behindCommits: [
		{
			authorName: "Alex",
			authorUnixSeconds: 2,
			hash: "222222222",
			subject: "Target work",
		},
	],
	behindCount: 1,
	changedFileCount: 1,
	currentBranchName: "main",
	currentHash: "111111111",
	files: [{ path: "src/new.ts", status: "Added" }],
	isFileListTruncated: false,
	isHistoryPartial: false,
	mergeBaseHash: "000000000",
	targetBranchName: "feature/demo",
	targetHash: "222222222",
};
