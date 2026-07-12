// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { RemoteSyncStatusResponse } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import {
	clearRemoteSyncStatusCache,
	setCachedRemoteSyncStatus,
} from "./remoteSyncStatusCache";
import {
	CACHED_SYNC_REFRESH_DELAY_MS,
	useRemoteSyncStatus,
} from "./useRemoteSyncStatus";

vi.mock("@/lib/commands", () => ({
	sendRequestWithResponse: vi.fn(),
	subscribeToServerEvent: vi.fn(() => vi.fn()),
}));

const send = vi.mocked(sendRequestWithResponse);
const subscribe = vi.mocked(subscribeToServerEvent);

describe("useRemoteSyncStatus", () => {
	beforeEach(() => {
		clearRemoteSyncStatusCache();
		send.mockReset();
		subscribe.mockClear();
	});

	afterEach(() => vi.useRealTimers());

	it("shows cached status immediately and defers revalidation", async () => {
		vi.useFakeTimers();
		setCachedRemoteSyncStatus("repo-1", status(3, 2));
		send.mockResolvedValueOnce(status(1, 0));
		const { result } = renderHook(() => useRemoteSyncStatus("repo-1", "main"));

		expect(result.current.status?.aheadCount).toBe(3);
		expect(send).not.toHaveBeenCalled();
		act(() => vi.advanceTimersByTime(CACHED_SYNC_REFRESH_DELAY_MS));
		await act(async () => Promise.resolve());

		expect(send).toHaveBeenCalledOnce();
		expect(result.current.status?.aheadCount).toBe(1);
	});

	it("cancels an abandoned cached refresh on a rapid tab switch", () => {
		vi.useFakeTimers();
		setCachedRemoteSyncStatus("repo-1", status(1, 0));
		setCachedRemoteSyncStatus("repo-2", status(2, 0));
		send.mockResolvedValue(status(0, 0));
		const { rerender } = renderHook(
			({ repositoryId }) => useRemoteSyncStatus(repositoryId, "main"),
			{ initialProps: { repositoryId: "repo-1" } },
		);

		rerender({ repositoryId: "repo-2" });
		act(() => vi.advanceTimersByTime(CACHED_SYNC_REFRESH_DELAY_MS));

		expect(send).toHaveBeenCalledOnce();
		expect(send).toHaveBeenCalledWith({
			commandType: "GetRemoteSyncStatus",
			arguments: { repositoryId: "repo-2" },
		});
	});

	it("loads native status and refreshes after graph notifications", async () => {
		let graphChanged: (() => void) | undefined;
		subscribe.mockImplementation((_event, listener) => {
			graphChanged = listener as () => void;
			return vi.fn();
		});
		send
			.mockResolvedValueOnce(status(1, 0))
			.mockResolvedValueOnce(status(0, 2));
		const { result } = renderHook(() => useRemoteSyncStatus("repo-1", "main"));

		await waitFor(() => expect(result.current.status?.aheadCount).toBe(1));
		expect(subscribe).toHaveBeenCalledWith(
			"CommitGraphChanged",
			expect.any(Function),
		);
		act(() => graphChanged?.());
		await waitFor(() => expect(result.current.status?.behindCount).toBe(2));
		expect(send).toHaveBeenLastCalledWith({
			commandType: "GetRemoteSyncStatus",
			arguments: { repositoryId: "repo-1" },
		});
	});

	it("ignores a stale response after switching repositories", async () => {
		const first = deferred<RemoteSyncStatusResponse>();
		send.mockReturnValueOnce(first.promise).mockResolvedValueOnce(status(4, 3));
		const { result, rerender } = renderHook(
			({ repositoryId }) => useRemoteSyncStatus(repositoryId, "main"),
			{ initialProps: { repositoryId: "repo-1" as string | null } },
		);

		rerender({ repositoryId: "repo-2" });
		await waitFor(() => expect(result.current.status?.aheadCount).toBe(4));
		await act(async () => first.resolve(status(99, 99)));
		expect(result.current.status?.aheadCount).toBe(4);
	});

	it("does not duplicate a pending read for transient branch states", async () => {
		const pending = deferred<RemoteSyncStatusResponse>();
		send.mockReturnValueOnce(pending.promise);
		const { result, rerender } = renderHook(
			({ branch }) => useRemoteSyncStatus("repo-1", branch),
			{ initialProps: { branch: "previous" as string | null } },
		);

		rerender({ branch: null });
		rerender({ branch: "main" });
		expect(send).toHaveBeenCalledOnce();

		await act(async () => pending.resolve(status(2, 1)));
		expect(result.current.status?.aheadCount).toBe(2);
	});

	it("reloads when a loaded status belongs to the previous branch", async () => {
		send
			.mockResolvedValueOnce(status(1, 0))
			.mockResolvedValueOnce(status(0, 3, "feature"));
		const { result, rerender } = renderHook(
			({ branch }) => useRemoteSyncStatus("repo-1", branch),
			{ initialProps: { branch: "main" } },
		);
		await waitFor(() => expect(result.current.status?.aheadCount).toBe(1));
		vi.useFakeTimers();

		rerender({ branch: "feature" });
		expect(send).toHaveBeenCalledOnce();
		act(() => vi.advanceTimersByTime(CACHED_SYNC_REFRESH_DELAY_MS));
		await act(async () => Promise.resolve());

		expect(result.current.status?.behindCount).toBe(3);
		expect(send).toHaveBeenCalledTimes(2);
	});

	it("suppresses a response that does not match the latest branch", async () => {
		const pending = deferred<RemoteSyncStatusResponse>();
		send.mockReturnValueOnce(pending.promise);
		const { result, rerender } = renderHook(
			({ branch }) => useRemoteSyncStatus("repo-1", branch),
			{ initialProps: { branch: "main" } },
		);
		rerender({ branch: "feature" });

		await act(async () => pending.resolve(status(8, 8, "main")));

		expect(result.current.status).toBeNull();
		expect(send).toHaveBeenCalledOnce();
	});

	it("can refresh successfully after an initial read failure", async () => {
		let graphChanged: (() => void) | undefined;
		subscribe.mockImplementation((_event, listener) => {
			graphChanged = listener as () => void;
			return vi.fn();
		});
		const consoleError = vi
			.spyOn(console, "error")
			.mockImplementation(() => {});
		send
			.mockRejectedValueOnce(new Error("read failed"))
			.mockResolvedValueOnce(status(5, 2));
		const { result } = renderHook(() => useRemoteSyncStatus("repo-1", "main"));
		await waitFor(() => expect(consoleError).toHaveBeenCalledOnce());
		expect(result.current.status).toBeNull();

		act(() => graphChanged?.());

		await waitFor(() => expect(result.current.status?.aheadCount).toBe(5));
		expect(send).toHaveBeenCalledTimes(2);
		consoleError.mockRestore();
	});

	it("does not issue a read without a selected repository", () => {
		const { result } = renderHook(() => useRemoteSyncStatus(null, null));
		expect(result.current.status).toBeNull();
		expect(send).not.toHaveBeenCalled();
	});
});

function status(
	aheadCount: number,
	behindCount: number,
	branchName = "main",
): RemoteSyncStatusResponse {
	return {
		branchName,
		upstreamName: "origin/main",
		localHash: "local",
		upstreamHash: "remote",
		aheadCount,
		behindCount,
		hasUpstream: true,
		isUpstreamAvailable: true,
		isHistoryPartial: false,
	};
}

function deferred<T>() {
	let resolve!: (value: T) => void;
	const promise = new Promise<T>((resolvePromise) => {
		resolve = resolvePromise;
	});
	return { promise, resolve };
}
