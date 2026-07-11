// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { CommitFileDiffLine } from "@/generated/types";
import {
	buildDiffHunkLookup,
	DiffHunkActionButton,
	getDiffHunkAction,
} from "./DiffHunkActions";

describe("diff hunk actions", () => {
	it("groups nearby changes and separates changes beyond their context", () => {
		const first = line("Modified");
		const second = line("Inserted");
		const third = line("Deleted");
		const lines = [
			first,
			line(),
			line(),
			second,
			line(),
			line(),
			line(),
			third,
		];
		const lookup = buildDiffHunkLookup(lines, 1);

		expect(lookup.get(first)).toEqual([first, second]);
		expect(lookup.get(third)).toEqual([third]);
		expect(lookup.size).toBe(2);
	});

	it("exposes one accessible action on the first changed line", async () => {
		const user = userEvent.setup();
		const changed = line("Modified");
		const lookup = buildDiffHunkLookup([changed], 3);
		const onStage = vi.fn();
		const action = getDiffHunkAction(changed, lookup, onStage);
		if (!action) throw new Error("Expected a hunk action.");
		render(<DiffHunkActionButton action={action} disabled={false} />);

		await user.click(screen.getByRole("button", { name: "Stage hunk" }));

		expect(onStage).toHaveBeenCalledWith([changed]);
	});
});

function line(changeType = "Unchanged"): CommitFileDiffLine {
	return {
		changeSpans: [],
		changeType,
		newChangeSpans: [],
		newLineNumber: null,
		newSyntaxSpans: [],
		newText: "",
		oldChangeSpans: [],
		oldLineNumber: null,
		oldSyntaxSpans: [],
		oldText: "",
		syntaxSpans: [],
		text: "",
	};
}
