import { motion, useReducedMotion } from "motion/react";
import { useRef } from "react";
import type { ConflictResolutionResponse } from "@/generated/types";
import { ConflictSourcePane } from "./ConflictSourcePane";
import type { ConflictSide } from "./conflictDiffItems";
import type { ConflictChoice } from "./conflictDocument";

export function ConflictSourcePanes({
	activeConflict,
	choices,
	conflict,
	contextLines,
	disabled,
	onChoice,
	lineDisplayMode,
	wrapLines,
}: {
	activeConflict: number;
	choices: Record<number, ConflictChoice>;
	conflict: ConflictResolutionResponse;
	contextLines: number;
	disabled: boolean;
	onChoice: (id: number, choice: ConflictChoice) => void;
	lineDisplayMode: "Changes" | "FullFile";
	wrapLines: boolean;
}) {
	const currentRef = useRef<HTMLDivElement>(null);
	const incomingRef = useRef<HTMLDivElement>(null);
	const syncing = useRef(false);
	const reduceMotion = useReducedMotion();
	const synchronize = (side: ConflictSide, source: HTMLDivElement) => {
		if (syncing.current) return;
		const target = side === "ours" ? incomingRef.current : currentRef.current;
		if (!target) return;
		const sourceRange = source.scrollHeight - source.clientHeight;
		const targetRange = target.scrollHeight - target.clientHeight;
		const progress = sourceRange <= 0 ? 0 : source.scrollTop / sourceRange;
		syncing.current = true;
		target.scrollTop = progress * Math.max(0, targetRange);
		requestAnimationFrame(() => {
			syncing.current = false;
		});
	};

	return (
		<motion.div
			animate={{ opacity: 1, y: 0 }}
			className="flex h-full min-h-0 divide-x overflow-hidden bg-background"
			initial={{ opacity: 0, y: reduceMotion ? 0 : -10 }}
			transition={{
				duration: reduceMotion ? 0 : 0.2,
				ease: [0.22, 1, 0.36, 1],
			}}
		>
			<ConflictSourcePane
				activeConflict={activeConflict}
				baseText={conflict.base.text ?? ""}
				choices={choices}
				contextLines={contextLines}
				diff={conflict.currentComparison}
				disabled={disabled}
				hunks={conflict.hunks}
				metadata={conflict.currentSource}
				lineDisplayMode={lineDisplayMode}
				onChoice={onChoice}
				onScroll={synchronize}
				side="ours"
				sourceText={conflict.ours.text ?? ""}
				viewportRef={currentRef}
				wrapLines={wrapLines}
			/>
			<ConflictSourcePane
				activeConflict={activeConflict}
				baseText={conflict.base.text ?? ""}
				choices={choices}
				contextLines={contextLines}
				diff={conflict.incomingComparison}
				disabled={disabled}
				hunks={conflict.hunks}
				metadata={conflict.incomingSource}
				lineDisplayMode={lineDisplayMode}
				onChoice={onChoice}
				onScroll={synchronize}
				side="theirs"
				sourceText={conflict.theirs.text ?? ""}
				viewportRef={incomingRef}
				wrapLines={wrapLines}
			/>
		</motion.div>
	);
}
