// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useHorizontalPanelResize } from "./useHorizontalPanelResize";

describe("useHorizontalPanelResize", () => {
	it("clamps committed keyboard sizes to panel bounds", () => {
		const onCommit = vi.fn();
		const { result, rerender } = renderHook(
			({ width }) =>
				useHorizontalPanelResize({
					direction: 1,
					max: 520,
					min: 208,
					onCommit,
					width,
				}),
			{ initialProps: { width: 256 } },
		);

		act(() => result.current.resizeBy(-1000));
		expect(onCommit).toHaveBeenLastCalledWith(208);
		rerender({ width: 500 });
		act(() => result.current.resizeBy(100));
		expect(onCommit).toHaveBeenLastCalledWith(520);
	});
});
