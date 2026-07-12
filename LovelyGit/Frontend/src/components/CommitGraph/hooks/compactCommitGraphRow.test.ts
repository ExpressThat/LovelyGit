import { describe, expect, it } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { compactCommitGraphRow } from "./compactCommitGraphRow";

describe("compactCommitGraphRow", () => {
	it("shares frozen empty collections without replacing populated data", () => {
		const first = row();
		const second = row();
		const populatedParents = ["parent"];
		first.commit.parents = populatedParents;

		compactCommitGraphRow(first);
		compactCommitGraphRow(second);

		expect(first.commit.branches).toBe(second.commit.branches);
		expect(first.edgesAbove).toBe(second.edgesAbove);
		expect(Object.isFrozen(first.commit.branches)).toBe(true);
		expect(first.commit.parents).toBe(populatedParents);
	});
});

function row(): CommitGraphRow {
	return {
		activeLanesAbove: [],
		activeLanesBelow: [],
		colorIndex: 0,
		commit: {
			author: "Author",
			branches: [],
			date: 0,
			email: "",
			hash: "a".repeat(40),
			message: "Message",
			parents: [],
			refs: [],
			signatureKind: "None",
			stats: null,
			tags: [],
		},
		edgesAbove: [],
		edgesBelow: [],
		isBranchTip: false,
		isMergeCommit: false,
		lane: 0,
		laneColorsAbove: [],
		laneColorsBelow: [],
		rowIndex: 0,
	};
}
