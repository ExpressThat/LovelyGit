export type ScrollPosition = {
	scrollLeft: number;
	scrollTop: number;
};

export function copyScrollPosition(
	source: ScrollPosition,
	target: ScrollPosition | null,
) {
	if (!target) return;
	target.scrollLeft = source.scrollLeft;
	target.scrollTop = source.scrollTop;
}
