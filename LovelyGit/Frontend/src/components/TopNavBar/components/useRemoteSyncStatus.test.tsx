// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RemoteSyncStatusResponse } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { useRemoteSyncStatus } from "./useRemoteSyncStatus";

vi.mock("@/lib/commands", () => ({
	sendRequestWithResponse: vi.fn(),
	subscribeToServerEvent: vi.fn(() => vi.fn()),
}));

const send = vi.mocked(sendRequestWithResponse);
const subscribe = vi.mocked(subscribeToServerEvent);

describe("useRemoteSyncStatus", () => {
	beforeEach(() => {
		send.mockReset();
		subscribe.mockClear();
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

	it("does not issue a read without a selected repository", () => {
		const { result } = renderHook(() => useRemoteSyncStatus(null, null));
		expect(result.current.status).toBeNull();
		expect(send).not.toHaveBeenCalled();
	});
});

function status(
	aheadCount: number,
	behindCount: number,
): RemoteSyncStatusResponse {
	return {
		branchName: "main",
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
