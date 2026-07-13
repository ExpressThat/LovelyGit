import type { VirtualItem } from "@tanstack/react-virtual";
import type { CommitGraphRow } from "@/generated/types";
import type { CommitGraphViewProps } from "./CommitGraphViewProps";
import { CommitRow } from "./components/CommitRow";
import { ROW_HEIGHT } from "./constants";
import type { useBranchCreation } from "./hooks/useBranchCreation";
import type { useBranchWorktreeControllers } from "./hooks/useBranchWorktreeControllers";
import type { useCommitGraphDialogs } from "./hooks/useCommitGraphDialogs";
import type { useCommitMultiSelection } from "./hooks/useCommitMultiSelection";
import type { useCommitPatchActions } from "./hooks/useCommitPatchActions";

export function CommitGraphRows({ actions, rows, selection, view }: Props) {
	const selectionIncludesHead = view.currentHeadHash
		? selection.hashes.has(view.currentHeadHash)
		: false;
	const isOperationSelected = (row: CommitGraphRow | null | undefined) =>
		row ? selection.hashes.has(row.commit.hash) : false;
	return actions.virtualItems.map((item) => {
		const row = rows[item.index] ?? null;
		const operationSelected = isOperationSelected(row);
		return (
			<div
				className="absolute left-0 top-0 w-full"
				key={item.key}
				style={{
					height: `${ROW_HEIGHT}px`,
					transform: `translateY(${Math.round(item.start)}px)`,
				}}
			>
				<CommitRow
					archiveBusy={busy(actions.patchActions, "archive", row)}
					branchMutationBusy={actions.branchController.busyBranch !== null}
					branchRemoteName={view.tagRemoteName}
					comparison={actions.dialogs.comparison}
					copyPatchBusy={busy(actions.patchActions, "copy", row)}
					currentBranchName={view.currentBranchName}
					graph={{
						contentWidth: view.graphContentWidth,
						scrollLeft: view.graphScrollLeft,
					}}
					isHead={row?.commit.hash === view.currentHeadHash}
					isOperationSelected={operationSelected}
					isSelected={
						Boolean(row) && row?.commit.hash === view.selectedCommitHash
					}
					onBranchAction={actions.manageBranch}
					onCheckoutCommit={actions.dialogs.setCheckoutCommit}
					onCherryPick={(selected) => {
						actions.dialogs.setCherryPickCommits(
							selection.rowsFor(selected, "cherry-pick"),
						);
						selection.clear();
					}}
					onCopyPatch={actions.patchActions.copyPatch}
					onCreateBranch={actions.branchCreation.createAtCommit}
					onCreateBranchFromTag={actions.branchCreation.createFromTag}
					onCreateTag={actions.dialogs.setTagCommit}
					onIntegrateBranch={actions.dialogs.integrateBranch}
					onInteractiveRebase={actions.dialogs.setInteractiveRebaseBase}
					onReset={actions.dialogs.setResetCommit}
					onRevert={(selected) => {
						actions.dialogs.setRevertCommits(
							selection.rowsFor(selected, "revert"),
						);
						selection.clear();
					}}
					onSaveArchive={actions.patchActions.saveArchive}
					onSavePatch={actions.patchActions.savePatch}
					onSelect={(selected, gesture) =>
						selection.select(
							selected,
							item.index,
							gesture,
							actions.onSelectCommit,
						)
					}
					onStartBisect={actions.dialogs.setBisectCommit}
					onTagAction={actions.tagController.manageTag}
					operationIncludesHead={
						Boolean(view.currentHeadHash) &&
						(operationSelected
							? selectionIncludesHead
							: row?.commit.hash === view.currentHeadHash)
					}
					operationSelectionCount={operationSelected ? selection.count : 1}
					remotePrefixes={view.remotePrefixes}
					repositoryId={view.repositoryId}
					row={row}
					rowIndex={item.index}
					savePatchBusy={busy(actions.patchActions, "save", row)}
					tagMutationBusy={actions.tagController.busyTag !== null}
					tagRemoteName={view.tagRemoteName}
					templateColumns={view.templateColumns}
				/>
			</div>
		);
	});
}

function busy(
	actions: ReturnType<typeof useCommitPatchActions>,
	kind: "archive" | "copy" | "save",
	row: CommitGraphRow | null,
) {
	return (
		actions.busyAction === kind && actions.busyCommitHash === row?.commit.hash
	);
}

type Props = {
	actions: {
		branchController: ReturnType<
			typeof useBranchWorktreeControllers
		>["branchController"];
		branchCreation: ReturnType<typeof useBranchCreation>;
		dialogs: ReturnType<typeof useCommitGraphDialogs>;
		manageBranch: ReturnType<
			typeof useBranchWorktreeControllers
		>["manageBranch"];
		onSelectCommit: CommitGraphViewProps["onSelectCommit"];
		patchActions: ReturnType<typeof useCommitPatchActions>;
		tagController: ReturnType<
			typeof useBranchWorktreeControllers
		>["tagController"];
		virtualItems: VirtualItem[];
	};
	rows: Array<CommitGraphRow | null>;
	selection: ReturnType<typeof useCommitMultiSelection>;
	view: {
		currentBranchName: string | null;
		currentHeadHash: string | null;
		graphContentWidth: number;
		graphScrollLeft: number;
		remotePrefixes: string[];
		repositoryId: string | null;
		selectedCommitHash: string | null;
		tagRemoteName: string | null;
		templateColumns: string;
	};
};
