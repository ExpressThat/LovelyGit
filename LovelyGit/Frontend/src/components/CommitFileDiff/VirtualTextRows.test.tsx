// @vitest-environment jsdom

import { render } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import { VirtualTextRow } from "./VirtualTextRows";

it("registers wrapped rows for dynamic virtualizer measurement", () => {
	const measureElement = vi.fn();
	const { container } = render(
		<VirtualTextRow
			changeType="Inserted"
			contentWidth={800}
			index={12}
			line={"a long line ".repeat(30)}
			measureElement={measureElement}
			scrollLeft={0}
			viewMode="SideBySide"
			wrapLines
			y={216}
		/>,
	);

	const row = container.firstElementChild;
	expect(row).toHaveAttribute("data-index", "12");
	expect(measureElement).toHaveBeenCalledWith(row);
});
