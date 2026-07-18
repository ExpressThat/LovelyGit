// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { GitReflogEntry } from "@/generated/types";
import { useReflog } from "../hooks/useReflog";
import { ReflogDialog } from "./ReflogDialog";

vi.mock("../hooks/useReflog", () => ({ useReflog: vi.fn() }));

describe("ReflogDialog", () => {
	it("bounds rendering for the maximum reflog response", () => {
		const responseEntries = entries(200);
		const onCreateBranch = vi.fn();
		vi.mocked(useReflog).mockReturnValue({
			entries: responseEntries,
			error: null,
			filteredEntries: responseEntries,
			isLoading: false,
			query: "",
			setQuery: vi.fn(),
		});
		const startedAt = performance.now();
		render(
			<ReflogDialog
				branchName="main"
				onClose={vi.fn()}
				onCreateBranch={onCreateBranch}
				onReset={vi.fn()}
				repositoryId="repo"
			/>,
		);
		const elapsed = performance.now() - startedAt;

		expect(
			screen.getAllByRole("button", { name: /Create recovery branch at/ }),
		).toHaveLength(10);
		expect(elapsed).toBeLessThan(250);
		expect(screen.getByText("commit: performance entry 0")).toBeInTheDocument();
		expect(screen.queryByText("commit: performance entry 199")).toBeNull();
		fireEvent.click(
			screen.getByRole("button", {
				name: "Create recovery branch at main@{0}",
			}),
		);
		expect(onCreateBranch).toHaveBeenCalledWith(responseEntries[0]);
	});

	it("keeps the empty-filter state lightweight and actionable", () => {
		const setQuery = vi.fn();
		vi.mocked(useReflog).mockReturnValue({
			entries: entries(200),
			error: null,
			filteredEntries: [],
			isLoading: false,
			query: "missing",
			setQuery,
		});

		render(
			<ReflogDialog
				branchName="main"
				onClose={vi.fn()}
				onCreateBranch={vi.fn()}
				onReset={vi.fn()}
				repositoryId="repo"
			/>,
		);

		expect(
			screen.getByText("No reflog entries match this filter."),
		).toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: /Create recovery branch/ }),
		).toBeNull();
		fireEvent.click(
			screen.getByRole("button", { name: "Clear reflog filter" }),
		);
		expect(setQuery).toHaveBeenCalledWith("");
	});
});

function entries(count: number): GitReflogEntry[] {
	return Array.from({ length: count }, (_, index) => ({
		actorEmail: "performance@example.invalid",
		actorName: "LovelyGit Performance",
		message: `commit: performance entry ${index}`,
		newHash: index.toString(16).padStart(40, "0"),
		oldHash: Math.max(0, index - 1)
			.toString(16)
			.padStart(40, "0"),
		selector: `main@{${index}}`,
		timestampUnixSeconds: 1_700_000_000 + index,
		timezone: "+0000",
	}));
}
