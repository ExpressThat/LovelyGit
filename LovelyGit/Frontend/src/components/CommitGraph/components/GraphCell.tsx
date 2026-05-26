import { Fragment } from "react";
import {
	GRAPH_BOTTOM_Y,
	GRAPH_CURVE_MASK_WIDTH,
	GRAPH_STROKE_WIDTH,
	GRAPH_TOP_Y,
	LANE_GAP,
	ROW_CENTER_Y,
	ROW_HEIGHT,
} from "../constants";
import type { CommitGraphRow } from "../types/graph";
import {
	edgePath,
	laneColor,
	lanesCoveredByEdges,
	xForLane,
} from "../utils/graphLayout";

export function GraphCell({
	graphContentWidth,
	graphScrollLeft,
	row,
}: {
	graphContentWidth: number;
	graphScrollLeft: number;
	row: CommitGraphRow | null;
}) {
	if (!row) {
		return null;
	}

	const dotX = xForLane(row.lane);
	const dotColor = laneColor(row.lane);
	const { coveredAbove, coveredBelow } = lanesCoveredByEdges(row);
	const activeLanesAbove = new Set(row.active_lanes_above);
	const activeLanesBelow = new Set(row.active_lanes_below);
	const visibleLanes = Array.from(
		new Set([...row.active_lanes_above, ...row.active_lanes_below]),
	);
	const curvedEdgesAbove = row.edges_above.filter(
		(edge) => edge.from_lane !== edge.to_lane,
	);
	const curvedEdgesBelow = row.edges_below.filter(
		(edge) => edge.from_lane !== edge.to_lane,
	);
	const hasCurveMask =
		curvedEdgesAbove.length > 0 || curvedEdgesBelow.length > 0;
	const curveMaskId = `graph-curve-mask-${row.row_index}`;

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
					<defs>
						<mask
							id={curveMaskId}
							maskUnits="userSpaceOnUse"
							x={0}
							y={0}
							width={graphContentWidth}
							height={ROW_HEIGHT}
						>
							<rect
								fill="white"
								height={ROW_HEIGHT}
								width={graphContentWidth}
								x={0}
								y={0}
							/>
							{[...curvedEdgesAbove, ...curvedEdgesBelow].map((edge, index) => (
								<path
									d={edgePath(
										edge,
										index < curvedEdgesAbove.length ? "above" : "below",
									)}
									fill="none"
									key={`${row.row_index}-mask-${edge.from_lane}-${edge.to_lane}-${row.commit.hash}`}
									stroke="black"
									strokeLinecap="butt"
									strokeWidth={GRAPH_CURVE_MASK_WIDTH}
								/>
							))}
						</mask>
					</defs>
				) : null}

				<g mask={hasCurveMask ? `url(#${curveMaskId})` : undefined}>
					{visibleLanes.map((lane) => {
						const drawAbove =
							activeLanesAbove.has(lane) && !coveredAbove.has(lane);
						const drawBelow =
							activeLanesBelow.has(lane) && !coveredBelow.has(lane);

						if (!drawAbove && !drawBelow) {
							return null;
						}

						const x = xForLane(lane);
						if (x > graphContentWidth + LANE_GAP) {
							return null;
						}

						return (
							<g key={`${row.row_index}-active-${lane}`}>
								{drawAbove ? (
									<line
										stroke={laneColor(lane)}
										strokeLinecap="butt"
										strokeOpacity="0.82"
										strokeWidth={GRAPH_STROKE_WIDTH}
										x1={x}
										x2={x}
										y1={GRAPH_TOP_Y}
										y2={ROW_CENTER_Y}
									/>
								) : null}
								{drawBelow ? (
									<line
										stroke={laneColor(lane)}
										strokeLinecap="butt"
										strokeOpacity="0.82"
										strokeWidth={GRAPH_STROKE_WIDTH}
										x1={x}
										x2={x}
										y1={ROW_CENTER_Y}
										y2={GRAPH_BOTTOM_Y}
									/>
								) : null}
							</g>
						);
					})}
				</g>

				{row.edges_above.map((edge, _) => (
					<path
						d={edgePath(edge, "above")}
						fill="none"
						key={`${row.row_index}-above-${edge.from_lane}-${edge.to_lane}-${row.commit.hash}`}
						stroke={laneColor(edge.from_lane)}
						strokeLinecap="butt"
						strokeWidth={GRAPH_STROKE_WIDTH}
						className="above"
					/>
				))}

				{row.edges_below.map((edge, _) => {
					const x = xForLane(edge.to_lane);
					return (
						<Fragment
							key={`${row.row_index}-below-${edge.from_lane}-${edge.to_lane}-${row.commit.hash}`}
						>
							<path
								d={edgePath(edge, "below")}
								fill="none"
								stroke={laneColor(edge.to_lane)}
								strokeLinecap="butt"
								strokeWidth={GRAPH_STROKE_WIDTH}
								className="below"
							/>
							<line
								stroke={laneColor(edge.to_lane)}
								strokeLinecap="butt"
								strokeOpacity="0.82"
								strokeWidth={GRAPH_STROKE_WIDTH}
								x1={x}
								x2={x}
								y1={ROW_CENTER_Y}
								y2={GRAPH_BOTTOM_Y}
							/>
						</Fragment>
					);
				})}

				<circle
					cx={dotX}
					cy={ROW_CENTER_Y}
					fill="var(--card)"
					r="5.4"
					stroke={dotColor}
					strokeWidth="2.6"
				/>
				<circle cx={dotX} cy={ROW_CENTER_Y} fill={dotColor} r="2.2" />
			</svg>
		</div>
	);
}
