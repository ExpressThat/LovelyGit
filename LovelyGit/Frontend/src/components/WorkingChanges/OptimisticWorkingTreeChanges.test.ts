import { describe, expect, it } from "vitest";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import {
	applyObservedWorkingTreeChanges,
	countObservedNewPaths,
	shouldApplyObservedWorkingTreeChanges,
} from "./OptimisticWorkingTreeChanges";

describe("applyObservedWorkingTreeChanges", () => {
	it("adds an observed untracked file to an empty state", () => {
		const next = applyObservedWorkingTreeChanges(null, [
			file("deep/path/new.txt", "Added", "Untracked"),
		]);

		expect(next?.untracked.map((change) => change.path)).toEqual([
			"deep/path/new.txt",
		]);
		expect(next?.totalCount).toBe(1);
	});

	it("moves an observed path to its latest group", () => {
		const current = response([file("src/app.ts", "Added", "Untracked")]);
		const next = applyObservedWorkingTreeChanges(current, [
			file("src/app.ts", "Modified", "Unstaged"),
		]);

		expect(next?.untracked).toEqual([]);
		expect(next?.unstaged.map((change) => change.path)).toEqual(["src/app.ts"]);
		expect(next?.totalCount).toBe(1);
	});

	it("removes an observed untracked file when it is deleted", () => {
		const current = response([file("scratch.txt", "Added", "Untracked")]);
		const next = applyObservedWorkingTreeChanges(current, [
			file("scratch.txt", "Deleted", "Unstaged"),
		]);

		expect(next?.untracked).toEqual([]);
		expect(next?.unstaged).toEqual([]);
		expect(next?.totalCount).toBe(0);
	});
});

describe("countObservedNewPaths", () => {
	it("counts only paths not already present", () => {
		const current = response([file("existing.txt", "Added", "Untracked")]);

		expect(
			countObservedNewPaths(current, [
				file("existing.txt", "Added", "Untracked"),
				file("new.txt", "Added", "Untracked"),
			]),
		).toBe(1);
	});
});

describe("shouldApplyObservedWorkingTreeChanges", () => {
	it("applies small event batches optimistically", () => {
		expect(
			shouldApplyObservedWorkingTreeChanges([
				file("small.txt", "Modified", "Unstaged"),
			]),
		).toBe(true);
	});

	it("skips large event batches until an authoritative reload", () => {
		const files = Array.from({ length: 26 }, (_, index) =>
			file(`large-${index}.txt`, "Added", "Untracked"),
		);

		expect(shouldApplyObservedWorkingTreeChanges(files)).toBe(false);
	});
});

function response(files: WorkingTreeChangedFile[]): WorkingTreeChangesResponse {
	return {
		isComplete: true,
		staged: [],
		unstaged: [],
		untracked: files,
		unmerged: [],
		totalCount: files.length,
	};
}

function file(
	path: string,
	status: string,
	group: WorkingTreeChangedFile["group"],
): WorkingTreeChangedFile {
	return {
		path,
		oldPath: null,
		status,
		group,
		additions: 0,
		deletions: 0,
		isBinary: false,
	};
}
