import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	NativeMessageType,
	NativeMessageTypesWithResponse,
} from "@/generated/native-message-contracts";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { runIndexCommand } from "./WorkingChangesPanelCommands";
import { waitForWorkingTreePaint } from "./WorkingTreePaintBoundary";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({ toast: { success: vi.fn() } }));
vi.mock("./WorkingTreePaintBoundary", () => ({
	waitForWorkingTreePaint: vi.fn().mockResolvedValue(undefined),
}));

describe("working changes index commands", () => {
	beforeEach(() => vi.clearAllMocks());

	it("waits for native stage and unstage outcomes before reconciliation", () => {
		for (const command of [
			NativeMessageType.StageWorkingTreeFiles,
			NativeMessageType.UnstageWorkingTreeFiles,
			NativeMessageType.StageWorkingTreeLine,
			NativeMessageType.UnstageWorkingTreeLine,
			NativeMessageType.StageWorkingTreeHunk,
			NativeMessageType.UnstageWorkingTreeHunk,
		]) {
			expect(NativeMessageTypesWithResponse).toContain(command);
		}
	});

	it("publishes the intended stage state before waiting for Git", async () => {
		const command = deferred<unknown>();
		vi.mocked(sendRequestWithResponse).mockReturnValue(command.promise);
		const harness = createHarness();

		const pending = runIndexCommand(harness);

		expect(harness.setOptimisticChanges).toHaveBeenCalledWith(
			expect.objectContaining({
				isComplete: true,
				staged: [expect.objectContaining({ path: "file.ts" })],
				unstaged: [],
			}),
		);
		expect(harness.onRefresh).not.toHaveBeenCalled();
		expect(waitForWorkingTreePaint).toHaveBeenCalledOnce();
		expect(sendRequestWithResponse).not.toHaveBeenCalled();

		command.resolve(undefined);
		await pending;
		expect(harness.onRefresh).toHaveBeenCalledOnce();
		expect(harness.setOptimisticChanges).toHaveBeenLastCalledWith(null);
	});

	it("rolls the preview back and permits retry when Git rejects the mutation", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("index locked"))
			.mockResolvedValueOnce(undefined);
		const harness = createHarness();

		await runIndexCommand(harness);

		expect(harness.setOptimisticChanges).toHaveBeenLastCalledWith(null);
		expect(harness.setActionError).toHaveBeenLastCalledWith("index locked");
		expect(harness.onRefresh).not.toHaveBeenCalled();
		expect(harness.setIsMutating).toHaveBeenLastCalledWith(false);

		await runIndexCommand(harness);
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(2);
		expect(harness.onRefresh).toHaveBeenCalledOnce();
	});

	it("keeps the successful preview when only reconciliation fails", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(undefined);
		const harness = createHarness();
		harness.onRefresh.mockRejectedValueOnce(new Error("status unavailable"));

		await runIndexCommand(harness);

		expect(harness.setOptimisticChanges).not.toHaveBeenLastCalledWith(null);
		expect(harness.setActionError).toHaveBeenLastCalledWith(
			"status unavailable",
		);
		expect(harness.setIsMutating).toHaveBeenLastCalledWith(false);
	});
});

function createHarness() {
	const changed = file();
	return {
		changes: response(changed),
		commandType: "StageWorkingTreeFiles" as const,
		files: [changed],
		includeAll: false,
		onRefresh: vi.fn().mockResolvedValue(undefined),
		repositoryId: "repo",
		setActionError: vi.fn(),
		setIsMutating: vi.fn(),
		setOptimisticChanges: vi.fn(),
		setSelectedKeys: vi.fn(),
	};
}

function response(changed: WorkingTreeChangedFile): WorkingTreeChangesResponse {
	return {
		isComplete: true,
		staged: [],
		totalCount: 1,
		unmerged: [],
		unstaged: [changed],
		untracked: [],
	};
}

function file(): WorkingTreeChangedFile {
	return {
		additions: 1,
		deletions: 0,
		group: "Unstaged",
		isBinary: false,
		oldPath: null,
		path: "file.ts",
		status: "Modified",
	};
}

function deferred<T>() {
	let resolve = (_value: T) => undefined;
	const promise = new Promise<T>((complete) => {
		resolve = (value) => {
			complete(value);
			return undefined;
		};
	});
	return { promise, resolve };
}
