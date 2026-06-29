import { Check } from "lucide-react";
import type { RefObject, UIEventHandler } from "react";
import { Button } from "@/components/ui/button";
import type { GitConflictTextLine } from "@/generated/types";
import { ConflictCodeScroller } from "./ConflictCodeScroller";
import {
	type ConflictChoice,
	type ConflictHunk,
	findTextSequenceRange,
} from "./ConflictHunks";

type Side = Exclude<ConflictChoice, null>;

export function ConflictChoicePane({
	activeHunkIndex,
	choices,
	hunks,
	lines,
	onChoose,
	onScroll,
	scrollContainerRef,
	side,
	title,
}: {
	activeHunkIndex: number;
	choices: ConflictChoice[];
	hunks: ConflictHunk[];
	lines: GitConflictTextLine[];
	onChoose: (index: number, choice: Side) => void;
	onScroll?: UIEventHandler<HTMLElement>;
	scrollContainerRef: RefObject<HTMLElement | null>;
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
			<ConflictCodeScroller
				ariaLabel={`${side === "current" ? "Current" : "Incoming"} conflict preview`}
				lineClassName={(_, lineIndex) => {
					const hunkIndex = hunkIndexForLine(ranges, lineIndex);
					const isChosen = hunkIndex >= 0 && choices[hunkIndex] === side;
					const isActive = hunkIndex === activeHunkIndex;
					return hunkClass(isActive, isChosen, side);
				}}
				lines={lines}
				onScroll={onScroll}
				renderAction={(_, lineIndex) => {
					const hunkIndex = hunkIndexForLine(ranges, lineIndex);
					const isStart = ranges[hunkIndex]?.start === lineIndex;
					const isChosen = hunkIndex >= 0 && choices[hunkIndex] === side;
					if (!isStart) return null;
					return (
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
					);
				}}
				scrollContainerRef={scrollContainerRef}
			/>
		</section>
	);
}

function hunkIndexForLine(
	ranges: ({ start: number; end: number } | null)[],
	lineIndex: number,
) {
	return ranges.findIndex(
		(range) => range && lineIndex >= range.start && lineIndex <= range.end,
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
