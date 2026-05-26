import type { ColKey } from "../constants";
import { COL_MIN, COL_ORDER } from "../constants";

export function resolveWidths(
	targetWidth: number,
	preferred: Record<ColKey, number>,
) {
	const minSum = COL_ORDER.reduce((sum, key) => sum + COL_MIN[key], 0);
	const safeWidth = Math.max(1, targetWidth);

	if (safeWidth <= minSum) {
		const ratio = safeWidth / minSum;
		return COL_ORDER.reduce(
			(acc, key) => {
				acc[key] = Math.max(40, Math.floor(COL_MIN[key] * ratio));
				return acc;
			},
			{} as Record<ColKey, number>,
		);
	}

	const next = COL_ORDER.reduce(
		(acc, key) => {
			acc[key] = Math.max(COL_MIN[key], Math.floor(preferred[key]));
			return acc;
		},
		{} as Record<ColKey, number>,
	);

	let total = COL_ORDER.reduce((sum, key) => sum + next[key], 0);
	if (total < safeWidth) {
		next.message += safeWidth - total;
		return next;
	}

	let overflow = total - safeWidth;
	const shrinkOrder: ColKey[] = [
		"message",
		"graph",
		"author",
		"branch",
		"hash",
	];
	for (const key of shrinkOrder) {
		if (overflow <= 0) {
			break;
		}
		const reducible = next[key] - COL_MIN[key];
		if (reducible <= 0) {
			continue;
		}
		const delta = Math.min(reducible, overflow);
		next[key] -= delta;
		overflow -= delta;
	}

	total = COL_ORDER.reduce((sum, key) => sum + next[key], 0);
	if (total !== safeWidth) {
		next.message = Math.max(
			COL_MIN.message,
			next.message + (safeWidth - total),
		);
	}

	return next;
}
