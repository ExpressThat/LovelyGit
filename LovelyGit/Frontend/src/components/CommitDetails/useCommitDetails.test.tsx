// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CommitDetailsResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { useCommitDetails } from "./useCommitDetails";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

describe("useCommitDetails", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads details relative to the selected merge parent", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue(
			details("feature.txt"),
		);
		const { result } = renderHook(() =>
			useCommitDetails("repo", "merge", 1, 0),
		);

		await waitFor(() => expect(result.current.state.status).toBe("loaded"));
		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			commandType: "GetCommitDetails",
			arguments: {
				commitHash: "merge",
				parentIndex: 1,
				repositoryId: "repo",
			},
		});
	});

	it("keeps existing details visible while another parent loads", async () => {
		let resolveSecond: (value: CommitDetailsResponse) => void = () => undefined;
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce(details("feature.txt"))
			.mockImplementationOnce(
				() =>
					new Promise((resolve) => {
						resolveSecond = resolve;
					}),
			);
		const { result, rerender } = renderHook(
			({ parent }) => useCommitDetails("repo", "merge", parent, 0),
			{ initialProps: { parent: 0 } },
		);
		await waitFor(() => expect(result.current.state.status).toBe("loaded"));

		rerender({ parent: 1 });
		await waitFor(() => {
			expect(result.current.state.status).toBe("loaded");
			if (result.current.state.status === "loaded") {
				expect(result.current.state.isRefreshing).toBe(true);
				expect(result.current.state.details.changedFiles[0]?.path).toBe(
					"feature.txt",
				);
			}
		});
		await act(() => resolveSecond(details("main.txt")));
		await waitFor(() => {
			if (result.current.state.status === "loaded") {
				expect(result.current.state.isRefreshing).toBe(false);
				expect(result.current.state.details.changedFiles[0]?.path).toBe(
					"main.txt",
				);
			}
		});
	});

	it("preserves details after failure and retries the same parent", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce(details("feature.txt"))
			.mockRejectedValueOnce(new Error("Parent object is unavailable"))
			.mockResolvedValueOnce(details("main.txt"));
		const { result, rerender } = renderHook(
			({ parent }) => useCommitDetails("repo", "merge", parent, 0),
			{ initialProps: { parent: 0 } },
		);
		await waitFor(() => expect(result.current.state.status).toBe("loaded"));
		rerender({ parent: 1 });
		await waitFor(() => {
			if (result.current.state.status === "loaded") {
				expect(result.current.state.refreshError).toBe(
					"Parent object is unavailable",
				);
			}
		});

		act(() => result.current.retry());
		await waitFor(() => {
			if (result.current.state.status === "loaded") {
				expect(result.current.state.refreshError).toBeNull();
				expect(result.current.state.details.changedFiles[0]?.path).toBe(
					"main.txt",
				);
			}
		});
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(3);
	});
});

function details(path: string): CommitDetailsResponse {
	return {
		author: "Ada",
		body: "",
		branches: [],
		changedFiles: [
			{ additions: 1, deletions: 0, isBinary: false, path, status: "A" },
		],
		date: 1,
		email: "ada@example.test",
		hash: "a".repeat(40),
		message: "Merge",
		parents: ["b".repeat(40), "c".repeat(40)],
		signatureKind: "None",
		stats: { additions: 1, deletions: 0 },
		subject: "Merge",
		tags: [],
	};
}
