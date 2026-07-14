// @vitest-environment jsdom
import { act, renderHook } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
	overlayIdleRetentionMs,
	useRetainedSurface,
} from "./useRetainedSurface";

describe("useRetainedSurface", () => {
	afterEach(() => vi.useRealTimers());

	it("releases a closed surface after its idle retention window", () => {
		vi.useFakeTimers();
		const { result, rerender } = renderHook(
			({ active }) => useRetainedSurface(active),
			{ initialProps: { active: false } },
		);

		rerender({ active: true });
		expect(result.current).toBe(true);
		rerender({ active: false });
		expect(result.current).toBe(true);

		act(() => vi.advanceTimersByTime(overlayIdleRetentionMs - 1));
		expect(result.current).toBe(true);
		act(() => vi.advanceTimersByTime(1));
		expect(result.current).toBe(false);
	});

	it("cancels release when the surface reopens", () => {
		vi.useFakeTimers();
		const { result, rerender } = renderHook(
			({ active }) => useRetainedSurface(active),
			{ initialProps: { active: true } },
		);

		rerender({ active: false });
		act(() => vi.advanceTimersByTime(overlayIdleRetentionMs - 1));
		rerender({ active: true });
		act(() => vi.advanceTimersByTime(1));

		expect(result.current).toBe(true);
	});
});
