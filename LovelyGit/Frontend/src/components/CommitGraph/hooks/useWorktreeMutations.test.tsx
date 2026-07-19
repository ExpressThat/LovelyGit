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
		const { result } = renderController(vi.fn());
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
		const { result } = renderController(vi.fn());
		act(() => result.current.manage("Remove", worktree));

		await act(() => result.current.mutate("Remove", worktree));

		expect(reconcileRepositoryRemoval).toHaveBeenCalledWith("linked-id");
		expect(result.current.removeTarget).toBeNull();
	});

	it("keeps a failed removal retryable without changing repositories", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("worktree is dirty"))
			.mockResolvedValueOnce(null);
		const worktree = linkedWorktree();
		const { result } = renderController(vi.fn());
		act(() => result.current.manage("Remove", worktree));

		await act(() => result.current.mutate("Remove", worktree));

		expect(reconcileRepositoryRemoval).not.toHaveBeenCalled();
		expect(result.current.removeTarget).toEqual(worktree);
		expect(result.current.busyPath).toBeNull();
		await act(() => result.current.mutate("Remove", worktree, { force: true }));
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(2);
	});
});

function renderController(onRepositoryChanged: () => void) {
	return renderHook(() =>
		useWorktreeMutations({ onRepositoryChanged, repositoryId: "repo" }),
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
