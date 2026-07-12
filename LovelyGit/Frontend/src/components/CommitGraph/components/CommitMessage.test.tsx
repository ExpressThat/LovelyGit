// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { CommitMessage } from "./CommitMessage";

describe("CommitMessage signature metadata", () => {
	it("shows signature presence beside the graph subject", () => {
		render(<CommitMessage row={row("Ssh")} />);

		expect(screen.getByLabelText("Signed commit (SSH)")).toBeVisible();
		expect(screen.getByText("Signed subject")).toBeVisible();
	});

	it("keeps unsigned graph subjects free of a signature badge", () => {
		render(<CommitMessage row={row("None")} />);
		expect(screen.queryByLabelText(/Signed commit/)).not.toBeInTheDocument();
	});
});

function row(signatureKind: "None" | "Ssh") {
	return {
		activeLanesAbove: [],
		activeLanesBelow: [],
		colorIndex: 0,
		commit: {
			author: "Author",
			date: 0,
			email: "author@example.invalid",
			hash: "a".repeat(40),
			message: "Signed subject",
			refs: [],
			signatureKind,
			stats: null,
		},
		edgesAbove: [],
		edgesBelow: [],
		isBranchTip: false,
		isMergeCommit: false,
		lane: 0,
		laneColorsAbove: [],
		laneColorsBelow: [],
		rowIndex: 0,
	} satisfies CommitGraphRow;
}
