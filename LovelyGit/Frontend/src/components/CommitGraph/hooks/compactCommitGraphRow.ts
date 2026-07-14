import type { CommitGraphRow, CommitLaneColor } from "@/generated/types";

const EMPTY_ARRAY = Object.freeze([]) as unknown as never[];

export function compactCommitGraphRow(row: CommitGraphRow) {
	row.lane ??= 0;
	row.colorIndex ??= 0;
	row.commit.signatureKind ||= "None";
	row.isBranchTip ||= false;
	row.isMergeCommit ||= false;
	row.activeLanesAbove = reuseEmpty(row.activeLanesAbove);
	row.activeLanesBelow = row.activeLanesBelow
		? reuseEmpty(row.activeLanesBelow)
		: row.activeLanesAbove;
	row.laneColorsAbove = reuseEmpty(row.laneColorsAbove);
	row.laneColorsBelow = row.laneColorsBelow
		? reuseEmpty(row.laneColorsBelow)
		: row.laneColorsAbove;
	hydrateLaneColors(row.laneColorsAbove);
	if (row.laneColorsBelow !== row.laneColorsAbove) {
		hydrateLaneColors(row.laneColorsBelow);
	}
	if (sameNumbers(row.activeLanesAbove, row.activeLanesBelow)) {
		row.activeLanesBelow = row.activeLanesAbove;
	}
	if (sameLaneColors(row.laneColorsAbove, row.laneColorsBelow)) {
		row.laneColorsBelow = row.laneColorsAbove;
	}
	row.edgesAbove = reuseEmpty(row.edgesAbove);
	row.edgesBelow = reuseEmpty(row.edgesBelow);
	row.commit.refs = reuseEmpty(row.commit.refs);
	return row;
}

function reuseEmpty<T>(values: T[] | undefined) {
	return !values || values.length === 0 ? (EMPTY_ARRAY as T[]) : values;
}

function hydrateLaneColors(colors: CommitLaneColor[]) {
	for (const color of colors) {
		color.lane ??= 0;
		color.colorIndex ??= 0;
	}
}

function sameNumbers(left: number[], right: number[]) {
	return (
		sameLength(left, right) &&
		left.every((value, index) => value === right[index])
	);
}

function sameLaneColors(left: CommitLaneColor[], right: CommitLaneColor[]) {
	return (
		sameLength(left, right) &&
		left.every((value, index) => {
			const other = right[index];
			return (
				value.lane === other?.lane && value.colorIndex === other.colorIndex
			);
		})
	);
}

function sameLength(left: unknown[], right: unknown[]) {
	return left.length === right.length;
}
