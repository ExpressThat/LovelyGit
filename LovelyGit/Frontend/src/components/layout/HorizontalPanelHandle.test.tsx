// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { HorizontalPanelHandle } from "./HorizontalPanelHandle";

describe("HorizontalPanelHandle", () => {
	it("supports precise keyboard resizing", () => {
		const onResizeBy = vi.fn();
		render(
			<HorizontalPanelHandle
				label="Resize refs panel"
				onPointerDown={vi.fn()}
				onResizeBy={onResizeBy}
				side="right"
			/>,
		);

		const handle = screen.getByRole("button", { name: "Resize refs panel" });
		fireEvent.keyDown(handle, { key: "ArrowLeft" });
		fireEvent.keyDown(handle, { key: "ArrowRight" });
		expect(onResizeBy).toHaveBeenNthCalledWith(1, -16);
		expect(onResizeBy).toHaveBeenNthCalledWith(2, 16);
	});
});
