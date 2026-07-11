// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { RefsAccordionPane, RefsPaneSplitter } from "./RefsAccordionPane";

describe("RefsAccordionPane", () => {
	it("renders expanded content in its own scroll region", () => {
		const { container } = render(
			<RefsAccordionPane
				count={2}
				id="Branches"
				isOpen
				onToggle={vi.fn()}
				weight={1}
			>
				<span>main</span>
			</RefsAccordionPane>,
		);

		expect(screen.getByText("main")).toBeInTheDocument();
		expect(container.querySelector(".overflow-y-auto")).not.toBeNull();
		expect(
			screen.getByRole("button", { name: "Collapse Branches" }),
		).toHaveAttribute("aria-expanded", "true");
	});

	it("keeps collapsed state independent from other panes", () => {
		const onToggle = vi.fn();
		render(
			<RefsAccordionPane
				count={2}
				id="Tags"
				isOpen={false}
				onToggle={onToggle}
				weight={1}
			>
				<span>v1.0</span>
			</RefsAccordionPane>,
		);

		expect(screen.queryByText("v1.0")).not.toBeInTheDocument();
		fireEvent.click(screen.getByRole("button", { name: "Expand Tags" }));
		expect(onToggle).toHaveBeenCalledOnce();
	});
});

describe("RefsPaneSplitter", () => {
	it("resizes adjacent panes from the keyboard", () => {
		const onResizeBy = vi.fn();
		render(
			<RefsPaneSplitter onPointerDown={vi.fn()} onResizeBy={onResizeBy} />,
		);
		const splitter = screen.getByRole("button", {
			name: "Resize reference sections",
		});
		fireEvent.keyDown(splitter, { key: "ArrowUp" });
		fireEvent.keyDown(splitter, { key: "ArrowDown" });
		expect(onResizeBy).toHaveBeenNthCalledWith(1, -0.1);
		expect(onResizeBy).toHaveBeenNthCalledWith(2, 0.1);
	});
});
