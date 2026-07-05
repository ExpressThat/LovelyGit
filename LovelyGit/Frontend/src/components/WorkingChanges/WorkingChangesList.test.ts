import { describe, expect, it } from "vitest";
import type { WorkingTreeChangedFile } from "@/generated/types";
import {
	flattenWorkingChangesForTests,
	splitWorkingChanges,
	stagedFilesOnly,
	workingFilesOnly,
} from "./WorkingChangesList";

describe("splitWorkingChanges", () => {
	it("splits files into unstaged and staged boxes", () => {
		const changes = {
			staged: [file("staged.txt", "Staged")],
			unstaged: [file("modified.txt", "Unstaged")],
			untracked: [file("new.txt", "Untracked")],
			unmerged: [file("conflict.txt", "Unmerged")],
			totalCount: 4,
		};

		const split = splitWorkingChanges(changes);

		expect(split.unstagedFiles.map((item) => item.path)).toEqual([
			"modified.txt",
			"new.txt",
			"conflict.txt",
		]);
		expect(split.stagedFiles.map((item) => item.path)).toEqual(["staged.txt"]);
	});

	it("keeps bulk action subsets scoped to ordinary working and staged files", () => {
		const files = [
			file("modified.txt", "Unstaged"),
			file("new.txt", "Untracked"),
			file("conflict.txt", "Unmerged"),
			file("staged.txt", "Staged"),
		];

		expect(workingFilesOnly(files).map((item) => item.path)).toEqual([
			"modified.txt",
			"new.txt",
		]);
		expect(stagedFilesOnly(files).map((item) => item.path)).toEqual([
			"staged.txt",
		]);
	});

	it("keeps the display order explicit", () => {
		const changes = {
			staged: [file("staged.txt", "Staged")],
			unstaged: [file("modified.txt", "Unstaged")],
			untracked: [file("new.txt", "Untracked")],
			unmerged: [file("conflict.txt", "Unmerged")],
			totalCount: 4,
		};

		expect(
			flattenWorkingChangesForTests(changes).map((item) => item.path),
		).toEqual(["modified.txt", "new.txt", "conflict.txt", "staged.txt"]);
	});
});

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
