import { beforeEach, describe, expect, it, vi } from "vitest";
import type { WorkingTreeChangesResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { ignoreWorkingTreePath } from "./WorkingChangesIgnoreCommand";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({ toast: { success: vi.fn() } }));
vi.mock("./WorkingTreePaintBoundary", () => ({
	waitForWorkingTreePaint: vi.fn().mockResolvedValue(undefined),
}));

describe("working changes ignore command", () => {
	beforeEach(() => vi.clearAllMocks());

	it("settles a local ignore before its authoritative refresh completes", async () => {
		mockIgnoreSuccess("Local");
		let finishRefresh: (() => void) | undefined;
		const onRefresh = vi.fn(
			() => new Promise<void>((resolve) => (finishRefresh = resolve)),
		);
		const harness = createHarness("Local", onRefresh);

		await ignoreWorkingTreePath(harness);

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: { path: "notes.local", repositoryId: "repo", target: "Local" },
			commandType: "IgnoreWorkingTreePath",
		});
		expect(onRefresh).toHaveBeenCalledOnce();
		expect(harness.clearOptimisticChanges).not.toHaveBeenCalled();
		expect(harness.setIsMutating).toHaveBeenLastCalledWith(false);
		finishRefresh?.();
		await vi.waitFor(() =>
			expect(harness.clearOptimisticChanges).toHaveBeenCalledOnce(),
		);
	});

	it("restores the authoritative view when the ignore write fails", async () => {
		vi.mocked(sendRequestWithResponse).mockRejectedValueOnce(
			new Error("ignore write failed"),
		);
		const harness = createHarness("Local", vi.fn());

		await ignoreWorkingTreePath(harness);

		expect(harness.setOptimisticChanges).toHaveBeenLastCalledWith(null);
		expect(harness.setActionError).toHaveBeenLastCalledWith(
			"ignore write failed",
		);
	});

	it("keeps the optimistic result when a local refresh fails", async () => {
		mockIgnoreSuccess("Local");
		const harness = createHarness(
			"Local",
			vi.fn().mockRejectedValue(new Error("refresh failed")),
		);

		await ignoreWorkingTreePath(harness);

		await vi.waitFor(() =>
			expect(harness.setActionError).toHaveBeenCalledWith(
				"The path was ignored, but its status could not be refreshed.",
			),
		);
		expect(harness.setOptimisticChanges).toHaveBeenCalledOnce();
		expect(harness.clearOptimisticChanges).not.toHaveBeenCalled();
	});

	it("does not surface a stale refresh failure over a newer mutation", async () => {
		mockIgnoreSuccess("Local");
		let failRefresh: ((error: Error) => void) | undefined;
		const harness = createHarness(
			"Local",
			() => new Promise<void>((_, reject) => (failRefresh = reject)),
		);

		await ignoreWorkingTreePath(harness);
		harness.isOptimisticChangesCurrent.mockReturnValue(false);
		failRefresh?.(new Error("stale refresh failed"));
		await vi.waitFor(() =>
			expect(harness.isOptimisticChangesCurrent).toHaveBeenCalled(),
		);

		expect(harness.setActionError).toHaveBeenLastCalledWith(null);
		expect(harness.clearOptimisticChanges).not.toHaveBeenCalled();
	});

	it("keeps shared-ignore controls busy until .gitignore is refreshed", async () => {
		mockIgnoreSuccess("Shared");
		let finishRefresh: (() => void) | undefined;
		const harness = createHarness(
			"Shared",
			() => new Promise<void>((resolve) => (finishRefresh = resolve)),
		);
		const operation = ignoreWorkingTreePath(harness);

		await vi.waitFor(() => expect(finishRefresh).toBeTypeOf("function"));
		expect(harness.setIsMutating).not.toHaveBeenLastCalledWith(false);
		finishRefresh?.();
		await operation;
		expect(harness.clearOptimisticChanges).toHaveBeenCalledOnce();
		expect(harness.setIsMutating).toHaveBeenLastCalledWith(false);
	});

	it("settles a shared ignore after exact .gitignore status arrives", async () => {
		mockIgnoreSuccess("Shared", changesWithGitIgnore());
		let finishRefresh: (() => void) | undefined;
		const harness = createHarness(
			"Shared",
			() => new Promise<void>((resolve) => (finishRefresh = resolve)),
		);

		await ignoreWorkingTreePath(harness);

		expect(finishRefresh).toBeTypeOf("function");
		expect(harness.setOptimisticChanges).toHaveBeenCalledTimes(2);
		expect(harness.setIsMutating).toHaveBeenLastCalledWith(false);
		expect(harness.clearOptimisticChanges).not.toHaveBeenCalled();
		finishRefresh?.();
		await vi.waitFor(() =>
			expect(harness.clearOptimisticChanges).toHaveBeenCalledOnce(),
		);
	});
});

function mockIgnoreSuccess(
	target: "Local" | "Shared",
	targetChanges?: WorkingTreeChangesResponse,
) {
	vi.mocked(sendRequestWithResponse).mockResolvedValueOnce({
		added: true,
		pattern: "/notes.local",
		target,
		targetChanges: targetChanges ?? null,
	});
}

function createHarness(
	target: "Local" | "Shared",
	onRefresh: () => Promise<void> | void,
) {
	return {
		changes: changesWithUntracked(),
		clearOptimisticChanges: vi.fn(),
		isOptimisticChangesCurrent: vi.fn(() => true),
		onRefresh,
		path: "notes.local",
		repositoryId: "repo",
		setActionError: vi.fn(),
		setIsMutating: vi.fn(),
		setOptimisticChanges: vi.fn(),
		target,
	};
}

function changesWithUntracked(): WorkingTreeChangesResponse {
	return {
		isComplete: true,
		staged: [],
		totalCount: 1,
		unmerged: [],
		unstaged: [],
		untracked: [
			{
				additions: 0,
				deletions: 0,
				group: "Untracked",
				isBinary: false,
				oldPath: null,
				path: "notes.local",
				status: "Added",
			},
		],
	};
}

function changesWithGitIgnore(): WorkingTreeChangesResponse {
	const changes = changesWithUntracked();
	changes.untracked = [{ ...changes.untracked[0], path: ".gitignore" }];
	return changes;
}
