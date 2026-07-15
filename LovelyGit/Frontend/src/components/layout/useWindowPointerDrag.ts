import { useCallback, useEffect, useRef } from "react";

type PointerDragHandlers = {
	onCancel?: () => void;
	onFinish?: (event: PointerEvent) => void;
	onMove: (event: PointerEvent) => void;
};

export function useWindowPointerDrag() {
	const cleanupRef = useRef<(() => void) | null>(null);
	useEffect(() => () => cleanupRef.current?.(), []);

	return useCallback(({ onCancel, onFinish, onMove }: PointerDragHandlers) => {
		cleanupRef.current?.();
		const cleanup = () => {
			window.removeEventListener("pointermove", onMove);
			window.removeEventListener("pointerup", finish);
			window.removeEventListener("pointercancel", cancel);
			window.removeEventListener("blur", cancel);
			if (cleanupRef.current === cleanup) cleanupRef.current = null;
		};
		const finish = (event: PointerEvent) => {
			cleanup();
			onFinish?.(event);
		};
		const cancel = () => {
			cleanup();
			onCancel?.();
		};
		cleanupRef.current = cleanup;
		window.addEventListener("pointermove", onMove);
		window.addEventListener("pointerup", finish, { once: true });
		window.addEventListener("pointercancel", cancel, { once: true });
		window.addEventListener("blur", cancel, { once: true });
	}, []);
}
