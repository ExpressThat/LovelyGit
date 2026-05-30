import { Fragment } from "react";
import type {
	CommitGraphRow,
	CommitLaneEdge,
} from "@/generated/ExpressThat.LovelyGit.Services.Git.CommitGraph.Models";
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
	graphRowLayout,
	laneColor,
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
						{renderEdgePath(edge, "above", edge.fromLane, "above")}
					</Fragment>
				))}

				{row.edgesBelow.map((edge) => (
					<BelowEdge edge={edge} key={`${keyPrefix}-below-${edgeKey(edge)}`} />
				))}

				{renderCommitDot(layout.dotX, layout.dotColor)}
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
	contentWidth,
	drawAbove,
	drawBelow,
	lane,
}: {
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
			{drawAbove ? renderLaneLine(lane, x, GRAPH_TOP_Y, ROW_CENTER_Y) : null}
			{drawBelow ? renderLaneLine(lane, x, ROW_CENTER_Y, GRAPH_BOTTOM_Y) : null}
		</g>
	);
}

function BelowEdge({ edge }: { edge: CommitLaneEdge }) {
	const x = xForLane(edge.toLane);

	return (
		<Fragment>
			{renderEdgePath(edge, "below", edge.toLane, "below")}
			{renderLaneLine(edge.toLane, x, ROW_CENTER_Y, GRAPH_BOTTOM_Y)}
		</Fragment>
	);
}

function renderEdgePath(
	edge: CommitLaneEdge,
	direction: GraphEdgeDirection,
	colorLane: number,
	className: string,
) {
	return (
		<path
			className={className}
			d={edgePath(edge, direction)}
			fill="none"
			stroke={laneColor(colorLane)}
			strokeLinecap="butt"
			strokeWidth={GRAPH_STROKE_WIDTH}
		/>
	);
}

function renderLaneLine(lane: number, x: number, y1: number, y2: number) {
	return (
		<line
			stroke={laneColor(lane)}
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

function renderCommitDot(x: number, color: string) {
	return (
		<>
			<circle
				cx={x}
				cy={ROW_CENTER_Y}
				fill="var(--card)"
				r="5.4"
				stroke={color}
				strokeWidth="2.6"
			/>
			<circle cx={x} cy={ROW_CENTER_Y} fill={color} r="2.2" />
		</>
	);
}

function edgeKey(edge: CommitLaneEdge) {
	return `${edge.fromLane}-${edge.toLane}`;
}

function rowKey(row: CommitGraphRow) {
	return `${row.rowIndex}-${row.commit.hash}`;
}
