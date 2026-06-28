import { describe, expect, it } from "vitest";
import type { CommitGraphRow, CommitInfo } from "@/generated/types";
import { buildRefPanelSections } from "./RefsPanelData";

describe("buildRefPanelSections", () => {
	it("groups loaded refs by kind and puts the current branch first", () => {
		const sections = buildRefPanelSections({
			currentBranchName: "main",
			remotePrefixes: ["origin"],
			rows: [
				row("a", [
					{ kind: "Remote", name: "origin/main" },
					{ kind: "Local", name: "main" },
					{ kind: "Tag", name: "v1" },
				]),
				row("b", [{ kind: "Local", name: "feature/x" }]),
			],
		});

		expect(sections.map((section) => section.label)).toEqual([
			"Branches",
			"Remote Branches",
			"Tags",
		]);
		expect(sections[0].items.map((item) => item.name)).toEqual([
			"main",
			"feature/x",
		]);
		expect(sections[1].items[0].label).toBe("main");
	});

	it("falls back to legacy branch and tag fields", () => {
		const sections = buildRefPanelSections({
			currentBranchName: null,
			remotePrefixes: ["origin"],
			rows: [legacyRow("a", ["origin/main"], ["v1"])],
		});

		expect(sections.map((section) => section.label)).toEqual([
			"Branches",
			"Tags",
		]);
		expect(sections[0].items[0].kind).toBe("Local");
	});
});

function row(hash: string, refs: CommitGraphRow["commit"]["refs"]) {
	return {
		activeLanesAbove: [],
		activeLanesBelow: [],
		commit: commit(hash, refs, [], []),
		edgesAbove: [],
		edgesBelow: [],
		isBranchTip: false,
		isMergeCommit: false,
		lane: 0,
		rowIndex: 0,
	} satisfies CommitGraphRow;
}

function legacyRow(hash: string, branches: string[], tags: string[]) {
	return {
		activeLanesAbove: [],
		activeLanesBelow: [],
		commit: commit(hash, [], branches, tags),
		edgesAbove: [],
		edgesBelow: [],
		isBranchTip: false,
		isMergeCommit: false,
		lane: 0,
		rowIndex: 0,
	} satisfies CommitGraphRow;
}

function commit(
	hash: string,
	refs: CommitInfo["refs"],
	branches: string[],
	tags: string[],
) {
	return {
		author: "Test Author",
		branches,
		date: 0,
		email: "test@example.invalid",
		hash,
		message: "Test commit",
		parents: [],
		refs,
		remoteRepositoryUrl: null,
		remoteUrl: null,
		stats: null,
		tags,
	} satisfies CommitInfo;
}
