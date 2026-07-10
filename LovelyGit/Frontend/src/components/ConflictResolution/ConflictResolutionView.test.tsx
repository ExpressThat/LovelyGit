// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	ConflictFileVersion,
	ConflictResolutionResponse,
	WorkingTreeChangedFile,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { ConflictResolutionView } from "./ConflictResolutionView";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({ toast: { success: vi.fn() } }));
vi.mock("@/lib/settings/settingsStore", () => ({
	useSetting: (name: string) =>
		({
			CommitDiffViewMode: "SideBySide",
			CommitDiffContextLines: 3,
			CommitDiffLineDisplayMode: "Changes",
			CommitDiffWrapLines: false,
			CommitDiffIgnoreWhitespace: false,
		})[name],
}));

const send = vi.mocked(sendRequestWithResponse);

describe("ConflictResolutionView", () => {
	beforeEach(() => send.mockReset());

	it("assembles line choices and marks the result resolved", async () => {
		const user = userEvent.setup();
		const onChange = vi.fn();
		const onClose = vi.fn();
		send.mockResolvedValueOnce(response()).mockResolvedValueOnce(undefined);
		renderView(onChange, onClose);

		await user.click(
			await screen.findByRole("button", { name: "Use current" }),
		);
		const result = screen.getByLabelText("Editable result preview");
		expect(result).toHaveValue("before\ncurrent\nafter\n");
		await user.click(screen.getByRole("button", { name: "Mark resolved" }));

		await waitFor(() =>
			expect(send).toHaveBeenLastCalledWith({
				commandType: "ResolveConflict",
				arguments: {
					repositoryId: "repo-1",
					path: "src/file.txt",
					expectedFingerprint: "ABC123",
					resultText: "before\ncurrent\nafter\n",
					source: null,
					deleteResult: false,
				},
			}),
		);
		expect(onChange).toHaveBeenCalledOnce();
		expect(onClose).toHaveBeenCalledOnce();
	});

	it("keeps controls available after a mutation failure so retry can succeed", async () => {
		const user = userEvent.setup();
		const onClose = vi.fn();
		send
			.mockResolvedValueOnce(response())
			.mockRejectedValueOnce(new Error("index is locked"))
			.mockResolvedValueOnce(undefined);
		renderView(vi.fn(), onClose);

		await user.click(
			await screen.findByRole("button", { name: "Use incoming" }),
		);
		await user.click(screen.getByRole("button", { name: "Mark resolved" }));
		expect(await screen.findByText("index is locked")).toBeInTheDocument();
		expect(onClose).not.toHaveBeenCalled();

		await user.click(screen.getByRole("button", { name: "Mark resolved" }));
		await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
		expect(send).toHaveBeenCalledTimes(3);
	});

	it("does not enable completion while manually edited markers remain", async () => {
		send.mockResolvedValueOnce(response());
		renderView(vi.fn(), vi.fn());

		expect(
			await screen.findByRole("button", { name: "Mark resolved" }),
		).toBeDisabled();
		expect(
			screen.getByText(/Resolve every highlighted conflict/),
		).toBeInTheDocument();
	});

	it("uses a whole incoming file for binary conflicts without decoding it", async () => {
		const user = userEvent.setup();
		const binary = response();
		binary.comparison = null;
		binary.ours = { ...binary.ours, isBinary: true, text: null };
		binary.theirs = { ...binary.theirs, isBinary: true, text: null };
		binary.result = { ...binary.result, isBinary: true, text: null };
		send.mockResolvedValueOnce(binary).mockResolvedValueOnce(undefined);
		renderView(vi.fn(), vi.fn());

		await user.click(
			await screen.findByRole("button", { name: "Use incoming branch" }),
		);
		await user.click(screen.getByRole("button", { name: "Mark resolved" }));

		await waitFor(() =>
			expect(send).toHaveBeenLastCalledWith({
				commandType: "ResolveConflict",
				arguments: {
					repositoryId: "repo-1",
					path: "src/file.txt",
					expectedFingerprint: "ABC123",
					resultText: null,
					source: "Theirs",
					deleteResult: false,
				},
			}),
		);
	});
});

function renderView(onChange: () => void, onClose: () => void) {
	render(
		<ConflictResolutionView
			file={file()}
			onChange={onChange}
			onClose={onClose}
			repositoryId="repo-1"
		/>,
	);
}

function file(): WorkingTreeChangedFile {
	return {
		path: "src/file.txt",
		oldPath: null,
		status: "Unmerged",
		group: "Unmerged",
		additions: 0,
		deletions: 0,
		isBinary: false,
	};
}

function version(text: string): ConflictFileVersion {
	return {
		exists: true,
		isBinary: false,
		isTooLarge: false,
		sizeBytes: text.length,
		text,
	};
}

function response(): ConflictResolutionResponse {
	return {
		path: "src/file.txt",
		worktreeFingerprint: "ABC123",
		base: version("base\n"),
		ours: version("current\n"),
		theirs: version("incoming\n"),
		result: version(
			"before\n<<<<<<< HEAD\ncurrent\n=======\nincoming\n>>>>>>> feature\nafter\n",
		),
		comparison: null,
	};
}
