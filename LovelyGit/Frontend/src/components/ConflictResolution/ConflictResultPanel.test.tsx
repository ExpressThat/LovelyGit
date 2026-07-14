// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	ConflictResultPanel,
	measureConflictText,
} from "./ConflictResultPanel";

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

	it("does not reflow a large editor when every line already fits", () => {
		const value = Array.from(
			{ length: 20_000 },
			(_, index) => `export const value${index} = ${index};`,
		).join("\n");
		render(
			<ConflictResultPanel
				isManualResult={false}
				isResolved={false}
				onEdit={vi.fn()}
				value={value}
				wrapLines
			/>,
		);

		const editor = screen.getByLabelText("Editable result preview");
		expect(editor).toHaveAttribute("wrap", "off");
		expect(editor).toHaveClass("whitespace-pre");
	});

	it("still wraps result lines that exceed the editor width", () => {
		render(
			<ConflictResultPanel
				isManualResult={false}
				isResolved={false}
				onEdit={vi.fn()}
				value={"x".repeat(200)}
				wrapLines
			/>,
		);

		const editor = screen.getByLabelText("Editable result preview");
		expect(editor).toHaveAttribute("wrap", "soft");
		expect(editor).toHaveClass("whitespace-pre-wrap");
	});

	it("measures long lines and tab stops for wrapping decisions", () => {
		expect(measureConflictText("short\n123\tX")).toEqual({
			lineCount: 2,
			maximumColumns: 5,
		});
	});
});
