// @vitest-environment jsdom

import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { FileBlameResponse } from "@/generated/types";
import {
	sendRequestWithoutResponse,
	sendRequestWithResponse,
} from "@/lib/commands";
import { useFileBlame } from "./useFileBlame";

vi.mock("@/lib/commands", () => ({
	sendRequestWithoutResponse: vi.fn(),
	sendRequestWithResponse: vi.fn(),
}));

describe("useFileBlame", () => {
	beforeEach(() => vi.clearAllMocks());

	it("requests bounded native blame for the selected revision", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(response());
		const { result } = renderHook(() =>
			useFileBlame("repo", "src/file.ts", "abc123", true, false),
		);

		await waitFor(() => expect(result.current.response).not.toBeNull());
		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			{
				arguments: {
					deep: false,
					knownRepositoryId: "repo",
					path: "src/file.ts",
					startCommitHash: "abc123",
				},
				commandType: "GetFileBlame",
			},
			undefined,
		);
	});

	it("uses the extended timeout for deeper attribution", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(response());
		renderHook(() => useFileBlame("repo", "src/file.ts", null, true, true));

		await waitFor(() => expect(sendRequestWithResponse).toHaveBeenCalled());
		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			expect.objectContaining({
				arguments: expect.objectContaining({ deep: true }),
			}),
			{ timeoutMs: 12_000 },
		);
	});

	it("does not read while the viewer is closed", () => {
		const { result } = renderHook(() =>
			useFileBlame("repo", "src/file.ts", null, false, false),
		);

		expect(result.current.isLoading).toBe(false);
		expect(sendRequestWithResponse).not.toHaveBeenCalled();
	});

	it("cancels native work when the viewer closes", () => {
		vi.mocked(sendRequestWithResponse).mockReturnValueOnce(
			new Promise(() => {}),
		);
		const { rerender } = renderHook(
			({ enabled }) => useFileBlame("repo", "src/file.ts", null, enabled, true),
			{ initialProps: { enabled: true } },
		);

		rerender({ enabled: false });

		expect(sendRequestWithoutResponse).toHaveBeenCalledWith({
			arguments: { knownRepositoryId: "repo" },
			commandType: "CancelFileBlame",
		});
	});

	it("surfaces native failures without retaining a stale response", async () => {
		vi.mocked(sendRequestWithResponse).mockRejectedValueOnce(
			new Error("Blame fixture failed."),
		);
		const { result } = renderHook(() =>
			useFileBlame("repo", "src/file.ts", null, true, false),
		);

		await waitFor(() => expect(result.current.isLoading).toBe(false));
		expect(result.current.error).toBe("Blame fixture failed.");
		expect(result.current.response).toBeNull();
	});
});

function response(): FileBlameResponse {
	return {
		compactPayloadGzipBase64: "",
		content: "line\n",
		hunks: [],
		isPartial: false,
		lineCount: 1,
		path: "src/file.ts",
		resolvedLineCount: 1,
		scannedCommitCount: 2,
		startCommitHash: "abc123",
	};
}
