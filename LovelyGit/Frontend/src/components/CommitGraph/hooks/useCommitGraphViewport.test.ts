// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	commitGraphContentHeight,
	useCommitGraphViewport,
} from "./useCommitGraphViewport";

describe("commitGraphContentHeight", () => {
	it("keeps a stable placeholder before the first page arrives", () => {
		expect(commitGraphContentHeight(0)).toBe(3_080);
	});

	it("uses the exact fixed-row height for loaded graph ranges", () => {
		expect(commitGraphContentHeight(768)).toBe(16_896);
	});
});

describe("useCommitGraphViewport", () => {
	it("resets vertical and horizontal scrolling when switching repositories", () => {
		const ensureRangeLoaded = vi.fn();
		const { result, rerender } = renderHook(
			({ repositoryId }) =>
				useCommitGraphViewport({
					ensureRangeLoaded,
					laneCount: 2,
					repositoryId,
					totalRows: 100,
				}),
			{ initialProps: { repositoryId: "first" } },
		);
		const verticalScroller = document.createElement("div");
		const horizontalScroller = document.createElement("div");
		verticalScroller.scrollTop = 440;
		horizontalScroller.scrollLeft = 120;
		result.current.scrollRef.current = verticalScroller;
		result.current.graphScrollerRef.current = horizontalScroller;

		act(() => {
			result.current.setGraphScrollLeft(120);
		});
		rerender({ repositoryId: "second" });

		expect(verticalScroller.scrollTop).toBe(0);
		expect(horizontalScroller.scrollLeft).toBe(0);
		expect(result.current.graphScrollLeft).toBe(0);
	});
});
