import { useVirtualizer } from "@tanstack/react-virtual";
import { useEffect, useMemo, useRef, useState } from "react";
import type { CommitGraphRow as CommitGraphRowModel } from "@/generated/types";
import { CommitGraphHeader } from "./components/CommitGraphHeader";
import { CommitRow } from "./components/CommitRow";
import { RefsPanel } from "./components/RefsPanel";
import type { ColKey } from "./constants";
import {
	COL_DEFAULT,
	COL_MIN,
	COL_ORDER,
	GRAPH_PADDING_LEFT,
	LANE_GAP,
	OVERSCAN,
	ROW_HEIGHT,
} from "./constants";
import { useCommitGraphData } from "./hooks/useCommitGraphData";
import { resolveWidths } from "./utils/columns";

export function CommitGraphView({
	onSelectCommit,
	onRefsChanged,
	refreshToken = 0,
	repositoryId,
	selectedCommitHash,
}: {
	onSelectCommit: (row: CommitGraphRowModel) => void;
	onRefsChanged: () => void;
	refreshToken?: number;
	repositoryId: string | null;
	selectedCommitHash: string | null;
}) {
	const viewportRef = useRef<HTMLDivElement | null>(null);
	const scrollRef = useRef<HTMLDivElement | null>(null);
	const [containerWidth, setContainerWidth] = useState(0);
	const [graphScrollLeft, setGraphScrollLeft] = useState(0);
	const [preferredWidths, setPreferredWidths] =
		useState<Record<ColKey, number>>(COL_DEFAULT);
	const graphScrollerRef = useRef<HTMLDivElement | null>(null);

	const {
		currentBranchName,
		ensureRangeLoaded,
		error,
		isInitialLoading,
		laneCount,
		remotePrefixes,
		rows,
		totalRows,
	} = useCommitGraphData(refreshToken);

	useEffect(() => {
		const node = viewportRef.current;
		if (!node) {
			return;
		}

		const observer = new ResizeObserver((entries) => {
			const nextWidth = Math.floor(entries[0]?.contentRect.width ?? 0);
			setContainerWidth((current) =>
				current === nextWidth ? current : nextWidth,
			);
		});
		observer.observe(node);
		return () => {
			observer.disconnect();
		};
	}, []);

	const widths = useMemo(
		() => resolveWidths(containerWidth, preferredWidths),
		[containerWidth, preferredWidths],
	);
	const templateColumns = useMemo(
		() =>
			`${widths.branch}px ${widths.graph}px ${widths.message}px ${widths.hash}px ${widths.author}px`,
		[widths],
	);
	const graphContentWidth = useMemo(
		() =>
			Math.max(widths.graph, GRAPH_PADDING_LEFT + (laneCount + 2) * LANE_GAP),
		[laneCount, widths.graph],
	);

	useEffect(() => {
		if (graphScrollLeft <= Math.max(0, graphContentWidth - widths.graph)) {
			return;
		}
		setGraphScrollLeft(Math.max(0, graphContentWidth - widths.graph));
	}, [graphContentWidth, graphScrollLeft, widths.graph]);

	const rowCount = totalRows > 0 ? totalRows : 140;
	const virtualizer = useVirtualizer({
		count: rowCount,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: OVERSCAN,
	});
	const virtualItems = virtualizer.getVirtualItems();

	useEffect(() => {
		const first = virtualItems[0];
		const last = virtualItems[virtualItems.length - 1];

		if (!first || !last) {
			return;
		}
		ensureRangeLoaded(first.index, last.index);
	}, [ensureRangeLoaded, virtualItems]);

	const handleResizeStart = (
		leftKey: ColKey,
		event: React.PointerEvent<HTMLButtonElement>,
	) => {
		const leftIndex = COL_ORDER.indexOf(leftKey);
		if (leftIndex < 0 || leftIndex >= COL_ORDER.length - 1) {
			return;
		}

		event.preventDefault();
		const rightKey = COL_ORDER[leftIndex + 1];
		const startX = event.clientX;
		const startLeft = widths[leftKey];
		const startRight = widths[rightKey];

		const onMove = (moveEvent: PointerEvent) => {
			const dx = moveEvent.clientX - startX;
			const minLeft = COL_MIN[leftKey];
			const minRight = COL_MIN[rightKey];
			const pair = startLeft + startRight;
			const nextLeft = Math.min(
				Math.max(startLeft + dx, minLeft),
				pair - minRight,
			);
			const nextRight = pair - nextLeft;

			setPreferredWidths((current) => ({
				...current,
				[leftKey]: nextLeft,
				[rightKey]: nextRight,
			}));
		};

		const onUp = () => {
			window.removeEventListener("pointermove", onMove);
			window.removeEventListener("pointerup", onUp);
		};

		window.addEventListener("pointermove", onMove);
		window.addEventListener("pointerup", onUp, { once: true });
	};

	return (
		<section className="h-full w-full overflow-hidden bg-background">
			<div className="flex h-full w-full min-w-0">
				<RefsPanel
					currentBranchName={currentBranchName}
					onSelectCommit={onSelectCommit}
					remotePrefixes={remotePrefixes}
					rows={rows}
				/>
				<div ref={viewportRef} className="flex h-full min-w-0 flex-1 flex-col">
					<CommitGraphHeader
						isInitialLoading={isInitialLoading}
						onResizeStart={handleResizeStart}
						templateColumns={templateColumns}
					/>

					{error ? (
						<div className="h-7 border-b border-destructive/40 bg-destructive/10 px-[10px] leading-[27px] text-destructive">
							{error}
						</div>
					) : null}

					<div
						ref={scrollRef}
						className="custom-scrollbar relative min-h-0 w-full flex-1 overflow-x-hidden overflow-y-auto bg-[repeating-linear-gradient(to_bottom,var(--background)_0,var(--background)_21px,var(--card)_21px,var(--card)_22px)]"
					>
						<div
							className="relative h-full w-full"
							style={{ height: `${virtualizer.getTotalSize()}px` }}
						>
							{virtualItems.map((item) => (
								<div
									className="absolute left-0 top-0 w-full"
									key={item.key}
									style={{
										height: `${ROW_HEIGHT}px`,
										transform: `translateY(${Math.round(item.start)}px)`,
									}}
								>
									<CommitRow
										graph={{
											contentWidth: graphContentWidth,
											scrollLeft: graphScrollLeft,
										}}
										isSelected={
											Boolean(rows[item.index]) &&
											rows[item.index]?.commit.hash === selectedCommitHash
										}
										onSelect={onSelectCommit}
										onRefsChanged={onRefsChanged}
										currentBranchName={currentBranchName}
										remotePrefixes={remotePrefixes}
										repositoryId={repositoryId}
										row={rows[item.index] ?? null}
										rowIndex={item.index}
										templateColumns={templateColumns}
									/>
								</div>
							))}
						</div>
					</div>
					<div
						className="grid h-3 border-t bg-background"
						style={{ gridTemplateColumns: templateColumns }}
					>
						<div />
						<div
							className="custom-scrollbar overflow-x-auto overflow-y-hidden"
							ref={graphScrollerRef}
							onScroll={(event) =>
								setGraphScrollLeft(event.currentTarget.scrollLeft)
							}
						>
							<div style={{ height: 1, width: graphContentWidth }} />
						</div>
						<div />
						<div />
						<div />
					</div>
				</div>
			</div>
		</section>
	);
}
