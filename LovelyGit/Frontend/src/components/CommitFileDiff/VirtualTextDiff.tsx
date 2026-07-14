import { useVirtualizer } from "@tanstack/react-virtual";
import { useEffect, useMemo, useRef, useState } from "react";
import type { CommitFileDiffResponse } from "@/generated/types";
import { DiffHorizontalScroller } from "./DiffHorizontalScroller";
import { estimateCodeWidth } from "./DiffLineViewport";
import {
	VirtualTextHeaders,
	VirtualTextLoading,
	VirtualTextRow,
} from "./VirtualTextRows";
import { loadVirtualText } from "./virtualTextPayload";

const OVERSCAN = 12;

export function VirtualTextDiff({
	diff,
	wrapLines,
}: {
	diff: CommitFileDiffResponse;
	wrapLines: boolean;
}) {
	const [text, setText] = useState(diff.virtualText ?? "");
	const [error, setError] = useState<string | null>(null);
	const changeType = diff.virtualChangeType || "Inserted";
	useEffect(() => {
		let active = true;
		setText(diff.virtualText ?? "");
		setError(null);
		if (diff.virtualText) {
			return;
		}

		loadVirtualText(diff)
			.then((loadedText) => {
				if (active) {
					setText(loadedText);
				}
			})
			.catch((loadError: unknown) => {
				if (active) {
					setError(
						loadError instanceof Error
							? loadError.message
							: "Failed to load virtual diff text.",
					);
				}
			});

		return () => {
			active = false;
		};
	}, [diff]);
	const lineStarts = useMemo(() => buildLineStarts(text), [text]);
	const lineCount = diff.virtualLineCount || lineStarts.length;
	const contentWidth = useMemo(
		() => estimateCodeWidth(iterVisibleWidthLines(text, lineStarts)),
		[text, lineStarts],
	);
	const viewportRef = useRef<HTMLDivElement | null>(null);
	const [scrollLeft, setScrollLeft] = useState(0);
	const virtualizer = useVirtualizer({
		count: lineCount,
		estimateSize: () => 18,
		getScrollElement: () => viewportRef.current,
		measureElement: (element) => element.getBoundingClientRect().height,
		overscan: OVERSCAN,
	});
	const virtualItems = virtualizer.getVirtualItems();

	if (error) {
		return (
			<div className="m-4 rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
				{error}
			</div>
		);
	}

	if (!text) {
		return <VirtualTextLoading />;
	}

	return (
		<div className="flex h-full min-w-0 flex-col font-mono text-[12px] leading-[18px] text-foreground">
			<VirtualTextHeaders viewMode={diff.viewMode} />
			<div
				className="custom-scrollbar relative min-h-0 flex-1 overflow-x-hidden overflow-y-auto"
				ref={viewportRef}
			>
				<div
					className="relative w-full"
					style={{ height: `${virtualizer.getTotalSize()}px` }}
				>
					{virtualItems.map((item) => (
						<VirtualTextRow
							changeType={changeType}
							index={item.index}
							key={item.key}
							line={readLine(text, lineStarts, item.index)}
							measureElement={virtualizer.measureElement}
							scrollLeft={scrollLeft}
							viewMode={diff.viewMode}
							wrapLines={wrapLines}
							y={item.start}
						/>
					))}
				</div>
			</div>
			{wrapLines ? null : (
				<DiffHorizontalScroller
					contentWidth={contentWidth}
					label="Horizontal text diff scroll"
					onValueChange={setScrollLeft}
					value={scrollLeft}
				/>
			)}
		</div>
	);
}

function buildLineStarts(text: string) {
	const starts = [0];
	for (let index = 0; index < text.length; index++) {
		if (text.charCodeAt(index) === 10) {
			starts.push(index + 1);
		}
	}

	return starts;
}

function readLine(text: string, starts: number[], index: number) {
	const start = starts[index] ?? text.length;
	const nextStart = starts[index + 1] ?? text.length + 1;
	const end = Math.max(start, nextStart - 1);
	return text.slice(start, text.charCodeAt(end - 1) === 13 ? end - 1 : end);
}

function* iterVisibleWidthLines(text: string, starts: number[]) {
	const sampleCount = Math.min(starts.length, 1_000);
	for (let index = 0; index < sampleCount; index++) {
		yield readLine(text, starts, index);
	}
}
