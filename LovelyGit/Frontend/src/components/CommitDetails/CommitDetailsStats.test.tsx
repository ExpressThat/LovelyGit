// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { CommitDetailsStats } from "./CommitDetailsStats";

describe("CommitDetailsStats", () => {
	it("shows exact totals for ordinary commits", () => {
		render(
			<CommitDetailsStats
				fileCount={3}
				hasLineStats
				stats={{ additions: 12, deletions: 4 }}
			/>,
		);

		expect(screen.getByText("+12")).toBeInTheDocument();
		expect(screen.getByText("-4")).toBeInTheDocument();
		expect(screen.queryByText(/totals are deferred/i)).not.toBeInTheDocument();
	});

	it("does not present deferred totals as zero", () => {
		render(
			<CommitDetailsStats
				fileCount={3_003}
				hasLineStats={false}
				stats={{ additions: 0, deletions: 0 }}
			/>,
		);

		expect(screen.getAllByText("Deferred")).toHaveLength(2);
		expect(screen.getByText(/totals are deferred/i)).toBeInTheDocument();
		expect(screen.queryByText("+0")).not.toBeInTheDocument();
	});
});
