import { describe, expect, it } from "vitest";
import type {
	CommitGraphRow,
	CommitInfo,
	CommitRefInfo,
	RepositoryRefItem,
} from "@/generated/types";
import {
	buildRefPanelSections,
	buildRefPanelSummary,
	filterRefPanelSections,
	refPanelItemToRefInfo,
} from "./RefsPanelData";

describe("buildRefPanelSections", () => {
	it("groups loaded refs by kind and puts the current branch first", () => {
		const sections = buildRefPanelSections({
			currentBranchName: "main",
			remotePrefixes: ["origin"],
			rows: [
				row("a", [
					ref("Remote", "origin/main"),
					ref("Local", "main"),
					ref("Tag", "v1"),
				]),
				row("b", [ref("Local", "feature/x")]),
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

	it("filters refs by name, display label, and hash", () => {
		const sections = buildRefPanelSections({
			currentBranchName: "main",
			remotePrefixes: ["origin"],
			rows: [
				row("abc1234", [
					ref("Local", "main"),
					ref("Remote", "origin/release/canary"),
				]),
				row("def5678", [ref("Tag", "v1.0.0")]),
			],
		});

		expect(filterRefPanelSections(sections, "canary")[0].items[0].name).toBe(
			"origin/release/canary",
		);
		expect(filterRefPanelSections(sections, "def")[0].items[0].name).toBe(
			"v1.0.0",
		);
		expect(filterRefPanelSections(sections, "missing")).toEqual([]);
	});

	it("uses complete repository refs before graph rows are loaded", () => {
		const sections = buildRefPanelSections({
			currentBranchName: "feature/x",
			remotePrefixes: ["origin"],
			refs: [
				repositoryRef("Local", "feature/x", "abc1234"),
				repositoryRef("Remote", "origin/main", "def5678"),
				repositoryRef("Tag", "v1", "fed4321", "https://example.test/v1"),
			],
			rows: [],
		});

		expect(sections.map((section) => section.label)).toEqual([
			"Branches",
			"Remote Branches",
			"Tags",
		]);
		expect(sections[0].items[0]).toMatchObject({
			isCurrent: true,
			name: "feature/x",
			row: null,
		});
		expect(refPanelItemToRefInfo(sections[2].items[0]).remoteUrl).toBe(
			"https://example.test/v1",
		);
	});

	it("maps sidebar items back to commit ref info for menus", () => {
		const sections = buildRefPanelSections({
			currentBranchName: null,
			remotePrefixes: ["origin"],
			rows: [
				row("abc1234", [
					ref("Tag", "v1", "https://github.com/example/repo/releases/tag/v1"),
				]),
			],
		});

		expect(refPanelItemToRefInfo(sections[0].items[0])).toEqual({
			kind: "Tag",
			name: "v1",
			remoteUrl: "https://github.com/example/repo/releases/tag/v1",
		});
	});
});

describe("buildRefPanelSummary", () => {
	it("counts ref kinds and normalizes the current branch label", () => {
		const sections = buildRefPanelSections({
			currentBranchName: "origin/main",
			remotePrefixes: ["origin"],
			rows: [
				row("abc1234", [
					ref("Local", "feature/x"),
					ref("Remote", "origin/main"),
					ref("Tag", "v1"),
					ref("Stash", "stash@{0}"),
				]),
			],
		});

		expect(
			buildRefPanelSummary({
				currentBranchName: "origin/main",
				remotePrefixes: ["origin"],
				sections,
			}),
		).toEqual({
			currentBranchLabel: "main",
			localBranchCount: 1,
			remoteBranchCount: 1,
			stashCount: 1,
			tagCount: 1,
			totalRefCount: 4,
		});
	});

	it("reports a detached current branch label when no branch is selected", () => {
		expect(
			buildRefPanelSummary({
				currentBranchName: null,
				remotePrefixes: ["origin"],
				sections: [],
			}).currentBranchLabel,
		).toBeNull();
	});
});

function row(hash: string, refs: CommitGraphRow["commit"]["refs"]) {
	return {
		activeLanesAbove: [],
		activeLanesBelow: [],
		commit: commit(hash, refs, [], []),
		colorIndex: 0,
		edgesAbove: [],
		edgesBelow: [],
		isBranchTip: false,
		isMergeCommit: false,
		lane: 0,
		laneColorsAbove: [],
		laneColorsBelow: [],
		rowIndex: 0,
	} satisfies CommitGraphRow;
}

function ref(
	kind: CommitRefInfo["kind"],
	name: string,
	remoteUrl: string | null = null,
) {
	return { kind, name, remoteUrl } satisfies CommitRefInfo;
}

function repositoryRef(
	kind: CommitRefInfo["kind"],
	name: string,
	commitHash: string,
	remoteUrl: string | null = null,
) {
	return { commitHash, kind, name, remoteUrl } satisfies RepositoryRefItem;
}

function legacyRow(hash: string, branches: string[], tags: string[]) {
	return {
		activeLanesAbove: [],
		activeLanesBelow: [],
		commit: commit(hash, [], branches, tags),
		colorIndex: 0,
		edgesAbove: [],
		edgesBelow: [],
		isBranchTip: false,
		isMergeCommit: false,
		lane: 0,
		laneColorsAbove: [],
		laneColorsBelow: [],
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
