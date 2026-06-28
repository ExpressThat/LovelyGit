import type { GitConflictTextLine } from "@/generated/types";
import { syntaxClass } from "../CommitFileDiff/DiffLineRendering";

export function ConflictRenderedLine({ line }: { line: GitConflictTextLine }) {
	if (line.text.length === 0) return "\u00a0";
	if (line.syntaxSpans.length === 0) return line.text;

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
		const syntax = line.syntaxSpans
			.filter((span) => start >= span.start && start < span.start + span.length)
			.sort((left, right) => left.length - right.length)[0];
		return (
			<span className={syntax ? syntaxClass(syntax.scope) : ""} key={start}>
				{line.text.slice(start, end)}
			</span>
		);
	});
}
