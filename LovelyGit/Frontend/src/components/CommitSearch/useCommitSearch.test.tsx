// @vitest-environment jsdom

import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	sendRequestWithoutResponse,
	sendRequestWithResponse,
} from "@/lib/commands";
import { searchResponse } from "./CommitSearchTestData";
import { emptyCommitSearchFilters } from "./commitSearchFilters";
import { useCommitSearch } from "./useCommitSearch";

vi.mock("@/lib/commands", () => ({
	sendRequestWithoutResponse: vi.fn(),
	sendRequestWithResponse: vi.fn(),
}));

describe("useCommitSearch", () => {
	beforeEach(() => vi.clearAllMocks());

	it("debounces a bounded native commit search", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(searchResponse());
		const { result } = renderHook(() =>
			useCommitSearch("repo", "needle", true),
		);

		expect(result.current.isLoading).toBe(true);
		await waitFor(() => expect(result.current.response).not.toBeNull());

		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			{
				arguments: {
					afterUnixSeconds: null,
					author: "",
					beforeUnixSeconds: null,
					deep: false,
					knownRepositoryId: "repo",
					limit: 50,
					query: "needle",
					scope: "",
				},
				commandType: "SearchCommits",
			},
			undefined,
		);
		expect(result.current.response?.results[0].subject).toBe("Needle result");
	});

	it("does not search until the query has two characters", async () => {
		const { result } = renderHook(() => useCommitSearch("repo", "n", true));

		expect(result.current.minimumQueryLength).toBe(2);
		expect(result.current.isLoading).toBe(false);
		expect(result.current.response).toBeNull();
		expect(sendRequestWithResponse).not.toHaveBeenCalled();
	});

	it("does not send a reversed date range", () => {
		const { result } = renderHook(() =>
			useCommitSearch("repo", "", true, false, {
				afterDate: "2025-02-02",
				author: "",
				beforeDate: "2025-02-01",
				scope: "",
			}),
		);

		expect(result.current.isLoading).toBe(false);
		expect(sendRequestWithResponse).not.toHaveBeenCalled();
	});

	it("does not submit a partial unknown ref scope", () => {
		const { result } = renderHook(() =>
			useCommitSearch(
				"repo",
				"needle",
				true,
				false,
				{ ...emptyCommitSearchFilters, scope: "feat" },
				false,
			),
		);

		expect(result.current.isLoading).toBe(false);
		expect(sendRequestWithResponse).not.toHaveBeenCalled();
	});

	it("surfaces native search errors", async () => {
		vi.mocked(sendRequestWithResponse).mockRejectedValueOnce(
			new Error("search failed"),
		);
		const { result } = renderHook(() =>
			useCommitSearch("repo", "needle", true),
		);

		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.error).toBe("search failed");
	});

	it("uses the extended timeout for an explicit deep search", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(searchResponse());
		renderHook(() => useCommitSearch("repo", "needle", true, true));

		await waitFor(() => expect(sendRequestWithResponse).toHaveBeenCalled());

		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			expect.objectContaining({
				arguments: expect.objectContaining({ deep: true, limit: 100 }),
			}),
			{ timeoutMs: 12_000 },
		);
	});

	it("releases native search state when the dialog closes", () => {
		const { rerender } = renderHook(
			({ enabled }) => useCommitSearch("repo", "needle", enabled),
			{ initialProps: { enabled: true } },
		);

		rerender({ enabled: false });

		expect(sendRequestWithoutResponse).toHaveBeenCalledWith({
			arguments: { knownRepositoryId: "repo" },
			commandType: "CancelCommitSearch",
		});
	});

	it("does not discard continuation state when search options change", () => {
		const { rerender } = renderHook(
			({ deep }) => useCommitSearch("repo", "needle", true, deep),
			{ initialProps: { deep: false } },
		);

		rerender({ deep: true });

		expect(sendRequestWithoutResponse).not.toHaveBeenCalled();
	});
});
