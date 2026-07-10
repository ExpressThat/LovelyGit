import type { RefObject } from "react";

export function CommitGraphHorizontalScrollbar({
	contentWidth,
	onScrollLeftChange,
	scrollerRef,
	templateColumns,
}: {
	contentWidth: number;
	onScrollLeftChange: (scrollLeft: number) => void;
	scrollerRef: RefObject<HTMLDivElement | null>;
	templateColumns: string;
}) {
	return (
		<div
			className="grid h-3 border-t bg-background"
			style={{ gridTemplateColumns: templateColumns }}
		>
			<div />
			<div
				className="custom-scrollbar overflow-x-auto overflow-y-hidden"
				ref={scrollerRef}
				onScroll={(event) => onScrollLeftChange(event.currentTarget.scrollLeft)}
			>
				<div style={{ height: 1, width: contentWidth }} />
			</div>
			<div />
			<div />
			<div />
		</div>
	);
}
