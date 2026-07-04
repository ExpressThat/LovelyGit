import { Fragment } from "react";
import type { CommitGraphRow, CommitLaneEdge } from "@/generated/types";
import {
	GRAPH_BOTTOM_Y,
	GRAPH_CURVE_MASK_WIDTH,
	GRAPH_STROKE_WIDTH,
	GRAPH_TOP_Y,
	LANE_GAP,
	ROW_CENTER_Y,
	ROW_HEIGHT,
} from "../constants";
import {
	edgePath,
	type GraphEdgeDirection,
	graphColor,
	graphRowLayout,
	xForLane,
} from "../utils/graphLayout";

export function GraphCell({
	graphContentWidth,
	graphScrollLeft,
	row,
}: {
	graphContentWidth: number;
	graphScrollLeft: number;
	row: CommitGraphRow;
}) {
	const layout = graphRowLayout(row);
	const curveMaskId = `graph-curve-mask-${row.rowIndex}`;
	const hasCurveMask = layout.maskEdges.length > 0;
	const keyPrefix = rowKey(row);

	return (
		<div className="h-full w-full overflow-hidden">
			<svg
				aria-hidden="true"
				className="block overflow-visible"
				height={ROW_HEIGHT}
				viewBox={`0 0 ${graphContentWidth} ${ROW_HEIGHT}`}
				style={{ transform: `translateX(${-graphScrollLeft}px)` }}
				width={graphContentWidth}
			>
				{hasCurveMask ? (
					<CurveMask
						contentWidth={graphContentWidth}
						id={curveMaskId}
						maskEdges={layout.maskEdges}
						rowKey={keyPrefix}
					/>
				) : null}

				<g mask={hasCurveMask ? `url(#${curveMaskId})` : undefined}>
					{layout.visibleLanes.map((lane) => (
						<ActiveLane
							aboveColorIndex={layout.laneColorsAbove.get(lane)}
							belowColorIndex={layout.laneColorsBelow.get(lane)}
							contentWidth={graphContentWidth}
							drawAbove={
								layout.activeAbove.has(lane) && !layout.coveredAbove.has(lane)
							}
							drawBelow={
								layout.activeBelow.has(lane) && !layout.coveredBelow.has(lane)
							}
							key={`${row.rowIndex}-active-${lane}`}
							lane={lane}
						/>
					))}
				</g>

				{row.edgesAbove.map((edge) => (
					<Fragment key={`${keyPrefix}-above-${edgeKey(edge)}`}>
						{renderEdgePath(edge, "above", "above")}
					</Fragment>
				))}

				{row.edgesBelow.map((edge) => (
					<BelowEdge edge={edge} key={`${keyPrefix}-below-${edgeKey(edge)}`} />
				))}

				{renderCommitDot(layout.dotX, layout.dotColor, layout.isStash)}
			</svg>
		</div>
	);
}

function CurveMask({
	contentWidth,
	id,
	maskEdges,
	rowKey,
}: {
	contentWidth: number;
	id: string;
	maskEdges: Array<{ edge: CommitLaneEdge; direction: GraphEdgeDirection }>;
	rowKey: string;
}) {
	return (
		<defs>
			<mask
				height={ROW_HEIGHT}
				id={id}
				maskUnits="userSpaceOnUse"
				width={contentWidth}
				x={0}
				y={0}
			>
				<rect
					fill="white"
					height={ROW_HEIGHT}
					width={contentWidth}
					x={0}
					y={0}
				/>
				{maskEdges.map(({ edge, direction }) => (
					<path
						d={edgePath(edge, direction)}
						fill="none"
						key={`${rowKey}-mask-${direction}-${edgeKey(edge)}`}
						stroke="black"
						strokeLinecap="butt"
						strokeWidth={GRAPH_CURVE_MASK_WIDTH}
					/>
				))}
			</mask>
		</defs>
	);
}

function ActiveLane({
	aboveColorIndex,
	belowColorIndex,
	contentWidth,
	drawAbove,
	drawBelow,
	lane,
}: {
	aboveColorIndex: number | undefined;
	belowColorIndex: number | undefined;
	contentWidth: number;
	drawAbove: boolean;
	drawBelow: boolean;
	lane: number;
}) {
	const x = xForLane(lane);

	if ((!drawAbove && !drawBelow) || x > contentWidth + LANE_GAP) {
		return null;
	}

	return (
		<g>
			{drawAbove && aboveColorIndex != null
				? renderLaneLine(
						graphColor(aboveColorIndex),
						x,
						GRAPH_TOP_Y,
						ROW_CENTER_Y,
					)
				: null}
			{drawBelow && belowColorIndex != null
				? renderLaneLine(
						graphColor(belowColorIndex),
						x,
						ROW_CENTER_Y,
						GRAPH_BOTTOM_Y,
					)
				: null}
		</g>
	);
}

function BelowEdge({ edge }: { edge: CommitLaneEdge }) {
	const x = xForLane(edge.toLane);

	return (
		<Fragment>
			{renderEdgePath(edge, "below", "below")}
			{renderLaneLine(
				graphColor(edge.colorIndex),
				x,
				ROW_CENTER_Y,
				GRAPH_BOTTOM_Y,
			)}
		</Fragment>
	);
}

function renderEdgePath(
	edge: CommitLaneEdge,
	direction: GraphEdgeDirection,
	className: string,
) {
	return (
		<path
			className={className}
			d={edgePath(edge, direction)}
			fill="none"
			stroke={graphColor(edge.colorIndex)}
			strokeLinecap="butt"
			strokeWidth={GRAPH_STROKE_WIDTH}
		/>
	);
}

function renderLaneLine(color: string, x: number, y1: number, y2: number) {
	return (
		<line
			stroke={color}
			strokeLinecap="butt"
			strokeOpacity="0.82"
			strokeWidth={GRAPH_STROKE_WIDTH}
			x1={x}
			x2={x}
			y1={y1}
			y2={y2}
		/>
	);
}

function renderCommitDot(x: number, color: string, isStash: boolean) {
	return (
		<>
			<circle
				cx={x}
				cy={ROW_CENTER_Y}
				fill="var(--card)"
				r="5.4"
				stroke={color}
				strokeDasharray={isStash ? "2 2" : undefined}
				strokeWidth="2.6"
			/>
			{isStash ? null : (
				<circle cx={x} cy={ROW_CENTER_Y} fill={color} r="2.2" />
			)}
		</>
	);
}

function edgeKey(edge: CommitLaneEdge) {
	return `${edge.kind}-${edge.fromLane}-${edge.toLane}-${edge.colorIndex}`;
}

function rowKey(row: CommitGraphRow) {
	return `${row.rowIndex}-${row.commit.hash}`;
}
