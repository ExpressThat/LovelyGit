import { describe, expect, it } from "vitest";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { applyOptimisticStash } from "./OptimisticWorkingTreeStash";

describe("optimistic stash state", () => {
	it("removes every stashable entry after an all-changes success", () => {
		const changes = response();

		const next = applyOptimisticStash(changes, false, [], true);

		expect(next.staged).toEqual([]);
		expect(next.unstaged).toEqual([]);
		expect(next.untracked).toEqual([]);
		expect(next.unmerged).toBe(changes.unmerged);
		expect(next.totalCount).toBe(1);
	});

	it("retains untracked entries when they were excluded", () => {
		const changes = response();

		const next = applyOptimisticStash(changes, false, [], false);

		expect(next.staged).toEqual([]);
		expect(next.unstaged).toEqual([]);
		expect(next.untracked).toBe(changes.untracked);
		expect(next.totalCount).toBe(2);
	});

	it("removes staged and working siblings for selected paths only", () => {
		const changes = response();

		const next = applyOptimisticStash(changes, true, ["shared.txt"], true);

		expect(next.staged.map((file) => file.path)).toEqual([]);
		expect(next.unstaged.map((file) => file.path)).toEqual(["other.txt"]);
		expect(next.untracked.map((file) => file.path)).toEqual(["new.txt"]);
		expect(next.totalCount).toBe(3);
	});
});

function response(): WorkingTreeChangesResponse {
	return {
		isComplete: true,
		staged: [file("shared.txt", "Staged")],
		unstaged: [
			file("shared.txt", "Unstaged"),
			file("other.txt", "Unstaged"),
		],
		untracked: [file("new.txt", "Untracked")],
		unmerged: [file("conflict.txt", "Unmerged")],
		totalCount: 5,
	};
}

function file(
	path: string,
	group: WorkingTreeChangedFile["group"],
): WorkingTreeChangedFile {
	return {
		additions: 1,
		deletions: 0,
		group,
		isBinary: false,
		oldPath: null,
		path,
		status: "Modified",
	};
}
