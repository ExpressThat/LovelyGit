// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { subscribeRemoteSyncStatus } from "@/components/TopNavBar/components/remoteSyncStatusCache";
import { sendRequestWithResponse } from "@/lib/commands";
import { useBranchMutations } from "./useBranchMutations";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({
	toast: { error: vi.fn(), loading: vi.fn(() => "toast"), success: vi.fn() },
}));

const send = vi.mocked(sendRequestWithResponse);

describe("useBranchMutations upstream reconciliation", () => {
	beforeEach(() => vi.clearAllMocks());

	it("updates locally without reloading the graph and refreshes current sync status", async () => {
		const callbacks = createCallbacks();
		let syncRefreshes = 0;
		const unsubscribe = subscribeRemoteSyncStatus(
			"repo",
			() => syncRefreshes++,
		);
		const { result } = renderHook(() => useController(callbacks));
		act(() => result.current.manageBranch("upstream", "main"));

		await act(() => result.current.manageUpstream("origin/main"));
		unsubscribe();

		expect(callbacks.onUpstreamChanged).toHaveBeenCalledWith(
			"main",
			"origin/main",
		);
		expect(callbacks.onRepositoryChanged).not.toHaveBeenCalled();
		expect(syncRefreshes).toBe(1);
	});

	it("does not refresh current sync status for another branch", async () => {
		let syncRefreshes = 0;
		const unsubscribe = subscribeRemoteSyncStatus(
			"repo",
			() => syncRefreshes++,
		);
		const { result } = renderHook(() => useController(createCallbacks()));
		act(() => result.current.manageBranch("upstream", "feature"));

		await act(() => result.current.manageUpstream("origin/feature"));
		unsubscribe();

		expect(syncRefreshes).toBe(0);
	});

	it("preserves a failed selection and refreshes only after retry succeeds", async () => {
		const callbacks = createCallbacks();
		send.mockRejectedValueOnce(new Error("Remote branch disappeared"));
		const { result } = renderHook(() => useController(callbacks));
		act(() => result.current.manageBranch("upstream", "main"));

		await act(() => result.current.manageUpstream("origin/missing"));
		expect(result.current.upstreamBranchName).toBe("main");
		expect(callbacks.onUpstreamChanged).not.toHaveBeenCalled();
		expect(toast.error).toHaveBeenCalledWith("Remote branch disappeared", {
			id: "toast",
		});

		send.mockResolvedValueOnce(undefined);
		await act(() => result.current.manageUpstream("origin/main"));
		await waitFor(() => expect(result.current.upstreamBranchName).toBeNull());
		expect(callbacks.onUpstreamChanged).toHaveBeenCalledOnce();
	});
});

function createCallbacks() {
	return {
		onCurrentBranchNameChange: vi.fn(),
		onRepositoryChanged: vi.fn(),
		onUpstreamChanged: vi.fn(),
	};
}

function useController(callbacks: ReturnType<typeof createCallbacks>) {
	return useBranchMutations({
		...callbacks,
		currentBranchName: "main",
		remoteName: "origin",
		repositoryId: "repo",
	});
}
