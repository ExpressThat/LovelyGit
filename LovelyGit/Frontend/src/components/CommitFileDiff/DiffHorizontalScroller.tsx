import { useEffect, useRef, useState } from "react";

export function DiffHorizontalScroller({
	contentWidth,
	label,
	onValueChange,
	value,
}: {
	contentWidth: number;
	label: string;
	onValueChange: (value: number) => void;
	value: number;
}) {
	const rootRef = useRef<HTMLDivElement | null>(null);
	const frameRef = useRef<number | null>(null);
	const pendingValueRef = useRef<number | null>(null);
	const [viewportWidth, setViewportWidth] = useState(0);
	const scheduleValueChange = (nextValue: number) => {
		pendingValueRef.current = nextValue;
		if (frameRef.current !== null) return;
		frameRef.current = requestAnimationFrame(() => {
			frameRef.current = null;
			const pendingValue = pendingValueRef.current;
			pendingValueRef.current = null;
			if (pendingValue === null) return;
			onValueChange(pendingValue);
		});
	};
	useEffect(() => {
		const root = rootRef.current;
		if (!root) return;
		const measure = () => setViewportWidth(root.clientWidth);
		measure();
		if (typeof ResizeObserver === "undefined") {
			window.addEventListener("resize", measure);
			return () => window.removeEventListener("resize", measure);
		}

		const observer = new ResizeObserver(measure);
		observer.observe(root);
		return () => observer.disconnect();
	}, []);
	useEffect(
		() => () => {
			if (frameRef.current !== null) cancelAnimationFrame(frameRef.current);
		},
		[],
	);
	const maximum = Math.max(0, Math.ceil(contentWidth - viewportWidth));
	const current = Math.min(value, maximum);
	useEffect(() => {
		if (value > maximum) onValueChange(maximum);
	}, [maximum, onValueChange, value]);

	return (
		<div className="h-3 w-full shrink-0 border-t bg-background" ref={rootRef}>
			{maximum > 0 ? (
				<input
					aria-label={label}
					className="diff-horizontal-range block h-full w-full"
					max={maximum}
					min={0}
					onChange={(event) =>
						scheduleValueChange(event.currentTarget.valueAsNumber)
					}
					onWheel={(event) => {
						const delta = Math.abs(event.deltaX) > Math.abs(event.deltaY)
							? event.deltaX
							: event.deltaY;
						scheduleValueChange(
							Math.min(maximum, Math.max(0, current + delta)),
						);
					}}
					step={1}
					type="range"
					value={current}
				/>
			) : null}
		</div>
	);
}
