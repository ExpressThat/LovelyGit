import { useVirtualizer } from "@tanstack/react-virtual";
import type { ReactNode, RefObject, UIEventHandler } from "react";
import type { GitConflictTextLine } from "@/generated/types";
import { estimateCodeWidth } from "../CommitFileDiff/DiffLineRendering";
import { ConflictRenderedLine } from "./ConflictRenderedLine";

const CONFLICT_LINE_HEIGHT = 20;
const CONFLICT_OVERSCAN = 16;

export function ConflictCodeScroller({
	ariaLabel,
	lineClassName,
	lines,
	onScroll,
	renderAction,
	scrollContainerRef,
}: {
	ariaLabel: string;
	lineClassName?: (line: GitConflictTextLine, index: number) => string;
	lines: GitConflictTextLine[];
	onScroll?: UIEventHandler<HTMLElement>;
	renderAction?: (line: GitConflictTextLine, index: number) => ReactNode;
	scrollContainerRef: RefObject<HTMLElement | null>;
}) {
	const contentWidth = estimateCodeWidth(lines.map((line) => line.text));
	const virtualizer = useVirtualizer({
		count: lines.length,
		estimateSize: () => CONFLICT_LINE_HEIGHT,
		getScrollElement: () => scrollContainerRef.current,
		overscan: CONFLICT_OVERSCAN,
	});
	const virtualItems = virtualizer.getVirtualItems();

	return (
		<section
			aria-label={ariaLabel}
			className="custom-scrollbar min-h-0 flex-1 overflow-auto font-mono text-[12px] leading-5"
			onScroll={onScroll}
			ref={scrollContainerRef}
		>
			<div
				className="relative"
				style={{
					height: `${virtualizer.getTotalSize()}px`,
					width: contentWidth,
				}}
			>
				{virtualItems.map((item) => {
					const line = lines[item.index];
					return (
						<div
							className={`absolute top-0 left-0 grid grid-cols-[64px_minmax(0,1fr)] ${lineClassName?.(line, item.index) ?? ""}`}
							key={item.key}
							style={{
								height: `${item.size}px`,
								transform: `translateY(${item.start}px)`,
								width: contentWidth,
							}}
						>
							<div className="sticky left-0 z-10 select-none border-r bg-card px-2 text-right text-muted-foreground">
								{line.lineNumber}
							</div>
							<pre className="relative min-w-max bg-transparent px-2 whitespace-pre">
								{renderAction?.(line, item.index)}
								<ConflictRenderedLine line={line} />
							</pre>
						</div>
					);
				})}
			</div>
		</section>
	);
}
