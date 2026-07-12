import { describe, expect, it } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { compactCommitGraphRow } from "./compactCommitGraphRow";

describe("compactCommitGraphRow", () => {
	it("shares frozen empty collections without replacing populated data", () => {
		const first = row();
		const second = row();
		compactCommitGraphRow(first);
		compactCommitGraphRow(second);

		expect(first.commit.refs).toBe(second.commit.refs);
		expect(first.edgesAbove).toBe(second.edgesAbove);
		expect(Object.isFrozen(first.commit.refs)).toBe(true);
	});

	it("shares equal lane snapshots but preserves transitions", () => {
		const stable = row();
		stable.activeLanesAbove = [0, 2];
		stable.activeLanesBelow = [0, 2];
		stable.laneColorsAbove = [{ colorIndex: 3, lane: 0 }];
		stable.laneColorsBelow = [{ colorIndex: 3, lane: 0 }];
		const transition = row();
		transition.activeLanesAbove = [0];
		transition.activeLanesBelow = [0, 1];

		compactCommitGraphRow(stable);
		compactCommitGraphRow(transition);

		expect(stable.activeLanesBelow).toBe(stable.activeLanesAbove);
		expect(stable.laneColorsBelow).toBe(stable.laneColorsAbove);
		expect(transition.activeLanesBelow).not.toBe(transition.activeLanesAbove);
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
			email: "",
			hash: "a".repeat(40),
			message: "Message",
			refs: [],
			signatureKind: "None",
			stats: null,
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
