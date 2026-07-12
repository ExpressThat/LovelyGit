// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { subscribeToServerEvent } from "@/lib/commands";
import { useWorkingTreeChanges } from "./useWorkingTreeChanges";
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

	it("reuses a bounded background result when the panel opens", async () => {
		loadChanges.mockResolvedValue(response(0));
		const { result, rerender } = renderHook(
			({ enabled }) => useWorkingTreeChanges("repo", enabled),
			{ initialProps: { enabled: false } },
		);
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
		await waitFor(() => expect(loadChanges).toHaveBeenCalledOnce());

		act(() => notifyWorkingTreeChanged({ generation: 1, observedChanges: [] }));
		finishOld(response(1));
		expect(result.current.changes).toBeNull();

		await waitFor(() => expect(loadChanges).toHaveBeenCalledTimes(2));
		await waitFor(() => expect(result.current.totalCount).toBe(2));
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
