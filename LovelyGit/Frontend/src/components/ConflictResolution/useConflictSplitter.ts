import { type RefObject, useState } from "react";
import { useWindowPointerDrag } from "@/components/layout/useWindowPointerDrag";

const MINIMUM_SOURCE_PERCENT = 30;
const MAXIMUM_SOURCE_PERCENT = 76;

export function useConflictSplitter(
	containerRef: RefObject<HTMLDivElement | null>,
) {
	const [sourcePercent, setSourcePercent] = useState(58);
	const [isDragging, setIsDragging] = useState(false);
	const startPointerDrag = useWindowPointerDrag();

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
		startPointerDrag({
			onCancel: () => setIsDragging(false),
			onFinish: () => setIsDragging(false),
			onMove: move,
		});
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
