// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { expect, it, vi } from "vitest";
import type {
	BranchComparisonResponse,
	CommitGraphRow,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { CommitComparisonDialog } from "./CommitComparisonDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("@/components/CommitFileDiff/CommitFileDiffView", () => ({
	CommitFileDiffView: ({
		comparisonCommitHash,
		onClose,
	}: {
		comparisonCommitHash: string;
		onClose: () => void;
	}) => (
		<div>
			Direct diff from {comparisonCommitHash.slice(0, 7)}
			<button onClick={onClose} type="button">
				Close direct diff
			</button>
		</div>
	),
}));

it("shows exact-commit file and history sections", async () => {
	const user = userEvent.setup();
	vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(response);
	render(
		<CommitComparisonDialog
			base={row("1", "Base commit")}
			onClose={vi.fn()}
			repositoryId="repo"
			target={row("2", "Target commit")}
		/>,
	);

	expect(await screen.findByText("src/new.ts")).toBeVisible();
	await user.click(
		screen.getByRole("button", { name: "Open comparison diff for src/new.ts" }),
	);
	expect(await screen.findByText("Direct diff from 1111111")).toBeVisible();
	await user.click(screen.getByRole("button", { name: "Close direct diff" }));
	await user.click(screen.getByRole("button", { name: /1111111 only/ }));
	expect(await screen.findByText("Base-only work")).toBeVisible();
	await user.click(screen.getByRole("button", { name: /2222222 only/ }));
	expect(await screen.findByText("Target-only work")).toBeVisible();
});

function row(digit: string, message: string) {
	return { commit: { hash: digit.repeat(40), message } } as CommitGraphRow;
}

const response: BranchComparisonResponse = {
	compactFilesGzipBase64: null,
	aheadCommits: [
		{
			authorName: "Ross",
			authorUnixSeconds: 1,
			hash: "1".repeat(40),
			subject: "Base-only work",
		},
	],
	aheadCount: 1,
	behindCommits: [
		{
			authorName: "Alex",
			authorUnixSeconds: 2,
			hash: "2".repeat(40),
			subject: "Target-only work",
		},
	],
	behindCount: 1,
	changedFileCount: 1,
	currentBranchName: "1111111",
	currentHash: "1".repeat(40),
	files: [{ path: "src/new.ts", status: "Added" }],
	isFileListTruncated: false,
	isHistoryPartial: false,
	mergeBaseHash: "0".repeat(40),
	targetBranchName: "2222222",
	targetHash: "2".repeat(40),
};
