import { describe, expect, it } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { messagePrefix } from "./format";

describe("messagePrefix", () => {
	it("uses the typed local ref for merge labels", () => {
		const value = row();
		value.commit.refs = [
			{ kind: "Remote", name: "origin/topic", remoteUrl: null },
			{ kind: "Local", name: "topic", remoteUrl: null },
		];

		expect(messagePrefix(value)).toBe("Merge branch 'topic' into seen");
	});

	it("uses the generic merge label without a local ref", () => {
		const value = row();
		value.commit.refs = [
			{ kind: "Remote", name: "origin/topic", remoteUrl: null },
		];

		expect(messagePrefix(value)).toBe("Merge branch into seen");
	});
});

function row(): CommitGraphRow {
	return {
		activeLanesAbove: [],
		activeLanesBelow: [],
		colorIndex: 0,
		commit: {
			author: "Author",
			date: 0,
			email: "author@example.invalid",
			hash: "a".repeat(40),
			message: "Merge subject",
			refs: [],
			signatureKind: "None",
			stats: null,
		},
		edgesAbove: [],
		edgesBelow: [],
		isBranchTip: true,
		isMergeCommit: true,
		lane: 0,
		laneColorsAbove: [],
		laneColorsBelow: [],
		rowIndex: 0,
	};
}
