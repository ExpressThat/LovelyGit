// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useWindowPointerDrag } from "./useWindowPointerDrag";

describe("useWindowPointerDrag", () => {
	it("routes move and finish events then releases its listeners", () => {
		const onMove = vi.fn();
		const onFinish = vi.fn();
		const { result } = renderHook(() => useWindowPointerDrag());

		act(() => result.current({ onFinish, onMove }));
		act(() => window.dispatchEvent(pointerEvent("pointermove", 12)));
		act(() => window.dispatchEvent(pointerEvent("pointerup", 24)));
		act(() => window.dispatchEvent(pointerEvent("pointermove", 36)));

		expect(onMove).toHaveBeenCalledTimes(1);
		expect(onFinish).toHaveBeenCalledTimes(1);
		expect(onFinish.mock.calls[0]?.[0].clientX).toBe(24);
	});

	it("releases global listeners when its owner unmounts mid-drag", () => {
		const onMove = vi.fn();
		const { result, unmount } = renderHook(() => useWindowPointerDrag());
		act(() => result.current({ onMove }));

		unmount();
		act(() => window.dispatchEvent(pointerEvent("pointermove", 12)));

		expect(onMove).not.toHaveBeenCalled();
	});

	it("cancels a drag when the pointer is lost", () => {
		const onCancel = vi.fn();
		const onMove = vi.fn();
		const { result } = renderHook(() => useWindowPointerDrag());
		act(() => result.current({ onCancel, onMove }));

		act(() => window.dispatchEvent(pointerEvent("pointercancel", 0)));
		act(() => window.dispatchEvent(pointerEvent("pointermove", 12)));

		expect(onCancel).toHaveBeenCalledOnce();
		expect(onMove).not.toHaveBeenCalled();
	});
});

function pointerEvent(type: string, clientX: number) {
	return new MouseEvent(type, { clientX }) as unknown as PointerEvent;
}
