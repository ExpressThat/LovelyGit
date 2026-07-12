// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { prefetchCommitDetails } from "@/lib/commitDetailsCache";
import { useCommitDetailsPrefetch } from "./useCommitDetailsPrefetch";

vi.mock("@/lib/commitDetailsCache", () => ({ prefetchCommitDetails: vi.fn() }));

describe("useCommitDetailsPrefetch", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.useFakeTimers();
	});
	afterEach(() => vi.useRealTimers());

	it("prefetches only after hover intent", () => {
		const { result } = renderHook(() =>
			useCommitDetailsPrefetch("repo", "hash"),
		);

		act(() => result.current.start());
		act(() => vi.advanceTimersByTime(119));
		expect(prefetchCommitDetails).not.toHaveBeenCalled();
		act(() => vi.advanceTimersByTime(1));
		expect(prefetchCommitDetails).toHaveBeenCalledWith("repo", "hash");
	});

	it("cancels prefetch when the pointer leaves early", () => {
		const { result } = renderHook(() =>
			useCommitDetailsPrefetch("repo", "hash"),
		);

		act(() => result.current.start());
		act(() => result.current.cancel());
		act(() => vi.runAllTimers());

		expect(prefetchCommitDetails).not.toHaveBeenCalled();
	});
});
