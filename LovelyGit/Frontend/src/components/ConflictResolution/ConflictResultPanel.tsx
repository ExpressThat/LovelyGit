import { motion, useReducedMotion } from "motion/react";
import { useMemo, useState } from "react";
import { cn } from "@/lib/utils";

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
	const lineNumbers = useMemo(() => buildLineNumbers(value), [value]);
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
						style={{ transform: `translateY(-${scrollTop}px)` }}
					>
						{lineNumbers}
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
					spellCheck={false}
					value={value}
					wrap={wrapLines ? "soft" : "off"}
				/>
			</div>
		</motion.section>
	);
}

function buildLineNumbers(value: string) {
	let lineCount = 1;
	for (let index = 0; index < value.length; index++) {
		if (value.charCodeAt(index) === 10) lineCount++;
	}
	return Array.from({ length: lineCount }, (_, index) => index + 1).join("\n");
}
