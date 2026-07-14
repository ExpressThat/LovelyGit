import { CodeCell, LineNumber, lineBackground } from "./DiffLineRendering";
import { DiffPaneHeader } from "./SideBySideRow";

const LOADING_ROWS = Array.from({ length: 12 }, (_, index) => ({
	id: `virtual-text-loading-${index}`,
	width: index % 4 === 0 ? 72 : 96,
}));

export function VirtualTextHeaders({ viewMode }: { viewMode: string }) {
	return viewMode === "SideBySide" ? (
		<div className="grid shrink-0 grid-cols-2 border-b bg-card text-[10px] font-semibold uppercase text-muted-foreground">
			<DiffPaneHeader headerLabel="Before" lineNumberLabel="Old" />
			<DiffPaneHeader headerLabel="After" lineNumberLabel="New" />
		</div>
	) : (
		<div className="grid shrink-0 grid-cols-[4rem_4rem_2rem_minmax(0,1fr)] border-b bg-card text-[10px] font-semibold uppercase text-muted-foreground">
			<div className="border-r px-2 py-1 text-right">Old</div>
			<div className="border-r px-2 py-1 text-right">New</div>
			<div className="border-r px-2 py-1 text-center"> </div>
			<div className="px-2 py-1">Code</div>
		</div>
	);
}

export function VirtualTextLoading() {
	return (
		<div className="space-y-2 p-4">
			{LOADING_ROWS.map((row) => (
				<div
					className="h-[18px] rounded bg-muted/50"
					key={row.id}
					style={{ width: `${row.width}%` }}
				/>
			))}
		</div>
	);
}

export function VirtualTextRow({
	changeType,
	index,
	line,
	measureElement,
	scrollLeft,
	viewMode,
	wrapLines,
	y,
}: {
	changeType: string;
	index: number;
	line: string;
	measureElement: (element: HTMLDivElement | null) => void;
	scrollLeft: number;
	viewMode: string;
	wrapLines: boolean;
	y: number;
}) {
	const lineNumber = index + 1;
	const isInserted = changeType === "Inserted";
	const oldNumber = isInserted ? null : lineNumber;
	const newNumber = isInserted ? lineNumber : null;
	const variant = isInserted ? "inserted" : "deleted";

	if (viewMode === "SideBySide") {
		return (
			<div
				className={`absolute left-0 top-0 grid w-full grid-cols-2 ${lineBackground(changeType)}`}
				data-index={index}
				ref={measureElement}
				style={{ transform: `translateY(${y}px)` }}
			>
				<VirtualSide
					number={oldNumber}
					showText={!isInserted}
					{...{ line, scrollLeft, variant, wrapLines }}
				/>
				<VirtualSide
					number={newNumber}
					showText={isInserted}
					{...{ line, scrollLeft, variant, wrapLines }}
				/>
			</div>
		);
	}

	return (
		<div
			className={`absolute left-0 top-0 grid w-full grid-cols-[4rem_4rem_2rem_minmax(0,1fr)] ${lineBackground(changeType)}`}
			data-index={index}
			ref={measureElement}
			style={{ transform: `translateY(${y}px)` }}
		>
			<LineNumber value={oldNumber} />
			<LineNumber value={newNumber} />
			<div className="border-r px-2 text-center text-muted-foreground">
				{isInserted ? "+" : "-"}
			</div>
			<CodeCell
				changeSpans={null}
				scrollLeft={scrollLeft}
				spans={null}
				text={line}
				variant={variant}
				wrapLines={wrapLines}
			/>
		</div>
	);
}

function VirtualSide({
	line,
	number,
	scrollLeft,
	showText,
	variant,
	wrapLines,
}: {
	line: string;
	number: number | null;
	scrollLeft: number;
	showText: boolean;
	variant: "deleted" | "inserted";
	wrapLines: boolean;
}) {
	return (
		<div className="grid min-w-0 grid-cols-[4rem_minmax(0,1fr)] border-r last:border-r-0">
			<LineNumber value={number} />
			<CodeCell
				changeSpans={null}
				scrollLeft={scrollLeft}
				spans={null}
				text={showText ? line : ""}
				variant={showText ? variant : "plain"}
				wrapLines={wrapLines}
			/>
		</div>
	);
}
