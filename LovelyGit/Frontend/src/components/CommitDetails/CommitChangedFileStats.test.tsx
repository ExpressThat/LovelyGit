// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { CommitChangedFileStats } from "./CommitChangedFileStats";

const file = {
	additions: 8,
	deletions: 3,
	isBinary: false,
	path: "src/large.ts",
	status: "Modified",
};

describe("CommitChangedFileStats", () => {
	it("renders exact file totals when available", () => {
		render(<CommitChangedFileStats file={file} visible />);

		expect(screen.getByText("+8")).toBeInTheDocument();
		expect(screen.getByText("-3")).toBeInTheDocument();
	});

	it("hides zero placeholders when totals are deferred", () => {
		const { container } = render(
			<CommitChangedFileStats
				file={{ ...file, additions: 0, deletions: 0 }}
				visible={false}
			/>,
		);

		expect(container).toBeEmptyDOMElement();
	});
});
