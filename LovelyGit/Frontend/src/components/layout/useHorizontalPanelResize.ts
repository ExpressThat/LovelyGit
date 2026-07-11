import { useState } from "react";

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
		const stop = (stopEvent: PointerEvent) => {
			const next = clamp(startWidth + (stopEvent.clientX - startX) * direction);
			setPreviewWidth(null);
			onCommit(next);
			window.removeEventListener("pointermove", move);
			window.removeEventListener("pointerup", stop);
		};
		window.addEventListener("pointermove", move);
		window.addEventListener("pointerup", stop, { once: true });
	};
	return { resizeBy, startResize, width: previewWidth ?? clamp(width) };
}
