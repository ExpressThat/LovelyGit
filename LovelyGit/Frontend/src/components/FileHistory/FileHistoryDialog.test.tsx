// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { FileHistoryResponse, FileHistoryResult } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { FileHistoryDialog } from "./FileHistoryDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

describe("FileHistoryDialog", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads native path history and opens a selected commit", async () => {
		const user = userEvent.setup();
		const second = historyResult({
			hash: "2222222234567890",
			subject: "Older change",
		});
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(
			historyResponse([historyResult(), second]),
		);
		const onOpenChange = vi.fn();
		const onSelectCommit = vi.fn();
		render(
			<FileHistoryDialog
				onOpenChange={onOpenChange}
				onSelectCommit={onSelectCommit}
				repositoryId="repo"
				target={{ path: "src/file.ts", startCommitHash: "abc123" }}
			/>,
		);

		await screen.findByRole("button", { name: /Older change/ });
		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			{
				arguments: {
					deep: false,
					knownRepositoryId: "repo",
					limit: 100,
					path: "src/file.ts",
					startCommitHash: "abc123",
				},
				commandType: "GetFileHistory",
			},
			undefined,
		);
		await user.type(
			screen.getByRole("textbox", { name: "Filter file history" }),
			"Older",
		);
		expect(screen.queryByRole("button", { name: /Latest change/ })).toBeNull();
		await user.keyboard("{Enter}");

		expect(onOpenChange).toHaveBeenCalledWith(false);
		expect(onSelectCommit).toHaveBeenCalledWith(second.hash);
	});

	it("offers an explicit deeper traversal for partial results", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce({ ...historyResponse([]), isPartial: true })
			.mockResolvedValueOnce(historyResponse());
		render(
			<FileHistoryDialog
				onOpenChange={vi.fn()}
				onSelectCommit={vi.fn()}
				repositoryId="repo"
				target={{ path: "src/file.ts", startCommitHash: null }}
			/>,
		);

		await user.click(
			await screen.findByRole("button", { name: "Search deeper history" }),
		);
		await waitFor(() =>
			expect(sendRequestWithResponse).toHaveBeenCalledTimes(2),
		);
		expect(sendRequestWithResponse).toHaveBeenLastCalledWith(
			expect.objectContaining({
				arguments: expect.objectContaining({ deep: true, limit: 250 }),
			}),
			{ timeoutMs: 12_000 },
		);
	});

	it("offers deeper history when the completed result is truncated", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce({
				...historyResponse(),
				matchingCommitCount: 251,
			})
			.mockResolvedValueOnce(historyResponse());
		render(
			<FileHistoryDialog
				onOpenChange={vi.fn()}
				onSelectCommit={vi.fn()}
				repositoryId="repo"
				target={{ path: "src/file.ts", startCommitHash: null }}
			/>,
		);

		await user.click(
			await screen.findByRole("button", { name: "Search deeper history" }),
		);
		await waitFor(() =>
			expect(sendRequestWithResponse).toHaveBeenCalledTimes(2),
		);
		expect(sendRequestWithResponse).toHaveBeenLastCalledWith(
			expect.objectContaining({
				arguments: expect.objectContaining({ deep: true, limit: 250 }),
			}),
			{ timeoutMs: 12_000 },
		);
	});

	it("bounds maximum deep-history rendering", async () => {
		const results = Array.from({ length: 250 }, (_, index) =>
			historyResult({
				hash: index.toString(16).padStart(40, "0"),
				subject: `Historical change ${index}`,
			}),
		);
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(
			historyResponse(results),
		);
		const startedAt = performance.now();
		render(
			<FileHistoryDialog
				onOpenChange={vi.fn()}
				onSelectCommit={vi.fn()}
				repositoryId="repo"
				target={{ path: "src/file.ts", startCommitHash: null }}
			/>,
		);

		await screen.findByRole("button", { name: /Historical change 0/ });
		const elapsed = performance.now() - startedAt;
		expect(
			screen.getAllByRole("button", { name: /Historical change/ }),
		).toHaveLength(10);
		expect(elapsed).toBeLessThan(250);
		expect(
			screen.queryByRole("button", { name: /Historical change 249/ }),
		).toBeNull();
	});
});

function historyResponse(results = [historyResult()]): FileHistoryResponse {
	return {
		isPartial: false,
		matchingCommitCount: results.length,
		path: "src/file.ts",
		results,
		scannedCommitCount: 42,
	};
}

function historyResult(
	overrides: Partial<FileHistoryResult> = {},
): FileHistoryResult {
	return {
		author: "Ross",
		changeKind: "Modified",
		date: 1_750_000_000,
		email: "ross@example.invalid",
		hash: "1111111134567890",
		path: "src/file.ts",
		previousPath: null,
		subject: "Latest change",
		...overrides,
	};
}
