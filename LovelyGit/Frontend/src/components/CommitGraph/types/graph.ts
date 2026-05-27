import type { CommitInfo } from "./commit";

export type CommitLaneEdge = {
	from_lane: number;
	to_lane: number;
	kind: "straight" | "merge_in" | "fork_out";
};

export type CommitGraphRow = {
	commit: CommitInfo;
	row_index: number;
	lane: number;
	active_lanes: number[];
	active_lanes_above: number[];
	active_lanes_below: number[];
	edges_above: CommitLaneEdge[];
	edges_below: CommitLaneEdge[];
	is_merge_commit: boolean;
	is_branch_tip: boolean;
};

export type CommitGraphResponse = {
	total_rows: number;
	lane_count: number;
	rows: CommitGraphRow[];
	next_cursor: string | null;
	has_more: boolean;
};
