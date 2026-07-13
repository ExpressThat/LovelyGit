import {
	ClipboardCopy,
	ExternalLink,
	GitBranch,
	GitCommitHorizontal,
	Info,
	ListRestart,
	ListTree,
	SearchCode,
	Tag,
	Undo2,
	Unplug,
} from "@/components/icons/lovelyIcons";
import { openRemoteWebResource } from "@/components/TopNavBar/components/RepositoryCommands";
import {
	ContextMenuGroup,
	ContextMenuItem,
	ContextMenuLabel,
	ContextMenuSeparator,
} from "@/components/ui/context-menu";
import type { CommitGraphRow } from "@/generated/types";
import { copyToClipboard } from "../utils/clipboard";
import { shortHash } from "../utils/format";
import { CommitComparisonMenuItem } from "./CommitComparisonMenuItem";
import { CommitExportMenuItems } from "./CommitExportMenuItems";

export type CommitContextMenuPopupProps = {
	comparisonBase: CommitGraphRow | null;
	copyPatchBusy: boolean;
	archiveBusy: boolean;
	savePatchBusy: boolean;
	currentBranchName: string | null;
	isHead: boolean;
	operationIncludesHead: boolean;
	operationSelectionCount: number;
	onCherryPick: (row: CommitGraphRow) => void;
	onCheckoutCommit: (row: CommitGraphRow) => void;
	onCompare: (row: CommitGraphRow) => void;
	onCreateTag: (row: CommitGraphRow) => void;
	onCreateBranch: (row: CommitGraphRow) => void;
	onCopyPatch: (row: CommitGraphRow) => void;
	onSaveArchive: (row: CommitGraphRow) => void;
	onSavePatch: (row: CommitGraphRow) => void;
	onOpenDetails: (row: CommitGraphRow) => void;
	onSetComparisonBase: (row: CommitGraphRow | null) => void;
	onStartBisect: (row: CommitGraphRow) => void;
	onInteractiveRebase: (row: CommitGraphRow) => void;
	onRevert: (row: CommitGraphRow) => void;
	onReset: (row: CommitGraphRow) => void;
	repositoryId: string | null;
	row: CommitGraphRow;
};

export function CommitContextMenuItems(props: CommitContextMenuPopupProps) {
	const { row } = props;
	const abbreviatedHash = shortHash(row.commit.hash);
	const subject = row.commit.message || "(no commit message)";
	return (
		<>
			<ContextMenuGroup>
				<ContextMenuLabel className="grid max-w-88 gap-0.5 normal-case">
					<span className="truncate text-foreground">{subject}</span>
					<span className="font-mono font-normal text-[10px]">
						{abbreviatedHash}
					</span>
				</ContextMenuLabel>
			</ContextMenuGroup>
			<ContextMenuSeparator />
			<PrimaryItems {...props} abbreviatedHash={abbreviatedHash} />
			<CommitExportMenuItems
				archiveBusy={props.archiveBusy}
				copyPatchBusy={props.copyPatchBusy}
				onCopyPatch={props.onCopyPatch}
				onSaveArchive={props.onSaveArchive}
				onSavePatch={props.onSavePatch}
				row={row}
				savePatchBusy={props.savePatchBusy}
			/>
			<ContextMenuSeparator />
			<MutationItems {...props} abbreviatedHash={abbreviatedHash} />
		</>
	);
}

type ItemProps = CommitContextMenuPopupProps & { abbreviatedHash: string };

