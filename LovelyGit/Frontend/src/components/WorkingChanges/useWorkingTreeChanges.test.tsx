// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { subscribeToServerEvent } from "@/lib/commands";
import { useWorkingTreeChanges } from "./useWorkingTreeChanges";
import {
	BACKGROUND_FULL_PRELOAD_DELAY_MS,
	CACHED_SUMMARY_REFRESH_DELAY_MS,
} from "./useWorkingTreePreload";
import {
	loadWorkingTreeChangeSummary,
	loadWorkingTreeChanges,
} from "./WorkingTreeChangesRequests";
import { clearWorkingTreeChangesCache } from "./workingTreeChangesCache";
import {
	clearWorkingTreeSummaryCache,
	setCachedWorkingTreeSummary,
} from "./workingTreeSummaryCache";

vi.mock("@/lib/commands", () => ({
	subscribeToServerEvent: vi.fn(() => vi.fn()),
}));
vi.mock("./WorkingTreeChangesRequests", () => ({
	loadWorkingTreeChangeSummary: vi.fn(),
	loadWorkingTreeChanges: vi.fn(),
}));

const loadChanges = vi.mocked(loadWorkingTreeChanges);
const loadSummary = vi.mocked(loadWorkingTreeChangeSummary);
let notifyWorkingTreeChanged: (event: {
	generation: number;
	observedChanges: [];
}) => void = () => undefined;

