// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ConflictResultPanel } from "./ConflictResultPanel";

describe("ConflictResultPanel", () => {
	it("keeps a large result gutter bounded to the visible lines", () => {
		const value = Array.from(
			{ length: 20_000 },
			(_, index) => `line ${index + 1}`,
		).join("\n");
		render(
			<ConflictResultPanel
				isManualResult={false}
				isResolved={false}
				onEdit={vi.fn()}
				value={value}
				wrapLines={false}
			/>,
		);
		const editor = screen.getByLabelText("Editable result preview");
		const gutter = screen.getByTestId("conflict-result-line-numbers");

		expect(gutter.textContent?.length).toBeLessThan(500);
		expect(gutter).not.toHaveTextContent("20000");

		fireEvent.scroll(editor, { target: { scrollTop: 9_000 } });

		expect(gutter).toHaveTextContent("500");
		expect(gutter).not.toHaveTextContent(/^1\s/);
		expect(gutter.textContent?.length).toBeLessThan(500);
	});
});
