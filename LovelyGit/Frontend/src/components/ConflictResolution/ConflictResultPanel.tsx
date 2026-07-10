import { CheckCheck, RotateCcw } from "lucide-react";
import { motion, useReducedMotion } from "motion/react";
import { Button } from "@/components/ui/button";
import { ConflictChoiceCard } from "./ConflictChoiceCard";
import type {
	ConflictChoice,
	ConflictDocumentSegment,
	ConflictSegment,
} from "./conflictDocument";

export function ConflictResultPanel({
	choices,
	isBusy,
	isResolved,
	onChoice,
	onEdit,
	onReset,
	onResolve,
	segments,
	value,
}: {
	choices: Record<number, ConflictChoice>;
	isBusy: boolean;
	isResolved: boolean;
	onChoice: (id: number, choice: ConflictChoice) => void;
	onEdit: (value: string) => void;
	onReset: () => void;
	onResolve: () => void;
	segments: ConflictDocumentSegment[];
	value: string;
}) {
	const reduceMotion = useReducedMotion();
	const conflicts = segments.filter(
		(segment): segment is ConflictSegment => segment.kind === "conflict",
	);
	return (
		<motion.section
			animate={{ opacity: 1, y: 0 }}
			className="flex min-h-0 flex-[0.85] flex-col border-t bg-popover"
			initial={{ opacity: 0, y: reduceMotion ? 0 : 22 }}
			transition={{
				duration: reduceMotion ? 0 : 0.24,
				ease: [0.22, 1, 0.36, 1],
			}}
		>
			<header className="flex h-10 shrink-0 items-center gap-2 border-b px-3">
				<div className="mr-auto">
					<div className="text-xs font-semibold">Resolution result</div>
					<div className="text-[10px] text-muted-foreground">
						Pick either side line-by-line, then refine the result.
					</div>
				</div>
				<Button onClick={onReset} size="sm" variant="ghost">
					<RotateCcw /> Reset
				</Button>
				<Button disabled={!isResolved || isBusy} onClick={onResolve} size="sm">
					<CheckCheck /> {isBusy ? "Resolving…" : "Mark resolved"}
				</Button>
			</header>
			<div className="grid min-h-0 flex-1 grid-cols-1 lg:grid-cols-[1.15fr_0.85fr]">
				<div className="min-h-0 space-y-2 overflow-auto border-r p-2">
					{conflicts.map((segment, index) => (
						<ConflictChoiceCard
							choice={choices[segment.id]}
							index={index}
							key={segment.id}
							onChange={(choice) => onChoice(segment.id, choice)}
							segment={segment}
						/>
					))}
				</div>
				<div className="flex min-h-0 flex-col bg-background">
					<label
						className="border-b px-3 py-1.5 text-[10px] font-semibold uppercase tracking-wide text-muted-foreground"
						htmlFor="conflict-result"
					>
						Editable result preview
					</label>
					<textarea
						aria-invalid={!isResolved}
						className="min-h-0 flex-1 resize-none bg-background p-3 font-mono text-xs leading-5 outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring"
						id="conflict-result"
						onChange={(event) => onEdit(event.target.value)}
						spellCheck={false}
						value={value}
					/>
					{!isResolved ? (
						<div className="border-t px-3 py-1.5 text-xs text-amber-600 dark:text-amber-400">
							Resolve every highlighted conflict before saving.
						</div>
					) : null}
				</div>
			</div>
		</motion.section>
	);
}
