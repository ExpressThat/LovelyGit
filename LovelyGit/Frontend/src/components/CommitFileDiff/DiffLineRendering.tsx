import type {
	CommitFileDiffChangeSpan,
	CommitFileDiffSyntaxSpan,
} from "@/generated/types";

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
	width,
	wrapLines,
}: {
	changeSpans: CommitFileDiffChangeSpan[];
	scrollLeft: number;
	spans: CommitFileDiffSyntaxSpan[];
	text: string;
	variant: "deleted" | "inserted" | "plain";
	width: number;
	wrapLines: boolean;
}) {
	return (
		<div
			className={`min-h-[18px] overflow-hidden border-r px-2 ${
				wrapLines ? "whitespace-pre-wrap break-all" : "whitespace-pre"
			} ${codeVariantClass(variant)}`}
		>
			<div
				style={
					wrapLines
						? undefined
						: {
								transform: `translateX(-${scrollLeft}px)`,
								width,
							}
				}
			>
				{renderSyntaxLine(text, spans, changeSpans, variant)}
			</div>
		</div>
	);
}

export function estimateCodeWidth(lines: string[]) {
	const longestLineLength = lines.reduce(
		(longest, line) => Math.max(longest, line.length),
		0,
	);
	return Math.min(48_000, Math.max(1_200, longestLineLength * 7.25 + 32));
}

function renderSyntaxLine(
	text: string,
	spans: CommitFileDiffSyntaxSpan[],
	changeSpans: CommitFileDiffChangeSpan[],
	variant: "deleted" | "inserted" | "plain",
): React.ReactNode {
	if (text.length === 0) {
		return "\u00a0";
	}

	const boundaries = new Set<number>([0, text.length]);
	for (const span of spans) {
		boundaries.add(Math.min(Math.max(span.start, 0), text.length));
		boundaries.add(
			Math.min(Math.max(span.start + span.length, 0), text.length),
		);
	}
	for (const span of changeSpans) {
		boundaries.add(Math.min(Math.max(span.start, 0), text.length));
		boundaries.add(
			Math.min(Math.max(span.start + span.length, 0), text.length),
		);
	}

	const sortedBoundaries = Array.from(boundaries).sort(
		(left, right) => left - right,
	);
	const nodes: React.ReactNode[] = [];
	for (let index = 0; index < sortedBoundaries.length - 1; index++) {
		const start = sortedBoundaries[index];
		const end = sortedBoundaries[index + 1];
		if (end <= start) {
			continue;
		}

		const syntax = findCoveringSyntaxSpan(start, spans);
		const change = findCoveringChangeSpan(start, changeSpans);
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

	return nodes.length > 0 ? nodes : text;
}

function findCoveringSyntaxSpan(
	offset: number,
	spans: CommitFileDiffSyntaxSpan[],
) {
	return spans
		.filter((span) => offset >= span.start && offset < span.start + span.length)
		.sort((left, right) => left.length - right.length)[0];
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
