import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { applyOptimisticDiscard } from "./OptimisticWorkingTreeDiscard";
import { discardWorkingChanges } from "./WorkingChangesDiscardCommand";
import { waitForWorkingTreePaint } from "./WorkingTreePaintBoundary";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("./WorkingTreePaintBoundary", () => ({
	waitForWorkingTreePaint: vi.fn().mockResolvedValue(undefined),
}));

const send = vi.mocked(sendRequestWithResponse);
const waitForPaint = vi.mocked(waitForWorkingTreePaint);

describe("discardWorkingChanges", () => {
	beforeEach(() => {
		send.mockReset();
		waitForPaint.mockClear();
	});

	it("hides confirmed files before the native discard completes", async () => {
		let complete: (value: undefined) => void = () => undefined;
		send.mockReturnValueOnce(new Promise((resolve) => (complete = resolve)));
		const harness = createHarness();

		const discarding = discardWorkingChanges(harness);

		expect(harness.setOptimisticChanges).toHaveBeenCalledWith(
			expect.objectContaining({ totalCount: 1, unstaged: [] }),
		);
		expect(harness.setDiscardFiles).toHaveBeenCalledWith([]);
		expect(harness.onRefresh).not.toHaveBeenCalled();
		expect(waitForPaint).toHaveBeenCalledOnce();
		expect(send).not.toHaveBeenCalled();
		complete(undefined);
		await discarding;

		expect(harness.onRefresh).toHaveBeenCalledOnce();
		expect(harness.setOptimisticChanges).toHaveBeenLastCalledWith(null);
		expect(harness.setIsMutating).toHaveBeenLastCalledWith(false);
	});

	it("rolls back the preview when the native discard fails", async () => {
		send.mockRejectedValueOnce(new Error("restore failed"));
		const harness = createHarness();

		await discardWorkingChanges(harness);

		expect(harness.setOptimisticChanges).toHaveBeenLastCalledWith(null);
		expect(harness.onRefresh).not.toHaveBeenCalled();
		expect(harness.setActionError).toHaveBeenLastCalledWith("restore failed");
		expect(harness.setIsMutating).toHaveBeenLastCalledWith(false);
	});

	it("keeps the accurate preview when only reconciliation fails", async () => {
		send.mockResolvedValueOnce(undefined);
		const harness = createHarness();
		harness.onRefresh.mockRejectedValueOnce(new Error("refresh failed"));

		await discardWorkingChanges(harness);

		expect(harness.setOptimisticChanges).toHaveBeenCalledOnce();
		expect(harness.setActionError).toHaveBeenLastCalledWith("refresh failed");
		expect(harness.setIsMutating).toHaveBeenLastCalledWith(false);
	});

	it("does nothing without a current response", async () => {
		const harness = createHarness();
		harness.changes = null;

		await discardWorkingChanges(harness);

		expect(send).not.toHaveBeenCalled();
		expect(harness.setOptimisticChanges).not.toHaveBeenCalled();
	});
});

describe("applyOptimisticDiscard", () => {
	it("removes only working entries and preserves a staged sibling", () => {
		const changes = response();
		changes.staged = [file("Unstaged", "same.txt")];
		changes.staged[0] = { ...changes.staged[0], group: "Staged" };
		changes.unstaged = [file("Unstaged", "same.txt")];
		changes.untracked = [file("Untracked", "new.txt")];
		changes.unmerged = [file("Unmerged", "conflict.txt")];
		changes.totalCount = 4;

		const next = applyOptimisticDiscard(changes, [
			changes.unstaged[0],
			changes.untracked[0],
		]);

		expect(next.staged).toHaveLength(1);
		expect(next.unmerged).toHaveLength(1);
		expect(next.unstaged).toEqual([]);
		expect(next.untracked).toEqual([]);
		expect(next.totalCount).toBe(2);
	});

	it("prepares a thousand-file preview within one interaction frame", () => {
		const changes = response();
		changes.unstaged = Array.from({ length: 20_000 }, (_, index) =>
			file("Unstaged", `src/file-${index.toString().padStart(5, "0")}.ts`),
		);
		changes.totalCount = changes.unstaged.length;
		const discarded = changes.unstaged.slice(0, 1_000);
		const startedAt = performance.now();

		const next = applyOptimisticDiscard(changes, discarded);
		const elapsed = performance.now() - startedAt;
		console.info(
			`Optimistic 1,000-of-20,000 discard: ${elapsed.toFixed(2)} ms`,
		);

		expect(next.unstaged).toHaveLength(19_000);
		expect(next.totalCount).toBe(19_000);
		expect(elapsed).toBeLessThan(20);
	});
});

function createHarness() {
	const changes = response();
	const discarded = file("Unstaged", "changed.txt");
	changes.unstaged = [discarded];
	changes.staged = [file("Staged", "staged.txt")];
	changes.totalCount = 2;
	return {
		changes: changes as WorkingTreeChangesResponse | null,
		discardFiles: [discarded],
		onRefresh: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
		repositoryId: "repo",
		setActionError: vi.fn(),
		setDiscardFiles: vi.fn(),
		setIsMutating: vi.fn(),
		setOptimisticChanges: vi.fn(),
		setSelectedKeys: vi.fn(),
	};
}

function response(): WorkingTreeChangesResponse {
	return {
		isComplete: true,
		staged: [],
		totalCount: 0,
		unmerged: [],
		unstaged: [],
		untracked: [],
	};
}

function file(
	group: WorkingTreeChangedFile["group"],
	path: string,
): WorkingTreeChangedFile {
	return {
		additions: 0,
		deletions: 0,
		group,
		isBinary: false,
		oldPath: null,
		path,
		status: "Modified",
	};
}
