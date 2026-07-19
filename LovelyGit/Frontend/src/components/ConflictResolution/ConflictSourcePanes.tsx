import { useRef } from "react";
import type { ConflictResolutionResponse } from "@/generated/types";
import { motion, useReducedMotion } from "@/lib/motion";
import { ConflictSourcePane } from "./ConflictSourcePane";
import type { ConflictSide } from "./conflictDiffItems";
import type { ConflictChoice } from "./conflictDocument";
import type {
	ConflictScrollAnchor,
	ConflictScrollApi,
} from "./conflictScrollSyncApi";

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
	const currentApi = useRef<ConflictScrollApi | null>(null);
	const incomingApi = useRef<ConflictScrollApi | null>(null);
	const reduceMotion = useReducedMotion();
	const register = (side: ConflictSide, api: ConflictScrollApi | null) => {
		if (side === "ours") currentApi.current = api;
		else incomingApi.current = api;
	};
	const synchronize = (side: ConflictSide, anchor: ConflictScrollAnchor) => {
		if (syncing.current) return;
		const target = side === "ours" ? incomingApi.current : currentApi.current;
		if (!target) return;
		syncing.current = true;
		target.scrollTo(anchor);
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
				onRegister={register}
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
				onRegister={register}
				onScroll={synchronize}
				side="theirs"
				sourceText={conflict.theirs.text ?? ""}
				viewportRef={incomingRef}
				wrapLines={wrapLines}
			/>
		</motion.div>
	);
}