describe("useWorkingTreeChanges preload", () => {
	beforeEach(() => {
		clearWorkingTreeSummaryCache();
		clearWorkingTreeChangesCache();
		vi.clearAllMocks();
		vi.mocked(subscribeToServerEvent).mockImplementation((_type, handler) => {
			notifyWorkingTreeChanged = handler;
			return vi.fn();
		});
		loadSummary.mockResolvedValue({
			hasChanges: false,
			isComplete: false,
			shouldPreloadChanges: true,
			totalCount: 0,
		});
	});

	afterEach(() => vi.useRealTimers());

	it("keeps a complete cached summary until the watcher invalidates it", () => {
		vi.useFakeTimers();
		setCachedWorkingTreeSummary("repo", {
			hasChanges: true,
			isComplete: true,
			shouldPreloadChanges: true,
			totalCount: 7,
		});
		const { result } = renderHook(() => useWorkingTreeChanges("repo", false));

		expect(result.current.totalCount).toBe(7);
		expect(result.current.isSummaryLoaded).toBe(true);
		expect(loadSummary).not.toHaveBeenCalled();
		act(() => vi.advanceTimersByTime(CACHED_SUMMARY_REFRESH_DELAY_MS));
		expect(loadSummary).not.toHaveBeenCalled();
	});

	it("revalidates an incomplete cached summary", async () => {
		vi.useFakeTimers();
		setCachedWorkingTreeSummary("repo", {
			hasChanges: true,
			isComplete: false,
			shouldPreloadChanges: true,
			totalCount: 7,
		});
		loadSummary.mockResolvedValue({
			hasChanges: false,
			isComplete: true,
			shouldPreloadChanges: true,
			totalCount: 0,
		});
		const { result } = renderHook(() => useWorkingTreeChanges("repo", false));

		act(() => vi.advanceTimersByTime(CACHED_SUMMARY_REFRESH_DELAY_MS));
		await act(async () => Promise.resolve());

		expect(loadSummary).toHaveBeenCalledOnce();
		expect(result.current.totalCount).toBe(0);
	});

	it("does not reread complete summaries while switching tabs", () => {
		vi.useFakeTimers();
		for (const repositoryId of ["repo-a", "repo-b"]) {
			setCachedWorkingTreeSummary(repositoryId, {
				hasChanges: false,
				isComplete: true,
				shouldPreloadChanges: true,
				totalCount: 0,
			});
		}
		const { rerender } = renderHook(
			({ repositoryId }) => useWorkingTreeChanges(repositoryId, false),
			{ initialProps: { repositoryId: "repo-a" } },
		);

		rerender({ repositoryId: "repo-b" });
		act(() => vi.advanceTimersByTime(CACHED_SUMMARY_REFRESH_DELAY_MS));

		expect(loadSummary).not.toHaveBeenCalled();
	});

	async function startBackgroundFullScan() {
		await waitFor(() => expect(loadSummary).toHaveBeenCalled());
		await act(async () => {
			await new Promise((resolve) =>
				setTimeout(resolve, BACKGROUND_FULL_PRELOAD_DELAY_MS),
			);
		});
	}

	it("reuses a bounded background result when the panel opens", async () => {
		loadChanges.mockResolvedValue(response(0));
		const { result, rerender } = renderHook(
			({ enabled }) => useWorkingTreeChanges("repo", enabled),
			{ initialProps: { enabled: false } },
		);
		await startBackgroundFullScan();
		await waitFor(() => expect(result.current.status).toBe("loaded"));

		rerender({ enabled: true });
		await waitFor(() => expect(result.current.status).toBe("loaded"));

		expect(loadChanges).toHaveBeenCalledTimes(1);
		expect(result.current.totalCount).toBe(0);
	});

	it("does not retain oversized background file lists", async () => {
		loadChanges.mockResolvedValue(response(501));
		const { result, rerender } = renderHook(
			({ enabled }) => useWorkingTreeChanges("repo", enabled),
			{ initialProps: { enabled: false } },
		);
		await startBackgroundFullScan();
		await waitFor(() => expect(result.current.isSummaryLoaded).toBe(true));
		expect(result.current.changes).toBeNull();

		rerender({ enabled: true });
		await waitFor(() => expect(loadChanges).toHaveBeenCalledTimes(2));
	});

	it("reuses a bounded background result for repositories with wide indexes", async () => {
		loadSummary.mockResolvedValueOnce({
			hasChanges: false,
			isComplete: false,
			shouldPreloadChanges: false,
			totalCount: 0,
		});
		loadChanges.mockResolvedValue(response(0));
		const { result } = renderHook(() =>
			useWorkingTreeChanges("wide-repo", false),
		);

		await startBackgroundFullScan();
		await waitFor(() => expect(result.current.status).toBe("loaded"));

		expect(loadSummary).toHaveBeenCalledOnce();
		expect(loadChanges).toHaveBeenCalledOnce();
		expect(result.current.totalCount).toBe(0);
		expect(result.current.changes).not.toBeNull();
	});

	it("discards a preload invalidated while its full scan is running", async () => {
		let finishOld: (value: ReturnTypeResponse) => void = () => undefined;
		loadChanges
			.mockReturnValueOnce(new Promise((resolve) => (finishOld = resolve)))
			.mockResolvedValueOnce(response(2));
		const { result } = renderHook(() => useWorkingTreeChanges("repo", false));
		await startBackgroundFullScan();
		await waitFor(() => expect(loadChanges).toHaveBeenCalledOnce());

		act(() => notifyWorkingTreeChanged({ generation: 1, observedChanges: [] }));
		finishOld(response(1));
		expect(result.current.changes).toBeNull();

		await waitFor(() => expect(loadSummary).toHaveBeenCalledTimes(2));
		await act(async () => {
			await new Promise((resolve) =>
				setTimeout(resolve, BACKGROUND_FULL_PRELOAD_DELAY_MS),
			);
		});
		await waitFor(() => expect(loadChanges).toHaveBeenCalledTimes(2));
		await waitFor(() => expect(result.current.totalCount).toBe(2));
	});

	it("cancels the delayed background scan when the panel opens", async () => {
		loadChanges.mockResolvedValue(response(0));
		const { rerender } = renderHook(
			({ enabled }) => useWorkingTreeChanges("repo", enabled),
			{ initialProps: { enabled: false } },
		);
		await waitFor(() => expect(loadSummary).toHaveBeenCalledOnce());

		rerender({ enabled: true });
		await waitFor(() => expect(loadChanges).toHaveBeenCalledOnce());
		await act(async () => {
			await new Promise((resolve) =>
				setTimeout(resolve, BACKGROUND_FULL_PRELOAD_DELAY_MS),
			);
		});

		expect(loadChanges).toHaveBeenCalledOnce();
	});

	it("leaves retryable state after a delayed background scan fails", async () => {
		loadChanges.mockRejectedValue(new Error("status failed"));
		const { result } = renderHook(() => useWorkingTreeChanges("repo", false));

		await startBackgroundFullScan();
		await waitFor(() => expect(result.current.isDirty).toBe(true));

		expect(result.current.isSummaryLoaded).toBe(false);
		expect(result.current.changes).toBeNull();
	});
});

function response(totalCount: number) {
	return {
		staged: [],
		unstaged: [],
		untracked: [],
		unmerged: [],
		totalCount,
	};
}

type ReturnTypeResponse = ReturnType<typeof response>;
