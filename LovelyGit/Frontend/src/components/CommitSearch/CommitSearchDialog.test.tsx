// @vitest-environment jsdom

import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { CommitSearchDialog } from "./CommitSearchDialog";
import { searchResponse, searchResult } from "./CommitSearchTestData";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("./useCommitSearchRefs", () => ({
	useCommitSearchRefs: () => ({ isLoading: false, loadFailed: true, refs: [] }),
}));

describe("CommitSearchDialog", () => {
	beforeEach(() => vi.clearAllMocks());

	it("searches reachable history and opens the selected result", async () => {
		const user = userEvent.setup();
		const first = searchResult();
		const second = searchResult({
			hash: "2222222234567890abcdef1234567890abcdef12",
			subject: "Second needle result",
		});
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(
			searchResponse([first, second]),
		);
		const onOpenChange = vi.fn();
		const onSelectCommit = vi.fn();
		render(
			<CommitSearchDialog
				onOpenChange={onOpenChange}
				onSelectCommit={onSelectCommit}
				open
				repositoryId="repo"
			/>,
		);

		expect(screen.getByText(/native parser/i)).toBeVisible();
		const input = screen.getByRole("textbox", {
			name: "Search commit history",
		});
		await user.type(input, "needle");
		await screen.findByRole("button", { name: /Second needle result/ });
		await user.type(input, "{ArrowDown}{Enter}");

		expect(onOpenChange).toHaveBeenCalledWith(false);
		expect(onSelectCommit).toHaveBeenCalledWith(second.hash);
	});

	it("reports search result and traversal counts", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(searchResponse());
		render(
			<CommitSearchDialog
				onOpenChange={vi.fn()}
				onSelectCommit={vi.fn()}
				open
				repositoryId="repo"
			/>,
		);

		await user.type(
			screen.getByRole("textbox", { name: "Search commit history" }),
			"needle",
		);
		await waitFor(() =>
			expect(screen.getByText(/1 of 1 matches · 42 commits/)).toBeVisible(),
		);
	});

	it("lets users explicitly search deeper after a bounded pass", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce({
				...searchResponse([]),
				isPartial: true,
				scannedCommitCount: 8_000,
			})
			.mockResolvedValueOnce(searchResponse());
		render(
			<CommitSearchDialog
				onOpenChange={vi.fn()}
				onSelectCommit={vi.fn()}
				open
				repositoryId="repo"
			/>,
		);
		await user.type(
			screen.getByRole("textbox", { name: "Search commit history" }),
			"needle",
		);
		await user.click(
			await screen.findByRole("button", { name: "Search deeper" }),
		);

		await waitFor(() =>
			expect(sendRequestWithResponse).toHaveBeenCalledTimes(2),
		);
		expect(sendRequestWithResponse).toHaveBeenLastCalledWith(
			expect.objectContaining({
				arguments: expect.objectContaining({ deep: true }),
			}),
			{ timeoutMs: 12_000 },
		);
	});

	it("searches by author and inclusive date range without message text", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse).mockResolvedValue(searchResponse());
		render(
			<CommitSearchDialog
				onOpenChange={vi.fn()}
				onSelectCommit={vi.fn()}
				open
				repositoryId="repo"
			/>,
		);

		await user.click(
			screen.getByRole("button", { name: "Advanced commit filters" }),
		);
		fireEvent.input(screen.getByLabelText("Filter by author"), {
			target: { value: "Alice" },
		});
		await user.type(screen.getByLabelText("From commit date"), "2024-06-01");
		await user.type(screen.getByLabelText("Until commit date"), "2024-06-30");

		await waitFor(() =>
			expect(sendRequestWithResponse).toHaveBeenLastCalledWith(
				expect.objectContaining({
					arguments: expect.objectContaining({
						afterUnixSeconds: 1717200000,
						author: "Alice",
						beforeUnixSeconds: 1719792000,
						query: "",
						scope: "",
					}),
				}),
				undefined,
			),
		);
	});

	it("scopes a filter-only search to a branch or tag", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse).mockResolvedValue(searchResponse());
		render(
			<CommitSearchDialog
				onOpenChange={vi.fn()}
				onSelectCommit={vi.fn()}
				open
				repositoryId="repo"
			/>,
		);

		await user.click(
			screen.getByRole("button", { name: "Advanced commit filters" }),
		);
		fireEvent.input(screen.getByLabelText("Limit search to branch or tag"), {
			target: { value: "origin/release" },
		});

		await waitFor(() =>
			expect(sendRequestWithResponse).toHaveBeenCalledWith(
				expect.objectContaining({
					arguments: expect.objectContaining({
						query: "",
						scope: "origin/release",
					}),
				}),
				undefined,
			),
		);
	});
});
