import { useState } from "react";
import { useWindowPointerDrag } from "./useWindowPointerDrag";

export function useHorizontalPanelResize({
	direction,
	max,
	min,
	onCommit,
	width,
}: {
	direction: -1 | 1;
	max: number;
	min: number;
	onCommit: (width: number) => void;
	width: number;
}) {
	const [previewWidth, setPreviewWidth] = useState<number | null>(null);
	const startPointerDrag = useWindowPointerDrag();
	const clamp = (value: number) => Math.min(max, Math.max(min, value));
	const resizeBy = (amount: number) => onCommit(clamp(width + amount));
	const startResize = (event: React.PointerEvent<HTMLButtonElement>) => {
		event.preventDefault();
		const startX = event.clientX;
		const startWidth = width;
		const move = (moveEvent: PointerEvent) => {
			setPreviewWidth(
				clamp(startWidth + (moveEvent.clientX - startX) * direction),
			);
		};
		const finish = (stopEvent: PointerEvent) => {
			const next = clamp(startWidth + (stopEvent.clientX - startX) * direction);
			setPreviewWidth(null);
			onCommit(next);
		};
		startPointerDrag({
			onCancel: () => setPreviewWidth(null),
			onFinish: finish,
			onMove: move,
		});
	};
	return { resizeBy, startResize, width: previewWidth ?? clamp(width) };
}
