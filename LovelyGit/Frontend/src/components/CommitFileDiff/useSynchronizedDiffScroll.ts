import { useState } from "react";

export function useSynchronizedDiffScroll() {
	const [scrollLeft, setScrollLeft] = useState(0);
	return {
		newScrollLeft: scrollLeft,
		oldScrollLeft: scrollLeft,
		setScrollLeft,
	};
}
