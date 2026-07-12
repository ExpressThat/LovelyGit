// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { CommitContextMenu } from "./CommitContextMenu";

const openRemoteWebResource = vi.hoisted(() => vi.fn());

vi.mock("@/components/TopNavBar/components/RepositoryCommands", () => ({
	openRemoteWebResource,
}));

describe("CommitContextMenu", () => {
	it("offers a branch-aware reset for a historical commit", async () => {
		const user = userEvent.setup();
		const onReset = vi.fn();
		renderMenu({ onReset });

		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));
		expect(await screen.findByText("Historical commit")).toBeVisible();
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

	it("copies the selected commit as a patch", async () => {
		const user = userEvent.setup();
		const onCopyPatch = vi.fn();
		renderMenu({ onCopyPatch });
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));

		await user.click(await screen.findByText("Copy commit as patch"));

		expect(onCopyPatch).toHaveBeenCalledWith(row);
	});

	it("opens the selected commit on the remote website", async () => {
		const user = userEvent.setup();
		renderMenu({});
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));

		await user.click(await screen.findByText("Open commit on remote website"));

		expect(openRemoteWebResource).toHaveBeenCalledWith(
			"repo",
			"Commit",
			row.commit.hash,
		);
	});

	it("saves the selected commit as a patch", async () => {
		const user = userEvent.setup();
		const onSavePatch = vi.fn();
		renderMenu({ onSavePatch });
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));

		await user.click(await screen.findByText("Save commit as patch…"));

		expect(onSavePatch).toHaveBeenCalledWith(row);
	});

	it("exports the selected commit tree as an archive", async () => {
		const user = userEvent.setup();
		const onSaveArchive = vi.fn();
		renderMenu({ onSaveArchive });
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));

		await user.click(await screen.findByText("Export commit archive…"));

		expect(onSaveArchive).toHaveBeenCalledWith(row);
	});

	it("opens a commit-aware bisect confirmation", async () => {
		const user = userEvent.setup();
		renderMenu({});
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));

		await user.click(
			await screen.findByRole("menuitem", {
				name: "Start bisect with 1111111 as good",
			}),
		);

		expect(
			await screen.findByRole("heading", { name: "Start a bisect session?" }),
		).toBeVisible();
	});

	it("offers an explicit detached checkout", async () => {
		const user = userEvent.setup();
		const onCheckoutCommit = vi.fn();
		renderMenu({ onCheckoutCommit });
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));

		await user.click(await screen.findByText("Checkout 1111111 (detached)…"));
		expect(onCheckoutCommit).toHaveBeenCalledWith(row);
	});

	it("marks a comparison base then compares another commit against it", async () => {
		const user = userEvent.setup();
		const onSetComparisonBase = vi.fn();
		const first = renderMenu({ onSetComparisonBase });
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));
		await user.click(
			await screen.findByText("Select 1111111 as comparison base"),
		);
		expect(onSetComparisonBase).toHaveBeenCalledWith(row);
		first.unmount();

		const onCompare = vi.fn();
		renderMenu({
			comparisonBase: {
				...row,
				commit: {
					...row.commit,
					hash: "2222222222222222222222222222222222222222",
				},
			},
			onCompare,
		});
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));
		await user.click(await screen.findByText("Compare 2222222 with 1111111"));
		expect(onCompare).toHaveBeenCalledWith(row);
	});
});

function renderMenu({
	comparisonBase = null,
	isHead = false,
	onCreateBranch = vi.fn(),
	onInteractiveRebase = vi.fn(),
	onCopyPatch = vi.fn(),
	onSavePatch = vi.fn(),
	onSaveArchive = vi.fn(),
	onReset = vi.fn(),
	onCompare = vi.fn(),
	onCheckoutCommit = vi.fn(),
	onSetComparisonBase = vi.fn(),
}: {
	comparisonBase?: CommitGraphRow | null;
	isHead?: boolean;
	onCreateBranch?: (selected: CommitGraphRow) => void;
	onInteractiveRebase?: (selected: CommitGraphRow) => void;
	onCopyPatch?: (selected: CommitGraphRow) => void;
	onSavePatch?: (selected: CommitGraphRow) => void;
	onSaveArchive?: (selected: CommitGraphRow) => void;
	onReset?: (selected: CommitGraphRow) => void;
	onCompare?: (selected: CommitGraphRow) => void;
	onCheckoutCommit?: (selected: CommitGraphRow) => void;
	onSetComparisonBase?: (selected: CommitGraphRow | null) => void;
}) {
	return render(
		<CommitContextMenu
			archiveBusy={false}
			comparisonBase={comparisonBase}
			copyPatchBusy={false}
			savePatchBusy={false}
			currentBranchName="main"
			isHead={isHead}
			onCherryPick={vi.fn()}
			onCheckoutCommit={onCheckoutCommit}
			onCompare={onCompare}
			onCreateBranch={onCreateBranch}
			onCopyPatch={onCopyPatch}
			onSavePatch={onSavePatch}
			onSaveArchive={onSaveArchive}
			onCreateTag={vi.fn()}
			onOpenDetails={vi.fn()}
			onSetComparisonBase={onSetComparisonBase}
			onInteractiveRebase={onInteractiveRebase}
			onReset={onReset}
			onRevert={vi.fn()}
			repositoryId="repo"
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
