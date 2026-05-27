import type { CommitInfo } from "./commit";

export type CommitLaneEdge = {
	fromLane: number;
	toLane: number;
	kind: "straight" | "merge_in" | "fork_out";
};

export type CommitGraphRow = {
	commit: CommitInfo;
	rowIndex: number;
	lane: number;
	activeLanesAbove: number[];
	activeLanesBelow: number[];
	edgesAbove: CommitLaneEdge[];
	edgesBelow: CommitLaneEdge[];
	isMergeCommit: boolean;
	isBranchTip: boolean;
};

export type CommitGraphResponse = {
	totalRows: number;
	laneCount: number;
	rows: CommitGraphRow[];
	nextCursor: string | null;
	hasMore: boolean;
};
