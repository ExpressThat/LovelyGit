// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RepositoryWorktreeItem } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { nativeDialogTimeoutMs } from "@/lib/nativeDialogTimeout";
import { useWorktreeMutations } from "./useWorktreeMutations";

const reconcileRepository = vi.fn();
const reconcileRepositoryRemoval = vi.fn();
const setCurrentRepositoryId = vi.fn(async () => undefined);

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("@/lib/repositoryContext", () => ({
	useRepositoryContext: () => ({
		reconcileRepository,
		reconcileRepositoryRemoval,
		setCurrentRepositoryId,
	}),
}));

describe("useWorktreeMutations", () => {
	beforeEach(() => vi.clearAllMocks());

	it("allows time for a native worktree destination picker", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce({
			path: "C:/repo-demo",
		});
		const { result } = renderController(vi.fn());

		await act(() => result.current.chooseDestination());

		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			{ commandType: "ChooseWorktreeDestination" },
			{ timeoutMs: nativeDialogTimeoutMs },
		);
	});

	it("creates the selected branch worktree and refreshes native refs", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(undefined);
		const onRepositoryChanged = vi.fn();
		const { result } = renderController(onRepositoryChanged);
		act(() => result.current.setCreateBranchName("feature/demo"));

		await act(() => result.current.create("C:/repo-demo"));

		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			{
				arguments: {
					branchName: "feature/demo",
					repositoryId: "repo",
					worktreePath: "C:/repo-demo",
				},
				commandType: "CreateWorktree",
			},
			expect.anything(),
		);
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
		expect(result.current.createBranchName).toBeNull();
	});

	it("locks with a reason and closes the lifecycle dialog", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(null);
		const worktree = linkedWorktree();
		const onRepositoryChanged = vi.fn();
		const onWorktreeLockChanged = vi.fn();
		const { result } = renderController(
			onRepositoryChanged,
			onWorktreeLockChanged,
		);
		act(() => result.current.manage("Lock", worktree));

		await act(() =>
			result.current.mutate("Lock", worktree, { lockReason: "External drive" }),
		);

		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			expect.objectContaining({
				arguments: expect.objectContaining({
					action: "Lock",
					lockReason: "External drive",
					worktreePath: worktree.path,
				}),
				commandType: "ManageWorktree",
			}),
			expect.anything(),
		);
		expect(result.current.lockTarget).toBeNull();
		expect(onWorktreeLockChanged).toHaveBeenCalledWith(
			worktree.path,
			true,
			"External drive",
		);
		expect(onRepositoryChanged).not.toHaveBeenCalled();
	});

	it("keeps failed locking retryable and reconciles only after success", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("already locked"))
			.mockResolvedValueOnce(null);
		const worktree = linkedWorktree();
		const onWorktreeLockChanged = vi.fn();
		const { result } = renderController(vi.fn(), onWorktreeLockChanged);
		act(() => result.current.manage("Lock", worktree));

		await act(() => result.current.mutate("Lock", worktree));
		expect(result.current.lockTarget).toEqual(worktree);
		expect(onWorktreeLockChanged).not.toHaveBeenCalled();

		await act(() => result.current.mutate("Lock", worktree));
		expect(result.current.lockTarget).toBeNull();
		expect(onWorktreeLockChanged).toHaveBeenCalledWith(worktree.path, true, "");
	});

	it("unlocks locally without requesting a broad refs refresh", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(null);
		const worktree = {
			...linkedWorktree(),
			isLocked: true,
			lockReason: "Away",
		};
		const onRepositoryChanged = vi.fn();
		const onWorktreeLockChanged = vi.fn();
		const { result } = renderController(
			onRepositoryChanged,
			onWorktreeLockChanged,
		);

		await act(() => result.current.mutate("Unlock", worktree));

		expect(onWorktreeLockChanged).toHaveBeenCalledWith(
			worktree.path,
			false,
			"",
		);
		expect(onRepositoryChanged).not.toHaveBeenCalled();
	});

	it("reconciles and selects a worktree without reloading all repositories", async () => {
		const opened = {
			id: "linked-id",
			name: "repo-demo",
			path: "C:/repo-demo",
		};
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(opened);
		const { result } = renderController(vi.fn());

		await act(() => result.current.mutate("Open", linkedWorktree()));

		expect(reconcileRepository).toHaveBeenCalledWith(opened);
		expect(setCurrentRepositoryId).toHaveBeenCalledWith("linked-id");
	});

	it("forgets a removed registered worktree without reloading all repositories", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce({
			id: "linked-id",
			name: "repo-demo",
			path: "C:/repo-demo",
		});
		const worktree = linkedWorktree();
		const onRepositoryChanged = vi.fn();
		const onWorktreeRemoved = vi.fn();
		const { result } = renderController(
			onRepositoryChanged,
			vi.fn(),
			onWorktreeRemoved,
		);
		act(() => result.current.manage("Remove", worktree));

		await act(() => result.current.mutate("Remove", worktree));

		expect(reconcileRepositoryRemoval).toHaveBeenCalledWith("linked-id");
		expect(onWorktreeRemoved).toHaveBeenCalledWith(worktree.path);
		expect(onRepositoryChanged).not.toHaveBeenCalled();
		expect(result.current.removeTarget).toBeNull();
	});

	it("keeps a failed removal retryable without changing repositories", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("worktree is dirty"))
			.mockResolvedValueOnce(null);
		const worktree = linkedWorktree();
		const onWorktreeRemoved = vi.fn();
		const { result } = renderController(vi.fn(), vi.fn(), onWorktreeRemoved);
		act(() => result.current.manage("Remove", worktree));

		await act(() => result.current.mutate("Remove", worktree));

		expect(reconcileRepositoryRemoval).not.toHaveBeenCalled();
		expect(onWorktreeRemoved).not.toHaveBeenCalled();
		expect(result.current.removeTarget).toEqual(worktree);
		expect(result.current.busyPath).toBeNull();
		await act(() => result.current.mutate("Remove", worktree, { force: true }));
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(2);
		expect(onWorktreeRemoved).toHaveBeenCalledWith(worktree.path);
	});
});

function renderController(
	onRepositoryChanged: () => void,
	onWorktreeLockChanged = vi.fn(),
	onWorktreeRemoved = vi.fn(),
) {
	return renderHook(() =>
		useWorktreeMutations({
			onRepositoryChanged,
			onWorktreeLockChanged,
			onWorktreeRemoved,
			repositoryId: "repo",
		}),
	);
}

function linkedWorktree(): RepositoryWorktreeItem {
	return {
		branchName: "feature/demo",
		isCurrent: false,
		isLocked: false,
		lockReason: "",
		path: "C:/repo-demo",
	};
}
