import { Check, Minus, Plus } from "lucide-react";
import { Checkbox } from "@/components/ui/checkbox";
import type { ConflictHunk, ConflictSourceMetadata } from "@/generated/types";
import { cn } from "@/lib/utils";
import { CodeCell } from "../CommitFileDiff/DiffLineRendering";
import { toggleLineChoice, updateWholeChoice } from "./conflictChoiceUpdates";
import type { ConflictDiffItem, ConflictSide } from "./conflictDiffItems";
import type { ConflictChoice } from "./conflictDocument";

type PaneItemProps = {
	item: ConflictDiffItem;
	choices: Record<number, ConflictChoice>;
	disabled: boolean;
	onChoice: (id: number, choice: ConflictChoice) => void;
	side: ConflictSide;
	width: number;
	wrapLines: boolean;
};

export function ConflictSourceHeader({
	metadata,
	side,
}: {
	metadata: ConflictSourceMetadata;
	side: ConflictSide;
}) {
	return (
		<header className="flex h-10 shrink-0 items-center gap-2 border-b bg-card px-3">
			<span
				className={cn(
					"grid size-5 place-items-center rounded border text-[11px] font-bold",
					side === "ours"
						? "border-cyan-500/50 text-cyan-500"
						: "border-amber-500/50 text-amber-500",
				)}
			>
				{side === "ours" ? "A" : "B"}
			</span>
			<div className="min-w-0">
				<div className="text-xs font-semibold">{sideLabel(side)}</div>
				<div className="truncate text-[10px] text-muted-foreground">
					{metadata.refName ?? metadata.label}
				</div>
			</div>
			{metadata.objectId ? (
				<code className="ml-auto shrink-0 text-[10px] text-muted-foreground">
					{metadata.objectId.slice(0, 8)}
				</code>
			) : null}
		</header>
	);
}

export function ConflictPaneItem(props: PaneItemProps) {
	return props.item.kind === "hunk" ? (
		<HunkControl {...props} hunk={props.item.hunk} />
	) : (
		<SourceLine {...props} item={props.item} />
	);
}

function HunkControl({
	choices,
	disabled,
	hunk,
	onChoice,
	side,
}: Omit<PaneItemProps, "item"> & { hunk: ConflictHunk }) {
	const choice = choices[hunk.id];
	const selection = choice?.[side];
	const count =
		side === "ours" ? hunk.currentLineCount : hunk.incomingLineCount;
	const selectedCount = selection?.lines.filter(Boolean).length ?? 0;
	const checked = Boolean(selection?.accepted && selectedCount === count);
	const partial = Boolean(selection?.accepted && !checked);
	return (
		<div
			className={cn(
				"flex h-[34px] items-center gap-2 border-y bg-secondary/55 px-3 font-sans",
				selection?.accepted && "ring-1 ring-inset ring-primary/70",
			)}
		>
			<span className="relative grid size-4 place-items-center">
				<Checkbox
					aria-label={`Keep entire ${sideLabel(side).toLowerCase()} chunk ${hunk.id + 1}`}
					checked={checked}
					disabled={disabled}
					indeterminate={partial}
					onCheckedChange={(value) => {
						if (choice)
							onChoice(
								hunk.id,
								updateWholeChoice(choice, side, value === true, count),
							);
					}}
				/>
				{partial ? (
					<Minus className="pointer-events-none absolute size-3 text-primary-foreground" />
				) : null}
			</span>
			<span className="text-[10px] font-semibold uppercase tracking-wide">
				Conflict {hunk.id + 1}
			</span>
			<span className="ml-auto text-[10px] text-muted-foreground">
				{partial
					? `${selectedCount}/${count} lines`
					: checked
						? count === 0
							? "Keep deletion"
							: "Included"
						: "Not included"}
			</span>
		</div>
	);
}

function SourceLine({
	choices,
	disabled,
	item,
	onChoice,
	side,
	width,
	wrapLines,
}: Omit<PaneItemProps, "item"> & {
	item: Extract<ConflictDiffItem, { kind: "line" }>;
}) {
	const selected =
		item.hunkId != null && item.candidateIndex != null
			? Boolean(choices[item.hunkId]?.[side].lines[item.candidateIndex])
			: false;
	return (
		<div
			className={cn(
				"grid min-h-[18px] grid-cols-[3rem_3rem_minmax(0,1fr)_2rem]",
				item.variant === "deleted" && "bg-red-500/8",
				item.variant === "inserted" && "bg-emerald-500/8",
				selected && "ring-1 ring-inset ring-primary/80",
			)}
		>
			<Gutter value={item.baseLine} />
			<Gutter value={item.sourceLine} />
			<CodeCell
				changeSpans={item.changeSpans}
				scrollLeft={0}
				spans={item.syntaxSpans}
				text={item.text}
				variant={item.variant}
				width={width}
				wrapLines={wrapLines}
			/>
			{item.candidateIndex == null || item.hunkId == null ? (
				<span className="border-l" />
			) : (
				<LineAction
					{...{ choices, disabled, item, onChoice, selected, side }}
				/>
			)}
		</div>
	);
}

function LineAction({
	choices,
	disabled,
	item,
	onChoice,
	selected,
	side,
}: Pick<
	Parameters<typeof SourceLine>[0],
	"choices" | "disabled" | "item" | "onChoice" | "side"
> & { selected: boolean }) {
	const hunkId = item.hunkId;
	const candidateIndex = item.candidateIndex;
	if (hunkId == null || candidateIndex == null || !choices[hunkId])
		return <span className="border-l" />;
	return (
		<button
			aria-label={`${selected ? "Remove" : "Apply"} ${sideLabel(side).toLowerCase()} line ${item.sourceLine ?? 0}`}
			aria-pressed={selected}
			className="grid place-items-center border-l text-muted-foreground hover:bg-accent hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring"
			disabled={disabled}
			onClick={() =>
				onChoice(
					hunkId,
					toggleLineChoice(choices[hunkId], side, candidateIndex),
				)
			}
			type="button"
		>
			{selected ? (
				<Check className="size-3.5 text-primary" />
			) : (
				<Plus className="size-3.5" />
			)}
		</button>
	);
}

function Gutter({ value }: { value: number | null }) {
	return (
		<span className="select-none border-r bg-card/45 px-2 text-right tabular-nums text-muted-foreground">
			{value ?? ""}
		</span>
	);
}

function sideLabel(side: ConflictSide) {
	return side === "ours" ? "Current" : "Incoming";
}
