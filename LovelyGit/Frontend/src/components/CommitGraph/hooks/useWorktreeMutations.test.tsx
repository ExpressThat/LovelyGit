// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RepositoryWorktreeItem } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { useWorktreeMutations } from "./useWorktreeMutations";

const reloadRepositories = vi.fn(async () => undefined);
const setCurrentRepositoryId = vi.fn(async () => undefined);

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("@/lib/repositoryContext", () => ({
	useRepositoryContext: () => ({ reloadRepositories, setCurrentRepositoryId }),
}));

describe("useWorktreeMutations", () => {
	beforeEach(() => vi.clearAllMocks());

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

	it("registers and selects a worktree opened in LovelyGit", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce({
			id: "linked-id",
			name: "repo-demo",
			path: "C:/repo-demo",
		});
		const { result } = renderController(vi.fn());

		await act(() => result.current.mutate("Open", linkedWorktree()));

		expect(reloadRepositories).toHaveBeenCalledOnce();
		expect(setCurrentRepositoryId).toHaveBeenCalledWith("linked-id");
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
