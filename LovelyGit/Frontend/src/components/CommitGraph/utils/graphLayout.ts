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

export function laneColor(lane: number) {
	return LANE_COLORS[lane % LANE_COLORS.length];
}

export function xForLane(lane: number) {
	return GRAPH_PADDING_LEFT + lane * LANE_GAP;
}

export function edgePath(edge: CommitLaneEdge, direction: "above" | "below") {
	const fromX = xForLane(edge.from_lane);
	const toX = xForLane(edge.to_lane);
	const midX = fromX + (toX - fromX) * 0.55;

	if (direction === "above") {
		if (edge.from_lane === edge.to_lane) {
			return `M ${fromX} ${GRAPH_TOP_Y} L ${toX} ${ROW_CENTER_Y}`;
		}
		return `M ${fromX} 0 Q ${midX} ${ROW_CENTER_Y} ${toX} ${ROW_CENTER_Y}`;
	}

	if (edge.from_lane === edge.to_lane) {
		return `M ${fromX} ${ROW_CENTER_Y} L ${toX} ${GRAPH_BOTTOM_Y}`;
	}

	return `M ${fromX} ${ROW_CENTER_Y} Q ${midX} ${ROW_CENTER_Y} ${toX} ${ROW_HEIGHT / 2}`;
}

export function lanesCoveredByEdges(row: CommitGraphRow) {
	const coveredAbove = new Set<number>();
	const coveredBelow = new Set<number>();

	for (const edge of row.edges_above) {
		coveredAbove.add(edge.from_lane);
		coveredAbove.add(edge.to_lane);
	}

	for (const edge of row.edges_below) {
		coveredBelow.add(edge.from_lane);
		coveredBelow.add(edge.to_lane);
	}

	return { coveredAbove, coveredBelow };
}
