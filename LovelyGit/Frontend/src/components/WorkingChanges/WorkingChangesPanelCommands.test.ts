import { beforeEach, describe, expect, it, vi } from "vitest";
import type { WorkingTreeChangesResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import {
	commitStagedChanges,
	ignoreWorkingTreePath,
	loadHeadCommitMessage,
} from "./WorkingChangesPanelCommands";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({ toast: { success: vi.fn() } }));
vi.mock("./WorkingTreePaintBoundary", () => ({
	waitForWorkingTreePaint: vi.fn().mockResolvedValue(undefined),
}));

describe("working changes commit commands", () => {
	beforeEach(() => vi.clearAllMocks());

	it("does not create a normal commit without staged changes", async () => {
		const harness = createCommitHarness({ amend: false });

		await commitStagedChanges(harness);

		expect(sendRequestWithResponse).not.toHaveBeenCalled();
	});

	it("allows a message-only amend and clears the form after success", async () => {
		const harness = createCommitHarness({ amend: true });

		await commitStagedChanges(harness);

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: {
				amend: true,
				body: "Updated body",
				repositoryId: "repo",
				sign: true,
				title: "Updated title",
			},
			commandType: "CommitStagedChanges",
		});
		expect(harness.setCommitTitle).toHaveBeenCalledWith("");
		expect(harness.setCommitBody).toHaveBeenCalledWith("");
		expect(harness.setIsAmending).toHaveBeenCalledWith(false);
		expect(harness.onCommitSuccess).toHaveBeenCalledOnce();
	});

	it("keeps the amend form intact and shows native failures", async () => {
		vi.mocked(sendRequestWithResponse).mockRejectedValueOnce(
			new Error("signing failed"),
		);
		const harness = createCommitHarness({ amend: true });

		await commitStagedChanges(harness);

		expect(harness.setActionError).toHaveBeenLastCalledWith("signing failed");
		expect(harness.setCommitTitle).not.toHaveBeenCalled();
		expect(harness.setIsCommitting).toHaveBeenLastCalledWith(false);
	});

	it("loads the HEAD message through the dedicated native read", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce({
			body: "Body",
			hash: "a".repeat(40),
			title: "Title",
		});

		await expect(loadHeadCommitMessage("repo")).resolves.toMatchObject({
			title: "Title",
		});
		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: { repositoryId: "repo" },
			commandType: "GetHeadCommitMessage",
		});
	});

	it("ignores an exact path locally and refreshes the native status", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce({
			added: true,
			pattern: "/notes.local",
			target: "Local",
		});
		const onRefresh = vi.fn();
		const setActionError = vi.fn();
		const setIsMutating = vi.fn();
		const setOptimisticChanges = vi.fn();
		const changes = changesWithUntracked("notes.local");

		const operation = ignoreWorkingTreePath({
			changes,
			onRefresh,
			path: "notes.local",
			repositoryId: "repo",
			setActionError,
			setIsMutating,
			setOptimisticChanges,
			target: "Local",
		});
		expect(setOptimisticChanges).toHaveBeenNthCalledWith(
			1,
			expect.objectContaining({ totalCount: 0, untracked: [] }),
		);
		await operation;

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: {
				path: "notes.local",
				repositoryId: "repo",
				target: "Local",
			},
			commandType: "IgnoreWorkingTreePath",
		});
		expect(onRefresh).toHaveBeenCalledOnce();
		expect(setOptimisticChanges).toHaveBeenLastCalledWith(null);
		expect(setIsMutating).toHaveBeenNthCalledWith(1, true);
		expect(setIsMutating).toHaveBeenLastCalledWith(false);
	});

	it("restores the authoritative view when ignore fails", async () => {
		vi.mocked(sendRequestWithResponse).mockRejectedValueOnce(
			new Error("ignore write failed"),
		);
		const setActionError = vi.fn();
		const setOptimisticChanges = vi.fn();

		await ignoreWorkingTreePath({
			changes: changesWithUntracked("notes.local"),
			onRefresh: vi.fn(),
			path: "notes.local",
			repositoryId: "repo",
			setActionError,
			setIsMutating: vi.fn(),
			setOptimisticChanges,
			target: "Local",
		});

		expect(setOptimisticChanges).toHaveBeenLastCalledWith(null);
		expect(setActionError).toHaveBeenLastCalledWith("ignore write failed");
	});

	it("keeps the optimistic result when refresh fails after the ignore write", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce({
			added: true,
			pattern: "/notes.local",
			target: "Local",
		});
		const setOptimisticChanges = vi.fn();

		await ignoreWorkingTreePath({
			changes: changesWithUntracked("notes.local"),
			onRefresh: vi.fn().mockRejectedValue(new Error("refresh failed")),
			path: "notes.local",
			repositoryId: "repo",
			setActionError: vi.fn(),
			setIsMutating: vi.fn(),
			setOptimisticChanges,
			target: "Local",
		});

		expect(setOptimisticChanges).toHaveBeenCalledOnce();
		expect(setOptimisticChanges).not.toHaveBeenCalledWith(null);
	});
});

function createCommitHarness({ amend }: { amend: boolean }) {
	return {
		amend,
		changes: emptyChanges(),
		commitBody: "Updated body",
		commitTitle: "Updated title",
		onCommitSuccess: vi.fn(),
		repositoryId: "repo",
		sign: true,
		setActionError: vi.fn(),
		setCommitBody: vi.fn(),
		setCommitTitle: vi.fn(),
		setIsAmending: vi.fn(),
		setIsCommitting: vi.fn(),
		setSelectedKeys: vi.fn(),
	};
}

function emptyChanges(): WorkingTreeChangesResponse {
	return {
		isComplete: true,
		staged: [],
		totalCount: 0,
		unmerged: [],
		unstaged: [],
		untracked: [],
	};
}

function changesWithUntracked(path: string): WorkingTreeChangesResponse {
	return {
		...emptyChanges(),
		totalCount: 1,
		untracked: [
			{
				additions: 0,
				deletions: 0,
				group: "Untracked",
				isBinary: false,
				oldPath: null,
				path,
				status: "Added",
			},
		],
	};
}
