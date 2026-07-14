// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { subscribeToServerEvent } from "@/lib/commands";
import { useWorkingTreeChanges } from "./useWorkingTreeChanges";
import {
	loadWorkingTreeChangeSummary,
	loadWorkingTreeChanges,
} from "./WorkingTreeChangesRequests";
import { clearWorkingTreeChangesCache } from "./workingTreeChangesCache";
import { clearWorkingTreeSummaryCache } from "./workingTreeSummaryCache";

vi.mock("@/lib/commands", () => ({
	subscribeToServerEvent: vi.fn(() => vi.fn()),
}));
vi.mock("./WorkingTreeChangesRequests", () => ({
	loadWorkingTreeChangeSummary: vi.fn(),
	loadWorkingTreeChanges: vi.fn(),
}));

const loadChanges = vi.mocked(loadWorkingTreeChanges);
const loadSummary = vi.mocked(loadWorkingTreeChangeSummary);

describe("useWorkingTreeChanges manual reload", () => {
	beforeEach(() => {
		clearWorkingTreeSummaryCache();
		clearWorkingTreeChangesCache();
		vi.clearAllMocks();
		vi.mocked(subscribeToServerEvent).mockReturnValue(vi.fn());
		loadSummary.mockResolvedValue({
			hasChanges: true,
			isComplete: true,
			shouldPreloadChanges: true,
			totalCount: 1,
		});
		loadChanges.mockResolvedValue(response(1));
	});

	it("coalesces overlapping reloads and exposes their busy state", async () => {
		const { result } = renderHook(() => useWorkingTreeChanges("repo", true));
		await waitFor(() => expect(result.current.status).toBe("loaded"));
		loadChanges.mockReset();
		const pending = deferred<ReturnTypeResponse>();
		loadChanges.mockReturnValue(pending.promise);

		let first: Promise<void> | undefined;
		let second: Promise<void> | undefined;
		act(() => {
			first = result.current.reload();
			second = result.current.reload();
		});

		expect(first).toBe(second);
		expect(loadChanges).toHaveBeenCalledOnce();
		expect(result.current.isReloading).toBe(true);

		await act(async () => {
			pending.resolve(response(2));
			await first;
		});

		expect(result.current.isReloading).toBe(false);
		expect(result.current.totalCount).toBe(2);
	});

	it("surfaces a failed reload, re-enables refresh, and permits retry", async () => {
		const { result } = renderHook(() => useWorkingTreeChanges("repo", true));
		await waitFor(() => expect(result.current.status).toBe("loaded"));
		loadChanges.mockReset();
		loadChanges.mockRejectedValueOnce(new Error("status failed"));

		await act(async () => {
			await expect(result.current.reload()).rejects.toThrow("status failed");
		});

		expect(result.current.status).toBe("error");
		expect(result.current.isReloading).toBe(false);
		expect(result.current.changes?.totalCount).toBe(1);

		loadChanges.mockResolvedValueOnce(response(3));
		await act(async () => result.current.reload());

		expect(result.current.status).toBe("loaded");
		expect(result.current.totalCount).toBe(3);
	});

	it("does not let an old repository reload overwrite the current one", async () => {
		const { result, rerender } = renderHook(
			({ repositoryId }) => useWorkingTreeChanges(repositoryId, true),
			{ initialProps: { repositoryId: "repo-a" } },
		);
		await waitFor(() => expect(result.current.status).toBe("loaded"));
		loadChanges.mockReset();
		const oldReload = deferred<ReturnTypeResponse>();
		loadChanges.mockImplementation((repositoryId) =>
			repositoryId === "repo-a"
				? oldReload.promise
				: Promise.resolve(response(2)),
		);

		let oldPromise: Promise<void> | undefined;
		act(() => {
			oldPromise = result.current.reload();
		});
		rerender({ repositoryId: "repo-b" });

		await waitFor(() => expect(result.current.totalCount).toBe(2));
		expect(result.current.isReloading).toBe(false);

		await act(async () => {
			oldReload.resolve(response(99));
			await oldPromise;
		});
		expect(result.current.totalCount).toBe(2);
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

function deferred<T>() {
	let resolve = (_value: T) => undefined;
	const promise = new Promise<T>((complete) => {
		resolve = (value) => {
			complete(value);
			return undefined;
		};
	});
	return { promise, resolve };
}

type ReturnTypeResponse = ReturnType<typeof response>;
