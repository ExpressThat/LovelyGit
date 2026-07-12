// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CommitGraphResponse, CommitGraphRow } from "@/generated/types";

const mocks = vi.hoisted(() => ({
	repositoryId: "repo-a" as string | null,
	sendRequest: vi.fn(),
	subscribe: vi.fn(() => () => {}),
}));

vi.mock("@/lib/commands", () => ({
	sendRequestWithResponse: mocks.sendRequest,
	subscribeToServerEvent: mocks.subscribe,
}));
vi.mock("@/lib/settings/settingsStore", () => ({
	useSetting: () => mocks.repositoryId,
}));

describe("useCommitGraphData", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.resetModules();
		mocks.repositoryId = "repo-a";
	});

	it("loads two bounded pages initially and prefetches requested ranges", async () => {
		mocks.sendRequest.mockImplementation(({ arguments: request }) => {
			const start = request.cursor ? Number(request.cursor) : 0;
			return Promise.resolve(response(start, 128, true));
		});
		const { useCommitGraphData } = await import("./useCommitGraphData");
		const { result } = renderHook(() => useCommitGraphData());

		await waitFor(() => expect(mocks.sendRequest).toHaveBeenCalledTimes(2));
		expect(result.current.rows).toHaveLength(256);
		expect(result.current.remoteRepositoryUrl).toBe(
			"https://example.test/repo",
		);
		expect(result.current.rows[0]?.commit.refs).toBe(
			result.current.rows[1]?.commit.refs,
		);
		expect(Object.isFrozen(result.current.rows[0]?.commit.refs)).toBe(true);
		const initialRows = result.current.rows;
		expect(mocks.sendRequest).toHaveBeenNthCalledWith(1, {
			commandType: "CommitGraph",
			arguments: {
				knownRepositoryId: "repo-a",
				cursor: null,
				limit: 128,
			},
		});

		act(() => result.current.ensureRangeLoaded(500, 520));
		await waitFor(() =>
			expect(result.current.rows.length).toBeGreaterThan(520),
		);
		expect(mocks.sendRequest).toHaveBeenCalledTimes(6);
		expect(result.current.rows).toBe(initialRows);
	});

	it("surfaces a failed initial page without retaining loading state", async () => {
		mocks.sendRequest.mockRejectedValueOnce(new Error("graph unavailable"));
		const { useCommitGraphData } = await import("./useCommitGraphData");
		const { result } = renderHook(() => useCommitGraphData());

		await waitFor(() => expect(result.current.error).toBe("graph unavailable"));

		expect(result.current.isInitialLoading).toBe(false);
		expect(result.current.rows).toEqual([]);
	});
});

function response(
	start: number,
	count: number,
	hasMore: boolean,
): CommitGraphResponse {
	return {
		currentBranchName: "main",
		hasMore,
		laneCount: 1,
		nextCursor: hasMore ? String(start + count) : null,
		remotePrefixes: ["origin"],
		remoteRepositoryUrl: "https://example.test/repo",
		rows: Array.from({ length: count }, (_, offset) => row(start + offset)),
		totalRows: hasMore ? start + count * 2 : start + count,
	};
}

function row(rowIndex: number): CommitGraphRow {
	return {
		activeLanesAbove: [],
		activeLanesBelow: [],
		colorIndex: 0,
		commit: {
			author: "Author",
			date: rowIndex,
			email: "author@example.invalid",
			hash: rowIndex.toString(16).padStart(40, "0"),
			message: `Commit ${rowIndex}`,
			refs: [],
			signatureKind: "None",
			stats: null,
		},
		edgesAbove: [],
		edgesBelow: [],
		isBranchTip: false,
		isMergeCommit: false,
		lane: 0,
		laneColorsAbove: [],
		laneColorsBelow: [],
		rowIndex,
	};
}
