import type { CommitGraphRow } from "@/generated/types";

const EMPTY_ARRAY = Object.freeze([]) as unknown as never[];

export function compactCommitGraphRow(row: CommitGraphRow) {
	row.activeLanesAbove = reuseEmpty(row.activeLanesAbove);
	row.activeLanesBelow = reuseEmpty(row.activeLanesBelow);
	row.laneColorsAbove = reuseEmpty(row.laneColorsAbove);
	row.laneColorsBelow = reuseEmpty(row.laneColorsBelow);
	row.edgesAbove = reuseEmpty(row.edgesAbove);
	row.edgesBelow = reuseEmpty(row.edgesBelow);
	row.commit.parents = reuseEmpty(row.commit.parents);
	row.commit.branches = reuseEmpty(row.commit.branches);
	row.commit.tags = reuseEmpty(row.commit.tags);
	row.commit.refs = reuseEmpty(row.commit.refs);
	return row;
}

function reuseEmpty<T>(values: T[]) {
	return values.length === 0 ? (EMPTY_ARRAY as T[]) : values;
}
