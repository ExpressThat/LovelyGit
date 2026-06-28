import { Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { GitConflictTextLine } from "@/generated/types";
import {
	type ConflictChoice,
	type ConflictHunk,
	findTextSequenceRange,
} from "./ConflictHunks";
import { ConflictRenderedLine } from "./ConflictRenderedLine";

type Side = Exclude<ConflictChoice, null>;

export function ConflictChoicePane({
	activeHunkIndex,
	choices,
	hunks,
	lines,
	onChoose,
	side,
	title,
}: {
	activeHunkIndex: number;
	choices: ConflictChoice[];
	hunks: ConflictHunk[];
	lines: GitConflictTextLine[];
	onChoose: (index: number, choice: Side) => void;
	side: Side;
	title: string;
}) {
	const ranges = hunks.map((hunk) =>
		findTextSequenceRange(
			lines,
			side === "current" ? hunk.current : hunk.incoming,
		),
	);

	return (
		<section className="flex min-h-0 flex-1 flex-col overflow-hidden border-r bg-background">
			<header
				className={`border-b px-3 py-2 font-medium text-sm ${toneClass(side)}`}
			>
				{title}
			</header>
			<div className="min-h-0 flex-1 overflow-auto font-mono text-[12px] leading-5">
				{lines.map((line, lineIndex) => {
					const hunkIndex = ranges.findIndex(
						(range) =>
							range && lineIndex >= range.start && lineIndex <= range.end,
					);
					const isStart = ranges[hunkIndex]?.start === lineIndex;
					const isChosen = hunkIndex >= 0 && choices[hunkIndex] === side;
					const isActive = hunkIndex === activeHunkIndex;
					return (
						<div
							className={`grid grid-cols-[64px_minmax(0,1fr)] ${hunkClass(isActive, isChosen, side)}`}
							key={line.lineNumber}
						>
							<div className="select-none border-r bg-card/45 px-2 text-right text-muted-foreground">
								{line.lineNumber}
							</div>
							<pre className="relative min-w-max bg-transparent px-2 whitespace-pre">
								{isStart ? (
									<Button
										className="absolute top-0 right-2 h-5 gap-1 px-2 text-[11px]"
										onClick={() => onChoose(hunkIndex, side)}
										size="xs"
										type="button"
										variant={isChosen ? "default" : "outline"}
									>
										{isChosen ? (
											<Check className="size-3" aria-hidden="true" />
										) : null}
										Use hunk
									</Button>
								) : null}
								<ConflictRenderedLine line={line} />
							</pre>
						</div>
					);
				})}
			</div>
		</section>
	);
}

function hunkClass(isActive: boolean, isChosen: boolean, side: Side) {
	if (isChosen) return side === "current" ? "bg-sky-500/12" : "bg-amber-500/12";
	if (isActive) return "bg-muted/55";
	return "";
}

function toneClass(side: Side) {
	return side === "current"
		? "bg-sky-500/10 text-sky-700 dark:text-sky-200"
		: "bg-amber-500/10 text-amber-700 dark:text-amber-200";
}
