// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { useBranchMutations } from "./useBranchMutations";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({
	toast: { error: vi.fn(), loading: vi.fn(() => "toast"), success: vi.fn() },
}));

const send = vi.mocked(sendRequestWithResponse);

describe("useBranchMutations", () => {
	beforeEach(() => vi.clearAllMocks());

	it("surfaces checkout failure, preserves callbacks, and permits retry", async () => {
		const callbacks = createCallbacks();
		send.mockRejectedValueOnce(new Error("Working tree is dirty"));
		const { result } = renderHook(() => useTestBranchMutations(callbacks));

		act(() => result.current.manageBranch("checkout", "feature"));
		await waitFor(() => expect(result.current.busyBranch).toBeNull());

		expect(toast.error).toHaveBeenCalledWith("Working tree is dirty", {
			id: "toast",
		});
		expect(callbacks.onCurrentBranchNameChange).not.toHaveBeenCalled();
		expect(callbacks.onRepositoryChanged).not.toHaveBeenCalled();

		send.mockResolvedValueOnce(undefined);
		act(() => result.current.manageBranch("checkout", "feature"));
		await waitFor(() =>
			expect(callbacks.onCurrentBranchNameChange).toHaveBeenCalledWith(
				"feature",
			),
		);
		expect(callbacks.onRepositoryChanged).toHaveBeenCalledOnce();
	});

	it("failed destructive delete keeps confirmation target and retry clears it", async () => {
		const callbacks = createCallbacks();
		send.mockRejectedValueOnce(new Error("Branch is not merged"));
		const { result } = renderHook(() => useTestBranchMutations(callbacks));
		act(() => result.current.manageBranch("delete", "feature"));

		await act(() => result.current.deleteBranch(false));

		expect(result.current.deleteBranchName).toBe("feature");
		expect(callbacks.onRepositoryChanged).not.toHaveBeenCalled();
		expect(send).toHaveBeenLastCalledWith(
			{
				arguments: {
					branchName: "feature",
					force: false,
					repositoryId: "repo",
				},
				commandType: "DeleteBranch",
			},
			{ timeoutMs: gitMutationTimeoutMs },
		);

		send.mockResolvedValueOnce(undefined);
		await act(() => result.current.deleteBranch(true));
		expect(result.current.deleteBranchName).toBeNull();
		expect(callbacks.onRepositoryChanged).toHaveBeenCalledOnce();
	});

	it("blocks overlapping mutations until the active request settles", async () => {
		let finish: (() => void) | undefined;
		send.mockImplementationOnce(
			() => new Promise<void>((resolve) => (finish = resolve)),
		);
		const { result } = renderHook(() =>
			useTestBranchMutations(createCallbacks()),
		);

		act(() => result.current.manageBranch("checkout", "feature"));
		await waitFor(() => expect(result.current.busyBranch).toBe("feature"));
		act(() => result.current.manageBranch("checkout", "other"));

		expect(send).toHaveBeenCalledOnce();
		await act(async () => finish?.());
		await waitFor(() => expect(result.current.busyBranch).toBeNull());
	});

	it("does not push without a configured repository and remote", () => {
		const callbacks = createCallbacks();
		const { result } = renderHook(() =>
			useBranchMutations({
				...callbacks,
				currentBranchName: "main",
				remoteName: null,
				repositoryId: null,
			}),
		);

		act(() => result.current.manageBranch("push", "main"));

		expect(send).not.toHaveBeenCalled();
		expect(result.current.busyBranch).toBeNull();
	});
});

function createCallbacks() {
	return {
		onCurrentBranchNameChange: vi.fn(),
		onRepositoryChanged: vi.fn(),
		onUpstreamChanged: vi.fn(),
	};
}

function useTestBranchMutations(callbacks: ReturnType<typeof createCallbacks>) {
	return useBranchMutations({
		...callbacks,
		currentBranchName: "main",
		remoteName: "origin",
		repositoryId: "repo",
	});
}
