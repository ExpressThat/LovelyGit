// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { CommitFileDiffLine } from "@/generated/types";
import { SideBySideRow } from "./SideBySideRow";

describe("SideBySideRow partial-stage controls", () => {
	it("keeps individual-line and whole-hunk actions distinct", async () => {
		const user = userEvent.setup();
		const onLine = vi.fn();
		const onHunk = vi.fn();
		render(
			<SideBySideRow
				hunkAction={{ kind: "stage", lines: [line], onClick: onHunk }}
				line={line}
				lineAction={{ kind: "stage", onClick: onLine }}
				scrollLeft={0}
				side="new"
				wrapLines={false}
			/>,
		);

		await user.click(screen.getByRole("button", { name: "Stage line" }));
		await user.click(screen.getByRole("button", { name: "Stage hunk" }));

		expect(onLine).toHaveBeenCalledWith(line);
		expect(onHunk).toHaveBeenCalledWith([line]);
	});

	it("disables both actions while an index mutation is active", () => {
		render(
			<SideBySideRow
				hunkAction={{ kind: "unstage", lines: [line], onClick: vi.fn() }}
				isLineActionBusy
				line={line}
				lineAction={{ kind: "unstage", onClick: vi.fn() }}
				scrollLeft={0}
				side="new"
				wrapLines={false}
			/>,
		);

		expect(screen.getByRole("button", { name: "Unstage line" })).toBeDisabled();
		expect(screen.getByRole("button", { name: "Unstage hunk" })).toBeDisabled();
	});
});

const line: CommitFileDiffLine = {
	changeSpans: [],
	changeType: "Modified",
	newChangeSpans: [],
	newLineNumber: 1,
	newSyntaxSpans: [],
	newText: "new",
	oldChangeSpans: [],
	oldLineNumber: 1,
	oldSyntaxSpans: [],
	oldText: "old",
	syntaxSpans: [],
	text: "new",
};
