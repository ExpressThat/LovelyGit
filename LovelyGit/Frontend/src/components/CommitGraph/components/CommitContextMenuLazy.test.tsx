// @vitest-environment jsdom

import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import type { CommitContextMenuPopupProps } from "./CommitContextMenuPopup";

const itemRender = vi.hoisted(() => vi.fn());
vi.mock("./CommitContextMenuPopup", async (importOriginal) => {
	const actual =
		await importOriginal<typeof import("./CommitContextMenuPopup")>();
	return {
		...actual,
		CommitContextMenuItems: (props: CommitContextMenuPopupProps) => {
			itemRender();
			return <actual.CommitContextMenuItems {...props} />;
		},
	};
});

import { CommitContextMenu } from "./CommitContextMenu";

describe("CommitContextMenu lazy popup", () => {
	it("does not render popup items until the row is right-clicked", async () => {
		render(
			<CommitContextMenu
				archiveBusy={false}
				comparisonBase={null}
				copyPatchBusy={false}
				currentBranchName="main"
				isHead={false}
				operationIncludesHead={false}
				operationSelectionCount={1}
				onCherryPick={vi.fn()}
				onCheckoutCommit={vi.fn()}
				onCompare={vi.fn()}
				onCopyPatch={vi.fn()}
				onCreateBranch={vi.fn()}
				onCreateTag={vi.fn()}
				onInteractiveRebase={vi.fn()}
				onOpenDetails={vi.fn()}
				onReset={vi.fn()}
				onRevert={vi.fn()}
				onSaveArchive={vi.fn()}
				onSavePatch={vi.fn()}
				onSetComparisonBase={vi.fn()}
				onStartBisect={vi.fn()}
				repositoryId="repo"
				row={row}
				savePatchBusy={false}
			>
				<button type="button">commit row</button>
			</CommitContextMenu>,
		);

		expect(itemRender).not.toHaveBeenCalled();
		fireEvent.contextMenu(screen.getByRole("button", { name: "commit row" }));
		await waitFor(() => expect(itemRender).toHaveBeenCalledOnce());
	});
});

const row = {
	commit: { hash: "1".repeat(40), message: "Subject" },
} as CommitGraphRow;
