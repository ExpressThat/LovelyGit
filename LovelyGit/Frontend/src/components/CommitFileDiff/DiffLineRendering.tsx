import type {
	CommitFileDiffChangeSpan,
	CommitFileDiffSyntaxSpan,
} from "@/generated/types";
import { getVisibleCharacterRange } from "./DiffLineViewport";

export function LineNumber({ value }: { value?: number | null }) {
	return (
		<div className="select-none border-r bg-card/45 px-2 text-right text-muted-foreground">
			{value ?? ""}
		</div>
	);
}

export function CodeCell({
	changeSpans,
	scrollLeft,
	spans,
	text,
	variant,
	wrapLines,
}: {
	changeSpans?: CommitFileDiffChangeSpan[] | null;
	scrollLeft: number;
	spans?: CommitFileDiffSyntaxSpan[] | null;
	text?: string | null;
	variant: "deleted" | "inserted" | "plain";
	wrapLines: boolean;
}) {
	const displayText = text ?? "";
	const { endOffset, startOffset } = getVisibleCharacterRange(
		displayText.length,
		scrollLeft,
		wrapLines,
	);
	return (
		<div
			className={`min-h-[18px] overflow-hidden border-r px-2 ${
				wrapLines ? "whitespace-pre-wrap break-all" : "whitespace-pre"
			} ${codeVariantClass(variant)}`}
		>
			<div>
				{renderSyntaxLine(
					displayText,
					spans ?? [],
					changeSpans ?? [],
					variant,
					startOffset,
					endOffset,
				)}
			</div>
		</div>
	);
}

function renderSyntaxLine(
	text: string,
	spans: CommitFileDiffSyntaxSpan[],
	changeSpans: CommitFileDiffChangeSpan[],
	variant: "deleted" | "inserted" | "plain",
	startOffset: number,
	endOffset: number,
): React.ReactNode {
	if (text.length === 0) {
		return "\u00a0";
	}

	const visibleSyntaxSpans = spans.filter((span) =>
		overlaps(span.start, span.length, startOffset, endOffset),
	);
	const visibleChangeSpans = changeSpans.filter((span) =>
		overlaps(span.start, span.length, startOffset, endOffset),
	);
	const boundaries = new Set<number>([startOffset, endOffset]);
	for (const span of visibleSyntaxSpans) {
		boundaries.add(Math.min(Math.max(span.start, startOffset), endOffset));
		boundaries.add(
			Math.min(Math.max(span.start + span.length, startOffset), endOffset),
		);
	}
	for (const span of visibleChangeSpans) {
		boundaries.add(Math.min(Math.max(span.start, startOffset), endOffset));
		boundaries.add(
			Math.min(Math.max(span.start + span.length, startOffset), endOffset),
		);
	}

	const sortedBoundaries = Array.from(boundaries)
		.filter((boundary) => boundary >= startOffset)
		.sort((left, right) => left - right);
	const nodes: React.ReactNode[] = [];
	for (let index = 0; index < sortedBoundaries.length - 1; index++) {
		const start = sortedBoundaries[index];
		const end = sortedBoundaries[index + 1];
		if (end <= start) {
			continue;
		}

		const syntax = findCoveringSyntaxSpan(start, visibleSyntaxSpans);
		const change = findCoveringChangeSpan(start, visibleChangeSpans);
		nodes.push(
			<span
				className={[
					syntax ? syntaxClass(syntax.scope) : "",
					change ? changeClass(change.changeType, variant) : "",
				]
					.filter(Boolean)
					.join(" ")}
				key={`${start}:${end}`}
			>
				{text.slice(start, end)}
			</span>,
		);
	}

	return nodes.length > 0 ? nodes : text.slice(startOffset, endOffset);
}

function overlaps(start: number, length: number, from: number, to: number) {
	return start < to && start + length > from;
}

function findCoveringSyntaxSpan(
	offset: number,
	spans: CommitFileDiffSyntaxSpan[],
) {
	let covering: CommitFileDiffSyntaxSpan | undefined;
	for (const span of spans) {
		if (
			offset >= span.start &&
			offset < span.start + span.length &&
			(covering === undefined || span.length < covering.length)
		) {
			covering = span;
		}
	}
	return covering;
}

function findCoveringChangeSpan(
	offset: number,
	spans: CommitFileDiffChangeSpan[],
) {
	return spans.find(
		(span) => offset >= span.start && offset < span.start + span.length,
	);
}

export function lineBackground(changeType: string) {
	switch (changeType) {
		case "Deleted":
			return "bg-red-500/8";
		case "Inserted":
			return "bg-emerald-500/8";
		default:
			return "";
	}
}

export function oldSideVariant(changeType: string) {
	return changeType === "Deleted" || changeType === "Modified"
		? "deleted"
		: "plain";
}

export function newSideVariant(changeType: string) {
	return changeType === "Inserted" || changeType === "Modified"
		? "inserted"
		: "plain";
}

function codeVariantClass(variant: "deleted" | "inserted" | "plain") {
	switch (variant) {
		case "deleted":
			return "bg-red-500/10";
		case "inserted":
			return "bg-emerald-500/10";
		default:
			return "";
	}
}

function changeClass(
	changeType: string,
	variant: "deleted" | "inserted" | "plain",
) {
	switch (changeType) {
		case "Deleted":
			return "rounded-sm bg-red-500/25 text-foreground";
		case "Inserted":
			return "rounded-sm bg-emerald-500/25 text-foreground";
		case "Modified":
			return variant === "deleted"
				? "rounded-sm bg-red-500/25 text-foreground"
				: "rounded-sm bg-emerald-500/25 text-foreground";
		default:
			return "";
	}
}

export function changeMarker(changeType: string) {
	switch (changeType) {
		case "Deleted":
			return "-";
		case "Inserted":
			return "+";
		default:
			return "";
	}
}

export function syntaxClass(scope: string) {
	switch (scope) {
		case "Keyword":
			return "text-blue-600 dark:text-blue-300";
		case "String":
		case "StringEscape":
			return "text-emerald-700 dark:text-emerald-300";
		case "Comment":
			return "text-muted-foreground italic";
		case "Number":
			return "text-amber-700 dark:text-amber-300";
		case "Operator":
			return "text-foreground";
		case "Type":
		case "TypeParameter":
			return "text-cyan-700 dark:text-cyan-300";
		case "Name":
		case "Function":
			return "text-violet-700 dark:text-violet-300";
		default:
			return "";
	}
}
