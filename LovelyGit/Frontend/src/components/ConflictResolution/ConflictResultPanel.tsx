import { motion, useReducedMotion } from "motion/react";
import { useEffect, useMemo, useRef, useState } from "react";
import { cn } from "@/lib/utils";

const LINE_HEIGHT = 18;
const GUTTER_OVERSCAN = 4;

export function ConflictResultPanel({
	isManualResult,
	isResolved,
	onEdit,
	value,
	wrapLines,
}: {
	isManualResult: boolean;
	isResolved: boolean;
	onEdit: (value: string) => void;
	value: string;
	wrapLines: boolean;
}) {
	const reduceMotion = useReducedMotion();
	const [scrollTop, setScrollTop] = useState(0);
	const [viewportHeight, setViewportHeight] = useState(400);
	const textareaRef = useRef<HTMLTextAreaElement>(null);
	const lineCount = useMemo(() => countLines(value), [value]);
	const lineNumbers = useMemo(
		() => visibleLineNumbers(lineCount, scrollTop, viewportHeight),
		[lineCount, scrollTop, viewportHeight],
	);
	useEffect(() => {
		const textarea = textareaRef.current;
		if (!textarea) return;
		const updateHeight = () => {
			if (textarea.clientHeight > 0) setViewportHeight(textarea.clientHeight);
		};
		updateHeight();
		const observer = new ResizeObserver(updateHeight);
		observer.observe(textarea);
		return () => observer.disconnect();
	}, []);
	return (
		<motion.section
			animate={{ opacity: 1, y: 0 }}
			className="flex h-full min-h-0 flex-col bg-background"
			initial={{ opacity: 0, y: reduceMotion ? 0 : 12 }}
			transition={{
				duration: reduceMotion ? 0 : 0.2,
				ease: [0.22, 1, 0.36, 1],
			}}
		>
			<div className="flex h-7 shrink-0 items-center gap-2 border-b bg-muted/25 px-3 text-[10px] text-muted-foreground">
				<span className="font-semibold uppercase tracking-wide text-foreground">
					Editable result
				</span>
				<span>
					{isManualResult
						? "Manual editing is active. Reset to use source controls again."
						: "Base content remains until you deliberately resolve each conflict."}
				</span>
				<span
					className={cn(
						"ml-auto rounded-full px-2 py-0.5",
						isResolved
							? "bg-emerald-500/10 text-emerald-600 dark:text-emerald-300"
							: "bg-amber-500/10 text-amber-600 dark:text-amber-300",
					)}
				>
					{isResolved ? "Ready to save" : "Resolution required"}
				</span>
			</div>
			<div className="relative flex min-h-0 flex-1 overflow-hidden font-mono text-[12px] leading-[18px]">
				<div
					aria-hidden="true"
					className="w-14 shrink-0 overflow-hidden border-r bg-card/45 py-2 text-right text-muted-foreground"
				>
					<div
						className="whitespace-pre px-2 tabular-nums"
						data-testid="conflict-result-line-numbers"
						style={{ transform: `translateY(${lineNumbers.offset}px)` }}
					>
						{lineNumbers.text}
					</div>
				</div>
				<textarea
					aria-invalid={!isResolved}
					aria-label="Editable result preview"
					className={cn(
						"custom-scrollbar min-h-0 min-w-0 flex-1 resize-none bg-background px-3 py-2 font-mono text-[12px] leading-[18px] outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring",
						wrapLines ? "whitespace-pre-wrap" : "whitespace-pre",
					)}
					onChange={(event) => onEdit(event.target.value)}
					onScroll={(event) => setScrollTop(event.currentTarget.scrollTop)}
					ref={textareaRef}
					spellCheck={false}
					value={value}
					wrap={wrapLines ? "soft" : "off"}
				/>
			</div>
		</motion.section>
	);
}

function countLines(value: string) {
	let lineCount = 1;
	for (let index = 0; index < value.length; index++) {
		if (value.charCodeAt(index) === 10) lineCount++;
	}
	return lineCount;
}

function visibleLineNumbers(
	lineCount: number,
	scrollTop: number,
	viewportHeight: number,
) {
	const first = Math.max(
		0,
		Math.floor(scrollTop / LINE_HEIGHT) - GUTTER_OVERSCAN,
	);
	const last = Math.min(
		lineCount,
		Math.ceil((scrollTop + viewportHeight) / LINE_HEIGHT) + GUTTER_OVERSCAN,
	);
	return {
		offset: first * LINE_HEIGHT - scrollTop,
		text: Array.from(
			{ length: last - first },
			(_, index) => first + index + 1,
		).join("\n"),
	};
}
