// @vitest-environment jsdom
import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { DiffHorizontalScroller } from "./DiffHorizontalScroller";

describe("DiffHorizontalScroller", () => {
	afterEach(() => vi.restoreAllMocks());

	it("provides an accessible range without a wide overflow child", () => {
		const paints: FrameRequestCallback[] = [];
		vi.spyOn(window, "requestAnimationFrame").mockImplementation((callback) => {
			paints.push(callback);
			return paints.length;
		});
		const onValueChange = vi.fn();
		render(
			<section>
				<DiffHorizontalScroller
					contentWidth={1_200}
					label="Horizontal combined diff scroll"
					onValueChange={onValueChange}
					value={0}
				/>
			</section>,
		);

		const slider = screen.getByRole("slider", {
			name: "Horizontal combined diff scroll",
		});
		expect(slider).toHaveClass("diff-horizontal-range");
		expect(slider).toBeInstanceOf(HTMLInputElement);
		expect(slider.childElementCount).toBe(0);
		fireEvent.change(slider, { target: { value: "240" } });
		expect(onValueChange).not.toHaveBeenCalled();
		act(() => paints.shift()?.(0));
		expect(onValueChange).toHaveBeenCalledWith(240);
		expect(paints).toHaveLength(0);
	});

	it("maps horizontal wheel input to the shared offset", () => {
		vi.spyOn(window, "requestAnimationFrame").mockImplementation((callback) => {
			callback(0);
			return 1;
		});
		const onValueChange = vi.fn();
		render(
			<DiffHorizontalScroller
				contentWidth={1_200}
				label="Horizontal text diff scroll"
				onValueChange={onValueChange}
				value={20}
			/>,
		);

		fireEvent.wheel(screen.getByRole("slider"), { deltaX: 80, deltaY: 0 });
		expect(onValueChange).toHaveBeenCalledWith(100);
	});

	it("does not show a scrollbar when the content fits", () => {
		vi.spyOn(HTMLElement.prototype, "clientWidth", "get").mockReturnValue(800);
		render(
			<DiffHorizontalScroller
				contentWidth={320}
				label="Horizontal combined diff scroll"
				onValueChange={vi.fn()}
				value={0}
			/>,
		);

		expect(screen.queryByRole("slider")).not.toBeInTheDocument();
	});
});