function PrimaryItems(props: ItemProps) {
	const { abbreviatedHash, currentBranchName, isHead, row } = props;
	return (
		<>
			<ContextMenuItem onClick={() => props.onOpenDetails(row)}>
				<Info aria-hidden="true" />
				Open commit details
			</ContextMenuItem>
			<ContextMenuItem
				disabled={currentBranchName === null && isHead}
				onClick={() => props.onCheckoutCommit(row)}
				title={
					currentBranchName === null && isHead
						? "This commit is already checked out in detached HEAD mode"
						: `Checkout ${abbreviatedHash} without moving a branch`
				}
			>
				<Unplug aria-hidden="true" />
				Checkout {abbreviatedHash} (detached)…
			</ContextMenuItem>
			<CommitComparisonMenuItem
				base={props.comparisonBase}
				onCompare={props.onCompare}
				onSetBase={props.onSetComparisonBase}
				row={row}
			/>
			<ContextMenuItem
				disabled={currentBranchName === null || isHead}
				onClick={() => props.onInteractiveRebase(row)}
				title={
					isHead
						? "Select an earlier commit as the rebase base"
						: `Edit commits after ${abbreviatedHash} on ${currentBranchName}`
				}
			>
				<ListTree aria-hidden="true" />
				<span className="min-w-0 truncate">
					Interactively rebase {currentBranchName ?? "current branch"} after{" "}
					{abbreviatedHash}…
				</span>
			</ContextMenuItem>
			<ContextMenuItem
				aria-label={`Start bisect with ${abbreviatedHash} as good`}
				disabled={currentBranchName === null || isHead}
				onClick={() => props.onStartBisect(row)}
				title={
					currentBranchName === null
						? "Check out a branch before starting bisect"
						: isHead
							? "Choose an earlier known-good commit"
							: `Mark ${abbreviatedHash} good and HEAD bad`
				}
			>
				<SearchCode aria-hidden="true" />
				Start bisect: {abbreviatedHash} good, HEAD bad…
			</ContextMenuItem>
			<ContextMenuItem
				onClick={() => void copyToClipboard(row.commit.hash, "Commit hash")}
			>
				<ClipboardCopy aria-hidden="true" />
				Copy commit hash
			</ContextMenuItem>
			<ContextMenuItem
				disabled={!props.repositoryId}
				onClick={() =>
					props.repositoryId &&
					void openRemoteWebResource(
						props.repositoryId,
						"Commit",
						row.commit.hash,
					)
				}
			>
				<ExternalLink aria-hidden="true" />
				Open commit on remote website
			</ContextMenuItem>
		</>
	);
}

function MutationItems(props: ItemProps) {
	const {
		abbreviatedHash,
		currentBranchName,
		isHead,
		operationIncludesHead,
		operationSelectionCount,
		row,
	} = props;
	const target =
		operationSelectionCount > 1
			? `${operationSelectionCount} selected commits`
			: abbreviatedHash;
	return (
		<>
			<ContextMenuItem onClick={() => props.onCreateTag(row)}>
				<Tag aria-hidden="true" /> Create tag at {abbreviatedHash}
			</ContextMenuItem>
			<ContextMenuItem onClick={() => props.onCreateBranch(row)}>
				<GitBranch aria-hidden="true" /> Create branch at {abbreviatedHash}…
			</ContextMenuItem>
			<ContextMenuItem
				disabled={currentBranchName === null}
				onClick={() => props.onRevert(row)}
				title={
					currentBranchName === null
						? "Check out a branch before reverting"
						: `Revert ${target} on ${currentBranchName}`
				}
			>
				<Undo2 aria-hidden="true" />
				<span className="min-w-0 truncate">
					Revert {target} on {currentBranchName ?? "a branch"}
				</span>
			</ContextMenuItem>
			<ContextMenuItem
				disabled={currentBranchName === null || operationIncludesHead}
				onClick={() => props.onCherryPick(row)}
				title={
					operationIncludesHead
						? "The selection includes the checked-out commit"
						: currentBranchName === null
							? "Check out a branch before cherry-picking"
							: `Cherry-pick ${target} onto ${currentBranchName}`
				}
			>
				<GitCommitHorizontal aria-hidden="true" />
				<span className="min-w-0 truncate">
					Cherry-pick {target} onto {currentBranchName ?? "a branch"}
				</span>
			</ContextMenuItem>
			<ContextMenuItem
				disabled={currentBranchName === null || isHead}
				onClick={() => props.onReset(row)}
				title={
					currentBranchName
						? `Reset ${currentBranchName} to ${abbreviatedHash}`
						: "Check out a branch before resetting"
				}
			>
				<ListRestart aria-hidden="true" />
				<span className="min-w-0 truncate">
					Reset {currentBranchName ?? "current branch"} to {abbreviatedHash}…
				</span>
			</ContextMenuItem>
		</>
	);
}
