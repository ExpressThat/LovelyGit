import { useRef, useState } from "react";

export function useSynchronizedDiffScroll() {
	const oldScrollerRef = useRef<HTMLDivElement | null>(null);
	const newScrollerRef = useRef<HTMLDivElement | null>(null);
	const syncingRef = useRef(false);
	const [oldScrollLeft, setOldScrollLeft] = useState(0);
	const [newScrollLeft, setNewScrollLeft] = useState(0);

	const syncBottomScroll = (
		source: "old" | "new",
		event: React.UIEvent<HTMLDivElement>,
	) => {
		if (syncingRef.current) return;
		const nextScrollLeft = event.currentTarget.scrollLeft;
		const target =
			source === "old" ? newScrollerRef.current : oldScrollerRef.current;
		if (!target) return;

		syncingRef.current = true;
		target.scrollLeft = nextScrollLeft;
		setOldScrollLeft(nextScrollLeft);
		setNewScrollLeft(nextScrollLeft);
		requestAnimationFrame(() => {
			syncingRef.current = false;
		});
	};

	return {
		newScrollerRef,
		newScrollLeft,
		oldScrollerRef,
		oldScrollLeft,
		syncBottomScroll,
	};
}
