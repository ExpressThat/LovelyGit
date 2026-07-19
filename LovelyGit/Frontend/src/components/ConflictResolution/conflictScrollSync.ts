import type { ConflictDiffItem, ConflictSide } from "./conflictDiffItems";

export interface ConflictScrollAnchor {
	line: number;
	offsetRatio: number;
}

export interface ConflictScrollRow {
	index: number;
	line: number;
}

export function buildConflictScrollRows(
	items: ConflictDiffItem[],
	side: ConflictSide,
) {
	const rows: ConflictScrollRow[] = [];
	for (let index = 0; index < items.length; index++) {
		const line = conflictItemLine(items[index], side);
		if (line !== null) rows.push({ index, line });
	}
	return rows;
}

export function conflictItemLine(
	item: ConflictDiffItem | undefined,
	side: ConflictSide,
) {
	if (!item) return null;
	if (item.kind === "line") return item.baseLine ?? item.sourceLine;
	if (item.hunk.baseStartLine > 0) return item.hunk.baseStartLine;
	return side === "ours"
		? item.hunk.currentStartLine
		: item.hunk.incomingStartLine;
}

export function findConflictScrollRow(rows: ConflictScrollRow[], line: number) {
	let low = 0;
	let high = rows.length;
	while (low < high) {
		const middle = low + ((high - low) >> 1);
		if ((rows[middle]?.line ?? Number.POSITIVE_INFINITY) < line)
			low = middle + 1;
		else high = middle;
	}
	if (rows[low]?.line === line) return rows[low];
	return rows[Math.max(0, low - 1)] ?? null;
}

export function findMeasurementAtOffset(
	measurements: Array<{ index: number; start: number; size: number }>,
	offset: number,
) {
	let low = 0;
	let high = measurements.length;
	while (low < high) {
		const middle = low + ((high - low) >> 1);
		const measurement = measurements[middle];
		if (measurement && measurement.start + measurement.size <= offset)
			low = middle + 1;
		else high = middle;
	}
	return measurements[Math.min(low, measurements.length - 1)] ?? null;
}
