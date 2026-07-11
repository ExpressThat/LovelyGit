import { type RefObject, useState } from "react";

const MINIMUM_SOURCE_PERCENT = 30;
const MAXIMUM_SOURCE_PERCENT = 76;

export function useConflictSplitter(
	containerRef: RefObject<HTMLDivElement | null>,
) {
	const [sourcePercent, setSourcePercent] = useState(58);
	const [isDragging, setIsDragging] = useState(false);

	const startDrag = (event: React.PointerEvent<HTMLButtonElement>) => {
		const container = containerRef.current;
		if (!container) return;
		event.preventDefault();
		setIsDragging(true);
		const bounds = container.getBoundingClientRect();
		const move = (moveEvent: PointerEvent) => {
			const percent = ((moveEvent.clientY - bounds.top) / bounds.height) * 100;
			setSourcePercent(
				Math.min(
					MAXIMUM_SOURCE_PERCENT,
					Math.max(MINIMUM_SOURCE_PERCENT, percent),
				),
			);
		};
		const stop = () => {
			setIsDragging(false);
			window.removeEventListener("pointermove", move);
			window.removeEventListener("pointerup", stop);
		};
		window.addEventListener("pointermove", move);
		window.addEventListener("pointerup", stop, { once: true });
	};
	const resizeBy = (amount: number) => {
		setSourcePercent((current) =>
			Math.min(
				MAXIMUM_SOURCE_PERCENT,
				Math.max(MINIMUM_SOURCE_PERCENT, current + amount),
			),
		);
	};

	return { isDragging, resizeBy, sourcePercent, startDrag };
}
