import type { GitConflictTextLine } from "@/generated/types";
import { syntaxClass } from "../CommitFileDiff/DiffLineRendering";

export function ConflictCodePane({
	lines,
	title,
	tone,
}: {
	lines: GitConflictTextLine[];
	title: string;
	tone: "ours" | "theirs" | "result";
}) {
	return (
		<section className="flex min-h-0 flex-1 flex-col overflow-hidden border-r bg-background">
			<header
				className={`border-b px-3 py-2 text-sm font-medium ${toneClass(tone)}`}
			>
				{title}
			</header>
			<div className="min-h-0 flex-1 overflow-auto font-mono text-[12px] leading-5">
				{lines.map((line) => (
					<div
						className={`grid grid-cols-[64px_minmax(0,1fr)] ${markerClass(line.markerKind)}`}
						key={line.lineNumber}
					>
						<div className="select-none border-r bg-card/45 px-2 text-right text-muted-foreground">
							{line.lineNumber}
						</div>
						<pre className="min-w-max bg-transparent px-2 whitespace-pre">
							{renderLine(line)}
						</pre>
					</div>
				))}
			</div>
		</section>
	);
}

function renderLine(line: GitConflictTextLine) {
	if (line.text.length === 0) {
		return "\u00a0";
	}

	const boundaries = new Set<number>([0, line.text.length]);
	for (const span of line.syntaxSpans) {
		boundaries.add(Math.min(Math.max(span.start, 0), line.text.length));
		boundaries.add(
			Math.min(Math.max(span.start + span.length, 0), line.text.length),
		);
	}

	const points = Array.from(boundaries).sort((left, right) => left - right);
	return points.slice(0, -1).map((start, index) => {
		const end = points[index + 1];
		const syntax = line.syntaxSpans.find(
			(span) => start >= span.start && start < span.start + span.length,
		);
		return (
			<span className={syntax ? syntaxClass(syntax.scope) : ""} key={start}>
				{line.text.slice(start, end)}
			</span>
		);
	});
}

function toneClass(tone: "ours" | "theirs" | "result") {
	if (tone === "ours") return "bg-sky-500/10 text-sky-700 dark:text-sky-200";
	if (tone === "theirs") {
		return "bg-amber-500/10 text-amber-700 dark:text-amber-200";
	}
	return "bg-violet-500/10 text-violet-700 dark:text-violet-200";
}

function markerClass(markerKind: string) {
	switch (markerKind) {
		case "OursStart":
			return "bg-sky-500/15";
		case "Divider":
			return "bg-border/70";
		case "TheirsEnd":
			return "bg-amber-500/15";
		default:
			return "";
	}
}
