// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	ConflictResultPanel,
	measureConflictText,
} from "./ConflictResultPanel";
import { buildLineStarts } from "./ConflictResultPreview";

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

		expect(editor).toHaveAttribute(
			"title",
			"Click to edit the complete output",
		);
		expect(editor.querySelectorAll("[data-result-line-number]")).toHaveLength(
			30,
		);
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
		expect(editor.tagName).toBe("BUTTON");
		expect(editor.querySelector(".whitespace-pre")).toBeTruthy();
	});

	it("activates the full editor only when a large result is edited", () => {
		const onEdit = vi.fn();
		const value = "result line\n".repeat(20_000);
		render(
			<ConflictResultPanel
				isManualResult={false}
				isResolved={false}
				onEdit={onEdit}
				value={value}
				wrapLines={false}
			/>,
		);

		const preview = screen.getByLabelText("Editable result preview");
		preview.scrollTop = 180;
		fireEvent.click(preview.querySelector('[data-index="10"]') as Element);
		const editor = screen.getByLabelText(
			"Editable result preview",
		) as HTMLTextAreaElement;
		expect(editor.tagName).toBe("TEXTAREA");
		expect(editor.scrollTop).toBe(180);
		expect(editor.selectionStart).toBe("result line\n".length * 10);
		fireEvent.change(editor, { target: { value: "manual" } });
		expect(onEdit).toHaveBeenCalledWith("manual");
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
		expect(buildLineStarts("one\n")).toEqual([0, 4]);
	});
});
