import type { ConflictHunk } from "@/generated/types";
import type { ConflictSide } from "./conflictDiffItems";

type IndexedRange = {
	start: number;
	end: number;
	order: number;
	hunk: ConflictHunk;
};

type RangeEvent = {
	position: number;
	start: boolean;
	range: IndexedRange;
};

export type ConflictHunkLookup = ReturnType<typeof createConflictHunkLookup>;

export function createConflictHunkLookup(
	hunks: ConflictHunk[],
	side: ConflictSide,
) {
	const base = buildRanges(hunks, (hunk) => [
		hunk.baseStartLine,
		hunk.baseLineCount,
	]);
	const source = buildRanges(hunks, (hunk) =>
		side === "ours"
			? [hunk.currentStartLine, hunk.currentLineCount]
			: [hunk.incomingStartLine, hunk.incomingLineCount],
	);
	return {
		find(baseLine: number | null, sourceLine: number | null) {
			const baseMatch = findRange(base, baseLine);
			const sourceMatch = findRange(source, sourceLine);
			if (!baseMatch) return sourceMatch?.hunk;
			if (!sourceMatch) return baseMatch.hunk;
			return baseMatch.order <= sourceMatch.order
				? baseMatch.hunk
				: sourceMatch.hunk;
		},
	};
}

function buildRanges(
	hunks: ConflictHunk[],
	select: (hunk: ConflictHunk) => [number, number],
) {
	const ranges: IndexedRange[] = [];
	for (let order = 0; order < hunks.length; order++) {
		const hunk = hunks[order];
		const [start, count] = select(hunk);
		if (count <= 0) continue;
		ranges.push({ start, end: start + count, order, hunk });
	}
	return normalizeRanges(ranges);
}

function normalizeRanges(ranges: IndexedRange[]) {
	const events: RangeEvent[] = [];
	for (const range of ranges) {
		events.push({ position: range.start, start: true, range });
		events.push({ position: range.end, start: false, range });
	}
	events.sort(
		(left, right) =>
			left.position - right.position ||
			Number(left.start) - Number(right.start) ||
			left.range.order - right.range.order,
	);
	const normalized: IndexedRange[] = [];
	const active = new Map<number, IndexedRange>();
	let previous = events[0]?.position ?? 0;
	let index = 0;
	while (index < events.length) {
		const position = events[index].position;
		if (position > previous && active.size > 0) {
			let selected: IndexedRange | undefined;
			for (const range of active.values()) {
				if (!selected || range.order < selected.order) selected = range;
			}
			if (selected) appendRange(normalized, selected, previous, position);
		}
		while (index < events.length && events[index].position === position) {
			const event = events[index++];
			if (event.start) active.set(event.range.order, event.range);
			else active.delete(event.range.order);
		}
		previous = position;
	}
	return normalized;
}

function appendRange(
	ranges: IndexedRange[],
	selected: IndexedRange,
	start: number,
	end: number,
) {
	const previous = ranges.at(-1);
	if (previous?.hunk === selected.hunk && previous.end === start) {
		previous.end = end;
		return;
	}
	ranges.push({ ...selected, start, end });
}

function findRange(ranges: IndexedRange[], value: number | null) {
	if (value == null) return undefined;
	let low = 0;
	let high = ranges.length - 1;
	while (low <= high) {
		const middle = (low + high) >>> 1;
		if (ranges[middle].start <= value) low = middle + 1;
		else high = middle - 1;
	}
	const candidate = ranges[high];
	return candidate && value < candidate.end ? candidate : undefined;
}
