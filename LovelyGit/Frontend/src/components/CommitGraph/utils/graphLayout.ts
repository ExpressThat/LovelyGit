import type {
	CommitGraphRow,
	CommitLaneColor,
	CommitLaneEdge,
} from "@/generated/types";
import {
	GRAPH_BOTTOM_Y,
	GRAPH_PADDING_LEFT,
	GRAPH_TOP_Y,
	LANE_COLORS,
	LANE_GAP,
	ROW_CENTER_Y,
	ROW_HEIGHT,
} from "../constants";

export type GraphEdgeDirection = "above" | "below";

export function graphColor(colorIndex: number) {
	return LANE_COLORS[Math.abs(colorIndex) % LANE_COLORS.length];
}

export function xForLane(lane: number) {
	return GRAPH_PADDING_LEFT + lane * LANE_GAP;
}

export function edgePath(edge: CommitLaneEdge, direction: GraphEdgeDirection) {
	const fromX = xForLane(edge.fromLane);
	const toX = xForLane(edge.toLane);
	const midX = fromX + (toX - fromX) * 0.55;

	if (direction === "above") {
		if (edge.fromLane === edge.toLane) {
			return `M ${fromX} ${GRAPH_TOP_Y} L ${toX} ${ROW_CENTER_Y}`;
		}
		return `M ${fromX} 0 Q ${midX} ${ROW_CENTER_Y} ${toX} ${ROW_CENTER_Y}`;
	}

	if (edge.fromLane === edge.toLane) {
		return `M ${fromX} ${ROW_CENTER_Y} L ${toX} ${GRAPH_BOTTOM_Y}`;
	}

	return `M ${fromX} ${ROW_CENTER_Y} Q ${midX} ${ROW_CENTER_Y} ${toX} ${ROW_HEIGHT / 2}`;
}

export function graphRowLayout(row: CommitGraphRow) {
	return {
		activeAbove: row.activeLanesAbove,
		activeBelow: row.activeLanesBelow,
		dotColor: graphColor(row.colorIndex),
		dotX: xForLane(row.lane),
		isStash: row.commit.refs.some((reference) => reference.kind === "Stash"),
		laneColorsAbove: row.laneColorsAbove,
		laneColorsBelow: row.laneColorsBelow,
		maskEdges: buildMaskEdges(row),
		visibleLanes: mergeVisibleLanes(row.activeLanesAbove, row.activeLanesBelow),
	};
}

function isCurvedEdge(edge: CommitLaneEdge) {
	return edge.fromLane !== edge.toLane;
}

export function laneColorIndex(colors: CommitLaneColor[], lane: number) {
	for (const color of colors) if (color.lane === lane) return color.colorIndex;
}

export function laneIsCovered(edges: CommitLaneEdge[], lane: number) {
	return edges.some((edge) => edge.fromLane === lane || edge.toLane === lane);
}

const EMPTY_MASK_EDGES = Object.freeze([]) as unknown as Array<{
	edge: CommitLaneEdge;
	direction: GraphEdgeDirection;
}>;

function buildMaskEdges(row: CommitGraphRow) {
	let result: typeof EMPTY_MASK_EDGES | null = null;
	for (const edge of row.edgesAbove) {
		if (isCurvedEdge(edge)) {
			result ??= [];
			result.push({ edge, direction: "above" });
		}
	}
	for (const edge of row.edgesBelow) {
		if (isCurvedEdge(edge)) {
			result ??= [];
			result.push({ edge, direction: "below" });
		}
	}
	return result ?? EMPTY_MASK_EDGES;
}

function mergeVisibleLanes(above: number[], below: number[]) {
	if (above.length === 0) return below;
	if (below.length === 0 || sameValues(above, below)) return above;
	const result = above.slice();
	for (const lane of below) if (!result.includes(lane)) result.push(lane);
	return result;
}

function sameValues(left: number[], right: number[]) {
	return (
		left.length === right.length &&
		left.every((value, index) => value === right[index])
	);
}
