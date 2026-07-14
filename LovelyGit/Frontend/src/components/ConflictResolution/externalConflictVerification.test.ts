import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { loadWorkingTreeChanges } from "@/components/WorkingChanges/WorkingTreeChangesRequests";
import { verifyExternalConflictResolved } from "./externalConflictVerification";

vi.mock("@/components/WorkingChanges/WorkingTreeChangesRequests", () => ({
	loadWorkingTreeChanges: vi.fn(),
}));

const load = vi.mocked(loadWorkingTreeChanges);
const status = (unmerged: boolean) => ({
	isComplete: true,
	staged: [],
	unstaged: [],
	untracked: [],
	unmerged: unmerged
		? [
				{
					additions: 0,
					deletions: 0,
					group: "Unmerged" as const,
					isBinary: false,
					oldPath: null,
					path: "file.txt",
					status: "Unmerged",
				},
			]
		: [],
	totalCount: unmerged ? 1 : 0,
});

describe("verifyExternalConflictResolved", () => {
	beforeEach(() => {
		vi.useFakeTimers();
		load.mockReset();
	});
	afterEach(() => vi.useRealTimers());

	it("waits through transient unmerged reads", async () => {
		load
			.mockResolvedValueOnce(status(true))
			.mockResolvedValueOnce(status(false));
		const verification = verifyExternalConflictResolved("repo", "file.txt");
		await vi.advanceTimersByTimeAsync(250);
		await expect(verification).resolves.toBeUndefined();
		expect(load).toHaveBeenCalledTimes(2);
	});

	it("rejects after the file remains unmerged for the full window", async () => {
		load.mockResolvedValue(status(true));
		const verification = verifyExternalConflictResolved("repo", "file.txt");
		const assertion =
			expect(verification).rejects.toThrow(/without resolving/i);
		await vi.advanceTimersByTimeAsync(12_000);
		await assertion;
		expect(load).toHaveBeenCalledTimes(7);
	});
});
