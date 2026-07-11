// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { copyToClipboard } from "../utils/clipboard";
import { useCommitPatchActions } from "./useCommitPatchActions";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("../utils/clipboard", () => ({ copyToClipboard: vi.fn() }));

describe("useCommitPatchActions", () => {
	beforeEach(() => vi.clearAllMocks());

	it("requests the native patch and copies it", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			commitHash: row.commit.hash,
			hasUnsupportedBinaryChanges: false,
			isTruncated: false,
			patch: "diff --git a/file.txt b/file.txt\n",
		});
		const { result } = renderHook(() => useCommitPatchActions("repository-id"));

		await act(() => result.current.copyPatch(row));

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: {
				commitHash: row.commit.hash,
				repositoryId: "repository-id",
			},
			commandType: "GetCommitPatch",
		});
		expect(copyToClipboard).toHaveBeenCalledWith(
			"diff --git a/file.txt b/file.txt\n",
			"Commit patch",
		);
		await waitFor(() => expect(result.current.busyCommitHash).toBeNull());
	});

	it("does not copy a truncated patch", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			commitHash: row.commit.hash,
			hasUnsupportedBinaryChanges: false,
			isTruncated: true,
			patch: "partial",
		});
		const { result } = renderHook(() => useCommitPatchActions("repository-id"));

		await act(() => result.current.copyPatch(row));

		expect(copyToClipboard).not.toHaveBeenCalled();
	});

	it("opens the native save flow for the selected commit", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			path: "C:/patches/change.patch",
			saved: true,
		});
		const { result } = renderHook(() => useCommitPatchActions("repository-id"));

		await act(() => result.current.savePatch(row));

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: {
				commitHash: row.commit.hash,
				repositoryId: "repository-id",
			},
			commandType: "SaveCommitPatch",
		});
		await waitFor(() => expect(result.current.busyCommitHash).toBeNull());
	});

	it("exports the selected commit as an archive and clears its busy state", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue({
			path: "C:/archives/selected.zip",
			saved: true,
		});
		const { result } = renderHook(() => useCommitPatchActions("repository-id"));

		await act(() => result.current.saveArchive(row));

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: {
				commitHash: row.commit.hash,
				repositoryId: "repository-id",
			},
			commandType: "SaveCommitArchive",
		});
		await waitFor(() => expect(result.current.busyCommitHash).toBeNull());
	});

	it("surfaces archive export failures and remains retryable", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("Archive failed"))
			.mockResolvedValueOnce({ path: "C:/selected.zip", saved: true });
		const { result } = renderHook(() => useCommitPatchActions("repository-id"));

		await act(() => result.current.saveArchive(row));
		await act(() => result.current.saveArchive(row));

		expect(sendRequestWithResponse).toHaveBeenCalledTimes(2);
		await waitFor(() => expect(result.current.busyAction).toBeNull());
	});
});

const row = {
	commit: { hash: "1".repeat(40), message: "Patch me" },
} as CommitGraphRow;
