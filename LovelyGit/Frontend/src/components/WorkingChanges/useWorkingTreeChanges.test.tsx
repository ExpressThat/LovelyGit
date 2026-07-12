// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { subscribeToServerEvent } from "@/lib/commands";
import { useWorkingTreeChanges } from "./useWorkingTreeChanges";
import { BACKGROUND_FULL_PRELOAD_DELAY_MS } from "./useWorkingTreePreload";
import {
	loadWorkingTreeChangeSummary,
	loadWorkingTreeChanges,
} from "./WorkingTreeChangesRequests";

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
		vi.clearAllMocks();
		vi.mocked(subscribeToServerEvent).mockImplementation((_type, handler) => {
			notifyWorkingTreeChanged = handler;
			return vi.fn();
		});
		loadSummary.mockResolvedValue({
			hasChanges: false,
			isComplete: false,
			totalCount: 0,
		});
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
