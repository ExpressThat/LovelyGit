import { describe, expect, it } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { compactCommitGraphRow } from "./compactCommitGraphRow";

describe("compactCommitGraphRow", () => {
	it("shares frozen empty collections without replacing populated data", () => {
		const first = row();
		const second = row();
		delete (first.commit as Partial<typeof first.commit>).signatureKind;
		delete (first as Partial<typeof first>).isBranchTip;
		delete (first as Partial<typeof first>).isMergeCommit;
		compactCommitGraphRow(first);
		compactCommitGraphRow(second);

		expect(first.commit.refs).toBe(second.commit.refs);
		expect(first.commit.signatureKind).toBe("None");
		expect(first.isBranchTip).toBe(false);
		expect(first.isMergeCommit).toBe(false);
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

	it("hydrates collections omitted by the compact wire format", () => {
		const compact = row();
		compact.activeLanesAbove = [0, 2];
		compact.laneColorsAbove = [{} as (typeof compact.laneColorsAbove)[number]];
		delete (compact as Partial<typeof compact>).lane;
		delete (compact as Partial<typeof compact>).colorIndex;
		delete (compact as Partial<typeof compact>).activeLanesBelow;
		delete (compact as Partial<typeof compact>).laneColorsBelow;
		delete (compact as Partial<typeof compact>).edgesAbove;
		delete (compact as Partial<typeof compact>).edgesBelow;
		delete (compact.commit as Partial<typeof compact.commit>).refs;

		compactCommitGraphRow(compact);

		expect(compact.activeLanesBelow).toBe(compact.activeLanesAbove);
		expect(compact.laneColorsBelow).toBe(compact.laneColorsAbove);
		expect(compact.laneColorsAbove).toEqual([{ colorIndex: 0, lane: 0 }]);
		expect(compact.lane).toBe(0);
		expect(compact.colorIndex).toBe(0);
		expect(compact.edgesAbove).toEqual([]);
		expect(compact.edgesBelow).toEqual([]);
		expect(compact.commit.refs).toEqual([]);
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
