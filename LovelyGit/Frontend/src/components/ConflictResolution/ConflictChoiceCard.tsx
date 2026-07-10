import { Check, Circle } from "lucide-react";
import { motion, useReducedMotion } from "motion/react";
import { Button } from "@/components/ui/button";
import type { ConflictChoice, ConflictSegment } from "./conflictDocument";

export function ConflictChoiceCard({
	choice,
	index,
	onChange,
	segment,
}: {
	choice: ConflictChoice;
	index: number;
	onChange: (choice: ConflictChoice) => void;
	segment: ConflictSegment;
}) {
	const reduceMotion = useReducedMotion();
	const choose = (ours: boolean, theirs: boolean) =>
		onChange({
			mode: "custom",
			ours: segment.ours.map(() => ours),
			theirs: segment.theirs.map(() => theirs),
		});

	return (
		<motion.article
			animate={{ opacity: 1, y: 0 }}
			className="overflow-hidden rounded-lg border bg-card"
			initial={{ opacity: 0, y: reduceMotion ? 0 : 6 }}
			transition={{ delay: reduceMotion ? 0 : Math.min(index * 0.035, 0.18) }}
		>
			<header className="flex flex-wrap items-center gap-1.5 border-b bg-muted/35 px-2.5 py-2">
				<span className="mr-auto text-xs font-semibold">
					Conflict {index + 1}
				</span>
				<Button onClick={() => choose(true, false)} size="xs" variant="outline">
					Use current
				</Button>
				<Button onClick={() => choose(false, true)} size="xs" variant="outline">
					Use incoming
				</Button>
				<Button onClick={() => choose(true, true)} size="xs" variant="outline">
					Use both
				</Button>
				<Button onClick={() => choose(false, false)} size="xs" variant="ghost">
					Remove
				</Button>
			</header>
			<div className="grid min-w-0 grid-cols-2 divide-x">
				<LineChoices
					label="Current branch (ours)"
					lines={segment.ours}
					onToggle={(lineIndex) =>
						onChange({
							...choice,
							mode: "custom",
							ours: toggle(choice.ours, lineIndex),
						})
					}
					selected={choice.ours}
				/>
				<LineChoices
					label="Incoming branch (theirs)"
					lines={segment.theirs}
					onToggle={(lineIndex) =>
						onChange({
							...choice,
							mode: "custom",
							theirs: toggle(choice.theirs, lineIndex),
						})
					}
					selected={choice.theirs}
				/>
			</div>
		</motion.article>
	);
}

function LineChoices({
	label,
	lines,
	onToggle,
	selected,
}: {
	label: string;
	lines: string[];
	onToggle: (index: number) => void;
	selected: boolean[];
}) {
	return (
		<div className="min-w-0">
			<div className="border-b px-2.5 py-1.5 text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
				{label}
			</div>
			{lines.length === 0 ? (
				<div className="px-2.5 py-3 text-xs italic text-muted-foreground">
					File absent
				</div>
			) : (
				lines.map((line, lineIndex) => (
					<button
						aria-pressed={selected[lineIndex]}
						className="flex w-full cursor-pointer items-start gap-2 border-b px-2 py-1.5 text-left font-mono text-xs last:border-0 hover:bg-accent aria-pressed:bg-primary/10"
						key={lineKey(lines, lineIndex)}
						onClick={() => onToggle(lineIndex)}
						type="button"
					>
						{selected[lineIndex] ? (
							<Check className="mt-0.5 size-3 shrink-0 text-primary" />
						) : (
							<Circle className="mt-0.5 size-3 shrink-0 text-muted-foreground" />
						)}
						<span className="min-w-0 whitespace-pre-wrap break-all">
							{line.replace(/\r?\n$/, "") || " "}
						</span>
					</button>
				))
			)}
		</div>
	);
}

function toggle(values: boolean[], index: number) {
	return values.map((value, valueIndex) =>
		valueIndex === index ? !value : value,
	);
}

function lineKey(lines: string[], index: number) {
	const line = lines[index];
	const occurrence = lines
		.slice(0, index)
		.filter((candidate) => candidate === line).length;
	return `${line}:${occurrence}`;
}
