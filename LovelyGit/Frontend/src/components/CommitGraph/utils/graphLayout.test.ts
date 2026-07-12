import { describe, expect, it } from "vitest";
import type { CommitGraphRow, CommitLaneEdge } from "@/generated/types";
import { graphRowLayout, laneColorIndex, laneIsCovered } from "./graphLayout";

describe("graphRowLayout", () => {
	it("reuses row collections for the common straight-lane case", () => {
		const first = row();
		const second = row();
		const firstLayout = graphRowLayout(first);
		const secondLayout = graphRowLayout(second);

		expect(firstLayout.visibleLanes).toBe(first.activeLanesAbove);
		expect(firstLayout.maskEdges).toBe(secondLayout.maskEdges);
		expect(Object.isFrozen(firstLayout.maskEdges)).toBe(true);
	});

	it("builds masks only for curved edges and scans lane metadata", () => {
		const value = row();
		const curved = edge(0, 2);
		value.edgesAbove = [edge(0, 0), curved];
		value.laneColorsAbove = [{ colorIndex: 7, lane: 2 }];

		const layout = graphRowLayout(value);

		expect(layout.maskEdges).toEqual([{ direction: "above", edge: curved }]);
		expect(laneIsCovered(value.edgesAbove, 2)).toBe(true);
		expect(laneIsCovered(value.edgesAbove, 4)).toBe(false);
		expect(laneColorIndex(value.laneColorsAbove, 2)).toBe(7);
	});
});

function row(): CommitGraphRow {
	return {
		activeLanesAbove: [0],
		activeLanesBelow: [0],
		colorIndex: 0,
		commit: {
			author: "A",
			date: 0,
			email: "",
			hash: "a",
			message: "M",
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

function edge(fromLane: number, toLane: number): CommitLaneEdge {
	return { colorIndex: 0, fromLane, kind: "Line", toLane };
}
