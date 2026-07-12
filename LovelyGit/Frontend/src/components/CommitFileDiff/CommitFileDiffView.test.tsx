// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { CommitFileDiffView } from "./CommitFileDiffView";

const settings = {
	CommitDiffContextLines: 3,
	CommitDiffIgnoreWhitespace: false,
	CommitDiffLineDisplayMode: "Changes",
	CommitDiffViewMode: "Combined",
	CommitDiffWrapLines: false,
};

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("@/lib/settings/settingsStore", () => ({
	useSetting: (setting: keyof typeof settings) => settings[setting],
}));
vi.mock("./CommitFileDiffHeader", () => ({
	CommitFileDiffHeader: () => <div>Diff header</div>,
}));
vi.mock("./DiffContent", () => ({
	DiffContent: () => <div>Parent diff loaded</div>,
	LoadingDiff: () => <div>Loading diff</div>,
}));

describe("CommitFileDiffView", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		settings.CommitDiffIgnoreWhitespace = false;
		settings.CommitDiffViewMode = "Combined";
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			commitHash: "merge",
			hasDifferences: true,
			isBinary: false,
			lines: [],
			path: "main.txt",
			status: "A",
			viewMode: "Combined",
		});
	});

	it("reuses a previously loaded display variant", async () => {
		const props = {
			commitHash: "merge",
			file: {
				additions: 1,
				deletions: 1,
				isBinary: false,
				path: "main.txt",
				status: "Modified",
			},
			onClose: vi.fn(),
			parentIndex: 0,
			repositoryId: "repo",
		};
		const { rerender } = render(<CommitFileDiffView {...props} />);
		await screen.findByText("Parent diff loaded");

		settings.CommitDiffViewMode = "SideBySide";
		rerender(<CommitFileDiffView {...props} />);
		await waitFor(() =>
			expect(sendRequestWithResponse).toHaveBeenCalledTimes(2),
		);

		settings.CommitDiffViewMode = "Combined";
		rerender(<CommitFileDiffView {...props} />);
		await screen.findByText("Parent diff loaded");
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(2);
	});

	it("requests the file against the selected merge parent", async () => {
		render(
			<CommitFileDiffView
				commitHash="merge"
				file={{
					additions: 1,
					deletions: 0,
					isBinary: false,
					path: "main.txt",
					status: "A",
				}}
				onClose={vi.fn()}
				parentIndex={1}
				repositoryId="repo"
			/>,
		);

		expect(await screen.findByText("Parent diff loaded")).toBeVisible();
		await waitFor(() =>
			expect(sendRequestWithResponse).toHaveBeenCalledWith({
				commandType: "GetCommitFileDiff",
				arguments: {
					commitHash: "merge",
					comparisonCommitHash: null,
					ignoreWhitespace: false,
					parentIndex: 1,
					path: "main.txt",
					repositoryId: "repo",
					viewMode: "Combined",
				},
			}),
		);
	});

	it("requests a direct diff against an explicit comparison commit", async () => {
		render(
			<CommitFileDiffView
				commitHash="target"
				comparisonCommitHash="base"
				file={{
					additions: 0,
					deletions: 0,
					isBinary: false,
					path: "shared.txt",
					status: "Modified",
				}}
				onClose={vi.fn()}
				parentIndex={0}
				repositoryId="repo"
			/>,
		);

		await screen.findByText("Parent diff loaded");
		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			commandType: "GetCommitFileDiff",
			arguments: {
				commitHash: "target",
				comparisonCommitHash: "base",
				ignoreWhitespace: false,
				parentIndex: 0,
				path: "shared.txt",
				repositoryId: "repo",
				viewMode: "Combined",
			},
		});
	});
});
