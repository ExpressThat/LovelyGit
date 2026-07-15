import { describe, expect, it } from "vitest";
import type {
	CommitGraphRow,
	CommitInfo,
	CommitRefInfo,
	RepositoryRefItem,
} from "@/generated/types";
import {
	buildRefPanelSections,
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

	it("treats loaded repository refs as authoritative over stale graph rows", () => {
		const sections = buildRefPanelSections({
			currentBranchName: "main",
			refs: [repositoryRef("Local", "main", "new")],
			remotePrefixes: ["origin"],
			rows: [row("old", [ref("Local", "stale")])],
		});

		expect(sections[0]?.items.map((item) => item.name)).toEqual(["main"]);
	});

	it("does not resurrect graph refs when the authoritative list is empty", () => {
		const sections = buildRefPanelSections({
			currentBranchName: null,
			refs: [],
			remotePrefixes: [],
			rows: [row("old", [ref("Local", "stale")])],
		});

		expect(sections).toEqual([]);
	});

	it("builds refs from a large sparse graph within the interaction budget", () => {
		const rows = Array<CommitGraphRow | null>(500_000).fill(null);
		const refs = Array.from({ length: 500 }, (_, index) =>
			repositoryRef("Local", `branch/${index}`, `hash-${index}`),
		);
		for (let index = 0; index < 128; index++) {
			rows[index * 3_000] = row(`hash-${index}`, [
				ref("Local", `branch/${index}`),
			]);
		}
		const startedAt = performance.now();

		const sections = buildRefPanelSections({
			currentBranchName: "branch/127",
			refs,
			remotePrefixes: ["origin"],
			rows,
		});
		const elapsed = performance.now() - startedAt;
		console.info(`Sparse 500k-row ref panel: ${elapsed.toFixed(2)} ms`);

		expect(sections[0]?.items).toHaveLength(500);
		expect(sections[0]?.items[0]?.name).toBe("branch/127");
		expect(elapsed).toBeLessThan(18);
	});
});

function row(hash: string, refs: CommitGraphRow["commit"]["refs"]) {
	return {
		activeLanesAbove: [],
		activeLanesBelow: [],
		commit: commit(hash, refs),
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

function commit(hash: string, refs: CommitInfo["refs"]) {
	return {
		author: "Test Author",
		date: 0,
		email: "test@example.invalid",
		hash,
		message: "Test commit",
		refs,
		signatureKind: "None",
		stats: null,
	} satisfies CommitInfo;
}
