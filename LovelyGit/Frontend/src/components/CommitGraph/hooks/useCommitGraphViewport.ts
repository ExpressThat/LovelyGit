import { useVirtualizer } from "@tanstack/react-virtual";
import { useEffect, useMemo, useRef, useState } from "react";
import type { ColKey } from "../constants";
import {
	COL_DEFAULT,
	COL_MIN,
	COL_ORDER,
	GRAPH_PADDING_LEFT,
	LANE_GAP,
	OVERSCAN,
	ROW_HEIGHT,
} from "../constants";
import { resolveWidths } from "../utils/columns";

export function useCommitGraphViewport({
	ensureRangeLoaded,
	laneCount,
	totalRows,
}: {
	ensureRangeLoaded: (startIndex: number, endIndex: number) => void;
	laneCount: number;
	totalRows: number;
}) {
	const viewportRef = useRef<HTMLDivElement | null>(null);
	const scrollRef = useRef<HTMLDivElement | null>(null);
	const graphScrollerRef = useRef<HTMLDivElement | null>(null);
	const [containerWidth, setContainerWidth] = useState(0);
	const [graphScrollLeft, setGraphScrollLeft] = useState(0);
	const [preferredWidths, setPreferredWidths] =
		useState<Record<ColKey, number>>(COL_DEFAULT);

	useEffect(() => {
		const node = viewportRef.current;
		if (!node) return;

		const observer = new ResizeObserver((entries) => {
			const nextWidth = Math.floor(entries[0]?.contentRect.width ?? 0);
			setContainerWidth((current) =>
				current === nextWidth ? current : nextWidth,
			);
		});
		observer.observe(node);
		return () => observer.disconnect();
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
		const maximum = Math.max(0, graphContentWidth - widths.graph);
		if (graphScrollLeft > maximum) setGraphScrollLeft(maximum);
	}, [graphContentWidth, graphScrollLeft, widths.graph]);

	const virtualizer = useVirtualizer({
		count: totalRows > 0 ? totalRows : 140,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: OVERSCAN,
	});
	const virtualItems = virtualizer.getVirtualItems();
	const contentHeight = commitGraphContentHeight(totalRows);

	useEffect(() => {
		const first = virtualItems[0];
		const last = virtualItems[virtualItems.length - 1];
		if (first && last) ensureRangeLoaded(first.index, last.index);
	}, [ensureRangeLoaded, virtualItems]);

	const handleResizeStart = (
		leftKey: ColKey,
		event: React.PointerEvent<HTMLButtonElement>,
	) => {
		const leftIndex = COL_ORDER.indexOf(leftKey);
		if (leftIndex < 0 || leftIndex >= COL_ORDER.length - 1) return;

		event.preventDefault();
		const rightKey = COL_ORDER[leftIndex + 1];
		const startX = event.clientX;
		const startLeft = widths[leftKey];
		const startRight = widths[rightKey];
		const onMove = (moveEvent: PointerEvent) => {
			const pair = startLeft + startRight;
			const nextLeft = Math.min(
				Math.max(startLeft + moveEvent.clientX - startX, COL_MIN[leftKey]),
				pair - COL_MIN[rightKey],
			);
			setPreferredWidths((current) => ({
				...current,
				[leftKey]: nextLeft,
				[rightKey]: pair - nextLeft,
			}));
		};
		const onUp = () => {
			window.removeEventListener("pointermove", onMove);
			window.removeEventListener("pointerup", onUp);
		};
		window.addEventListener("pointermove", onMove);
		window.addEventListener("pointerup", onUp, { once: true });
	};

	return {
		contentHeight,
		graphContentWidth,
		graphScrollerRef,
		graphScrollLeft,
		handleResizeStart,
		scrollRef,
		setGraphScrollLeft,
		templateColumns,
		virtualItems,
		viewportRef,
	};
}

export function commitGraphContentHeight(totalRows: number) {
	return (totalRows > 0 ? totalRows : 140) * ROW_HEIGHT;
}
