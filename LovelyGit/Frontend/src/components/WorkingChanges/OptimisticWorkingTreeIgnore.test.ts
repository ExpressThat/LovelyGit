import { describe, expect, it } from "vitest";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import {
	applyOptimisticIgnore,
	clearCompletedOptimisticIgnore,
	mergeTargetedStatus,
} from "./OptimisticWorkingTreeIgnore";

describe("applyOptimisticIgnore", () => {
	it("removes every untracked entry for the ignored path", () => {
		const changes = response([
			file("a.txt"),
			file("notes.local"),
			file("notes.local"),
		]);

		const result = applyOptimisticIgnore(changes, "notes.local");

		expect(result.untracked.map(({ path }) => path)).toEqual(["a.txt"]);
		expect(result.totalCount).toBe(1);
		expect(changes.untracked).toHaveLength(3);
	});

	it("returns the existing response when the path is absent", () => {
		const changes = response([file("a.txt"), file("z.txt")]);

		expect(applyOptimisticIgnore(changes, "missing.txt")).toBe(changes);
	});

	it("remains within the interaction budget for 20,000 files", () => {
		const changes = response(
			Array.from({ length: 20_000 }, (_, index) =>
				file(`path-${index.toString().padStart(5, "0")}.txt`),
			),
		);
		const startedAt = performance.now();

		const result = applyOptimisticIgnore(changes, "path-10000.txt");
		const elapsed = performance.now() - startedAt;

		expect(result.totalCount).toBe(19_999);
		expect(elapsed).toBeLessThan(10);
	});

	it("clears only the optimistic view owned by the completed ignore", () => {
		const ignored = response([]);
		const newer = response([file("later.txt")]);

		expect(
			clearCompletedOptimisticIgnore(
				{ changes: ignored, repositoryId: "repo" },
				"repo",
				ignored,
			),
		).toBeNull();
		expect(
			clearCompletedOptimisticIgnore(
				{ changes: newer, repositoryId: "repo" },
				"repo",
				ignored,
			),
		).toEqual({ changes: newer, repositoryId: "repo" });
	});

	it("merges exact .gitignore status without rebuilding unrelated lists", () => {
		const changes = response([file("notes.local")]);
		changes.unstaged = [file("source.ts", "Unstaged")];
		changes.totalCount = 2;
		const targeted = response([file(".gitignore")]);

		const result = mergeTargetedStatus(
			applyOptimisticIgnore(changes, "notes.local"),
			targeted,
			".gitignore",
		);

		expect(result.untracked.map(({ path }) => path)).toEqual([".gitignore"]);
		expect(result.unstaged).toBe(changes.unstaged);
		expect(result.totalCount).toBe(2);
	});

	it("replaces stale .gitignore entries across every status group", () => {
		const changes = response([file(".gitignore"), file("notes.local")]);
		changes.staged = [file(".gitignore", "Staged")];
		changes.unstaged = [file(".gitignore", "Unstaged")];
		const targeted = response([]);
		targeted.staged = [file(".gitignore", "Staged")];

		const result = mergeTargetedStatus(changes, targeted, ".gitignore");

		expect(result.staged).toHaveLength(1);
		expect(result.unstaged).toHaveLength(0);
		expect(result.untracked.map(({ path }) => path)).toEqual(["notes.local"]);
		expect(result.totalCount).toBe(2);
	});
});

function response(
	untracked: WorkingTreeChangedFile[],
): WorkingTreeChangesResponse {
	return {
		isComplete: true,
		staged: [],
		totalCount: untracked.length,
		unmerged: [],
		unstaged: [],
		untracked,
	};
}

function file(
	path: string,
	group: WorkingTreeChangedFile["group"] = "Untracked",
): WorkingTreeChangedFile {
	return {
		additions: 0,
		deletions: 0,
		group,
		isBinary: false,
		oldPath: null,
		path,
		status: "Added",
	};
}
