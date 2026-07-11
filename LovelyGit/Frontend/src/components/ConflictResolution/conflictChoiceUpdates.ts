import type { ConflictSide } from "./conflictDiffItems";
import type { ConflictChoice } from "./conflictDocument";

export function updateWholeChoice(
	choice: ConflictChoice,
	side: ConflictSide,
	accepted: boolean,
	count: number,
): ConflictChoice {
	const next = {
		...choice,
		[side]: { accepted, lines: Array.from({ length: count }, () => accepted) },
	};
	return withResolution(next);
}

export function toggleLineChoice(
	choice: ConflictChoice,
	side: ConflictSide,
	index: number,
): ConflictChoice {
	const lines = choice[side].lines.map((value, lineIndex) =>
		lineIndex === index ? !value : value,
	);
	const next = { ...choice, [side]: { accepted: lines.some(Boolean), lines } };
	return withResolution(next);
}

function withResolution(choice: ConflictChoice): ConflictChoice {
	return {
		...choice,
		resolution:
			choice.ours.accepted || choice.theirs.accepted
				? "selection"
				: "unresolved",
	};
}
