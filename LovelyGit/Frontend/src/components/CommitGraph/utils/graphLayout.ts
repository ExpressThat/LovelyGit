import {
	GRAPH_BOTTOM_Y,
	GRAPH_PADDING_LEFT,
	GRAPH_TOP_Y,
	LANE_COLORS,
	LANE_GAP,
	ROW_CENTER_Y,
	ROW_HEIGHT,
} from "../constants";
import type { CommitGraphRow, CommitLaneEdge } from "../types/graph";

export type GraphEdgeDirection = "above" | "below";

export function laneColor(lane: number) {
	return LANE_COLORS[lane % LANE_COLORS.length];
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

export function lanesCoveredByEdges(row: CommitGraphRow) {
	const coveredAbove = new Set<number>();
	const coveredBelow = new Set<number>();

	for (const edge of row.edgesAbove) {
		coveredAbove.add(edge.fromLane);
		coveredAbove.add(edge.toLane);
	}

	for (const edge of row.edgesBelow) {
		coveredBelow.add(edge.fromLane);
		coveredBelow.add(edge.toLane);
	}

	return { coveredAbove, coveredBelow };
}

export function graphRowLayout(row: CommitGraphRow) {
	const { coveredAbove, coveredBelow } = lanesCoveredByEdges(row);
	const activeAbove = new Set(row.activeLanesAbove);
	const activeBelow = new Set(row.activeLanesBelow);
	const curvedAbove = row.edgesAbove.filter(isCurvedEdge);
	const curvedBelow = row.edgesBelow.filter(isCurvedEdge);

	return {
		activeAbove,
		activeBelow,
		coveredAbove,
		coveredBelow,
		curvedAbove,
		curvedBelow,
		dotColor: laneColor(row.lane),
		dotX: xForLane(row.lane),
		maskEdges: [
			...curvedAbove.map((edge) => ({ edge, direction: "above" as const })),
			...curvedBelow.map((edge) => ({ edge, direction: "below" as const })),
		],
		visibleLanes: Array.from(
			new Set([...row.activeLanesAbove, ...row.activeLanesBelow]),
		),
	};
}

function isCurvedEdge(edge: CommitLaneEdge) {
	return edge.fromLane !== edge.toLane;
}
