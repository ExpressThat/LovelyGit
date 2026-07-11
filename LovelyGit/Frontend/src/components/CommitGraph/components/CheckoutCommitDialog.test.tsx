// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { CheckoutCommitDialog } from "./CheckoutCommitDialog";

const toast = vi.hoisted(() => ({
	error: vi.fn(),
	loading: vi.fn(() => "toast"),
	success: vi.fn(),
}));
vi.mock("sonner", () => ({ toast }));
vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
const send = vi.mocked(sendRequestWithResponse);

describe("CheckoutCommitDialog", () => {
	beforeEach(() => {
		send.mockReset();
		toast.error.mockReset();
		toast.success.mockReset();
	});

	it("confirms detached checkout and refreshes the repository", async () => {
		const user = userEvent.setup();
		send.mockResolvedValueOnce(undefined);
		const onClose = vi.fn();
		const onRepositoryChanged = vi.fn();
		renderDialog({ onClose, onRepositoryChanged });

		expect(screen.getByText(/branches remain unchanged/)).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "Checkout detached" }));
		await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
		expect(send).toHaveBeenCalledWith(
			{
				arguments: { commitHash: row.commit.hash, repositoryId: "repo" },
				commandType: "CheckoutCommit",
			},
			{ timeoutMs: 120_000 },
		);
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
	});

	it("keeps the dialog retryable after Git rejects dirty changes", async () => {
		const user = userEvent.setup();
		send
			.mockRejectedValueOnce(new Error("local changes would be overwritten"))
			.mockResolvedValueOnce(undefined);
		const onClose = vi.fn();
		renderDialog({ onClose, onRepositoryChanged: vi.fn() });

		await user.click(screen.getByRole("button", { name: "Checkout detached" }));
		await waitFor(() =>
			expect(toast.error).toHaveBeenCalledWith(
				"local changes would be overwritten",
				{ id: "toast" },
			),
		);
		expect(onClose).not.toHaveBeenCalled();
		await user.click(screen.getByRole("button", { name: "Checkout detached" }));
		await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
	});
});

function renderDialog({
	onClose,
	onRepositoryChanged,
}: {
	onClose: () => void;
	onRepositoryChanged: () => void;
}) {
	return render(
		<CheckoutCommitDialog
			commit={row}
			onClose={onClose}
			onRepositoryChanged={onRepositoryChanged}
			repositoryId="repo"
		/>,
	);
}

const row = {
	commit: {
		hash: "1111111111111111111111111111111111111111",
		message: "Historical snapshot",
	},
} as CommitGraphRow;
