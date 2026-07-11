// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import { CommitFileDiffHeader } from "./CommitFileDiffHeader";

const file = {
	additions: 0,
	deletions: 0,
	isBinary: false,
	path: "src/shared.txt",
	status: "Modified",
};

it("hides unavailable line statistics for direct comparisons", () => {
	render(
		<CommitFileDiffHeader file={file} onClose={vi.fn()} showStats={false} />,
	);
	expect(screen.getByText("Modified")).toBeVisible();
	expect(screen.queryByText("+0 -0")).not.toBeInTheDocument();
});

it("keeps known line statistics for ordinary commit diffs", () => {
	render(<CommitFileDiffHeader file={file} onClose={vi.fn()} />);
	expect(screen.getByText("+0 -0")).toBeVisible();
});
