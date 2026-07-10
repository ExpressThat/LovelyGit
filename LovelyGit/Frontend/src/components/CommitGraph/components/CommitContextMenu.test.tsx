// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { CommitContextMenu } from "./CommitContextMenu";

describe("CommitContextMenu", () => {
	it("offers a branch-aware reset for a historical commit", async () => {
		const user = userEvent.setup();
		const onReset = vi.fn();
		renderMenu({ onReset });

		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));
		const action = await screen.findByText("Reset main to 1111111…");
		expect(action.closest('[role="menuitem"]')).not.toHaveAttribute(
			"aria-disabled",
			"true",
		);
		await user.click(action);

		expect(onReset).toHaveBeenCalledWith(row);
	});

	it("disables reset when the selected commit is HEAD", async () => {
		renderMenu({ isHead: true });
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));

		expect(
			(await screen.findByText("Reset main to 1111111…")).closest(
				'[role="menuitem"]',
			),
		).toHaveAttribute("aria-disabled", "true");
	});

	it("creates a branch from the selected commit", async () => {
		const user = userEvent.setup();
		const onCreateBranch = vi.fn();
		renderMenu({ onCreateBranch });
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));

		await user.click(await screen.findByText("Create branch at 1111111…"));

		expect(onCreateBranch).toHaveBeenCalledWith(row);
	});

	it("starts an interactive rebase from a historical commit", async () => {
		const user = userEvent.setup();
		const onInteractiveRebase = vi.fn();
		renderMenu({ onInteractiveRebase });
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));

		await user.click(
			await screen.findByText(/Interactively rebase main after/),
		);

		expect(onInteractiveRebase).toHaveBeenCalledWith(row);
	});
});

function renderMenu({
	isHead = false,
	onCreateBranch = vi.fn(),
	onInteractiveRebase = vi.fn(),
	onReset = vi.fn(),
}: {
	isHead?: boolean;
	onCreateBranch?: (selected: CommitGraphRow) => void;
	onInteractiveRebase?: (selected: CommitGraphRow) => void;
	onReset?: (selected: CommitGraphRow) => void;
}) {
	return render(
		<CommitContextMenu
			currentBranchName="main"
			isHead={isHead}
			onCherryPick={vi.fn()}
			onCreateBranch={onCreateBranch}
			onCreateTag={vi.fn()}
			onOpenDetails={vi.fn()}
			onInteractiveRebase={onInteractiveRebase}
			onReset={onReset}
			onRevert={vi.fn()}
			row={row}
		>
			<button type="button">commit row</button>
		</CommitContextMenu>,
	);
}

const row = {
	commit: {
		hash: "1111111111111111111111111111111111111111",
		message: "Historical commit",
	},
} as CommitGraphRow;
