import { useVirtualizer } from "@tanstack/react-virtual";
import { useMemo, useRef } from "react";

const LINE_HEIGHT = 18;
const OVERSCAN = 12;

export function ConflictResultPreview({
	onActivate,
	value,
	wrapLines,
}: {
	onActivate: (position: { scrollTop: number; selectionStart: number }) => void;
	value: string;
	wrapLines: boolean;
}) {
	const lineStarts = useMemo(() => buildLineStarts(value), [value]);
	const contentWidth = useMemo(
		() => (wrapLines ? undefined : maximumLineWidth(value)),
		[value, wrapLines],
	);
	const viewportRef = useRef<HTMLButtonElement>(null);
	const virtualizer = useVirtualizer({
		count: lineStarts.length,
		estimateSize: () => LINE_HEIGHT,
		getScrollElement: () => viewportRef.current,
		initialRect: { width: 800, height: 400 },
		measureElement: (element) => element.getBoundingClientRect().height,
		overscan: OVERSCAN,
	});
	const virtualItems = virtualizer.getVirtualItems();
	const renderedItems =
		virtualItems.length > 0
			? virtualItems
			: Array.from({ length: Math.min(30, lineStarts.length) }, (_, index) => ({
					index,
					key: index,
					start: index * LINE_HEIGHT,
				}));

	return (
		<button
			aria-label="Editable result preview"
			className="custom-scrollbar relative min-h-0 flex-1 cursor-text overflow-auto bg-background p-0 text-left font-mono text-[12px] leading-[18px] text-foreground outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring"
			onClick={(event) => {
				const row =
					event.target instanceof Element
						? event.target.closest<HTMLElement>("[data-index]")
						: null;
				const index = Number(row?.dataset.index ?? 0);
				onActivate({
					scrollTop: event.currentTarget.scrollTop,
					selectionStart: lineStarts[index] ?? 0,
				});
			}}
			ref={viewportRef}
			title="Click to edit the complete output"
			type="button"
		>
			<span
				aria-hidden="true"
				className="sr-only"
				data-testid="conflict-result-line-numbers"
			>
				{renderedItems.map((item) => item.index + 1).join("\n")}
			</span>
			<div
				className="relative min-w-full"
				style={{
					height: virtualizer.getTotalSize(),
					width: contentWidth ? `${contentWidth}px` : undefined,
				}}
			>
				{renderedItems.map((item) => (
					<div
						className="absolute left-0 top-0 grid min-w-full grid-cols-[3.5rem_minmax(0,1fr)]"
						data-index={item.index}
						key={item.key}
						ref={virtualizer.measureElement}
						style={{ transform: `translateY(${item.start}px)` }}
					>
						<span
							aria-hidden="true"
							className="border-r bg-card/45 px-2 text-right tabular-nums text-muted-foreground"
							data-result-line-number=""
						>
							{item.index + 1}
						</span>
						<span
							className={
								wrapLines ? "whitespace-pre-wrap px-3" : "whitespace-pre px-3"
							}
						>
							{readLine(value, lineStarts, item.index)}
						</span>
					</div>
				))}
			</div>
		</button>
	);
}

export function buildLineStarts(text: string) {
	const starts = [0];
	for (let index = 0; index < text.length; index++) {
		if (text.charCodeAt(index) === 10) starts.push(index + 1);
	}
	return starts;
}

function readLine(text: string, starts: number[], index: number) {
	const start = starts[index] ?? text.length;
	const nextStart = starts[index + 1] ?? text.length + 1;
	const end = Math.max(start, nextStart - 1);
	return text.slice(start, text.charCodeAt(end - 1) === 13 ? end - 1 : end);
}

function maximumLineWidth(value: string) {
	let columns = 0;
	let maximum = 0;
	for (let index = 0; index < value.length; index++) {
		const character = value.charCodeAt(index);
		if (character === 10) {
			maximum = Math.max(maximum, columns);
			columns = 0;
		} else if (character === 9) columns += 4 - (columns % 4);
		else if (character !== 13) columns++;
	}
	return Math.max(800, Math.max(maximum, columns) * 7.25 + 80);
}
