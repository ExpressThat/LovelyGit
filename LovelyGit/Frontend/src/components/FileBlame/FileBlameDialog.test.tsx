// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { FileBlameResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { FileBlameDialog } from "./FileBlameDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

describe("FileBlameDialog", () => {
	beforeEach(() => vi.clearAllMocks());

	it("shows compact native attribution statistics", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(response());
		render(
			<FileBlameDialog
				onOpenChange={vi.fn()}
				onSelectCommit={vi.fn()}
				repositoryId="repo"
				target={{ path: "src/file.ts", startCommitHash: "abc123456" }}
			/>,
		);

		expect(
			await screen.findByText("2 of 2 lines · 7 commits scanned"),
		).toBeVisible();
		expect(screen.getByText("at abc1234")).toBeVisible();
		expect(
			screen.getByRole("region", { name: "Blamed file contents" }),
		).toBeVisible();
	});

	it("lets users explicitly trace unresolved older lines", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce({
				...response(),
				isPartial: true,
				resolvedLineCount: 1,
			})
			.mockResolvedValueOnce(response());
		render(
			<FileBlameDialog
				onOpenChange={vi.fn()}
				onSelectCommit={vi.fn()}
				repositoryId="repo"
				target={{ path: "src/file.ts", startCommitHash: null }}
			/>,
		);

		await user.click(
			await screen.findByRole("button", { name: "Trace older lines" }),
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
});

function response(): FileBlameResponse {
	return {
		compactPayloadGzipBase64: "",
		content: "one\ntwo\n",
		hunks: [
			{
				author: "Ross",
				date: 1_750_000_000,
				email: "ross@example.invalid",
				hash: "abc123456",
				lineCount: 2,
				startLine: 1,
				subject: "Change lines",
			},
		],
		isPartial: false,
		lineCount: 2,
		path: "src/file.ts",
		resolvedLineCount: 2,
		scannedCommitCount: 7,
		startCommitHash: "abc123456",
	};
}
