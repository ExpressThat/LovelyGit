import { describe, expect, it } from "vitest";
import type { WorkingTreeChangedFile } from "@/generated/types";
import {
	countWorkingChanges,
	filterWorkingChanges,
	type WorkingChangesFilterState,
} from "./WorkingChangesFilterUtils";

describe("filterWorkingChanges", () => {
	it("filters by visible group without mutating the source response", () => {
		const changes = response([
			file("src/app.ts", "Staged", "Modified"),
			file("src/view.tsx", "Unstaged", "Modified"),
			file("notes.txt", "Untracked", "Added"),
			file("src/conflict.ts", "Unmerged", "Unmerged"),
		]);

		const filtered = filterWorkingChanges(changes, filter("Changes", ""));

		expect(filtered.staged).toEqual([]);
		expect(filtered.unstaged.map((item) => item.path)).toEqual([
			"src/view.tsx",
		]);
		expect(filtered.untracked.map((item) => item.path)).toEqual(["notes.txt"]);
		expect(filtered.totalCount).toBe(2);
		expect(countWorkingChanges(changes)).toBe(4);
	});

	it("matches path, old path, status, and group terms", () => {
		const changes = response([
			file("src/new-name.ts", "Unstaged", "Renamed", "src/old-name.ts"),
			file("docs/readme.md", "Staged", "Modified"),
		]);

		expect(paths(changes, filter("All", "old-name renamed"))).toEqual([
			"src/new-name.ts",
		]);
		expect(paths(changes, filter("All", "staged readme"))).toEqual([
			"docs/readme.md",
		]);
	});
});

function paths(
	changes: ReturnType<typeof response>,
	state: WorkingChangesFilterState,
) {
	const filtered = filterWorkingChanges(changes, state);
	return [
		...filtered.staged,
		...filtered.unstaged,
		...filtered.untracked,
		...filtered.unmerged,
	].map((item) => item.path);
}

function filter(
	group: WorkingChangesFilterState["group"],
	query: string,
): WorkingChangesFilterState {
	return { group, query };
}

function response(files: WorkingTreeChangedFile[]) {
	return {
		staged: files.filter((file) => file.group === "Staged"),
		unstaged: files.filter((file) => file.group === "Unstaged"),
		unmerged: files.filter((file) => file.group === "Unmerged"),
		untracked: files.filter((file) => file.group === "Untracked"),
		totalCount: files.length,
	};
}

function file(
	path: string,
	group: WorkingTreeChangedFile["group"],
	status: string,
	oldPath: string | null = null,
) {
	return {
		additions: 1,
		deletions: 0,
		group,
		isBinary: false,
		oldPath,
		path,
		status,
	} satisfies WorkingTreeChangedFile;
}
