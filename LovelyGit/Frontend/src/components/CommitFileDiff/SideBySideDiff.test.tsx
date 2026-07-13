// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { CommitFileDiffLine } from "@/generated/types";
import { SideBySideDiff } from "./SideBySideDiff";

vi.mock("@tanstack/react-virtual", () => ({
	useVirtualizer: () => ({
		getTotalSize: () => 18,
		getVirtualItems: () => [{ index: 0, key: "row-0", size: 18, start: 0 }],
		measureElement: vi.fn(),
	}),
}));

describe("SideBySideDiff", () => {
	it("renders each visible source cell once while measuring the row", () => {
		const line: CommitFileDiffLine = {
			changeSpans: [],
			changeType: "Modified",
			newChangeSpans: [],
			newLineNumber: 7,
			newSyntaxSpans: [],
			newText: "after unique value",
			oldChangeSpans: [],
			oldLineNumber: 7,
			oldSyntaxSpans: [],
			oldText: "before unique value",
			syntaxSpans: [],
			text: "",
		};

		render(
			<SideBySideDiff lines={[{ kind: "line", line }]} wrapLines={false} />,
		);

		expect(screen.getAllByText("before unique value")).toHaveLength(1);
		expect(screen.getAllByText("after unique value")).toHaveLength(1);
		expect(document.querySelector('[aria-hidden="true"]')).toBeNull();
	});
});
