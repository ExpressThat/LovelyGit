const APPROXIMATE_CHARACTER_WIDTH = 7.25;
const MAX_UNWRAPPED_RENDER_CHARACTERS = 4_096;

export function getVisibleCharacterRange(
	textLength: number,
	scrollLeft: number,
	wrapLines: boolean,
) {
	if (wrapLines) return { endOffset: textLength, startOffset: 0 };

	const startOffset = Math.min(
		textLength,
		Math.floor(scrollLeft / APPROXIMATE_CHARACTER_WIDTH),
	);
	return {
		endOffset: Math.min(
			textLength,
			startOffset + MAX_UNWRAPPED_RENDER_CHARACTERS,
		),
		startOffset,
	};
}

export function estimateCodeWidth(values: Iterable<string | null | undefined>) {
	let longestLineLength = 0;
	for (const value of values) {
		longestLineLength = Math.max(longestLineLength, value?.length ?? 0);
	}

	return Math.max(320, longestLineLength * APPROXIMATE_CHARACTER_WIDTH + 32);
}
