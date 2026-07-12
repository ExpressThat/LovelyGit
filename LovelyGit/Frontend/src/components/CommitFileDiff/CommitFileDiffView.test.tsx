// @vitest-environment jsdom

import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { clearCommitFileDiffCache } from "@/lib/commitFileDiffCache";
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
	CommitFileDiffHeader: ({ onClose }: { onClose: () => void }) => (
		<button onClick={onClose} type="button">
			Close diff
		</button>
	),
}));
vi.mock("./DiffContent", () => ({
	DiffContent: () => <div>Parent diff loaded</div>,
	LoadingDiff: () => <div>Loading diff</div>,
}));

describe("CommitFileDiffView", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		clearCommitFileDiffCache();
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

	it("reuses a loaded diff after the view is closed and reopened", async () => {
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
		const first = render(<CommitFileDiffView {...props} />);
		await screen.findByText("Parent diff loaded");
		first.unmount();

		render(<CommitFileDiffView {...props} />);
		expect(await screen.findByText("Parent diff loaded")).toBeVisible();
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(1);
	});

	it("does not replace colored diff content with a skeleton while closing", async () => {
		const onClose = vi.fn();
		render(
			<CommitFileDiffView
				commitHash="merge"
				file={{
					additions: 1,
					deletions: 1,
					isBinary: false,
					path: "main.txt",
					status: "Modified",
				}}
				onClose={onClose}
				parentIndex={0}
				repositoryId="repo"
			/>,
		);
		await screen.findByText("Parent diff loaded");

		fireEvent.click(screen.getByRole("button", { name: "Close diff" }));
		expect(onClose).toHaveBeenCalledOnce();
		expect(screen.getByText("Parent diff loaded")).toBeVisible();
		expect(screen.queryByText("Loading diff")).not.toBeInTheDocument();
	});

	it("retries after a failed request instead of caching the failure", async () => {
		vi.mocked(sendRequestWithResponse).mockRejectedValueOnce(
			new Error("Diff unavailable"),
		);
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
		const first = render(<CommitFileDiffView {...props} />);
		expect(await screen.findByText("Diff unavailable")).toBeVisible();
		first.unmount();

		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			commitHash: "merge",
			hasDifferences: true,
			isBinary: false,
			lines: [],
			path: "main.txt",
			status: "A",
			viewMode: "Combined",
		});
		render(<CommitFileDiffView {...props} />);
		expect(await screen.findByText("Parent diff loaded")).toBeVisible();
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(2);
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
